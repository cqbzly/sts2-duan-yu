#if DEBUG
using DuanYu.Cards;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace DuanYu.DebugTests;

internal static class BalanceSelfTests
{
    internal static IEnumerable<(string Name, Func<Task> Action)> Cases
    {
        get
        {
            foreach (var upgraded in new[] { false, true })
            {
                var suffix = upgraded ? "+" : "";
                yield return ("balance:energy-cards-paid" + suffix, () => EnergyCards(upgraded));
                yield return ("balance:rebalanced-qi-sources-paid" + suffix, () => QiSources(upgraded));
                yield return ("balance:read-book-free-draw-with-stagnation" + suffix, () => ReadBook(upgraded));
                yield return ("balance:six-swords-paid" + suffix, () => SixSwordPayment(upgraded));
                yield return ("balance:sword-resource-boundaries" + suffix, () => SwordResourceBoundaries(upgraded));
                yield return ("balance:proficiency-all-swords" + suffix, () => ProficiencyAllSwords(upgraded));
                yield return ("balance:expanded-attacks-and-original-effects" + suffix, () => ExpandedAttacks(upgraded));
                yield return ("balance:seven-star-step" + suffix, () => SevenStarStep(upgraded));
                yield return ("balance:rebalanced-rare-defense" + suffix, () => RareDefense(upgraded));
                yield return ("balance:footwork-roles" + suffix, () => FootworkRoles(upgraded));
                yield return ("balance:formless-first-three-swords" + suffix, () => FormlessFirstThreeSwords(upgraded));
                yield return ("balance:simplicity-energy-cost-and-qi" + suffix, () => SimplicityCost(upgraded));
            }
            yield return ("balance:beginning-beiming-costs-and-stacks", BeginningBeiming);
            yield return ("balance:beiming-stacks-and-turn-limit", BeimingLimit);
            yield return ("balance:spend-callback-idempotence", SpendCallbackIdempotence);
            yield return ("balance:unity-third-and-sixth-kind", UnityThresholds);
            yield return ("balance:upgraded-swords-share-kind", UpgradedSwordIdentity);
            yield return ("balance:native-replay-spending", NativeReplaySpending);
            yield return ("balance:native-replay-unity-thresholds", NativeReplayUnityThresholds);
            yield return ("balance:unity-no-retroactive-trigger", UnityLateApplication);
            yield return ("balance:simplicity-start-and-first-sword-stacks", SimplicityTurnStartStacks);
            yield return ("balance:sword-array-mixed-upgrades", SwordArrayMixedUpgrades);
            yield return ("balance:six-sword-resource-cycle", SixSwordCycle);
            yield return ("balance:self-damage-stops-after-death", SelfDamageStopsAfterDeath);
            yield return ("balance:basic-upgrade-values", BasicUpgradeValues);
            yield return ("balance:damage-preview-does-not-spend-qi", DamagePreviewNoQi);
            yield return ("balance:combined-damage-enchantment-and-bonuses-once", CombinedDamageEnchantment);
            foreach (var test in RandomCardSelfTests.Cases) yield return test;
        }
    }

    internal static async Task Run()
    {
        foreach (var test in Cases) await test.Action();
    }

