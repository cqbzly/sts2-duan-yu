using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace DuanYu.Cards;

// X multiplies the printed coefficient before block modifiers apply.
public sealed class DuanYuXBlockVar(decimal coefficient) : DynamicVar("XBlock", coefficient)
{
    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        int x = PreviewX(card);
        decimal raw = card.DynamicVars.Block.BaseValue * x;
        if (x == 0)
        {
            EnchantedValue = PreviewValue = 0;
            return;
        }

        decimal enchanted = raw;
        if (card.Enchantment is { } enchantment)
        {
            enchanted += enchantment.EnchantBlockAdditive(enchanted);
            enchanted *= enchantment.EnchantBlockMultiplicative(enchanted);
        }
        EnchantedValue = enchanted;
        if (runGlobalHooks && card.Owner != null && card.CombatState != null)
        {
            PreviewValue = Hook.ModifyBlock(card.CombatState, card.Owner.Creature, raw, ValueProp.Move, card, null, out _);
        }
        else PreviewValue = enchanted;
    }

    internal static int PreviewX(CardModel card)
    {
        if (card.IsCanonical || card.Owner == null || card.CombatState == null) return 0;
        int captured = card.Pile?.Type == PileType.Play
            ? card.EnergyCost.CapturedXValue
            : card.EnergyCost.GetAmountToSpend();
        return Math.Max(0, Hook.ModifyXValue(card.CombatState, card, captured));
    }
}
