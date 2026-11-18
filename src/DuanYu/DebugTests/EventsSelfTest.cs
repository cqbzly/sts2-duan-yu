#if DEBUG
using DuanYu.Events;
using DuanYu.Character;
using DuanYu.Relics;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

namespace DuanYu.DebugTests;

internal static class EventsSelfTest
{
    internal static async Task Run()
    {
        await CheckAvailability();
        await CheckSpectators();
        await CheckLockedOptions();
        await CheckStudyChoice();
        await CheckUpgradeChoice();
        await CheckRemovalChoice();
        await CheckToadRelic();
        await CheckConfirmationRevalidation();
        await CheckPlayerIsolation();
    }

    private static Player CreatePlayer(bool duanYu = true)
    {
        var fixture = EngineSelfTests.CreateFixture(duanYu, combat: false);
        return fixture.Player;
    }

    private static async Task<T> Begin<T>(Player player) where T : DuanYuEvent
    {
        var model = (T)ModelDb.Event<T>().ToMutable();
        await model.BeginEvent(player, null, false);
        return model;
    }

    private static void Check(bool condition, string context) => EngineSelfTests.Equal(true, condition, context);

    private static async Task CheckAvailability()
    {
        Player player = CreatePlayer();
        var run = (RunState)player.RunState;
        StoneChamber stone = ModelDb.Event<StoneChamber>();
        Check(stone.IsAllowed(run), "Event: stone available in act one");
        run.CurrentActIndex = 1;
        Check(stone.IsAllowed(run), "Event: stone available in act two");
        run.CurrentActIndex = 2;
        Check(!stone.IsAllowed(run), "Event: stone excluded from act three");
        run.CurrentActIndex = 0;
        player.Creature.SetCurrentHpInternal(0);
        Check(!stone.IsAllowed(run), "Event: dead Duan Yu cannot cause event");
        player.Creature.SetCurrentHpInternal(72);
        StoneChamber instance = await Begin<StoneChamber>(player);
        Check(DuanYuState.HasVisitedEvent(player, instance.Id.Entry), "Event: visited recorded before choice");
        Check(!stone.IsAllowed(run), "Event: visited event is excluded from pool");
        await instance.CurrentOptions[2].Chosen();
        Check(instance.IsFinished, "Event: free leave finishes event");
        StoneChamber repeated = await Begin<StoneChamber>(player);
        EngineSelfTests.Equal(1, repeated.CurrentOptions.Count, "Event: repeated event only offers leave");
    }

    private static async Task CheckSpectators()
    {
        Player player = CreatePlayer(false);
        Check(!ModelDb.Event<StoneChamber>().IsAllowed(player.RunState), "Event: other character excluded from stone pool");
        Check(!ModelDb.Event<GroveToad>().IsAllowed(player.RunState), "Event: other character excluded from toad pool");
        StoneChamber stone = await Begin<StoneChamber>(player);
        GroveToad toad = await Begin<GroveToad>(player);
        EngineSelfTests.Equal(1, stone.CurrentOptions.Count, "Event: stone spectator only leaves");
        EngineSelfTests.Equal(1, toad.CurrentOptions.Count, "Event: toad spectator only leaves");
        int gold = player.Gold;
        int health = player.Creature.CurrentHp;
        await stone.CurrentOptions[0].Chosen();
        await toad.CurrentOptions[0].Chosen();
        EngineSelfTests.Equal(gold, player.Gold, "Event: spectator keeps gold");
        EngineSelfTests.Equal(health, player.Creature.CurrentHp, "Event: spectator keeps health");
    }

    private static async Task CheckLockedOptions()
    {
        Player player = CreatePlayer();
        player.Creature.SetCurrentHpInternal(8);
        player.Gold = 69;
        StoneChamber stone = await Begin<StoneChamber>(player);
        Check(stone.CurrentOptions[0].IsLocked, "Event: study locked at eight health");
        Check(stone.CurrentOptions[1].IsLocked, "Event: upgrade locked below seventy gold");
        Check(!stone.CurrentOptions[2].IsLocked, "Event: leave remains unlocked");

        player = CreatePlayer();
        player.Deck.Clear(true);
        stone = await Begin<StoneChamber>(player);
        Check(stone.CurrentOptions[1].IsLocked, "Event: upgrade locked without cards");
        GroveToad toad = await Begin<GroveToad>(player);
        Check(toad.CurrentOptions[1].IsLocked, "Event: removal locked without cards");

        player = CreatePlayer();
        player.Creature.SetCurrentHpInternal(10);
        toad = await Begin<GroveToad>(player);
        Check(toad.CurrentOptions[0].IsLocked, "Event: rescue locked at ten health");
        Check(!toad.CurrentOptions[1].IsLocked, "Event: removal available above eight health");

        player = CreatePlayer();
        player.Creature.SetCurrentHpInternal(8);
        toad = await Begin<GroveToad>(player);
        Check(toad.CurrentOptions[1].IsLocked, "Event: removal locked at eight health");
    }

