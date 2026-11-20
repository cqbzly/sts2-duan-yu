#if DEBUG
using DuanYu.Cards;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace DuanYu.DebugTests;

internal static class CardRoleSelfTests
{
    internal static IEnumerable<(string Name, Func<Task> Action)> Cases
    {
        get
        {
            foreach (bool upgraded in new[] { false, true })
            {
                string suffix = upgraded ? "+" : "";
                yield return ("roles:borrow-draw-then-discard" + suffix, () => BorrowCycle(upgraded));
                yield return ("roles:breath-draw-and-reduced-qi" + suffix, () => BreathCycle(upgraded));
                yield return ("roles:step-draw-and-fixed-lingbo" + suffix, () => StepCycle(upgraded));
                yield return ("roles:dual-swords-exhaust-retrieval" + suffix, () => ExhaustRetrieval(upgraded));
                yield return ("roles:continuous-strike-native-replay-draw" + suffix, () => ContinuousStrike(upgraded));
            }
        }
    }

    private static async Task BorrowCycle(bool upgraded)
    {
        var fixture = await Ready();
        var status = EngineSelfTests.AddCard(fixture, "DY_S01", PileType.Hand);
        var source = EngineSelfTests.AddCard(fixture, "DY_C02", PileType.Hand, upgraded);
        var selector = new ChoosingSelector(card => ReferenceEquals(card, status));
        using (RunManager.Instance.CardSelectorStack.PushSelector(fixture.Player, selector))
            await BalanceSelfTests.PaidPlay(fixture, source);
        int drawn = upgraded ? 3 : 2;
        EngineSelfTests.Equal(CardType.Skill, source.Type, "borrow is now a cycle skill rather than a duplicate attack");
        EngineSelfTests.Equal(TargetType.Self, source.TargetType, "borrow needs no enemy target");
        EngineSelfTests.Equal(0, source.RequiredQi, "borrow can cycle at zero Qi");
        EngineSelfTests.Equal(drawn + 1, selector.LastOptions.Length, "borrow draws its fixed amount before asking for the discard");
        EngineSelfTests.Equal(drawn, PileType.Hand.GetPile(fixture.Player).Cards.Count, "borrow retains its drawn cards after discarding the original status");
        EngineSelfTests.Equal(PileType.Discard, status.Pile!.Type, "borrow can discard an unplayable status");
        EngineSelfTests.Equal(0, ExhaustCount(status), "discarding a status does not count as exhaust");
        EngineSelfTests.Equal(PileType.Discard, source.Pile!.Type, "borrow remains reusable");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "borrow grants no Qi");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SpendingCount, "borrow records no Qi payment");
        EngineSelfTests.Equal(fixture.Enemy.MaxHp, fixture.Enemy.CurrentHp, "borrow no longer deals attack damage");

        var empty = await Ready();
        PileType.Draw.GetPile(empty.Player).Clear();
        EngineSelfTests.AddCard(empty, "DY_T01", PileType.Exhaust);
        var emptySource = EngineSelfTests.AddCard(empty, "DY_C02", PileType.Hand, upgraded);
        var noChoice = new ChoosingSelector();
        using (RunManager.Instance.CardSelectorStack.PushSelector(empty.Player, noChoice))
            await BalanceSelfTests.PaidPlay(empty, emptySource);
        EngineSelfTests.Equal(0, noChoice.Calls, "borrow safely finishes without a discard choice when no card can be drawn");
        EngineSelfTests.Equal(0, PileType.Hand.GetPile(empty.Player).Cards.Count, "borrow does not invent fallback cards from an empty deck");
    }

    private static async Task BreathCycle(bool upgraded)
    {
        var fixture = await Ready();
        var source = EngineSelfTests.AddCard(fixture, "DY_C09", PileType.Hand, upgraded);
        await BalanceSelfTests.PaidPlay(fixture, source);
        EngineSelfTests.Equal(1, PileType.Hand.GetPile(fixture.Player).Cards.Count, "breath always draws exactly one card");
        EngineSelfTests.Equal(upgraded ? 2 : 1, DuanYuState.Get(fixture.Player).Qi, "breath grants only its reduced printed Qi");
        EngineSelfTests.Equal(0, fixture.Player.Creature.Block, "breath no longer repeats the starting block skill");
        EngineSelfTests.Equal(false, source.GainsBlock, "breath no longer advertises block to engine hooks");
        EngineSelfTests.Equal(PileType.Discard, source.Pile!.Type, "breath remains reusable");
    }

    private static async Task StepCycle(bool upgraded)
    {
        var fixture = await Ready();
        var source = EngineSelfTests.AddCard(fixture, "DY_C11", PileType.Hand, upgraded);
        await BalanceSelfTests.PaidPlay(fixture, source);
        EngineSelfTests.Equal(upgraded ? 2 : 1, PileType.Hand.GetPile(fixture.Player).Cards.Count, "step upgrades its fixed draw count");
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).Lingbo, "step grants exactly one Lingbo at either upgrade level");
        EngineSelfTests.Equal(0, fixture.Player.Creature.Block, "step no longer repeats the basic block-and-Lingbo effect");
        EngineSelfTests.Equal(false, source.GainsBlock, "step no longer advertises block");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "step supplies no new Qi");
    }

    private static async Task ExhaustRetrieval(bool upgraded)
    {
        var fixture = await Ready();
        var swords = new[]
        {
            EngineSelfTests.AddCard(fixture, "DY_T01", PileType.Exhaust, true),
            EngineSelfTests.AddCard(fixture, "DY_T02", PileType.Exhaust),
            EngineSelfTests.AddCard(fixture, "DY_T03", PileType.Exhaust, true)
        };
        var status = EngineSelfTests.AddCard(fixture, "DY_S02", PileType.Exhaust);
        var attack = EngineSelfTests.AddCard(fixture, "DY_U07", PileType.Exhaust);
        await DuanYuState.AddEffect(fixture.Player, "DY_U28", 2);
        var source = EngineSelfTests.AddCard(fixture, "DY_U13", PileType.Hand, upgraded);
        fixture.Player.PlayerCombatState!.Energy = upgraded ? 0 : 1;
        EngineSelfTests.Equal(upgraded ? 0 : 1, source.EnergyCost.GetAmountToSpend(), "dual swords costs one energy before upgrade and zero after");
        EngineSelfTests.Equal(true, source.CanPlay(), "dual swords can be played with exactly its printed energy cost");
        var selector = new ChoosingSelector();
        using (RunManager.Instance.CardSelectorStack.PushSelector(fixture.Player, selector))
            await BalanceSelfTests.PaidPlay(fixture, source);
        int count = 2;
        EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, "dual swords pays its actual energy cost without generating energy");
        EngineSelfTests.Equal(2, selector.LastOptions.Length, "dual swords offers only upgraded exhausted sword tokens");
        EngineSelfTests.Equal(true, selector.LastOptions.All(DuanYuState.IsSword), "exhaust retrieval filters out statuses and ordinary attacks");
        EngineSelfTests.Equal(count, PileType.Hand.GetPile(fixture.Player).Cards.Count, "dual swords retrieves only its printed maximum");
        EngineSelfTests.Equal(true, swords.Where(card => card.IsUpgraded).All(card => card.Pile?.Type == PileType.Hand), "retrieval returns the chosen original card objects");
        EngineSelfTests.Equal(true, swords[0].IsUpgraded, "retrieval preserves a sword's existing upgrade even with an unupgraded source");
        EngineSelfTests.Equal(PileType.Exhaust, swords[1].Pile!.Type, "ordinary sword stays exhausted");
        EngineSelfTests.Equal(PileType.Exhaust, status.Pile!.Type, "foreign-Qi status cannot be retrieved as a sword");
        EngineSelfTests.Equal(PileType.Exhaust, attack.Pile!.Type, "ordinary attack cannot be retrieved as a sword");
        EngineSelfTests.Equal(0, CombatManager.Instance.History.Entries.OfType<CardGeneratedEntry>().Count(), "retrieval generates no card copies");
        EngineSelfTests.Equal(1, ExhaustCount(source), "retrieval source exhausts once");
        EngineSelfTests.Equal(2, fixture.Player.Creature.Block, "only exhausting the source triggers exhaust block, moving swords does not");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "retrieval grants no Qi");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SwordPlays, "retrieval does not count as playing recovered swords");

        var empty = await Ready();
        EngineSelfTests.AddCard(empty, "DY_S02", PileType.Exhaust);
        EngineSelfTests.AddCard(empty, "DY_T01", PileType.Exhaust);
        var emptySource = EngineSelfTests.AddCard(empty, "DY_U13", PileType.Hand, upgraded);
        var emptySelector = new ChoosingSelector();
        using (RunManager.Instance.CardSelectorStack.PushSelector(empty.Player, emptySelector))
            await BalanceSelfTests.PaidPlay(empty, emptySource);
        EngineSelfTests.Equal(0, emptySelector.Calls, "no eligible exhausted swords requires no choice");
        EngineSelfTests.Equal(0, PileType.Hand.GetPile(empty.Player).Cards.Count, "empty retrieval has no generated fallback");

        var single = await Ready();
        var onlySword = EngineSelfTests.AddCard(single, "DY_T06", PileType.Exhaust, true);
        var singleSource = EngineSelfTests.AddCard(single, "DY_U13", PileType.Hand, upgraded);
        var singleSelector = new ChoosingSelector();
        using (RunManager.Instance.CardSelectorStack.PushSelector(single.Player, singleSelector))
            await BalanceSelfTests.PaidPlay(single, singleSource);
        EngineSelfTests.Equal(1, PileType.Hand.GetPile(single.Player).Cards.Count, "retrieval can recover a single available sword at either upgrade tier");
        EngineSelfTests.Equal(PileType.Hand, onlySword.Pile!.Type, "single available sword returns as its original object");

        var full = await Ready();
        for (int index = 0; index < 9; index++) EngineSelfTests.AddCard(full, "DY_B01", PileType.Hand);
        var recovered = EngineSelfTests.AddCard(full, "DY_T01", PileType.Exhaust, true);
        var leftBehind = EngineSelfTests.AddCard(full, "DY_T02", PileType.Exhaust, true);
        var replayed = EngineSelfTests.AddCard(full, "DY_U13", PileType.Hand, upgraded);
        replayed.BaseReplayCount = 1;
        var fullSelector = new ChoosingSelector();
        using (RunManager.Instance.CardSelectorStack.PushSelector(full.Player, fullSelector))
            await BalanceSelfTests.PaidPlay(full, replayed);
        EngineSelfTests.Equal(CardPile.MaxCardsInHand, PileType.Hand.GetPile(full.Player).Cards.Count, "retrieval obeys the hand limit even with native replay");
        EngineSelfTests.Equal(1, fullSelector.Calls, "second replay body skips selection once the hand fills");
        EngineSelfTests.Equal(PileType.Hand, recovered.Pile!.Type, "first retrieval fills the single free slot");
        EngineSelfTests.Equal(PileType.Exhaust, leftBehind.Pile!.Type, "swords without hand space remain exhausted");
        EngineSelfTests.Equal(1, ExhaustCount(replayed), "native replay exhausts the physical retrieval card only once");
        EngineSelfTests.Equal(0, PileType.Discard.GetPile(full.Player).Cards.Count, "full hand does not push unretrieved swords into discard");
    }

    private static async Task ContinuousStrike(bool upgraded)
    {
        var fixture = await Ready();
        await DuanYuState.GainQi(fixture.Player, 3);
        var source = EngineSelfTests.AddCard(fixture, "DY_U33", PileType.Hand, upgraded);
        source.BaseReplayCount = 1;
        int health = fixture.Enemy.CurrentHp;
        await BalanceSelfTests.PaidPlay(fixture, source);
        EngineSelfTests.Equal(2 * (upgraded ? 22 : 16), health - fixture.Enemy.CurrentHp, "continuous strike trades raw damage for card flow on each replay body");
        EngineSelfTests.Equal(4, PileType.Hand.GetPile(fixture.Player).Cards.Count, "continuous strike draws exactly two cards per native body");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "continuous strike still requires three Qi");
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).SpendingCount, "native replay pays the attack's Qi only once");
        EngineSelfTests.Equal(PileType.Discard, source.Pile!.Type, "continuous strike remains reusable");
    }

    private static async Task<TestFixture> Ready()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        await DuanYuState.LoseQi(fixture.Player, int.MaxValue);
        fixture.Player.PlayerCombatState!.Energy = 3;
        return fixture;
    }

    private static int ExhaustCount(CardModel card)
        => CombatManager.Instance.History.Entries.OfType<CardExhaustedEntry>().Count(entry => ReferenceEquals(entry.Card, card));

    private sealed class ChoosingSelector(Func<CardModel, bool>? filter = null) : ICardSelector
    {
        internal int Calls { get; private set; }
        internal CardModel[] LastOptions { get; private set; } = [];

        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
        {
            Calls++;
            LastOptions = options.ToArray();
            return Task.FromResult<IEnumerable<CardModel>>(LastOptions.Where(card => filter?.Invoke(card) ?? true).Take(maxSelect).ToArray());
        }

        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options, IReadOnlyList<CardRewardAlternative> alternatives)
            => new() { card = options.FirstOrDefault()?.Card };
    }
}
#endif
