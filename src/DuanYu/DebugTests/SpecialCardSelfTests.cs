#if DEBUG
using DuanYu.Cards;
using DuanYu.Character;
using DuanYu.Relics;
using DuanYu.Runtime;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;

namespace DuanYu.DebugTests;

internal static class SpecialCardSelfTests
{
    private static readonly (Type Type, int Damage, int UpgradedDamage)[] NativeAttacks =
    [
        (typeof(IronWave), 5, 7), (typeof(PommelStrike), 9, 10), (typeof(TwinStrike), 10, 14),
        (typeof(Bludgeon), 32, 42),
        (typeof(DaggerSpray), 8, 12), (typeof(DaggerThrow), 9, 12), (typeof(Slice), 6, 9), (typeof(PoisonedStab), 6, 8),
        (typeof(BeamCell), 3, 4), (typeof(GoForTheEyes), 3, 4), (typeof(SweepingBeam), 6, 9), (typeof(RipAndTear), 14, 18),
        (typeof(KinglyKick), 27, 35), (typeof(KinglyPunch), 8, 10),
        (typeof(DeathMarch), 8, 9), (typeof(Defile), 13, 17), (typeof(Graveblast), 4, 6),
        (typeof(Eradicate), 11, 14),
        (typeof(SorrowfulStrike), 13, 17), (typeof(BowSmack), 8, 11), (typeof(PunchOfJoy), 13, 17), (typeof(KiWhip), 5, 6)
    ];

    private static readonly HashSet<Type> XCostAttacks = [typeof(Eradicate)];

    internal static IEnumerable<(string Name, Func<Task> Action)> Cases
    {
        get
        {
            yield return ("special:status-models-and-native-rewards", StatusModels);
            yield return ("special:qi-loss-does-not-spend-or-refund", QiLoss);
            yield return ("special:status-native-turn-end-and-ethereal", StatusTurnEnd);
            yield return ("special:status-paid-effect-without-skill-or-sword", StatusPaidEffect);
            yield return ("special:status-fuel-draws-extra-card", StatusFuelDraw);
            yield return ("special:discard-status-retrieval", DiscardStatusRetrieval);
            yield return ("special:foreign-fixed-seed-reproducible", ForeignReproducible);
            yield return ("special:inactive-combat-stops-generation-and-qi-loss", InactiveCombat);
            yield return ("special:exhaust-power-stacks-and-no-passive-end-turn", ExhaustPower);
            yield return ("special:exhaust-power-owner-and-lifetime", ExhaustPowerLifetime);
            foreach (var upgraded in new[] { false, true })
            {
                var suffix = upgraded ? "+" : "";
                yield return ("special:attack-replay-creates-one-status-per-body" + suffix, () => AttackCreatesStatus(upgraded));
                yield return ("special:foreign-complete-pool-cost-and-overflow" + suffix, () => ForeignGeneration(upgraded));
                yield return ("special:borrowed-attacks-paid-and-status-cost" + suffix, () => BorrowedAttacks(upgraded));
                yield return ("special:secondary-borrow-card" + suffix, () => SecondaryBorrowedAttack(upgraded));
                yield return ("special:x-resource-cards" + suffix, () => XResourceCards(upgraded));
                yield return ("special:active-exhaust-fixed-draw" + suffix, () => ActiveExhaust(upgraded));
                yield return ("special:selective-exhaust-actual-count" + suffix, () => SelectiveExhaust(upgraded));
                yield return ("special:stoke-hand-into-random-swords" + suffix, () => StokeHand(upgraded));
                yield return ("special:cleanse-native-wither-and-custom-statuses" + suffix, () => CleanseStatuses(upgraded));
                foreach (var attack in NativeAttacks)
                    yield return ("special:foreign-native-play-" + attack.Type.Name + suffix, () => PlayForeignAttack(attack.Type, upgraded ? attack.UpgradedDamage : attack.Damage, upgraded));
            }
        }
    }

    private static async Task StatusModels()
    {
        var fixture = await ReadyFixture();
        foreach (var id in new[] { "DY_S01", "DY_S02" })
        {
            var card = EngineSelfTests.AddCard(fixture, id, PileType.Hand);
            EngineSelfTests.Equal(CardType.Status, card.Type, id + " is a native Status");
            EngineSelfTests.Equal(CardRarity.Status, card.Rarity, id + " has Status rarity");
            EngineSelfTests.Equal(false, card.IsUpgradable, id + " cannot be upgraded");
            EngineSelfTests.Equal(0, card.MaxUpgradeLevel, id + " has no upgrade tier");
            EngineSelfTests.Equal(false, DuanYuState.IsSword(card), id + " is not a sword kind");
        }
        var stagnation = PileType.Hand.GetPile(fixture.Player).Cards.Single(card => card is DY_S01);
        EngineSelfTests.Equal(false, stagnation.CanPlay(), "Qi stagnation is unplayable despite available energy");
        EngineSelfTests.Equal(true, stagnation.Keywords.Contains(CardKeyword.Ethereal), "Qi stagnation is ethereal");
        var foreignQi = PileType.Hand.GetPile(fixture.Player).Cards.Single(card => card is DY_S02);
        EngineSelfTests.Equal(true, foreignQi.Keywords.Contains(CardKeyword.Exhaust), "foreign Qi exhausts when played");
        foreach (var room in new[] { RoomType.Monster, RoomType.Elite, RoomType.Boss, RoomType.Shop })
        {
            var rewards = CardFactory.CreateForReward(fixture.Player, 3, CardCreationOptions.ForRoom(fixture.Player, room)).ToArray();
            EngineSelfTests.Equal(true, rewards.All(reward => reward.Card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare), "ordinary native " + room + " rewards exclude Status rarity");
        }
    }

