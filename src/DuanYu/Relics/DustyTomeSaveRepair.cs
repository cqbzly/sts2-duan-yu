using DuanYu.Cards;
using DuanYu.Character;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace DuanYu.Relics;

[HarmonyPatch(typeof(RunState), nameof(RunState.FromSerializable))]
internal static class DustyTomeSaveRepair
{
    private static void Prefix(SerializableRun save)
    {
        ModelId duanYu = ModelDb.GetId(typeof(DuanYuCharacter));
        ModelId dustyTome = ModelDb.GetId(typeof(DustyTome));
        foreach (var player in save.Players.Where(player => player.CharacterId == duanYu))
        {
            var relic = player.Relics.FirstOrDefault(relic => relic.Id == dustyTome);
            if (relic == null || relic.Props?.modelIds?.Any(property => property.name == nameof(DustyTome.AncientCard)) == true)
                continue;

            ModelId ancient = ModelDb.GetId(typeof(DY_A01));
            relic.Props ??= new SavedProperties();
            relic.Props.modelIds ??= [];
            relic.Props.modelIds.Add(new(nameof(DustyTome.AncientCard), ancient));

            int? floor = relic.FloorAddedToDeck;
            if (player.Deck.Any(card => card.Id == ancient && card.FloorAddedToDeck == floor))
                continue;
            player.Deck.Add(new SerializableCard { Id = ancient, CurrentUpgradeLevel = 1, FloorAddedToDeck = floor });

            if (floor is int number && number > 0)
            {
                var point = save.MapPointHistory.SelectMany(act => act).ElementAtOrDefault(number - 1);
                var history = point?.PlayerStats.FirstOrDefault(entry => entry.PlayerId == player.NetId);
                if (history != null && !history.CardsGained.Any(card => card.Id == ancient))
                    history.CardsGained.Add(new SerializableCard { Id = ancient, CurrentUpgradeLevel = 1 });
            }
        }
    }
}
