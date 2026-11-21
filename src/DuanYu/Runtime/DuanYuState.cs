using System.Runtime.CompilerServices;
using System.Text.Json;
using BaseLib.Abstracts;
using DuanYu.Cards;
using BaseLib.Patches.Saves;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace DuanYu.Runtime;

public sealed record DuanYuEffect(string CardId, int Amount, int SecondaryAmount, CardModel? Source, int EnergyAmount = 0, int UpgradedMeridianCopies = 0);
public sealed record DuanYuDelayedEffect(string Kind, int Amount);

public sealed class PlayerRuntimeState : DuanYuCombatLedger
{
    public List<DuanYuEffect> Effects { get; } = [];
    public List<DuanYuDelayedEffect> DelayedEffects { get; } = [];
    internal Dictionary<CardPlay, DuanYuPlayRecord> Plays { get; } = [];
    internal Stack<CardPlay> ActivePlays { get; } = [];
    internal bool SealUsed { get; set; }
    internal bool PrayerBeadsUsedThisTurn { get; set; }
    internal bool PreviousTurnWasPeaceful { get; set; }
    internal int StatusExhaustTriggers { get; set; }
    internal ICombatState? Combat { get; set; }
    internal Dictionary<CardModel, DuanYuPlayRecord> PendingCardSpends { get; } = [];
}

internal sealed class DuanYuPlayRecord
{
    internal int SkillOrdinal { get; init; }
    internal int SwordOrdinal { get; init; }
    internal bool ThirdSwordKind { get; init; }
    internal bool SixthSwordKind { get; init; }
    internal List<DuanYuEffect> Effects { get; init; } = [];
    internal int SpendOrdinal { get; set; }
    internal int Spent { get; set; }
    internal bool GrantsSealEnergy { get; set; }
    internal bool GrantsPrayerBlock { get; set; }
}

public static class DuanYuState
{
    public const int LingboReduction = DuanYuCombatLedger.LingboReduction;
    public const int SwordMomentumCap = DuanYuCombatLedger.SwordMomentumCap;
    private static readonly ConditionalWeakTable<Player, PlayerRuntimeState> States = new();
    private static readonly ConditionalWeakTable<Player, DuanYuRunHistory> Histories = new();
    private static bool initialized;

    public static Func<Player, PlayerChoiceContext, int, bool, Task>? GenerateSwordChoice { get; set; }
    public static PlayerRuntimeState Get(Player player) => States.GetOrCreateValue(player);
    public static bool IsDuanYu(Player player) => player.Character.Id.Entry == "CQBZLY_DUAN_YU";
    public static bool CanAct(Player player) => player.Creature.IsAlive && player.Creature.CombatState != null && !CombatManager.Instance.IsEnding;
    public static string CardId(CardModel card) => card.Id.Entry.StartsWith("CQBZLY_", StringComparison.Ordinal) ? card.Id.Entry[7..] : card.Id.Entry;
    public static bool IsSword(CardModel card) => CardId(card) is "DY_T01" or "DY_T02" or "DY_T03" or "DY_T04" or "DY_T05" or "DY_T06";
    internal static bool ReceivesSwordDamageBonus(CardModel card) => IsSword(card) || CardId(card) == "DY_R21";

    public static void Initialize()
    {
        if (initialized) return;
        initialized = true;
        ExtendedSaveHandlers<Player, SerializablePlayer>.RegisterSave<string>(
            "cqbzly.sts2.duanyu.history.v1",
            player => JsonSerializer.Serialize(Histories.GetOrCreateValue(player)),
            (player, saved) =>
            {
                Histories.Remove(player);
                Histories.Add(player, string.IsNullOrEmpty(saved) ? new DuanYuRunHistory() : JsonSerializer.Deserialize<DuanYuRunHistory>(saved) ?? new DuanYuRunHistory());
            },
            (value, writer) => writer.WriteString(value),
            reader => reader.ReadString());
    }

