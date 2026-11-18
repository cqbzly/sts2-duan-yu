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
        Task Attack(int amount, bool all = false, int hits = 1) => DealDamage(card, context, play, amount, all, hits);
        async Task Block(int amount, Creature? recipient = null)
        {
            if (DuanYuState.CanAct(player) && (recipient ?? player.Creature).IsAlive)
                await CreatureCmd.GainBlock(recipient ?? player.Creature, card.Numbers.Block > 0 ? card.DynamicVars.Block.BaseValue : amount, ValueProp.Move, recipient != null && recipient != player.Creature ? null : play);
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
        async Task GenerateStatus(string id)
        {
            if (!DuanYuState.CanAct(player)) return;
            var status = player.Creature.CombatState!.CreateCard(DuanYuCard.Canonical(id), player);
            await CardPileCmd.AddGeneratedCardToCombat(status, PileType.Discard, player);
        }
        async Task<bool> ExhaustHandCard(CardModel selected)
        {
            if (!DuanYuState.CanAct(player) || selected.Pile?.Type != PileType.Hand || ReferenceEquals(selected, card)) return false;
            return await CardCmd.Exhaust(context, selected) is { success: true };
        }
        async Task Spend(int amount, Func<Task> effect)
        {
            if (!await DuanYuState.SpendQi(player, amount, card)) return;
            await effect();
            await DuanYuState.AfterQiSpent(player, context, card);
        }
        async Task<bool> RequireQi(int amount)
        {
            if (DuanYuState.Get(player).Qi < amount) return false;
            if (!await DuanYuState.SpendQi(player, amount, card)) return false;
            return true;
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
        if (DuanYuState.IsSword(card) && !await RequireQi(1)) return;
        switch (card.DesignId)
        {
            case "DY_B01": await Attack(Value(8, 11)); break;
            case "DY_B02": await Block(Value(6, 9)); break;
            case "DY_B03": await Block(Value(6, 8)); await Qi(Value(3, 4)); break;
            case "DY_B04": await Block(Value(5, 8)); await Lingbo(1); break;
            case "DY_B05": await Attack(Value(13, 18)); await Spend(2, () => Attack(Value(13, 18))); break;
            case "DY_C01": await Attack(Value(13, 18)); await Qi(2); break;
            case "DY_C02": await Attack(Value(12, 17)); await Spend(2, () => Attack(Value(12, 17))); break;
            case "DY_C03": await Attack(Value(8, 12)); await Debuff<VulnerablePower>(1); break;
            case "DY_C04": await Attack(Value(12, 17), true); break;
            case "DY_C05": await Attack(Value(10, 14)); await Debuff<WeakPower>(1); break;
            case "DY_C06": await Attack(Value(13, 18)); if (state.Lingbo > 0) await Block(Value(6, 8)); break;
            case "DY_C07": await Attack(Value(13, 18)); await Block(Value(5, 7)); await Generate(1); break;
            case "DY_C08": await Attack(Value(10, 14)); await Generate(2, card.IsUpgraded); await Draw(1); break;
            case "DY_C09": await Block(Value(6, 8)); await Qi(3); break;
            case "DY_C10": await Qi(Value(2, 3)); break;
            case "DY_C11": await Block(Value(5, 8)); await Lingbo(Value(1, 2)); break;
            case "DY_C12": await Lingbo(Value(1, 2)); break;
            case "DY_C13": await Attack(Value(8, 12)); await Block(Value(3, 5)); await Draw(1); break;
            case "DY_C14": await Draw(Value(2, 3)); await Spend(3, () => Energy(1)); break;
            case "DY_C15": await Block(Value(7, 9)); await Spend(2, () => Block(Value(7, 9))); break;
            case "DY_C16": await Attack(Value(12, 17)); await Block(Value(6, 8)); DuanYuState.QueueNextTurn(player, "qi", 1); break;
            case "DY_C17": await Attack(Value(7, 10)); await Spend(1, () => Draw(Value(1, 2))); break;
            case "DY_C18": await Attack(Value(10, 15)); await Block(Value(5, 7)); await Generate(4); break;
            case "DY_C19": await Qi(1); await Choose(1); break;
            case "DY_C20": if (target?.GetPower<WeakPower>() is { Amount: > 0 }) await Draw(1); await Attack(Value(13, 18)); break;
            case "DY_U01": await Qi(2); await Effect(); break;
            case "DY_U02": await Qi(Value(4, 6)); await Energy(2); break;
            case "DY_U03": await Block(Value(16, 21)); await Qi(Value(4, 5)); break;
            case "DY_U04": await LoseHp(Value(3, 2)); await Qi(Value(3, 4)); await Energy(1); break;
            case "DY_U05": await Effect(1, Value(0, 2)); break;
            case "DY_U06":
            {
                var selected = (await CardSelectCmd.FromHand(context, player, new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1), null, card)).FirstOrDefault();
                if (selected != null) await ExhaustHandCard(selected);
                await Draw(Value(3, 4));
                await Qi(Value(2, 3));
                break;
            }
            case "DY_U07": await Attack(Value(18, 25)); await Spend(3, () => Attack(Value(18, 25))); break;
            case "DY_U08": await Attack(Value(20, 28) + state.Qi * 2); await GenerateStatus("DY_S01"); break;
            case "DY_U09": await Qi(Value(1, 2)); await DuanYuState.AddSwordDamage(player, Value(3, 5)); break;
            case "DY_U10": await Attack(Value(12, 16), true); await Qi(1); await Generate(3, card.IsUpgraded); break;
            case "DY_U11": await Attack(Value(10, 15)); await Block(Value(2, 4)); await Generate(5); break;
            case "DY_U12": await Attack(Value(12, 17)); await Block(2); await Generate(6, card.IsUpgraded); break;
            case "DY_U13": await Choose(2); break;
            case "DY_U14": await Effect(Value(3, 4)); break;
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
            case "DY_U16": await Attack(Value(15, 21)); if (state.SwordPlays > 0) await Draw(1); break;
            case "DY_U17": await Attack(Value(16, 22), true); await Spend(3, () => Attack(Value(12, 16), true)); break;
            case "DY_U18": await Block(Value(8, 10)); await Lingbo(Value(1, 2)); break;
            case "DY_U19": await Retain(Value(2, 3)); await Lingbo(Value(1, 2)); break;
            case "DY_U20": await Attack(Value(13, 18)); await Block(Value(5, 7)); await Debuff<WeakPower>(2); break;
            case "DY_U21": await Attack(Value(12, 17)); await Lingbo(2); await Draw(Value(1, 2)); break;
            case "DY_U22": await Attack(Value(15, 22)); await Retain(1, selected => selected.Type == CardType.Attack && !DuanYuState.IsSword(selected)); await Qi(1); break;
            case "DY_U23": await Effect(Value(4, 6)); break;
            case "DY_U24": await Block(Value(12, 16)); await Draw(2); break;
            case "DY_U25": await Lingbo(Value(1, 2)); DuanYuState.QueueNextTurn(player, "block", Value(6, 8)); break;
            case "DY_U26":
                await Draw(Value(2, 3));
                if (state.Qi >= 5) await Draw(1);
                await Qi(Value(2, 3));
                await Momentum(Value(1, 2));
                break;
            case "DY_U27":
                await DuanYuForeignCards.GenerateAttacks(player, context, 2, card.IsUpgraded);
                await GenerateStatus("DY_S02");
                break;
            case "DY_U28": await Effect(Value(2, 3)); break;
            case "DY_U29": if (target?.IsAlive == true) await Block(Value(8, 11), target); await Qi(Value(1, 2)); break;
            case "DY_U30": if (target?.Player is { } companion && target.IsAlive) { await Block(Value(5, 8), target); await Draw(1, companion); } break;
            case "DY_U31":
            {
                bool killed = await DealDamage(card, context, play, Value(15, 22));
                if (killed) await Qi(2);
                break;
            }
            case "DY_U32": await Attack(Value(12, 17)); await Spend(2, () => Debuff<VulnerablePower>(Value(2, 3))); break;
            case "DY_U33": await Attack(Value(11, 16), hits: 2); await Spend(3, () => Attack(Value(11, 16))); break;
            case "DY_U34": await Block(Value(6, 9)); await Lingbo(1); DuanYuState.QueueNextTurn(player, "draw", 1); break;
            case "DY_U35":
            {
                var selected = (await CardSelectCmd.FromHand(context, player, new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, Value(3, 4)), null, card)).ToArray();
                foreach (var chosen in selected)
                {
                    if (!DuanYuState.CanAct(player)) break;
                    if (!await ExhaustHandCard(chosen)) continue;
                    await Qi(1);
                    await Block(Value(3, 4));
                }
                break;
            }
            case "DY_R01": await Effect(2, energy: 1); break;
            case "DY_R02": await Effect(); break;
            case "DY_R03":
                await Block(Value(10, 14));
                await Spend(3, () => Choose(6));
                break;
            case "DY_R04": await Qi(Value(6, 8)); await Energy(Value(2, 3)); await Draw(Value(2, 3)); break;
            case "DY_R05": await Attack(Value(40, 52)); await Spend(3, () => Attack(Value(28, 36), true)); break;
            case "DY_R06":
                if (target?.IsAlive == true) await CreatureCmd.LoseBlock(context, target, target.Block, player.Creature);
                await Attack(Value(26, 35)); await Spend(2, () => Debuff<VulnerablePower>(Value(2, 3))); break;
            case "DY_R07": await Choose(1); await Effect(1, card.IsUpgraded ? 1 : 0); break;
            case "DY_R08": await Effect(Value(1, 2), 2, 1); break;
            case "DY_R09":
                await Attack(Value(18, 24), true);
                await Choose(1);
                if (!DuanYuState.CanAct(player)) break;
                foreach (var sword in PileType.Hand.GetPile(player).Cards.Where(DuanYuState.IsSword).ToArray())
                    if (sword.IsUpgradable) CardCmd.Upgrade(sword);
                break;
            case "DY_R10": await Attack(Value(18, 24)); await Choose(3); break;
            case "DY_R11": await Block(Value(18, 23)); await Lingbo(3); break;
            case "DY_R12": await Block(Value(8, 10)); await Effect(Value(8, 10), 1); break;
            case "DY_R13": await Attack(Value(22, 30)); await Spend(5, () => Attack(Value(55, 70))); break;
            case "DY_R14":
                await CreatureCmd.LoseBlock(context, player.Creature, player.Creature.Block, player.Creature);
                await Draw(Value(3, 4));
                await Qi(Value(3, 4));
                await Energy(1);
                await Momentum(Value(2, 3));
                break;
            case "DY_R15": await Effect(3, energy: 1); break;
            case "DY_R16":
            {
                foreach (var power in player.Creature.Powers.Where(power => power is WeakPower or FrailPower or PoisonPower).ToArray()) await PowerCmd.Remove(power);
                await Block(8);
                var statuses = PileType.Hand.GetPile(player).Cards.Where(selected => selected.Type == CardType.Status).ToArray();
                foreach (var status in statuses)
                {
                    if (!DuanYuState.CanAct(player)) break;
                    if (!await ExhaustHandCard(status)) continue;
                    await Qi(2);
                    await Draw(1);
                }
                break;
            }
            case "DY_R17":
                await LoseHp(Value(4, 3));
                if (DuanYuState.CanAct(player) && target?.Player is { } ally && target.IsAlive)
                {
                    await Block(Value(18, 23), target);
                    await Lingbo(2, ally);
                }
                break;
            case "DY_R18": await Effect(Value(5, 7)); break;
            case "DY_R19":
            {
                await Block(Value(7, 10));
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
            case "DY_R20": await Attack(Value(24, 32), true); await Block(Value(10, 13)); await Debuff<WeakPower>(2, true); await Spend(3, () => Block(Value(10, 13))); break;
            case "DY_T01": await Attack(Value(10, 14)); await Attack(Value(4, 6)); break;
            case "DY_T02": await Attack(Value(6, 9)); await Debuff<VulnerablePower>(Value(1, 2)); break;
            case "DY_T03": await Attack(Value(6, 9), true); await Attack(Value(4, 6), true); break;
            case "DY_T04": await Attack(Value(6, 9)); await Block(Value(6, 8)); break;
            case "DY_T05": await Attack(Value(4, 6)); await Draw(1); break;
            case "DY_T06": await Attack(Value(5, 8)); await Debuff<WeakPower>(Value(1, 2)); break;
            case "DY_S01": break;
            case "DY_S02": await Qi(3); await Momentum(2); await Draw(1); break;
            default: throw new InvalidOperationException("No executor for " + card.DesignId);
        }
        if (DuanYuState.IsSword(card)) await DuanYuState.AfterQiSpent(player, context, card);
    }

    private static async Task<bool> DealDamage(DuanYuCard card, PlayerChoiceContext context, CardPlay play, int amount, bool all = false, int hits = 1)
    {
        if (!DuanYuState.CanAct(play.Player) || (!all && play.Target?.IsAlive != true)) return false;
        decimal baseDamage = amount;
        if (card.DesignId != "DY_U08" && card.Numbers.Damage > 0 && amount == card.Value(card.Numbers.Damage, card.Numbers.DamageUp)) baseDamage = card.DynamicVars.Damage.BaseValue;
        else if (card.Numbers.ExtraDamage > 0 && amount == card.Value(card.Numbers.ExtraDamage, card.Numbers.ExtraDamageUp)) baseDamage = card.DynamicVars["ExtraDamage"].BaseValue;
        var attack = (card.DesignId == "DY_U08" ? DamageCmd.Attack(card.DynamicVars.CalculatedDamage) : DamageCmd.Attack(baseDamage)).WithHitCount(hits).FromCard(card, play);
        if (all) attack.TargetingAllOpponents(play.Player.Creature.CombatState!);
        else attack.Targeting(play.Target!);
        await attack.Execute(context);
        return attack.Results.SelectMany(result => result).Any(result => result.WasTargetKilled);
    }

    public static async Task GenerateSword(Player player, int number, bool upgraded)
    {
        if (!DuanYuState.CanAct(player)) return;
        var sword = player.Creature.CombatState!.CreateCard(DuanYuCard.Canonical($"DY_T{number:00}"), player);
        if (upgraded) CardCmd.Upgrade(sword);
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
