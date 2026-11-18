#if DEBUG
using System.Diagnostics;
using System.Text.Json;
using DuanYu.Cards;
using DuanYu.Runtime;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using File = System.IO.File;

namespace DuanYu.DebugTests;

internal static class FullRunSimulation
{
    private sealed record BossVictory(int Act, int Floor, string Encounter, int Hp);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly List<object> Attempts = [];
    private static readonly List<object> Log = [];
    private static readonly List<BossVictory> WonBosses = [];
    private static string activeCard = "";
    private static string seed = "";
    private static Player? currentPlayer;
    private static RunState? currentRun;
    private static int lastCombatHp;
    private static bool sampleRuns;
    private static int requestedAttempts;
    private static int victories;
    private static int defeats;
    private static int errors;
    internal static bool IsRunning { get; private set; }

    internal static async Task Run()
    {
        SaveManager.Instance.SetFtuesEnabled(false);
        using var selector = CardSelectorStack.SetTestSelector(new RunSelector());
        var previousRewardsSelector = RewardsSet.testSelector;
        RewardsSet.testSelector = TakeRewards;
        IsRunning = true;
        var count = IntArgument("--duanyu-run-attempts=", 20);
        var start = IntArgument("--duanyu-run-start=", 1);
        var explicitSeed = OS.GetCmdlineUserArgs().SingleOrDefault(argument => argument.StartsWith("--duanyu-run-seed=", StringComparison.Ordinal));
        sampleRuns = OS.GetCmdlineUserArgs().Contains("--duanyu-run-sample");
        requestedAttempts = explicitSeed == null ? count : 1;
        Attempts.Clear();
        victories = 0;
        defeats = 0;
        errors = 0;
        try
        {
            if (sampleRuns && explicitSeed != null)
                throw new InvalidOperationException("Sampling mode accepts a seed range, not --duanyu-run-seed.");
            for (var attempt = start; attempt < start + count; attempt++)
            {
                seed = explicitSeed?.Split('=', 2)[1] ?? $"DUANYUFULLA10-{attempt:000}";
                Log.Clear();
                WonBosses.Clear();
                FullRunRooms.Decisions.Clear();
                currentPlayer = null;
                currentRun = null;
                lastCombatHp = 0;
                GD.Print($"DUANYU_FULL_RUN_START seed={seed}");
                var won = false;
                string? error = null;
                try
                {
                    var created = FullRunPath.Create(seed);
                    currentRun = created.Run;
                    currentPlayer = created.Player;
                    await FullRunPath.Start();
                    Record("start", Snapshot());
                    for (var roomCount = 0; roomCount < 100 && currentPlayer.Creature.IsAlive; roomCount++)
                    {
                        var room = currentRun.CurrentRoom ?? throw new InvalidOperationException("No room entered.");
                        Record("room-entered", new { room = room.RoomType.ToString(), model = room.ModelId?.Entry, state = Snapshot() });
                        if (room.IsVictoryRoom)
                        {
                            EngineSelfTests.Equal(3, currentRun.Acts.Count, "full run act count");
                            EngineSelfTests.Equal(2, currentRun.CurrentActIndex, "full run reaches final act");
                            EngineSelfTests.Equal(true, currentRun.Act.HasSecondBoss, "full run second boss enabled");
                            EngineSelfTests.Equal(currentRun.Map.SecondBossMapPoint!.coord, currentRun.CurrentMapCoord!.Value, "final boss coordinate visited");
                            await FinishArchitect((EventRoom)room);
                            EngineSelfTests.Equal(true, RunManager.Instance.HasRunEnded, "native win handler completed");
                            EngineSelfTests.Equal(true, RunManager.Instance.WinTime > 0, "native final boss victory time recorded");
                            EngineSelfTests.Equal(0, ((EventRoom)room).LocalMutableEvent.CurrentOptions.Count, "native victory option completed after WinRun");
                            EngineSelfTests.Equal(4, WonBosses.Count, "four native boss victories");
                            EngineSelfTests.Equal(2, WonBosses.Where(boss => boss.Act == 3).Select(boss => boss.Encounter).Distinct().Count(), "two different final act bosses defeated");
                            Record("native-run-completed", new
                            {
                                nativeEnded = RunManager.Instance.HasRunEnded,
                                winTime = RunManager.Instance.WinTime,
                                remainingVictoryOptions = ((EventRoom)room).LocalMutableEvent.CurrentOptions.Count,
                                bosses = WonBosses.ToArray()
                            });
                            won = true;
                            break;
                        }
                        if (room is CombatRoom combat)
                        {
                            await Fight(currentPlayer, combat);
                            lastCombatHp = currentPlayer.Creature.CurrentHp;
                            Record("combat-ended", new { encounter = combat.Encounter.Id.Entry, victory = combat.IsPreFinished, state = Snapshot() });
                            if (!currentPlayer.Creature.IsAlive) break;
                            EngineSelfTests.Equal(true, combat.IsPreFinished, "native combat victory");
                            if (combat.RoomType == RoomType.Boss)
                                WonBosses.Add(new BossVictory(currentRun.CurrentActIndex + 1, currentRun.TotalFloor, combat.Encounter.Id.Entry, lastCombatHp));
                            if (combat.Encounter.ShouldGiveRewards)
                            {
                                var rewards = await RewardsCmd.GenerateForRoomEnd(currentPlayer, combat);
                                await Hook.BeforeCombatRewardOffered(rewards, currentRun, combat);
                                await rewards.Offer();
                            }
                            await RunManager.Instance.ProceedFromTerminalRewardsScreenAsync();
                            if (!ReferenceEquals(currentRun.CurrentRoom, room)) continue;
                        }
                        else
                        {
                            await FullRunRooms.Resolve(currentPlayer, room);
                            Record("room-resolved", Snapshot());
                            if (!currentPlayer.Creature.IsAlive) break;
                            if (!ReferenceEquals(currentRun.CurrentRoom, room)) continue;
                        }
                        await FullRunPath.Advance(currentRun);
                    }
                    if (currentPlayer.Creature.IsAlive && !won) throw new InvalidOperationException("Full run room limit reached.");
                }
                catch (Exception exception)
                {
                    error = exception.ToString();
                    GD.PrintErr(error);
                }
                var outcome = error != null ? "error" : won ? "victory" : "defeat";
                var result = new { seed, ascension = 10, outcome, lastCombatHp, nativeEnded = RunManager.Instance.HasRunEnded, winTime = RunManager.Instance.WinTime,
                    bosses = WonBosses.ToArray(), state = Snapshot(), error, decisions = FullRunRooms.Decisions.ToArray(), log = Log.ToArray() };
                Attempts.Add(result);
                switch (outcome)
                {
                    case "victory": victories++; break;
                    case "defeat": defeats++; break;
                    default: errors++; break;
                }
                WriteReport(outcome);
                GD.Print($"DUANYU_FULL_RUN_RESULT seed={seed} outcome={outcome} act={currentRun?.CurrentActIndex + 1} floor={currentRun?.TotalFloor} hp={lastCombatHp}");
                if (error != null && !sampleRuns) throw new InvalidOperationException("Full run harness failed; see full report.");
                if (!sampleRuns && won) return;
                if (explicitSeed != null) break;
            }
            if (sampleRuns)
            {
                WriteReport("sample");
                GD.Print($"DUANYU_FULL_RUN_SAMPLE_RESULT attempted={Attempts.Count} victories={victories} defeats={defeats} errors={errors} winRate={WinRate():F4}");
                return;
            }
            throw new InvalidOperationException("No complete A10 victory in the attempted runs; defeats are retained in the report.");
        }
        finally
        {
            RewardsSet.testSelector = previousRewardsSelector;
            IsRunning = false;
        }
    }

