#if DEBUG
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace DuanYu.DebugTests;

[HarmonyPatch(typeof(SoulNexus), "AfterDeath", new[] { typeof(Creature) })]
internal static class FullRunHeadlessVisuals
{
    [HarmonyPrefix]
    private static bool Prefix(SoulNexus __instance)
    {
        if (!FullRunSimulation.IsRunning || !TestMode.IsOn || NCombatRoom.Instance != null) return true;
        var callback = AccessTools.DeclaredMethod(typeof(SoulNexus), "AfterDeath", [typeof(Creature)]).CreateDelegate<Action<Creature>>(__instance);
        __instance.Creature.Died -= callback;
        FullRunSimulation.Record("headless-animation-skipped", new { monster = __instance.Id.Entry, animation = "tracks/empty" });
        return false;
    }
}

[HarmonyPatch]
internal static class FullRunTrial
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.DeclaredMethod(typeof(Trial), "Accept");
        yield return AccessTools.DeclaredMethod(typeof(Trial), "AddVfxAnchoredToPortrait");
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var original = AccessTools.Method(typeof(LocalContext), nameof(LocalContext.IsMe), [typeof(Player)]);
        var replacement = AccessTools.Method(typeof(FullRunTrial), nameof(HasLocalEventScreen));
        var patched = instructions.ToList();
        var replacements = 0;
        foreach (var instruction in patched)
        {
            if (!instruction.Calls(original)) continue;
            instruction.operand = replacement;
            replacements++;
        }
        var expected = __originalMethod.Name == "Accept" ? 2 : 1;
        if (replacements != expected)
            throw new InvalidOperationException($"Trial UI adapter expected {expected} checks in {__originalMethod.Name}, found {replacements}.");
        return patched;
    }

    private static bool HasLocalEventScreen(Player player) =>
        !(FullRunSimulation.IsRunning && TestMode.IsOn && NEventRoom.Instance == null) && LocalContext.IsMe(player);
}
#endif