    private static async Task QiLoss()
    {
        var fixture = await ReadyFixture();
        await RelicCmd.Obtain<TianlongPrayerBeads>(fixture.Player);
        await RelicCmd.Obtain<DaliSeal>(fixture.Player);
        await DuanYuState.AddEffect(fixture.Player, "DY_U01");
        await DuanYuState.AddEffect(fixture.Player, "DY_R01", 2, energyAmount: 1);
        await SetQi(fixture, 3);
        fixture.Player.PlayerCombatState!.Energy = 0;
        await DuanYuState.LoseQi(fixture.Player, 2);
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).Qi, "Qi loss subtracts exactly two");
        EngineSelfTests.Equal(1, fixture.Player.Creature.GetPowerAmount<QiPower>(), "Qi loss synchronizes displayed power");
        await DuanYuState.LoseQi(fixture.Player, int.MaxValue);
        await DuanYuState.LoseQi(fixture.Player, -1);
        await DuanYuState.AfterQiSpent(fixture.Player, new ThrowingPlayerChoiceContext());
        var state = DuanYuState.Get(fixture.Player);
        EngineSelfTests.Equal(0, state.Qi, "large Qi loss floors at zero and negative loss is ignored");
        EngineSelfTests.Equal(0, fixture.Player.Creature.GetPowerAmount<QiPower>(), "zero Qi removes its power display");
        EngineSelfTests.Equal(0, state.SpendingCount, "Qi loss is not a successful spend");
        EngineSelfTests.Equal(false, state.SealUsed, "Qi loss does not consume the seal refund");
        EngineSelfTests.Equal(false, state.PrayerBeadsUsedThisTurn, "Qi loss does not consume the prayer bead trigger");
        EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, "Qi loss grants no refund energy");
        EngineSelfTests.Equal(0, fixture.Player.Creature.Block, "Qi loss grants no refund block");
        EngineSelfTests.Equal(0, PileType.Hand.GetPile(fixture.Player).Cards.Count, "Qi loss grants no spend-triggered draw");
        await DuanYuState.GainQi(fixture.Player, 1);
        fixture.Player.PlayerCombatState.Stars = 4;
        EngineSelfTests.Equal(4, fixture.Player.PlayerCombatState.Stars, "native star resource remains independent from Qi");
        EngineSelfTests.Equal(true, await DuanYuState.SpendQi(fixture.Player, 1), "a later real spend remains payable");
        EngineSelfTests.Equal(4, fixture.Player.PlayerCombatState.Stars, "spending Qi does not change native star resource");
        await DuanYuState.AfterQiSpent(fixture.Player, new ThrowingPlayerChoiceContext());
        EngineSelfTests.Equal(0, state.Qi, "first actual spend receives no second-payment Beiming refund");
        EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, "first actual spend receives no second-payment energy reward");
        EngineSelfTests.Equal(1, PileType.Hand.GetPile(fixture.Player).Cards.Count, "first actual spend retains its draw reward");
    }

    private static async Task StatusTurnEnd()
    {
        foreach (var startingQi in new[] { 1, 3 })
        {
            var fixture = await ReadyFixture();
            await SetQi(fixture, startingQi);
            await DuanYuState.AddEffect(fixture.Player, "DY_U28", 3);
            var status = EngineSelfTests.AddCard(fixture, "DY_S01", PileType.Hand);
            CardCmd.ApplySingleTurnRetain(status);
            await NativeTurnEnd(fixture);
            EngineSelfTests.Equal(Math.Max(0, startingQi - 2) + 1, DuanYuState.Get(fixture.Player).Qi, "native turn-end resolves Qi loss before status-fuel recovery");
            EngineSelfTests.Equal(PileType.Exhaust, status.Pile!.Type, "native ethereal exhausts the retained status");
            EngineSelfTests.Equal(1, ExhaustCount(status), "turn-end status exhausts exactly once");
            EngineSelfTests.Equal(3, fixture.Player.Creature.Block, "ethereal status activates exhaust block once");
            EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SpendingCount, "turn-end Qi loss never becomes a spend");
        }
    }

    private static async Task StatusPaidEffect()
    {
        var fixture = await ReadyFixture();
        await SetQi(fixture, 0);
        fixture.Player.PlayerCombatState!.Energy = 0;
        var status = EngineSelfTests.AddCard(fixture, "DY_S02", PileType.Hand);
        EngineSelfTests.Equal(false, status.CanPlay(), "foreign Qi requires one actual energy");
        fixture.Player.PlayerCombatState.Energy = 1;
        await PaidPlay(fixture, status);
        var state = DuanYuState.Get(fixture.Player);
        EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, "status pays energy and receives no first-skill refund");
        EngineSelfTests.Equal(2, state.Qi, "foreign Qi grants exactly two Qi");
        EngineSelfTests.Equal(1, state.SwordMomentum, "foreign Qi grants exactly one momentum");
        EngineSelfTests.Equal(1, PileType.Hand.GetPile(fixture.Player).Cards.Count, "foreign Qi draws one card");
        EngineSelfTests.Equal(0, state.SkillsPlayed, "status does not consume the first-skill ordinal");
        EngineSelfTests.Equal(0, state.SwordPlays, "status is not a sword play");
        EngineSelfTests.Equal(0, state.DistinctSwords.Count, "status does not unlock a sword threshold");
        EngineSelfTests.Equal(PileType.Exhaust, status.Pile!.Type, "foreign Qi exhausts after play");
        fixture.Player.PlayerCombatState.Energy = 1;
        await PaidPlay(fixture, EngineSelfTests.AddCard(fixture, "DY_B02", PileType.Hand));
        EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, "later first skill receives no starter relic energy");
        EngineSelfTests.Equal(2, state.Qi, "later first skill receives no starter relic Qi");
    }

    private static async Task StatusFuelDraw()
    {
        var fixture = await ReadyFixture();
        await SetQi(fixture, 0);
        await DuanYuState.AddEffect(fixture.Player, "DY_U28", 3);
        var status = EngineSelfTests.AddCard(fixture, "DY_S01", PileType.Hand);
        var card = EngineSelfTests.AddCard(fixture, "DY_U06", PileType.Hand);
        using var scope = RunManager.Instance.CardSelectorStack.PushSelector(fixture.Player, new CountSelector(1));
        await PaidPlay(fixture, card);
        EngineSelfTests.Equal(PileType.Exhaust, status.Pile!.Type, "meditation can consume a custom status");
        EngineSelfTests.Equal(3, PileType.Hand.GetPile(fixture.Player).Cards.Count, "meditation draws its base cards plus one status-fuel card");
        EngineSelfTests.Equal(2, DuanYuState.Get(fixture.Player).Qi, "meditation's status bonus stacks with the power's custom-status bonus");
    }

    private static async Task DiscardStatusRetrieval()
    {
        var fixture = await ReadyFixture();
        await SetQi(fixture, 0);
        var status = EngineSelfTests.AddCard(fixture, "DY_S01", PileType.Discard);
        var card = EngineSelfTests.AddCard(fixture, "DY_C21", PileType.Hand);
        var fuel = EngineSelfTests.AddCard(fixture, "DY_B01", PileType.Hand);
        using var scope = RunManager.Instance.CardSelectorStack.PushSelector(fixture.Player, new CountSelector(1));
        await PaidPlay(fixture, card);
        EngineSelfTests.Equal(PileType.Hand, status.Pile!.Type, "recovery card returns a status from discard");
        EngineSelfTests.Equal(PileType.Discard, card.Pile!.Type, "recovery card remains reusable");
        EngineSelfTests.Equal(PileType.Exhaust, fuel.Pile!.Type, "recovery card exhausts the selected hand fuel only");
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).Qi, "recovery card gains only its printed Qi");
    }

    private static async Task AttackCreatesStatus(bool upgraded)
    {
        var fixture = await ReadyFixture();
        await SetQi(fixture, 0);
        var attack = EngineSelfTests.AddCard(fixture, "DY_U08", PileType.Hand, upgraded);
        attack.BaseReplayCount = 2;
        var initialHp = fixture.Enemy.CurrentHp;
        await PaidPlay(fixture, attack);
        var statuses = GeneratedCards().OfType<DY_S01>().ToArray();
        EngineSelfTests.Equal(3 * 16, initialHp - fixture.Enemy.CurrentHp, "replayed attack at zero Qi deals 16 damage for every body");
        EngineSelfTests.Equal(3, statuses.Length, "every completed attack body creates one stagnation");
        EngineSelfTests.Equal(true, statuses.All(status => status.Pile?.Type == PileType.Discard), "attack statuses enter discard");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "creating stagnation does not immediately lose Qi");
    }

    private static async Task ForeignGeneration(bool upgraded)
    {
        var fixture = await ReadyFixture();
        var characters = ModelDb.AllCharacters.Where(character => character is not DuanYuCharacter).ToArray();
        var characterPools = characters.Select(character => character.CardPool.Id).ToHashSet();
        var templates = characters.SelectMany(character => character.CardPool.AllCards.Concat(character.StartingDeck))
            .Concat(ModelDb.AllCards.Where(card => card.Type == CardType.Attack && characterPools.Contains(card.VisualCardPool.Id)))
            .Where(card => card.Type == CardType.Attack && card.Rarity != CardRarity.Basic && card is not DuanYuCard)
            .DistinctBy(card => card.Id).ToArray();
        var originals = templates.ToDictionary(card => card.Id, TemplateSnapshot);
        var rngBefore = fixture.Run.Rng.CombatCardGeneration.ToSerializable();
        await DuanYuForeignCards.GenerateAttacks(fixture.Player, new ThrowingPlayerChoiceContext(), 0, upgraded);
        await DuanYuForeignCards.GenerateAttacks(fixture.Player, new ThrowingPlayerChoiceContext(), -1, upgraded);
        EngineSelfTests.Equal(rngBefore, fixture.Run.Rng.CombatCardGeneration.ToSerializable(), "nonpositive generation count consumes no RNG");
        await DuanYuForeignCards.GenerateAttacks(fixture.Player, new ThrowingPlayerChoiceContext(), int.MaxValue, upgraded);
        var cards = GeneratedCards();
        EngineSelfTests.Equal(templates.Length, cards.Length, "foreign generation reaches every non-basic attack in all other complete character pools");
        EngineSelfTests.Equal(templates.Length, cards.Select(card => card.Id).Distinct().Count(), "foreign generation samples without replacement");
        EngineSelfTests.Equal(true, templates.Select(card => card.Id).ToHashSet().SetEquals(cards.Select(card => card.Id)), "generated attacks cover all other character non-basic attack models without further exclusions");
        EngineSelfTests.Equal(true, cards.All(card => card is not DuanYuCard && card.Type == CardType.Attack), "foreign cards use real native implementations");
        EngineSelfTests.Equal(true, cards.All(card => card.Rarity != CardRarity.Basic), "borrowed attacks never include Basic-rarity attacks");
        EngineSelfTests.Equal(true, cards.All(card => !card.IsCanonical && card.Owner == fixture.Player && card.IsUpgraded == upgraded), "only owned generated copies receive upgrades");
        EngineSelfTests.Equal(true, cards.All(card => card.Keywords.Contains(CardKeyword.Exhaust) && (card.EnergyCost.CostsX || card.EnergyCost.GetWithModifiers(CostModifiers.All) == 0)), "generated copies receive temporary zero energy cost and exhaust, with native X costs preserved");
        EngineSelfTests.Equal(templates.Count(card => card.EnergyCost.CostsX), cards.Count(card => card.EnergyCost.CostsX), "all X-cost native attacks retain their native X-cost semantics");
        EngineSelfTests.Equal(10, cards.Count(card => card.Pile?.Type == PileType.Hand), "native hand capacity remains ten");
        EngineSelfTests.Equal(templates.Length - 10, cards.Count(card => card.Pile?.Type == PileType.Discard), "foreign overflow goes to native discard");
        EngineSelfTests.Equal(templates.Length, fixture.Run.Rng.CombatCardGeneration.ToSerializable().counter - rngBefore.counter, "complete-pool sampling consumes one native draw per card");
        foreach (var card in cards)
            EngineSelfTests.Equal(true, !string.IsNullOrWhiteSpace(card.GetDescriptionForPile(card.Pile!.Type)), card.Id.Entry + " keeps a renderable native card description in the complete pool");
        fixture.Player.PlayerCombatState!.EndOfTurnCleanup();
        foreach (var card in cards)
        {
            var nativeCopy = fixture.Combat.CreateCard(templates.Single(template => template.Id == card.Id), fixture.Player);
            if (upgraded) CardCmd.Upgrade(nativeCopy);
            EngineSelfTests.Equal(nativeCopy.EnergyCost.GetWithModifiers(CostModifiers.All), card.EnergyCost.GetWithModifiers(CostModifiers.All), "turn cleanup restores the native upgraded cost and keeps native discounts");
            EngineSelfTests.Equal(nativeCopy.CurrentStarCost, card.CurrentStarCost, "borrowing never waives or replaces native star costs");
        }
        foreach (var template in templates)
            EngineSelfTests.Equal(originals[template.Id], TemplateSnapshot(template), "foreign generation leaves native canonical models unchanged");
    }

    private static async Task ForeignReproducible()
    {
        async Task<string> GenerateSequence()
        {
            var fixture = await ReadyFixture("DUANYUFOREIGNREPEAT");
            await DuanYuForeignCards.GenerateAttacks(fixture.Player, new ThrowingPlayerChoiceContext(), 2, false);
            await DuanYuForeignCards.GenerateAttacks(fixture.Player, new ThrowingPlayerChoiceContext(), 2, true);
            return string.Join(",", GeneratedCards().Select(card => card.Id.Entry + ":" + card.CurrentUpgradeLevel)) + ":" + fixture.Run.Rng.CombatCardGeneration.ToSerializable();
        }
        EngineSelfTests.Equal(await GenerateSequence(), await GenerateSequence(), "same native seed reproduces both borrowed pairs and RNG state");
    }

    private static async Task BorrowedAttacks(bool upgraded)
    {
        var fixture = await ReadyFixture();
        await SetQi(fixture, 0);
        fixture.Player.PlayerCombatState!.Energy = 1;
        var borrowing = EngineSelfTests.AddCard(fixture, "DY_U27", PileType.Hand, upgraded);
        EngineSelfTests.Equal(false, borrowing.GetDescriptionForPile(PileType.Hand).Contains("具有消耗", StringComparison.Ordinal), "the source card does not advertise added Exhaust on borrowed attacks");
        await PaidPlay(fixture, borrowing);
        var generated = GeneratedCards();
        var attacks = generated.Where(card => card.Type == CardType.Attack).ToArray();
        EngineSelfTests.Equal(2, attacks.Length, "borrowing produces exactly two native attacks");
        EngineSelfTests.Equal(2, attacks.Select(card => card.Id).Distinct().Count(), "borrowed pair has different attacks");
        EngineSelfTests.Equal(true, attacks.All(card => card.IsUpgraded == upgraded && card.Pile?.Type == PileType.Hand), "borrowing preserves generated upgrade tier");
        foreach (var attack in attacks)
        {
            var nativeCopy = fixture.Combat.CreateCard(attack.CanonicalInstance, fixture.Player);
            if (upgraded) CardCmd.Upgrade(nativeCopy);
            EngineSelfTests.Equal(nativeCopy.Keywords.Contains(CardKeyword.Exhaust), attack.Keywords.Contains(CardKeyword.Exhaust),
                "Beiming Borrow preserves the native Exhaust keyword without adding it");
        }
        EngineSelfTests.Equal(1, generated.OfType<DY_S02>().Count(status => status.Pile?.Type == PileType.Discard), "borrowing adds one foreign-Qi status to discard");
        EngineSelfTests.Equal(PileType.Exhaust, borrowing.Pile!.Type, "borrowing source exhausts");
        EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, "borrowing pays its energy without a starter relic refund");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "borrowing receives no starter relic Qi");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SwordMomentum, "generating foreign attacks grants no sword momentum");
    }

    private static async Task SecondaryBorrowedAttack(bool upgraded)
    {
        var fixture = await ReadyFixture("DUANYUSECONDBORROW");
        await SetQi(fixture, 0);
        fixture.Player.PlayerCombatState!.Energy = upgraded ? 2 : 2;
        var borrowing = EngineSelfTests.AddCard(fixture, "DY_U36", PileType.Hand, upgraded);
        await PaidPlay(fixture, borrowing);
        var generated = GeneratedCards();
        var attack = generated.Single(card => card.Type == CardType.Attack);
        EngineSelfTests.Equal(true, attack.Keywords.Contains(CardKeyword.Exhaust), "secondary borrow still adds Exhaust to the generated attack");
        EngineSelfTests.Equal(1, generated.OfType<DY_S02>().Count(card => card.Pile?.Type == PileType.Hand), "secondary borrow leaves one foreign-Qi status in hand");
        EngineSelfTests.Equal(upgraded ? 17 : 12, fixture.Enemy.MaxHp - fixture.Enemy.CurrentHp, "secondary borrow deals its printed attack damage");
        EngineSelfTests.Equal(PileType.Discard, borrowing.Pile!.Type, "secondary borrow card goes to discard");
    }

    private static async Task XResourceCards(bool upgraded)
    {
        var fixture = await ReadyFixture(upgraded ? "DUANYUXUP" : "DUANYUX");
        await RelicCmd.Remove(fixture.Player.Relics.OfType<LanghuanPendant>().Single());
        await SetQi(fixture, 3);
        fixture.Player.PlayerCombatState!.Energy = 3;
        var firstStatus = EngineSelfTests.AddCard(fixture, "DY_S01", PileType.Hand);
        var secondStatus = EngineSelfTests.AddCard(fixture, "DY_S02", PileType.Hand);
        EngineSelfTests.AddCard(fixture, "DY_B01", PileType.Hand);
        var guard = EngineSelfTests.AddCard(fixture, "DY_U37", PileType.Hand, upgraded);
        await PaidPlay(fixture, guard);
        EngineSelfTests.Equal(3, guard.EnergyCost.CapturedXValue, "万流归宗捕获支付时的X值");
        EngineSelfTests.Equal(upgraded ? 18 : 12, fixture.Player.Creature.Block, "万流归宗按X获得格挡");
        EngineSelfTests.Equal(2, fixture.Player.PlayerCombatState.Energy, "万流归宗按实际消耗状态牌返还能量");
        EngineSelfTests.Equal(5, DuanYuState.Get(fixture.Player).Qi, "万流归宗按实际消耗状态牌获得真气");
        EngineSelfTests.Equal(PileType.Exhaust, firstStatus.Pile!.Type, "万流归宗消耗第一张状态牌");
        EngineSelfTests.Equal(PileType.Exhaust, secondStatus.Pile!.Type, "万流归宗消耗第二张状态牌");
        EngineSelfTests.Equal(PileType.Discard, guard.Pile!.Type, "万流归宗自身不消耗");

        var noStatus = await ReadyFixture("DUANYUXNOSTATUS" + (upgraded ? "U" : ""));
        await RelicCmd.Remove(noStatus.Player.Relics.OfType<LanghuanPendant>().Single());
        await SetQi(noStatus, 0);
        noStatus.Player.PlayerCombatState!.Energy = 3;
        var emptyGuard = EngineSelfTests.AddCard(noStatus, "DY_U37", PileType.Hand, upgraded);
        await PaidPlay(noStatus, emptyGuard);
        EngineSelfTests.Equal(upgraded ? 18 : 12, noStatus.Player.Creature.Block, "万流归宗无状态牌仍按X获得格挡");
        EngineSelfTests.Equal(0, noStatus.Player.PlayerCombatState.Energy, "万流归宗无状态牌不返还能量");
        EngineSelfTests.Equal(0, DuanYuState.Get(noStatus.Player).Qi, "万流归宗无状态牌不获得真气");

        var zero = await ReadyFixture("DUANYUXZERO" + (upgraded ? "U" : ""));
        await SetQi(zero, 0);
        zero.Player.PlayerCombatState!.Energy = 0;
        var volley = EngineSelfTests.AddCard(zero, "DY_R21", PileType.Hand, upgraded);
        var initialHp = zero.Enemy.CurrentHp;
        await PaidPlay(zero, volley);
        EngineSelfTests.Equal(0, initialHp - zero.Enemy.CurrentHp, "六脉齐发真气X为0时不造成伤害");
        EngineSelfTests.Equal(0, DuanYuState.Get(zero.Player).Qi, "六脉齐发零真气不产生资源");
        EngineSelfTests.Equal(0, DuanYuState.Get(zero.Player).SpendingCount, "六脉齐发零真气不计成功支付");
        EngineSelfTests.Equal(PileType.Exhaust, volley.Pile!.Type, "六脉齐发自身消耗");

        var combat = await ReadyFixture("DUANYUXHITS" + (upgraded ? "U" : ""));
        await SetQi(combat, 5);
        combat.Player.PlayerCombatState!.Energy = 2;
        var secondEnemy = combat.Combat.CreateCreature(ModelDb.Monster<BigDummy>().ToMutable(), CombatSide.Enemy, null);
        combat.Combat.AddCreature(secondEnemy);
        var volleyWithX = EngineSelfTests.AddCard(combat, "DY_R21", PileType.Hand, upgraded);
        var xInitialHp = combat.Enemy.CurrentHp;
        var secondInitialHp = secondEnemy.CurrentHp;
        await PaidPlay(combat, volleyWithX);
        EngineSelfTests.Equal(5 * (upgraded ? 8 : 6), xInitialHp - combat.Enemy.CurrentHp, "六脉齐发按实际支付真气攻击五次");
        EngineSelfTests.Equal(0, secondInitialHp - secondEnemy.CurrentHp, "六脉齐发仅伤害选中敌人");
        EngineSelfTests.Equal(0, DuanYuState.Get(combat.Player).Qi, "六脉齐发支付全部真气");
        EngineSelfTests.Equal(2, combat.Player.PlayerCombatState.Energy, "六脉齐发不支付能量");
    }

    private static async Task PlayForeignAttack(Type attackType, int expectedDamage, bool upgraded)
    {
        var fixture = await ReadyFixture("DUANYUFOREIGNPLAY");
        await SetQi(fixture, 0);
        await DuanYuForeignCards.GenerateAttacks(fixture.Player, new ThrowingPlayerChoiceContext(), int.MaxValue, upgraded);
        var attack = GeneratedCards().Single(card => card.GetType() == attackType);
        foreach (var existing in PileType.Hand.GetPile(fixture.Player).Cards.ToArray())
            await CardPileCmd.Add(existing, PileType.Discard);
        await CardPileCmd.Add(attack, PileType.Hand);
        var isXCost = XCostAttacks.Contains(attackType);
        fixture.Player.PlayerCombatState!.Energy = isXCost ? 3 : 0;
        var initialHp = fixture.Enemy.CurrentHp;
        await PaidPlay(fixture, attack);
        EngineSelfTests.Equal(expectedDamage * (isXCost ? 3 : 1), initialHp - fixture.Enemy.CurrentHp, attackType.Name + " executes its native attack body");
        EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, "borrowed native attack is legally playable at zero energy");
        if (isXCost) EngineSelfTests.Equal(3, attack.EnergyCost.CapturedXValue, "borrowed X-cost attack captures the actual energy spent");
        EngineSelfTests.Equal(PileType.Exhaust, attack.Pile!.Type, "borrowed native attack exhausts after playing");
        EngineSelfTests.Equal(1, ExhaustCount(attack), "borrowed attack exhausts once");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SwordPlays, "native attack does not count as a six-pulse sword");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SwordMomentum, "native attack grants no sword momentum");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SkillsPlayed, "native attack grants no first-skill reward");
        if (attack is IronWave) EngineSelfTests.Equal(upgraded ? 7 : 5, fixture.Player.Creature.Block, "borrowed Iron Wave keeps native block");
        if (attack is PoisonedStab) EngineSelfTests.Equal(upgraded ? 4 : 3, fixture.Enemy.GetPowerAmount<PoisonPower>(), "borrowed Poisoned Stab keeps native poison");
        if (attack is BeamCell) EngineSelfTests.Equal(upgraded ? 2 : 1, fixture.Enemy.GetPowerAmount<VulnerablePower>(), "borrowed Beam Cell keeps native vulnerability");
        if (attack is PommelStrike) EngineSelfTests.Equal(upgraded ? 2 : 1, PileType.Hand.GetPile(fixture.Player).Cards.Count, "borrowed Pommel Strike keeps native draw");
        if (attack is SweepingBeam) EngineSelfTests.Equal(1, PileType.Hand.GetPile(fixture.Player).Cards.Count, "borrowed Sweeping Beam keeps native draw");
        if (attack is DaggerThrow) EngineSelfTests.Equal(0, PileType.Hand.GetPile(fixture.Player).Cards.Count, "borrowed Dagger Throw performs both draw and discard");
    }

    private static async Task InactiveCombat()
    {
        foreach (var defeat in new[] { false, true })
        {
            var fixture = await ReadyFixture();
            await SetQi(fixture, 5);
            var victim = defeat ? fixture.Player.Creature : fixture.Enemy;
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), victim, victim.CurrentHp, ValueProp.Unblockable | ValueProp.Unpowered, defeat ? fixture.Enemy : fixture.Player.Creature);
            EngineSelfTests.Equal(false, DuanYuState.CanAct(fixture.Player), "finished-combat fixture cannot act");
            var generationBefore = fixture.Run.Rng.CombatCardGeneration.ToSerializable();
            var qiBefore = DuanYuState.Get(fixture.Player).Qi;
            await DuanYuForeignCards.GenerateAttacks(fixture.Player, new ThrowingPlayerChoiceContext(), 2, true);
            await DuanYuState.LoseQi(fixture.Player, 2);
            EngineSelfTests.Equal(generationBefore, fixture.Run.Rng.CombatCardGeneration.ToSerializable(), "inactive combat does not consume generation RNG");
            EngineSelfTests.Equal(0, GeneratedCards().Length, "inactive combat creates no borrowed attacks");
            EngineSelfTests.Equal(qiBefore, DuanYuState.Get(fixture.Player).Qi, "inactive combat does not mutate Qi");
        }
    }

    private static async Task ExhaustPower()
    {
        var fixture = await ReadyFixture();
        await PaidPlay(fixture, EngineSelfTests.AddCard(fixture, "DY_U28", PileType.Hand));
        await PaidPlay(fixture, EngineSelfTests.AddCard(fixture, "DY_U28", PileType.Hand, true));
        EngineSelfTests.Equal(5, fixture.Player.Creature.GetPowerAmount<PeacefulHeartPower>(), "exhaust-block powers stack normal and upgraded amounts");
        EngineSelfTests.Equal(0, fixture.Player.Creature.Block, "playing the power grants no immediate block");
        await SetQi(fixture, 0);
        var fuel = EngineSelfTests.AddCard(fixture, "DY_B01", PileType.Hand);
        await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), fuel);
        EngineSelfTests.Equal(5, fixture.Player.Creature.Block, "active exhaust grants stacked power block once");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "exhaust power grants no Qi");
        EngineSelfTests.Equal(0, PileType.Hand.GetPile(fixture.Player).Cards.Count, "exhaust power grants no card draw");
        await DuanYuState.EndTurn(fixture.Player);
        EngineSelfTests.Equal(5, fixture.Player.Creature.Block, "peaceful end-turn grants no obsolete passive block");
        await PaidPlay(fixture, EngineSelfTests.AddCard(fixture, "DY_S02", PileType.Hand));
        EngineSelfTests.Equal(10, fixture.Player.Creature.Block, "played Exhaust status triggers the power once");
        EngineSelfTests.Equal(3, DuanYuState.Get(fixture.Player).Qi, "played Exhaust status restores one Qi through the exhaust power");
    }

    private static async Task ExhaustPowerLifetime()
    {
        var fixture = await ReadyFixture();
        var companion = Player.CreateForNewRun(ModelDb.Character<DuanYuCharacter>(), UnlockState.all, 2);
        fixture.Run.AddPlayerDebug(companion, -1);
        companion.ResetCombatState();
        fixture.Combat.AddPlayer(companion);
        await DuanYuState.AddEffect(fixture.Player, "DY_U28", 3);
        await DuanYuState.AddEffect(companion, "DY_U28", 4);
        var companionFuel = fixture.Combat.CreateCard(ModelDb.Card<DY_B01>(), companion);
        PileType.Hand.GetPile(companion).AddInternal(companionFuel);
        await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), companionFuel);
        EngineSelfTests.Equal(0, fixture.Player.Creature.Block, "companion exhaustion does not reward another owner");
        EngineSelfTests.Equal(4, companion.Creature.Block, "companion exhaustion rewards its own power");
        await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), EngineSelfTests.AddCard(fixture, "DY_B01", PileType.Hand));
        EngineSelfTests.Equal(3, fixture.Player.Creature.Block, "local exhaustion rewards its own power");
        EngineSelfTests.Equal(4, companion.Creature.Block, "local exhaustion leaves companion block unchanged");
        foreach (var defeat in new[] { false, true })
        {
            var inactive = await ReadyFixture();
            await DuanYuState.AddEffect(inactive.Player, "DY_U28", 3);
            var power = inactive.Player.Creature.GetPower<PeacefulHeartPower>()!;
            var fuel = EngineSelfTests.AddCard(inactive, "DY_B01", PileType.Hand);
            var victim = defeat ? inactive.Player.Creature : inactive.Enemy;
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), victim, victim.CurrentHp, ValueProp.Unblockable | ValueProp.Unpowered, defeat ? inactive.Enemy : inactive.Player.Creature);
            var beforeBlock = inactive.Player.Creature.Block;
            await power.AfterCardExhausted(new ThrowingPlayerChoiceContext(), fuel, false);
            EngineSelfTests.Equal(beforeBlock, inactive.Player.Creature.Block, "queued exhaust hook grants no block after death or victory");
        }
    }

    private static async Task ActiveExhaust(bool upgraded)
    {
        var fixture = await ReadyFixture();
        await SetQi(fixture, 0);
        await DuanYuState.AddEffect(fixture.Player, "DY_U28", 3);
        var fuel = AddNative<Wither>(fixture, PileType.Hand);
        var card = EngineSelfTests.AddCard(fixture, "DY_U06", PileType.Hand, upgraded);
        fixture.Player.PlayerCombatState!.Energy = 1;
        await PaidPlay(fixture, card);
        EngineSelfTests.Equal(PileType.Exhaust, fuel.Pile!.Type, "active meditation exhausts a native unplayable Wither");
        EngineSelfTests.Equal(1, ExhaustCount(fuel), "active meditation consumes fuel once");
        EngineSelfTests.Equal(PileType.Discard, card.Pile!.Type, "active meditation remains reusable");
        EngineSelfTests.Equal(upgraded ? 4 : 3, PileType.Hand.GetPile(fixture.Player).Cards.Count, "active meditation draws its base cards plus one native status-fuel card");
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).Qi, "active meditation grants one Qi for a native status at either upgrade level");
        EngineSelfTests.Equal(3, fixture.Player.Creature.Block, "active fuel triggers exhaust power once");

        foreach (bool hasFuel in new[] { true, false })
        {
            var ordinary = await ReadyFixture();
            await SetQi(ordinary, 0);
            var otherCard = hasFuel ? EngineSelfTests.AddCard(ordinary, "DY_B01", PileType.Hand) : null;
            var source = EngineSelfTests.AddCard(ordinary, "DY_U06", PileType.Hand, upgraded);
            await PaidPlay(ordinary, source);
            if (otherCard != null)
                EngineSelfTests.Equal(PileType.Exhaust, otherCard.Pile!.Type, "active meditation can exhaust an ordinary card");
            EngineSelfTests.Equal(upgraded ? 3 : 2, PileType.Hand.GetPile(ordinary.Player).Cards.Count, "active meditation draws its base amount without status fuel");
            EngineSelfTests.Equal(0, DuanYuState.Get(ordinary.Player).Qi, "active meditation grants no Qi without status fuel");
        }
    }

    private static async Task SelectiveExhaust(bool upgraded)
    {
        foreach (var selectedCount in new[] { 0, upgraded ? 4 : 3 })
        {
            var fixture = await ReadyFixture();
            await SetQi(fixture, 0);
            await DuanYuState.AddEffect(fixture.Player, "DY_U28", 3);
            for (var index = 0; index < 5; index++) EngineSelfTests.AddCard(fixture, "DY_B01", PileType.Hand);
            using var scope = RunManager.Instance.CardSelectorStack.PushSelector(fixture.Player, new CountSelector(selectedCount));
            var card = EngineSelfTests.AddCard(fixture, "DY_U35", PileType.Hand, upgraded);
            await PaidPlay(fixture, card);
            EngineSelfTests.Equal(5 - selectedCount, PileType.Hand.GetPile(fixture.Player).Cards.Count, "selective exhaust honors zero or maximum selection");
            EngineSelfTests.Equal(selectedCount + 1, PileType.Exhaust.GetPile(fixture.Player).Cards.Count, "selective exhaust includes each selected card and its source");
            EngineSelfTests.Equal(selectedCount, DuanYuState.Get(fixture.Player).Qi, "selective exhaust rewards only actual selected cards");
            EngineSelfTests.Equal(selectedCount * (upgraded ? 3 : 2) + (selectedCount + 1) * 3, fixture.Player.Creature.Block, "selective block and native exhaust power both resolve once per card");
        }
    }

    private static async Task StokeHand(bool upgraded)
    {
        var fixture = await ReadyFixture("DUANYUSTOKE");
        await DuanYuState.AddEffect(fixture.Player, "DY_U28", 3);
        var fuel = new List<CardModel>
        {
            AddNative<Wither>(fixture, PileType.Hand),
            EngineSelfTests.AddCard(fixture, "DY_S01", PileType.Hand),
            EngineSelfTests.AddCard(fixture, "DY_S02", PileType.Hand)
        };
        for (var index = 0; index < 4; index++) fuel.Add(EngineSelfTests.AddCard(fixture, "DY_B01", PileType.Hand));
        var card = EngineSelfTests.AddCard(fixture, "DY_R19", PileType.Hand, upgraded);
        var rngBefore = fixture.Run.Rng.CombatCardGeneration.ToSerializable();
        var prediction = new Rng(rngBefore);
        var expected = Enumerable.Range(0, fuel.Count).Select(_ => $"DY_T{prediction.NextInt(1, 7):00}").ToArray();
        await PaidPlay(fixture, card);
        var swords = GeneratedCards().Where(DuanYuState.IsSword).ToArray();
        EngineSelfTests.Equal(true, fuel.All(item => item.Pile?.Type == PileType.Exhaust && ExhaustCount(item) == 1), "stoke exhausts every original hand card once");
        EngineSelfTests.Equal(PileType.Discard, card.Pile!.Type, "stoke source remains reusable");
        EngineSelfTests.Equal(7, swords.Length, "stoke generates one sword per actual exhausted card");
        EngineSelfTests.Equal(string.Join(",", expected), string.Join(",", swords.Select(DuanYuState.CardId)), "stoke consumes native RNG for independent sword choices");
        EngineSelfTests.Equal(true, swords.Select(card => card.Id).Distinct().Count() < swords.Length, "stoke allows duplicate sword kinds");
        EngineSelfTests.Equal(true, swords.All(sword => sword.IsUpgraded == upgraded && sword.Pile?.Type == PileType.Hand), "stoke sword tier follows source upgrade");
        EngineSelfTests.Equal((upgraded ? 8 : 6) + 21, fixture.Player.Creature.Block, "stoke block combines printed block with seven exhaust triggers");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SwordPlays, "stoke generation is not sword play");
    }

    private static async Task CleanseStatuses(bool upgraded)
    {
        var fixture = await ReadyFixture();
        await SetQi(fixture, 0);
        await DuanYuState.AddEffect(fixture.Player, "DY_U28", 3);
        var wither = AddNative<Wither>(fixture, PileType.Hand);
        wither.FakeUpgrade();
        wither.FakeUpgrade();
        var statuses = new CardModel[] { wither, EngineSelfTests.AddCard(fixture, "DY_S01", PileType.Hand), EngineSelfTests.AddCard(fixture, "DY_S02", PileType.Hand) };
        EngineSelfTests.AddCard(fixture, "DY_B01", PileType.Hand);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 2, fixture.Enemy, null);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 2, fixture.Enemy, null);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 3, fixture.Enemy, null);
        await PowerCmd.Apply<PoisonPower>(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 2, fixture.Enemy, null);
        var card = EngineSelfTests.AddCard(fixture, "DY_R16", PileType.Hand, upgraded);
        int energyBefore = fixture.Player.PlayerCombatState!.Energy;
        await PaidPlay(fixture, card);
        EngineSelfTests.Equal(energyBefore - (upgraded ? 1 : 2), fixture.Player.PlayerCombatState.Energy, "cleanse costs two energy before upgrade and one after");
        EngineSelfTests.Equal(true, statuses.All(status => status.Pile?.Type == PileType.Exhaust && ExhaustCount(status) == 1), "cleanse exhausts custom statuses and upgraded native Wither");
        EngineSelfTests.Equal(5, DuanYuState.Get(fixture.Player).Qi, "cleanse grants one Qi per status plus custom-status exhaust rewards");
        EngineSelfTests.Equal(4, PileType.Hand.GetPile(fixture.Player).Cards.Count, "cleanse draws three and preserves the non-status card");
        EngineSelfTests.Equal(16, fixture.Player.Creature.Block, "cleanse removes Frail before base block and three status exhaust triggers");
        EngineSelfTests.Equal(0, fixture.Player.Creature.GetPowerAmount<FrailPower>(), "cleanse removes Frail");
        EngineSelfTests.Equal(0, fixture.Player.Creature.GetPowerAmount<WeakPower>(), "cleanse removes Weak");
        EngineSelfTests.Equal(0, fixture.Player.Creature.GetPowerAmount<VulnerablePower>(), "cleanse removes Vulnerable");
        EngineSelfTests.Equal(2, fixture.Player.Creature.GetPowerAmount<PoisonPower>(), "cleanse preserves unrelated Poison");
        EngineSelfTests.Equal(PileType.Discard, card.Pile!.Type, "cleanse itself goes to discard");
    }

    private static async Task<TestFixture> ReadyFixture(string seed = "DUANYUSPECIAL")
    {
        var fixture = EngineSelfTests.CreateFixture(seed: seed);
        await Hook.BeforeCombatStart(fixture.Run, fixture.Combat);
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [fixture.Player.Creature]);
        await Hook.BeforeHandDraw(fixture.Combat, fixture.Player, new ThrowingPlayerChoiceContext());
        foreach (var pile in fixture.Player.PlayerCombatState!.AllPiles) pile.Clear();
        for (var index = 0; index < 20; index++) EngineSelfTests.AddCard(fixture, "DY_B01", PileType.Draw);
        return fixture;
    }

    private static async Task PaidPlay(TestFixture fixture, CardModel card)
    {
        EngineSelfTests.Equal(PileType.Hand, card.Pile!.Type, card.Id.Entry + " starts in hand");
        EngineSelfTests.Equal(true, card.CanPlay(), card.Id.Entry + " has sufficient resources before its effects");
        var energyValue = card.EnergyCost.GetAmountToSpend();
        var beforeEnergy = fixture.Player.PlayerCombatState!.Energy;
        var (energySpent, starsSpent) = await card.SpendResources();
        EngineSelfTests.Equal(energyValue, energySpent, card.Id.Entry + " pays the native energy cost");
        EngineSelfTests.Equal(beforeEnergy - energySpent, fixture.Player.PlayerCombatState.Energy, card.Id.Entry + " pays before effects");
        var target = card.TargetType switch
        {
            TargetType.AnyEnemy => fixture.Enemy,
            TargetType.AnyPlayer => fixture.Player.Creature,
            _ => null
        };
        await card.OnPlayWrapper(new ThrowingPlayerChoiceContext(), target, false,
            new ResourceInfo { EnergySpent = energySpent, EnergyValue = energyValue, StarsSpent = starsSpent, StarValue = 0 }, true);
    }

    private static async Task SetQi(TestFixture fixture, int amount)
    {
        await DuanYuState.LoseQi(fixture.Player, int.MaxValue);
        await DuanYuState.GainQi(fixture.Player, amount);
    }

    private static async Task NativeTurnEnd(TestFixture fixture)
    {
        var turnState = AccessTools.Field(typeof(CombatManager), "_turnState").GetValue(CombatManager.Instance)!;
        await (Task)AccessTools.Method(typeof(CombatManager), "DoTurnEnd").Invoke(CombatManager.Instance, [turnState, fixture.Player, new ThrowingPlayerChoiceContext()])!;
    }

    private static T AddNative<T>(TestFixture fixture, PileType pile) where T : CardModel, new()
    {
        var card = (T)fixture.Combat.CreateCard(ModelDb.Card<T>(), fixture.Player);
        pile.GetPile(fixture.Player).AddInternal(card);
        return card;
    }

    private static string TemplateSnapshot(CardModel card)
        => card.CurrentUpgradeLevel + ":" + card.EnergyCost.GetWithModifiers(CostModifiers.All) + ":" + string.Join(",", card.DynamicVars.OrderBy(pair => pair.Key).Select(pair => pair.Key + "=" + pair.Value.BaseValue)) + ":" + string.Join(",", card.Keywords.Order());

    private static CardModel[] GeneratedCards()
        => CombatManager.Instance.History.Entries.OfType<CardGeneratedEntry>().Select(entry => entry.Card).ToArray();

    private static int ExhaustCount(CardModel card)
        => CombatManager.Instance.History.Entries.OfType<CardExhaustedEntry>().Count(entry => ReferenceEquals(entry.Card, card));

    private sealed class CountSelector(int count) : ICardSelector
    {
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
            => Task.FromResult<IEnumerable<CardModel>>(options.Take(Math.Clamp(count, minSelect, maxSelect)).ToArray());
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options, IReadOnlyList<CardRewardAlternative> alternatives)
            => new() { card = options.FirstOrDefault()?.Card };
    }
}
#endif
