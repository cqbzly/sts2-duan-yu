#if DEBUG
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using DuanYu.Cards;
using DuanYu.Character;
using DuanYu.Runtime;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Encounters.Mocks;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using File = System.IO.File;

namespace DuanYu.DebugTests;

internal sealed record TestFixture(Player Player, RunState Run, CombatState Combat, Creature Enemy);
internal sealed record EngineTestResult(string Name, bool Passed, string? Error = null);

internal static class EngineSelfTests
{
    private static readonly List<EngineTestResult> Results = [];
    private static bool started;

    public static void StartIfRequested()
    {
        if (started || !OS.GetCmdlineUserArgs().Contains("--duanyu-self-test")) return;
        started = true;
        _ = RunWhenReady();
    }

    private static async Task RunWhenReady()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var timer = Stopwatch.StartNew();
        try
        {
            while (NGame.Instance?.MainMenu is not { } menu || !menu.IsNodeReady())
            {
                if (timer.Elapsed > TimeSpan.FromSeconds(90)) throw new TimeoutException("Main menu did not initialize for Duan Yu self-tests.");
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
            TestMode.IsOn = true;
            using var selector = CardSelectorStack.SetTestSelector(new FirstLegalSelector());
            foreach (var prefixAndCount in new[] { ("B", 5), ("C", 20), ("U", 35), ("R", 20), ("T", 6) })
            {
                for (var number = 1; number <= prefixAndCount.Item2; number++)
                {
                    var id = $"DY_{prefixAndCount.Item1}{number:00}";
                    foreach (var upgraded in new[] { false, true })
                        await RunCase($"card:{id}{(upgraded ? "+" : "")}", () => ExerciseCard(id, upgraded));
                }
            }
            await RunCase("runtime:initial-deck-sequence", InitialDeckSequence);
            await RunCase("runtime:spending-cannot-borrow-pendant", CannotBorrowPendant);
            await RunCase("runtime:lingbo-five-hits", LingboFiveHits);
            await RunCase("runtime:lingbo-zero-and-non-attack", LingboNonAttacks);
            await RunCase("runtime:lingbo-cap-only-new-hit", LingboCapOnlyHit);
            await RunCase("runtime:lingbo-preview-is-readonly", LingboPreview);
            await RunCase("runtime:sword-bonus-after-enchantment", SwordDamageOrdering);
            await RunCase("runtime:wind-block-after-damage", WindBlockAfterDamage);
            await RunCase("runtime:ability-limit-is-not-reset", AbilityLimit);
            await RunCase("runtime:turn-start-clears-old-lingbo", TurnStartLingbo);
            await RunCase("runtime:generated-sword-overflow", SwordOverflow);
            await RunCase("runtime:ethereal-precedes-retain", EtherealBeforeRetain);
            await RunCase("runtime:run-history-save-roundtrip", HistoryRoundTrip);
            await RunCase("events:engine-boundaries", EventsSelfTest.Run);
        }
        catch (Exception error)
        {
            Results.Add(new EngineTestResult("harness", false, error.ToString()));
            GD.PrintErr(error);
        }
        finally
        {
            var passed = Results.Count(result => result.Passed);
            var failed = Results.Count - passed;
            var reportPath = ProjectSettings.GlobalizePath("user://duanyu-engine-tests.json");
            File.WriteAllText(reportPath, JsonSerializer.Serialize(new { passed, failed, tests = Results }, new JsonSerializerOptions { WriteIndented = true }));
            GD.Print($"DUANYU_SELF_TEST_RESULT passed={passed} failed={failed} report={reportPath}");
            tree.Quit(failed == 0 ? 0 : 1);
        }
    }

    private static async Task RunCase(string name, Func<Task> action)
    {
        try
        {
            await action();
            Results.Add(new EngineTestResult(name, true));
            GD.Print($"DUANYU_TEST_PASS {name}");
        }
        catch (Exception error)
        {
            Results.Add(new EngineTestResult(name, false, error.ToString()));
            GD.PrintErr($"DUANYU_TEST_FAIL {name}: {error}");
        }
    }

