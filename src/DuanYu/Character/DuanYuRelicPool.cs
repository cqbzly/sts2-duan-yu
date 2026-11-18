using BaseLib.Abstracts;
using BaseLib.Utils.Attributes;
using DuanYu.Assets;
using Godot;

namespace DuanYu.Character;

[CustomID("CQBZLY_DUAN_YU_RELIC_POOL")]
public sealed class DuanYuRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => DuanYuCharacter.JadeColor;
    public override string BigEnergyIconPath => DuanYuAssets.BigEnergy;
    public override string TextEnergyIconPath => DuanYuAssets.TextEnergy;
}
