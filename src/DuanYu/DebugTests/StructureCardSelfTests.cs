#if DEBUG
using DuanYu.Cards;
using DuanYu.Relics;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace DuanYu.DebugTests;

internal static class StructureCardSelfTests
{
    internal static IEnumerable<(string Name, Func<Task> Action)> Cases
    {
        get
        {
            foreach (var upgraded in new[] { false, true })
            {
                var suffix = upgraded ? "+" : "";
                yield return ("structure:discard-retrieval-choice-and-fallback" + suffix, () => DiscardRetrieval(upgraded));
                yield return ("structure:draw-pile-search-filter-and-identity" + suffix, () => DrawPileSearch(upgraded));
                yield return ("structure:zero-cost-attack-generates-hand-fuel" + suffix, () => StatusAttack(upgraded));
                yield return ("structure:three-cost-power-status-cycle-and-turn-limit" + suffix, () => StatusPower(upgraded));
                yield return ("structure:x-cost-chemical-x-and-block-modifiers" + suffix, () => XModifiers(upgraded));
            }
            yield return ("structure:status-power-stacks-without-resetting-limit", StatusPowerStacks);
            yield return ("structure:status-power-hand-overflow", StatusPowerOverflow);
        }
    }

    private static async Task DiscardRetrieval(bool upgraded)
    {
        foreach (var desiredId in new[] { "DY_U08", "DY_S02" })
        {
            var fixture = await Ready();
            var desired = EngineSelfTests.AddCard(fixture, desiredId, PileType.Discard, desiredId == "DY_U08");
            var other = EngineSelfTests.AddCard(fixture, "DY_B02", PileType.Discard);
            var source = EngineSelfTests.AddCard(fixture, "DY_C22", PileType.Hand, upgraded);
            using var selector = RunManager.Instance.CardSelectorStack.PushSelector(fixture.Player, new IdSelector(desiredId));
            await PaidPlay(fixture, source);
            EngineSelfTests.Equal(PileType.Hand, desired.Pile!.Type, "retrieval moves the chosen original object, including statuses");
            EngineSelfTests.Equal(desiredId == "DY_U08", desired.IsUpgraded, "retrieval preserves the chosen card's upgrade");
            EngineSelfTests.Equal(PileType.Discard, other.Pile!.Type, "retrieval leaves the unchosen card in discard");
            EngineSelfTests.Equal(PileType.Exhaust, source.Pile!.Type, "retrieval exhaust prevents reusable retrieval loops");
            EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).Qi, "retrieval always grants one Qi");
        }
        var empty = await Ready();
        await PaidPlay(empty, EngineSelfTests.AddCard(empty, "DY_C22", PileType.Hand, upgraded));
        EngineSelfTests.Equal(upgraded ? 2 : 1, PileType.Hand.GetPile(empty.Player).Cards.Count, "empty discard falls back to the printed draw count");
        EngineSelfTests.Equal(1, DuanYuState.Get(empty.Player).Qi, "empty discard still grants one Qi");
    }

    private static async Task DrawPileSearch(bool upgraded)
    {
        var fixture = await Ready();
        PileType.Draw.GetPile(fixture.Player).Clear();
        var excluded = EngineSelfTests.AddCard(fixture, "DY_S02", PileType.Draw);
        var desired = EngineSelfTests.AddCard(fixture, "DY_R05", PileType.Draw, true);
        var other = EngineSelfTests.AddCard(fixture, "DY_B01", PileType.Draw);
        var source = EngineSelfTests.AddCard(fixture, "DY_U38", PileType.Hand, upgraded);
        using var selector = RunManager.Instance.CardSelectorStack.PushSelector(fixture.Player, new IdSelector("DY_R05", true));
        await PaidPlay(fixture, source);
        EngineSelfTests.Equal(PileType.Hand, desired.Pile!.Type, "search moves the selected upgraded card from draw to hand");
        EngineSelfTests.Equal(true, desired.IsUpgraded, "search preserves the original card's upgrades");
        EngineSelfTests.Equal(PileType.Draw, excluded.Pile!.Type, "search cannot select a status");
        EngineSelfTests.Equal(PileType.Draw, other.Pile!.Type, "search does not draw an unrelated card");
        EngineSelfTests.Equal(upgraded ? 2 : 1, DuanYuState.Get(fixture.Player).Qi, "search grants printed Qi");
        EngineSelfTests.Equal(PileType.Exhaust, source.Pile!.Type, "search source exhausts");

        var empty = await Ready();
        PileType.Draw.GetPile(empty.Player).Clear();
        EngineSelfTests.AddCard(empty, "DY_S01", PileType.Draw);
        await PaidPlay(empty, EngineSelfTests.AddCard(empty, "DY_U38", PileType.Hand, upgraded));
        EngineSelfTests.Equal(0, PileType.Hand.GetPile(empty.Player).Cards.Count, "no legal search target resolves without a choice or softlock");
        EngineSelfTests.Equal(upgraded ? 2 : 1, DuanYuState.Get(empty.Player).Qi, "search with no eligible card retains its Qi effect");
    }

    private static async Task StatusAttack(bool upgraded)
    {
        foreach (var fullHand in new[] { false, true })
        {
            var fixture = await Ready();
            fixture.Player.PlayerCombatState!.Energy = 0;
            if (fullHand)
                for (var index = 0; index < 10; index++) EngineSelfTests.AddCard(fixture, "DY_B02", PileType.Hand);
            var source = EngineSelfTests.AddCard(fixture, "DY_C23", PileType.Hand, upgraded);
            var health = fixture.Enemy.CurrentHp;
            await PaidPlay(fixture, source);
            EngineSelfTests.Equal(upgraded ? 14 : 10, health - fixture.Enemy.CurrentHp, "zero-cost attack deals its printed damage");
            var pile = fullHand ? PileType.Discard : PileType.Hand;
            EngineSelfTests.Equal(1, pile.GetPile(fixture.Player).Cards.Count(card => card is DY_S01), "attack-generated fuel follows native hand overflow");
            EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SkillsPlayed, "zero-cost attack does not trigger a skill resource rebate");
            EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "creating a hand status does not immediately gain or lose Qi");
        }
    }

    private static async Task StatusPower(bool upgraded)
    {
        var fixture = await Ready();
        fixture.Player.PlayerCombatState!.Energy = 3;
        var source = EngineSelfTests.AddCard(fixture, "DY_R22", PileType.Hand, upgraded);
        await PaidPlay(fixture, source);
        EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, "three-cost ability pays three energy at either upgrade tier");
        EngineSelfTests.Equal(0, fixture.Player.Creature.Block, "ability's reactive block is not immediate block");
        var fuel = PileType.Hand.GetPile(fixture.Player).Cards.OfType<DY_S02>().Single();
        fixture.Player.PlayerCombatState.Energy = 4;
        await PaidPlay(fixture, fuel);
        EngineSelfTests.Equal(3, DuanYuState.Get(fixture.Player).Qi, "playing foreign Qi also triggers its actual exhaust reward");
        EngineSelfTests.Equal(upgraded ? 7 : 5, fixture.Player.Creature.Block, "first exhausted status grants ability block");
        EngineSelfTests.Equal(1, PileType.Hand.GetPile(fixture.Player).Cards.Count(DuanYuState.IsSword), "first exhausted status generates one sword");
        var second = EngineSelfTests.AddCard(fixture, "DY_S01", PileType.Hand);
        await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), second);
        var third = (Wither)fixture.Combat.CreateCard(ModelDb.Card<Wither>(), fixture.Player);
        PileType.Hand.GetPile(fixture.Player).AddInternal(third);
        await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), third);
        EngineSelfTests.Equal(2 * (upgraded ? 7 : 5), fixture.Player.Creature.Block, "third status gives no additional reward");
        EngineSelfTests.Equal(4, DuanYuState.Get(fixture.Player).Qi, "only two status exhausts grant ability Qi");
        EngineSelfTests.Equal(2, PileType.Hand.GetPile(fixture.Player).Cards.Count(DuanYuState.IsSword), "only two status exhausts generate swords");
        await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), EngineSelfTests.AddCard(fixture, "DY_B01", PileType.Hand));
        EngineSelfTests.Equal(2, DuanYuState.Get(fixture.Player).StatusExhaustTriggers, "ordinary card exhaust does not enter the status counter");

        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [fixture.Player.Creature]);
        await Hook.BeforeHandDraw(fixture.Combat, fixture.Player, new ThrowingPlayerChoiceContext());
        var nextFuel = PileType.Hand.GetPile(fixture.Player).Cards.OfType<DY_S02>().Single();
        await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), nextFuel);
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).StatusExhaustTriggers, "new turn restores the status reward allowance");
        EngineSelfTests.Equal(3 * (upgraded ? 7 : 5), fixture.Player.Creature.Block, "new turn state fuel triggers persistent ability block");
    }

    private static async Task StatusPowerStacks()
    {
        var fixture = await Ready();
        await PaidPlay(fixture, EngineSelfTests.AddCard(fixture, "DY_R22", PileType.Hand));
        await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), EngineSelfTests.AddCard(fixture, "DY_S01", PileType.Hand));
        await PaidPlay(fixture, EngineSelfTests.AddCard(fixture, "DY_R22", PileType.Hand, true));
        await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), EngineSelfTests.AddCard(fixture, "DY_S01", PileType.Hand));
        await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), EngineSelfTests.AddCard(fixture, "DY_S01", PileType.Hand));
        EngineSelfTests.Equal(17, fixture.Player.Creature.Block, "mixed ability upgrades add block without resetting the two-trigger allowance");
        EngineSelfTests.Equal(3, DuanYuState.Get(fixture.Player).Qi, "stacked copies grant one Qi each per eligible exhaust");
        var swords = PileType.Hand.GetPile(fixture.Player).Cards.OfType<DuanYuCard>().Where(DuanYuState.IsSword).ToArray();
        EngineSelfTests.Equal(3, swords.Length, "stacked copies generate one sword each");
        var freeSword = swords.Single(sword => sword.IsUpgraded);
        EngineSelfTests.Equal(0, freeSword.RequiredQi, "only the upgraded ability produces a free sword");
        EngineSelfTests.Equal(true, swords.Where(sword => !sword.IsUpgraded).All(sword => sword.RequiredQi == 1), "base ability still produces swords that cost one Qi");
        DuanYuState.Get(fixture.Player).Qi = 0;
        fixture.Player.PlayerCombatState!.Energy = 0;
        EngineSelfTests.Equal(true, freeSword.CanPlay(), "generated upgraded sword is playable with no energy or Qi");
        await PaidPlay(fixture, freeSword);
        EngineSelfTests.Equal(PileType.Exhaust, freeSword.Pile!.Type, "the free sword resolves and exhausts normally");
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).SwordPlays, "free sword still counts as a sword play");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SpendingCount, "free sword does not count as paying Qi");
        PileType.Hand.GetPile(fixture.Player).Clear();
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [fixture.Player.Creature]);
        await Hook.BeforeHandDraw(fixture.Combat, fixture.Player, new ThrowingPlayerChoiceContext());
        EngineSelfTests.Equal(2, PileType.Hand.GetPile(fixture.Player).Cards.Count(card => card is DY_S02), "each ability copy generates its own turn-start status");
    }

    private static async Task StatusPowerOverflow()
    {
        var fixture = await Ready();
        await PaidPlay(fixture, EngineSelfTests.AddCard(fixture, "DY_R22", PileType.Hand));
        PileType.Hand.GetPile(fixture.Player).Clear();
        for (var index = 0; index < 10; index++) EngineSelfTests.AddCard(fixture, "DY_B01", PileType.Hand);
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [fixture.Player.Creature]);
        await Hook.BeforeHandDraw(fixture.Combat, fixture.Player, new ThrowingPlayerChoiceContext());
        EngineSelfTests.Equal(10, PileType.Hand.GetPile(fixture.Player).Cards.Count, "turn-start ability respects the hand limit");
        EngineSelfTests.Equal(1, PileType.Discard.GetPile(fixture.Player).Cards.Count(card => card is DY_S02), "overflow ability fuel moves to discard");
    }

    private static async Task XModifiers(bool upgraded)
    {
        var fixture = await Ready();
        await RelicCmd.Obtain<ChemicalX>(fixture.Player);
        fixture.Player.PlayerCombatState!.Energy = 0;
        var fuel = EngineSelfTests.AddCard(fixture, "DY_S01", PileType.Hand);
        var source = EngineSelfTests.AddCard(fixture, "DY_U37", PileType.Hand, upgraded);
        await PaidPlay(fixture, source);
        EngineSelfTests.Equal(0, source.EnergyCost.CapturedXValue, "Chemical X does not pretend two energy were paid");
        EngineSelfTests.Equal(2 * (upgraded ? 6 : 4), fixture.Player.Creature.Block, "Chemical X adds two to X before combined block is calculated");
        EngineSelfTests.Equal(PileType.Exhaust, fuel.Pile!.Type, "Chemical X permits status conversion at zero paid energy");
        EngineSelfTests.Equal(1, fixture.Player.PlayerCombatState.Energy, "status conversion uses actual exhaust count and fixed resolved X");

        var attack = await Ready();
        await RelicCmd.Obtain<ChemicalX>(attack.Player);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), attack.Player.Creature, 2, attack.Player.Creature, null);
        await PowerCmd.Apply<ThornsPower>(new ThrowingPlayerChoiceContext(), attack.Enemy, 1, attack.Enemy, null);
        attack.Player.PlayerCombatState!.Energy = 1;
        await DuanYuState.GainQi(attack.Player, 3);
        var volley = EngineSelfTests.AddCard(attack, "DY_R21", PileType.Hand, upgraded);
        volley.BaseReplayCount = 1;
        var health = attack.Enemy.CurrentHp;
        var playerHealth = attack.Player.Creature.CurrentHp;
        await PaidPlay(attack, volley);
        EngineSelfTests.Equal(6 * (upgraded ? 8 : 6) + 4, health - attack.Enemy.CurrentHp, "native replay reuses paid Qi X without Chemical X and applies Strength once per replay body");
        EngineSelfTests.Equal(2, playerHealth - attack.Player.Creature.CurrentHp, "native replay triggers Thorns once per combined body");
        EngineSelfTests.Equal(1, DuanYuState.Get(attack.Player).SpendingCount, "native X replay pays mandatory Qi once for the activation");
        EngineSelfTests.Equal(1, attack.Player.PlayerCombatState.Energy, "replayed Qi-X card never spends energy");
    }

    private static async Task<TestFixture> Ready()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        await RelicCmd.Remove(fixture.Player.Relics.OfType<LanghuanPendant>().Single());
        await DuanYuState.LoseQi(fixture.Player, int.MaxValue);
        return fixture;
    }

    private static async Task PaidPlay(TestFixture fixture, CardModel card)
    {
        EngineSelfTests.Equal(true, card.CanPlay(), "card has legal resources before execution");
        var energyValue = card.EnergyCost.GetAmountToSpend();
        var (energySpent, starsSpent) = await card.SpendResources();
        var target = card.TargetType == TargetType.AnyEnemy ? fixture.Enemy : null;
        await card.OnPlayWrapper(new ThrowingPlayerChoiceContext(), target, false,
            new ResourceInfo { EnergySpent = energySpent, EnergyValue = energyValue, StarsSpent = starsSpent, StarValue = 0 }, true);
    }

    private sealed class IdSelector(string desiredId, bool excludeStatuses = false) : ICardSelector
    {
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
        {
            var cards = options.ToArray();
            if (excludeStatuses) EngineSelfTests.Equal(true, cards.All(card => card.Type != CardType.Status), "search selection excludes all statuses");
            return Task.FromResult<IEnumerable<CardModel>>(cards.Where(card => DuanYuState.CardId(card) == desiredId).Take(1));
        }

        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options, IReadOnlyList<CardRewardAlternative> alternatives)
            => new() { card = options.FirstOrDefault()?.Card };
    }
}
#endif