    private static int IntArgument(string prefix, int fallback)
    {
        var argument = OS.GetCmdlineUserArgs().SingleOrDefault(argument => argument.StartsWith(prefix, StringComparison.Ordinal));
        var value = argument == null ? fallback : int.Parse(argument[prefix.Length..]);
        return value is >= 1 and <= 1000 ? value : throw new ArgumentOutOfRangeException(prefix);
    }

    private static async Task Fight(Player player, CombatRoom room)
    {
        var manager = CombatManager.Instance;
        var state = room.CombatState;
        var observedTurn = 0;
        var actionsThisTurn = 0;
        while (player.Creature.IsAlive && !room.IsPreFinished)
        {
            await WaitUntil(() => !player.Creature.IsAlive || room.IsPreFinished ||
                manager.IsInProgress && player.PlayerCombatState?.Phase == PlayerTurnPhase.Play &&
                !manager.IsPlayerReadyToEndTurn(player) && state.CurrentSide == CombatSide.Player && !RunManager.Instance.ActionExecutor.IsRunning,
                $"player turn {room.Encounter.Id.Entry}");
            if (!player.Creature.IsAlive || room.IsPreFinished) break;
            var turn = player.PlayerCombatState!.TurnNumber;
            if (observedTurn != turn)
            {
                observedTurn = turn;
                actionsThisTurn = 0;
                if (turn > 100) throw new InvalidOperationException("Full run combat exceeded 100 turns.");
                Record("turn", new { turn, hp = player.Creature.CurrentHp, block = player.Creature.Block, incoming = FullRunPolicy.IncomingDamage(player, state), energy = player.PlayerCombatState.Energy,
                    swordMomentum = DuanYuState.Get(player).SwordMomentum,
                    hand = PileType.Hand.GetPile(player).Cards.Select(CardId).ToArray(),
                    enemies = state.Enemies.Select(enemy => new { id = enemy.Monster!.Id.Entry, hp = enemy.CurrentHp, block = enemy.Block, move = enemy.Monster.NextMove.Id }).ToArray() });
                await UsePotions(player, state, room, false);
            }
            if (!manager.IsInProgress || !player.Creature.IsAlive) break;
            var choice = FullRunPolicy.ChoosePlay(player, state);
            if (choice.Card != null && choice.Score > 0)
            {
                if (++actionsThisTurn > 150) throw new InvalidOperationException("Full run exceeded 150 actions in one turn.");
                activeCard = choice.Card is DuanYuCard custom ? custom.DesignId : choice.Card.Id.Entry;
                var energyBefore = player.PlayerCombatState.Energy;
                var action = new PlayCardAction(choice.Card, choice.Target);
                await Execute(action);
                Record("play", new { card = CardId(choice.Card), target = choice.Target?.Monster?.Id.Entry, score = choice.Score, energyBefore, energyAfter = player.PlayerCombatState?.Energy, qi = DuanYuState.Get(player).Qi, lingbo = DuanYuState.Get(player).Lingbo, swordMomentum = DuanYuState.Get(player).SwordMomentum });
                activeCard = "";
                EngineSelfTests.Equal(true, DuanYuState.Get(player).Qi is >= 0 and <= 9, "full run Qi bounds");
                EngineSelfTests.Equal(true, DuanYuState.Get(player).SwordMomentum >= 0 && DuanYuState.Get(player).SwordMomentum <= DuanYuState.SwordMomentumCap, "full run sword momentum bounds");
                EngineSelfTests.Equal(true, DuanYuState.Get(player).Lingbo is >= 0 and <= 3, "full run Lingbo bounds");
                continue;
            }
            await UsePotions(player, state, room, true);
            if (!manager.IsInProgress || !player.Creature.IsAlive) break;
            await Execute(new EndPlayerTurnAction(player, turn));
            await WaitUntil(() => !player.Creature.IsAlive || room.IsPreFinished || player.PlayerCombatState!.TurnNumber > turn, "native enemy turn");
        }
        await RunManager.Instance.ActionExecutor.FinishedExecutingActions();
    }

