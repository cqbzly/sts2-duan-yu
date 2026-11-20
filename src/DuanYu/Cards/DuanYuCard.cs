using System.Reflection;
using System.Text.Json;
using BaseLib.Abstracts;
using BaseLib.Utils;
using BaseLib.Utils.Attributes;
using DuanYu.Assets;
using DuanYu.Character;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace DuanYu.Cards;

public sealed record CardUpgrade(int Cost, string Text);
public sealed record CardDefinition(string Id, string Name, string Rarity, string Type, int Cost, string Text, CardUpgrade Upgrade, bool CostX = false, bool QiCostX = false);
public sealed record CardDesign(CardDefinition[] Cards);

[Pool(typeof(DuanYuCardPool))]
public abstract class DuanYuCard : CustomCardModel
{
    private static readonly IReadOnlyDictionary<string, CardDefinition> Definitions = LoadDefinitions();
    public string DesignId { get; }
    public CardDefinition Definition => Definitions[DesignId];
    public LocString SelectionPrompt => SelectionScreenPrompt;
    public DuanYuCardNumbers Numbers => DuanYuCardNumbers.For(DesignId);
    public bool QiCostX => Definition.QiCostX;
    public bool HasQiCost => QiCostX || QiCost > 0;
    public int CapturedQiX => Math.Max(0, CustomResources<QiResource>.Cost(this)?.CapturedXValue ?? 0);
    public int RequiredQi => QiCostX && (IsCanonical || Owner == null) ? 0
        : CustomResources<QiResource>.Cost(this)?.GetAmountToSpend() ?? QiCost;
    public int QiCost => DesignId switch
    {
        "DY_T01" or "DY_T02" or "DY_T03" or "DY_T04" or "DY_T05" or "DY_T06" => 1,
        "DY_C17" => 1,
        "DY_B05" or "DY_C15" or "DY_U32" or "DY_R06" => 2,
        "DY_C14" or "DY_U07" or "DY_U17" or "DY_U33" or "DY_R05" or "DY_R20" => 3,
        "DY_R13" => 5, _ => 0
    };
    public int Value(int normal, int upgraded) => IsUpgraded ? upgraded : normal;

    public override CardMultiplayerConstraint MultiplayerConstraint => DesignId is "DY_U29" or "DY_U30" or "DY_R17"
        ? CardMultiplayerConstraint.MultiplayerOnly
        : CardMultiplayerConstraint.None;

    protected DuanYuCard(string id) : base(Definitions[id].Cost, TypeOf(Definitions[id].Type), RarityOf(Definitions[id].Rarity), TargetOf(id, Definitions[id].Type))
    {
        DesignId = id;
        if (QiCostX) CustomResources<QiResource>.SetXCost(this);
        else if (QiCost > 0) CustomResources<QiResource>.SetCanonicalCost(this, QiCost);
    }

    protected override bool HasEnergyCostX => Definition.CostX;

