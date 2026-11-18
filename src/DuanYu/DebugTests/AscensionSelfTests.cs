#if DEBUG
using Godot;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters.Mocks;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Runs;

namespace DuanYu.DebugTests;

internal static class AscensionSelfTests
{
    internal static Task Run()
    {
        var baseline = EngineSelfTests.CreateFixture(combat: false, ascensionLevel: 0);
        Equal(0, baseline.Run.AscensionLevel, "A0 run level");
        Equal(10, baseline.Player.Deck.Cards.Count, "A0 starting deck");
        Equal(0, baseline.Player.Deck.Cards.OfType<AscendersBane>().Count(), "A0 curse count");
        var baselinePotionSlots = baseline.Player.MaxPotionCount;
        Equal(3, baselinePotionSlots, "A0 potion slots");
        Equal(5, new MapPointTypeCounts(12, 5).NumOfElites, "A0 elite map count");
        Equal(75, new MerchantCardRemovalEntry(baseline.Player).Cost, "A0 removal price");
        Equal(25, MerchantCardRemovalEntry.PriceIncrease, "A0 removal price increase");
        Equal(10, ModelDb.Encounter<MockMonsterEncounter>().MinGoldReward, "A0 monster minimum gold");
        Equal(20, ModelDb.Encounter<MockMonsterEncounter>().MaxGoldReward, "A0 monster maximum gold");
        Equal(0.03f, CardRarityOdds.RegularRareOdds, "A0 regular rare odds");
        var baselineMinimumHp = ModelDb.Monster<Wriggler>().MinInitialHp;
        var baselineMaximumHp = ModelDb.Monster<Wriggler>().MaxInitialHp;
        Equal(17, baselineMinimumHp, "A0 Wriggler minimum HP");
        Equal(21, baselineMaximumHp, "A0 Wriggler maximum HP");
        RunManager.Instance.GenerateRooms();
        Equal(true, baseline.Run.Acts.All(act => !act.HasSecondBoss), "A0 has no second boss");
        GD.Print("DUANYU_A10_CHECK_PASS baseline-a0");

        var ascended = EngineSelfTests.CreateFixture(combat: false, ascensionLevel: 10);
        Equal(GameMode.Standard, ascended.Run.GameMode, "A10 standard mode");
        Equal(10, ascended.Run.AscensionLevel, "A10 run level");
        foreach (var level in Enum.GetValues<AscensionLevel>().Where(level => level != AscensionLevel.None))
            Equal(true, RunManager.Instance.HasAscension(level), "A10 active rule " + level);
        GD.Print("DUANYU_A10_CHECK_PASS all-ten-rules-active");

        Equal(11, ascended.Player.Deck.Cards.Count, "A10 starting deck");
        var curses = ascended.Player.Deck.Cards.OfType<AscendersBane>().ToArray();
        Equal(1, curses.Length, "A10 exactly one Ascender's Bane");
        foreach (var keyword in new[] { CardKeyword.Unplayable, CardKeyword.Ethereal, CardKeyword.Eternal })
            Equal(true, curses[0].Keywords.Contains(keyword), "A10 curse keyword " + keyword);
        Equal(1, curses[0].FloorAddedToDeck, "A10 curse starting floor");
        Equal(baselinePotionSlots - 1, ascended.Player.MaxPotionCount, "A10 one fewer potion slot");
        GD.Print("DUANYU_A10_CHECK_PASS starting-curse-and-potion-slots");

        Equal(8, new MapPointTypeCounts(12, 5).NumOfElites, "A10 elite map count");
        Equal(100, new MerchantCardRemovalEntry(ascended.Player).Cost, "A10 removal price");
        Equal(50, MerchantCardRemovalEntry.PriceIncrease, "A10 removal price increase");
        ascended.Player.ExtraFields.CardShopRemovalsUsed = 1;
        Equal(150, new MerchantCardRemovalEntry(ascended.Player).Cost, "A10 second removal price");
        Equal(7, ModelDb.Encounter<MockMonsterEncounter>().MinGoldReward, "A10 monster minimum gold");
        Equal(15, ModelDb.Encounter<MockMonsterEncounter>().MaxGoldReward, "A10 monster maximum gold");
        Equal(0.0149f, CardRarityOdds.RegularRareOdds, "A10 regular rare odds");
        Equal(0.05f, CardRarityOdds.EliteRareOdds, "A10 elite rare odds");
        Equal(0.045f, CardRarityOdds.ShopRareOdds, "A10 shop rare odds");
        GD.Print("DUANYU_A10_CHECK_PASS economy-and-map-pressure");

        Equal(baselineMinimumHp + 1, ModelDb.Monster<Wriggler>().MinInitialHp, "A10 Wriggler minimum HP increase");
        Equal(baselineMaximumHp + 1, ModelDb.Monster<Wriggler>().MaxInitialHp, "A10 Wriggler maximum HP increase");
        GD.Print("DUANYU_A10_CHECK_PASS enemy-hp-scaling");

        RunManager.Instance.GenerateRooms();
        Equal(3, ascended.Run.Acts.Count, "A10 standard act count");
        Equal(true, ascended.Run.Acts.Take(2).All(act => !act.HasSecondBoss), "A10 first two acts have one boss");
        var finalAct = ascended.Run.Acts[^1];
        Equal(true, finalAct.HasSecondBoss, "A10 final act second boss");
        Equal(true, finalAct.SecondBossEncounter != null, "A10 second boss encounter exists");
        Equal(false, finalAct.BossEncounter.Id == finalAct.SecondBossEncounter!.Id, "A10 final bosses differ");
        GD.Print("DUANYU_A10_CHECK_PASS distinct-final-double-boss");
        return Task.CompletedTask;
    }

    private static void Equal<T>(T expected, T actual, string label)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}.");
    }
}
#endif
