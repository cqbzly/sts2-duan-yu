using BaseLib.Abstracts;
using BaseLib.Utils.Attributes;
using BaseLib.Utils.NodeFactories;
using DuanYu.Assets;
using DuanYu.Cards;
using DuanYu.Relics;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace DuanYu.Character;

[CustomID("CQBZLY_DUAN_YU")]
public sealed class DuanYuCharacter : PlaceholderCharacterModel
{
    public static readonly Color JadeColor = new("4EBDA8");

    public override Color NameColor => JadeColor;
    public override Color DreadNameColor => new("8FE6C7");
    public override Color DialogueColor => new("215B65");
    public override Color MapDrawingColor => JadeColor;
    public override Color RemoteTargetingLineColor => JadeColor;
    public override Color RemoteTargetingLineOutline => new("102D33");
    public override CharacterGender Gender => CharacterGender.Masculine;
    public override int StartingHp => 72;
    public override int StartingGold => 99;
    public override int MaxEnergy => 3;
    public override float DeathAnimTime => 0.7f;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<DY_B01>(), ModelDb.Card<DY_B01>(), ModelDb.Card<DY_B01>(), ModelDb.Card<DY_B01>(),
        ModelDb.Card<DY_B02>(), ModelDb.Card<DY_B02>(), ModelDb.Card<DY_B02>(), ModelDb.Card<DY_B02>(),
        ModelDb.Card<DY_B03>(), ModelDb.Card<DY_B05>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<LanghuanPendant>()];
    public override CardPoolModel CardPool => ModelDb.CardPool<DuanYuCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<DuanYuRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<DuanYuPotionPool>();

    public override string CustomVisualPath => DuanYuAssets.CharacterScene;
    public override string CustomRestSiteAnimPath => DuanYuAssets.RestScene;
    public override string CustomMerchantAnimPath => DuanYuAssets.MerchantScene;
    public override string CustomIconPath => DuanYuAssets.IconScene;
    public override string CustomIconTexturePath => DuanYuAssets.CharacterIcon;
    public override string CustomIconOutlineTexturePath => DuanYuAssets.CharacterIconOutline;
    public override string CustomCharacterSelectIconPath => DuanYuAssets.CharacterIcon;
    public override string CustomCharacterSelectLockedIconPath => DuanYuAssets.LockedIcon;
    public override string CustomMapMarkerPath => DuanYuAssets.CharacterIcon;
    public override string CustomCharacterSelectBg => DuanYuAssets.CharacterSelectScene;

    public override Control CustomIcon
    {
        get
        {
            Control icon = NodeFactory<Control>.CreateFromResource(DuanYuAssets.Texture(DuanYuAssets.CharacterIcon));
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }

    public override List<(string, string)> Localization => LocManager.Instance.Language == "eng"
        ? new CharacterLoc("Duan Yu", "Duan Yu", "A gentle scholar bearing extraordinary martial arts. Store [gold]Qi[/gold], evade with [gold]Lingbo[/gold], and choose among six sword breaths.", "him", "he", "his", "his", "jade and mountain mist", "Please, take your time.", "May your path be safe.", "There must be another way.", "Perhaps this will help someone.", "Duan Yu Cards", "Add Duan Yu's cards to the card pool.", ("shortTitle", "Duan Yu"), ("bestiaryQuote", "I would rather find a peaceful way."), ("bestiaryKillQuote", "Forgive me. I had no other choice."))
        : new CharacterLoc("大理世子·段誉", "段誉", "不愿争胜，却身负绝学。以[gold]北冥[/gold]积蓄真气，以[gold]凌波[/gold]卸力周旋，以[gold]六脉剑气[/gold]破局。", "他", "他", "他的", "他的", "玉石与山间水雾", "莫急，在下等你。", "愿诸位前路平安。", "或许还有别的办法。", "这些盘缠，总会用得上。", "段誉卡牌", "将段誉的卡牌加入卡池。", ("shortTitle", "段誉"), ("bestiaryQuote", "若能不动刀兵，便是最好。"), ("bestiaryKillQuote", "承让。愿前路少些刀兵。"));
}
