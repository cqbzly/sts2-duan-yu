#if DEBUG
using System.Text.Json;
using DuanYu.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace DuanYu.DebugTests;

internal static class DustyTomeSaveSelfTests
{
    internal static Task Run()
    {
        EngineSelfTests.CreateFixture(combat: false);
        RunManager.Instance.GenerateRooms();
        var save = RunManager.Instance.ToSave(null);
        var player = save.Players.Single();
        var tomeId = ModelDb.Relic<DustyTome>().Id;
        var ancientId = ModelDb.Card<DY_A01>().Id;
        int deckSize = player.Deck.Count;
        player.Relics.Add(new SerializableRelic { Id = tomeId, FloorAddedToDeck = 34 });

        var restored = RunState.FromSerializable(save).Players.Single();
        EngineSelfTests.Equal(ancientId, restored.GetRelic<DustyTome>()!.AncientCard, "the legacy tome displays its restored card");
        var gained = restored.Deck.Cards.Single(card => card.Id == ancientId);
        EngineSelfTests.Equal(true, gained.IsUpgraded, "the missing reward is restored upgraded");
        EngineSelfTests.Equal<int?>(34, gained.FloorAddedToDeck, "the card uses the reward's original acquisition floor");
        EngineSelfTests.Equal(deckSize + 1, restored.Deck.Cards.Count, "only the missing card is added");

        save.Players[0] = restored.ToSerializable();
        var reloaded = RunState.FromSerializable(RoundTrip(save)).Players.Single();
        EngineSelfTests.Equal(deckSize + 1, reloaded.Deck.Cards.Count, "a subsequent save and load adds no duplicate");
        EngineSelfTests.Equal(ancientId, reloaded.GetRelic<DustyTome>()!.AncientCard, "the Ancient card survives native serialization");

        save.Players[0].Deck.RemoveAll(card => card.Id == ancientId);
        var removed = RunState.FromSerializable(save).Players.Single();
        EngineSelfTests.Equal(deckSize, removed.Deck.Cards.Count, "a card removed after acquisition stays removed");

        EngineSelfTests.CreateFixture(duanYu: false, combat: false);
        RunManager.Instance.GenerateRooms();
        var otherSave = RunManager.Instance.ToSave(null);
        otherSave.Players[0].Relics.Add(new SerializableRelic { Id = tomeId, FloorAddedToDeck = 34 });
        var other = RunState.FromSerializable(otherSave).Players.Single();
        EngineSelfTests.Equal<ModelId?>(null, other.GetRelic<DustyTome>()!.AncientCard, "the repair only applies to Duan Yu");

        string? copiedSavePath = Environment.GetEnvironmentVariable("DUANYU_DUSTY_TOME_TEST_SAVE");
        if (!string.IsNullOrWhiteSpace(copiedSavePath))
        {
            var copiedSave = JsonSerializer.Deserialize(File.ReadAllText(copiedSavePath), JsonSerializationUtility.GetTypeInfo<SerializableRun>())!;
            var copiedPlayer = copiedSave.Players.Single();
            int originalCount = copiedPlayer.Deck.Count;
            var recovered = RunState.FromSerializable(copiedSave).Players.Single();
            var recoveredTome = recovered.GetRelic<DustyTome>()!;
            var recoveredCard = recovered.Deck.Cards.Single(card => card.Id == ancientId);
            EngineSelfTests.Equal(ancientId, recoveredTome.AncientCard, "the copied legacy save displays the correct reward");
            EngineSelfTests.Equal(true, recoveredCard.IsUpgraded, "the copied legacy save receives the upgraded reward");
            EngineSelfTests.Equal(0, recoveredCard.EnergyCost.GetWithModifiers(CostModifiers.All), "the recovered card has the upgraded energy cost");
            EngineSelfTests.Equal(recoveredTome.FloorAddedToDeck, recoveredCard.FloorAddedToDeck, "the copied save preserves acquisition floor");
            EngineSelfTests.Equal(originalCount + 1, recovered.Deck.Cards.Count, "the copied save keeps its original deck and adds one reward");
        }
        return Task.CompletedTask;
    }

    private static SerializableRun RoundTrip(SerializableRun save) =>
        JsonSerializer.Deserialize(JsonSerializationUtility.ToJson(save), JsonSerializationUtility.GetTypeInfo<SerializableRun>())!;
}
#endif
