using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils.Attributes;
using DuanYu.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.ValueProps;

namespace DuanYu.Events;

[CustomID("CQBZLY_DY_EVENT01")]
public sealed class StoneChamber : DuanYuEvent
{
    private List<CardCreationResult>? _candidates;

    private static IEnumerable<DuanYuCard> EligibleCards => ModelDb.AllCards.OfType<DuanYuCard>()
        .Where(card => card.Rarity == CardRarity.Uncommon && card.Type == CardType.Skill);

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (!CanParticipate) return [Choice("LEAVE", Leave)];
        return
        [
            Choice("STUDY", StudyBreath, Participant.Creature.CurrentHp > 8 && EligibleCards.Any(), true),
            Choice("SORT", SortLessons, Participant.Gold >= 70 && PileType.Deck.GetPile(Participant).Cards.Any(card => card.IsUpgradable), true),
            Choice("LEAVE", Leave)
        ];
    }

    private List<CardCreationResult> GetCandidates()
    {
        if (_candidates != null) return _candidates;
        _candidates = [];
        List<DuanYuCard> pool = EligibleCards.OrderBy(card => card.Id.Entry, System.StringComparer.Ordinal).ToList();
        while (pool.Count > 0 && _candidates.Count < 3)
        {
            int index = Rng.NextInt(pool.Count);
            DuanYuCard canonical = pool[index];
            pool.RemoveAt(index);
            _candidates.Add(new CardCreationResult(Participant.RunState.CreateCard(canonical, Participant)));
        }
        return _candidates;
    }

    private async Task StudyBreath()
    {
        if (!CanParticipate || IsFinished || Participant.Creature.CurrentHp <= 8) return;
        List<CardCreationResult> candidates = GetCandidates();
        if (candidates.Count == 0) return;
        var prefs = new CardSelectorPrefs(PageDescription("SELECTION"), 0, 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
        CardModel? selected = (await CardSelectCmd.FromSimpleGridForRewards(new BlockingPlayerChoiceContext(), candidates, Participant, prefs)).SingleOrDefault();
        if (selected == null || !candidates.Any(candidate => candidate.Card == selected)) return;
        if (!CanParticipate || IsFinished || Participant.Creature.CurrentHp <= 8) return;
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Participant.Creature, 8, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(selected, PileType.Deck));
        foreach (CardCreationResult skipped in candidates.Where(candidate => candidate.Card != selected))
            Participant.RunState.CurrentMapPointHistoryEntry?.GetEntry(Participant.NetId).CardChoices.Add(new CardChoiceHistoryEntry(skipped.Card, false));
        SetEventFinished(PageDescription("STUDY"));
    }

    private async Task SortLessons()
    {
        if (!CanParticipate || IsFinished || Participant.Gold < 70) return;
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
        CardModel? selected = (await CardSelectCmd.FromDeckForUpgrade(Participant, prefs)).SingleOrDefault();
        if (selected == null || !selected.IsUpgradable || !PileType.Deck.GetPile(Participant).Cards.Contains(selected)) return;
        if (!CanParticipate || IsFinished || Participant.Gold < 70) return;
        await PlayerCmd.LoseGold(70, Participant, GoldLossType.Spent);
        CardCmd.Upgrade([selected], CardPreviewStyle.GridLayout);
        SetEventFinished(PageDescription("SORT"));
    }

    public override List<(string, string)> Localization => new EventLoc(
        "无量山石室",
        new EventPageLoc("INITIAL", "你沿着石阶走入一间寂静的石室。卷帛上的步履轻灵，行气路径却让人心惊。尖塔外的旧日奇遇仿佛又近在眼前；这一次，你可以决定自己愿意学到哪一步。",
            new EventOptionLoc("STUDY", "依图行气", "失去[red]8[/red]点生命。从3张随机的段誉罕见技能牌中选择1张加入牌组。生命不足或没有候选时不可选。"),
            new EventOptionLoc("SORT", "整理所学", "支付[gold]70[/gold]金币。选择牌组中1张可升级牌并升级。金币不足或没有可升级牌时不可选。"),
            new EventOptionLoc("LEAVE", "长揖而去", "离开石室。")),
        new EventPageLoc("SPECTATOR", "卷帛上的旧日字迹似乎只对段誉述说着什么。你在石阶旁等候，准备继续前行。"),
        new EventPageLoc("SELECTION", "选择1张牌加入牌组；不选牌并确认可返回事件。"),
        new EventPageLoc("STUDY", "真气沿着经脉奔涌，新的招式在指尖成形。你忍住胸口的闷痛，把行气的要诀记在心中。"),
        new EventPageLoc("SORT", "你请同行的山民送来纸墨，将散乱的心得重新整理。纷乱的招式终于融会贯通。"),
        new EventPageLoc("LEAVE", "你向石室长揖一礼，轻轻合上卷帛，继续前行。"));
}
