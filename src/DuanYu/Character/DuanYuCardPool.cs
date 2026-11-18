using BaseLib.Abstracts;
using BaseLib.Utils.Attributes;
using DuanYu.Assets;
using Godot;

namespace DuanYu.Character;

[CustomID("CQBZLY_DUAN_YU_CARD_POOL")]
public sealed class DuanYuCardPool : CustomCardPoolModel
{
    public override string Title => "duan_yu";
    public override Color ShaderColor => DuanYuCharacter.JadeColor;
    public override Color DeckEntryCardColor => DuanYuCharacter.JadeColor;
    public override Color EnergyOutlineColor => new("173E46");
    public override bool IsColorless => false;
    public override string BigEnergyIconPath => DuanYuAssets.BigEnergy;
    public override string TextEnergyIconPath => DuanYuAssets.TextEnergy;
}