    private static async Task ExpandedAttacks(bool upgraded)
    {
        var regular = new[] { ("C", 23), ("U", 38), ("R", 22) }
            .SelectMany(group => Enumerable.Range(1, group.Item2).Select(number => DuanYuCard.Canonical($"DY_{group.Item1}{number:00}"))).ToArray();
        EngineSelfTests.Equal(83, regular.Length, "regular reward catalog size");
        EngineSelfTests.Equal(33, regular.Count(card => card.Type == CardType.Attack), "regular reward catalog includes 33 attacks");
        var specifications = new (string Id, int Damage, int DamageUp, int Block, int BlockUp, int Swords, bool All)[]
        {
            ("DY_C07", 11, 15, 4, 6, 1, false), ("DY_C08", 8, 12, 0, 0, 1, false),
            ("DY_C13", 7, 10, 0, 0, 0, false),
            ("DY_C18", 8, 12, 4, 6, 1, false), ("DY_U10", 10, 14, 0, 0, 1, true),
            ("DY_U11", 8, 12, 2, 3, 1, false), ("DY_U12", 10, 14, 2, 2, 1, false),
            ("DY_U20", 11, 15, 4, 6, 0, false), ("DY_U21", 10, 14, 0, 0, 0, false),
            ("DY_U22", 13, 18, 0, 0, 0, false), ("DY_R09", 15, 20, 0, 0, 1, true),
            ("DY_R10", 15, 20, 0, 0, 3, false), ("DY_R20", 20, 27, 16, 22, 0, true)
        };
        foreach (var specification in specifications)
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            await SetQi(fixture, specification.Id == "DY_R20" ? 3 : 0);
            var secondEnemy = fixture.Combat.CreateCreature(ModelDb.Monster<BigDummy>().ToMutable(), CombatSide.Enemy, null);
            fixture.Combat.AddCreature(secondEnemy);
            var firstHp = fixture.Enemy.CurrentHp;
            var secondHp = secondEnemy.CurrentHp;
            var existingSword = specification.Id == "DY_R09"
                ? EngineSelfTests.AddCard(fixture, "DY_T01", PileType.Hand)
                : null;
            var card = EngineSelfTests.AddCard(fixture, specification.Id, PileType.Hand, upgraded);
            var energyBefore = fixture.Player.PlayerCombatState!.Energy;
            var cost = card.EnergyCost.GetAmountToSpend();
            EngineSelfTests.Equal(CardType.Attack, card.Type, specification.Id + " uses native attack type");
            EngineSelfTests.Equal(specification.All ? TargetType.AllEnemies : TargetType.AnyEnemy, card.TargetType, specification.Id + " native targeting");
            await PaidPlay(fixture, card);
            var damage = upgraded ? specification.DamageUp : specification.Damage;
            EngineSelfTests.Equal(damage, firstHp - fixture.Enemy.CurrentHp, specification.Id + " actual first target damage");
            EngineSelfTests.Equal(specification.All ? damage : 0, secondHp - secondEnemy.CurrentHp, specification.Id + " actual second target damage");
            var expectedBlock = upgraded ? specification.BlockUp : specification.Block;
            EngineSelfTests.Equal(expectedBlock, fixture.Player.Creature.Block, specification.Id + " retains block effect");
            var generatedSwords = PileType.Hand.GetPile(fixture.Player).Cards
                .Where(candidate => DuanYuState.IsSword(candidate) && candidate != existingSword).ToArray();
            EngineSelfTests.Equal(specification.Swords, generatedSwords.Length, specification.Id + " retains sword generation");
            if (existingSword != null)
            {
                EngineSelfTests.Equal(upgraded, existingSword.IsUpgraded, "only upgraded Sword Return upgrades existing swords");
                EngineSelfTests.Equal(upgraded, generatedSwords.Single().IsUpgraded, "Sword Return generates a sword of the matching upgrade level");
            }
            EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SkillsPlayed, specification.Id + " does not consume first skill trigger");
            EngineSelfTests.Equal(energyBefore - cost, fixture.Player.PlayerCombatState.Energy, specification.Id + " does not receive first skill energy");
            if (specification.Id == "DY_U20")
            {
                EngineSelfTests.Equal(1, PileType.Hand.GetPile(fixture.Player).Cards.OfType<DY_S01>().Count(), "evasive strike creates stagnation in hand");
                EngineSelfTests.Equal(2, fixture.Enemy.GetPowerAmount<WeakPower>(), "evasive strike retains two Weak alongside stagnation");
            }
            if (specification.Id is "DY_R09" or "DY_R10")
            {
                EngineSelfTests.Equal(false, card.Keywords.Contains(CardKeyword.Exhaust), specification.Id + " no longer carries Exhaust");
                EngineSelfTests.Equal(PileType.Discard, card.Pile!.Type, specification.Id + " returns to discard after use");
                await CardPileCmd.Add(card, PileType.Hand);
                fixture.Player.PlayerCombatState.Energy = 2;
                var health = fixture.Enemy.CurrentHp;
                await PaidPlay(fixture, card);
                EngineSelfTests.Equal(damage, health - fixture.Enemy.CurrentHp, specification.Id + " can be played again from discard");
                EngineSelfTests.Equal(PileType.Discard, card.Pile!.Type, specification.Id + " remains reusable after the second play");
            }
        }
    }

    private static async Task QiSources(bool upgraded)
    {
        foreach (var id in new[] { "DY_B03", "DY_C10", "DY_U03" })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            await SetQi(fixture, 0);
            var card = EngineSelfTests.AddCard(fixture, id, PileType.Hand, upgraded);
            fixture.Player.PlayerCombatState!.Energy = card.EnergyCost.GetAmountToSpend();
            await PaidPlay(fixture, card);
            var qi = id == "DY_B03" ? upgraded ? 2 : 1 : upgraded ? 3 : 2;
            var block = id switch { "DY_B03" => upgraded ? 7 : 5, "DY_U03" => upgraded ? 17 : 13, _ => 0 };
            EngineSelfTests.Equal(qi, DuanYuState.Get(fixture.Player).Qi, id + " grants its exact rebalanced Qi");
            EngineSelfTests.Equal(block, fixture.Player.Creature.Block, id + " retains its printed block");
            EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, id + " pays its actual energy cost");
            EngineSelfTests.Equal(PileType.Discard, card.Pile!.Type, id + " goes to discard after playing");
        }
    }

    private static async Task EnergyCards(bool upgraded)
    {
        foreach (var id in new[] { "DY_U02", "DY_U04", "DY_R04" })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            await SetQi(fixture, 0);
            var card = EngineSelfTests.AddCard(fixture, id, PileType.Hand, upgraded);
            var paid = id == "DY_U02" ? 1 : 0;
            fixture.Player.PlayerCombatState!.Energy = paid;
            var health = fixture.Player.Creature.CurrentHp;
            await PaidPlay(fixture, card);
            var energy = id switch { "DY_U02" when upgraded => 3, "DY_U02" => 2, "DY_U04" when upgraded => 2, "DY_U04" => 1, "DY_R04" when upgraded => 2, "DY_R04" => 1, _ => 0 };
            EngineSelfTests.Equal(energy, fixture.Player.PlayerCombatState.Energy, id + " energy after actual payment");
            var qi = upgraded ? 3 : 2;
            EngineSelfTests.Equal(qi, DuanYuState.Get(fixture.Player).Qi, id + " printed Qi");
            EngineSelfTests.Equal(id == "DY_U04" ? upgraded ? 2 : 3 : 0, health - fixture.Player.Creature.CurrentHp, id + " HP payment");
            EngineSelfTests.Equal(id == "DY_U04" ? PileType.Discard : PileType.Exhaust, card.Pile!.Type, id + " resource card lifecycle");
        }
        var empty = await EngineSelfTests.ReadyFixture();
        empty.Player.PlayerCombatState!.Energy = 0;
        var requiresPayment = EngineSelfTests.AddCard(empty, "DY_U02", PileType.Hand, upgraded);
        EngineSelfTests.Equal(false, requiresPayment.CanPlay(), "Qi Sea cannot borrow its future energy");
    }

    private static async Task ReadBook(bool upgraded)
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        await SetQi(fixture, 0);
        fixture.Player.PlayerCombatState!.Energy = 0;
        var card = EngineSelfTests.AddCard(fixture, "DY_C14", PileType.Hand, upgraded);
        EngineSelfTests.Equal(false, card.HasQiCost, "read book has no Qi cost");
        await PaidPlay(fixture, card);
        var hand = PileType.Hand.GetPile(fixture.Player).Cards;
        EngineSelfTests.Equal(upgraded ? 4 : 3, hand.OfType<DY_B01>().Count(), "read book draws its printed number of cards");
        EngineSelfTests.Equal(1, hand.OfType<DY_S01>().Count(), "read book creates one stagnation in hand");
        EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, "read book costs and grants zero energy");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SpendingCount, "read book does not trigger Qi spend effects");
    }

    private static async Task SixSwordPayment(bool upgraded)
    {
        foreach (var qi in new[] { 0, 3 })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            await SetQi(fixture, qi);
            fixture.Player.PlayerCombatState!.Energy = 2;
            var card = EngineSelfTests.AddCard(fixture, "DY_R03", PileType.Hand, upgraded);
            EngineSelfTests.Equal(0, card.RequiredQi, "six swords has no Qi cost");
            await PaidPlay(fixture, card);
            var swords = PileType.Hand.GetPile(fixture.Player).Cards.Where(DuanYuState.IsSword).ToArray();
            EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, "six swords has no starter relic energy refund");
            EngineSelfTests.Equal(upgraded ? 11 : 8, fixture.Player.Creature.Block, "six swords immediate block");
            EngineSelfTests.Equal(6, swords.Length, "six swords generates with or without Qi");
            EngineSelfTests.Equal(6, swords.Select(sword => sword.Id).Distinct().Count(), "six swords are distinct");
            EngineSelfTests.Equal(true, swords.All(sword => sword.IsUpgraded == upgraded), "six swords tokens preserve the generator upgrade");
            EngineSelfTests.Equal(qi, DuanYuState.Get(fixture.Player).Qi, "six sword generation leaves Qi untouched");
            EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SwordPlays, "generation does not trigger sword-play abilities");
        }
    }

    private static async Task SwordResourceBoundaries(bool upgraded)
    {
        foreach (var id in new[] { "DY_T01", "DY_T02", "DY_T03", "DY_T04", "DY_T05", "DY_T06" })
        {
            foreach (var funded in new[] { false, true })
            {
                var fixture = await EngineSelfTests.ReadyFixture();
                var cost = 1;
                await SetQi(fixture, funded ? cost : 0);
                fixture.Player.PlayerCombatState!.Energy = 0;
                if (!funded)
                {
                    var unavailable = EngineSelfTests.AddCard(fixture, id, PileType.Hand, upgraded);
                    EngineSelfTests.Equal(false, unavailable.CanPlay(), id + " cannot play without one Qi");
                    continue;
                }
                var health = fixture.Enemy.CurrentHp;
                var sword = await PaidPlay(fixture, id, upgraded);
                EngineSelfTests.Equal(cost, sword.QiCost, id + " preview reports the actual Qi payment");
                var damage = id switch
                {
                    "DY_T01" => upgraded ? 10 : 7,
                    "DY_T02" => upgraded ? 5 : 3,
                    "DY_T03" => upgraded ? 7 : 5,
                    "DY_T04" => upgraded ? 5 : 3,
                    "DY_T05" => upgraded ? 3 : 2,
                    _ => upgraded ? 4 : 2
                };
                EngineSelfTests.Equal(damage, health - fixture.Enemy.CurrentHp, id + " funded damage");
                EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, id + " pays one Qi");
                EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, id + " grants no energy");
                EngineSelfTests.Equal(id == "DY_T04" ? (upgraded ? 5 : 3) : 0, fixture.Player.Creature.Block, id + " conditional block");
                EngineSelfTests.Equal(id == "DY_T05" ? 1 : 0, PileType.Hand.GetPile(fixture.Player).Cards.Count, id + " native draw");
                EngineSelfTests.Equal(id == "DY_T02" ? 1 : 0, fixture.Enemy.GetPowerAmount<VulnerablePower>(), id + " vulnerable");
                EngineSelfTests.Equal(id == "DY_T06" ? 1 : 0, fixture.Enemy.GetPowerAmount<WeakPower>(), id + " weak");
                EngineSelfTests.Equal(true, sword.Keywords.Contains(CardKeyword.Ethereal) && PileType.Exhaust.GetPile(fixture.Player).Cards.Contains(sword), id + " bounded token lifecycle");
            }
        }
    }

    private static async Task ProficiencyAllSwords(bool upgraded)
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 2;
        await PaidPlay(fixture, "DY_U14", upgraded);
        int ordinaryHealth = fixture.Enemy.CurrentHp;
        await PaidPlay(fixture, "DY_B01");
        EngineSelfTests.Equal(6, ordinaryHealth - fixture.Enemy.CurrentHp, "proficiency excludes ordinary attacks");
        for (var ordinal = 1; ordinal <= 4; ordinal++)
        {
            await SetQi(fixture, 0);
            var health = fixture.Enemy.CurrentHp;
            await PaidPlay(fixture, "DY_T01");
            var expected = 7 + ordinal - 1 + (upgraded ? 3 : 2);
            EngineSelfTests.Equal(expected, health - fixture.Enemy.CurrentHp, "proficiency adds damage to every sword");
        }
    }

    private static async Task BeginningBeiming()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        var player = fixture.Player;
        var hand = PileType.Hand.GetPile(player);
        var state = DuanYuState.Get(player);
        await SetQi(fixture, 0);
        player.PlayerCombatState!.Energy = 1;

        var card = EngineSelfTests.AddCard(fixture, "DY_U01", PileType.Hand);
        const string rule = "运气1。每回合首次支付真气后，抽1张牌。";
        EngineSelfTests.Equal(rule, card.GetDescriptionForPile(PileType.Hand), "base card shows the payment trigger");
        EngineSelfTests.Equal(true, card.HoverTips.OfType<HoverTip>().Any(tip => tip.Description.EndsWith("打出本牌无需支付真气。", StringComparison.Ordinal)),
            "the live Qi tip specifies the resource waived");
        EngineSelfTests.Equal(1, card.EnergyCost.GetAmountToSpend(), "base card still needs one energy");
        await PaidPlay(fixture, card);
        EngineSelfTests.Equal(0, player.PlayerCombatState.Energy, "base card pays one energy");
        EngineSelfTests.Equal(1, state.Qi, "base card grants one Qi");
        EngineSelfTests.Equal(0, hand.Cards.Count, "installing the power does not draw");
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(1, hand.Cards.Count, "the first payment draws one card");

        var upgraded = EngineSelfTests.AddCard(fixture, "DY_U01", PileType.Hand, true);
        EngineSelfTests.Equal(rule, upgraded.GetDescriptionForPile(PileType.Hand), "upgraded card keeps the same effect");
        await PaidPlay(fixture, upgraded);
        EngineSelfTests.Equal(0, player.PlayerCombatState.Energy, "upgraded card costs zero energy");
        EngineSelfTests.Equal(1, state.Qi, "upgraded card also grants one Qi");
        var power = player.Creature.GetPower<BeginningBeimingPower>()!;
        EngineSelfTests.Equal(2, power.Amount, "both versions stack in the combat power");
        EngineSelfTests.Equal("每回合首次支付真气后，抽2张牌。", power.GetDumbHoverTip().Description, "stacked power tip previews the combined draw");
        EngineSelfTests.Equal(1, hand.Cards.Count, "installing a stack late does not draw retroactively");
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(1, hand.Cards.Count, "the current turn's second payment does not draw");

        await NextTurn(fixture);
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(3, hand.Cards.Count, "the next turn's first payment draws once per stack");
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(3, hand.Cards.Count, "the next turn's second payment does not draw");
    }

    private static async Task BeimingLimit()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 8;
        await PaidPlay(fixture, "DY_R01");
        await PaidPlay(fixture, "DY_R01");
        await SetQi(fixture, 3);
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(6, fixture.Player.PlayerCombatState.Energy, "first spend gives no Beiming energy");
        EngineSelfTests.Equal(2, DuanYuState.Get(fixture.Player).Qi, "first spend gives no Beiming Qi");
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(8, fixture.Player.PlayerCombatState.Energy, "second spend refunds one energy per Beiming stack");
        EngineSelfTests.Equal(3, DuanYuState.Get(fixture.Player).Qi, "second spend refunds one Qi per Beiming stack");
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(2, DuanYuState.Get(fixture.Player).Qi, "third spend gives no refund");
        await PaidPlay(fixture, "DY_R01");
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(7, fixture.Player.PlayerCombatState.Energy, "late Beiming does not reset energy ordinal");
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).Qi, "fourth spend does not repeat the pair reward or reset its ordinal");
        await NextTurn(fixture);
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(7, fixture.Player.PlayerCombatState.Energy, "next turn first spend still gives no energy");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "next turn first spend still gives no Qi");
        await DuanYuState.GainQi(fixture.Player, 1);
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(10, fixture.Player.PlayerCombatState.Energy, "three Beiming stacks refresh on the next turn's second payment");
        EngineSelfTests.Equal(3, DuanYuState.Get(fixture.Player).Qi, "next turn second spend refunds one Qi per each of three stacks");
    }

    private static async Task SpendCallbackIdempotence()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 3;
        await PaidPlay(fixture, "DY_U01");
        await PaidPlay(fixture, "DY_R01");
        await SetQi(fixture, 3);
        var source = EngineSelfTests.AddCard(fixture, "DY_T01", PileType.Hand);
        var play = new CardPlay
        {
            Card = source, Player = fixture.Player, Target = fixture.Enemy, ResultPile = PileType.Exhaust,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 },
            IsAutoPlay = false, PlayIndex = 0, PlayCount = 1
        };
        var context = new ThrowingPlayerChoiceContext();
        DuanYuState.BeforeCard(play);
        EngineSelfTests.Equal(true, await DuanYuState.SpendQi(fixture.Player, 1, source), "callback fixture spends real Qi");
        await DuanYuState.AfterQiSpent(fixture.Player, context, source);
        EngineSelfTests.Equal(2, DuanYuState.Get(fixture.Player).Qi, "first callback gives no second-payment Qi reward");
        EngineSelfTests.Equal(1, fixture.Player.PlayerCombatState.Energy, "first callback gives no second-payment energy reward");
        EngineSelfTests.Equal(2, PileType.Hand.GetPile(fixture.Player).Cards.Count, "first callback draws once");
        await DuanYuState.AfterQiSpent(fixture.Player, context, source);
        await DuanYuState.AfterCard(play, context);
        await DuanYuState.AfterQiSpent(fixture.Player, context, source);
        EngineSelfTests.Equal(2, DuanYuState.Get(fixture.Player).Qi, "duplicate active and completed callbacks cannot refund Qi");
        EngineSelfTests.Equal(1, fixture.Player.PlayerCombatState.Energy, "duplicate callbacks cannot refund energy");
        EngineSelfTests.Equal(2, PileType.Hand.GetPile(fixture.Player).Cards.Count, "duplicate callbacks cannot draw again");
        EngineSelfTests.Equal(true, await DuanYuState.SpendQi(fixture.Player, 1), "second callback fixture spends real Qi");
        await DuanYuState.AfterQiSpent(fixture.Player, context);
        EngineSelfTests.Equal(2, DuanYuState.Get(fixture.Player).Qi, "second payment grants its Qi refund");
        EngineSelfTests.Equal(2, fixture.Player.PlayerCombatState.Energy, "second payment grants its energy refund");
        await DuanYuState.AfterQiSpent(fixture.Player, context);
        EngineSelfTests.Equal(2, DuanYuState.Get(fixture.Player).Qi, "duplicate second-payment callback cannot refund Qi twice");
        EngineSelfTests.Equal(2, fixture.Player.PlayerCombatState.Energy, "duplicate second-payment callback cannot refund energy twice");
        await NextTurn(fixture);
        await SetQi(fixture, 0);
        EngineSelfTests.Equal(false, await DuanYuState.SpendQi(fixture.Player, 1, source), "insufficient Qi does not register spend");
        await DuanYuState.AfterQiSpent(fixture.Player, context, source);
        EngineSelfTests.Equal(2, fixture.Player.PlayerCombatState.Energy, "failed spend callback gives no energy");
    }

    private static async Task UnityThresholds()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 4;
        await PaidPlay(fixture, "DY_R08");
        await PaidPlay(fixture, "DY_R08", true);
        var ids = new[] { "DY_T01", "DY_T02", "DY_T03", "DY_T03", "DY_T04", "DY_T05", "DY_T06", "DY_T06" };
        for (var index = 0; index < ids.Length; index++)
        {
            await SetQi(fixture, 0);
            await PaidPlay(fixture, ids[index]);
            var thresholds = index >= 6 ? 2 : index >= 2 ? 1 : 0;
            EngineSelfTests.Equal(3 + thresholds * 2, fixture.Player.PlayerCombatState.Energy, "unity gives energy at each eligible threshold only");
            EngineSelfTests.Equal(thresholds * 3 + (index >= 5 ? 1 : 0), PileType.Hand.GetPile(fixture.Player).Cards.Count, "mixed unity draws exact amounts");
            var qi = index == 2 ? 2 : index == 6 ? 2 : 0;
            EngineSelfTests.Equal(qi, DuanYuState.Get(fixture.Player).Qi, "unity returns Qi only at third and sixth kinds");
        }
        EngineSelfTests.Equal(6, DuanYuState.Get(fixture.Player).DistinctSwords.Count, "duplicates do not create new sword kinds");
    }

    private static async Task UpgradedSwordIdentity()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 1;
        await PaidPlay(fixture, "DY_R08");
        var cards = new (string Id, bool Upgraded, int Kinds)[]
        {
            ("DY_T01", false, 1), ("DY_T01", true, 1), ("DY_T02", true, 2),
            ("DY_T03", false, 3), ("DY_T03", true, 3), ("DY_T04", true, 4),
            ("DY_T05", false, 5), ("DY_T06", true, 6), ("DY_T06", false, 6)
        };
        for (var index = 0; index < cards.Length; index++)
        {
            var expected = cards[index];
            await SetQi(fixture, 0);
            var sword = await PaidPlay(fixture, expected.Id, expected.Upgraded);
            var thresholds = index >= 7 ? 2 : index >= 3 ? 1 : 0;
            EngineSelfTests.Equal(expected.Upgraded, sword.IsUpgraded, "identity case uses actual upgraded card models");
            EngineSelfTests.Equal(expected.Kinds, DuanYuState.Get(fixture.Player).DistinctSwords.Count, "upgraded and normal swords share a kind");
            EngineSelfTests.Equal(thresholds, fixture.Player.PlayerCombatState.Energy, "upgrading a repeated sword cannot create a second unity trigger");
            EngineSelfTests.Equal(thresholds + (index >= 6 ? 1 : 0), PileType.Hand.GetPile(fixture.Player).Cards.Count, "upgraded duplicate swords cannot repeat unity draws");
            EngineSelfTests.Equal(index == 3 ? 1 : index == 7 ? 1 : 0, DuanYuState.Get(fixture.Player).Qi, "upgraded duplicate swords cannot repeat unity Qi");
        }
        EngineSelfTests.Equal(9, DuanYuState.Get(fixture.Player).SwordPlays, "all normal and upgraded plays still count as sword plays");
    }

    private static async Task NativeReplaySpending()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 4;
        await PaidPlay(fixture, "DY_R01");
        await PaidPlay(fixture, "DY_R15", true);
        await PaidPlay(fixture, "DY_U01");
        await SetQi(fixture, 1);
        var sword = EngineSelfTests.AddCard(fixture, "DY_T01", PileType.Hand);
        sword.BaseReplayCount = 2;
        var health = fixture.Enemy.CurrentHp;
        await PaidPlay(fixture, sword);
        AssertNativeReplaySeries(fixture, sword, 3);
        EngineSelfTests.Equal(24, health - fixture.Enemy.CurrentHp, "native replay executes three single-hit bodies using one paid activation with increasing momentum");
        EngineSelfTests.Equal(3, DuanYuState.Get(fixture.Player).SwordMomentum, "each replay adds momentum after its combined damage");
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).SpendingCount, "native replay pays Qi once per activation");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "first replay activation receives no Beiming refund");
        EngineSelfTests.Equal(2, fixture.Player.PlayerCombatState.Energy, "sword replay gets exactly one first-sword energy from simplicity");
        EngineSelfTests.Equal(1, PileType.Hand.GetPile(fixture.Player).Cards.Count, "beginning Beiming draws only on the first replay spend");
        EngineSelfTests.Equal(3, DuanYuState.Get(fixture.Player).SwordPlays, "each native replay advances the sword play ordinal");
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).DistinctSwords.Count, "native replay cannot introduce a new sword identity");
        await SetQi(fixture, 0);
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(2, DuanYuState.Get(fixture.Player).SpendingCount, "a separately funded follow-up records a new payment");
        EngineSelfTests.Equal(3, fixture.Player.PlayerCombatState.Energy, "follow-up sword triggers only Beiming's second payment");
    }

    private static async Task NativeReplayUnityThresholds()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 1;
        await PaidPlay(fixture, "DY_R08");
        await SetQi(fixture, 0);
        await PaidPlay(fixture, "DY_T01");
        await PaidPlay(fixture, "DY_T04");
        var thirdKind = EngineSelfTests.AddCard(fixture, "DY_T03", PileType.Hand, true);
        thirdKind.BaseReplayCount = 2;
        var health = fixture.Enemy.CurrentHp;
        await PaidPlay(fixture, thirdKind);
        AssertNativeReplaySeries(fixture, thirdKind, 3);
        EngineSelfTests.Equal(32, health - fixture.Enemy.CurrentHp, "third-kind activation is paid once and momentum increases between single-hit replays");
        EngineSelfTests.Equal(6, DuanYuState.Get(fixture.Player).SwordMomentum, "third-kind replay grants its extra momentum only once");
        EngineSelfTests.Equal(1, fixture.Player.PlayerCombatState.Energy, "third-kind native replay retains unity energy");
        EngineSelfTests.Equal(1, PileType.Hand.GetPile(fixture.Player).Cards.Count, "third-kind native replay grants unity draw once");
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).Qi, "third-kind native replay preserves its unity Qi refund");
        EngineSelfTests.Equal(3, DuanYuState.Get(fixture.Player).SpendingCount, "third-kind replay pays one Qi fee per activation");
        await PaidPlay(fixture, "DY_T02");
        await PaidPlay(fixture, "DY_T05");
        await SetQi(fixture, 0);
        var sixthKind = EngineSelfTests.AddCard(fixture, "DY_T06", PileType.Hand, true);
        sixthKind.BaseReplayCount = 2;
        await PaidPlay(fixture, sixthKind);
        AssertNativeReplaySeries(fixture, sixthKind, 3);
        EngineSelfTests.Equal(2, fixture.Player.PlayerCombatState.Energy, "sixth-kind replay adds only unity energy grants");
        EngineSelfTests.Equal(3, PileType.Hand.GetPile(fixture.Player).Cards.Count, "each unity threshold and the fifth sword draw once");
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).Qi, "sixth-kind replay preserves unity Qi refund");
        EngineSelfTests.Equal(6, DuanYuState.Get(fixture.Player).SpendingCount, "each threshold series pays one fee per activation");
        EngineSelfTests.Equal(6, DuanYuState.Get(fixture.Player).DistinctSwords.Count, "two replay series still contain only six sword kinds");
        EngineSelfTests.Equal(10, DuanYuState.Get(fixture.Player).SwordPlays, "native replay records every actual sword play");
        EngineSelfTests.Equal(8, DuanYuState.Get(fixture.Player).SwordMomentum, "native replay stacks momentum to its reduced cap");
    }

    private static void AssertNativeReplaySeries(TestFixture fixture, DuanYuCard card, int playCount)
    {
        var plays = CombatManager.Instance.History.CardPlaysFinished.Where(entry => ReferenceEquals(entry.CardPlay.Card, card)).Select(entry => entry.CardPlay).ToArray();
        EngineSelfTests.Equal(playCount, plays.Length, "native history records each replay completion");
        EngineSelfTests.Equal(playCount, plays.Distinct().Count(), "native replay produces distinct CardPlay instances");
        for (var index = 0; index < plays.Length; index++)
        {
            EngineSelfTests.Equal(index, plays[index].PlayIndex, "native replay index");
            EngineSelfTests.Equal(playCount, plays[index].PlayCount, "native replay total play count");
        }
        EngineSelfTests.Equal(1, PileType.Exhaust.GetPile(fixture.Player).Cards.Count(existing => ReferenceEquals(existing, card)), "replay moves the physical token into exhaust exactly once");
        EngineSelfTests.Equal(false, PileType.Play.GetPile(fixture.Player).Cards.Contains(card), "replayed token leaves the play pile");
    }

    private static async Task UnityLateApplication()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 1;
        foreach (var id in new[] { "DY_T01", "DY_T02", "DY_T03" })
        {
            await SetQi(fixture, 0);
            await PaidPlay(fixture, id);
        }
        await PaidPlay(fixture, "DY_R08");
        await SetQi(fixture, 0);
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, "late unity does not replay the third-kind threshold");
        foreach (var id in new[] { "DY_T04", "DY_T05", "DY_T06" })
        {
            await SetQi(fixture, 0);
            await PaidPlay(fixture, id);
        }
        EngineSelfTests.Equal(1, fixture.Player.PlayerCombatState.Energy, "late unity triggers the remaining sixth-kind threshold");
        await NextTurn(fixture);
        foreach (var id in new[] { "DY_T01", "DY_T02", "DY_T03" })
        {
            await SetQi(fixture, 0);
            await PaidPlay(fixture, id);
        }
        EngineSelfTests.Equal(2, fixture.Player.PlayerCombatState.Energy, "unity threshold refreshes next turn");
    }

    private static async Task SimplicityTurnStartStacks()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 6;
        await PaidPlay(fixture, "DY_R15");
        await PaidPlay(fixture, "DY_R15", true);
        await PaidPlay(fixture, "DY_R15", true);
        await SetQi(fixture, 3);
        await DuanYuState.LoseQi(fixture.Player, 1);
        EngineSelfTests.Equal(2, fixture.Player.PlayerCombatState.Energy, "losing Qi grants no simplicity energy");
        await PaidPlay(fixture, "DY_B05");
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).SpendingCount, "the ordinary attack pays Qi");
        EngineSelfTests.Equal(1, fixture.Player.PlayerCombatState.Energy, "a non-sword Qi payment grants no simplicity energy");
        await DuanYuCardExecutor.GenerateSword(fixture.Player, 1, false, freeQi: true);
        var freeSword = PileType.Hand.GetPile(fixture.Player).Cards.OfType<DY_T01>().Single();
        await PaidPlay(fixture, freeSword);
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).SpendingCount, "the first sword does not spend Qi");
        EngineSelfTests.Equal(4, fixture.Player.PlayerCombatState.Energy, "the free first sword grants one energy per copy");
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(4, fixture.Player.PlayerCombatState.Energy, "the second sword grants no simplicity energy even when paid");
        await DuanYuState.EndTurn(fixture.Player);
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "simplicity does not grant Qi at turn end");
        EngineSelfTests.Equal(4, fixture.Player.PlayerCombatState.Energy, "simplicity does not grant energy at turn end");
        await NextTurn(fixture);
        EngineSelfTests.Equal(6, DuanYuState.Get(fixture.Player).Qi, "simplicity grants two Qi per copy at turn start");
        EngineSelfTests.Equal(4, fixture.Player.PlayerCombatState.Energy, "simplicity does not grant energy at turn start");
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(7, fixture.Player.PlayerCombatState.Energy, "the first sword on the next turn grants energy again");
        await NextTurn(fixture);
        EngineSelfTests.Equal(11, DuanYuState.Get(fixture.Player).Qi, "simplicity grants Qi again on subsequent turns");
        EngineSelfTests.Equal(7, fixture.Player.PlayerCombatState.Energy, "turn-start Qi remains separate from first-sword energy");
    }

    private static async Task SimplicityCost(bool upgraded)
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 2;
        await SetQi(fixture, 0);
        var card = EngineSelfTests.AddCard(fixture, "DY_R15", PileType.Hand, upgraded);
        EngineSelfTests.Equal(upgraded ? 1 : 2, card.EnergyCost.GetAmountToSpend(), "simplicity uses its new two/one energy cost");
        await PaidPlay(fixture, card);
        EngineSelfTests.Equal(upgraded ? 1 : 0, fixture.Player.PlayerCombatState.Energy, "simplicity pays its actual upgraded or normal cost");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "simplicity does not grant Qi immediately");
        await NextTurn(fixture);
        EngineSelfTests.Equal(2, DuanYuState.Get(fixture.Player).Qi, "simplicity grants two Qi on the next turn for either upgrade");
        EngineSelfTests.Equal(upgraded ? 1 : 0, fixture.Player.PlayerCombatState.Energy, "simplicity does not grant energy at turn start");
    }

    private static async Task RareDefense(bool upgraded)
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 1;
        var card = EngineSelfTests.AddCard(fixture, "DY_R11", PileType.Hand, upgraded);
        EngineSelfTests.Equal((decimal)(upgraded ? 10 : 8), card.DynamicVars.Block.BaseValue, "independent wind exposes its reduced block in the live preview");
        await PaidPlay(fixture, card);
        EngineSelfTests.Equal(upgraded ? 10 : 8, fixture.Player.Creature.Block, "independent wind grants eight/ten block");
        EngineSelfTests.Equal(upgraded ? 3 : 2, DuanYuState.Get(fixture.Player).Lingbo, "independent wind grants two/three Lingbo");
        EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, "independent wind retains its one energy cost");
        EngineSelfTests.Equal(PileType.Discard, card.Pile!.Type, "independent wind goes to the discard pile after play");
    }

    private static async Task FootworkRoles(bool upgraded)
    {
        var attack = await EngineSelfTests.ReadyFixture();
        attack.Player.PlayerCombatState!.Energy = 2;
        var initialHp = attack.Enemy.CurrentHp;
        await PaidPlay(attack, "DY_C06", upgraded);
        EngineSelfTests.Equal(0, attack.Player.Creature.Block, "returning finger checks Lingbo before adding its own layer");
        EngineSelfTests.Equal(1, DuanYuState.Get(attack.Player).Lingbo, "returning finger starts Lingbo by itself");
        await PaidPlay(attack, "DY_C06", upgraded);
        EngineSelfTests.Equal(upgraded ? 7 : 5, attack.Player.Creature.Block, "returning finger grants block with existing Lingbo");
        EngineSelfTests.Equal(2, DuanYuState.Get(attack.Player).Lingbo, "returning finger adds Lingbo on both plays");
        EngineSelfTests.Equal(upgraded ? 30 : 22, initialHp - attack.Enemy.CurrentHp, "returning finger keeps its damage");

        var sidestep = await EngineSelfTests.ReadyFixture();
        sidestep.Player.PlayerCombatState!.Energy = 0;
        await PaidPlay(sidestep, "DY_C12", upgraded);
        EngineSelfTests.Equal(upgraded ? 3 : 2, sidestep.Player.Creature.Block, "sidestep adds immediate block at zero energy");
        EngineSelfTests.Equal(upgraded ? 2 : 1, DuanYuState.Get(sidestep.Player).Lingbo, "sidestep retains its Lingbo");

        var defense = await EngineSelfTests.ReadyFixture();
        defense.Player.PlayerCombatState!.Energy = 3;
        await PaidPlay(defense, "DY_U18", upgraded);
        var shadowBlock = upgraded ? 8 : 6;
        EngineSelfTests.Equal(shadowBlock, defense.Player.Creature.Block, "shadow step grants its printed block");
        EngineSelfTests.Equal(1, DuanYuState.Get(defense.Player).Lingbo, "shadow step grants one Lingbo immediately");
        var hand = PileType.Hand.GetPile(defense.Player).Cards;
        var beforeDraw = hand.Count;
        EngineSelfTests.AddCard(defense, "DY_B01", PileType.Draw);
        EngineSelfTests.AddCard(defense, "DY_B01", PileType.Draw);
        await PaidPlay(defense, "DY_U24", upgraded);
        EngineSelfTests.Equal(0, defense.Player.PlayerCombatState.Energy, "shadow step and roaming step cost one and two energy");
        EngineSelfTests.Equal(shadowBlock + (upgraded ? 17 : 13), defense.Player.Creature.Block, "roaming step grants its printed block");
        EngineSelfTests.Equal(beforeDraw + 2, hand.Count, "roaming step retains two draws");
        await NextTurn(defense);
        EngineSelfTests.Equal(1, DuanYuState.Get(defense.Player).Lingbo, "shadow step grants a new Lingbo layer next turn");
        await NextTurn(defense);
        EngineSelfTests.Equal(0, DuanYuState.Get(defense.Player).Lingbo, "shadow step grants its delayed layer once");
    }

    private static async Task SevenStarStep(bool upgraded)
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 1;
        var hand = PileType.Hand.GetPile(fixture.Player);
        var cardsBefore = hand.Cards.Count;
        EngineSelfTests.AddCard(fixture, "DY_B01", PileType.Draw);
        await PaidPlay(fixture, "DY_U34", upgraded);
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).Lingbo, "seven star step grants immediate Lingbo");
        EngineSelfTests.Equal(cardsBefore, hand.Cards.Count, "seven star step does not draw immediately");
        await NextTurn(fixture);
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).Lingbo, "seven star step grants Lingbo after old Lingbo clears");
        EngineSelfTests.Equal(cardsBefore + 1, hand.Cards.Count, "seven star step keeps its next-turn draw");
        await NextTurn(fixture);
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Lingbo, "delayed Lingbo is granted only once");
    }

    private static async Task FormlessFirstThreeSwords(bool upgraded)
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 5;
        await PaidPlay(fixture, "DY_R18", upgraded);
        await SetQi(fixture, 20);
        for (int ordinal = 1; ordinal <= 5; ordinal++)
        {
            await PaidPlay(fixture, "DY_T01");
            EngineSelfTests.Equal(Math.Min(ordinal, 3) * (upgraded ? 6 : 4), fixture.Player.Creature.Block, "formless grants block only after the first three swords each turn");
        }
        await PaidPlay(fixture, "DY_R18", !upgraded);
        int blockBefore = fixture.Player.Creature.Block;
        var replay = EngineSelfTests.AddCard(fixture, "DY_T01", PileType.Hand);
        replay.BaseReplayCount = 2;
        await PaidPlay(fixture, replay);
        EngineSelfTests.Equal(blockBefore, fixture.Player.Creature.Block, "stacking a new copy does not restart the three-sword limit");
        await PaidPlay(fixture, "DY_B01");
        EngineSelfTests.Equal(blockBefore, fixture.Player.Creature.Block, "ordinary attacks never trigger formless");
        await NextTurn(fixture);
        int nextTurnBlock = fixture.Player.Creature.Block;
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(nextTurnBlock + 10, fixture.Player.Creature.Block, "formless continues to trigger on the following turn");
    }

    private static async Task SwordArrayMixedUpgrades()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 2;
        await PaidPlay(fixture, "DY_R07");
        await PaidPlay(fixture, "DY_R07", true);
        EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, "each sword array costs one energy");
        AssertMixedSwords(fixture, 1, "on play");
        PileType.Hand.GetPile(fixture.Player).Clear();
        await NextTurn(fixture);
        AssertMixedSwords(fixture, 1, "on turn start");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SwordPlays, "sword array generation does not trigger sword powers");
    }

    private static void AssertMixedSwords(TestFixture fixture, int perUpgrade, string when)
    {
        var swords = PileType.Hand.GetPile(fixture.Player).Cards.Where(DuanYuState.IsSword).ToArray();
        EngineSelfTests.Equal(perUpgrade * 2, swords.Length, "mixed array produces the expected number of swords " + when);
        EngineSelfTests.Equal(perUpgrade, swords.Count(sword => sword.IsUpgraded), "mixed array preserves upgrade counts " + when);
        foreach (var upgraded in new[] { false, true })
            EngineSelfTests.Equal(perUpgrade, swords.Where(sword => sword.IsUpgraded == upgraded).Select(DuanYuState.CardId).Distinct().Count(), "one array selects different sword kinds " + when);
    }

    private static async Task SixSwordCycle()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 2;
        await SetQi(fixture, 9);
        await PaidPlay(fixture, "DY_U02");
        await PaidPlay(fixture, "DY_R03", true);
        foreach (var id in new[] { "DY_T05", "DY_T02", "DY_T06", "DY_T01", "DY_T03", "DY_T04" })
        {
            var sword = PileType.Hand.GetPile(fixture.Player).Cards.OfType<DuanYuCard>().Single(card => card.DesignId == id);
            await PaidPlay(fixture, sword);
        }
        EngineSelfTests.Equal(1, fixture.Player.PlayerCombatState.Energy, "Qi Sea leaves one energy after the six-sword cycle");
        EngineSelfTests.Equal(5, DuanYuState.Get(fixture.Player).Qi, "cycle spends Qi on the six swords only");
        EngineSelfTests.Equal(6, DuanYuState.Get(fixture.Player).SpendingCount, "cycle has six successful Qi payments");
        EngineSelfTests.Equal(8, DuanYuState.Get(fixture.Player).SwordMomentum, "one complete six-sword cycle reaches the momentum cap");
        EngineSelfTests.Equal(6, PileType.Exhaust.GetPile(fixture.Player).Cards.Count(DuanYuState.IsSword), "all six played swords exhaust");
    }

    private static async Task SelfDamageStopsAfterDeath()
    {
        foreach (var upgraded in new[] { false, true })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            await SetQi(fixture, 0);
            fixture.Player.PlayerCombatState!.Energy = 0;
            await CreatureCmd.SetCurrentHp(fixture.Player.Creature, upgraded ? 2 : 3);
            await PaidPlay(fixture, "DY_U04", upgraded);
            EngineSelfTests.Equal(false, fixture.Player.Creature.IsAlive, "self damage can kill at exact HP cost");
            EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, "dead player receives no energy");
            EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "dead player receives no Qi");
        }
    }

    private static async Task BasicUpgradeValues()
    {
        var blockFixture = await EngineSelfTests.ReadyFixture();
        blockFixture.Player.PlayerCombatState!.Energy = 1;
        await SetQi(blockFixture, 0);
        await PaidPlay(blockFixture, "DY_B03", true);
        EngineSelfTests.Equal(7, blockFixture.Player.Creature.Block, "upgraded B03 block");
        EngineSelfTests.Equal(2, DuanYuState.Get(blockFixture.Player).Qi, "upgraded B03 printed Qi");

        var attackFixture = await EngineSelfTests.ReadyFixture();
        attackFixture.Player.PlayerCombatState!.Energy = 1;
        await SetQi(attackFixture, 2);
        var initialHp = attackFixture.Enemy.CurrentHp;
        await PaidPlay(attackFixture, "DY_B05", true);
        EngineSelfTests.Equal(22, initialHp - attackFixture.Enemy.CurrentHp, "upgraded B05 combined damage");
        EngineSelfTests.Equal(0, DuanYuState.Get(attackFixture.Player).Qi, "upgraded B05 spends exactly two Qi");
    }

    private static async Task DamagePreviewNoQi()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        await SetQi(fixture, 3);
        var first = EngineSelfTests.AddCard(fixture, "DY_T01", PileType.Hand);
        var second = EngineSelfTests.AddCard(fixture, "DY_T03", PileType.Hand);
        var firstPreview = Hook.ModifyDamage(fixture.Run, fixture.Combat, fixture.Enemy, fixture.Player.Creature, first.DynamicVars.Damage.BaseValue,
            ValueProp.Move, first, null, ModifyDamageHookType.All, CardPreviewMode.Normal, out _);
        var secondPreview = Hook.ModifyDamage(fixture.Run, fixture.Combat, fixture.Enemy, fixture.Player.Creature, second.DynamicVars.Damage.BaseValue,
            ValueProp.Move, second, null, ModifyDamageHookType.All, CardPreviewMode.Normal, out _);
        EngineSelfTests.Equal(7m, firstPreview, "T01 preview includes all combined damage");
        EngineSelfTests.Equal(5m, secondPreview, "T03 preview includes all combined damage");
        EngineSelfTests.Equal(3, DuanYuState.Get(fixture.Player).Qi, "T01 and T03 previews never spend Qi");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SpendingCount, "combined damage preview never registers a spend");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SwordPlays, "damage preview never registers a sword play");
    }

    private static async Task CombinedDamageEnchantment()
    {
        foreach (var id in new[] { "DY_T01", "DY_T03" })
        {
            foreach (var upgraded in new[] { false, true })
            {
                var fixture = await EngineSelfTests.ReadyFixture();
                fixture.Player.PlayerCombatState!.Energy = 1;
                await PaidPlay(fixture, "DY_U14");
                await SetQi(fixture, id == "DY_T01" ? 1 : 2);
                await DuanYuState.AddSwordDamage(fixture.Player, 3);
                await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 2, fixture.Player.Creature, null);
                var card = EngineSelfTests.AddCard(fixture, id, PileType.Hand, upgraded);
                CardCmd.Enchant<Instinct>(card, 1);
                var initialHp = fixture.Enemy.CurrentHp;
                await PaidPlay(fixture, card);
                var expected = id == "DY_T01" ? upgraded ? 27 : 21 : upgraded ? 21 : 17;
                EngineSelfTests.Equal(expected, initialHp - fixture.Enemy.CurrentHp, id + " doubles its combined base then adds Strength, temporary bonus and proficiency once");
                EngineSelfTests.Equal(id == "DY_T01" ? 0 : 1, DuanYuState.Get(fixture.Player).Qi, id + " retains no extra Qi beyond its payment");
                EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).SpendingCount, id + " one hit retains one mandatory spend");
                EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).SwordPlays, id + " combined damage is one sword play");
            }
        }
    }

    private static async Task NextTurn(TestFixture fixture)
    {
        await Hook.BeforeSideTurnStart(fixture.Combat, MegaCrit.Sts2.Core.Combat.CombatSide.Player, [fixture.Player.Creature]);
        await Hook.BeforeHandDraw(fixture.Combat, fixture.Player, new ThrowingPlayerChoiceContext());
    }

    private static async Task SetQi(TestFixture fixture, int amount)
    {
        DuanYuState.Get(fixture.Player).Qi = 0;
        await PowerCmd.Remove<QiPower>(fixture.Player.Creature);
        await DuanYuState.GainQi(fixture.Player, amount);
    }

    private static Task<DuanYuCard> PaidPlay(TestFixture fixture, string id, bool upgraded = false)
        => PaidPlay(fixture, EngineSelfTests.AddCard(fixture, id, PileType.Hand, upgraded));

    internal static async Task<DuanYuCard> PaidPlay(TestFixture fixture, DuanYuCard card)
    {
        if (card.RequiredQi > DuanYuState.Get(fixture.Player).Qi)
            await DuanYuState.GainQi(fixture.Player, card.RequiredQi - DuanYuState.Get(fixture.Player).Qi);
        EngineSelfTests.Equal(true, fixture.Player.Relics.Any(relic => !relic.IsMelted && relic.Id.Entry == "CQBZLY_DY_RELIC01"), "balance fixture includes the native starting pendant");
        EngineSelfTests.Equal(true, PileType.Hand.GetPile(fixture.Player).Cards.Contains(card), card.DesignId + " starts in hand");
        EngineSelfTests.Equal(true, card.CanPlay(), card.DesignId + " has resources before effects");
        var energyValue = card.EnergyCost.GetAmountToSpend();
        var beforeEnergy = fixture.Player.PlayerCombatState!.Energy;
        var (energySpent, starsSpent) = await card.SpendResources();
        EngineSelfTests.Equal(energyValue, energySpent, card.DesignId + " native energy payment");
        EngineSelfTests.Equal(beforeEnergy - energySpent, fixture.Player.PlayerCombatState.Energy, card.DesignId + " payment occurs before effects");
        var target = card.TargetType switch
        {
            TargetType.AnyEnemy => fixture.Enemy,
            TargetType.AnyPlayer => fixture.Player.Creature,
            _ => null
        };
        await card.OnPlayWrapper(new ThrowingPlayerChoiceContext(), target, false,
            new ResourceInfo { EnergySpent = energySpent, EnergyValue = energyValue, StarsSpent = starsSpent, StarValue = 0 }, true);
        return card;
    }
}
#endif