    public override string CustomPortraitPath => DuanYuAssets.CardPortrait(DesignId);
    public override bool GainsBlock => Numbers.Block > 0 || DesignId == "DY_U37";
    protected override bool IsPlayable => base.IsPlayable && (RequiredQi == 0 || Owner == null || DuanYuState.Get(Owner).Qi >= RequiredQi);
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            if (DesignId == "DY_U08")
            {
                foreach (var variable in MakeCalculatedDamage(16, (source, _) => source.IsCanonical || source.Owner == null ? 0 : DuanYuState.Get(source.Owner).Qi, 1)) yield return variable;
            }
            else if (Numbers.Damage > 0) yield return new DamageVar(Numbers.Damage, ValueProp.Move);
            if (Numbers.ExtraDamage > 0) yield return new DamageVar("ExtraDamage", Numbers.ExtraDamage, ValueProp.Move);
            if (Numbers.Block > 0) yield return new DuanYuBlockVar(Numbers.Block);
            if (DesignId == "DY_U37") yield return new DuanYuXBlockVar(Numbers.Block);
        }
    }
    protected override HashSet<CardTag> CanonicalTags => DesignId switch
    {
        "DY_B01" => [CardTag.Strike], "DY_B02" => [CardTag.Defend], _ => []
    };
    public override IEnumerable<CardKeyword> CanonicalKeywords => DesignId == "DY_S01"
        ? [CardKeyword.Unplayable, CardKeyword.Ethereal]
        : DesignId.StartsWith("DY_T", StringComparison.Ordinal) ? [CardKeyword.Exhaust, CardKeyword.Ethereal]
        : DesignId is "DY_C22" or "DY_C10" or "DY_U02" or "DY_U09" or "DY_U13" or "DY_U15" or "DY_U19" or "DY_U27" or "DY_U35" or "DY_U38" or "DY_R04" or "DY_R11" or "DY_R14" or "DY_R17" or "DY_R21" or "DY_S02" ? [CardKeyword.Exhaust] : [];
    public override List<(string, string)> Localization => new CardLoc(Definition.Name,
        "{IfUpgraded:show:" + DescriptionText(true) + "|" + DescriptionText(false) + "}",
        ("selectionScreenPrompt", "选择卡牌"), ("qiTipTitle", "真气与耗气"), ("lingboTipTitle", "凌波"), ("swordTipTitle", "六脉剑气"), ("statusTipTitle", "专属状态牌"), ("foreignTipTitle", "外门招式"));
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            if (Definition.Text.Contains("气", StringComparison.Ordinal))
                yield return new HoverTip(new LocString("cards", Id.Entry + ".qiTipTitle"), "运气N：获得N点真气，无上限，跨回合保留。需N点真气：打出整张牌前必须足额支付，不足时不能打出。能量免费仍需支付真气；同一次出牌的重放不重复支付。" + QiPreview());
            if (Definition.Text.Contains("凌波", StringComparison.Ordinal)) yield return HoverTipFactory.FromPower<LingboPower>();
            if (DuanYuState.ReceivesSwordDamageBonus(this) || Definition.Text.Contains("剑势", StringComparison.Ordinal)) yield return HoverTipFactory.FromPower<SwordMomentumPower>();
            if (Definition.Text.Contains("剑气", StringComparison.Ordinal))
                yield return new HoverTip(new LocString("cards", Id.Entry + ".swordTipTitle"), "六种0费衍生攻击牌，每次发动必须支付1点真气，均具有消耗、虚无。每层剑势令剑气伤害增加1点，最多8层，在你的下回合开始时清除。每张剑气结算后获得1层剑势；每回合第3、第6种不同剑式各额外获得1层。升级版本仍算同一种剑式。手牌已满时生成牌进入弃牌堆。");
            if (Definition.Text.Contains("虚弱", StringComparison.Ordinal)) yield return HoverTipFactory.FromPower<WeakPower>();
            if (Definition.Text.Contains("易伤", StringComparison.Ordinal)) yield return HoverTipFactory.FromPower<VulnerablePower>();
            if (Definition.Text.Contains("气滞", StringComparison.Ordinal) || Definition.Text.Contains("异种真气", StringComparison.Ordinal) || Type == CardType.Status)
                yield return new HoverTip(new LocString("cards", Id.Entry + ".statusTipTitle"), "气滞：不可打出、虚无；回合结束时失去2点真气。失去真气不算支付。异种真气：1费、消耗；运气2，获得1层剑势，抽1张牌。两者均不能升级。");
            if (DesignId is "DY_U27" or "DY_U36")
                yield return new HoverTip(new LocString("cards", Id.Entry + ".foreignTipTitle"), "从其他角色的全部非基础攻击牌中等概率借招，同一次不重复。固定能量费用本回合为0，X费用、专属资源费用和打出条件照常。借来的牌具有消耗，升级借招生成升级牌。");
        }
    }
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(Definition.Upgrade.Cost - Definition.Cost);
        if (DesignId == "DY_U08") DynamicVars.CalculationBase.UpgradeValueBy(7);
        else if (Numbers.Damage > 0) DynamicVars.Damage.UpgradeValueBy(Numbers.DamageUp - Numbers.Damage);
        if (Numbers.ExtraDamage > 0) DynamicVars["ExtraDamage"].UpgradeValueBy(Numbers.ExtraDamageUp - Numbers.ExtraDamage);
        if (Numbers.Block > 0) DynamicVars.Block.UpgradeValueBy(Numbers.BlockUp - Numbers.Block);
        if (DesignId == "DY_U37") DynamicVars["XBlock"].UpgradeValueBy(Numbers.BlockUp - Numbers.Block);
    }
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => DuanYuCardExecutor.Play(this, choiceContext, cardPlay);

    public static CardModel Canonical(string id) => ModelDb.GetById<CardModel>(ModelDb.GetId(typeof(DuanYuCard).Assembly.GetType("DuanYu.Cards." + id) ?? throw new ArgumentException("Unknown card: " + id)));
    private static string Clean(string text) => string.Join("。", text.Split('。').Where(sentence => sentence is not ("消耗" or "虚无" or "不可打出")));
    private string QiPreview()
    {
        if (IsCanonical || Owner == null) return "";
        int current = DuanYuState.Get(Owner).Qi;
        if (QiCostX) return $"当前真气：{current}。每支付1点真气，攻击1次。";
        if (RequiredQi == 0) return "本牌可免费打出。";
        return current >= RequiredQi ? $"当前真气：{current}；出牌前支付{RequiredQi}点。" : $"当前{current}/需要{RequiredQi}：真气不足，不能打出。";
    }
    private string DescriptionText(bool upgraded)
    {
        if (DesignId == "DY_U08") return $"造成{{CalculatedDamage:diff()}}点伤害（{(upgraded ? 23 : 16)}+真气）。将1张气滞加入弃牌堆。";
        string result = Clean(upgraded ? Definition.Upgrade.Text : Definition.Text);
        if (QiCost > 0)
        {
            result = result.Replace($"需{QiCost}点真气。", "", StringComparison.Ordinal)
                .Replace($"需支付{QiCost}点真气。", "", StringComparison.Ordinal);
        }
        if (DesignId == "DY_U37")
        {
            int coefficient = upgraded ? Numbers.BlockUp : Numbers.Block;
            result = result.Replace($"获得{coefficient}点格挡×X", $"获得{{XBlock:diff()}}点格挡（{coefficient}×X）");
        }
        if (Numbers.Damage > 0)
        {
            string damage = DesignId == "DY_R21" ? "剑气伤害" : "伤害";
            result = result.Replace($"造成{(upgraded ? Numbers.DamageUp : Numbers.Damage)}点{damage}", $"造成{{Damage:diff()}}点{damage}");
        }
        if (Numbers.ExtraDamage > 0) result = result.Replace($"造成{(upgraded ? Numbers.ExtraDamageUp : Numbers.ExtraDamage)}点伤害", "造成{ExtraDamage:diff()}点伤害");
        if (Numbers.Block > 0) result = result.Replace($"获得{(upgraded ? Numbers.BlockUp : Numbers.Block)}点格挡", "获得{Block:diff()}点格挡");
        return result;
    }
    private static CardType TypeOf(string type) => type switch { "攻击" => CardType.Attack, "能力" => CardType.Power, "状态" => CardType.Status, _ => CardType.Skill };
    private static CardRarity RarityOf(string rarity) => rarity switch
    {
        "基础" => CardRarity.Basic, "普通" => CardRarity.Common, "罕见" => CardRarity.Uncommon,
        "稀有" => CardRarity.Rare, "衍生" => CardRarity.Token, "状态" => CardRarity.Status, _ => throw new ArgumentException(rarity)
    };
    private static TargetType TargetOf(string id, string type) => id switch
    {
        "DY_S01" or "DY_S02" => TargetType.None,
        "DY_U29" or "DY_U30" or "DY_R17" => TargetType.AnyPlayer,
        "DY_C04" or "DY_U10" or "DY_U17" or "DY_R09" or "DY_R20" or "DY_T03" => TargetType.AllEnemies,
        "DY_U20" => TargetType.AnyEnemy,
        _ => type == "攻击" ? TargetType.AnyEnemy : TargetType.Self
    };
    private static IReadOnlyDictionary<string, CardDefinition> LoadDefinitions()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DuanYu.cards.json") ?? throw new InvalidOperationException("Embedded card catalog is missing.");
        return (JsonSerializer.Deserialize<CardDesign>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("Invalid card catalog.")).Cards.ToDictionary(card => card.Id);
    }
}
