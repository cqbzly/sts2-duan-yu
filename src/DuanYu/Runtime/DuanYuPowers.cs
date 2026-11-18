using BaseLib.Abstracts;
using BaseLib.Utils.Attributes;
using DuanYu.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace DuanYu.Runtime;

public abstract class DuanYuPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => DuanYuAssets.PowerIcon(Id.Entry);
    public override string CustomBigIconPath => DuanYuAssets.PowerIcon(Id.Entry);
}

[CustomID("CQBZLY_DY_QI")]
public sealed class QiPower : DuanYuPower
{
    public override List<(string, string)> Localization => new PowerLoc("真气", "当前有{Amount}点真气，最多9点。跨回合保留。耗气效果支付真气后才能发动。", "当前有{Amount}点真气，最多9点。跨回合保留。耗气效果支付真气后才能发动。");
}

[CustomID("CQBZLY_DY_LINGBO")]
public sealed class LingboPower : DuanYuPower
{
    public override List<(string, string)> Localization => new PowerLoc("凌波", "接下来的{Amount}段正数攻击伤害各降低3点，然后结算格挡。最多3层，在你的下回合开始时清除。", "接下来的{Amount}段正数攻击伤害各降低3点，然后结算格挡。最多3层，在你的下回合开始时清除。");
    public override Task AfterModifyingDamageAmount(CardModel? cardSource) => Owner.Player is { } player ? DuanYuState.ConsumeLingbo(player) : Task.CompletedTask;
}

[CustomID("CQBZLY_DY_QI_BLADE")]
public sealed class QiBladePower : DuanYuPower
{
    public override List<(string, string)> Localization => new PowerLoc("凝气成锋", "本回合剑气造成的伤害增加{Amount}点。", "本回合剑气造成的伤害增加{Amount}点。");
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        dealer == Owner && props.IsPoweredAttack() && cardSource != null && DuanYuState.IsSword(cardSource) ? Amount : 0;
}

[CustomID("CQBZLY_DY_VERMILION_WARD")]
public sealed class VermilionWardPower : DuanYuPower
{
    public override List<(string, string)> Localization => new PowerLoc("朱蛤元息", "阻止本场战斗下一次虚弱或中毒的施加。", "阻止本场战斗下一次虚弱或中毒的施加。");
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (target != Owner || amount <= 0 || canonicalPower is not (WeakPower or PoisonPower)) return false;
        modifiedAmount = 0;
        return true;
    }
    public override Task AfterModifyingPowerAmountReceived(PowerModel power) => PowerCmd.Remove(this);
}

public abstract class DuanYuEffectPower : DuanYuPower
{
    public static readonly IReadOnlyDictionary<string, Type> Types = new Dictionary<string, Type>
    {
        ["DY_U01"] = typeof(BeginningBeimingPower), ["DY_U05"] = typeof(QiGuardPower),
        ["DY_U14"] = typeof(SwordProficiencyPower), ["DY_U23"] = typeof(WindWithoutTracePower),
        ["DY_U28"] = typeof(PeacefulHeartPower), ["DY_R01"] = typeof(BeimingPower),
        ["DY_R02"] = typeof(LingboFootworkPower), ["DY_R07"] = typeof(InvisibleSwordArrayPower),
        ["DY_R08"] = typeof(SixPulseUnityPower), ["DY_R12"] = typeof(YijingPower),
        ["DY_R15"] = typeof(GuardSimplicityPower), ["DY_R18"] = typeof(FormlessPower)
    };
    protected abstract new string Title { get; }
    protected abstract string Rule { get; }
    public override List<(string, string)> Localization => new PowerLoc(Title, Rule, Rule);
}

[CustomID("CQBZLY_DY_POWER_U01")]
public sealed class BeginningBeimingPower : DuanYuEffectPower
{
    protected override string Title => "北冥初成";
    protected override string Rule => "每回合首次成功发动耗气效果后，抽{Amount}张牌。";
}
[CustomID("CQBZLY_DY_POWER_U05")]
public sealed class QiGuardPower : DuanYuEffectPower
{
    protected override string Title => "真气护体";
    protected override string Rule => "回合结束时，每层获得当前真气一半（向下取整）的格挡；升级版本另加2点。当前{Amount}层。";
}
[CustomID("CQBZLY_DY_POWER_U14")]
public sealed class SwordProficiencyPower : DuanYuEffectPower
{
    protected override string Title => "指法纯熟";
    protected override string Rule => "每回合打出的前2张剑气伤害增加{Amount}点。";
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        dealer == Owner && props.IsPoweredAttack() && cardSource != null ? DuanYuState.SwordBonus(cardSource, cardPlay) : 0;
}
[CustomID("CQBZLY_DY_POWER_U23")]
public sealed class WindWithoutTracePower : DuanYuEffectPower
{
    protected override string Title => "风过无痕";
    protected override string Rule => "每回合首次凌波减伤的该段伤害结算后，获得{Amount}点格挡。";
}
[CustomID("CQBZLY_DY_POWER_U28")]
public sealed class PeacefulHeartPower : DuanYuEffectPower
{
    protected override string Title => "不争之心";
    protected override string Rule => "若本回合没有打出攻击牌，回合结束时获得{Amount}点格挡。";
}
[CustomID("CQBZLY_DY_POWER_R01")]
public sealed class BeimingPower : DuanYuEffectPower
{
    protected override string Title => "北冥神功";
    protected override string Rule => "每回合前2次成功发动耗气效果后，运气{Amount}。";
}
[CustomID("CQBZLY_DY_POWER_R02")]
public sealed class LingboFootworkPower : DuanYuEffectPower
{
    protected override string Title => "凌波微步";
    protected override string Rule => "每回合前2张技能牌结算后，获得{Amount}层凌波。";
}
[CustomID("CQBZLY_DY_POWER_R07")]
public sealed class InvisibleSwordArrayPower : DuanYuEffectPower
{
    protected override string Title => "无形剑阵";
    protected override string Rule => "回合开始时，选择并生成{Amount}张剑气。";
}
[CustomID("CQBZLY_DY_POWER_R08")]
public sealed class SixPulseUnityPower : DuanYuEffectPower
{
    protected override string Title => "六脉贯通";
    protected override string Rule => "每回合打出第3种不同剑气后抽{Amount}张牌，并按已获得的层数运气。";
}
[CustomID("CQBZLY_DY_POWER_R12")]
public sealed class YijingPower : DuanYuEffectPower
{
    protected override string Title => "易经心法";
    protected override string Rule => "回合开始时获得{Amount}点格挡；至少有5点真气时再按层数获得凌波。";
}
[CustomID("CQBZLY_DY_POWER_R15")]
public sealed class GuardSimplicityPower : DuanYuEffectPower
{
    protected override string Title => "守拙";
    protected override string Rule => "回合结束时，若至少剩余1点能量，运气{Amount}。";
}
[CustomID("CQBZLY_DY_POWER_R18")]
public sealed class FormlessPower : DuanYuEffectPower
{
    protected override string Title => "无相之境";
    protected override string Rule => "每回合前3张剑气结算后，获得{Amount}点格挡。";
}
