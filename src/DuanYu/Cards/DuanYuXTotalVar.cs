using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace DuanYu.Cards;

// X multiplies the printed coefficient before the single damage/block packet is modified.
// Keep the coefficient vars separate so upgrades and card modifiers retain their native behavior.
public sealed class DuanYuXTotalVar(bool damage, decimal coefficient)
    : DynamicVar(damage ? "XDamage" : "XBlock", coefficient)
{
    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        int x = PreviewX(card);
        decimal raw = (damage ? card.DynamicVars.Damage.BaseValue : card.DynamicVars.Block.BaseValue) * (x + (damage ? 1 : 0));
        if (!damage && x == 0)
        {
            // No block command is issued for X=0, so Dexterity cannot turn it into block.
            EnchantedValue = PreviewValue = 0;
            return;
        }

        decimal enchanted = raw;
        if (card.Enchantment is { } enchantment)
        {
            enchanted += damage ? enchantment.EnchantDamageAdditive(enchanted, ValueProp.Move) : enchantment.EnchantBlockAdditive(enchanted);
            enchanted *= damage ? enchantment.EnchantDamageMultiplicative(enchanted, ValueProp.Move) : enchantment.EnchantBlockMultiplicative(enchanted);
        }
        EnchantedValue = enchanted;
        if (runGlobalHooks && card.Owner != null && card.CombatState != null)
        {
            PreviewValue = damage
                ? Hook.ModifyDamage(card.Owner.RunState, card.CombatState, target, card.Owner.Creature, raw, ValueProp.Move, card, null, ModifyDamageHookType.All, previewMode, out _)
                : Hook.ModifyBlock(card.CombatState, card.Owner.Creature, raw, ValueProp.Move, card, null, out _);
        }
        else PreviewValue = enchanted;
        if (damage) PreviewValue = Math.Max(0, PreviewValue);
    }

    private static int PreviewX(CardModel card)
    {
        if (card.IsCanonical || card.Owner == null || card.CombatState == null) return 0;
        int captured = card.Pile?.Type == PileType.Hand
            ? card.EnergyCost.GetAmountToSpend()
            : card.EnergyCost.CapturedXValue;
        return Math.Max(0, Hook.ModifyXValue(card.CombatState, card, captured));
    }
}