    public static void RecordRelic(Player player, string id) => Histories.GetOrCreateValue(player).Relics.Add(id);
    public static bool HasAcquiredRelic(Player player, string id) => HasRelic(player, id) || Histories.GetOrCreateValue(player).Relics.Contains(id);
    public static void RecordEvent(Player player, string id) => Histories.GetOrCreateValue(player).Events.Add(id);
    public static bool HasVisitedEvent(Player player, string id) => Histories.GetOrCreateValue(player).Events.Contains(id);
    private static bool HasRelic(Player player, string id) => player.Relics.Any(relic => !relic.IsMelted && relic.Id.Entry == id);

    public static async Task GainQi(Player player, int amount, CardModel? source = null)
    {
        if (amount <= 0 || !CanAct(player)) return;
        var state = Get(player);
        state.GainQi(amount);
        SyncQiCounter(player, state.Qi);
        await SyncPower<QiPower>(player, state.Qi, source);
    }

    public static async Task LoseQi(Player player, int amount, CardModel? source = null)
    {
        if (amount <= 0 || !CanAct(player)) return;
        var state = Get(player);
        state.Qi -= Math.Min(state.Qi, amount);
        SyncQiCounter(player, state.Qi);
        await SyncPower<QiPower>(player, state.Qi, source);
    }

    public static Task<bool> SpendQi(Player player, int amount, CardModel? source = null)
    {
        var state = Get(player);
        DuanYuPlayRecord? record = null;
        if (state.ActivePlays.TryPeek(out var active)) state.Plays.TryGetValue(active, out record);
        return SpendQi(player, amount, source, record);
    }

    internal static async Task<bool> PayCardQi(DuanYuCard card, int amount)
    {
        var state = Get(card.Owner);
        var record = new DuanYuPlayRecord { Effects = state.Effects.ToList() };
        if (amount == 0 && card.QiCostX)
        {
            if (!CanAct(card.Owner)) return false;
        }
        else if (!await SpendQi(card.Owner, amount, card, record)) return false;
        if (card.QiCostX) CustomResources<QiResource>.Cost(card)!.CapturedXValue = amount;
        state.PendingCardSpends[card] = record;
        return true;
    }

    internal static bool HasCardQiPayment(DuanYuCard card) =>
        Get(card.Owner).PendingCardSpends.ContainsKey(card);

    internal static void ClearCardQiPayment(DuanYuCard card) => Get(card.Owner).PendingCardSpends.Remove(card);

    private static async Task<bool> SpendQi(Player player, int amount, CardModel? source, DuanYuPlayRecord? record)
    {
        var state = Get(player);
        if (!CanAct(player) || !state.TrySpendQi(amount)) return false;
        if (record != null)
        {
            record.Spent = amount;
            record.SpendOrdinal = state.SpendingCount;
            if (state.Qi == 0 && !state.SealUsed && HasRelic(player, "CQBZLY_DY_RELIC06"))
            {
                state.SealUsed = true;
                record.GrantsSealEnergy = true;
            }
            if (amount >= 3 && !state.PrayerBeadsUsedThisTurn && HasRelic(player, "CQBZLY_DY_RELIC04"))
            {
                state.PrayerBeadsUsedThisTurn = true;
                record.GrantsPrayerBlock = true;
            }
        }
        SyncQiCounter(player, state.Qi);
        await SyncPower<QiPower>(player, state.Qi, source);
        return true;
    }

    public static async Task AfterQiSpent(Player player, PlayerChoiceContext context, CardModel? source = null)
    {
        if (!CanAct(player)) return;
        var state = Get(player);
        var effects = state.Effects.ToList();
        var ordinal = state.SpendingCount;
        if (state.ActivePlays.TryPeek(out var active) && state.Plays.TryGetValue(active, out var record))
        {
            effects = record.Effects;
            ordinal = record.SpendOrdinal;
        }
        if (!state.TryProcessSpend(ordinal)) return;
        foreach (var effect in effects)
        {
            if (!CanAct(player)) break;
            if (effect.CardId == "DY_U01" && ordinal == 1)
                await CardPileCmd.Draw(context, effect.Amount, player);
            else if (effect.CardId == "DY_R01" && ordinal == 2)
            {
                await GainQi(player, effect.Amount, source);
                if (effect.EnergyAmount > 0 && CanAct(player))
                    await PlayerCmd.GainEnergy(effect.EnergyAmount, player);
            }
        }
    }

