using BaseLib.Abstracts;
using BaseLib.Hooks;
using DuanYu.Assets;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace DuanYu.Runtime;

public sealed class QiResource() : BasicCustomResource("CQBZLY_DY_QI_RESOURCE")
{
    public override string TexturePath => DuanYuAssets.PowerIcon("CQBZLY_DY_QI");
    public override string IconPath => TexturePath;
    public override Color MainColor => new("215B65");
    public override bool ApplySharedModification => false;
    public override int Amount
    {
        get => Owner == null ? base.Amount : DuanYuState.Get(Owner).Qi;
        set
        {
            int clamped = Math.Clamp(value, 0, DuanYuState.QiCap);
            if (Owner != null) DuanYuState.Get(Owner).Qi = clamped;
            base.Amount = clamped;
        }
    }
    public override bool ShouldShowDisplay() => Owner != null && (DuanYuState.IsDuanYu(Owner) || Amount > 0);
    public override string GetTextForAmount(int displayAmount) => $"{displayAmount}/{DuanYuState.QiCap}";
    public override HoverTip? MakeTip() => new(ModelDb.Power<QiPower>().Title,
        "段誉的专属资源，最多9点，跨回合保留。运气N：获得N点真气。需N点真气：打出整张牌前必须足额支付；不足时不能打出。能量免费不会免除真气费用；同一次出牌的重放不重复支付。",
        DuanYuAssets.Texture(TexturePath));

    public override async Task<bool> Spend<T>(ICombatState combatState, AbstractModel? spender, int amount, bool optional)
    {
        var resource = ValidateType<T>();
        if (Owner == null || amount < 0 || amount > Amount) return false;
        if (amount == 0) return true;
        bool paid = spender is Cards.DuanYuCard card
            ? await DuanYuState.PayCardQi(card, amount)
            : await DuanYuState.SpendQi(Owner, amount, spender as CardModel);
        if (paid) await BaseLibHooks.AfterSpendCustomResource(combatState, resource, spender, amount);
        return paid;
    }
}
