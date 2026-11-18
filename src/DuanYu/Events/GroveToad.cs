using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils.Attributes;
using DuanYu.Relics;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace DuanYu.Events;

[CustomID("CQBZLY_DY_EVENT02")]
public sealed class GroveToad : DuanYuEvent
{
    private bool CanRescue => CanParticipate && Participant.Creature.CurrentHp > 10
        && !DuanYuState.HasAcquiredRelic(Participant, "CQBZLY_DY_RELIC08")
        && !Participant.Relics.OfType<VermilionToadEssence>().Any();

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (!CanParticipate) return [Choice("LEAVE", Leave)];
        return
        [
            Choice("RESCUE", Rescue, CanRescue),
            Choice("PROTECT", Protect, Participant.Creature.CurrentHp > 8 && PileType.Deck.GetPile(Participant).Cards.Any(card => card.IsRemovable), true),
            Choice("LEAVE", Leave)
        ];
    }

    private async Task Rescue()
    {
        if (!CanRescue || IsFinished) return;
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Participant.Creature, 10, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        await RelicCmd.Obtain<VermilionToadEssence>(Participant);
        SetEventFinished(PageDescription("RESCUE"));
    }

    private async Task Protect()
    {
        if (!CanParticipate || IsFinished || Participant.Creature.CurrentHp <= 8) return;
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
        CardModel? selected = (await CardSelectCmd.FromDeckForRemoval(Participant, prefs)).SingleOrDefault();
        if (selected == null || !selected.IsRemovable || !PileType.Deck.GetPile(Participant).Cards.Contains(selected)) return;
        if (!CanParticipate || IsFinished || Participant.Creature.CurrentHp <= 8) return;
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Participant.Creature, 8, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        await CardPileCmd.RemoveFromDeck(selected, false);
        SetEventFinished(PageDescription("PROTECT"));
    }

    public override List<(string, string)> Localization => new EventLoc(
        "林间蛤鸣",
        new EventPageLoc("INITIAL", "红色小蛤在草叶间鼓起喉囊，一只小貂焦躁地绕着它打转。你本想将它们分开，却发现这一步远比想象中危险。奇异的鸣声催促着你作出选择。",
            new EventOptionLoc("RESCUE", "冒险解围", "失去[red]10[/red]点生命。获得[gold]朱蛤元息[/gold]。生命不足或本局已取得该遗物时不可选。"),
            new EventOptionLoc("PROTECT", "护住小貂", "失去[red]8[/red]点生命。选择并移除牌组中1张可移除牌。生命不足或没有可移除牌时不可选。"),
            new EventOptionLoc("LEAVE", "循声绕行", "离开林间。")),
        new EventPageLoc("SPECTATOR", "草叶间传来异样的鸣声。你在林边等候段誉，准备循声绕行。"),
        new EventPageLoc("RESCUE", "你伸手分开两只小兽，不料身形一晃，朱蛤竟在混乱中落入口中。胸腹骤然灼痛，随后一股奇异气息缓缓沉入经脉。"),
        new EventPageLoc("PROTECT", "你护住小貂，手臂却被毒息灼伤。待它远去，你取出一页不再需要的旧招式，让它随山风飘散。"),
        new EventPageLoc("LEAVE", "你绕过草丛，把鸣声留在身后，继续向前。"));
}
