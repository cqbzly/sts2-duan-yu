#if DEBUG
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent;
using MegaCrit.Sts2.Core.TestSupport;

namespace DuanYu.DebugTests;

[HarmonyPatch(typeof(CrystalSphereMinigame), nameof(CrystalSphereMinigame.PlayMinigame))]
internal static class FullRunCrystalSphere
{
    [HarmonyPrefix]
    private static bool Prefix(CrystalSphereMinigame __instance, ref Task __result)
    {
        if (!FullRunSimulation.IsRunning || !TestMode.IsOn || !LocalContext.IsMe(__instance.Owner)) return true;
        __result = PlayWithoutScreen(__instance);
        return false;
    }

    private static async Task PlayWithoutScreen(CrystalSphereMinigame game)
    {
        var initialDivinations = game.DivinationCount;
        if (initialDivinations <= 0) throw new InvalidOperationException("Crystal Sphere starts without divinations.");
        game.SetTool(CrystalSphereMinigame.CrystalSphereToolType.Big);
        FullRunSimulation.Record("crystal-sphere-start", new
        {
            divinations = initialDivinations,
            width = game.GridSize.X,
            height = game.GridSize.Y,
            policy = "Maximize hidden cells in the visible 3x3 selection; coordinate tie-break; no item lookup or RNG sampling."
        });
        for (var click = 0; click < initialDivinations; click++)
        {
            var candidates = new List<(CrystalSphereCell Cell, int Hidden)>();
            for (var column = 0; column < game.GridSize.X; column++)
            {
                for (var row = 0; row < game.GridSize.Y; row++)
                {
                    var cell = game.cells[column, row];
                    if (cell.IsHidden) candidates.Add((cell, HiddenCellsAround(game, column, row)));
                }
            }
            var selected = candidates.OrderByDescending(candidate => candidate.Hidden)
                .ThenBy(candidate => candidate.Cell.X).ThenBy(candidate => candidate.Cell.Y).FirstOrDefault();
            if (selected.Cell == null) throw new InvalidOperationException("Crystal Sphere has no hidden legal cell before its divinations are exhausted.");
            var remaining = game.DivinationCount;
            if (remaining <= 0) throw new InvalidOperationException("Crystal Sphere exhausted its divinations before the expected click count.");
            await game.CellClicked(selected.Cell);
            if (game.DivinationCount != remaining - 1)
                throw new InvalidOperationException("Native Crystal Sphere click did not consume exactly one divination.");
            FullRunSimulation.Record("crystal-sphere-click", new
            {
                ordinal = click + 1,
                column = selected.Cell.X,
                row = selected.Cell.Y,
                hiddenInSelection = selected.Hidden,
                remaining = game.DivinationCount
            });
        }
        if (!game.IsFinished) throw new InvalidOperationException("Crystal Sphere did not finish after its native click budget.");
        var complete = AccessTools.Method(typeof(CrystalSphereMinigame), "CompleteMinigame")
            ?? throw new MissingMethodException(typeof(CrystalSphereMinigame).FullName, "CompleteMinigame");
        await (Task)complete.Invoke(game, null)!;
        FullRunSimulation.Record("crystal-sphere-finished", new
        {
            clicks = initialDivinations,
            remaining = game.DivinationCount,
            rewards = "Native CompleteMinigame and OneOffSynchronizer"
        });
    }

    private static int HiddenCellsAround(CrystalSphereMinigame game, int column, int row)
    {
        var hidden = 0;
        for (var nearbyColumn = Math.Max(0, column - 1); nearbyColumn <= Math.Min(game.GridSize.X - 1, column + 1); nearbyColumn++)
            for (var nearbyRow = Math.Max(0, row - 1); nearbyRow <= Math.Min(game.GridSize.Y - 1, row + 1); nearbyRow++)
                if (game.cells[nearbyColumn, nearbyRow].IsHidden) hidden++;
        return hidden;
    }
}
#endif
