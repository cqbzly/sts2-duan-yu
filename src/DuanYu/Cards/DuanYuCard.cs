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
public sealed record CardDefinition(string Id, string Name, string Rarity, string Type, int Cost, string Text, CardUpgrade Upgrade);
public sealed record CardDesign(CardDefinition[] Cards);

[Pool(typeof(DuanYuCardPool))]
public abstract class DuanYuCard : CustomCardModel
{
    private static readonly IReadOnlyDictionary<string, CardDefinition> Definitions = LoadDefinitions();
    public string DesignId { get; }
    public CardDefinition Definition => Definitions[DesignId];
    public LocString SelectionPrompt => SelectionScreenPrompt;
    public DuanYuCardNumbers Numbers => DuanYuCardNumbers.For(DesignId);
    public int QiCost => DesignId switch
    {
        "DY_C17" => 1,
        "DY_B05" or "DY_C02" or "DY_C15" or "DY_U32" => 2,
        "DY_U07" or "DY_U17" or "DY_U33" or "DY_R20" => 3,
        "DY_R06" => 4, "DY_R03" or "DY_R05" => 6, "DY_R13" => 9, _ => 0
    };
    public int Value(int normal, int upgraded) => IsUpgraded ? upgraded : normal;

    protected DuanYuCard(string id) : base(Definitions[id].Cost, TypeOf(Definitions[id].Type), RarityOf(Definitions[id].Rarity), TargetOf(id, Definitions[id].Type))
    {
        DesignId = id;
    }

