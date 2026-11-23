#if DEBUG
using DuanYu.Cards;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace DuanYu.DebugTests;

internal static class AncientCardSelfTests
{
    internal static IEnumerable<(string Name, Func<Task> Action)> Cases =>
    [
        ("ancient:dusty-tome-awards-upgraded-six-swords", DustyTomeReward),
        ("ancient:registry-compendium-and-ordinary-rewards", CardRegistration),
        ("ancient:six-swords-normal-play", () => PlayAncientCard(false)),
        ("ancient:six-swords-upgraded-play", () => PlayAncientCard(true)),
        ("ancient:six-swords-replay-uses-one-payment", ReplayAncientCard)
    ];

    private static async Task DustyTomeReward()
    {
        var fixture = EngineSelfTests.CreateFixture(combat: false);
        var tome = (DustyTome)ModelDb.Relic<DustyTome>().ToMutable();
        var ancient = DuanYuCard.Canonical("DY_A01");
        int deckSize = fixture.Player.Deck.Cards.Count;
        tome.SetupForPlayer(fixture.Player);
        EngineSelfTests.Equal(ancient.Id, tome.AncientCard, "native Dusty Tome resolves a character-specific Ancient card");

        await RelicCmd.Obtain(tome, fixture.Player);

        var awarded = fixture.Player.Deck.Cards.Single(card => card.Id == ancient.Id);
        EngineSelfTests.Equal(deckSize + 1, fixture.Player.Deck.Cards.Count, "native Dusty Tome adds one permanent card");
        EngineSelfTests.Equal(true, awarded.IsUpgraded, "native Dusty Tome upgrades its card before awarding it");
        EngineSelfTests.Equal(0, awarded.EnergyCost.GetWithModifiers(CostModifiers.All), "the native award is the zero-energy upgrade");
    }

    private static Task CardRegistration()
    {
        var fixture = EngineSelfTests.CreateFixture(combat: false);
        var ancient = DuanYuCard.Canonical("DY_A01");
        var pool = fixture.Player.Character.CardPool;
        EngineSelfTests.Equal(CardRarity.Ancient, ancient.Rarity, "Six Swords uses native Ancient rarity");
        EngineSelfTests.Equal(CardType.Skill, ancient.Type, "Six Swords is a skill");
        EngineSelfTests.Equal(true, ancient.ShouldShowInCardLibrary, "the Ancient card can appear in the compendium");
        EngineSelfTests.Equal(true, pool.AllCards.Contains(ancient), "the Duan Yu compendium pool includes the Ancient card");
        EngineSelfTests.Equal(ancient.Id, pool.GetUnlockedCards(fixture.Player.UnlockState, CardMultiplayerConstraint.None)
            .Single(card => card.Rarity == CardRarity.Ancient).Id, "there is exactly one eligible Duan Yu Ancient card");

        foreach (var room in new[] { RoomType.Monster, RoomType.Elite, RoomType.Boss, RoomType.Shop })
        {
            var cards = CardFactory.CreateForReward(fixture.Player, 3, CardCreationOptions.ForRoom(fixture.Player, room)).ToArray();
            EngineSelfTests.Equal(3, cards.Length, "ordinary " + room + " rewards still contain three cards");
            EngineSelfTests.Equal(true, cards.All(option => option.Card.Rarity != CardRarity.Ancient), "ordinary " + room + " rewards exclude Ancient rarity");
        }
        return Task.CompletedTask;
    }

    private static async Task PlayAncientCard(bool upgraded)
    {
        foreach (int startingQi in new[] { 0, 5 })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            var resources = DuanYuState.Get(fixture.Player);
            await DuanYuState.LoseQi(fixture.Player, resources.Qi);
            await DuanYuState.GainQi(fixture.Player, startingQi);
            var card = EngineSelfTests.AddCard(fixture, "DY_A01", PileType.Hand, upgraded);
            fixture.Player.PlayerCombatState!.Energy = upgraded ? 0 : 1;
            await EngineSelfTests.PaidPlay(fixture, card);

            EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, "the Ancient card pays its printed energy cost");
            EngineSelfTests.Equal(0, resources.Qi, "the Ancient card spends all current Qi");
            EngineSelfTests.Equal(startingQi, card.CapturedQiX, "the Ancient card captures the actual payment");
            EngineSelfTests.Equal(startingQi > 0 ? 1 : 0, resources.SpendingCount, "only nonzero Qi counts as a payment");
            EngineSelfTests.Equal(startingQi, fixture.Player.Creature.GetPowerAmount<StrengthPower>(), "the Ancient card gains one Strength per Qi paid");
            EngineSelfTests.Equal(PileType.Exhaust, card.Pile!.Type, "the Ancient card exhausts on play");
        }
    }

    private static async Task ReplayAncientCard()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        var resources = DuanYuState.Get(fixture.Player);
        await DuanYuState.LoseQi(fixture.Player, resources.Qi);
        await DuanYuState.GainQi(fixture.Player, 7);
        var card = EngineSelfTests.AddCard(fixture, "DY_A01", PileType.Hand, upgraded: true);
        card.BaseReplayCount = 2;
        fixture.Player.PlayerCombatState!.Energy = 0;
        await EngineSelfTests.PaidPlay(fixture, card);

        EngineSelfTests.Equal(3, CombatManager.Instance.History.CardPlaysFinished.Count(entry => ReferenceEquals(entry.CardPlay.Card, card)), "native replay resolves all three card bodies");
        EngineSelfTests.Equal(1, resources.SpendingCount, "native replay only pays Qi once");
        EngineSelfTests.Equal(0, resources.Qi, "the one payment spends all Qi");
        EngineSelfTests.Equal(7, fixture.Player.Creature.GetPowerAmount<StrengthPower>(), "native replay gains Strength only for the one actual payment");
    }
}
#endif