    private static async Task UsePotions(Player player, CombatState state, CombatRoom room, bool endingTurn)
    {
        foreach (var potion in player.Potions.ToArray())
        {
            if (!CombatManager.Instance.IsInProgress || potion.Usage is PotionUsage.Automatic or PotionUsage.None) continue;
            var incoming = FullRunPolicy.IncomingDamage(player, state) - player.Creature.Block;
            var identifier = potion.Id.Entry;
            var shouldUse = endingTurn
                ? incoming >= 10 && (identifier.Contains("BLOCK", StringComparison.Ordinal) || identifier.Contains("WEAK", StringComparison.Ordinal) || identifier.Contains("GHOST", StringComparison.Ordinal))
                : (room.RoomType is RoomType.Elite or RoomType.Boss || player.Creature.CurrentHp < player.Creature.MaxHp * 0.6) &&
                  !identifier.Contains("BLOCK", StringComparison.Ordinal) && !identifier.Contains("WEAK", StringComparison.Ordinal) && !identifier.Contains("GHOST", StringComparison.Ordinal);
            if (!shouldUse) continue;
            var target = potion.TargetType switch
            {
                TargetType.AnyEnemy => state.Enemies.Where(enemy => enemy.IsAlive).OrderBy(enemy => enemy.CurrentHp).FirstOrDefault(),
                TargetType.Self or TargetType.AnyPlayer => player.Creature,
                _ => null
            };
            if (!potion.IsValidTarget(target)) continue;
            Record("potion", new { potion = identifier, target = target?.Monster?.Id.Entry });
            await Execute(new UsePotionAction(potion, target, true));
        }
    }

