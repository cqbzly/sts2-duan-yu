using BaseLib.Abstracts;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace DuanYu.Cards;

public static class DuanYuCardExecutor
{
    public static void Initialize() => DuanYuState.GenerateSwordChoice = async (player, context, count, upgraded) =>
    {
        for (int index = 0; index < count && DuanYuState.CanAct(player); index++)
            await ChooseSwords(player, context, 1, upgraded);
    };

    public static async Task Play(DuanYuCard card, PlayerChoiceContext context, CardPlay play)
    {
        var player = play.Player;
        var state = DuanYuState.Get(player);
        var target = play.Target;
        int Value(int normal, int upgraded) => card.Value(normal, upgraded);
        Task Attack(decimal amount, bool all = false, int hitCount = 1) => DealDamage(card, context, play, amount, all, hitCount);
        async Task Block(int amount, Creature? recipient = null, int multiplier = 1)
        {
            if (multiplier > 0 && DuanYuState.CanAct(player) && (recipient ?? player.Creature).IsAlive)
                await CreatureCmd.GainBlock(recipient ?? player.Creature, (card.Numbers.Block > 0 ? card.DynamicVars.Block.BaseValue : amount) * multiplier, ValueProp.Move, recipient != null && recipient != player.Creature ? null : play);
        }
        async Task Draw(int amount, Player? recipient = null)
        {
            if (DuanYuState.CanAct(player) && (recipient ?? player).Creature.IsAlive)
                await CardPileCmd.Draw(context, amount, recipient ?? player);
        }
        Task Qi(int amount) => DuanYuState.GainQi(player, amount, card);
        Task Momentum(int amount) => DuanYuState.GainSwordMomentum(player, amount, card);
        Task Lingbo(int amount, Player? recipient = null) => DuanYuState.GainLingbo(recipient ?? player, amount, card);
        Task Generate(int number, bool upgraded = false) => GenerateSword(player, number, upgraded);
        Task Choose(int count) => ChooseSwords(player, context, count, card.IsUpgraded);
        Task Effect(int amount = 1, int secondary = 0, int energy = 0) => DuanYuState.AddEffect(player, card.DesignId, amount, secondary, card, energy);
        async Task Energy(int amount)
        {
            if (DuanYuState.CanAct(player)) await PlayerCmd.GainEnergy(amount, player);
        }
        async Task GenerateStatus(string id, PileType pile = PileType.Discard)
        {
            if (!DuanYuState.CanAct(player)) return;
            var status = player.Creature.CombatState!.CreateCard(DuanYuCard.Canonical(id), player);
            await CardPileCmd.AddGeneratedCardToCombat(status, pile, player);
        }
        async Task<bool> RetrieveDiscardStatus()
        {
            if (!DuanYuState.CanAct(player)) return false;
            var status = (await CardSelectCmd.FromCombatPile(context, PileType.Discard.GetPile(player), player,
                new CardSelectorPrefs(card.SelectionPrompt, 1), candidate => candidate.Type == CardType.Status)).FirstOrDefault();
            if (status == null) return false;
            await CardPileCmd.Add(status, PileType.Hand);
            return status.Pile?.Type == PileType.Hand;
        }
        async Task<bool> ExhaustHandCard(CardModel selected)
        {
            if (!DuanYuState.CanAct(player) || selected.Pile?.Type != PileType.Hand || ReferenceEquals(selected, card)) return false;
            return await CardCmd.Exhaust(context, selected) is { success: true };
        }
        async Task LoseHp(int amount)
        {
            if (DuanYuState.CanAct(player))
                await CreatureCmd.Damage(context, player.Creature, amount, ValueProp.Unblockable | ValueProp.Unpowered, card, play);
        }
        async Task Debuff<TPower>(int amount, bool all = false) where TPower : PowerModel
        {
            if (!DuanYuState.CanAct(player)) return;
            var targets = all ? player.Creature.CombatState!.GetOpponentsOf(player.Creature).Where(enemy => enemy.IsAlive).ToArray() : target is { IsAlive: true } ? [target] : Array.Empty<Creature>();
            foreach (var enemy in targets)
                await PowerCmd.Apply<TPower>(context, enemy, amount, player.Creature, card);
        }
        async Task Retain(int maximum, Func<CardModel, bool>? filter = null)
        {
            if (!DuanYuState.CanAct(player)) return;
            var chosen = await CardSelectCmd.FromHand(context, player, new CardSelectorPrefs(card.SelectionPrompt, 0, maximum), filter, card);
            foreach (var selected in chosen) CardCmd.ApplySingleTurnRetain(selected);
        }
        if (!DuanYuState.CanAct(player)) return;
        switch (card.DesignId)
        {
            case "DY_B01": await Attack(Value(6, 9)); break;
            case "DY_B02": await Block(Value(5, 8)); break;
            case "DY_B03": await Block(Value(5, 7)); await Qi(Value(1, 2)); break;
            case "DY_B04": await Block(Value(4, 6)); await Lingbo(1); break;
            case "DY_B05": await Attack(Value(16, 22)); break;
            case "DY_C01": await Attack(Value(11, 15)); await Qi(1); break;
            case "DY_C02":
            {
                await Draw(Value(2, 3));
                if (!DuanYuState.CanAct(player)) break;
                var selected = (await CardSelectCmd.FromHandForDiscard(context, player,
                    new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1), null, card)).FirstOrDefault();
                if (selected?.Pile?.Type == PileType.Hand && DuanYuState.CanAct(player))
                    await CardCmd.Discard(context, selected);
                break;
            }
            case "DY_C03": await Attack(Value(7, 10)); await Debuff<VulnerablePower>(1); break;
            case "DY_C04": await Attack(Value(9, 12), true); break;
            case "DY_C05": await Attack(Value(8, 11)); await Debuff<WeakPower>(1); break;
            case "DY_C06": await Attack(Value(11, 15)); if (state.Lingbo > 0) await Block(Value(5, 7)); break;
            case "DY_C07": await Attack(Value(11, 15)); await Block(Value(4, 6)); await Generate(1); break;
            case "DY_C08": await Attack(Value(8, 12)); await Generate(2, card.IsUpgraded); await Draw(1); break;
            case "DY_C09": await Draw(Value(1, 2)); await Qi(2); break;
            case "DY_C10": await Qi(Value(2, 3)); break;
            case "DY_C11": await Lingbo(1); await Draw(Value(1, 2)); break;
            case "DY_C12": await Lingbo(Value(1, 2)); break;
            case "DY_C13": await Attack(Value(7, 10)); await Block(Value(2, 4)); await Draw(1); break;
            case "DY_C14": await Draw(Value(2, 3)); await Energy(1); break;
            case "DY_C15": await Block(Value(12, 16)); break;
            case "DY_C16": await Attack(Value(10, 14)); await Block(Value(5, 7)); DuanYuState.QueueNextTurn(player, "qi", 1); break;
            case "DY_C17": await Attack(Value(6, 9)); await Draw(Value(1, 2)); break;
            case "DY_C18": await Attack(Value(8, 12)); await Block(Value(4, 6)); await Generate(4); break;
            case "DY_C19": await Choose(1); break;
            case "DY_C20": if (target?.GetPower<WeakPower>() is { Amount: > 0 }) await Draw(1); await Attack(Value(11, 15)); break;
            case "DY_C21":
            {
                var selected = (await CardSelectCmd.FromHand(context, player, new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1), null, card)).FirstOrDefault();
                if (selected != null) await ExhaustHandCard(selected);
                if (!await RetrieveDiscardStatus()) await Draw(card.IsUpgraded ? 2 : 1);
                await Qi(Value(1, 2));
                break;
            }
            case "DY_C22":
            {
                var selected = (await CardSelectCmd.FromCombatPile(
                    context,
                    PileType.Discard.GetPile(player),
                    player,
                    new CardSelectorPrefs(card.SelectionPrompt, 1))).FirstOrDefault();
                if (selected != null) await CardPileCmd.Add(selected, PileType.Hand);
                else await Draw(card.IsUpgraded ? 2 : 1);
                await Qi(1);
                break;
            }
            case "DY_C23": await Attack(Value(10, 14), all: true); await GenerateStatus("DY_S01", PileType.Hand); break;
            case "DY_U01": await Qi(1); await Effect(); break;
            case "DY_U02": await Qi(Value(2, 3)); await Energy(Value(2, 3)); break;
            case "DY_U03": await Block(Value(15, 20)); await Qi(Value(2, 3)); break;
            case "DY_U04": await LoseHp(Value(3, 2)); await Qi(Value(2, 3)); await Energy(Value(1, 2)); break;
            case "DY_U05": await Effect(1, Value(0, 2)); break;
            case "DY_U06":
            {
                var selected = (await CardSelectCmd.FromHand(context, player, new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1), null, card)).FirstOrDefault();
                var exhausted = selected != null && await ExhaustHandCard(selected);
                await Draw(Value(2, 3));
                if (exhausted && selected!.Type == CardType.Status)
                {
                    await Draw(1);
                    await Qi(1);
                }
                break;
            }
            case "DY_U07": await Attack(Value(27, 37)); break;
            case "DY_U08": await Attack(card.DynamicVars.CalculationBase.BaseValue); await GenerateStatus("DY_S01"); break;
            case "DY_U09": await DuanYuState.AddSwordDamage(player, Value(2, 3)); break;
            case "DY_U10": await Attack(Value(10, 14), true); await Generate(3, card.IsUpgraded); break;
            case "DY_U11": await Attack(Value(8, 12)); await Block(Value(2, 3)); await Generate(5); break;
            case "DY_U12": await Attack(Value(10, 14)); await Block(2); await Generate(6, card.IsUpgraded); break;
            case "DY_U13":
            {
                var hand = PileType.Hand.GetPile(player);
                var maximum = Math.Min(2, Math.Max(0, CardPile.MaxCardsInHand - hand.Cards.Count));
                if (maximum == 0) break;
                var selected = (await CardSelectCmd.FromCombatPile(context, PileType.Exhaust.GetPile(player), player,
                    new CardSelectorPrefs(card.SelectionPrompt, 0, maximum), IsUpgradedSword)).ToArray();
                foreach (var sword in selected)
                {
                    if (!DuanYuState.CanAct(player) || hand.Cards.Count >= CardPile.MaxCardsInHand) break;
                    if (sword.Pile?.Type == PileType.Exhaust && IsUpgradedSword(sword))
                        await CardPileCmd.Add(sword, PileType.Hand);
                }
                break;
            }
            case "DY_U14": await Effect(Value(2, 3)); break;
            case "DY_U15":
            {
                var options = Enumerable.Range(1, 6).ToList();
                for (var index = 0; index < 2 && DuanYuState.CanAct(player); index++)
                {
                    var selected = player.RunState.Rng.CombatCardGeneration.NextInt(options.Count);
                    var sword = options[selected];
                    options.RemoveAt(selected);
                    await Generate(sword, card.IsUpgraded);
                }
                break;
            }
            case "DY_U16": await Attack(Value(13, 18)); if (state.SwordPlays > 0) await Draw(1); break;
            case "DY_U17": await Attack(Value(20, 27), true); break;
            case "DY_U18": await Block(Value(7, 10)); await Lingbo(Value(1, 2)); break;
            case "DY_U19": await Retain(Value(2, 3)); await Lingbo(Value(1, 2)); break;
            case "DY_U20": await Attack(Value(11, 15)); await Block(Value(4, 6)); await Debuff<WeakPower>(2); break;
            case "DY_U21": await Attack(Value(10, 14)); await Lingbo(2); await Draw(Value(1, 2)); break;
            case "DY_U22": await Attack(Value(13, 18)); await Retain(1, selected => selected.Type == CardType.Attack && !DuanYuState.IsSword(selected)); break;
            case "DY_U23": await Effect(Value(5, 7)); break;
            case "DY_U24": await Block(Value(12, 15)); await Draw(2); break;
            case "DY_U25": await Lingbo(Value(2, 3)); DuanYuState.QueueNextTurn(player, "block", Value(5, 7)); break;
            case "DY_U26":
                await Draw(Value(2, 3));
                if (state.Qi >= 5) await Draw(1);
                await Qi(1);
                await Momentum(1);
                break;
            case "DY_U27":
                await DuanYuForeignCards.GenerateAttacks(player, context, 2, card.IsUpgraded, addExhaust: false);
                await GenerateStatus("DY_S02");
                break;
            case "DY_U28": await Effect(Value(2, 3)); break;
            case "DY_U29": if (target?.IsAlive == true) await Block(Value(6, 9), target); await Qi(1); break;
            case "DY_U30": if (target?.Player is { } companion && target.IsAlive) { await Block(Value(4, 6), target); await Draw(1, companion); } break;
            case "DY_U31":
            {
                bool killed = await DealDamage(card, context, play, card.DynamicVars.Damage.BaseValue);
                if (killed) await Qi(Value(2, 3));
                break;
            }
            case "DY_U32": await Attack(Value(12, 17)); await Debuff<VulnerablePower>(Value(2, 3)); break;
            case "DY_U33": await Attack(Value(16, 22)); await Draw(2); break;
            case "DY_U34": await Block(Value(5, 7)); await Lingbo(1); DuanYuState.QueueNextTurn(player, "lingbo", 1); DuanYuState.QueueNextTurn(player, "draw", 1); break;
            case "DY_U35":
            {
                var selected = (await CardSelectCmd.FromHand(context, player, new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, Value(3, 4)), null, card)).ToArray();
                foreach (var chosen in selected)
                {
                    if (!DuanYuState.CanAct(player)) break;
                    if (!await ExhaustHandCard(chosen)) continue;
                    await Qi(1);
                    await Block(Value(2, 3));
                }
                break;
            }
            case "DY_U36":
                await Attack(Value(12, 17));
                await DuanYuForeignCards.GenerateAttacks(player, context, 1, card.IsUpgraded);
                await GenerateStatus("DY_S02", PileType.Hand);
                break;
            case "DY_U37":
            {
                var xValue = card.ResolveEnergyXValue();
                await Block(Value(4, 6), multiplier: xValue);
                var statuses = PileType.Hand.GetPile(player).Cards
                    .Where(candidate => candidate.Type == CardType.Status && !ReferenceEquals(candidate, card))
                    .Take(xValue)
                    .ToArray();
                foreach (var statusCard in statuses)
                {
                    if (!await ExhaustHandCard(statusCard)) continue;
                    await Energy(1);
                    await Qi(1);
                }
                break;
            }
            case "DY_U38":
            {
                var selected = (await CardSelectCmd.FromCombatPile(
                    context,
                    PileType.Draw.GetPile(player),
                    player,
                    new CardSelectorPrefs(card.SelectionPrompt, 1),
                    candidate => candidate.Type != CardType.Status)).FirstOrDefault();
                if (selected != null) await CardPileCmd.Add(selected, PileType.Hand);
                await Qi(card.IsUpgraded ? 2 : 1);
                break;
            }
            case "DY_R01": await Effect(1, energy: 1); break;
            case "DY_R02": await Effect(); break;
            case "DY_R03":
                await Block(Value(8, 11));
                await Choose(6);
                break;
            case "DY_R04": await Qi(Value(2, 3)); await Energy(Value(1, 2)); await Draw(2); break;
            case "DY_R05":
            {
                var otherEnemies = player.Creature.CombatState!.GetOpponentsOf(player.Creature).Where(enemy => enemy.IsAlive && enemy != target).ToArray();
                await Attack(Value(48, 64));
                foreach (var enemy in otherEnemies)
                    await DealDamage(card, context, play, Value(20, 26), recipient: enemy);
                break;
            }
            case "DY_R06":
                if (target?.IsAlive == true) await CreatureCmd.LoseBlock(context, target, target.Block, player.Creature);
                await Attack(Value(22, 30)); await Debuff<VulnerablePower>(Value(2, 3)); break;
            case "DY_R07": await Choose(1); await Effect(1, card.IsUpgraded ? 1 : 0); break;
            case "DY_R08": await Effect(Value(1, 2), 1, 1); break;
            case "DY_R09":
                await Attack(Value(15, 20), true);
                await Choose(1);
                if (!DuanYuState.CanAct(player)) break;
                foreach (var sword in PileType.Hand.GetPile(player).Cards.Where(DuanYuState.IsSword).ToArray())
                    if (sword.IsUpgradable) CardCmd.Upgrade(sword);
                break;
            case "DY_R10": await Attack(Value(15, 20)); await Choose(3); break;
            case "DY_R11": await Block(Value(8, 10)); await Lingbo(Value(2, 3)); break;
            case "DY_R12": await Effect(Value(6, 8), 1); break;
            case "DY_R13": await Attack(Value(52, 70)); break;
            case "DY_R14":
                await CreatureCmd.LoseBlock(context, player.Creature, player.Creature.Block, player.Creature);
                await Draw(Value(3, 4));
                await Qi(Value(2, 3));
                await Energy(Value(2, 3));
                await Momentum(Value(2, 3));
                break;
            case "DY_R15": await Effect(1, energy: 1); break;
            case "DY_R16":
            {
                foreach (var power in player.Creature.Powers.Where(power => power is FrailPower or WeakPower or VulnerablePower).ToArray()) await PowerCmd.Remove(power);
                await Block(7);
                var statuses = PileType.Hand.GetPile(player).Cards.Where(selected => selected.Type == CardType.Status).ToArray();
                foreach (var status in statuses)
                {
                    if (!DuanYuState.CanAct(player)) break;
                    if (!await ExhaustHandCard(status)) continue;
                    await Qi(1);
                    await Draw(1);
                }
                break;
            }
            case "DY_R17":
                await LoseHp(Value(4, 3));
                if (DuanYuState.CanAct(player) && target?.Player is { } ally && target.IsAlive)
                {
                    await Block(Value(16, 21), target);
                    await Lingbo(2, ally);
                }
                break;
            case "DY_R18": await Effect(Value(4, 6)); break;
            case "DY_R19":
            {
                await Block(Value(6, 8));
                var selected = PileType.Hand.GetPile(player).Cards.Where(chosen => !ReferenceEquals(chosen, card)).ToArray();
                var exhausted = 0;
                foreach (var chosen in selected)
                {
                    if (!DuanYuState.CanAct(player)) break;
                    if (await ExhaustHandCard(chosen)) exhausted++;
                }
                for (var index = 0; index < exhausted && DuanYuState.CanAct(player); index++)
                    await Generate(player.RunState.Rng.CombatCardGeneration.NextInt(1, 7), card.IsUpgraded);
                break;
            }
            case "DY_R20": await Attack(Value(20, 27), true); await Block(Value(16, 22)); await Debuff<WeakPower>(2, true); break;
            case "DY_R21":
            {
                var xValue = card.CapturedQiX;
                if (xValue > 0) await Attack(Value(6, 8), hitCount: xValue);
                break;
            }
            case "DY_R22": await GenerateStatus("DY_S02", PileType.Hand); await Effect(Value(5, 7), 1); break;
            case "DY_T01": await Attack(Value(7, 10)); break;
            case "DY_T02": await Attack(Value(3, 5)); await Debuff<VulnerablePower>(1); break;
            case "DY_T03": await Attack(Value(5, 7), true); break;
            case "DY_T04": await Attack(Value(3, 5)); await Block(Value(3, 5)); break;
            case "DY_T05": await Attack(Value(2, 3)); await Draw(1); break;
            case "DY_T06": await Attack(Value(2, 4)); await Debuff<WeakPower>(1); break;
            case "DY_S01": break;
            case "DY_S02": await Qi(2); await Momentum(1); await Draw(1); break;
            default: throw new InvalidOperationException("No executor for " + card.DesignId);
        }
        if (card.HasQiCost) await DuanYuState.AfterQiSpent(player, context, card);
    }

    private static async Task<bool> DealDamage(DuanYuCard card, PlayerChoiceContext context, CardPlay play, decimal amount, bool all = false, int hitCount = 1, Creature? recipient = null)
    {
        var target = recipient ?? play.Target;
        if (!DuanYuState.CanAct(play.Player) || (!all && target?.IsAlive != true)) return false;
        decimal baseDamage = amount;
        if (card.DesignId != "DY_U08" && card.Numbers.Damage > 0 && amount == card.Value(card.Numbers.Damage, card.Numbers.DamageUp)) baseDamage = card.DynamicVars.Damage.BaseValue;
        else if (card.Numbers.ExtraDamage > 0 && amount == card.Value(card.Numbers.ExtraDamage, card.Numbers.ExtraDamageUp)) baseDamage = card.DynamicVars["ExtraDamage"].BaseValue;
        var attack = (card.DesignId == "DY_U08" ? DamageCmd.Attack(card.DynamicVars.CalculatedDamage) : DamageCmd.Attack(baseDamage))
            .FromCard(card, play).WithHitCount(hitCount);
        if (all) attack.TargetingAllOpponents(play.Player.Creature.CombatState!);
        else attack.Targeting(target!);
        await attack.Execute(context);
        return attack.Results.SelectMany(result => result).Any(result => result.Receiver == target && result.WasTargetKilled);
    }

    private static bool IsUpgradedSword(CardModel card) => card.IsUpgraded && DuanYuState.IsSword(card);

    public static async Task GenerateSword(Player player, int number, bool upgraded, bool freeQi = false)
    {
        if (!DuanYuState.CanAct(player)) return;
        var sword = player.Creature.CombatState!.CreateCard(DuanYuCard.Canonical($"DY_T{number:00}"), player);
        if (upgraded) CardCmd.Upgrade(sword);
        if (freeQi) CustomResources<QiResource>.Cost(sword)!.SetThisCombat(0);
        await CardPileCmd.AddGeneratedCardToCombat(sword, PileType.Hand, player);
    }

    public static async Task ChooseSwords(Player player, PlayerChoiceContext context, int count, bool upgraded)
    {
        if (!DuanYuState.CanAct(player)) return;
        var options = Enumerable.Range(1, 6).ToList();
        for (int index = 0; index < count && DuanYuState.CanAct(player) && options.Count > 0; index++)
        {
            var selectedIndex = player.RunState.Rng.CombatCardGeneration.NextInt(options.Count);
            var number = options[selectedIndex];
            options.RemoveAt(selectedIndex);
            await GenerateSword(player, number, upgraded);
        }
    }
}
