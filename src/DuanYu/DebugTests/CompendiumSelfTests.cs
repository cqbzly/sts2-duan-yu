#if DEBUG
using DuanYu.Cards;
using DuanYu.Character;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace DuanYu.DebugTests;

internal static class CompendiumSelfTests
{
    internal static Task Run()
    {
        TestMode.AssertOn();
        var character = ModelDb.Character<DuanYuCharacter>();
        var pool = ModelDb.CardPool<DuanYuCardPool>();
        var cards = pool.AllCards.ToArray();
        EngineSelfTests.Equal(true, ModelDb.AllCharacters.Contains(character), "compendium character registered");
        EngineSelfTests.Equal(true, ModelDb.AllCardPools.Contains(pool), "compendium card pool registered");
        EngineSelfTests.Equal(false, character.HideInCompendium, "compendium character filter enabled");
        EngineSelfTests.Equal(character.Id, character.DefaultCompendiumOpenModelId, "compendium opens current character");
        EngineSelfTests.Equal(96, cards.Length, "compendium contains all 96 card designs");
        EngineSelfTests.Equal(96, cards.Select(card => card.Id).Distinct().Count(), "compendium card IDs unique");
        EngineSelfTests.Equal(2, cards.Count(card => card.Type == CardType.Status), "compendium includes both exclusive statuses");
        EngineSelfTests.Equal(true, cards.All(card => card is DuanYuCard && card.ShouldShowInCardLibrary), "compendium includes playable and token cards");
        EngineSelfTests.Equal(true, character.IconTexture.GetWidth() > 0, "compendium filter icon loads");
        var vanillaCard = ModelDb.Character<Ironclad>().CardPool.AllCards.First();
        var originalProgress = SaveManager.Instance.Progress;
        try
        {
            foreach (var hasPriorDiscoveries in new[] { false, true })
            {
                SaveManager.Instance.Progress = ProgressState.CreateDefault();
                if (hasPriorDiscoveries)
                {
                    SaveManager.Instance.Progress.MarkCardAsSeen(cards[0].Id);
                    SaveManager.Instance.Progress.MarkCardAsSeen(vanillaCard.Id);
                }
                var grid = new NCardLibraryGrid();
                try
                {
                    grid.RefreshVisibility();
                    CheckVisible(grid, cards);
                    EngineSelfTests.Equal(hasPriorDiscoveries, SaveManager.Instance.Progress.DiscoveredCards.Contains(vanillaCard.Id), "compendium preserves vanilla discovery state");
                    var discoveredCount = SaveManager.Instance.Progress.DiscoveredCards.Count;
                    grid.RefreshVisibility();
                    EngineSelfTests.Equal(discoveredCount, SaveManager.Instance.Progress.DiscoveredCards.Count, "compendium reopen is idempotent");
                    CheckVisible(grid, cards);
                }
                finally
                {
                    grid.Free();
                }
                GD.Print($"DUANYU_COMPENDIUM_CHECK_PASS profile={(hasPriorDiscoveries ? "existing" : "fresh")} visible={cards.Length}");
            }
        }
        finally
        {
            SaveManager.Instance.Progress = originalProgress;
        }
        return Task.CompletedTask;
    }

    private static void CheckVisible(NCardLibraryGrid grid, CardModel[] cards)
    {
        var visibility = AccessTools.Method(typeof(NCardLibraryGrid), "GetCardVisibility");
        var unlocked = ModelDb.CardPool<DuanYuCardPool>()
            .GetUnlockedCards(SaveManager.Instance.GenerateUnlockStateFromProgress(), CardMultiplayerConstraint.None)
            .Select(card => card.Id).ToHashSet();
        foreach (var card in cards)
        {
            EngineSelfTests.Equal(true, unlocked.Contains(card.Id), "compendium unlocked " + card.Id.Entry);
            EngineSelfTests.Equal(true, SaveManager.Instance.Progress.DiscoveredCards.Contains(card.Id), "compendium detail gate " + card.Id.Entry);
            EngineSelfTests.Equal(ModelVisibility.Visible, (ModelVisibility)visibility.Invoke(grid, [card])!, "compendium visible " + card.Id.Entry);
        }
    }
}
#endif
