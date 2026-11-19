using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Abstracts;
using BaseLib.Utils.Patching;
using DuanYu.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;

namespace DuanYu.Runtime;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.SpendResources))]
internal static class QiResourcePaymentGuard
{
    private static bool Prefix(CardModel __instance, ref Task<(int, int)> __result)
    {
        if (__instance is not DuanYuCard card || card.RequiredQi <= DuanYuState.Get(card.Owner).Qi) return true;
        __result = Task.FromResult((0, 0));
        return false;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper), MethodType.Async)]
internal static class QiCardPlayGuard
{
    private static IEnumerable<CodeInstruction> Transpiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original)
        => AsyncMethodCall.Create(generator, instructions, original,
            AccessTools.Method(typeof(QiCardPlayGuard), nameof(RejectUnpaidPlay)), beforeState: original, resultName: "returnIf");

    private static async Task<bool> RejectUnpaidPlay(CardModel __instance)
    {
        if (__instance is not DuanYuCard card || card.QiCost == 0) return false;
        if (!DuanYuState.CanAct(card.Owner)) return true;
        if (DuanYuState.HasCardQiPayment(card)) return false;
        var resource = CustomResources<QiResource>.Get(card.Owner.PlayerCombatState!);
        return !await resource.Spend<QiResource>(card.CombatState!, card, card.RequiredQi, false);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
internal static class QiCardPaymentCleanup
{
    private static void Postfix(CardModel __instance, ref Task __result)
    {
        if (__instance is DuanYuCard card && card.QiCost > 0) __result = CompletePlay(__result, card);
    }

    private static async Task CompletePlay(Task original, DuanYuCard card)
    {
        try { await original; }
        finally { DuanYuState.ClearCardQiPayment(card); }
    }
}

[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.AutoPlay), MethodType.Async)]
internal static class QiAutoPlayGuard
{
    private static IEnumerable<CodeInstruction> Transpiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original)
        => AsyncMethodCall.Create(generator, instructions, original,
            AccessTools.Method(typeof(QiAutoPlayGuard), nameof(RejectUnavailableQi)), beforeState: original, resultName: "returnIf");

    private static Task<bool> RejectUnavailableQi(CardModel card) => Task.FromResult(
        card is DuanYuCard duanYuCard && duanYuCard.QiCost > 0 && !DuanYuState.HasCardQiPayment(duanYuCard) &&
        (!DuanYuState.CanAct(card.Owner) || duanYuCard.RequiredQi > DuanYuState.Get(card.Owner).Qi));
}
