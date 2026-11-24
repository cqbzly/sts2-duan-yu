#if DEBUG
using DuanYu.Cards;
using DuanYu.Character;
using DuanYu.Relics;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

namespace DuanYu.DebugTests;

internal static class MultiplayerAndFanSelfTests
{
    internal static IEnumerable<(string Name, Func<Task> Action)> Cases
    {
        get
        {
            yield return ("pool:multiplayer-cards-filter-from-singleplayer", MultiplayerCardsFilter);
            yield return ("multiplayer:ally-support-values", AllySupportValues);
            yield return ("relic:ornamental-fan-counts-card-plays", OrnamentalFanCountsCardPlays);
        }
    }

    private static Task MultiplayerCardsFilter()
    {
        var pool = ModelDb.CardPool<DuanYuCardPool>();
        var all = pool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.None).ToArray();
        var singlePlayer = pool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.SingleplayerOnly).ToArray();
        var multiplayerOnly = all.Where(card => card.MultiplayerConstraint == CardMultiplayerConstraint.MultiplayerOnly).Select(card => card.Id.Entry).ToHashSet();
        EngineSelfTests.Equal(3, multiplayerOnly.Count, "three Duan Yu cards are marked multiplayer-only");
        EngineSelfTests.Equal(true, multiplayerOnly.SetEquals(["CQBZLY_DY_U29", "CQBZLY_DY_U30", "CQBZLY_DY_R17"]), "only ally-target cards are multiplayer-only");
        EngineSelfTests.Equal(true, singlePlayer.All(card => card.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly), "single-player pool excludes ally-target cards");
        EngineSelfTests.Equal(all.Length - multiplayerOnly.Count, singlePlayer.Length, "single-player pool keeps every other card");
        return Task.CompletedTask;
    }

    private static async Task AllySupportValues()
    {
        foreach (bool upgraded in new[] { false, true })
        foreach (string id in new[] { "DY_U29", "DY_U30", "DY_R17" })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            var ally = MegaCrit.Sts2.Core.Entities.Players.Player.CreateForNewRun(
                ModelDb.Character<DuanYuCharacter>(), UnlockState.all, 2);
            fixture.Run.AddPlayerDebug(ally, -1);
            ally.ResetCombatState();
            fixture.Combat.AddPlayer(ally);
            PileType.Draw.GetPile(ally).AddInternal(fixture.Combat.CreateCard(ModelDb.Card<DY_B01>(), ally));

            await DuanYuState.LoseQi(fixture.Player, DuanYuState.Get(fixture.Player).Qi);
            fixture.Player.PlayerCombatState!.Energy = 1;
            var card = EngineSelfTests.AddCard(fixture, id, PileType.Hand, upgraded);
            int startingHp = fixture.Player.Creature.CurrentHp;
            var (energySpent, starsSpent) = await card.SpendResources();
            await card.OnPlayWrapper(new ThrowingPlayerChoiceContext(), ally.Creature, false,
                new ResourceInfo { EnergySpent = energySpent, EnergyValue = 1, StarsSpent = starsSpent, StarValue = 0 }, true);

            int expectedBlock = id switch
            {
                "DY_U29" => upgraded ? 14 : 10,
                "DY_U30" => upgraded ? 12 : 8,
                _ => upgraded ? 21 : 16
            };
            EngineSelfTests.Equal(expectedBlock, ally.Creature.Block, id + " blocks for the targeted player");
            EngineSelfTests.Equal(0, fixture.Player.Creature.Block, id + " does not grant block to the source");
            EngineSelfTests.Equal(id == "DY_R17" ? upgraded ? 1 : 2 : 0, startingHp - fixture.Player.Creature.CurrentHp, id + " has the printed HP cost");
            EngineSelfTests.Equal(id == "DY_R17" ? 2 : 0, DuanYuState.Get(ally).Lingbo, id + " has the printed Lingbo effect");
            EngineSelfTests.Equal(id == "DY_U30" ? 1 : 0, PileType.Hand.GetPile(ally).Cards.Count, id + " draws for the targeted player");
            EngineSelfTests.Equal(id == "DY_U29" ? 1 : 0, DuanYuState.Get(fixture.Player).Qi, id + " grants Qi to the source");
            EngineSelfTests.Equal(PileType.Discard, card.Pile!.Type, id + " remains reusable");
        }
    }

    private static async Task OrnamentalFanCountsCardPlays()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        await RelicCmd.Remove(fixture.Player.Relics.OfType<LanghuanPendant>().Single());
        await RelicCmd.Obtain<OrnamentalFan>(fixture.Player);
        fixture.Player.PlayerCombatState!.Energy = 3;
        await EngineSelfTests.PaidPlay(fixture, EngineSelfTests.AddCard(fixture, "DY_B01", PileType.Hand));
        await EngineSelfTests.PaidPlay(fixture, EngineSelfTests.AddCard(fixture, "DY_B01", PileType.Hand));
        EngineSelfTests.Equal(0, fixture.Player.Creature.Block, "fan does not activate before the third attack card");
        await DuanYuStateForTest.GainQi(fixture.Player, 1);
        await EngineSelfTests.PaidPlay(fixture, EngineSelfTests.AddCard(fixture, "DY_T03", PileType.Hand));
        EngineSelfTests.Equal(4, fixture.Player.Creature.Block, "fan counts the AOE sword as one played attack card");
    }

    private static class DuanYuStateForTest
    {
        internal static Task GainQi(MegaCrit.Sts2.Core.Entities.Players.Player player, int amount)
            => DuanYu.Runtime.DuanYuState.GainQi(player, amount);
    }
}
#endif
