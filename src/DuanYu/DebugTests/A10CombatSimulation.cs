#if DEBUG
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using DuanYu.Cards;
using DuanYu.Runtime;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using File = System.IO.File;

namespace DuanYu.DebugTests;

internal static class A10CombatSimulation
{
    private sealed record Scenario(string Build, Func<EncounterModel> Encounter, string[]? Deck);
    private sealed record EnemySnapshot(string Id, int Hp, int Block, string Move);
    private sealed record PlaySnapshot(string Card, string? Target, int EnergyBefore, int EnergySpent, int EnergyAfter, int Qi, int Lingbo);
    private sealed record TurnSnapshot(int Turn, int StartHp, int StartEnergy, string[] Hand, EnemySnapshot[] Enemies, List<PlaySnapshot> Plays, int EndHp);
    private sealed record FightResult(string Build, string Encounter, int Ascension, string Seed, string Outcome, int StartingHp, int EndingHp, int Turns, int CardsPlayed, int EnergySpent, string[] Deck, EnemySnapshot[] InitialEnemies, List<TurnSnapshot> Trace, string? Error);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    internal static async Task Run()
    {
        SaveManager.Instance.MarkFtueAsComplete("shuffle_ftue");
        var sampleArgument = OS.GetCmdlineUserArgs().SingleOrDefault(argument => argument.StartsWith("--duanyu-sim-samples=", StringComparison.Ordinal));
        var samples = sampleArgument == null ? 12 : int.Parse(sampleArgument.Split('=')[1]);
        if (samples is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(samples));
        var scenarios = new List<Scenario>
        {
            new("starter", () => ModelDb.Encounter<NibbitsWeak>(), null),
            new("starter", () => ModelDb.Encounter<BowlbugsWeak>(), null)
        };
        var builds = new Dictionary<string, string[]>
        {
            ["beiming"] = ["B01", "B01", "B02", "B02", "B02", "B03+", "B04", "B05", "C09", "C09", "U02", "U07+", "U08", "U26", "U22", "C15+", "U01", "R13"],
            ["lingbo"] = ["B01", "B02", "B02", "B02", "B03", "B04", "B05", "C06+", "C06", "U18+", "U18", "U34", "U23", "U20", "C15", "U17", "R02+", "U07"],
            ["swords"] = ["B01", "B02", "B02", "B02", "B03", "B04", "B05", "C19+", "C19", "C07", "C08+", "U11", "U13", "U09", "U14", "R08", "R03+", "C09"]
        };
        foreach (var build in builds)
        {
            scenarios.Add(new Scenario(build.Key, () => ModelDb.Encounter<ByrdonisElite>(), build.Value));
            scenarios.Add(new Scenario(build.Key, () => ModelDb.Encounter<CeremonialBeastBoss>(), build.Value));
        }
        var results = new List<FightResult>();
        var reportPath = ProjectSettings.GlobalizePath("user://duanyu-a10-simulation.json");
        foreach (var scenario in scenarios)
        foreach (var ascension in new[] { 0, 10 })
        for (var sample = 1; sample <= samples; sample++)
        {
            var result = await Fight(scenario, ascension, $"DUANYUA10-{sample:000}");
            results.Add(result);
            File.WriteAllText(reportPath, JsonSerializer.Serialize(new
            {
                host = "STS2 0.113.1 public-beta",
                policy = "Visible-hand greedy policy v1; native costs, draw, turns, enemy AI and hooks; first legal card selection; no potions; starting relic only.",
                scope = "Independent fixed-deck combats, not complete runs. Starter HP uses native Neow healing without choosing a boon; constructed decks start at 72 HP, three upgrades, three energy. A10 keeps Ascender's Bane. Same seeds across A0/A10.",
                samplesPerScenario = samples,
                results
            }, JsonOptions));
            GD.Print($"DUANYU_SIM_RESULT build={result.Build} encounter={result.Encounter} A{ascension} seed={result.Seed} outcome={result.Outcome} hp={result.StartingHp}->{result.EndingHp} turns={result.Turns}");
            if (result.Error != null) GD.PrintErr(result.Error);
        }
        GD.Print($"DUANYU_SIM_COMPLETE fights={results.Count} wins={results.Count(result => result.Outcome == "win")} losses={results.Count(result => result.Outcome == "loss")} errors={results.Count(result => result.Error != null)} report={reportPath}");
        EngineSelfTests.Equal(0, results.Count(result => result.Error != null), "simulation exceptions/invariants");
        EngineSelfTests.Equal(0, results.Count(result => result.Outcome == "turn-limit"), "simulation turn-limit stalls");
    }

