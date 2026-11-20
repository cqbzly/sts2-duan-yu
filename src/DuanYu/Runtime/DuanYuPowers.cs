using BaseLib.Abstracts;
using BaseLib.Utils.Attributes;
using DuanYu.Cards;
using DuanYu.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
    public override List<(string, string)> Localization => new PowerLoc("真气", "当前有{Amount}点真气，无上限，跨回合保留。卡牌的真气费用必须在出牌前足额支付，不足时整张牌不能打出。", "当前有{Amount}点真气，无上限，跨回合保留。卡牌的真气费用必须在出牌前足额支付，不足时整张牌不能打出。");
}

[CustomID("CQBZLY_DY_LINGBO")]
public sealed class LingboPower : DuanYuPower
{
    public override List<(string, string)> Localization => new PowerLoc("凌波", "本回合受到的伤害每段降低{Amount}点。最多3层，下个自己的回合开始清除。失去生命不受影响。", "本回合受到的伤害每段降低{Amount}点。最多3层，下个自己的回合开始清除。失去生命不受影响。");
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

[CustomID("CQBZLY_DY_SWORD_MOMENTUM")]
public sealed class SwordMomentumPower : DuanYuPower
{
    public override List<(string, string)> Localization => new PowerLoc("剑势", "六种剑气伤害增加{Amount}点。最多8层，在你的下回合开始时清除。每次剑气结算后获得1层；每回合第3、第6种不同剑气各额外获得1层。", "六种剑气伤害增加{Amount}点。最多8层，在你的下回合开始时清除。每次剑气结算后获得1层；每回合第3、第6种不同剑气各额外获得1层。");
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        dealer == Owner && props.IsPoweredAttack() && cardSource != null && DuanYuState.IsSword(cardSource) ? Amount : 0;
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
        ["DY_R15"] = typeof(GuardSimplicityPower), ["DY_R18"] = typeof(FormlessPower),
        ["DY_R22"] = typeof(MeridianUnityPower)
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
    protected override string Rule => "每获得1层凌波，获得{Amount}点格挡。";
}
[CustomID("CQBZLY_DY_POWER_U28")]
public sealed class PeacefulHeartPower : DuanYuEffectPower
{
    protected override string Title => "百脉归炉";
    protected override string Rule => "每消耗1张牌，获得{Amount}点格挡；消耗气滞或异种真气时，额外运气1。";
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (Owner.Player is { } player && card.Owner == player && DuanYuState.CanAct(player))
        {
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
            if (DuanYuState.CardId(card) is "DY_S01" or "DY_S02")
                await DuanYuState.GainQi(player, 1, card);
        }
    }
}
[CustomID("CQBZLY_DY_POWER_R01")]
public sealed class BeimingPower : DuanYuEffectPower
{
    protected override string Title => "北冥神功";
    protected override string Rule => "每回合第2次支付真气后，运气{Amount}，每份北冥神功获得1点能量。";
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
    protected override string Rule => "打出时及每回合开始时，随机将剑气加入手牌。每份无形剑阵生成1张，升级版本生成升级剑气。";
}
[CustomID("CQBZLY_DY_POWER_R08")]
public sealed class SixPulseUnityPower : DuanYuEffectPower
{
    protected override string Title => "六脉贯通";
    protected override string Rule => "每回合打出第3和第6种不同剑气后，各抽{Amount}张牌，每份六脉贯通运气1并获得1点能量。";
}
[CustomID("CQBZLY_DY_POWER_R12")]
public sealed class YijingPower : DuanYuEffectPower
{
    protected override string Title => "易经心法";
    protected override string Rule => "回合开始时获得{Amount}点格挡；至少有5点真气时，每份已打出的易经心法再获得1层凌波。";
}
[CustomID("CQBZLY_DY_POWER_R15")]
public sealed class GuardSimplicityPower : DuanYuEffectPower
{
    protected override string Title => "守拙";
    protected override string Rule => "回合开始时运气{Amount}；每回合首张剑气结算后，每份守拙获得1点能量。";
}
[CustomID("CQBZLY_DY_POWER_R18")]
public sealed class FormlessPower : DuanYuEffectPower
{
    protected override string Title => "无相之境";
    protected override string Rule => "每张剑气结算后，获得{Amount}点格挡。";
}

[CustomID("CQBZLY_DY_POWER_R22")]
public sealed class MeridianUnityPower : DuanYuEffectPower
{
    protected override string Title => "经脉归一";
    protected override string Rule => "打出时及回合开始时生成异种真气。每回合前2次消耗状态牌后，获得{Amount}点格挡，每份经脉归一运气1并随机生成1张剑气。升级版生成的剑气已升级且可免费打出。";

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (Owner.Player is not { } player || card.Owner != player || card.Type != CardType.Status || !DuanYuState.CanAct(player)) return;
        var state = DuanYuState.Get(player);
        if (state.StatusExhaustTriggers >= 2) return;
        state.StatusExhaustTriggers++;
        var effect = state.Effects.Single(entry => entry.CardId == "DY_R22");
        var copies = effect.SecondaryAmount;
        await DuanYuState.GainQi(player, copies, card);
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
        for (var index = 0; index < copies && DuanYuState.CanAct(player); index++)
        {
            bool upgraded = index >= copies - effect.UpgradedMeridianCopies;
            await DuanYuCardExecutor.GenerateSword(player, player.RunState.Rng.CombatCardGeneration.NextInt(1, 7), upgraded, freeQi: upgraded);
        }
    }
}