    public static TestFixture CreateFixture(bool duanYu = true, bool combat = true)
    {
        TestMode.AssertOn();
        if (AccessTools.Property(typeof(RunManager), "State").GetValue(RunManager.Instance) != null) RunManager.Instance.CleanUp();
        var character = duanYu ? (CharacterModel)ModelDb.Character<DuanYuCharacter>() : ModelDb.Character<Ironclad>();
        var player = Player.CreateForNewRun(character, UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "DUANYUTEST");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService(), shouldSave: false);
        LocalContext.NetId = player.NetId;
        var encounter = (EncounterModel)ModelDb.Encounter<MockMonsterEncounter>().ToMutable();
        var state = new CombatState(encounter, run);
        if (!combat) return new TestFixture(player, run, state, null!);
        state.AddPlayer(player);
        var enemy = state.CreateCreature(ModelDb.Monster<BigDummy>().ToMutable(), CombatSide.Enemy, null);
        state.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(state);
        var turnState = AccessTools.Field(typeof(CombatManager), "_turnState").GetValue(CombatManager.Instance)!;
        AccessTools.Property(turnState.GetType(), "IsInProgress").SetValue(turnState, true);
        AccessTools.Property(turnState.GetType(), "IsStarting").SetValue(turnState, false);
        player.PlayerCombatState!.Energy = 99;
        return new TestFixture(player, run, state, enemy);
    }

    private static async Task<TestFixture> ReadyFixture()
    {
        var fixture = CreateFixture();
        await Hook.BeforeCombatStart(fixture.Run, fixture.Combat);
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [fixture.Player.Creature]);
        await Hook.BeforeHandDraw(fixture.Combat, fixture.Player, new ThrowingPlayerChoiceContext());
        foreach (var pile in fixture.Player.PlayerCombatState!.AllPiles) pile.Clear();
        for (var number = 0; number < 12; number++)
            PileType.Draw.GetPile(fixture.Player).AddInternal(fixture.Combat.CreateCard(ModelDb.Card<DY_B01>(), fixture.Player));
        return fixture;
    }

    private static async Task ExerciseCard(string id, bool upgraded)
    {
        var fixture = await ReadyFixture();
        var player = fixture.Player;
        await DuanYuState.GainQi(player, 9);
        await DuanYuState.GainLingbo(player, 2);
        foreach (var supportId in new[] { "DY_B01", "DY_B02", "DY_T01", "DY_T05" })
            AddCard(fixture, supportId, PileType.Hand);
        AddCard(fixture, "DY_B01", PileType.Discard);
        AddCard(fixture, "DY_B02", PileType.Exhaust);
        var card = await Play(fixture, id, upgraded);
        var state = DuanYuState.Get(player);
        Equal(true, state.Qi is >= 0 and <= 9, id + " Qi bounds");
        Equal(true, state.Lingbo is >= 0 and <= 3, id + " Lingbo bounds");
        Equal(true, player.Creature.IsAlive, id + " player survives");
        Equal(true, !PileType.Play.GetPile(player).Cards.Contains(card), id + " leaves play pile");
        if (card.Type != CardType.Power && card.Keywords.Contains(CardKeyword.Exhaust))
            Equal(true, PileType.Exhaust.GetPile(player).Cards.Contains(card), id + " exhaust keyword");
    }

    internal static DuanYuCard AddCard(TestFixture fixture, string id, PileType pile, bool upgraded = false)
    {
        var card = (DuanYuCard)fixture.Combat.CreateCard(DuanYuCard.Canonical(id), fixture.Player);
        if (upgraded) card.UpgradeInternal();
        pile.GetPile(fixture.Player).AddInternal(card);
        return card;
    }

    internal static async Task<DuanYuCard> Play(TestFixture fixture, string id, bool upgraded = false)
    {
        var card = AddCard(fixture, id, PileType.Hand, upgraded);
        await PlayCard(fixture, card);
        return card;
    }

    private static async Task PlayCard(TestFixture fixture, DuanYuCard card)
    {
        var target = card.TargetType switch
        {
            TargetType.AnyEnemy => fixture.Enemy,
            TargetType.AnyPlayer => fixture.Player.Creature,
            _ => null
        };
        await card.OnPlayWrapper(new ThrowingPlayerChoiceContext(), target, true, new ResourceInfo { EnergySpent = 0, EnergyValue = card.EnergyCost.GetWithModifiers(CostModifiers.All), StarsSpent = 0, StarValue = 0 }, true);
    }

    private static async Task InitialDeckSequence()
    {
        var fixture = await ReadyFixture();
        var initialHp = fixture.Enemy.CurrentHp;
        await Play(fixture, "DY_B03");
        await Play(fixture, "DY_B04");
        await Play(fixture, "DY_B05");
        Equal(3, DuanYuState.Get(fixture.Player).Qi, "initial Qi");
        Equal(1, DuanYuState.Get(fixture.Player).Lingbo, "initial Lingbo");
        Equal(9, fixture.Player.Creature.Block, "initial block");
        Equal(12, initialHp - fixture.Enemy.CurrentHp, "initial damage");
    }

    private static async Task CannotBorrowPendant()
    {
        var fixture = await ReadyFixture();
        await DuanYuState.SpendQi(fixture.Player, 1);
        await Play(fixture, "DY_C15");
        Equal(7, fixture.Player.Creature.Block, "insufficient Qi skips extra block");
        Equal(2, DuanYuState.Get(fixture.Player).Qi, "pendant arrives after skill");
    }

    private static async Task LingboFiveHits()
    {
        var fixture = await ReadyFixture();
        await DuanYuState.GainLingbo(fixture.Player, 3);
        fixture.Player.Creature.GainBlockInternal(4);
        var initialHp = fixture.Player.Creature.CurrentHp;
        for (var hit = 0; hit < 5; hit++)
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 5, ValueProp.Move, fixture.Enemy);
        Equal(12, initialHp - fixture.Player.Creature.CurrentHp, "five attacks HP loss");
        Equal(0, DuanYuState.Get(fixture.Player).Lingbo, "all Lingbo consumed");
        Equal(3, DuanYuState.Get(fixture.Player).LingboTriggers, "three reductions");
    }

    private static async Task LingboNonAttacks()
    {
        var fixture = await ReadyFixture();
        await DuanYuState.GainLingbo(fixture.Player, 1);
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 0, ValueProp.Move, fixture.Enemy);
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 5, ValueProp.Unpowered | ValueProp.Unblockable, fixture.Enemy);
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 2, ValueProp.Move | ValueProp.Unpowered | ValueProp.Unblockable, fixture.Enemy);
        Equal(1, DuanYuState.Get(fixture.Player).Lingbo, "nonattacks preserve Lingbo");
        fixture.Player.Creature.GainBlockInternal(20);
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 5, ValueProp.Move, fixture.Enemy);
        Equal(18, fixture.Player.Creature.Block, "full block still consumes Lingbo");
        Equal(0, DuanYuState.Get(fixture.Player).Lingbo, "full block consumes layer");
    }

    private static async Task WindBlockAfterDamage()
    {
        var fixture = await ReadyFixture();
        await Play(fixture, "DY_U23");
        await DuanYuState.GainLingbo(fixture.Player, 1);
        var initialHp = fixture.Player.Creature.CurrentHp;
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 5, ValueProp.Move, fixture.Enemy);
        Equal(2, initialHp - fixture.Player.Creature.CurrentHp, "wind does not retroactively block");
        Equal(4, fixture.Player.Creature.Block, "wind grants later block");
    }

    private static async Task LingboCapOnlyHit()
    {
        var fixture = await ReadyFixture();
        await DuanYuState.GainLingbo(fixture.Player, 2);
        var initialHp = fixture.Player.Creature.CurrentHp;
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), new[] { fixture.Player.Creature }, 5, ValueProp.Move, fixture.Enemy, null, null, ModifyDamageHookType.Cap);
        Equal(2, initialHp - fixture.Player.Creature.CurrentHp, "cap-only copied attack receives target Lingbo");
        Equal(1, DuanYuState.Get(fixture.Player).Lingbo, "cap-only attack consumes one layer");
    }

    private static async Task LingboPreview()
    {
        var fixture = await ReadyFixture();
        await DuanYuState.GainLingbo(fixture.Player, 2);
        var preview = Hook.ModifyDamage(fixture.Run, fixture.Combat, fixture.Player.Creature, fixture.Enemy, 5, ValueProp.Move, null, null, ModifyDamageHookType.All, CardPreviewMode.Normal, out _);
        Equal(2m, preview, "Lingbo preview damage");
        Equal(2, DuanYuState.Get(fixture.Player).Lingbo, "preview does not consume layer");
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 5, ValueProp.Move, fixture.Enemy);
        Equal(1, DuanYuState.Get(fixture.Player).Lingbo, "real damage consumes layer");
    }

    private static async Task SwordDamageOrdering()
    {
        var fixture = await ReadyFixture();
        await Play(fixture, "DY_U14");
        await DuanYuState.AddSwordDamage(fixture.Player, 3);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 2, fixture.Player.Creature, null);
        var sword = AddCard(fixture, "DY_T01", PileType.Hand);
        CardCmd.Enchant<Instinct>(sword, 1);
        var initialHp = fixture.Enemy.CurrentHp;
        await PlayCard(fixture, sword);
        Equal(21, initialHp - fixture.Enemy.CurrentHp, "7 times 2 enchantment plus 2 Strength plus 3 temporary plus 2 proficiency");
    }

    private static async Task EtherealBeforeRetain()
    {
        var fixture = await ReadyFixture();
        var sword = AddCard(fixture, "DY_T01", PileType.Hand);
        CardCmd.ApplySingleTurnRetain(sword);
        var turnState = AccessTools.Field(typeof(CombatManager), "_turnState").GetValue(CombatManager.Instance)!;
        var turnEnd = (Task)AccessTools.Method(typeof(CombatManager), "DoTurnEnd").Invoke(CombatManager.Instance, [turnState, fixture.Player, new ThrowingPlayerChoiceContext()])!;
        await turnEnd;
        Equal(true, PileType.Exhaust.GetPile(fixture.Player).Cards.Contains(sword), "ethereal sword exhausted despite retain");
    }

    private static async Task AbilityLimit()
    {
        var fixture = await ReadyFixture();
        await Play(fixture, "DY_B02");
        await Play(fixture, "DY_B02");
        await Play(fixture, "DY_R02");
        await Play(fixture, "DY_B02");
        Equal(0, DuanYuState.Get(fixture.Player).Lingbo, "new ability does not reset skill ordinal");
    }

    private static async Task TurnStartLingbo()
    {
        var fixture = await ReadyFixture();
        await Play(fixture, "DY_R12");
        await DuanYuState.GainQi(fixture.Player, 9);
        await DuanYuState.GainLingbo(fixture.Player, 3);
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [fixture.Player.Creature]);
        Equal(0, DuanYuState.Get(fixture.Player).Lingbo, "old Lingbo cleared first");
        fixture.Player.Creature.LoseBlockInternal(fixture.Player.Creature.Block);
        await Hook.BeforeHandDraw(fixture.Combat, fixture.Player, new ThrowingPlayerChoiceContext());
        Equal(1, DuanYuState.Get(fixture.Player).Lingbo, "new Lingbo survives reset");
        Equal(4, fixture.Player.Creature.Block, "new block survives old block cleanup");
    }

    private static async Task SwordOverflow()
    {
        var fixture = await ReadyFixture();
        await DuanYuState.GainQi(fixture.Player, 9);
        for (var number = 0; number < 8; number++) AddCard(fixture, "DY_B01", PileType.Hand);
        await Play(fixture, "DY_R03");
        Equal(2, PileType.Hand.GetPile(fixture.Player).Cards.Count(DuanYuState.IsSword), "two swords fit in hand");
        Equal(4, PileType.Discard.GetPile(fixture.Player).Cards.Count(DuanYuState.IsSword), "four swords overflow");
        Equal(0, DuanYuState.Get(fixture.Player).SwordPlays, "generating is not playing");
    }

    private static Task HistoryRoundTrip()
    {
        var fixture = CreateFixture(combat: false);
        DuanYuState.RecordEvent(fixture.Player, "CQBZLY_DY_EVENT01");
        DuanYuState.RecordRelic(fixture.Player, "CQBZLY_DY_RELIC08");
        var restored = Player.FromSerializable(fixture.Player.ToSerializable());
        Equal(true, DuanYuState.HasVisitedEvent(restored, "CQBZLY_DY_EVENT01"), "event save restore");
        Equal(true, DuanYuState.HasAcquiredRelic(restored, "CQBZLY_DY_RELIC08"), "relic save restore");
        Equal(0, DuanYuState.Get(restored).Qi, "combat Qi not saved between rooms");
        return Task.CompletedTask;
    }

    public static void Equal<T>(T expected, T actual, string context)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{context}: expected {expected}, got {actual}.");
    }

    private sealed class FirstLegalSelector : ICardSelector
    {
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect) =>
            Task.FromResult<IEnumerable<CardModel>>(options.Take(Math.Min(maxSelect, Math.Max(1, minSelect))).ToArray());
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options, IReadOnlyList<CardRewardAlternative> alternatives) => new() { card = options.FirstOrDefault()?.Card };
    }
}
#endif
