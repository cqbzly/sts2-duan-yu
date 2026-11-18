#if DEBUG
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using DuanYu.Cards;
using DuanYu.Character;
using DuanYu.Relics;
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
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Encounters.Mocks;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
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
        if (started || !OS.GetCmdlineUserArgs().Any(argument => argument is "--duanyu-self-test" or "--duanyu-a10-sim" or "--duanyu-full-run")) return;
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
            if (OS.GetCmdlineUserArgs().Contains("--duanyu-full-run"))
            {
                await RunCase("full-run:a10", FullRunSimulation.Run);
                return;
            }
            using var selector = CardSelectorStack.SetTestSelector(new FirstLegalSelector());
            await RunCase("compendium:visibility-and-registration", CompendiumSelfTests.Run);
            if (OS.GetCmdlineUserArgs().Contains("--duanyu-a10-sim"))
            {
                await RunCase("ascension:a10-rules", AscensionSelfTests.Run);
                await RunCase("simulation:a10", A10CombatSimulation.Run);
                return;
            }
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
            await RunCase("runtime:pendant-first-skill-native-replay", PendantSkillReplay);
            await RunCase("runtime:pendant-third-sixth-native-replay", PendantSwordThresholds);
            await RunCase("runtime:pendant-ownership-and-late-acquisition", PendantLateAcquisition);
            await RunCase("runtime:pendant-no-rewards-after-death-or-victory", PendantInactiveCombat);
            await RunCase("runtime:pendant-player-isolation", PendantPlayerIsolation);
            await RunCase("runtime:momentum-consecutive-swords", MomentumConsecutiveSwords);
            await RunCase("runtime:momentum-additive-multihit-and-aoe", MomentumAdditiveDamage);
            await RunCase("runtime:momentum-preview-and-damage-boundaries", MomentumPreview);
            await RunCase("runtime:momentum-turn-and-combat-lifetime", MomentumLifetime);
            await RunCase("runtime:momentum-cap-and-death", MomentumCapAndDeath);
            await RunCase("runtime:momentum-native-replay-and-thresholds", MomentumReplayThresholds);
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
            await RunCase("ascension:a10-rules", AscensionSelfTests.Run);
            foreach (var test in BalanceSelfTests.Cases) await RunCase(test.Name, test.Action);
            foreach (var test in SpecialCardSelfTests.Cases) await RunCase(test.Name, test.Action);
            await RunCase("runtime:architect-native-victory", AncientDialogueSelfTests.Run);
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

    public static TestFixture CreateFixture(bool duanYu = true, bool combat = true, int ascensionLevel = 0, string seed = "DUANYUTEST")
    {
        TestMode.AssertOn();
        if (AccessTools.Property(typeof(RunManager), "State").GetValue(RunManager.Instance) != null) RunManager.Instance.CleanUp();
        var character = duanYu ? (CharacterModel)ModelDb.Character<DuanYuCharacter>() : ModelDb.Character<Ironclad>();
        var player = Player.CreateForNewRun(character, UnlockState.all, 1);
        var run = RunState.CreateForTest([player], ascensionLevel: ascensionLevel, seed: seed);
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

    internal static async Task<TestFixture> ReadyFixture()
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

    internal static async Task PlayCard(TestFixture fixture, DuanYuCard card)
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
        foreach (var upgraded in new[] { false, true })
        {
            var fixture = await ReadyFixture();
            fixture.Player.PlayerCombatState!.Energy = 3;
            Equal(3, DuanYuState.Get(fixture.Player).Qi, "pendant starts combat with three Qi");
            var initialHp = fixture.Enemy.CurrentHp;
            await PaidPlay(fixture, "DY_B03", upgraded);
            await PaidPlay(fixture, "DY_B04", upgraded);
            await PaidPlay(fixture, "DY_B05", upgraded);
            Equal(7, DuanYuState.Get(fixture.Player).Qi, "initial Qi after cap and paid double attack");
            Equal(1, DuanYuState.Get(fixture.Player).Lingbo, "initial Lingbo");
            Equal(upgraded ? 18 : 12, fixture.Player.Creature.Block, "initial block");
            Equal(upgraded ? 44 : 32, initialHp - fixture.Enemy.CurrentHp, "initial damage");
            Equal(1, fixture.Player.PlayerCombatState.Energy, "three paid cards receive one pendant energy");
        }
    }

    private static async Task CannotBorrowPendant()
    {
        var fixture = await ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 1;
        await SetQi(fixture, 1);
        await PaidPlay(fixture, "DY_C15");
        Equal(8, fixture.Player.Creature.Block, "insufficient Qi skips extra block");
        Equal(3, DuanYuState.Get(fixture.Player).Qi, "pendant arrives after skill");
        Equal(0, DuanYuState.Get(fixture.Player).SpendingCount, "pendant Qi cannot fund its triggering skill");
        Equal(1, fixture.Player.PlayerCombatState.Energy, "first skill refunds one paid energy");
        await PaidPlay(fixture, "DY_C15");
        Equal(24, fixture.Player.Creature.Block, "later skill may spend the earned pendant Qi");
        Equal(1, DuanYuState.Get(fixture.Player).Qi, "later skill receives no second pendant refund");
        Equal(0, fixture.Player.PlayerCombatState.Energy, "later skill receives no second energy refund");
    }

    private static Task<DuanYuCard> PaidPlay(TestFixture fixture, string id, bool upgraded = false) =>
        PaidPlay(fixture, AddCard(fixture, id, PileType.Hand, upgraded));

    private static async Task<DuanYuCard> PaidPlay(TestFixture fixture, DuanYuCard card)
    {
        Equal(true, card.CanPlay(), card.DesignId + " is payable before its effects");
        var energyValue = card.EnergyCost.GetWithModifiers(CostModifiers.All);
        var beforeEnergy = fixture.Player.PlayerCombatState!.Energy;
        var (energySpent, starsSpent) = await card.SpendResources();
        Equal(energyValue, energySpent, card.DesignId + " pays its native energy cost");
        Equal(beforeEnergy - energySpent, fixture.Player.PlayerCombatState.Energy, card.DesignId + " pays before any pendant refund");
        var target = card.TargetType switch
        {
            TargetType.AnyEnemy => fixture.Enemy,
            TargetType.AnyPlayer => fixture.Player.Creature,
            _ => null
        };
        await card.OnPlayWrapper(new ThrowingPlayerChoiceContext(), target, false,
            new ResourceInfo { EnergySpent = energySpent, EnergyValue = energyValue, StarsSpent = starsSpent, StarValue = 0 }, true);
        return card;
    }

    private static async Task SetQi(TestFixture fixture, int amount)
    {
        DuanYuState.Get(fixture.Player).Qi = 0;
        await PowerCmd.Remove<QiPower>(fixture.Player.Creature);
        await DuanYuState.GainQi(fixture.Player, amount);
    }

    private static async Task PendantSkillReplay()
    {
        var fixture = await ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 1;
        var card = AddCard(fixture, "DY_B02", PileType.Hand);
        card.BaseReplayCount = 2;
        await PaidPlay(fixture, card);
        var plays = CombatManager.Instance.History.CardPlaysFinished.Where(entry => ReferenceEquals(entry.CardPlay.Card, card)).Select(entry => entry.CardPlay).ToArray();
        Equal(3, plays.Length, "native skill replay executes three card plays");
        Equal(3, plays.Distinct().Count(), "native skill replay has distinct play records");
        Equal(18, fixture.Player.Creature.Block, "native skill replay executes all three bodies");
        Equal(5, DuanYuState.Get(fixture.Player).Qi, "replayed first skill earns pendant Qi only once");
        Equal(1, fixture.Player.PlayerCombatState.Energy, "replayed first skill earns pendant energy only once");
        foreach (var play in plays) await DuanYuState.AfterCard(play, new ThrowingPlayerChoiceContext());
        Equal(5, DuanYuState.Get(fixture.Player).Qi, "repeated completion callbacks do not duplicate pendant Qi");
        Equal(1, fixture.Player.PlayerCombatState.Energy, "repeated completion callbacks do not duplicate pendant energy");
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [fixture.Player.Creature]);
        await Hook.BeforeHandDraw(fixture.Combat, fixture.Player, new ThrowingPlayerChoiceContext());
        await PaidPlay(fixture, "DY_B02");
        Equal(7, DuanYuState.Get(fixture.Player).Qi, "new turn restores the first-skill pendant trigger");
        Equal(1, fixture.Player.PlayerCombatState.Energy, "new turn refunds one paid energy");
    }

    private static async Task PendantSwordThresholds()
    {
        var fixture = await ReadyFixture();
        fixture.Player.PlayerCombatState!.Energy = 0;
        await SetQi(fixture, 0);
        await PaidPlay(fixture, "DY_T01");
        await PaidPlay(fixture, "DY_T04");
        await PaidPlay(fixture, "DY_T01", true);
        Equal(2, DuanYuState.Get(fixture.Player).DistinctSwords.Count, "upgraded sword shares its ordinary identity");
        Equal(0, PileType.Hand.GetPile(fixture.Player).Cards.Count, "repeated sword kind earns no draw");
        await PaidPlay(fixture, "DY_T03");
        Equal(0, DuanYuState.Get(fixture.Player).SpendingCount, "third-kind reward cannot fund the triggering sword body");
        Equal(2, DuanYuState.Get(fixture.Player).Qi, "third sword kind grants two Qi after its body");
        Equal(1, PileType.Hand.GetPile(fixture.Player).Cards.Count, "third sword kind draws exactly one card");
        Equal(0, fixture.Player.PlayerCombatState.Energy, "third sword kind grants no pendant energy");
        var thirdReplay = AddCard(fixture, "DY_T03", PileType.Hand, true);
        thirdReplay.BaseReplayCount = 2;
        await PaidPlay(fixture, thirdReplay);
        Equal(3, CombatManager.Instance.History.CardPlaysFinished.Count(entry => ReferenceEquals(entry.CardPlay.Card, thirdReplay)), "native third-kind replay executes three bodies");
        Equal(0, DuanYuState.Get(fixture.Player).Qi, "later replay can spend previously earned Qi");
        Equal(1, DuanYuState.Get(fixture.Player).SpendingCount, "replayed third kind earns no repeated Qi reward");
        Equal(1, PileType.Hand.GetPile(fixture.Player).Cards.Count, "replayed third kind earns no repeated draw");
        await PaidPlay(fixture, "DY_T02");
        await PaidPlay(fixture, "DY_T05");
        await SetQi(fixture, 0);
        var beforeSpends = DuanYuState.Get(fixture.Player).SpendingCount;
        var sixthReplay = AddCard(fixture, "DY_T06", PileType.Hand, true);
        sixthReplay.BaseReplayCount = 2;
        await PaidPlay(fixture, sixthReplay);
        Equal(3, CombatManager.Instance.History.CardPlaysFinished.Count(entry => ReferenceEquals(entry.CardPlay.Card, sixthReplay)), "native sixth-kind replay executes three bodies");
        Equal(6, DuanYuState.Get(fixture.Player).DistinctSwords.Count, "native replays retain six distinct sword kinds");
        Equal(beforeSpends + 2, DuanYuState.Get(fixture.Player).SpendingCount, "sixth-kind reward funds both later replay bodies");
        Equal(1, DuanYuState.Get(fixture.Player).Qi, "sixth-kind replay receives one three-Qi reward");
        Equal(3, fixture.Player.PlayerCombatState.Energy, "sixth-kind replay earns one pendant energy plus two funded sword energy");
        Equal(2, PileType.Hand.GetPile(fixture.Player).Cards.Count, "sixth-kind replay adds no pendant draw");
        await PaidPlay(fixture, "DY_T06");
        Equal(0, DuanYuState.Get(fixture.Player).Qi, "repeated sixth kind spends the remaining Qi without another pendant reward");
        Equal(beforeSpends + 3, DuanYuState.Get(fixture.Player).SpendingCount, "repeated sixth kind pays its own Qi cost");
        Equal(4, fixture.Player.PlayerCombatState.Energy, "repeated sixth kind earns only its funded sword energy");
    }

    private static async Task PendantLateAcquisition()
    {
        var fixture = await ReadyFixture();
        await RelicCmd.Remove(fixture.Player.Relics.OfType<LanghuanPendant>().Single());
        fixture.Player.PlayerCombatState!.Energy = 3;
        await SetQi(fixture, 0);
        await DuanYuState.StartRelicCombat(fixture.Player, 1);
        Equal(0, DuanYuState.Get(fixture.Player).Qi, "missing pendant grants no combat-start Qi");
        await PaidPlay(fixture, "DY_B02");
        await PaidPlay(fixture, "DY_T01");
        await PaidPlay(fixture, "DY_T04");
        await PaidPlay(fixture, "DY_T03");
        Equal(0, DuanYuState.Get(fixture.Player).Qi, "missing pendant grants no skill or sword Qi");
        Equal(2, fixture.Player.PlayerCombatState.Energy, "missing pendant grants no skill energy");
        Equal(0, PileType.Hand.GetPile(fixture.Player).Cards.Count, "missing pendant grants no sword draw");
        await RelicCmd.Obtain<LanghuanPendant>(fixture.Player);
        await PaidPlay(fixture, "DY_B02");
        await PaidPlay(fixture, "DY_T03", true);
        Equal(0, DuanYuState.Get(fixture.Player).Qi, "late pendant does not restore passed skill or sword thresholds");
        Equal(1, fixture.Player.PlayerCombatState.Energy, "late pendant grants no retrospective skill energy");
        await PaidPlay(fixture, "DY_T02");
        await PaidPlay(fixture, "DY_T05");
        await SetQi(fixture, 0);
        await PaidPlay(fixture, "DY_T06");
        Equal(3, DuanYuState.Get(fixture.Player).Qi, "late pendant can reward a later new sixth kind");
        Equal(2, fixture.Player.PlayerCombatState.Energy, "late pendant rewards the later sixth kind once");

        var pendingFixture = await ReadyFixture();
        await RelicCmd.Remove(pendingFixture.Player.Relics.OfType<LanghuanPendant>().Single());
        await SetQi(pendingFixture, 0);
        pendingFixture.Player.PlayerCombatState!.Energy = 0;
        var pendingCard = AddCard(pendingFixture, "DY_B02", PileType.Hand);
        var pendingPlay = new CardPlay
        {
            Card = pendingCard, Player = pendingFixture.Player, Target = null, ResultPile = PileType.Discard,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 1, StarsSpent = 0, StarValue = 0 },
            IsAutoPlay = false, PlayIndex = 0, PlayCount = 1
        };
        DuanYuState.BeforeCard(pendingPlay);
        await RelicCmd.Obtain<LanghuanPendant>(pendingFixture.Player);
        await DuanYuState.AfterCard(pendingPlay, new ThrowingPlayerChoiceContext());
        Equal(0, DuanYuState.Get(pendingFixture.Player).Qi, "pendant acquired during a card does not reward that pending play");
        Equal(0, pendingFixture.Player.PlayerCombatState.Energy, "pending play uses the relic ownership snapshot");
    }

    private static async Task PendantInactiveCombat()
    {
        var deathFixture = await ReadyFixture();
        await SetQi(deathFixture, 0);
        deathFixture.Player.PlayerCombatState!.Energy = 0;
        await CreatureCmd.SetCurrentHp(deathFixture.Player.Creature, 3);
        await PaidPlay(deathFixture, "DY_U04");
        Equal(false, deathFixture.Player.Creature.IsAlive, "first skill may kill its owner before pendant rewards");
        Equal(0, DuanYuState.Get(deathFixture.Player).Qi, "dead owner receives no pendant Qi");
        Equal(0, deathFixture.Player.PlayerCombatState.Energy, "dead owner receives no pendant energy");

        foreach (var finalKind in new[] { 3, 6 })
        {
            var victoryFixture = await ReadyFixture();
            await SetQi(victoryFixture, 0);
            victoryFixture.Player.PlayerCombatState!.Energy = 0;
            await PaidPlay(victoryFixture, "DY_T01");
            await PaidPlay(victoryFixture, "DY_T04");
            if (finalKind == 6)
            {
                await PaidPlay(victoryFixture, "DY_T03");
                await PaidPlay(victoryFixture, "DY_T02");
                await PaidPlay(victoryFixture, "DY_T05");
            }
            await SetQi(victoryFixture, 0);
            PileType.Hand.GetPile(victoryFixture.Player).Clear();
            var momentumBeforeVictory = DuanYuState.Get(victoryFixture.Player).SwordMomentum;
            await CreatureCmd.SetCurrentHp(victoryFixture.Enemy, 1);
            await PaidPlay(victoryFixture, finalKind == 3 ? "DY_T03" : "DY_T06");
            Equal(false, victoryFixture.Enemy.IsAlive, "threshold sword kills the final enemy");
            Equal(0, DuanYuState.Get(victoryFixture.Player).Qi, "winning sword receives no post-combat pendant Qi");
            Equal(0, victoryFixture.Player.PlayerCombatState.Energy, "winning sword receives no post-combat energy");
            Equal(0, PileType.Hand.GetPile(victoryFixture.Player).Cards.Count, "winning sword receives no post-combat draw");
            Equal(momentumBeforeVictory, DuanYuState.Get(victoryFixture.Player).SwordMomentum, "winning sword gains no momentum after the last enemy dies");
            await DuanYuState.GainSwordMomentum(victoryFixture.Player, 3);
            Equal(momentumBeforeVictory, DuanYuState.Get(victoryFixture.Player).SwordMomentum, "momentum API rejects gains while combat is ending");
        }
    }

    private static async Task PendantPlayerIsolation()
    {
        var fixture = await ReadyFixture();
        var companion = Player.CreateForNewRun(ModelDb.Character<DuanYuCharacter>(), UnlockState.all, 2);
        fixture.Run.AddPlayerDebug(companion, -1);
        companion.ResetCombatState();
        fixture.Combat.AddPlayer(companion);
        await Hook.BeforeCombatStart(fixture.Run, fixture.Combat);
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [fixture.Player.Creature, companion.Creature]);
        fixture.Player.PlayerCombatState!.Energy = 1;
        companion.PlayerCombatState!.Energy = 1;
        var companionFixture = fixture with { Player = companion };
        await PaidPlay(fixture, "DY_B02");
        Equal(5, DuanYuState.Get(fixture.Player).Qi, "first player's skill rewards its owner");
        Equal(3, DuanYuState.Get(companion).Qi, "first player's skill leaves companion Qi unchanged");
        Equal(0, DuanYuState.Get(companion).SkillsPlayed, "first player's skill leaves companion ordinal unchanged");
        var localNetId = LocalContext.NetId;
        try
        {
            LocalContext.NetId = companion.NetId;
            await PaidPlay(companionFixture, "DY_B02");
        }
        finally
        {
            LocalContext.NetId = localNetId;
        }
        Equal(5, DuanYuState.Get(companion).Qi, "companion receives its independent first-skill reward");
        Equal(5, DuanYuState.Get(fixture.Player).Qi, "companion's skill leaves the first player unchanged");
        Equal(1, fixture.Player.PlayerCombatState.Energy, "first player's energy refund stays isolated");
        Equal(1, companion.PlayerCombatState.Energy, "companion's energy refund stays isolated");
        await SetQi(fixture, 0);
        await SetQi(companionFixture, 0);
        await DuanYuState.GainSwordMomentum(fixture.Player, 4);
        await DuanYuState.GainSwordMomentum(companion, 1);
        var enemyHp = fixture.Enemy.CurrentHp;
        await PaidPlay(fixture, "DY_T01");
        Equal(16, enemyHp - fixture.Enemy.CurrentHp, "first player's sword uses only its own momentum");
        Equal(5, DuanYuState.Get(fixture.Player).SwordMomentum, "first player's sword advances its own momentum");
        Equal(1, DuanYuState.Get(companion).SwordMomentum, "first player's sword leaves companion momentum unchanged");
        enemyHp = fixture.Enemy.CurrentHp;
        try
        {
            LocalContext.NetId = companion.NetId;
            await PaidPlay(companionFixture, "DY_T01");
        }
        finally
        {
            LocalContext.NetId = localNetId;
        }
        Equal(13, enemyHp - fixture.Enemy.CurrentHp, "companion's sword uses only its own momentum");
        Equal(2, DuanYuState.Get(companion).SwordMomentum, "companion's momentum advances independently");
        Equal(5, DuanYuState.Get(fixture.Player).SwordMomentum, "companion's sword leaves first player momentum unchanged");
    }

    private static async Task<TestFixture> MomentumFixture()
    {
        var fixture = await ReadyFixture();
        await RelicCmd.Remove(fixture.Player.Relics.OfType<LanghuanPendant>().Single());
        await SetQi(fixture, 0);
        Equal(0, DuanYuState.Get(fixture.Player).SwordMomentum, "new combat starts without momentum");
        Equal(0, fixture.Player.Creature.GetPowerAmount<SwordMomentumPower>(), "new combat has no momentum power");
        return fixture;
    }

    private static async Task MomentumConsecutiveSwords()
    {
        var fixture = await MomentumFixture();
        await SetQi(fixture, 2);
        var health = fixture.Enemy.CurrentHp;
        await PaidPlay(fixture, "DY_T01");
        Equal(18, health - fixture.Enemy.CurrentHp, "first sword completes both segments before gaining momentum");
        Equal(1, DuanYuState.Get(fixture.Player).SwordMomentum, "sword gains momentum without the pendant");
        Equal(1, fixture.Player.Creature.GetPowerAmount<SwordMomentumPower>(), "momentum power mirrors the ledger");
        health = fixture.Enemy.CurrentHp;
        await PaidPlay(fixture, "DY_T01");
        Equal(20, health - fixture.Enemy.CurrentHp, "next sword adds existing momentum to both segments");
        Equal(2, DuanYuState.Get(fixture.Player).SwordMomentum, "same sword kind still gains one base momentum");
        health = fixture.Enemy.CurrentHp;
        await PaidPlay(fixture, "DY_B01");
        Equal(10, health - fixture.Enemy.CurrentHp, "ordinary attack receives no sword momentum damage");
        Equal(2, DuanYuState.Get(fixture.Player).SwordMomentum, "ordinary attack gains no sword momentum");
    }

    private static async Task MomentumAdditiveDamage()
    {
        var fixture = await MomentumFixture();
        var context = new ThrowingPlayerChoiceContext();
        await DuanYuState.GainSwordMomentum(fixture.Player, 3);
        await DuanYuState.AddSwordDamage(fixture.Player, 2);
        await DuanYuState.AddEffect(fixture.Player, "DY_U14", 4);
        await PowerCmd.Apply<StrengthPower>(context, fixture.Player.Creature, 1, fixture.Player.Creature, null);
        await SetQi(fixture, 1);
        var health = fixture.Enemy.CurrentHp;
        await PaidPlay(fixture, "DY_T01");
        Equal(38, health - fixture.Enemy.CurrentHp, "both sword segments add momentum, temporary damage, proficiency and Strength");
        Equal(4, DuanYuState.Get(fixture.Player).SwordMomentum, "two-hit sword gains momentum once after both hits");
        var secondEnemy = fixture.Combat.CreateCreature(ModelDb.Monster<BigDummy>().ToMutable(), CombatSide.Enemy, null);
        fixture.Combat.AddCreature(secondEnemy);
        var secondHealth = secondEnemy.CurrentHp;
        health = fixture.Enemy.CurrentHp;
        await SetQi(fixture, 2);
        await PaidPlay(fixture, "DY_T03");
        Equal(36, health - fixture.Enemy.CurrentHp, "first enemy takes the additive bonus on both AOE segments");
        Equal(36, secondHealth - secondEnemy.CurrentHp, "second enemy takes the same bonus without mid-card momentum growth");
        Equal(5, DuanYuState.Get(fixture.Player).SwordMomentum, "multi-enemy multi-hit sword grants only one base momentum");
    }

    private static async Task MomentumPreview()
    {
        var fixture = await MomentumFixture();
        await DuanYuState.GainSwordMomentum(fixture.Player, 4);
        var sword = AddCard(fixture, "DY_T01", PileType.Hand);
        var ordinary = AddCard(fixture, "DY_B01", PileType.Hand);
        var momentumPower = fixture.Player.Creature.GetPower<SwordMomentumPower>()!;
        for (var preview = 0; preview < 3; preview++)
        {
            Equal(16m, Hook.ModifyDamage(fixture.Run, fixture.Combat, fixture.Enemy, fixture.Player.Creature, 12,
                ValueProp.Move, sword, null, ModifyDamageHookType.All, CardPreviewMode.Normal, out _), "sword preview includes current momentum");
            Equal(7m, Hook.ModifyDamage(fixture.Run, fixture.Combat, fixture.Enemy, fixture.Player.Creature, 7,
                ValueProp.Move, ordinary, null, ModifyDamageHookType.All, CardPreviewMode.Normal, out _), "ordinary attack preview excludes momentum");
            Equal(12m, Hook.ModifyDamage(fixture.Run, fixture.Combat, fixture.Enemy, fixture.Player.Creature, 12,
                ValueProp.Move | ValueProp.Unpowered, sword, null, ModifyDamageHookType.All, CardPreviewMode.Normal, out _), "unpowered damage excludes momentum");
            Equal(12m, Hook.ModifyDamage(fixture.Run, fixture.Combat, fixture.Enemy, fixture.Player.Creature, 12,
                ValueProp.Move, sword, null, ModifyDamageHookType.Cap, CardPreviewMode.Normal, out _), "cap-only copied damage does not add momentum twice");
        }
        Equal(4, DuanYuState.Get(fixture.Player).SwordMomentum, "damage previews never change momentum");
        Equal(4, momentumPower.Amount, "damage previews never consume the momentum power");
        Equal(0, DuanYuState.Get(fixture.Player).SwordPlays, "damage previews never register sword plays");
        var health = fixture.Enemy.CurrentHp;
        await PaidPlay(fixture, sword);
        Equal(16, health - fixture.Enemy.CurrentHp, "real sword uses the previewed momentum");
        Equal(5, DuanYuState.Get(fixture.Player).SwordMomentum, "only completed sword grows momentum");
    }

    private static async Task MomentumLifetime()
    {
        var fixture = await MomentumFixture();
        await DuanYuState.GainSwordMomentum(fixture.Player, 5);
        await DuanYuState.EndTurn(fixture.Player);
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [fixture.Player.Creature]);
        await Hook.BeforeHandDraw(fixture.Combat, fixture.Player, new ThrowingPlayerChoiceContext());
        Equal(5, DuanYuState.Get(fixture.Player).SwordMomentum, "momentum persists through turn end and start");
        Equal(5, fixture.Player.Creature.GetPowerAmount<SwordMomentumPower>(), "momentum power persists with its ledger");
        await Hook.AfterCombatEnd(fixture.Run, fixture.Combat, new CombatRoom(fixture.Combat));
        Equal(0, DuanYuState.Get(fixture.Player).SwordMomentum, "combat end clears momentum state");
        Equal(0, fixture.Player.Creature.GetPowerAmount<SwordMomentumPower>(), "combat end removes momentum power");
        await DuanYuState.GainSwordMomentum(fixture.Player, 6);
        await Hook.BeforeCombatStart(fixture.Run, fixture.Combat);
        Equal(0, DuanYuState.Get(fixture.Player).SwordMomentum, "new combat clears old momentum state");
        Equal(0, fixture.Player.Creature.GetPowerAmount<SwordMomentumPower>(), "new combat removes a stale momentum power");
    }

    private static async Task MomentumCapAndDeath()
    {
        var fixture = await MomentumFixture();
        await DuanYuState.GainSwordMomentum(fixture.Player, int.MaxValue);
        await DuanYuState.GainSwordMomentum(fixture.Player, int.MaxValue);
        await DuanYuState.GainSwordMomentum(fixture.Player, -1);
        await DuanYuState.GainSwordMomentum(fixture.Player, 0);
        Equal(12, DuanYuState.Get(fixture.Player).SwordMomentum, "momentum cap resists overflow and nonpositive changes");
        Equal(12, fixture.Player.Creature.GetPowerAmount<SwordMomentumPower>(), "momentum power respects the cap");
        await PaidPlay(fixture, "DY_T01");
        Equal(12, DuanYuState.Get(fixture.Player).SwordMomentum, "completed swords cannot exceed the momentum cap");

        var deathFixture = await MomentumFixture();
        await DuanYuState.GainSwordMomentum(deathFixture.Player, 5);
        await PowerCmd.Apply<ThornsPower>(new ThrowingPlayerChoiceContext(), deathFixture.Enemy, 1, deathFixture.Enemy, null);
        await CreatureCmd.SetCurrentHp(deathFixture.Player.Creature, 1);
        await PaidPlay(deathFixture, "DY_T01");
        Equal(false, deathFixture.Player.Creature.IsAlive, "retaliation kills the sword's owner during its body");
        Equal(5, DuanYuState.Get(deathFixture.Player).SwordMomentum, "lethal sword resolution gains no momentum");
        await DuanYuState.GainSwordMomentum(deathFixture.Player, 4);
        Equal(5, DuanYuState.Get(deathFixture.Player).SwordMomentum, "momentum API grants nothing to a dead player");
    }

    private static async Task MomentumReplayThresholds()
    {
        var fixture = await MomentumFixture();
        await PaidPlay(fixture, "DY_T01");
        await PaidPlay(fixture, "DY_T01", true);
        await PaidPlay(fixture, "DY_T04");
        Equal(3, DuanYuState.Get(fixture.Player).SwordMomentum, "upgraded duplicate gains base momentum without a new-kind bonus");
        var thirdKind = AddCard(fixture, "DY_T03", PileType.Hand);
        thirdKind.BaseReplayCount = 2;
        var health = fixture.Enemy.CurrentHp;
        await PaidPlay(fixture, thirdKind);
        Equal(38, health - fixture.Enemy.CurrentHp, "native replay uses the momentum earned by each preceding resolution");
        Equal(7, DuanYuState.Get(fixture.Player).SwordMomentum, "third-kind replay grants three base stacks and one threshold stack");
        var plays = CombatManager.Instance.History.CardPlaysFinished.Where(entry => ReferenceEquals(entry.CardPlay.Card, thirdKind)).Select(entry => entry.CardPlay).ToArray();
        Equal(3, plays.Length, "third-kind test executes three native replay bodies");
        foreach (var play in plays) await DuanYuState.AfterCard(play, new ThrowingPlayerChoiceContext());
        Equal(7, DuanYuState.Get(fixture.Player).SwordMomentum, "duplicate callbacks cannot grant momentum twice");

        var sixthFixture = await MomentumFixture();
        foreach (var id in new[] { "DY_T01", "DY_T04", "DY_T03", "DY_T02", "DY_T05" }) await PaidPlay(sixthFixture, id);
        Equal(6, DuanYuState.Get(sixthFixture.Player).SwordMomentum, "first five distinct swords include only the third-kind bonus");
        var sixthKind = AddCard(sixthFixture, "DY_T06", PileType.Hand);
        sixthKind.BaseReplayCount = 2;
        await PaidPlay(sixthFixture, sixthKind);
        Equal(3, CombatManager.Instance.History.CardPlaysFinished.Count(entry => ReferenceEquals(entry.CardPlay.Card, sixthKind)), "sixth-kind test executes three native replay bodies");
        Equal(11, DuanYuState.Get(sixthFixture.Player).SwordMomentum, "sixth-kind replay grants three base stacks and exactly two threshold stacks");
        Equal(11, sixthFixture.Player.Creature.GetPowerAmount<SwordMomentumPower>(), "native replay keeps the momentum power synchronized");
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
        var initialHp = fixture.Player.Creature.CurrentHp;
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 0, ValueProp.Move, fixture.Enemy);
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 5, ValueProp.Unpowered | ValueProp.Unblockable, fixture.Enemy);
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 2, ValueProp.Move | ValueProp.Unpowered | ValueProp.Unblockable, fixture.Enemy);
        Equal(7, initialHp - fixture.Player.Creature.CurrentHp, "direct HP loss is not reduced");
        Equal(1, DuanYuState.Get(fixture.Player).Lingbo, "nonattacks preserve Lingbo");
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 1, fixture.Enemy, null);
        var wither = fixture.Combat.CreateCard(ModelDb.Card<Wither>(), fixture.Player);
        await wither.OnTurnEndInHandWrapper(new ThrowingPlayerChoiceContext());
        Equal(10, initialHp - fixture.Player.Creature.CurrentHp, "native Wither damage ignores Weak and Lingbo");
        Equal(1, DuanYuState.Get(fixture.Player).Lingbo, "native Wither preserves Lingbo");
        fixture.Player.Creature.GainBlockInternal(20);
        var burn = fixture.Combat.CreateCard(ModelDb.Card<Burn>(), fixture.Player);
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, burn.DynamicVars.Damage, burn, null);
        Equal(18, fixture.Player.Creature.Block, "native Burn damage is blocked without Lingbo reduction");
        var toxic = fixture.Combat.CreateCard(ModelDb.Card<Toxic>(), fixture.Player);
        await toxic.OnTurnEndInHandWrapper(new ThrowingPlayerChoiceContext());
        Equal(13, fixture.Player.Creature.Block, "native Toxic damage is blocked without Lingbo reduction");
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 4, ValueProp.Unpowered, fixture.Enemy);
        Equal(9, fixture.Player.Creature.Block, "unpowered retaliation is blocked without Lingbo reduction");
        Equal(10, initialHp - fixture.Player.Creature.CurrentHp, "blocked nonattacks preserve HP");
        Equal(1, DuanYuState.Get(fixture.Player).Lingbo, "blocked nonattacks preserve Lingbo");
        Equal(0, DuanYuState.Get(fixture.Player).LingboTriggers, "nonattacks never trigger Lingbo");
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 5, ValueProp.Move, fixture.Enemy);
        Equal(7, fixture.Player.Creature.Block, "following attack receives Lingbo reduction before block");
        Equal(0, DuanYuState.Get(fixture.Player).Lingbo, "full block consumes layer");
        Equal(1, DuanYuState.Get(fixture.Player).LingboTriggers, "following attack consumes exactly one Lingbo");
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
        await DuanYuState.SpendQi(fixture.Player, DuanYuState.Get(fixture.Player).Qi);
        var initialHp = fixture.Enemy.CurrentHp;
        await PlayCard(fixture, sword);
        Equal(33, initialHp - fixture.Enemy.CurrentHp, "12 times 2 enchantment plus 2 Strength plus 3 temporary plus 4 proficiency");
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
        Equal(8, fixture.Player.Creature.Block, "new block survives old block cleanup");
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
