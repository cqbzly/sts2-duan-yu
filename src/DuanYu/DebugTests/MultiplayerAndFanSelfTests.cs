#if DEBUG
using DuanYu.Cards;
using DuanYu.Character;
using DuanYu.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