    private static async Task Execute(GameAction action)
    {
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(action);
        await WaitUntil(() => action.CompletionTask.IsCompleted, action.ToString() ?? action.GetType().Name);
        await action.CompletionTask;
        if (action.Exception != null) throw new InvalidOperationException("Native action failed", action.Exception);
        await RunManager.Instance.ActionExecutor.FinishedExecutingActions();
    }

    internal static async Task WaitUntil(Func<bool> condition, string context)
    {
        var timer = Stopwatch.StartNew();
        var tree = (SceneTree)Engine.GetMainLoop();
        while (!condition())
        {
            var turnLoop = AccessTools.Property(typeof(CombatManager), "DebugOnlyCurrentTurnLoopTask").GetValue(CombatManager.Instance) as Task;
            if (turnLoop?.IsFaulted == true) throw new InvalidOperationException("Native turn loop failed", turnLoop.Exception);
            if (timer.Elapsed > TimeSpan.FromSeconds(30)) throw new TimeoutException("Waiting for " + context);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static async Task TakeRewards(RewardsSet rewards)
    {
        var synchronizer = RunManager.Instance.RewardsSetSynchronizer;
        foreach (var reward in rewards.Rewards.ToArray())
        {
            Record("reward", new { type = reward.GetType().Name, description = reward.Description.GetFormattedText(), choices = reward is CardReward cards ? cards.Cards.Select(CardId).ToArray() : null });
            await synchronizer.SelectLocalReward(reward);
        }
        if (!synchronizer.IsRewardsSetCompleted(rewards)) synchronizer.SkipLocalRewardsSet();
    }

    private static async Task FinishArchitect(EventRoom room)
    {
        for (var choice = 0; choice < 30 && !RunManager.Instance.HasRunEnded; choice++)
        {
            var model = room.LocalMutableEvent;
            await WaitUntil(() => model.CurrentOptions.Count > 0 || RunManager.Instance.HasRunEnded, "Architect dialogue");
            if (RunManager.Instance.HasRunEnded) break;
            RunManager.Instance.EventSynchronizer.ChooseLocalOption(0);
            await RunManager.Instance.EventSynchronizer.AwaitPendingOptionTasks();
        }
    }

    internal static void Record(string kind, object details)
    {
        Log.Add(new { kind, act = currentRun?.CurrentActIndex + 1, floor = currentRun?.TotalFloor, details });
        if (kind is "room-entered" or "room-resolved" or "combat-ended")
        {
            GD.Print("DUANYU_FULL_RUN_STEP " + JsonSerializer.Serialize(Log[^1]));
            WriteReport("running");
        }
    }

    private static object Snapshot() => new
    {
        act = currentRun?.CurrentActIndex + 1,
        floor = currentRun?.TotalFloor,
        coord = currentRun?.CurrentMapCoord?.ToString(),
        hp = currentPlayer?.Creature.CurrentHp,
        maxHp = currentPlayer?.Creature.MaxHp,
        gold = currentPlayer?.Gold,
        deck = currentPlayer?.Deck.Cards.Select(CardId).ToArray(),
        relics = currentPlayer?.Relics.Select(relic => relic.Id.Entry).ToArray(),
        potions = currentPlayer?.Potions.Select(potion => potion.Id.Entry).ToArray()
    };

    private static void WriteReport(string outcome)
    {
        var path = ProjectSettings.GlobalizePath("user://duanyu-full-run.json");
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(new { host = "STS2 0.113.1 public-beta", mode = "Standard", ascension = 10,
            method = "Native complete runs: generated map, legal travel, original starter deck, persistent HP/gold/deck/relics, native rewards/shop/rest/events, normal action queue and enemy turns; no stat boosts, forced rewards, midrun resets or save scumming.",
            outcome, seed, sample = sampleRuns, summary = new { requested = requestedAttempts, attempted = Attempts.Count, victories, defeats, errors, winRate = WinRate() }, attempts = Attempts, current = new { state = Snapshot(), log = Log } }, JsonOptions));
        for (var retry = 0; ; retry++)
        {
            try
            {
                File.Move(temporaryPath, path, true);
                return;
            }
            catch (IOException exception) when (retry < 200 && (exception.HResult & 0xffff) is 32 or 33)
            {
                Thread.Sleep(25);
            }
        }
    }

    private static double WinRate() => victories + defeats == 0 ? 0 : (double)victories / (victories + defeats);

    private static string CardId(CardModel card) => (card is DuanYuCard custom ? custom.DesignId : card.Id.Entry) + (card.IsUpgraded ? "+" : "");

    private sealed class RunSelector : ICardSelector
    {
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
        {
            var cards = options.ToArray();
            if (cards.Length == 0) return Task.FromResult<IEnumerable<CardModel>>([]);
            var player = currentPlayer ?? cards.First().Owner;
            var purpose = FullRunSelectionContext.Purpose ?? FullRunRooms.SelectionPurpose;
            var exhaust = purpose == "exhaust" || activeCard is "DY_U06" or "DY_U35";
            var remove = purpose is "remove" or "transform" or "discard" or "absorb";
            var source = PileType.Play.GetPile(player).Cards.OfType<DuanYuCard>().LastOrDefault(card => card.DesignId == activeCard);
            double SelectionValue(CardModel card) => exhaust ? FullRunPolicy.ExhaustPriority(card, player, source) : remove ? FullRunPolicy.RemovalValue(card, player) : FullRunPolicy.CardValue(card, player);
            var ordered = cards.OrderByDescending(SelectionValue);
            var desired = exhaust ? cards.Count(card => SelectionValue(card) > 0) : remove ? cards.Count(card => SelectionValue(card) >= 7) : maxSelect;
            var count = Math.Max(minSelect, Math.Min(maxSelect, desired));
            var selected = ordered.Take(count).ToArray();
            Record("card-selection", new { purpose, activeCard, offered = cards.Select(CardId).ToArray(), selected = selected.Select(CardId).ToArray() });
            return Task.FromResult<IEnumerable<CardModel>>(selected);
        }

        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options, IReadOnlyList<CardRewardAlternative> alternatives)
        {
            var player = currentPlayer!;
            var best = options.Select(option => option.Card).OrderByDescending(card => FullRunPolicy.CardValue(card, player)).FirstOrDefault();
            var threshold = player.Deck.Cards.Count < 16 ? 11 : player.Deck.Cards.Count < 24 ? 17 : 22;
            if (best != null && FullRunPolicy.CardValue(best, player) < threshold) best = null;
            Record("reward-selection", new { offered = options.Select(option => CardId(option.Card)).ToArray(), chosen = best == null ? null : CardId(best) });
            return new CardRewardSelection { card = best };
        }
    }
}
#endif
