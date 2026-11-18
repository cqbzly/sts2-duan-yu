#if DEBUG
using System.Diagnostics;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Entities.TreasureRelicPicking;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace DuanYu.DebugTests;

internal static class FullRunRooms
{
    internal static string SelectionPurpose { get; set; } = "default";
    internal static List<string> Decisions { get; } = [];

    internal static async Task Resolve(Player player, AbstractRoom room)
    {
        switch (room)
        {
            case TreasureRoom treasure:
                await Treasure(player, treasure);
                break;
            case RestSiteRoom:
                await Rest(player);
                break;
            case MerchantRoom:
                await Shop(player);
                break;
            case EventRoom eventRoom:
                await Event(player, eventRoom);
                break;
            default:
                throw new InvalidOperationException($"Unsupported noncombat room {room.GetType().Name}.");
        }
    }

    private static async Task Treasure(Player player, TreasureRoom room)
    {
        if (NRun.Instance != null)
            throw new InvalidOperationException("Full-run treasure adapter requires a test session without a live run UI.");
        var synchronizer = RunManager.Instance.TreasureRoomRelicSynchronizer;
        var awardTasks = new List<Task>();
        void Award(List<RelicPickingResult> results, bool finished)
        {
            foreach (var result in results)
            {
                if (result.type == RelicPickingResultType.Skipped || result.player == null) continue;
                Log($"treasure relic={result.relic.Id.Entry}");
                awardTasks.Add(RelicCmd.Obtain(result.relic.ToMutable(), result.player));
                foreach (var otherPlayer in player.RunState.Players.Where(otherPlayer => otherPlayer != result.player))
                    otherPlayer.RelicGrabBag.MoveToFallback(result.relic);
            }
        }
        synchronizer.RelicsAwarded += Award;
        try
        {
            var gold = await room.DoNormalRewards();
            Log($"treasure gold={gold}");
            if (synchronizer.CurrentRelics is { Count: > 0 })
            {
                synchronizer.PickRelicLocally(0);
                await RunManager.Instance.ActionExecutor.FinishedExecutingActions();
                await Task.WhenAll(awardTasks);
                if (synchronizer.CurrentRelics != null)
                    throw new InvalidOperationException("Treasure relic vote did not finish.");
            }
            await room.DoExtraRewardsIfNeeded();
        }
        finally
        {
            synchronizer.RelicsAwarded -= Award;
        }
    }

    private static async Task Rest(Player player)
    {
        var synchronizer = RunManager.Instance.RestSiteSynchronizer;
        var attempted = new HashSet<string>();
        for (var selection = 0; selection < 8; selection++)
        {
            var options = synchronizer.GetLocalOptions();
            var choice = options.Select((option, index) => (Option: option, Index: index))
                .Where(candidate => candidate.Option.IsEnabled && !attempted.Contains(candidate.Option.OptionId))
                .OrderByDescending(candidate => RestValue(candidate.Option, player))
                .FirstOrDefault();
            if (choice.Option == null) return;
            attempted.Add(choice.Option.OptionId);
            var previous = SelectionPurpose;
            SelectionPurpose = choice.Option is SmithRestSiteOption ? "upgrade" : "default";
            try
            {
                var hpBefore = player.Creature.CurrentHp;
                var success = await synchronizer.ChooseLocalOption(choice.Index);
                Log($"rest option={choice.Option.OptionId} success={success} hp={hpBefore}->{player.Creature.CurrentHp}");
            }
            finally
            {
                SelectionPurpose = previous;
            }
        }
        throw new InvalidOperationException("Rest-site selection exceeded eight choices.");
    }

    private static double RestValue(RestSiteOption option, Player player) => option switch
    {
        HealRestSiteOption => player.Creature.CurrentHp < player.Creature.MaxHp *
            (player.RunState.CurrentMapPoint?.Children.Any(point => point.PointType == MapPointType.Boss) == true ? 0.95 : 0.72) ? 100 : 2,
        SmithRestSiteOption => 50,
        _ => 10
    };