    public override string CustomPortraitPath => DuanYuAssets.CardPortrait(DesignId);
    public override bool GainsBlock => Numbers.Block > 0;
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            if (DesignId == "DY_U08")
            {
                foreach (var variable in MakeCalculatedDamage(16, (source, _) => source.IsCanonical || source.Owner == null ? 0 : DuanYuState.Get(source.Owner).Qi, 2)) yield return variable;
            }
            else if (Numbers.Damage > 0) yield return new DamageVar(Numbers.Damage, ValueProp.Move);
            if (Numbers.ExtraDamage > 0) yield return new DamageVar("ExtraDamage", Numbers.ExtraDamage, ValueProp.Move);
            if (Numbers.Block > 0) yield return new DuanYuBlockVar(Numbers.Block);
        }
    }
    protected override HashSet<CardTag> CanonicalTags => DesignId switch
    {
        "DY_B01" => [CardTag.Strike], "DY_B02" => [CardTag.Defend], _ => []
    };
    public override IEnumerable<CardKeyword> CanonicalKeywords => DesignId.StartsWith("DY_T", StringComparison.Ordinal)
        ? [CardKeyword.Exhaust, CardKeyword.Ethereal]
        : DesignId is "DY_C10" or "DY_C17" or "DY_U02" or "DY_U04" or "DY_U13" or "DY_U15" or "DY_U19" or "DY_U25" or "DY_U35" or "DY_R04" or "DY_R09" or "DY_R10" or "DY_R11" or "DY_R14" or "DY_R16" or "DY_R17" or "DY_R19" ? [CardKeyword.Exhaust] : [];
    public override List<(string, string)> Localization => new CardLoc(Definition.Name,
        "{IfUpgraded:show:" + DescriptionText(true) + "|" + DescriptionText(false) + "}",
        ("selectionScreenPrompt", "选择卡牌"), ("qiTipTitle", "真气与耗气"), ("lingboTipTitle", "凌波"), ("swordTipTitle", "六脉剑气"));
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            if (Definition.Text.Contains("气", StringComparison.Ordinal) && !DesignId.StartsWith("DY_T", StringComparison.Ordinal))
                yield return new HoverTip(new LocString("cards", Id.Entry + ".qiTipTitle"), "运气N：获得N点真气，最多9点。耗气N：在该句结算时自动支付足额真气，否则跳过附加效果。" + QiPreview());
            if (Definition.Text.Contains("凌波", StringComparison.Ordinal)) yield return HoverTipFactory.FromPower<LingboPower>();
            if (Definition.Text.Contains("剑气", StringComparison.Ordinal))
                yield return new HoverTip(new LocString("cards", Id.Entry + ".swordTipTitle"), "六种0费衍生攻击牌，均具有消耗、虚无。升级版本仍算同一种剑式。手牌已满时生成牌进入弃牌堆。");
            if (Definition.Text.Contains("虚弱", StringComparison.Ordinal)) yield return HoverTipFactory.FromPower<WeakPower>();
            if (Definition.Text.Contains("易伤", StringComparison.Ordinal)) yield return HoverTipFactory.FromPower<VulnerablePower>();
        }
    }
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(Definition.Upgrade.Cost - Definition.Cost);
        if (DesignId == "DY_U08") DynamicVars.CalculationBase.UpgradeValueBy(6);
        else if (Numbers.Damage > 0) DynamicVars.Damage.UpgradeValueBy(Numbers.DamageUp - Numbers.Damage);
        if (Numbers.ExtraDamage > 0) DynamicVars["ExtraDamage"].UpgradeValueBy(Numbers.ExtraDamageUp - Numbers.ExtraDamage);
        if (Numbers.Block > 0) DynamicVars.Block.UpgradeValueBy(Numbers.BlockUp - Numbers.Block);
    }
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => DuanYuCardExecutor.Play(this, choiceContext, cardPlay);

    public static CardModel Canonical(string id) => ModelDb.GetById<CardModel>(ModelDb.GetId(typeof(DuanYuCard).Assembly.GetType("DuanYu.Cards." + id) ?? throw new ArgumentException("Unknown card: " + id)));
    private static string Clean(string text) => text.Replace("消耗。", "").Replace("虚无。", "");
    private string QiPreview()
    {
        if (IsCanonical || Owner == null) return "";
        int current = DuanYuState.Get(Owner).Qi;
        if (QiCost == 0) return $"当前真气：{current}/9。";
        return current >= QiCost ? $"按当前真气可发动：{current}→{current - QiCost}；实际以耗气句结算时为准。" : $"当前{current}/需要{QiCost}：附加效果不执行；实际以耗气句结算时为准。";
    }
    private string DescriptionText(bool upgraded)
    {
        if (DesignId == "DY_U08") return "造成{CalculatedDamage:diff()}点伤害（基础伤害加当前真气的2倍）。";
        string result = Clean(upgraded ? Definition.Upgrade.Text : Definition.Text);
        if (Numbers.Damage > 0) result = result.Replace($"造成{(upgraded ? Numbers.DamageUp : Numbers.Damage)}点伤害", "造成{Damage:diff()}点伤害");
        if (Numbers.ExtraDamage > 0) result = result.Replace($"造成{(upgraded ? Numbers.ExtraDamageUp : Numbers.ExtraDamage)}点伤害", "造成{ExtraDamage:diff()}点伤害");
        if (Numbers.Block > 0) result = result.Replace($"获得{(upgraded ? Numbers.BlockUp : Numbers.Block)}点格挡", "获得{Block:diff()}点格挡");
        return result;
    }
    private static CardType TypeOf(string type) => type switch { "攻击" => CardType.Attack, "能力" => CardType.Power, _ => CardType.Skill };
    private static CardRarity RarityOf(string rarity) => rarity switch
    {
        "基础" => CardRarity.Basic, "普通" => CardRarity.Common, "罕见" => CardRarity.Uncommon,
        "稀有" => CardRarity.Rare, "衍生" => CardRarity.Token, _ => throw new ArgumentException(rarity)
    };
    private static TargetType TargetOf(string id, string type) => id switch
    {
        "DY_U29" or "DY_U30" or "DY_R17" => TargetType.AnyPlayer,
        "DY_C04" or "DY_U17" or "DY_R20" or "DY_T03" => TargetType.AllEnemies,
        "DY_U20" => TargetType.AnyEnemy,
        _ => type == "攻击" ? TargetType.AnyEnemy : TargetType.Self
    };
    private static IReadOnlyDictionary<string, CardDefinition> LoadDefinitions()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DuanYu.cards.json") ?? throw new InvalidOperationException("Embedded card catalog is missing.");
        return (JsonSerializer.Deserialize<CardDesign>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("Invalid card catalog.")).Cards.ToDictionary(card => card.Id);
    }
}