    private static async Task CheckStudyChoice()
    {
        Player player = CreatePlayer();
        StoneChamber stone = await Begin<StoneChamber>(player);
        int health = player.Creature.CurrentHp;
        int deckCount = player.Deck.Cards.Count;
        var selector = new EventSelector { Cancellations = 2 };
        using var scope = RunManager.Instance.CardSelectorStack.PushSelector(player, selector);
        var option = stone.CurrentOptions[0];
        await option.Chosen();
        await option.Chosen();
        EngineSelfTests.Equal(health, player.Creature.CurrentHp, "Event: study cancellation preserves health");
        EngineSelfTests.Equal(deckCount, player.Deck.Cards.Count, "Event: study cancellation preserves deck");
        Check(!stone.IsFinished, "Event: study cancellation permits another choice");
        EngineSelfTests.Equal(0, selector.LastMinSelect, "Event: reward grid allows empty selection");
        EngineSelfTests.Equal(3, selector.Seen[0].Count, "Event: study offers three cards");
        EngineSelfTests.Equal(3, selector.Seen[0].Select(card => card.Id).Distinct().Count(), "Event: study cards have distinct IDs");
        Check(selector.Seen[0].All(card => card.Rarity == CardRarity.Uncommon && card.Type == CardType.Skill), "Event: study only offers uncommon skills");
        Check(selector.Seen[0].SequenceEqual(selector.Seen[1]), "Event: canceled study does not reroll candidates");
        await option.Chosen();
        EngineSelfTests.Equal(health - 8, player.Creature.CurrentHp, "Event: study charges eight health once");
        EngineSelfTests.Equal(deckCount + 1, player.Deck.Cards.Count, "Event: study adds exactly one card");
        Check(stone.IsFinished, "Event: study finishes after purchase");
        await option.Chosen();
        EngineSelfTests.Equal(health - 8, player.Creature.CurrentHp, "Event: repeated study cannot charge twice");
        EngineSelfTests.Equal(deckCount + 1, player.Deck.Cards.Count, "Event: repeated study cannot grant twice");
    }

    private static async Task CheckUpgradeChoice()
    {
        Player player = CreatePlayer();
        StoneChamber stone = await Begin<StoneChamber>(player);
        int gold = player.Gold;
        var selector = new EventSelector { Cancellations = 1 };
        using var scope = RunManager.Instance.CardSelectorStack.PushSelector(player, selector);
        var option = stone.CurrentOptions[1];
        await option.Chosen();
        EngineSelfTests.Equal(gold, player.Gold, "Event: upgrade cancellation preserves gold");
        Check(!stone.IsFinished, "Event: canceled upgrade remains available");
        await option.Chosen();
        EngineSelfTests.Equal(gold - 70, player.Gold, "Event: upgrade charges seventy gold");
        Check(selector.Seen[^1][0].IsUpgraded, "Event: confirmed card is upgraded");
        Check(stone.IsFinished, "Event: confirmed upgrade finishes event");
    }

    private static async Task CheckRemovalChoice()
    {
        Player player = CreatePlayer();
        GroveToad toad = await Begin<GroveToad>(player);
        int health = player.Creature.CurrentHp;
        int deckCount = player.Deck.Cards.Count;
        var selector = new EventSelector { Cancellations = 1 };
        using var scope = RunManager.Instance.CardSelectorStack.PushSelector(player, selector);
        var option = toad.CurrentOptions[1];
        await option.Chosen();
        EngineSelfTests.Equal(health, player.Creature.CurrentHp, "Event: removal cancellation preserves health");
        EngineSelfTests.Equal(deckCount, player.Deck.Cards.Count, "Event: removal cancellation preserves deck");
        await option.Chosen();
        EngineSelfTests.Equal(health - 8, player.Creature.CurrentHp, "Event: removal charges eight health");
        EngineSelfTests.Equal(deckCount - 1, player.Deck.Cards.Count, "Event: removal deletes one permanent card");
        Check(toad.IsFinished, "Event: confirmed removal finishes event");
    }