    private static async Task Shop(Player player)
    {
        var synchronizer = RunManager.Instance.MerchantSynchronizer;
        var inventory = synchronizer.GetLocalInventory();
        var attempted = new HashSet<MerchantEntry>();
        try
        {
            for (var purchase = 0; purchase < 20; purchase++)
            {
                var entry = inventory.AllEntries.Where(entry => entry.IsStocked && entry.EnoughGold && !attempted.Contains(entry))
                    .Select(entry => (Entry: entry, Score: ShopValue(entry, player)))
                    .Where(candidate => candidate.Score > 0)
                    .OrderByDescending(candidate => candidate.Score)
                    .Select(candidate => candidate.Entry).FirstOrDefault();
                if (entry == null) return;
                attempted.Add(entry);
                var item = ShopItem(entry);
                var previous = SelectionPurpose;
                SelectionPurpose = entry is MerchantCardRemovalEntry ? "remove" : "default";
                try
                {
                    var goldBefore = player.Gold;
                    var cost = entry.Cost;
                    var success = await synchronizer.PurchaseLocalEntry(entry);
                    Log($"shop item={item} cost={cost} success={success} gold={goldBefore}->{player.Gold}");
                    if (player.Gold < 0) throw new InvalidOperationException("Shop purchase produced negative gold.");
                }
                finally
                {
                    SelectionPurpose = previous;
                }
            }
            throw new InvalidOperationException("Shop selection exceeded twenty purchases.");
        }
        finally
        {
            if (TestMode.IsOn) synchronizer.OnMerchantExited();
        }
    }

    private static double ShopValue(MerchantEntry entry, Player player) => entry switch
    {
        MerchantRelicEntry relic => (relic.Model?.Id.Entry is "JUZU_BRACELET" or "THE_COURIER" or "MEMBERSHIP_CARD" ? 8d : 30d) / Math.Max(1, entry.Cost),
        MerchantCardEntry card when card.CreationResult != null && player.Deck.Cards.Count < 26 =>
            Math.Max(0, FullRunPolicy.CardValue(card.CreationResult.Card, player) - 7) / Math.Max(1, entry.Cost),
        MerchantCardRemovalEntry when player.Deck.Cards.Count >= 16 => 25d / Math.Max(1, entry.Cost),
        MerchantPotionEntry when player.HasOpenPotionSlots => (!player.Potions.Any() && player.RunState.ActFloor >= 13 ? 30d : 9d) / Math.Max(1, entry.Cost),
        _ => 0
    };

    private static string ShopItem(MerchantEntry entry) => entry switch
    {
        MerchantCardEntry card => card.CreationResult?.Card.Id.Entry ?? "card",
        MerchantRelicEntry relic => relic.Model?.Id.Entry ?? "relic",
        MerchantPotionEntry potion => potion.Model?.Id.Entry ?? "potion",
        MerchantCardRemovalEntry => "remove-card",
        _ => entry.GetType().Name
    };

