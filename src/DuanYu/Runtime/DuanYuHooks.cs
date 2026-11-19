using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace DuanYu.Runtime;

internal static class DuanYuHookTasks
{
    internal static async Task After(Task original, Func<Task> action)
    {
        await original;
        await action();
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
internal static class DuanYuCombatStartHook
{
    private static void Prefix(ICombatState? combatState) => DuanYuState.BeginCombat(combatState);
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
internal static class DuanYuCombatEndHook
{
    private static void Postfix(ICombatState? combatState, ref Task __result) => __result = DuanYuHookTasks.After(__result, () =>
    {
        DuanYuState.EndCombat(combatState);
        return Task.CompletedTask;
    });
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeHandDraw))]
internal static class DuanYuTurnStartHook
{
    private static void Postfix(Player player, PlayerChoiceContext playerChoiceContext, ref Task __result) => __result = DuanYuHookTasks.After(__result, () => DuanYuState.BeginTurn(player, playerChoiceContext));
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeSideTurnStart))]
internal static class DuanYuPrepareTurnHook
{
    private static void Prefix(CombatSide side, IReadOnlyList<Creature> participants)
    {
        if (side != CombatSide.Player) return;
        foreach (var creature in participants)
            if (creature.Player is { } player) DuanYuState.PrepareTurn(player);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeSideTurnEnd))]
internal static class DuanYuTurnEndHook
{
    private static void Postfix(CombatSide side, IEnumerable<Creature> participants, ref Task __result)
    {
        if (side != CombatSide.Player) return;
        var players = participants.Select(creature => creature.Player).OfType<Player>().ToArray();
        __result = DuanYuHookTasks.After(__result, async () =>
        {
            foreach (var player in players) await DuanYuState.EndTurn(player);
        });
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed))]
internal static class DuanYuBeforeCardHook
{
    private static void Prefix(CardPlay cardPlay) => DuanYuState.BeforeCard(cardPlay);
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
internal static class DuanYuAfterCardHook
{
    private static void Postfix(CardPlay cardPlay, PlayerChoiceContext choiceContext, ref Task __result) => __result = DuanYuHookTasks.After(__result, () => DuanYuState.AfterCard(cardPlay, choiceContext));
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
internal static class DuanYuDamageHook
{
    private static void Postfix(Creature? target, ValueProp props, ref IEnumerable<AbstractModel> modifiers, ref decimal __result)
    {
        if (target?.Player == null || target.GetPower<LingboPower>() is not { } lingbo) return;
        var reduced = DuanYuCombatLedger.ReduceDamage(__result, props.HasFlag(ValueProp.Unblockable), lingbo.Amount);
        if (reduced == __result) return;
        __result = reduced;
        modifiers = modifiers.Append(lingbo).Distinct().ToArray();
    }
}
