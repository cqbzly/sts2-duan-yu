#if DEBUG
using DuanYu.Cards;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace DuanYu.DebugTests;

internal static class RandomCardSelfTests
{
    internal static IEnumerable<(string Name, Func<Task> Action)> Cases
    {
        get
        {
            foreach (var upgraded in new[] { false, true })
            {
                var suffix = upgraded ? "+" : "";
                yield return ("random:swords-distinct-and-full-hand" + suffix, () => RandomSwords(upgraded));
                yield return ("fixed:scholar-resources-and-qi-timing" + suffix, () => ScholarBenefits(upgraded));
                yield return ("fixed:dream-resources-and-no-roll" + suffix, () => DreamBenefits(upgraded));
            }
            yield return ("random:fixed-seed-reproducible", ReproducibleSequence);
            yield return ("random:previews-preserve-rng", PreviewPreservesRng);
            yield return ("random:native-replay-regenerates-swords", NativeSwordReplay);
            yield return ("fixed:native-replay-stacks-benefits", NativeBenefitReplay);
            yield return ("random:dead-player-consumes-no-rng", DeadPlayerStopsRandomEffects);
        }
    }

    private static async Task RandomSwords(bool upgraded)
    {
        foreach (var fullHand in new[] { false, true })
        {
            var fixture = await ReadyFixture("DUANYURANDOMSWORDS");
            if (fullHand)
                for (var index = 0; index < CardPile.MaxCardsInHand - 1; index++)
                    EngineSelfTests.AddCard(fixture, "DY_B01", PileType.Hand);
            var rngBefore = fixture.Run.Rng.CombatCardGeneration.ToSerializable();
            var expected = SwordPair(new Rng(rngBefore));
            var card = EngineSelfTests.AddCard(fixture, "DY_U15", PileType.Hand, upgraded);
            await BalanceSelfTests.PaidPlay(fixture, card);
            var generated = GeneratedSwords();
            EngineSelfTests.Equal(2, generated.Length, "random sword generation needs no existing sword");
            EngineSelfTests.Equal(string.Join(",", expected), string.Join(",", generated.Select(DuanYuState.CardId)), "random swords follow native combat generation RNG");
            EngineSelfTests.Equal(2, generated.Select(DuanYuState.CardId).Distinct().Count(), "random generation samples without replacement");
            EngineSelfTests.Equal(true, generated.All(sword => sword.IsUpgraded == upgraded), "random swords preserve the generator upgrade");
            EngineSelfTests.Equal(2, fixture.Run.Rng.CombatCardGeneration.ToSerializable().counter - rngBefore.counter, "two random swords consume two native draws");
            EngineSelfTests.Equal(fullHand ? 1 : 2, generated.Count(sword => sword.Pile?.Type == PileType.Hand), "generated swords use available hand slots");
            EngineSelfTests.Equal(fullHand ? 1 : 0, generated.Count(sword => sword.Pile?.Type == PileType.Discard), "overflow sword moves to native discard pile");
            EngineSelfTests.Equal(fullHand ? 9 : 0, PileType.Hand.GetPile(fixture.Player).Cards.Count(existing => !DuanYuState.IsSword(existing)), "random generation does not exhaust a hand card");
            EngineSelfTests.Equal(true, PileType.Exhaust.GetPile(fixture.Player).Cards.Contains(card), "random sword generator exhausts");
            EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SwordMomentum, "generation alone grants no sword momentum");
        }
    }

    private static async Task ScholarBenefits(bool upgraded)
    {
        foreach (var startingQi in new[] { 4, 5, 9, 12, 50 })
        {
            var fixture = await ReadyFixture("DUANYUSCHOLAR" + startingQi);
            await SetQi(fixture, startingQi);
            fixture.Player.PlayerCombatState!.Energy = 2;
            var rngBefore = fixture.Run.Rng.Niche.ToSerializable();
            var card = EngineSelfTests.AddCard(fixture, "DY_U26", PileType.Hand, upgraded);
            await BalanceSelfTests.PaidPlay(fixture, card);
            var qiAmount = 1;
            EngineSelfTests.Equal(startingQi + qiAmount, DuanYuState.Get(fixture.Player).Qi, "scholar grants its full printed Qi above nine");
            EngineSelfTests.Equal(upgraded ? 1 : 0, fixture.Player.PlayerCombatState.Energy, "scholar pays its printed energy without a starter relic refund");
            EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).SwordMomentum, "scholar always grants one printed momentum");
            EngineSelfTests.Equal((upgraded ? 3 : 2) + (startingQi >= 5 ? 1 : 0), PileType.Hand.GetPile(fixture.Player).Cards.Count, "scholar tests bonus draw before fixed Qi gains");
            EngineSelfTests.Equal(rngBefore, fixture.Run.Rng.Niche.ToSerializable(), "scholar never advances benefit RNG");
            EngineSelfTests.Equal(PileType.Discard, card.Pile!.Type, "scholar remains reusable");
        }
    }

    private static async Task DreamBenefits(bool upgraded)
    {
        var fixture = await ReadyFixture("DUANYUDREAM");
        await SetQi(fixture, 0);
        fixture.Player.PlayerCombatState!.Energy = 0;
        await CreatureCmd.GainBlock(fixture.Player.Creature, 11, ValueProp.Unpowered, null);
        var rngBefore = fixture.Run.Rng.Niche.ToSerializable();
        var card = EngineSelfTests.AddCard(fixture, "DY_R14", PileType.Hand, upgraded);
        await BalanceSelfTests.PaidPlay(fixture, card);
        var amount = upgraded ? 3 : 2;
        EngineSelfTests.Equal(amount, DuanYuState.Get(fixture.Player).Qi, "dream grants only its printed Qi");
        EngineSelfTests.Equal(amount, fixture.Player.PlayerCombatState.Energy, "dream grants its printed energy without requiring energy to start");
        EngineSelfTests.Equal(upgraded ? 3 : 2, DuanYuState.Get(fixture.Player).SwordMomentum, "dream always grants printed momentum");
        EngineSelfTests.Equal(0, fixture.Player.Creature.Block, "dream loses all block before rewards");
        EngineSelfTests.Equal(upgraded ? 4 : 3, PileType.Hand.GetPile(fixture.Player).Cards.Count, "dream draws its printed amount");
        EngineSelfTests.Equal(rngBefore, fixture.Run.Rng.Niche.ToSerializable(), "dream never advances benefit RNG");
        EngineSelfTests.Equal(PileType.Exhaust, card.Pile!.Type, "dream exhausts");
    }

    private static async Task ReproducibleSequence()
    {
        var first = await RunSequence("DUANYUREPEATABLE");
        var second = await RunSequence("DUANYUREPEATABLE");
        EngineSelfTests.Equal(first, second, "same native run seed reproduces generated swords, benefits and RNG state");
    }

    private static async Task<string> RunSequence(string seed)
    {
        var fixture = await ReadyFixture(seed);
        await SetQi(fixture, 0);
        fixture.Player.PlayerCombatState!.Energy = 4;
        foreach (var id in new[] { "DY_U15", "DY_U26", "DY_R14" })
            await BalanceSelfTests.PaidPlay(fixture, EngineSelfTests.AddCard(fixture, id, PileType.Hand));
        return string.Join(",", GeneratedSwords().Select(DuanYuState.CardId)) + ":" + DuanYuState.Get(fixture.Player).Qi + ":" + fixture.Player.PlayerCombatState.Energy + ":" + DuanYuState.Get(fixture.Player).SwordMomentum + ":" + fixture.Run.Rng.CombatCardGeneration.ToSerializable() + ":" + fixture.Run.Rng.Niche.ToSerializable();
    }

    private static async Task PreviewPreservesRng()
    {
        var fixture = await ReadyFixture("DUANYURANDOMPREVIEW");
        var generationBefore = fixture.Run.Rng.CombatCardGeneration.ToSerializable();
        var benefitsBefore = fixture.Run.Rng.Niche.ToSerializable();
        foreach (var id in new[] { "DY_U15", "DY_U26", "DY_R14", "DY_T01" })
        {
            var card = EngineSelfTests.AddCard(fixture, id, PileType.Hand);
            for (var repetition = 0; repetition < 3; repetition++)
            {
                _ = card.GetDescriptionForPile(PileType.Hand);
                _ = card.GetDescriptionForUpgradePreview();
                _ = card.HoverTips.ToArray();
                _ = card.CanPlay();
            }
        }
        EngineSelfTests.Equal(generationBefore, fixture.Run.Rng.CombatCardGeneration.ToSerializable(), "all card and upgrade previews preserve combat generation RNG");
        EngineSelfTests.Equal(benefitsBefore, fixture.Run.Rng.Niche.ToSerializable(), "all card and upgrade previews preserve benefit RNG");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SwordMomentum, "previews do not grant momentum");
    }

    private static async Task NativeSwordReplay()
    {
        var fixture = await ReadyFixture("DUANYURANDOMREPLAY");
        var rngBefore = fixture.Run.Rng.CombatCardGeneration.ToSerializable();
        var prediction = new Rng(rngBefore);
        var expected = Enumerable.Range(0, 3).SelectMany(_ => SwordPair(prediction)).ToArray();
        var card = EngineSelfTests.AddCard(fixture, "DY_U15", PileType.Hand, true);
        card.BaseReplayCount = 2;
        await BalanceSelfTests.PaidPlay(fixture, card);
        var generated = GeneratedSwords();
        EngineSelfTests.Equal(6, generated.Length, "native replay generates two new swords per body");
        EngineSelfTests.Equal(string.Join(",", expected), string.Join(",", generated.Select(DuanYuState.CardId)), "every native replay consumes fresh generation draws");
        EngineSelfTests.Equal(true, generated.All(sword => sword.IsUpgraded), "all replay-generated swords preserve upgrades");
        for (var index = 0; index < 3; index++)
            EngineSelfTests.Equal(2, generated.Skip(index * 2).Take(2).Select(DuanYuState.CardId).Distinct().Count(), "each replay independently samples without replacement");
        EngineSelfTests.Equal(6, fixture.Run.Rng.CombatCardGeneration.ToSerializable().counter - rngBefore.counter, "replay advances native generation state once per generated card");
        AssertReplay(fixture, card);
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SwordPlays, "random generation and replay never count as played swords");
    }

    private static async Task NativeBenefitReplay()
    {
        var fixture = await ReadyFixture("DUANYUBENEFITREPLAY");
        await SetQi(fixture, 0);
        fixture.Player.PlayerCombatState!.Energy = 0;
        var rngBefore = fixture.Run.Rng.Niche.ToSerializable();
        var card = EngineSelfTests.AddCard(fixture, "DY_R14", PileType.Hand);
        card.BaseReplayCount = 2;
        await BalanceSelfTests.PaidPlay(fixture, card);
        EngineSelfTests.Equal(6, DuanYuState.Get(fixture.Player).Qi, "replayed fixed Qi grants two Qi per body without a starter relic trigger");
        EngineSelfTests.Equal(6, fixture.Player.PlayerCombatState.Energy, "replayed dream grants two energy per body");
        EngineSelfTests.Equal(6, DuanYuState.Get(fixture.Player).SwordMomentum, "replayed fixed momentum follows the reduced printed gain");
        EngineSelfTests.Equal(9, PileType.Hand.GetPile(fixture.Player).Cards.Count, "three dream bodies perform three native draws each");
        EngineSelfTests.Equal(rngBefore, fixture.Run.Rng.Niche.ToSerializable(), "replayed fixed dream never advances benefit RNG");
        AssertReplay(fixture, card);
    }

    private static async Task DeadPlayerStopsRandomEffects()
    {
        var fixture = await ReadyFixture("DUANYURANDOMDEATH");
        await SetQi(fixture, 0);
        fixture.Player.PlayerCombatState!.Energy = 0;
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, fixture.Player.Creature.CurrentHp,
            ValueProp.Unblockable | ValueProp.Unpowered, fixture.Enemy);
        EngineSelfTests.Equal(false, fixture.Player.Creature.IsAlive, "death guard fixture is dead");
        var generationBefore = fixture.Run.Rng.CombatCardGeneration.ToSerializable();
        var benefitsBefore = fixture.Run.Rng.Niche.ToSerializable();
        foreach (var id in new[] { "DY_U15", "DY_U26", "DY_R14" })
        {
            var card = EngineSelfTests.AddCard(fixture, id, PileType.Hand);
            await DuanYuCardExecutor.Play(card, new ThrowingPlayerChoiceContext(), new CardPlay
            {
                Card = card, Player = fixture.Player, Target = null, ResultPile = PileType.Exhaust,
                Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 },
                IsAutoPlay = false, PlayIndex = 0, PlayCount = 1
            });
        }
        EngineSelfTests.Equal(generationBefore, fixture.Run.Rng.CombatCardGeneration.ToSerializable(), "dead player does not roll new swords");
        EngineSelfTests.Equal(benefitsBefore, fixture.Run.Rng.Niche.ToSerializable(), "dead player does not roll benefits");
        EngineSelfTests.Equal(0, GeneratedSwords().Length, "dead player generates no cards");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SwordMomentum, "dead player gains no momentum");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "dead player gains no Qi");
        EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, "dead player gains no energy");
    }

    private static void AssertReplay(TestFixture fixture, DuanYuCard card)
    {
        var plays = CombatManager.Instance.History.CardPlaysFinished.Where(entry => ReferenceEquals(entry.CardPlay.Card, card)).Select(entry => entry.CardPlay).ToArray();
        EngineSelfTests.Equal(3, plays.Length, "native history records three completed random card bodies");
        EngineSelfTests.Equal(3, plays.Distinct().Count(), "native replay uses three distinct CardPlay objects");
        EngineSelfTests.Equal("0,1,2", string.Join(",", plays.Select(play => play.PlayIndex)), "native replay retains ordered play indices");
        EngineSelfTests.Equal(true, plays.All(play => play.PlayCount == 3), "native history records the complete replay count");
        EngineSelfTests.Equal(1, CombatManager.Instance.History.Entries.OfType<CardExhaustedEntry>().Count(entry => ReferenceEquals(entry.Card, card)), "replay exhausts its physical source once");
        EngineSelfTests.Equal(1, PileType.Exhaust.GetPile(fixture.Player).Cards.Count(existing => ReferenceEquals(existing, card)), "replayed source occupies one exhaust pile slot");
    }

    private static DuanYuCard[] GeneratedSwords()
        => CombatManager.Instance.History.Entries.OfType<CardGeneratedEntry>().Select(entry => entry.Card).OfType<DuanYuCard>().Where(DuanYuState.IsSword).ToArray();

    private static string[] SwordPair(Rng rng)
    {
        var first = rng.NextInt(6);
        var second = rng.NextInt(5);
        if (second >= first) second++;
        return [$"DY_T{first + 1:00}", $"DY_T{second + 1:00}"];
    }

    private static async Task<TestFixture> ReadyFixture(string seed)
    {
        var fixture = EngineSelfTests.CreateFixture(seed: seed);
        await Hook.BeforeCombatStart(fixture.Run, fixture.Combat);
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [fixture.Player.Creature]);
        await Hook.BeforeHandDraw(fixture.Combat, fixture.Player, new ThrowingPlayerChoiceContext());
        foreach (var pile in fixture.Player.PlayerCombatState!.AllPiles) pile.Clear();
        for (var index = 0; index < 20; index++) EngineSelfTests.AddCard(fixture, "DY_B01", PileType.Draw);
        return fixture;
    }

    private static async Task SetQi(TestFixture fixture, int amount)
    {
        DuanYuState.Get(fixture.Player).Qi = 0;
        await PowerCmd.Remove<QiPower>(fixture.Player.Creature);
        await DuanYuState.GainQi(fixture.Player, amount);
    }
}
#endif
