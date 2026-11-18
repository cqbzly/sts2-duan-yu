#if DEBUG
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.TestSupport;

namespace DuanYu.DebugTests;

[HarmonyPatch]
internal static class FullRunNativeRules
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(CallingBell), "GenerateRewards");
        yield return AccessTools.Method(typeof(Cauldron), "GenerateRewards");
        yield return AccessTools.Method(typeof(MerchantPotionEntry), nameof(MerchantPotionEntry.CalcCost));
    }

    [HarmonyPrefix]
    private static void Prefix(out bool __state)
    {
        __state = FullRunSimulation.IsRunning && TestMode.IsOn;
        if (__state) TestMode.IsOn = false;
    }

    [HarmonyFinalizer]
    private static void Finalizer(bool __state)
    {
        if (__state) TestMode.IsOn = true;
    }
}
#endif
