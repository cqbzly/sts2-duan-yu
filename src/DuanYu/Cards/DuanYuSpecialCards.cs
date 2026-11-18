using BaseLib.Utils.Attributes;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace DuanYu.Cards;

[CustomID("CQBZLY_DY_S01")]
public sealed class DY_S01 : DuanYuCard
{
    public DY_S01() : base("DY_S01") { }
    public override int MaxUpgradeLevel => 0;
    public override bool HasTurnEndInHandEffect => true;
    protected override Task OnTurnEndInHand(PlayerChoiceContext choiceContext) => DuanYuState.LoseQi(Owner, 2, this);
}

[CustomID("CQBZLY_DY_S02")]
public sealed class DY_S02 : DuanYuCard
{
    public DY_S02() : base("DY_S02") { }
    public override int MaxUpgradeLevel => 0;
}