    private static async Task CheckToadRelic()
    {
        Player player = CreatePlayer();
        GroveToad toad = await Begin<GroveToad>(player);
        int health = player.Creature.CurrentHp;
        await toad.CurrentOptions[0].Chosen();
        EngineSelfTests.Equal(health - 10, player.Creature.CurrentHp, "Event: rescue charges ten health");
        EngineSelfTests.Equal(1, player.Relics.OfType<VermilionToadEssence>().Count(), "Event: rescue grants toad relic");
        var relic = player.Relics.OfType<VermilionToadEssence>().Single();
        await RelicCmd.Remove(relic);
        Check(DuanYuState.HasAcquiredRelic(player, relic.Id.Entry), "Event: relic acquisition survives removal");

        player = CreatePlayer();
        DuanYuState.RecordRelic(player, "CQBZLY_DY_RELIC08");
        toad = await Begin<GroveToad>(player);
        Check(toad.CurrentOptions[0].IsLocked, "Event: previously acquired toad cannot be claimed again");
    }

    private static async Task CheckConfirmationRevalidation()
    {
        Player player = CreatePlayer();
        StoneChamber stone = await Begin<StoneChamber>(player);
        var selector = new EventSelector { BeforeReturn = () => player.Gold = 69 };
        using (RunManager.Instance.CardSelectorStack.PushSelector(player, selector))
            await stone.CurrentOptions[1].Chosen();
        EngineSelfTests.Equal(69, player.Gold, "Event: upgrade rechecks gold after selection");
        Check(!selector.Seen[0][0].IsUpgraded, "Event: invalidated upgrade grants no benefit");

        player = CreatePlayer();
        stone = await Begin<StoneChamber>(player);
        int deckCount = player.Deck.Cards.Count;
        selector = new EventSelector { BeforeReturn = () => player.Creature.SetCurrentHpInternal(8) };
        using (RunManager.Instance.CardSelectorStack.PushSelector(player, selector))
            await stone.CurrentOptions[0].Chosen();
        EngineSelfTests.Equal(8, player.Creature.CurrentHp, "Event: study rechecks health after selection");
        EngineSelfTests.Equal(deckCount, player.Deck.Cards.Count, "Event: invalidated study grants no card");
    }

    private static async Task CheckPlayerIsolation()
    {
        Player player = CreatePlayer();
        Player teammate = Player.CreateForNewRun<DuanYuCharacter>(UnlockState.all, 2);
        ((RunState)player.RunState).AddPlayerDebug(teammate, -1);
        StoneChamber stone = await Begin<StoneChamber>(player);
        GroveToad toad = await Begin<GroveToad>(teammate);
        int playerHealth = player.Creature.CurrentHp;
        int teammateHealth = teammate.Creature.CurrentHp;
        int playerCards = player.Deck.Cards.Count;
        int teammateCards = teammate.Deck.Cards.Count;
        using (RunManager.Instance.CardSelectorStack.PushSelector(player, new EventSelector()))
            await stone.CurrentOptions[0].Chosen();
        EngineSelfTests.Equal(teammateHealth, teammate.Creature.CurrentHp, "Event: study does not charge teammate health");
        EngineSelfTests.Equal(teammateCards, teammate.Deck.Cards.Count, "Event: study does not change teammate deck");
        using (RunManager.Instance.CardSelectorStack.PushSelector(teammate, new EventSelector()))
            await toad.CurrentOptions[1].Chosen();
        EngineSelfTests.Equal(playerHealth - 8, player.Creature.CurrentHp, "Event: teammate removal does not charge player");
        EngineSelfTests.Equal(playerCards + 1, player.Deck.Cards.Count, "Event: teammate removal does not change player deck");
        EngineSelfTests.Equal(teammateHealth - 8, teammate.Creature.CurrentHp, "Event: teammate removal charges its owner");
        EngineSelfTests.Equal(teammateCards - 1, teammate.Deck.Cards.Count, "Event: teammate removal changes its owner deck");
    }

    private sealed class EventSelector : ICardSelector
    {
        public int Cancellations { get; set; }
        public Action? BeforeReturn { get; init; }
        public List<List<CardModel>> Seen { get; } = [];
        public int LastMinSelect { get; private set; }

        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
        {
            List<CardModel> cards = options.ToList();
            Seen.Add(cards);
            LastMinSelect = minSelect;
            BeforeReturn?.Invoke();
            if (Cancellations-- > 0) return Task.FromResult<IEnumerable<CardModel>>([]);
            return Task.FromResult<IEnumerable<CardModel>>(cards.Take(1).ToList());
        }

        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options, IReadOnlyList<CardRewardAlternative> alternatives) =>
            new() { card = options.FirstOrDefault()?.Card };
    }
}
#endif
