using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace DuanYu.Relics;

// Older unclaimed Orobas offers stored the vanilla fallback instead of the pendant upgrade.
[HarmonyPatch(typeof(SavedProperties), nameof(SavedProperties.FillInternal))]
internal static class LanghuanUpgradeSavePatch
{
    private static bool Prefix(SavedProperties __instance, object model)
    {
        if (model is not TouchOfOrobas || __instance.modelIds is not { } ids ||
            !ids.Any(property => property.name == nameof(TouchOfOrobas.StarterRelic) &&
                property.value == ModelDb.Relic<LanghuanPendant>().Id) ||
            !ids.Any(property => property.name == nameof(TouchOfOrobas.UpgradedRelic) &&
                property.value == ModelDb.Relic<Circlet>().Id)) return true;

        // Copy before filling so the original save data and other models remain untouched.
        var corrected = new SavedProperties
        {
            ints = __instance.ints,
            bools = __instance.bools,
            strings = __instance.strings,
            intArrays = __instance.intArrays,
            cards = __instance.cards,
            cardArrays = __instance.cardArrays,
            modelIds = ids.Select(property => property.name == nameof(TouchOfOrobas.UpgradedRelic)
                ? new SavedProperties.SavedProperty<ModelId>(property.name, ModelDb.Relic<LanghuanCirclet>().Id)
                : property).ToList()
        };
        corrected.FillInternal(model);
        return false;
    }
}
