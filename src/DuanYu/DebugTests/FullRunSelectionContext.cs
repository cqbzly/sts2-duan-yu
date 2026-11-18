#if DEBUG
using System.Reflection;
using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.TestSupport;

namespace DuanYu.DebugTests;

[HarmonyPatch]
internal static class FullRunSelectionContext
{
    private readonly record struct ContextState(bool Applied, string? Previous);
    private static readonly AsyncLocal<string?> CurrentPurpose = new();

    internal static string? Purpose => CurrentPurpose.Value;

    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() => typeof(CardSelectCmd)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(method => method.GetParameters().Any(parameter => parameter.Name == "prefs" && parameter.ParameterType == typeof(CardSelectorPrefs)));

    [HarmonyPrefix]
    private static void Prefix(MethodBase __originalMethod, CardSelectorPrefs prefs, out ContextState __state)
    {
        __state = new ContextState(TestMode.IsOn, CurrentPurpose.Value);
        if (!__state.Applied) return;
        var purpose = __originalMethod.Name switch
        {
            nameof(CardSelectCmd.FromDeckForRemoval) => "remove",
            nameof(CardSelectCmd.FromDeckForTransformation) => "transform",
            nameof(CardSelectCmd.FromDeckForUpgrade) => "upgrade",
            nameof(CardSelectCmd.FromDeckForEnchantment) => "enchant",
            _ => null
        };
        if (purpose == null && prefs.Prompt?.LocTable == "card_selection")
        {
            purpose = prefs.Prompt.LocEntryKey switch
            {
                "TO_REMOVE" => "remove",
                "TO_TRANSFORM" => "transform",
                "TO_UPGRADE" => "upgrade",
                "TO_ENCHANT" => "enchant",
                "TO_DISCARD" => "discard",
                "TO_EXHAUST" => "exhaust",
                "TO_ABSORB" => "absorb",
                _ => null
            };
        }
        if (purpose != null) CurrentPurpose.Value = purpose;
    }

    [HarmonyPostfix]
    private static void Postfix(ContextState __state)
    {
        if (__state.Applied) CurrentPurpose.Value = __state.Previous;
    }

    [HarmonyFinalizer]
    private static Exception? Finalizer(Exception? __exception, ContextState __state)
    {
        if (__state.Applied) CurrentPurpose.Value = __state.Previous;
        return __exception;
    }
}
#endif
