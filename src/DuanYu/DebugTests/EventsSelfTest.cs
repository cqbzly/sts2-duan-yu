#if DEBUG
using DuanYu.Events;
using DuanYu.Character;
using DuanYu.Relics;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
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

    internal static async Task CheckToadProtection()
    {
        var fixture = EngineSelfTests.CreateFixture();
        await RelicCmd.Obtain<VermilionToadEssence>(fixture.Player);
        await Hook.BeforeCombatStart(fixture.Run, fixture.Combat);
        var target = fixture.Player.Creature;
        var context = new ThrowingPlayerChoiceContext();
        var relic = fixture.Player.Relics.OfType<VermilionToadEssence>().Single();
        var ward = target.GetPower<VermilionWardPower>()!;
        string rule = relic.DynamicDescription.GetFormattedText();
        string[] protectedEffects = LocManager.Instance.Language == "eng"
            ? ["Frail", "Weak", "Vulnerable"] : ["脆弱", "虚弱", "易伤"];
        Check(protectedEffects.All(effect => rule.Contains(effect)), "toad relic tooltip names all three protected debuffs");
        Check(ward.Description.GetFormattedText() == rule, "toad power preview matches the relic tooltip");
        Check(ward.SmartDescription.GetRawText().Contains(rule), "toad in-combat tooltip includes the same protection rule");
        EngineSelfTests.Equal(3, target.GetPowerAmount<VermilionWardPower>(), "toad starts combat with three protections");

        await PowerCmd.Apply<PoisonPower>(context, target, 2, fixture.Enemy, null);
        EngineSelfTests.Equal(2, target.GetPowerAmount<PoisonPower>(), "toad allows other debuffs");
        EngineSelfTests.Equal(3, target.GetPowerAmount<VermilionWardPower>(), "an unrelated debuff spends no protection");

        await PowerCmd.Apply<ArtifactPower>(context, target, 1, target, null);
        await PowerCmd.Apply<FrailPower>(context, target, 2, fixture.Enemy, null);
        EngineSelfTests.Equal(0, target.GetPowerAmount<FrailPower>(), "Artifact blocks Frail first");
        EngineSelfTests.Equal(0, target.GetPowerAmount<ArtifactPower>(), "Artifact supplies its own protection");
        EngineSelfTests.Equal(3, target.GetPowerAmount<VermilionWardPower>(), "Artifact leaves all toad protections available");

        await PowerCmd.Apply<FrailPower>(context, target, 3, fixture.Enemy, null);
        EngineSelfTests.Equal(0, target.GetPowerAmount<FrailPower>(), "toad blocks the entire first Frail application");
        EngineSelfTests.Equal(2, target.GetPowerAmount<VermilionWardPower>(), "blocking Frail spends one protection");
        await PowerCmd.Apply<FrailPower>(context, target, 2, fixture.Enemy, null);
        EngineSelfTests.Equal(2, target.GetPowerAmount<FrailPower>(), "later Frail applies normally while other protections remain");

        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [target]);
        await PowerCmd.Apply<FrailPower>(context, target, 1, fixture.Enemy, null);
        EngineSelfTests.Equal(3, target.GetPowerAmount<FrailPower>(), "starting a new turn does not restore Frail protection");

        await PowerCmd.Apply<WeakPower>(context, target, 3, fixture.Enemy, null);
        EngineSelfTests.Equal(0, target.GetPowerAmount<WeakPower>(), "toad independently blocks the first Weak");
        EngineSelfTests.Equal(1, target.GetPowerAmount<VermilionWardPower>(), "blocking Weak leaves the Vulnerable protection");
        await PowerCmd.Apply<WeakPower>(context, target, 2, fixture.Enemy, null);
        EngineSelfTests.Equal(2, target.GetPowerAmount<WeakPower>(), "later Weak applies normally");

        await PowerCmd.Apply<VulnerablePower>(context, target, 3, fixture.Enemy, null);
        EngineSelfTests.Equal(0, target.GetPowerAmount<VulnerablePower>(), "toad independently blocks the first Vulnerable");
        EngineSelfTests.Equal(0, target.GetPowerAmount<VermilionWardPower>(), "toad has no protections after blocking all three kinds");
        await PowerCmd.Apply<VulnerablePower>(context, target, 2, fixture.Enemy, null);
        EngineSelfTests.Equal(2, target.GetPowerAmount<VulnerablePower>(), "later Vulnerable applies normally");

        await Hook.BeforeCombatStart(fixture.Run, fixture.Combat);
        EngineSelfTests.Equal(3, target.GetPowerAmount<VermilionWardPower>(), "combat start restores all three protections");
        await PowerCmd.Apply<FrailPower>(context, target, 1, fixture.Enemy, null);
        EngineSelfTests.Equal(3, target.GetPowerAmount<FrailPower>(), "restored protection also blocks a new application on an existing debuff");
        EngineSelfTests.Equal(2, target.GetPowerAmount<VermilionWardPower>(), "restored Frail protection spends one use");
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