    public static async Task GainLingbo(Player player, int amount, CardModel? source = null)
    {
        if (amount <= 0 || !CanAct(player)) return;
        var state = Get(player);
        var gained = state.GainLingbo(amount);
        if (gained <= 0) return;
        await SyncPower<LingboPower>(player, state.Lingbo, source);
        DuanYuAttackIntents.Refresh(player);
        foreach (var effect in state.Effects.Where(effect => effect.CardId == "DY_U23").ToArray())
            if (CanAct(player)) await GainBlock(player, gained * effect.Amount);
    }

    public static async Task GainSwordMomentum(Player player, int amount, CardModel? source = null)
    {
        if (amount <= 0 || !CanAct(player)) return;
        var state = Get(player);
        state.GainSwordMomentum(amount);
        await SyncPower<SwordMomentumPower>(player, state.SwordMomentum, source);
    }

    public static async Task AddEffect(Player player, string cardId, int amount = 1, int secondaryAmount = 0, CardModel? source = null, int energyAmount = 0)
    {
        if (!CanAct(player)) return;
        if (!DuanYuEffectPower.Types.TryGetValue(cardId, out var powerType))
            throw new ArgumentOutOfRangeException(nameof(cardId), cardId, "Unknown Duan Yu ability.");
        var state = Get(player);
        var upgradedMeridianCopies = cardId == "DY_R22" && source?.IsUpgraded == true ? secondaryAmount : 0;
        var existingIndex = state.Effects.FindIndex(effect => effect.CardId == cardId);
        if (existingIndex < 0)
            state.Effects.Add(new DuanYuEffect(cardId, amount, secondaryAmount, source, energyAmount, upgradedMeridianCopies));
        else
        {
            var previous = state.Effects[existingIndex];
            state.Effects[existingIndex] = previous with
            {
                Amount = previous.Amount + amount,
                SecondaryAmount = previous.SecondaryAmount + secondaryAmount,
                EnergyAmount = previous.EnergyAmount + energyAmount,
                UpgradedMeridianCopies = previous.UpgradedMeridianCopies + upgradedMeridianCopies
            };
        }
        var canonical = ModelDb.GetById<PowerModel>(ModelDb.GetId(powerType));
        await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), canonical.ToMutable(), player.Creature, amount, player.Creature, source);
    }

    public static void QueueNextTurn(Player player, string kind, int amount)
    {
        if (kind is not ("qi" or "draw" or "block" or "lingbo")) throw new ArgumentOutOfRangeException(nameof(kind));
        if (amount > 0 && CanAct(player)) Get(player).DelayedEffects.Add(new DuanYuDelayedEffect(kind, amount));
    }

    public static async Task AddSwordDamage(Player player, int amount)
    {
        if (!CanAct(player) || amount <= 0) return;
        Get(player).SwordDamage += amount;
        await PowerCmd.Apply<QiBladePower>(new ThrowingPlayerChoiceContext(), player.Creature, amount, player.Creature, null);
    }

    internal static void BeginCombat(ICombatState? combatState)
    {
        if (combatState == null) return;
        foreach (var player in combatState.Players)
        {
            States.Remove(player);
            player.Creature.GetPower<SwordMomentumPower>()?.RemoveInternal();
            var state = Get(player);
            state.Combat = combatState;
            SyncQiCounter(player, state.Qi);
            foreach (var relic in player.Relics) RecordRelic(player, relic.Id.Entry);
        }
    }

    public static async Task StartRelicCombat(Player player, int relicNumber)
    {
        if (relicNumber == 1 && HasRelic(player, "CQBZLY_DY_RELIC01")) await GainQi(player, 3);
        if (relicNumber == 9 && HasRelic(player, "CQBZLY_DY_RELIC09")) await GainQi(player, 5);
        if (relicNumber == 8)
            await PowerCmd.Apply<VermilionWardPower>(new ThrowingPlayerChoiceContext(), player.Creature, 3, player.Creature, null);
    }

    internal static void EndCombat(ICombatState? combatState)
    {
        if (combatState == null) return;
        foreach (var player in combatState.Players)
        {
            SyncQiCounter(player, 0);
            States.Remove(player);
            player.Creature.GetPower<SwordMomentumPower>()?.RemoveInternal();
        }
    }

    internal static void PrepareTurn(Player player)
    {
        var state = Get(player);
        state.PreviousTurnWasPeaceful = state.BeginTurn();
        state.StatusExhaustTriggers = 0;
        state.PrayerBeadsUsedThisTurn = false;
        if (player.Creature.GetPower<LingboPower>() is { } lingbo)
        {
            lingbo.RemoveInternal();
            DuanYuAttackIntents.Refresh(player);
        }
        if (player.Creature.GetPower<SwordMomentumPower>() is { } momentum) momentum.RemoveInternal();
    }

    internal static async Task BeginTurn(Player player, PlayerChoiceContext context)
    {
        if (!CanAct(player)) return;
        var state = Get(player);
        var delayed = state.DelayedEffects.ToArray();
        state.DelayedEffects.Clear();
        foreach (var effect in delayed)
        {
            if (!CanAct(player)) return;
            switch (effect.Kind)
            {
                case "qi": await GainQi(player, effect.Amount); break;
                case "draw": await CardPileCmd.Draw(context, effect.Amount, player); break;
                case "block": await GainBlock(player, effect.Amount); break;
                case "lingbo": await GainLingbo(player, effect.Amount); break;
            }
        }
        foreach (var relic in player.Relics.Where(relic => !relic.IsMelted).ToArray())
        {
            if (state.TurnNumber > 1 && relic.Id.Entry == "CQBZLY_DY_RELIC02" && state.Qi == 0) await GainQi(player, 1);
            if (state.TurnNumber > 1 && relic.Id.Entry == "CQBZLY_DY_RELIC07" && state.PreviousTurnWasPeaceful) await GainQi(player, 1);
        }
        foreach (var effect in state.Effects.ToArray())
        {
            if (!CanAct(player)) return;
            if (effect.CardId == "DY_R12")
            {
                await GainBlock(player, effect.Amount);
                if (state.Qi >= 5) await GainLingbo(player, effect.SecondaryAmount);
            }
            else if (effect.CardId == "DY_R07")
            {
                if (GenerateSwordChoice == null) throw new InvalidOperationException("Duan Yu sword choice executor is not registered.");
                var ordinarySwords = effect.Amount - effect.SecondaryAmount;
                if (ordinarySwords > 0) await GenerateSwordChoice(player, context, ordinarySwords, false);
                if (effect.SecondaryAmount > 0 && CanAct(player)) await GenerateSwordChoice(player, context, effect.SecondaryAmount, true);
            }
            else if (effect.CardId == "DY_R15")
                await GainQi(player, effect.Amount);
            else if (effect.CardId == "DY_R22")
            {
                for (var index = 0; index < effect.SecondaryAmount && CanAct(player); index++)
                    await AddGeneratedStatus(player, "DY_S02", PileType.Hand);
            }
        }
    }

    internal static async Task AddGeneratedStatus(Player player, string id, PileType pile)
    {
        if (!CanAct(player)) return;
        var status = player.Creature.CombatState!.CreateCard(DuanYuCard.Canonical(id), player);
        await CardPileCmd.AddGeneratedCardToCombat(status, pile, player);
    }

    internal static async Task EndTurn(Player player)
    {
        if (!CanAct(player)) return;
        var state = Get(player);
        foreach (var effect in state.Effects.ToArray())
        {
            if (!CanAct(player)) break;
            switch (effect.CardId)
            {
                case "DY_U05":
                    await GainBlock(player, (decimal)(state.Qi / 2) * effect.Amount + effect.SecondaryAmount);
                    break;
            }
        }
        state.SwordDamage = 0;
        await PowerCmd.Remove<QiBladePower>(player.Creature);
    }

    internal static void BeforeCard(CardPlay play)
    {
        var state = Get(play.Player);
        if (state.Plays.ContainsKey(play)) return;
        var ordinal = state.RegisterPlay(play.Card.Type == CardType.Skill, play.Card.Type == CardType.Attack, IsSword(play.Card) ? CardId(play.Card) : null);
        DuanYuPlayRecord? payment = null;
        if (play.IsFirstInSeries) state.PendingCardSpends.Remove(play.Card, out payment);
        state.Plays.Add(play, new DuanYuPlayRecord
        {
            SkillOrdinal = ordinal.Skill,
            SwordOrdinal = ordinal.Sword,
            ThirdSwordKind = ordinal.ThirdSwordKind,
            SixthSwordKind = ordinal.SixthSwordKind,
            Effects = payment?.Effects ?? state.Effects.ToList(),
            Spent = payment?.Spent ?? 0,
            SpendOrdinal = payment?.SpendOrdinal ?? 0,
            GrantsSealEnergy = payment?.GrantsSealEnergy ?? false,
            GrantsPrayerBlock = payment?.GrantsPrayerBlock ?? false
        });
        state.ActivePlays.Push(play);
    }

    internal static async Task AfterCard(CardPlay play, PlayerChoiceContext context)
    {
        var player = play.Player;
        var state = Get(player);
        if (!state.Plays.Remove(play, out var record)) return;
        if (state.ActivePlays.TryPeek(out var top) && ReferenceEquals(top, play)) state.ActivePlays.Pop();
        if (!CanAct(player)) return;
        if (record.SwordOrdinal > 0)
            await GainSwordMomentum(player, 1 + (record.ThirdSwordKind ? 1 : 0) + (record.SixthSwordKind ? 1 : 0), play.Card);
        foreach (var relic in player.Relics.Where(relic => !relic.IsMelted).ToArray())
        {
            if (!CanAct(player)) return;
            switch (relic.Id.Entry)
            {
                case "CQBZLY_DY_RELIC03" when record.SkillOrdinal == 3: await GainBlock(player, 4); break;
                case "CQBZLY_DY_RELIC04" when record.GrantsPrayerBlock: await GainBlock(player, 4); break;
                case "CQBZLY_DY_RELIC05" when record.ThirdSwordKind: await PlayerCmd.GainEnergy(1, player); break;
                case "CQBZLY_DY_RELIC06" when record.GrantsSealEnergy: await PlayerCmd.GainEnergy(1, player); break;
            }
        }
        foreach (var effect in record.Effects)
        {
            if (!CanAct(player)) return;
            switch (effect.CardId)
            {
                case "DY_R02" when record.SkillOrdinal is > 0 and <= 3:
                    await GainLingbo(player, effect.Amount); break;
                case "DY_R08" when record.ThirdSwordKind || record.SixthSwordKind:
                    await CardPileCmd.Draw(context, effect.Amount, player);
                    await GainQi(player, effect.SecondaryAmount);
                    if (effect.EnergyAmount > 0 && CanAct(player)) await PlayerCmd.GainEnergy(effect.EnergyAmount, player);
                    break;
                case "DY_R15" when record.SwordOrdinal == 1:
                    if (effect.EnergyAmount > 0) await PlayerCmd.GainEnergy(effect.EnergyAmount, player);
                    break;
                case "DY_R18" when record.SwordOrdinal is > 0 and <= 3:
                    await GainBlock(player, effect.Amount); break;
            }
        }
    }

    internal static int SwordBonus(CardModel card, CardPlay? play)
    {
        if (!ReceivesSwordDamageBonus(card) || card.IsCanonical || card.Owner == null) return 0;
        var state = Get(play?.Player ?? card.Owner);
        var effects = play != null && state.Plays.TryGetValue(play, out var record) ? record.Effects : state.Effects;
        return effects.Where(effect => effect.CardId == "DY_U14").Sum(effect => effect.Amount);
    }

    private static Task<decimal> GainBlock(Player player, decimal amount) => CreatureCmd.GainBlock(player.Creature, amount, ValueProp.Move, null);

    private static async Task SyncPower<T>(Player player, int amount, CardModel? source = null) where T : PowerModel
    {
        var power = player.Creature.GetPower<T>();
        if (amount <= 0)
        {
            if (power != null) await PowerCmd.Remove(power);
        }
        else if (power == null)
            await PowerCmd.Apply<T>(new ThrowingPlayerChoiceContext(), player.Creature, amount, player.Creature, source);
        else
            power.SetAmount(amount);
    }

    private static void SyncQiCounter(Player player, int qi)
    {
        if (player.PlayerCombatState != null)
            CustomResources<QiResource>.Get(player.PlayerCombatState).Amount = qi;
    }
}
