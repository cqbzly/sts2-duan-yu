using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Events;

namespace DuanYu.Character;

[HarmonyPatch(typeof(TheArchitect), "DefineDialogues")]
internal static class DuanYuArchitectDialogue
{
    private static void Postfix(AncientDialogueSet __result)
    {
        DuanYuAncientLocalization.Merge();
        __result.CharacterDialogues[MainFile.ModId] =
        [
            new AncientDialogue("", "", "")
            {
                VisitIndex = 0,
                IsRepeating = true,
                StartAttackers = ArchitectAttackers.None,
                EndAttackers = ArchitectAttackers.Architect
            }
        ];
    }
}

[HarmonyPatch(typeof(LocManager), "SetLanguageInternal")]
internal static class DuanYuAncientLocalization
{
    private static void Prefix(string language, Dictionary<string, LocTable> tables) =>
        tables["ancients"].MergeWith(Translations(language));

    internal static void Merge() => LocManager.Instance.GetTable("ancients")
        .MergeWith(Translations(LocManager.Instance.Language));

    private static Dictionary<string, string> Translations(string language)
    {
        const string prefix = "THE_ARCHITECT.talk.CQBZLY_DUAN_YU.0-";
        var english = language == "eng";
        return new Dictionary<string, string>
        {
            [prefix + "0r.ancient"] = english
                ? "You carry six swords without a blade, yet hesitate to strike. What brought you to the top?"
                : "你身无寸铁，却怀六道剑气；明明不愿伤人，为何仍走到了这里？",
            [prefix + "0r.next"] = english ? "Answer honestly" : "以诚相答",
            [prefix + "1r.char"] = english
                ? "I never wished to prove my strength. If these arts can open a way for others to return home, that is enough."
                : "在下从未想过争什么天下第一。若这身武功能为旁人开一条归路，便已足够。",
            [prefix + "1r.next"] = english ? "Stand your ground" : "静候其言",
            [prefix + "2r.ancient"] = english
                ? "Then keep that wish, prince of Dali. Let us see what remains when the last step is taken."
                : "那便记住这个心愿，大理的世子。且看迈出最后一步之后，你还留下些什么。"
        };
    }
}
