#if DEBUG
using DuanYu.Cards;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
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
                yield return ("balance:qi-before-pendant" + suffix, () => QiBeforePendant(upgraded));
                yield return ("balance:six-swords-paid" + suffix, () => SixSwordPayment(upgraded));
                yield return ("balance:sword-resource-boundaries" + suffix, () => SwordResourceBoundaries(upgraded));
                yield return ("balance:proficiency-three-swords" + suffix, () => ProficiencyLimit(upgraded));
                yield return ("balance:expanded-attacks-and-original-effects" + suffix, () => ExpandedAttacks(upgraded));
            }
            yield return ("balance:beiming-stacks-and-turn-limit", BeimingLimit);
            yield return ("balance:spend-callback-idempotence", SpendCallbackIdempotence);
            yield return ("balance:unity-third-and-sixth-kind", UnityThresholds);
            yield return ("balance:upgraded-swords-share-kind", UpgradedSwordIdentity);
            yield return ("balance:native-replay-spending-and-first-sword", NativeReplaySpending);
            yield return ("balance:native-replay-unity-thresholds", NativeReplayUnityThresholds);
            yield return ("balance:unity-no-retroactive-trigger", UnityLateApplication);
            yield return ("balance:simplicity-first-sword-limit", SimplicityLimit);
            yield return ("balance:sword-array-mixed-upgrades", SwordArrayMixedUpgrades);
            yield return ("balance:six-sword-resource-cycle", SixSwordCycle);
            yield return ("balance:self-damage-stops-after-death", SelfDamageStopsAfterDeath);
            yield return ("balance:basic-upgrade-values", BasicUpgradeValues);
            yield return ("balance:damage-preview-does-not-spend-qi", DamagePreviewNoQi);
            yield return ("balance:extra-damage-enchantment-applies-per-hit", ExtraDamageEnchantment);
            foreach (var test in RandomCardSelfTests.Cases) yield return test;
        }
    }

    internal static async Task Run()
    {
        foreach (var test in Cases) await test.Action();
    }

    private static async Task ExpandedAttacks(bool upgraded)
    {
        var regular = new[] { ("C", 20), ("U", 35), ("R", 20) }
            .SelectMany(group => Enumerable.Range(1, group.Item2).Select(number => DuanYuCard.Canonical($"DY_{group.Item1}{number:00}"))).ToArray();
        EngineSelfTests.Equal(75, regular.Length, "regular reward catalog size");
        EngineSelfTests.Equal(32, regular.Count(card => card.Type == CardType.Attack), "regular reward catalog includes 32 attacks");
        var specifications = new (string Id, int Damage, int DamageUp, int Block, int BlockUp, int Swords, bool All)[]
        {
            ("DY_C07", 13, 18, 5, 7, 1, false), ("DY_C08", 10, 14, 0, 0, 1, false),
            ("DY_C13", 8, 12, 3, 5, 0, false), ("DY_C16", 12, 17, 6, 8, 0, false),
            ("DY_C18", 10, 15, 5, 7, 1, false), ("DY_U10", 12, 16, 0, 0, 1, true),
            ("DY_U11", 10, 15, 2, 4, 1, false), ("DY_U12", 12, 17, 2, 2, 1, false),
            ("DY_U20", 13, 18, 5, 7, 0, false), ("DY_U21", 12, 17, 0, 0, 0, false),
            ("DY_U22", 15, 22, 0, 0, 0, false), ("DY_R09", 18, 24, 0, 0, 1, true),
            ("DY_R10", 18, 24, 0, 0, 3, false), ("DY_R20", 24, 32, 10, 13, 0, true)
        };
        foreach (var specification in specifications)
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            await SetQi(fixture, 0);
            var secondEnemy = fixture.Combat.CreateCreature(ModelDb.Monster<BigDummy>().ToMutable(), CombatSide.Enemy, null);
            fixture.Combat.AddCreature(secondEnemy);
            var firstHp = fixture.Enemy.CurrentHp;
            var secondHp = secondEnemy.CurrentHp;
            var card = EngineSelfTests.AddCard(fixture, specification.Id, PileType.Hand, upgraded);
            var energyBefore = fixture.Player.PlayerCombatState!.Energy;
            var cost = card.EnergyCost.GetAmountToSpend();
            EngineSelfTests.Equal(CardType.Attack, card.Type, specification.Id + " uses native attack type");
            EngineSelfTests.Equal(specification.All ? TargetType.AllEnemies : TargetType.AnyEnemy, card.TargetType, specification.Id + " native targeting");
            await PaidPlay(fixture, card);
            var damage = upgraded ? specification.DamageUp : specification.Damage;
            EngineSelfTests.Equal(damage, firstHp - fixture.Enemy.CurrentHp, specification.Id + " actual first target damage");
            EngineSelfTests.Equal(specification.All ? damage : 0, secondHp - secondEnemy.CurrentHp, specification.Id + " actual second target damage");
            EngineSelfTests.Equal(upgraded ? specification.BlockUp : specification.Block, fixture.Player.Creature.Block, specification.Id + " retains block effect");
            EngineSelfTests.Equal(specification.Swords, PileType.Hand.GetPile(fixture.Player).Cards.Count(DuanYuState.IsSword), specification.Id + " retains sword generation");
            EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SkillsPlayed, specification.Id + " does not consume first skill trigger");
            EngineSelfTests.Equal(energyBefore - cost, fixture.Player.PlayerCombatState.Energy, specification.Id + " does not receive first skill energy");
        }
    }

    private static async Task EnergyCards(bool upgraded)
    {
        foreach (var id in new[] { "DY_U02", "DY_U04", "DY_R04" })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            await SetQi(fixture, 0);
            var card = EngineSelfTests.AddCard(fixture, id, PileType.Hand, upgraded);
            var paid = id == "DY_U02" || id == "DY_R04" && !upgraded ? 1 : 0;
            fixture.Player.PlayerCombatState!.Energy = paid;
            var health = fixture.Player.Creature.CurrentHp;
            await PaidPlay(fixture, card);
            var energy = id switch { "DY_U04" => 2, "DY_R04" when upgraded => 4, _ => 3 };
            EngineSelfTests.Equal(energy, fixture.Player.PlayerCombatState.Energy, id + " energy after actual payment and first skill pendant");
            var qi = id switch { "DY_U02" => upgraded ? 8 : 6, "DY_U04" => upgraded ? 6 : 5, _ => upgraded ? 9 : 8 };
            EngineSelfTests.Equal(qi, DuanYuState.Get(fixture.Player).Qi, id + " Qi plus first skill pendant");
            EngineSelfTests.Equal(id == "DY_U04" ? upgraded ? 2 : 3 : 0, health - fixture.Player.Creature.CurrentHp, id + " HP payment");
            EngineSelfTests.Equal(true, PileType.Exhaust.GetPile(fixture.Player).Cards.Contains(card), id + " resource card exhausts");
        }
        var empty = await EngineSelfTests.ReadyFixture();
        empty.Player.PlayerCombatState!.Energy = 0;
        var requiresPayment = EngineSelfTests.AddCard(empty, "DY_U02", PileType.Hand, upgraded);
        EngineSelfTests.Equal(false, requiresPayment.CanPlay(), "Qi Sea cannot borrow its future energy");
    }

    private static async Task QiBeforePendant(bool upgraded)
    {
        foreach (var qi in new[] { 2, 3 })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            await SetQi(fixture, qi);
            fixture.Player.PlayerCombatState!.Energy = 1;
            await PaidPlay(fixture, "DY_C14", upgraded);
            EngineSelfTests.Equal(qi == 3 ? 2 : 1, fixture.Player.PlayerCombatState.Energy, "read book and first skill pendant have separate energy grants");
            EngineSelfTests.Equal(qi == 3 ? 2 : 4, DuanYuState.Get(fixture.Player).Qi, "pendant cannot fund the current spend");
            EngineSelfTests.Equal(upgraded ? 3 : 2, PileType.Hand.GetPile(fixture.Player).Cards.Count, "read book native draw");
        }
    }

    private static async Task SixSwordPayment(bool upgraded)
    {
        foreach (var qi in new[] { 2, 3 })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            await SetQi(fixture, qi);
            fixture.Player.PlayerCombatState!.Energy = 2;
            await PaidPlay(fixture, "DY_R03", upgraded);
            var swords = PileType.Hand.GetPile(fixture.Player).Cards.Where(DuanYuState.IsSword).ToArray();
            EngineSelfTests.Equal(1, fixture.Player.PlayerCombatState.Energy, "pendant refunds energy after six swords payment");
            EngineSelfTests.Equal(upgraded ? 14 : 10, fixture.Player.Creature.Block, "six swords immediate block");
            EngineSelfTests.Equal(qi == 3 ? 6 : 0, swords.Length, "six swords requires three Qi before generation");
            EngineSelfTests.Equal(qi == 3 ? 6 : 0, swords.Select(sword => sword.Id).Distinct().Count(), "six swords are distinct");
            EngineSelfTests.Equal(true, swords.All(sword => sword.IsUpgraded == upgraded), "six swords tokens preserve the generator upgrade");
            EngineSelfTests.Equal(qi == 3 ? 2 : 4, DuanYuState.Get(fixture.Player).Qi, "six sword generation spends its available Qi");
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
                    "DY_T01" => upgraded ? 20 : 14,
                    "DY_T02" => upgraded ? 9 : 6,
                    "DY_T03" => upgraded ? 15 : 10,
                    "DY_T04" => upgraded ? 9 : 6,
                    "DY_T05" => upgraded ? 6 : 4,
                    _ => upgraded ? 8 : 5
                };
                EngineSelfTests.Equal(damage, health - fixture.Enemy.CurrentHp, id + " funded damage");
                EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, id + " pays one Qi");
                EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, id + " grants no energy");
                EngineSelfTests.Equal(id == "DY_T04" ? (upgraded ? 8 : 6) : 0, fixture.Player.Creature.Block, id + " conditional block");
                EngineSelfTests.Equal(id == "DY_T05" ? 1 : 0, PileType.Hand.GetPile(fixture.Player).Cards.Count, id + " native draw");
                EngineSelfTests.Equal(id == "DY_T02" ? upgraded ? 2 : 1 : 0, fixture.Enemy.GetPowerAmount<VulnerablePower>(), id + " vulnerable");
                EngineSelfTests.Equal(id == "DY_T06" ? upgraded ? 2 : 1 : 0, fixture.Enemy.GetPowerAmount<WeakPower>(), id + " weak");
                EngineSelfTests.Equal(true, sword.Keywords.Contains(CardKeyword.Ethereal) && PileType.Exhaust.GetPile(fixture.Player).Cards.Contains(sword), id + " bounded token lifecycle");
            }
        }
    }

    private static async Task ProficiencyLimit(bool upgraded)
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 1;
        await PaidPlay(fixture, "DY_U14", upgraded);
        for (var ordinal = 1; ordinal <= 4; ordinal++)
        {
            await SetQi(fixture, 0);
            var health = fixture.Enemy.CurrentHp;
            await PaidPlay(fixture, "DY_T01");
            EngineSelfTests.Equal(true, health - fixture.Enemy.CurrentHp >= 10, "proficiency preserves sword base damage after balancing");
        }
    }

    private static async Task BeimingLimit()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 8;
        await PaidPlay(fixture, "DY_R01");
        await PaidPlay(fixture, "DY_R01");
        await SetQi(fixture, 3);
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(8, fixture.Player.PlayerCombatState.Energy, "two Beiming stacks refund energy once");
        EngineSelfTests.Equal(6, DuanYuState.Get(fixture.Player).Qi, "two Beiming stacks refund first spend Qi");
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(8, fixture.Player.PlayerCombatState.Energy, "second spend gives no energy");
        EngineSelfTests.Equal(9, DuanYuState.Get(fixture.Player).Qi, "second spend refunds Qi up to the cap");
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(8, DuanYuState.Get(fixture.Player).Qi, "third spend gives no refund");
        await PaidPlay(fixture, "DY_R01");
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(7, fixture.Player.PlayerCombatState.Energy, "late Beiming does not reset energy ordinal");
        EngineSelfTests.Equal(7, DuanYuState.Get(fixture.Player).Qi, "late Beiming does not reset Qi ordinal");
        await NextTurn(fixture);
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(10, fixture.Player.PlayerCombatState.Energy, "three Beiming stacks refresh next turn");
        EngineSelfTests.Equal(9, DuanYuState.Get(fixture.Player).Qi, "next turn first spend refunds six Qi up to the cap");
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
        EngineSelfTests.Equal(4, DuanYuState.Get(fixture.Player).Qi, "first callback refunds Qi");
        EngineSelfTests.Equal(2, fixture.Player.PlayerCombatState.Energy, "first callback refunds energy");
        EngineSelfTests.Equal(2, PileType.Hand.GetPile(fixture.Player).Cards.Count, "first callback draws once");
        await DuanYuState.AfterQiSpent(fixture.Player, context, source);
        await DuanYuState.AfterCard(play, context);
        await DuanYuState.AfterQiSpent(fixture.Player, context, source);
        EngineSelfTests.Equal(4, DuanYuState.Get(fixture.Player).Qi, "duplicate active and completed callbacks cannot refund Qi");
        EngineSelfTests.Equal(2, fixture.Player.PlayerCombatState.Energy, "duplicate callbacks cannot refund energy");
        EngineSelfTests.Equal(2, PileType.Hand.GetPile(fixture.Player).Cards.Count, "duplicate callbacks cannot draw again");
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
            EngineSelfTests.Equal(3 + thresholds * 2 + (index >= 6 ? 1 : 0), fixture.Player.PlayerCombatState.Energy, "unity and pendant give energy at each eligible threshold only");
            EngineSelfTests.Equal(thresholds * 3 + (index >= 2 ? 1 : 0) + (index >= 5 ? 1 : 0), PileType.Hand.GetPile(fixture.Player).Cards.Count, "mixed unity stacks and pendant draw exact amounts");
            var qi = index == 2 ? 6 : index == 6 ? 7 : 0;
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
            EngineSelfTests.Equal(thresholds + (index >= 7 ? 1 : 0), fixture.Player.PlayerCombatState.Energy, "upgrading a repeated sword cannot create a unity or pendant energy trigger");
            EngineSelfTests.Equal(thresholds + (index >= 3 ? 1 : 0) + (index >= 6 ? 1 : 0), PileType.Hand.GetPile(fixture.Player).Cards.Count, "upgraded duplicate swords cannot repeat unity or pendant draws");
            EngineSelfTests.Equal(index == 3 ? 4 : index == 7 ? 5 : 0, DuanYuState.Get(fixture.Player).Qi, "upgraded duplicate swords cannot repeat unity or pendant Qi");
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
        EngineSelfTests.Equal(48, health - fixture.Enemy.CurrentHp, "native replay executes three separately funded sword bodies with increasing momentum");
        EngineSelfTests.Equal(3, DuanYuState.Get(fixture.Player).SwordMomentum, "each replay adds momentum after both damage segments");
        EngineSelfTests.Equal(3, DuanYuState.Get(fixture.Player).SpendingCount, "native replay pays Qi separately for each play");
        EngineSelfTests.Equal(2, DuanYuState.Get(fixture.Player).Qi, "only the first two replay spends receive Beiming refunds");
        EngineSelfTests.Equal(4, fixture.Player.PlayerCombatState.Energy, "Beiming first spend and simplicity first sword each refund energy once per series");
        EngineSelfTests.Equal(1, PileType.Hand.GetPile(fixture.Player).Cards.Count, "beginning Beiming draws only on the first replay spend");
        EngineSelfTests.Equal(3, DuanYuState.Get(fixture.Player).SwordPlays, "each native replay advances the sword play ordinal");
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).DistinctSwords.Count, "native replay cannot introduce a new sword identity");
        await SetQi(fixture, 0);
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(4, DuanYuState.Get(fixture.Player).SpendingCount, "unfunded follow-up cannot borrow a replay refund");
        EngineSelfTests.Equal(4, fixture.Player.PlayerCombatState.Energy, "follow-up sword cannot repeat either first-play energy trigger");
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
        EngineSelfTests.Equal(67, health - fixture.Enemy.CurrentHp, "third-kind rewards fund later replay bodies and momentum increases between replays");
        EngineSelfTests.Equal(6, DuanYuState.Get(fixture.Player).SwordMomentum, "third-kind replay grants its extra momentum only once");
        EngineSelfTests.Equal(1, fixture.Player.PlayerCombatState.Energy, "third-kind native replay grants unity energy exactly once");
        EngineSelfTests.Equal(2, PileType.Hand.GetPile(fixture.Player).Cards.Count, "third-kind native replay grants unity and pendant draw once each");
        EngineSelfTests.Equal(2, DuanYuState.Get(fixture.Player).Qi, "third-kind native replay retains post-resolution Qi");
        EngineSelfTests.Equal(5, DuanYuState.Get(fixture.Player).SpendingCount, "third-kind replay cannot borrow Qi before the threshold resolves");
        await PaidPlay(fixture, "DY_T02");
        await PaidPlay(fixture, "DY_T05");
        await SetQi(fixture, 0);
        var sixthKind = EngineSelfTests.AddCard(fixture, "DY_T06", PileType.Hand, true);
        sixthKind.BaseReplayCount = 2;
        await PaidPlay(fixture, sixthKind);
        AssertNativeReplaySeries(fixture, sixthKind, 3);
        EngineSelfTests.Equal(3, fixture.Player.PlayerCombatState.Energy, "sixth-kind replay adds only available energy grants");
        EngineSelfTests.Equal(4, PileType.Hand.GetPile(fixture.Player).Cards.Count, "each unity threshold plus pendant and the fifth sword draw once");
        EngineSelfTests.Equal(3, DuanYuState.Get(fixture.Player).Qi, "sixth-kind replay leaves three Qi after two separate one-Qi payments");
        EngineSelfTests.Equal(10, DuanYuState.Get(fixture.Player).SpendingCount, "each threshold series records all funded replay spends");
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
        EngineSelfTests.Equal(2, fixture.Player.PlayerCombatState.Energy, "late unity and pendant trigger the remaining sixth-kind threshold");
        await NextTurn(fixture);
        foreach (var id in new[] { "DY_T01", "DY_T02", "DY_T03" })
        {
            await SetQi(fixture, 0);
            await PaidPlay(fixture, id);
        }
        EngineSelfTests.Equal(3, fixture.Player.PlayerCombatState.Energy, "unity threshold refreshes next turn");
    }

    private static async Task SimplicityLimit()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 6;
        await PaidPlay(fixture, "DY_R15", true);
        await PaidPlay(fixture, "DY_R15", true);
        await SetQi(fixture, 0);
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(8, fixture.Player.PlayerCombatState.Energy, "two simplicity stacks refund first sword");
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(8, fixture.Player.PlayerCombatState.Energy, "second sword gives no simplicity energy");
        await PaidPlay(fixture, "DY_R15", true);
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(8, fixture.Player.PlayerCombatState.Energy, "late simplicity does not reset first-sword ordinal");
        await DuanYuState.EndTurn(fixture.Player);
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "simplicity no longer returns Qi at turn end");
        await NextTurn(fixture);
        EngineSelfTests.Equal(9, DuanYuState.Get(fixture.Player).Qi, "simplicity grants three Qi per stack at turn start");
        await SetQi(fixture, 0);
        await PaidPlay(fixture, "DY_T01");
        EngineSelfTests.Equal(11, fixture.Player.PlayerCombatState.Energy, "simplicity first-sword trigger refreshes next turn");
    }

    private static async Task SwordArrayMixedUpgrades()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 4;
        await PaidPlay(fixture, "DY_R07");
        await PaidPlay(fixture, "DY_R07", true);
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
        fixture.Player.PlayerCombatState!.Energy = 1;
        await SetQi(fixture, 0);
        await PaidPlay(fixture, "DY_U02");
        await PaidPlay(fixture, "DY_R03", true);
        foreach (var id in new[] { "DY_T05", "DY_T02", "DY_T06", "DY_T01", "DY_T03", "DY_T04" })
        {
            var sword = PileType.Hand.GetPile(fixture.Player).Cards.OfType<DuanYuCard>().Single(card => card.DesignId == id);
            await PaidPlay(fixture, sword);
        }
        EngineSelfTests.Equal(2, fixture.Player.PlayerCombatState.Energy, "one starting energy expands into a paid six-sword cycle");
        EngineSelfTests.Equal(3, DuanYuState.Get(fixture.Player).Qi, "cycle preserves only available resource gains");
        EngineSelfTests.Equal(7, DuanYuState.Get(fixture.Player).SpendingCount, "cycle has exactly seven successful spends");
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
        EngineSelfTests.Equal(8, blockFixture.Player.Creature.Block, "upgraded B03 block");
        EngineSelfTests.Equal(6, DuanYuState.Get(blockFixture.Player).Qi, "upgraded B03 Qi plus starter relic");

        var attackFixture = await EngineSelfTests.ReadyFixture();
        attackFixture.Player.PlayerCombatState!.Energy = 1;
        await SetQi(attackFixture, 2);
        var initialHp = attackFixture.Enemy.CurrentHp;
        await PaidPlay(attackFixture, "DY_B05", true);
        EngineSelfTests.Equal(36, initialHp - attackFixture.Enemy.CurrentHp, "upgraded B05 two paid segments");
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
        EngineSelfTests.Equal(10m, firstPreview, "T01 preview damage");
        EngineSelfTests.Equal(6m, secondPreview, "T03 preview damage");
        EngineSelfTests.Equal(3, DuanYuState.Get(fixture.Player).Qi, "T01 and T03 previews never spend Qi");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SpendingCount, "extra damage preview never registers a spend");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SwordPlays, "damage preview never registers a sword play");
    }

    private static async Task ExtraDamageEnchantment()
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
                var expected = id == "DY_T01" ? upgraded ? 56 : 44 : upgraded ? 46 : 36;
                EngineSelfTests.Equal(expected, initialHp - fixture.Enemy.CurrentHp, id + " each segment doubles its base then adds Strength, temporary bonus and proficiency");
                EngineSelfTests.Equal(id == "DY_T01" ? 0 : 1, DuanYuState.Get(fixture.Player).Qi, id + " retains no extra Qi beyond its payment");
                EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).SpendingCount, id + " two hits are one spend");
                EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).SwordPlays, id + " two hits are one sword play");
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
        var energyValue = card.EnergyCost.GetWithModifiers(CostModifiers.All);
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