    private static async Task<FightResult> Fight(Scenario scenario, int ascension, string seed)
    {
        var trace = new List<TurnSnapshot>();
        var deckIds = Array.Empty<string>();
        var initialEnemies = Array.Empty<EnemySnapshot>();
        Player? player = null;
        var startingHp = 0;
        var turns = 0;
        var cardsPlayed = 0;
        var energySpent = 0;
        var encounterId = scenario.Encounter().Id.Entry;
        try
        {
            var fixture = EngineSelfTests.CreateFixture(combat: false, ascensionLevel: ascension, seed: seed);
            player = fixture.Player;
            if (scenario.Deck != null)
            {
                foreach (var card in player.Deck.Cards.Where(card => card is not AscendersBane).ToArray())
                {
                    player.Deck.RemoveInternal(card);
                    fixture.Run.RemoveCard(card);
                }
                foreach (var cardId in scenario.Deck)
                {
                    var card = fixture.Run.CreateCard(DuanYuCard.Canonical("DY_" + cardId.TrimEnd('+')), player);
                    if (cardId.EndsWith('+')) card.UpgradeInternal();
                    player.Deck.AddInternal(card);
                }
            }
            else
            {
                var neow = (Neow)ModelDb.Event<Neow>().ToMutable();
                AccessTools.Property(typeof(EventModel), "Owner").SetValue(neow, player);
                await (Task)AccessTools.Method(typeof(AncientEventModel), "BeforeEventStarted").Invoke(neow, [false])!;
            }
            startingHp = player.Creature.CurrentHp;
            EngineSelfTests.Equal(scenario.Deck == null && ascension == 10 ? 57 : 72, startingHp, "native Neow or constructed starting HP");
            deckIds = player.Deck.Cards.Select(CardId).ToArray();
            EngineSelfTests.Equal(ascension == 10 ? 1 : 0, player.Deck.Cards.OfType<AscendersBane>().Count(), "simulation curse count");
            var encounter = scenario.Encounter().ToMutable();
            var room = new CombatRoom(encounter, fixture.Run);
            fixture.Run.PushRoom(room);
            fixture.Run.AppendToMapPointHistory(encounter.RoomType == RoomType.Boss ? MapPointType.Boss : encounter.RoomType == RoomType.Elite ? MapPointType.Elite : MapPointType.Monster, encounter.RoomType, encounter.Id);
            var state = room.CombatState;
            state.AddPlayer(player);
            encounter.GenerateMonstersWithSlots(fixture.Run);
            foreach (var (monster, slot) in encounter.MonstersWithSlots)
                state.AddCreature(state.CreateCreature(monster, CombatSide.Enemy, slot));
            var manager = CombatManager.Instance;
            manager.SetUpCombat(state);
            await Hook.AfterRoomEntered(fixture.Run, room);
            foreach (var creature in state.Creatures) await manager.AfterCreatureAdded(creature);
            RunManager.Instance.ActionQueueSynchronizer.SetCombatState(ActionSynchronizerCombatState.NotPlayPhase);
            var turnState = AccessTools.Field(typeof(CombatManager), "_turnState").GetValue(manager)!;
            AccessTools.Property(turnState.GetType(), "IsInProgress").SetValue(turnState, true);
            AccessTools.Property(turnState.GetType(), "IsStarting").SetValue(turnState, false);
            await Hook.BeforeCombatStart(fixture.Run, state);
            initialEnemies = SnapshotEnemies(state);
            while (manager.IsInProgress && turns < 60)
            {
                await InvokeTurnMethod("StartTurn", turnState, null);
                if (!manager.IsInProgress) break;
                EngineSelfTests.Equal(CombatSide.Player, state.CurrentSide, "simulation player side");
                turns++;
                var startHp = player.Creature.CurrentHp;
                var startEnergy = player.PlayerCombatState!.Energy;
                EngineSelfTests.Equal(3, startEnergy, "simulation normal turn energy");
                var hand = PileType.Hand.GetPile(player).Cards.Select(CardId).ToArray();
                var enemies = SnapshotEnemies(state);
                var plays = new List<PlaySnapshot>();
                while (manager.IsInProgress && player.Creature.IsAlive)
                {
                    var candidate = PileType.Hand.GetPile(player).Cards.Where(card => card.CanPlay())
                        .Select(card => (Card: card, Target: ChooseTarget(card, state)))
                        .Where(option => option.Card.IsValidTarget(option.Target))
                        .Select(option => (option.Card, option.Target, Score: Score(option.Card, option.Target, state, player)))
                        .OrderByDescending(option => option.Score).FirstOrDefault();
                    if (candidate.Card == null || candidate.Score <= 0) break;
                    if (plays.Count >= 100) throw new InvalidOperationException("More than 100 cards in one turn; possible loop.");
                    var energyBefore = player.PlayerCombatState!.Energy;
                    var (energy, stars) = await candidate.Card.SpendResources();
                    EngineSelfTests.Equal(energyBefore - energy, player.PlayerCombatState.Energy, "manual play pays energy");
                    await candidate.Card.OnPlayWrapper(new ThrowingPlayerChoiceContext(), candidate.Target, false,
                        new ResourceInfo { EnergySpent = energy, EnergyValue = energy, StarsSpent = stars, StarValue = stars }, true);
                    cardsPlayed++;
                    energySpent += energy;
                    var resources = DuanYuState.Get(player);
                    EngineSelfTests.Equal(true, resources.Qi >= 0, "combat Qi remains nonnegative");
                    EngineSelfTests.Equal(true, resources.Lingbo is >= 0 and <= 3, "combat Lingbo bounds");
                    EngineSelfTests.Equal(true, player.PlayerCombatState.Energy >= 0, "nonnegative combat energy");
                    plays.Add(new PlaySnapshot(CardId(candidate.Card), candidate.Target?.Monster?.Id.Entry, energyBefore, energy, player.PlayerCombatState.Energy, resources.Qi, resources.Lingbo));
                    await manager.CheckWinCondition();
                }
                if (manager.IsInProgress)
                {
                    RunManager.Instance.ActionQueueSynchronizer.SetCombatState(ActionSynchronizerCombatState.EndTurnPhaseOne);
                    await InvokeTurnMethod("EndPlayerTurnPhaseOneInternal");
                }
                if (manager.IsInProgress)
                {
                    RunManager.Instance.ActionQueueSynchronizer.SetCombatState(ActionSynchronizerCombatState.NotPlayPhase);
                    await InvokeTurnMethod("EndPlayerTurnPhaseTwoInternal");
                }
                if (manager.IsInProgress) await InvokeTurnMethod("SwitchFromPlayerToEnemySide", turnState);
                if (manager.IsInProgress && state.CurrentSide == CombatSide.Enemy) await InvokeTurnMethod("StartTurn", turnState, null);
                trace.Add(new TurnSnapshot(turns, startHp, startEnergy, hand, enemies, plays, player.Creature.CurrentHp));
            }
            var outcome = player.Creature.IsDead ? "loss" : room.IsPreFinished ? "win" : "turn-limit";
            return new FightResult(scenario.Build, encounterId, ascension, seed, outcome, startingHp, player.Creature.CurrentHp, turns, cardsPlayed, energySpent, deckIds, initialEnemies, trace, null);
        }
        catch (Exception error)
        {
            return new FightResult(scenario.Build, encounterId, ascension, seed, "error", startingHp, player?.Creature.CurrentHp ?? 0, turns, cardsPlayed, energySpent, deckIds, initialEnemies, trace, error.ToString());
        }
    }

