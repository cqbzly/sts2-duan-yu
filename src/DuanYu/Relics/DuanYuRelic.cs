using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using BaseLib.Utils.Attributes;
using DuanYu.Assets;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace DuanYu.Relics;

[Pool(typeof(DuanYu.Character.DuanYuRelicPool))]
public abstract class DuanYuRelic : CustomRelicModel
{
    public abstract int RelicNumber { get; }

    protected abstract string LocalizationId { get; }
    protected abstract string TitleText { get; }
    protected abstract string DescriptionText { get; }
    protected abstract string FlavorText { get; }

    public override List<(string, string)> Localization => new RelicLoc(TitleText, DescriptionText, FlavorText);
    public override string PackedIconPath => DuanYuAssets.RelicIcon(Id.Entry);
    protected override string PackedIconOutlinePath => DuanYuAssets.RelicIcon($"{Id.Entry}_outline");
    protected override string BigIconPath => DuanYuAssets.RelicIcon($"{Id.Entry}_big");

    public override Task BeforeCombatStart() => DuanYuState.StartRelicCombat(Owner, RelicNumber);

    public override Task AfterObtained()
    {
        if (Owner != null)
            DuanYuState.RecordRelic(Owner, Id.Entry);
        return Task.CompletedTask;
    }

}

[CustomID("CQBZLY_DY_RELIC01")]
public sealed class LanghuanPendant : DuanYuRelic
{
    public override int RelicNumber => 1;
    protected override string LocalizationId => "DY_RELIC01";
    protected override string TitleText => "琅嬛玉佩";
    protected override string DescriptionText => "每场战斗开始时，运气3。";
    protected override string FlavorText => "玉声清越，似有无数旧卷在其中翻动。";
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override RelicModel GetUpgradeReplacement() => ModelDb.Relic<LanghuanCirclet>();
}

[CustomID("CQBZLY_DY_RELIC09")]
public sealed class LanghuanCirclet : DuanYuRelic
{
    public override int RelicNumber => 9;
    protected override string LocalizationId => "DY_RELIC09";
    protected override string TitleText => "头环";
    protected override string DescriptionText => "每场战斗开始时，运气5。每回合开始时，运气1。";
    protected override string FlavorText => "玉光凝成一环，百川自此周流。";
    public override RelicRarity Rarity => RelicRarity.Starter;
}

[CustomID("CQBZLY_DY_RELIC02")]
public sealed class ScrollFragment : DuanYuRelic
{
    public override int RelicNumber => 2;
    protected override string LocalizationId => "DY_RELIC02";
    protected override string TitleText => "帛卷残页";
    protected override string DescriptionText => "从你的第二回合开始，若回合开始时真气为0，运气1。";
    protected override string FlavorText => "残字未完，行气之法却自成一脉。";
    public override RelicRarity Rarity => RelicRarity.Common;
}

[CustomID("CQBZLY_DY_RELIC03")]
public sealed class WuliangShoes : DuanYuRelic
{
    public override int RelicNumber => 3;
    protected override string LocalizationId => "DY_RELIC03";
    protected override string TitleText => "无量青履";
    protected override string DescriptionText => "每回合首次打出第三张技能牌后，获得4点格挡。";
    protected override string FlavorText => "鞋底沾着无量山的青苔。";
    public override RelicRarity Rarity => RelicRarity.Common;
}

[CustomID("CQBZLY_DY_RELIC04")]
public sealed class TianlongPrayerBeads : DuanYuRelic
{
    public override int RelicNumber => 4;
    protected override string LocalizationId => "DY_RELIC04";
    protected override string TitleText => "天龙念珠";
    protected override string DescriptionText => "每回合首次由一张牌实际消耗至少3真气后，获得4点格挡。";
    protected override string FlavorText => "一珠一念，念念归于本心。";
    public override RelicRarity Rarity => RelicRarity.Uncommon;
}

[CustomID("CQBZLY_DY_RELIC05")]
public sealed class SixPulseManual : DuanYuRelic
{
    public override int RelicNumber => 5;
    protected override string LocalizationId => "DY_RELIC05";
    protected override string TitleText => "六脉图谱";
    protected override string DescriptionText => "每回合首次打出第三种不同的剑式后，获得1能量。";
    protected override string FlavorText => "图上六道墨线，指向六种不同的锋芒。";
    public override RelicRarity Rarity => RelicRarity.Rare;
}

[CustomID("CQBZLY_DY_RELIC06")]
public sealed class DaliSeal : DuanYuRelic
{
    public override int RelicNumber => 6;
    protected override string LocalizationId => "DY_RELIC06";
    protected override string TitleText => "大理国印";
    protected override string DescriptionText => "每场战斗首次因支付耗气而使真气恰好变为0后，获得1能量。";
    protected override string FlavorText => "方寸国印，承着不肯争斗的责任。";
    public override RelicRarity Rarity => RelicRarity.Uncommon;
}

[CustomID("CQBZLY_DY_RELIC07")]
public sealed class WitheredBloomPrayer : DuanYuRelic
{
    public override int RelicNumber => 7;
    protected override string LocalizationId => "DY_RELIC07";
    protected override string TitleText => "枯荣禅珠";
    protected override string DescriptionText => "从你的第二回合开始，若你上回合打出过技能牌且未打出攻击牌，回合开始时运气1。";
    protected override string FlavorText => "枯荣之间，取舍本就是修行。";
    public override RelicRarity Rarity => RelicRarity.Rare;
}

[CustomID("CQBZLY_DY_RELIC08")]
public sealed class VermilionToadEssence : DuanYuRelic
{
    public override int RelicNumber => 8;
    protected override string LocalizationId => "DY_RELIC08";
    protected override string TitleText => "朱蛤元息";
    protected override string DescriptionText => "每场战斗第一次将要获得虚弱或中毒时，阻止该次状态施加。";
    protected override string FlavorText => "一声蛤鸣，毒与气在体内走过另一条路。";
    public override RelicRarity Rarity => RelicRarity.Event;
}
