#if DEBUG
using BaseLib.Abstracts;
using DuanYu.Character;
using DuanYu.Relics;
using DuanYu.Runtime;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;

namespace DuanYu.DebugTests;

internal static class QiRetentionSelfTests
{
    internal static IEnumerable<(string Name, Func<Task> Action)> Cases
    {
        get
        {
            yield return ("qi-retention:boundaries-and-independent-resource", ResourceBoundaries);
            yield return ("qi-retention:repeated-turn-end-and-extra-turn", RepeatedTurnEnds);
            yield return ("qi-retention:preserves-first-payment-rewards", PaymentRewards);
            yield return ("qi-retention:side-participants-and-player-isolation", PlayerIsolation);
            yield return ("qi-retention:explicit-status-loss-and-ethereal-refund", TurnEndOrdering);
            yield return ("qi-retention:inactive-combat-preserves-resource", InactiveCombat);
        }
    }

    private static async Task ResourceBoundaries()
    {
        foreach (int startingQi in new[] { 0, 1, 5, 9, 12, 50, 100 })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            var player = fixture.Player;
            await SetQi(player, startingQi);
            player.PlayerCombatState!.Stars = 5;
            await Hook.BeforeSideTurnEnd(fixture.Combat, CombatSide.Player, [player.Creature]);
            await Hook.AfterSideTurnEnd(fixture.Combat, CombatSide.Player, [player.Creature]);
            AssertQi(player, startingQi, "turn-end retains " + startingQi + " Qi");
            EngineSelfTests.Equal(5, player.PlayerCombatState.Stars, "Qi retention preserves independent native stars");
        }
    }

    private static async Task RepeatedTurnEnds()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        var player = fixture.Player;
        await SetQi(player, 5);
        int turn = DuanYuState.Get(player).TurnNumber;
        await Hook.AfterSideTurnEnd(fixture.Combat, CombatSide.Player, [player.Creature]);
        await Hook.AfterSideTurnEnd(fixture.Combat, CombatSide.Player, [player.Creature]);
        AssertQi(player, 5, "duplicate turn-end hooks do not change Qi");
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [player.Creature]);
        await Hook.BeforeHandDraw(fixture.Combat, player, new ThrowingPlayerChoiceContext());
        EngineSelfTests.Equal(turn + 1, DuanYuState.Get(player).TurnNumber, "an extra player turn advances the independent turn counter");
        await Hook.AfterSideTurnEnd(fixture.Combat, CombatSide.Player, [player.Creature]);
        AssertQi(player, 5, "an extra player turn preserves Qi");
        await Hook.AfterSideTurnEnd(fixture.Combat, CombatSide.Player, [player.Creature]);
        AssertQi(player, 5, "duplicate extra-turn hooks also preserve Qi");
    }

    private static async Task PaymentRewards()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        var player = fixture.Player;
        await RelicCmd.Obtain<TianlongPrayerBeads>(player);
        await RelicCmd.Obtain<DaliSeal>(player);
        await DuanYuState.AddEffect(player, "DY_U01");
        await DuanYuState.AddEffect(player, "DY_R01", 1, energyAmount: 1);
        await SetQi(player, 1);
        player.PlayerCombatState!.Energy = 0;
        int draw = PileType.Draw.GetPile(player).Cards.Count;
        await Hook.AfterSideTurnEnd(fixture.Combat, CombatSide.Player, [player.Creature]);
        var state = DuanYuState.Get(player);
        AssertQi(player, 1, "ordinary turn-end receives no Beiming refund or Qi loss");
        EngineSelfTests.Equal(0, state.SpendingCount, "ordinary turn-end records no successful payment");
        EngineSelfTests.Equal(false, state.SealUsed, "ordinary turn-end preserves the first seal trigger");
        EngineSelfTests.Equal(false, state.PrayerBeadsUsedThisTurn, "ordinary turn-end preserves the first prayer trigger");
        EngineSelfTests.Equal(0, player.PlayerCombatState.Energy, "ordinary turn-end grants no refund energy");
        EngineSelfTests.Equal(0, player.Creature.Block, "ordinary turn-end grants no prayer block");
        EngineSelfTests.Equal(0, PileType.Hand.GetPile(player).Cards.Count, "ordinary turn-end grants no first-payment draw");
        EngineSelfTests.Equal(draw, PileType.Draw.GetPile(player).Cards.Count, "ordinary turn-end preserves the draw pile");
        await SetQi(player, 3);
        player.PlayerCombatState.Energy = 1;
        var card = EngineSelfTests.AddCard(fixture, "DY_U07", PileType.Hand);
        card.BaseReplayCount = 2;
        await EngineSelfTests.PaidPlay(fixture, card);
        AssertQi(player, 0, "the first genuine payment receives no second-payment Beiming refund");
        EngineSelfTests.Equal(1, state.SpendingCount, "native replay records one genuine payment after ordinary turn-end");
        EngineSelfTests.Equal(true, state.SealUsed, "genuine payment consumes the first seal trigger");
        EngineSelfTests.Equal(true, state.PrayerBeadsUsedThisTurn, "genuine payment consumes the first prayer trigger");
        EngineSelfTests.Equal(1, player.PlayerCombatState.Energy, "first genuine payment receives seal energy without a Beiming reward");
        EngineSelfTests.Equal(4, player.Creature.Block, "genuine payment receives prayer block once");
        EngineSelfTests.Equal(1, PileType.Hand.GetPile(player).Cards.Count, "genuine payment receives its first-payment draw once");
        EngineSelfTests.Equal(draw - 1, PileType.Draw.GetPile(player).Cards.Count, "native replay draws only one first-payment card");
        EngineSelfTests.Equal(3, CombatManager.Instance.History.CardPlaysFinished.Count(entry => ReferenceEquals(entry.CardPlay.Card, card)), "payment reward test resolves all three native replay bodies");
    }

    private static async Task PlayerIsolation()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        var first = fixture.Player;
        var second = Player.CreateForNewRun(ModelDb.Character<DuanYuCharacter>(), UnlockState.all, 2);
        fixture.Run.AddPlayerDebug(second, -1);
        second.ResetCombatState();
        fixture.Combat.AddPlayer(second);
        await Hook.BeforeCombatStart(fixture.Run, fixture.Combat);
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [first.Creature, second.Creature]);
        await Hook.BeforeHandDraw(fixture.Combat, first, new ThrowingPlayerChoiceContext());
        await Hook.BeforeHandDraw(fixture.Combat, second, new ThrowingPlayerChoiceContext());
        await SetQi(first, 5);
        await SetQi(second, 3);
        await Hook.AfterSideTurnEnd(fixture.Combat, CombatSide.Enemy, [first.Creature, second.Creature, fixture.Enemy]);
        AssertQi(first, 5, "enemy-side end preserves the first player Qi");
        AssertQi(second, 3, "enemy-side end preserves companion Qi");
        await Hook.AfterSideTurnEnd(fixture.Combat, CombatSide.Player, [first.Creature]);
        AssertQi(first, 5, "participating player retains Qi");
        AssertQi(second, 3, "nonparticipating companion preserves Qi");
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [first.Creature, second.Creature]);
        await Hook.BeforeHandDraw(fixture.Combat, first, new ThrowingPlayerChoiceContext());
        await Hook.BeforeHandDraw(fixture.Combat, second, new ThrowingPlayerChoiceContext());
        await Hook.AfterSideTurnEnd(fixture.Combat, CombatSide.Player, [first.Creature, second.Creature, first.Creature]);
        AssertQi(first, 5, "duplicate participant retains Qi");
        AssertQi(second, 3, "companion independently retains Qi");
        await Hook.AfterSideTurnEnd(fixture.Combat, CombatSide.Player, [first.Creature, second.Creature]);
        AssertQi(first, 5, "duplicate shared end preserves first player Qi");
        AssertQi(second, 3, "duplicate shared end preserves companion Qi");
        await DuanYuState.LoseQi(first, 2);
        AssertQi(first, 3, "explicit Qi loss still affects its owner");
        AssertQi(second, 3, "explicit owner loss does not change companion Qi");
    }

    private static async Task TurnEndOrdering()
    {
        foreach (int startingQi in new[] { 1, 3 })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            var player = fixture.Player;
            await SetQi(player, startingQi);
            await DuanYuState.AddEffect(player, "DY_U05");
            await DuanYuState.AddEffect(player, "DY_U28", 2);
            var status = EngineSelfTests.AddCard(fixture, "DY_S01", PileType.Hand);
            CardCmd.ApplySingleTurnRetain(status);
            await Hook.BeforeSideTurnEnd(fixture.Combat, CombatSide.Player, [player.Creature]);
            AssertQi(player, startingQi, "guard hook preserves stored Qi");
            EngineSelfTests.Equal(startingQi / 2, player.Creature.Block, "Qi Guard uses half the stored Qi before explicit status loss");
            var turnState = AccessTools.Field(typeof(CombatManager), "_turnState").GetValue(CombatManager.Instance)!;
            await (Task)AccessTools.Method(typeof(CombatManager), "DoTurnEnd")
                .Invoke(CombatManager.Instance, [turnState, player, new ThrowingPlayerChoiceContext()])!;
            int afterStatus = Math.Max(0, startingQi - 2) + 1;
            AssertQi(player, afterStatus, "status loses two Qi before ethereal exhaust restores one");
            EngineSelfTests.Equal(PileType.Exhaust, status.Pile!.Type, "ethereal status exhausts despite single-turn retain");
            EngineSelfTests.Equal(1, CombatManager.Instance.History.Entries.OfType<CardExhaustedEntry>().Count(entry => ReferenceEquals(entry.Card, status)), "native status exhaust occurs once");
            EngineSelfTests.Equal(startingQi / 2 + 2, player.Creature.Block, "native status exhaustion grants one exhaust-power block trigger");
            await Hook.AfterSideTurnEnd(fixture.Combat, CombatSide.Player, [player.Creature]);
            AssertQi(player, afterStatus, "side end preserves Qi after explicit status loss and exhaust refund");
            EngineSelfTests.Equal(0, DuanYuState.Get(player).SpendingCount, "the complete turn-end sequence records no genuine Qi payment");
        }
    }

    private static async Task InactiveCombat()
    {
        foreach (bool defeat in new[] { false, true })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            var player = fixture.Player;
            await SetQi(player, 5);
            var victim = defeat ? player.Creature : fixture.Enemy;
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), victim, victim.CurrentHp,
                ValueProp.Unblockable | ValueProp.Unpowered, defeat ? fixture.Enemy : player.Creature);
            EngineSelfTests.Equal(false, DuanYuState.CanAct(player), "dead owner or completed combat cannot act");
            int qi = DuanYuState.Get(player).Qi;
            int resource = CustomResources<QiResource>.Get(player.PlayerCombatState!).Amount;
            int power = player.Creature.GetPowerAmount<QiPower>();
            await Hook.AfterSideTurnEnd(fixture.Combat, CombatSide.Player, [player.Creature]);
            EngineSelfTests.Equal(qi, DuanYuState.Get(player).Qi, "inactive turn-end hook preserves Qi");
            EngineSelfTests.Equal(resource, CustomResources<QiResource>.Get(player.PlayerCombatState!).Amount, "inactive turn-end hook preserves the resource display");
            EngineSelfTests.Equal(power, player.Creature.GetPowerAmount<QiPower>(), "inactive turn-end hook preserves the Qi power");
        }
    }

    private static async Task SetQi(Player player, int amount)
    {
        await DuanYuState.LoseQi(player, int.MaxValue);
        await DuanYuState.GainQi(player, amount);
    }

    private static void AssertQi(Player player, int expected, string context)
    {
        EngineSelfTests.Equal(expected, DuanYuState.Get(player).Qi, context + " ledger");
        EngineSelfTests.Equal(expected, CustomResources<QiResource>.Get(player.PlayerCombatState!).Amount, context + " resource display");
        EngineSelfTests.Equal(expected, player.Creature.GetPowerAmount<QiPower>(), context + " power display");
    }
}
#endif