    private static async Task Event(Player player, EventRoom room)
    {
        var synchronizer = RunManager.Instance.EventSynchronizer;
        for (var page = 0; page < 24; page++)
        {
            var model = synchronizer.GetLocalEvent();
            if (model.IsFinished || player.Creature.IsDead || player.RunState.CurrentRoom is CombatRoom) return;
            Log($"event-options id={model.Id.Entry} page={page} options={string.Join('|', model.CurrentOptions.Select(option => option.TextKey + ":" + (option.IsProceed ? "proceed" : "choice")))}");
            var choice = model.CurrentOptions.Select((option, index) => (Option: option, Index: index))
                .Where(candidate => !candidate.Option.IsLocked && !candidate.Option.WasChosen && candidate.Option.WillKillPlayer?.Invoke(player) != true)
                .OrderByDescending(candidate => EventValue(candidate.Option, player))
                .FirstOrDefault();
            if (choice.Option == null)
                throw new InvalidOperationException($"Event {model.Id} has no legal nonlethal choice on page {page}.");
            var previous = SelectionPurpose;
            var key = choice.Option.TextKey.ToUpperInvariant();
            SelectionPurpose = key.Contains("UPGRADE", StringComparison.Ordinal) ? "upgrade" :
                key.Contains("REMOVE", StringComparison.Ordinal) || choice.Option.Relic?.Id.Entry == "PRECISE_SCISSORS" ? "remove" : "default";
            try
            {
                Log($"event id={model.Id.Entry} page={page} option={choice.Option.TextKey}");
                synchronizer.ChooseLocalOption(choice.Index);
                var pending = synchronizer.AwaitPendingOptionTasks();
                var timeout = Stopwatch.StartNew();
                while (!pending.IsCompleted)
                {
                    if (player.RunState.CurrentRoom is CombatRoom) return;
                    if (timeout.Elapsed > TimeSpan.FromSeconds(15))
                        throw new TimeoutException($"Event {model.Id} option {choice.Option.TextKey} did not complete.");
                    var tree = (SceneTree)Engine.GetMainLoop();
                    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                }
                await pending;
            }
            finally
            {
                SelectionPurpose = previous;
            }
        }
        throw new InvalidOperationException($"Event {room.ModelId} exceeded twenty-four pages.");
    }

    private static double EventValue(EventOption option, Player player)
    {
        var key = option.TextKey.ToUpperInvariant();
        if (option.IsProceed) return 1000;
        if (key.StartsWith("TABLET_OF_TRUTH.", StringComparison.Ordinal))
            return key.EndsWith("SMASH", StringComparison.Ordinal) || key.EndsWith("GIVE_UP", StringComparison.Ordinal) ? 200 : 0;
        if (key.StartsWith("MORPHIC_GROVE.", StringComparison.Ordinal))
            return key.EndsWith("LONER", StringComparison.Ordinal) ? 200 : 0;
        if (key.StartsWith("JUNGLE_MAZE_ADVENTURE.", StringComparison.Ordinal))
            return key.EndsWith("JOIN_FORCES", StringComparison.Ordinal) ? 200 : 0;
        if (option.Relic != null && key.StartsWith("NEOW.", StringComparison.Ordinal))
            return option.Relic.Id.Entry switch
            {
                "STONE_HUMIDIFIER" => 180,
                "NUTRITIOUS_OYSTER" => 170,
                "LOST_COFFER" => 160,
                "PHIAL_HOLSTER" => 150,
                "SCROLL_BOXES" => 145,
                "NEW_LEAF" or "PRECISE_SCISSORS" => 135,
                "BOOMING_CONCH" or "LAVA_ROCK" or "KALEIDOSCOPE" => 60,
                _ => 100
            };
        if (option.Relic != null) return 100;
        if (key.Contains("HEAL", StringComparison.Ordinal) || key.Contains("REST", StringComparison.Ordinal))
            return player.Creature.CurrentHp < player.Creature.MaxHp * 0.7 ? 80 : 12;
        if (key.Contains("UPGRADE", StringComparison.Ordinal)) return 60;
        if (key.Contains("REMOVE", StringComparison.Ordinal)) return 55;
        if (key.Contains("LEAVE", StringComparison.Ordinal) || key.Contains("IGNORE", StringComparison.Ordinal) || key.Contains("REFUSE", StringComparison.Ordinal)) return 30;
        if (key.Contains("FIGHT", StringComparison.Ordinal) || key.Contains("ATTACK", StringComparison.Ordinal)) return 5;
        return 20;
    }

    private static void Log(string message)
    {
        Decisions.Add(message);
        GD.Print($"DUANYU_FULL_RUN_CHOICE {message}");
    }
}
#endif
