#if DEBUG
using DuanYu.Character;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

namespace DuanYu.DebugTests;

internal static class FullRunPath
{
    internal static (RunState Run, Player Player) Create(string seed)
    {
        TestMode.AssertOn();
        if (RunManager.Instance.IsInProgress) RunManager.Instance.CleanUp();
        var player = Player.CreateForNewRun<DuanYuCharacter>(UnlockState.all, 1);
        var acts = ActModel.GetDefaultList().Select(act => act.ToMutable()).ToList();
        var run = RunState.CreateForNewRun([player], acts, Array.Empty<ModifierModel>(), GameMode.Standard,
            isCustom: false, ascensionLevel: 10, seed);
        RunManager.Instance.SetUpNewSingleplayer(run, shouldSave: false);
        LocalContext.NetId = player.NetId;
        EngineSelfTests.Equal(10, run.AscensionLevel, "full run A10 level");
        EngineSelfTests.Equal(GameMode.Standard, run.GameMode, "full run standard mode");
        EngineSelfTests.Equal(3, run.Acts.Count, "full run three acts");
        EngineSelfTests.Equal(1, player.Deck.Cards.OfType<AscendersBane>().Count(), "full run native ascension curse");
        EngineSelfTests.Equal(true, run.ExtraFields.StartedWithNeow, "full run starts with Neow");
        EngineSelfTests.Equal(true, run.Acts[^1].HasSecondBoss, "full run final act double boss");
        return (run, player);
    }

    internal static async Task Start()
    {
        TestMode.AssertOn();
        await RunManager.Instance.FinalizeStartingRelics();
        RunManager.Instance.Launch();
        await RunManager.Instance.EnterAct(0, doTransition: false);
    }

    internal static MapPoint? ChooseNextPoint(RunState run)
    {
        if (run.CurrentMapPoint == null) return run.Map.StartingMapPoint;
        var player = run.Players.Single();
        return MapTravel.GetTravelablePointsFrom(run, run.CurrentMapPoint)
            .Where(point => !run.VisitedMapCoords.Contains(point.coord))
            .OrderByDescending(point => Score(point, player, 4))
            .ThenBy(point => point.coord.col)
            .FirstOrDefault();
    }

    internal static async Task<bool> Advance(RunState run)
    {
        TestMode.AssertOn();
        var manager = RunManager.Instance;
        if (manager.HasRunEnded || run.Players.Any(player => player.Creature.IsDead)) return false;
        if (run.CurrentRoom?.IsVictoryRoom == true)
            throw new InvalidOperationException("The Architect must finish through its event options.");
        if (run.CurrentRoom is CombatRoom { IsPreFinished: false })
            throw new InvalidOperationException("Cannot leave a combat before native combat completion.");
        if (run.CurrentRoom is EventRoom eventRoom && !eventRoom.LocalMutableEvent.IsFinished)
            throw new InvalidOperationException("Cannot leave an event before its options finish.");

        var next = ChooseNextPoint(run);
        if (next != null)
        {
            if (run.CurrentMapPoint != null && !MapTravel.GetTravelablePointsFrom(run, run.CurrentMapPoint).Contains(next))
                throw new InvalidOperationException("Selected map coordinate is not legally reachable.");
            await manager.EnterMapCoord(next.coord);
            EngineSelfTests.Equal(next.coord, run.CurrentMapCoord!.Value, "full run enters selected map coordinate");
            return true;
        }

        var terminalBoss = run.Map.SecondBossMapPoint ?? run.Map.BossMapPoint;
        if (run.CurrentMapPoint?.coord != terminalBoss.coord || run.CurrentRoom is not CombatRoom { IsPreFinished: true, RoomType: RoomType.Boss })
            throw new InvalidOperationException("Act transition requires victory at the final boss map point.");
        var previousAct = run.CurrentActIndex;
        await manager.EnterNextAct();
        if (run.CurrentActIndex != previousAct)
        {
            EngineSelfTests.Equal(previousAct + 1, run.CurrentActIndex, "full run advances one act");
            if (run.CurrentMapPoint == null) await manager.EnterMapCoord(run.Map.StartingMapPoint.coord);
        }
        else
        {
            EngineSelfTests.Equal(run.Acts.Count - 1, previousAct, "full run completes final act");
            EngineSelfTests.Equal(true, run.CurrentRoom.IsVictoryRoom, "full run enters Architect victory room");
        }
        return true;
    }

    private static double Score(MapPoint point, Player player, int lookahead)
    {
        var healthRatio = (double)player.Creature.CurrentHp / Math.Max(1, player.Creature.MaxHp);
        var immediate = point.PointType switch
        {
            MapPointType.Boss => 1000,
            MapPointType.Ancient => 90,
            MapPointType.Treasure => 80,
            MapPointType.RestSite => healthRatio < 0.75 ? 110 : 65,
            MapPointType.Shop => player.Gold >= 250 ? 110 : player.Gold >= 150 ? 75 : player.Gold >= 100 ? 28 : -15,
            MapPointType.Elite => healthRatio >= 0.9 && player.Deck.Cards.Count >= 20 ? 5 : -120,
            MapPointType.Monster => player.Deck.Cards.Count < 16 ? 34 : 16,
            MapPointType.Unknown => 25,
            _ => 0
        };
        if (lookahead <= 0 || point.Children.Count == 0) return immediate;
        return immediate + 0.65 * point.Children.Max(child => Score(child, player, lookahead - 1));
    }
}
#endif
