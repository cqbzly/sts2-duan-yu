using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace DuanYu.Cards;

public sealed class DuanYuBlockVar(decimal amount) : BlockVar(amount, ValueProp.Move)
{
    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        if (card.TargetType != TargetType.AnyPlayer || target?.Player == null || target == card.Owner?.Creature || !runGlobalHooks || card.CombatState == null)
        {
            base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
            return;
        }
        PreviewValue = Hook.ModifyBlock(card.CombatState, target, BaseValue, Props, null, null, out _);
    }
}
[HarmonyPatch(typeof(FrailPower), nameof(FrailPower.ModifyBlockMultiplicative))]
internal static class CleanseBeforeBlockPreview
{
    private static void Postfix(FrailPower __instance, Creature target, CardModel? cardSource, CardPlay? cardPlay, ref decimal __result)
    {
        if (cardPlay == null && cardSource is DuanYuCard { DesignId: "DY_R16" } card && target == card.Owner?.Creature && __instance.Owner == target)
            __result = 1m;
    }
}
