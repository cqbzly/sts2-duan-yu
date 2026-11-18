#if DEBUG
using System.Diagnostics;
using DuanYu.Character;
using Godot;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace DuanYu.DebugTests;

internal static class AncientDialogueSelfTests
{
    internal static async Task Run()
    {
        TestMode.AssertOn();
        var character = ModelDb.Character<DuanYuCharacter>();
        var fresh = ((TheArchitect)ModelDb.Event<TheArchitect>().ToMutable()).DialogueSet;
        var cached = ModelDb.Event<TheArchitect>().DialogueSet;
        foreach (var set in new[] { fresh, cached })
        foreach (var visits in new[] { 0, 1, 100 })
        {
            var dialogues = set.GetValidDialogues(character.Id, visits, visits, allowAnyCharacterDialogues: false).ToArray();
            EngineSelfTests.Equal(true, dialogues.Length > 0, $"Architect dialogue available at visit {visits}");
            foreach (var dialogue in dialogues)
            {
                EngineSelfTests.Equal(3, dialogue.Lines.Count, "Architect Duan Yu dialogue line count");
                EngineSelfTests.Equal(true, dialogue.IsRepeating, "Architect dialogue supports later victories");
                EngineSelfTests.Equal(ArchitectAttackers.Architect, dialogue.EndAttackers, "Architect native ending attack");
                foreach (var line in dialogue.Lines)
                    EngineSelfTests.Equal(true, !string.IsNullOrWhiteSpace(line.LineText?.GetFormattedText()), "Architect dialogue localized line");
                foreach (var line in dialogue.Lines.Take(dialogue.Lines.Count - 1))
                    EngineSelfTests.Equal(true, !string.IsNullOrWhiteSpace(line.NextButtonText!.GetFormattedText()), "Architect dialogue localized next button");
            }
        }

        var manager = RunManager.Instance;
        try
        {
            var fixture = FullRunPath.Create("DUANYUARCHITECTUNITTEST");
            await manager.SetActInternal(2);
            var room = new EventRoom(ModelDb.Event<TheArchitect>());
            await manager.EnterRoom(room);
            var model = room.LocalMutableEvent;
            EngineSelfTests.Equal(false, manager.HasRunEnded, "Architect fixture begins before native victory");
            for (var page = 0; page < 6 && !manager.HasRunEnded; page++)
            {
                await AwaitOptions(model);
                var choice = model.CurrentOptions.Select((option, index) => (Option: option, Index: index))
                    .First(candidate => !candidate.Option.IsLocked && !candidate.Option.WasChosen);
                manager.EventSynchronizer.ChooseLocalOption(choice.Index);
                await manager.EventSynchronizer.AwaitPendingOptionTasks();
            }
            EngineSelfTests.Equal(0, model.CurrentOptions.Count, "Architect native final option completes");
            EngineSelfTests.Equal(true, manager.HasRunEnded, "Architect native WinRun handler completes");
            EngineSelfTests.Equal(false, manager.ShouldSave, "Architect unit fixture does not write run history");
            EngineSelfTests.Equal(0, fixture.Run.MapPointHistory.SelectMany(act => act).Count(), "Architect fixture is not represented as a full run");
        }
        finally
        {
            manager.CleanUp();
        }
    }

    private static async Task AwaitOptions(EventModel model)
    {
        var timer = Stopwatch.StartNew();
        var tree = (SceneTree)Engine.GetMainLoop();
        while (!model.IsFinished && model.CurrentOptions.Count == 0)
        {
            if (timer.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("Architect fixture did not expose its next native dialogue option.");
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
#endif
