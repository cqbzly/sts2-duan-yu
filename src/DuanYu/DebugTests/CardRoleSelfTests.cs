#if DEBUG
using DuanYu.Cards;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
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
                yield return ("roles:breath-draw-and-fixed-qi" + suffix, () => BreathCycle(upgraded));
                yield return ("roles:first-step-lingbo-upgrade" + suffix, () => FirstStep(upgraded));
                yield return ("roles:step-lingbo-and-next-turn-draw" + suffix, () => StepCycle(upgraded));
                yield return ("roles:weave-conditional-draw" + suffix, () => Weave(upgraded));
                yield return ("roles:retreat-and-advance-next-turn-energy" + suffix, () => RetreatAndAdvance(upgraded));
                yield return ("roles:turn-in-the-gap-creates-stagnation" + suffix, () => TurnInTheGap(upgraded));
                yield return ("roles:dual-swords-exhaust-retrieval" + suffix, () => ExhaustRetrieval(upgraded));
                yield return ("roles:continuous-strike-native-replay-draw" + suffix, () => ContinuousStrike(upgraded));
                yield return ("roles:reserve-strength-fatal-qi" + suffix, () => ReserveStrength(upgraded));
            }
        }
    }

    private static async Task ReserveStrength(bool upgraded)
    {
        int damage = upgraded ? 13 : 10;
        int reward = upgraded ? 3 : 2;
        foreach (bool lethal in new[] { true, false })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            var survivor = fixture.Combat.CreateCreature(ModelDb.Monster<BigDummy>().ToMutable(), CombatSide.Enemy, null);
            fixture.Combat.AddCreature(survivor);
            int otherHp = survivor.CurrentHp;
            await CreatureCmd.SetCurrentHp(fixture.Enemy, lethal ? damage : damage + 1);
            var card = EngineSelfTests.AddCard(fixture, "DY_U31", PileType.Hand, upgraded);
            int initialQi = DuanYuState.Get(fixture.Player).Qi;
            EngineSelfTests.Equal(TargetType.AnyEnemy, card.TargetType, "reserve strength targets one enemy");
            EngineSelfTests.Equal((decimal)damage, card.DynamicVars.Damage.BaseValue, "reserve strength printed damage");
            await BalanceSelfTests.PaidPlay(fixture, card);
            EngineSelfTests.Equal(lethal ? 0 : 1, fixture.Enemy.CurrentHp, "reserve strength exact lethal threshold");
            EngineSelfTests.Equal(otherHp, survivor.CurrentHp, "reserve strength leaves the second enemy alone");
            EngineSelfTests.Equal(initialQi + (lethal ? reward : 0), DuanYuState.Get(fixture.Player).Qi, "only a fatal hit grants the upgraded or base Qi amount");
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
        EngineSelfTests.Equal(upgraded ? 2 : 1, PileType.Hand.GetPile(fixture.Player).Cards.Count, "breath draws one more card when upgraded");
        EngineSelfTests.Equal(2, DuanYuState.Get(fixture.Player).Qi, "breath grants exactly two Qi at either upgrade level");
        EngineSelfTests.Equal(0, fixture.Player.Creature.Block, "breath no longer repeats the starting block skill");
        EngineSelfTests.Equal(false, source.GainsBlock, "breath no longer advertises block to engine hooks");
        EngineSelfTests.Equal(PileType.Discard, source.Pile!.Type, "breath remains reusable");
    }

    private static async Task StepCycle(bool upgraded)
    {
        var fixture = await Ready();
        var source = EngineSelfTests.AddCard(fixture, "DY_C11", PileType.Hand, upgraded);
        await BalanceSelfTests.PaidPlay(fixture, source);
        var hand = PileType.Hand.GetPile(fixture.Player);
        EngineSelfTests.Equal(0, hand.Cards.Count, "step defers its draw to the next turn");
        EngineSelfTests.Equal(upgraded ? 2 : 1, DuanYuState.Get(fixture.Player).Lingbo, "step upgrades its immediate Lingbo");
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [fixture.Player.Creature]);
        await Hook.BeforeHandDraw(fixture.Combat, fixture.Player, new ThrowingPlayerChoiceContext());
        EngineSelfTests.Equal(2, hand.Cards.Count, "step draws two on the next turn");
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [fixture.Player.Creature]);
        await Hook.BeforeHandDraw(fixture.Combat, fixture.Player, new ThrowingPlayerChoiceContext());
        EngineSelfTests.Equal(2, hand.Cards.Count, "step's additional draw resolves only once");
    }

    private static async Task Weave(bool upgraded)
    {
        foreach (int initialLingbo in new[] { 0, 1 })
        {
            var fixture = await Ready();
            await DuanYuState.GainLingbo(fixture.Player, initialLingbo);
            int health = fixture.Enemy.CurrentHp;
            var source = await EngineSelfTests.PaidPlay(fixture, "DY_C13", upgraded);
            EngineSelfTests.Equal(upgraded ? 10 : 7, health - fixture.Enemy.CurrentHp, "weave's printed damage resolves");
            EngineSelfTests.Equal(initialLingbo + 1, DuanYuState.Get(fixture.Player).Lingbo, "weave always grants one Lingbo");
            EngineSelfTests.Equal(initialLingbo, PileType.Hand.GetPile(fixture.Player).Cards.Count, "weave draws once only when Lingbo was already present");
            EngineSelfTests.Equal(false, source.GainsBlock, "weave does not advertise a block effect");
        }
    }

    private static async Task RetreatAndAdvance(bool upgraded)
    {
        var fixture = await Ready();
        var combat = fixture.Player.PlayerCombatState!;
        await EngineSelfTests.PaidPlay(fixture, "DY_C16", upgraded);
        EngineSelfTests.Equal(2, combat.Energy, "retreat and advance only spends its cost on this turn");
        EngineSelfTests.Equal(upgraded ? 9 : 6, fixture.Player.Creature.Block, "retreat and advance grants its printed block");
        EngineSelfTests.Equal(1, fixture.Player.Creature.GetPowerAmount<EnergyNextTurnPower>(), "retreat and advance schedules one energy");
        combat.Energy = 3;
        await Hook.AfterEnergyReset(fixture.Combat, fixture.Player);
        EngineSelfTests.Equal(4, combat.Energy, "scheduled energy is granted after the next reset");
        EngineSelfTests.Equal(0, fixture.Player.Creature.GetPowerAmount<EnergyNextTurnPower>(), "next-turn energy resolves only once");
    }

    private static async Task FirstStep(bool upgraded)
    {
        var fixture = await Ready();
        await EngineSelfTests.PaidPlay(fixture, "DY_B04", upgraded);
        EngineSelfTests.Equal(upgraded ? 2 : 1, DuanYuState.Get(fixture.Player).Lingbo, "first step gains the upgraded or base Lingbo amount");
        EngineSelfTests.Equal(upgraded ? 6 : 4, fixture.Player.Creature.Block, "first step retains its block");
    }

    private static async Task TurnInTheGap(bool upgraded)
    {
        var fixture = await Ready();
        await DuanYuState.GainQi(fixture.Player, 3);
        int health = fixture.Enemy.CurrentHp;
        await EngineSelfTests.PaidPlay(fixture, "DY_U21", upgraded);

        var hand = PileType.Hand.GetPile(fixture.Player).Cards;
        EngineSelfTests.Equal(1, hand.OfType<DY_S01>().Count(), "turn in the gap adds one stagnation to the hand");
        EngineSelfTests.Equal(upgraded ? 3 : 2, hand.Count, "turn in the gap draws before adding stagnation");
        EngineSelfTests.Equal(2, DuanYuState.Get(fixture.Player).Lingbo, "turn in the gap retains its Lingbo gain");
        EngineSelfTests.Equal(upgraded ? 14 : 10, health - fixture.Enemy.CurrentHp, "turn in the gap retains its damage");
        EngineSelfTests.Equal(3, DuanYuState.Get(fixture.Player).Qi, "generating stagnation does not immediately consume Qi");
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
        EngineSelfTests.Equal(0, ExhaustCount(source), "retrieval source remains reusable");
        EngineSelfTests.Equal(PileType.Discard, source.Pile!.Type, "retrieval source goes to discard");
        EngineSelfTests.Equal(0, fixture.Player.Creature.Block, "moving swords does not trigger exhaust block");
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
        EngineSelfTests.Equal(0, ExhaustCount(replayed), "native replay does not exhaust the retrieval card");
        EngineSelfTests.Equal(replayed, PileType.Discard.GetPile(full.Player).Cards.Single(), "only the played retrieval card goes to discard");
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