    private static Task InvokeTurnMethod(string name, params object?[] arguments)
    {
        var method = typeof(CombatManager).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(method => method.Name == name && method.GetParameters().Length == arguments.Length);
        return (Task)method.Invoke(CombatManager.Instance, arguments)!;
    }

    private static string CardId(CardModel card) => (card is DuanYuCard custom ? custom.DesignId : card.Id.Entry) + (card.IsUpgraded ? "+" : "");

    private static EnemySnapshot[] SnapshotEnemies(CombatState state) => state.Enemies
        .Select(enemy => new EnemySnapshot(enemy.Monster!.Id.Entry, enemy.CurrentHp, enemy.Block, enemy.Monster.NextMove.Id)).ToArray();

    private static Creature? ChooseTarget(CardModel card, CombatState state) => card.TargetType == TargetType.AnyEnemy
        ? state.Enemies.Where(enemy => enemy.IsAlive).OrderBy(enemy => enemy.CurrentHp + enemy.Block).FirstOrDefault()
        : null;

    private static double Score(CardModel model, Creature? target, CombatState state, Player player)
    {
        if (model is not DuanYuCard card) return 0;
        var resources = DuanYuState.Get(player);
        var text = card.IsUpgraded ? card.Definition.Upgrade.Text : card.Definition.Text;
        var incoming = state.Enemies.Where(enemy => enemy.IsAlive).Sum(enemy => enemy.Monster!.NextMove.Intents.OfType<AttackIntent>().Sum(intent => intent.GetTotalDamage([player.Creature], enemy)));
        var neededBlock = Math.Max(0, incoming - player.Creature.Block);
        double damage = card.Value(card.Numbers.Damage, card.Numbers.DamageUp);
        if (resources.Qi >= card.QiCost) damage += card.Value(card.Numbers.ExtraDamage, card.Numbers.ExtraDamageUp);
        if (card.DesignId == "DY_U08") damage += 2 * resources.Qi;
        if (card.TargetType == TargetType.AllEnemies) damage *= state.Enemies.Count(enemy => enemy.IsAlive);
        double block = card.Value(card.Numbers.Block, card.Numbers.BlockUp);
        if (card.DesignId == "DY_C15" && resources.Qi >= 2) block *= 2;
        if (card.DesignId == "DY_C06" && resources.Lingbo == 0) block = 0;
        var lingbo = Regex.Match(text, @"获得(\d+)层凌波");
        if (lingbo.Success) block += Math.Min(3 - resources.Lingbo, int.Parse(lingbo.Groups[1].Value)) * 3;
        double score = damage + 1.3 * Math.Min(neededBlock, block);
        if (target != null && damage >= target.CurrentHp + target.Block) score += 100;
        var gainQi = Regex.Match(text, @"运气(\d+)");
        if (gainQi.Success) score += int.Parse(gainQi.Groups[1].Value) * 2;
        var draw = Regex.Match(text, @"抽(\d+)张牌");
        if (draw.Success) score += int.Parse(draw.Groups[1].Value) * (player.PlayerCombatState!.Energy > card.EnergyCost.GetAmountToSpend() ? 4 : 0.5);
        if (text.Contains("易伤", StringComparison.Ordinal)) score += 3;
        if (text.Contains("虚弱", StringComparison.Ordinal)) score += Math.Min(5, neededBlock * 0.25);
        if (text.Contains("加入手牌", StringComparison.Ordinal)) score += card.DesignId == "DY_R03" ? resources.Qi >= 6 ? 35 : 0 : card.DesignId == "DY_U13" ? 15 : 8;
        if (card.Type == CardType.Power) score += 9;
        return score / Math.Max(1, card.EnergyCost.GetAmountToSpend());
    }
}
#endif
