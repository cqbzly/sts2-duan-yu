#if DEBUG
using BaseLib.Abstracts;
using DuanYu.Cards;
using DuanYu.Character;
using DuanYu.Relics;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;

namespace DuanYu.DebugTests;

internal static class QiResourceSelfTests
{
    private static readonly string[] FeeCards =
    [
        "DY_B05", "DY_C14", "DY_C15", "DY_C17", "DY_U07", "DY_U17", "DY_U32", "DY_U33",
        "DY_R03", "DY_R05", "DY_R06", "DY_R13", "DY_R20", "DY_R21",
        "DY_T01", "DY_T02", "DY_T03", "DY_T04", "DY_T05", "DY_T06"
    ];

    internal static IEnumerable<(string Name, Func<Task> Action)> Cases
    {
        get
        {
            foreach (string id in FeeCards)
                foreach (bool upgraded in new[] { false, true })
                    yield return ($"qi:mandatory-payment-{id}{(upgraded ? "+" : "")}", () => MandatoryPayment(id, upgraded));
            yield return ("qi:independent-display-and-native-stars", IndependentResource);
            yield return ("qi:free-energy-retains-mandatory-cost", FreeEnergy);
            yield return ("qi:native-replay-pays-once-and-refunds-once", ReplayPayment);
            yield return ("qi:auto-play-pays-and-cannot-reuse-old-payment", AutoPlayPayment);
            yield return ("qi:player-resource-isolation", PlayerIsolation);
            yield return ("qi:cancelled-play-clears-payment", CancelledPayment);
        }
    }

    private static async Task MandatoryPayment(string id, bool upgraded)
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        var player = fixture.Player;
        var combat = player.PlayerCombatState!;
        var card = EngineSelfTests.AddCard(fixture, id, PileType.Hand, upgraded);
        await SetQi(player, card.RequiredQi - 1);
        int qi = DuanYuState.Get(player).Qi;
        int energy = combat.Energy;
        int block = player.Creature.Block;
        int health = fixture.Enemy.CurrentHp;
        int hand = PileType.Hand.GetPile(player).Cards.Count;
        int draw = PileType.Draw.GetPile(player).Cards.Count;
        int discard = PileType.Discard.GetPile(player).Cards.Count;
        int exhaust = PileType.Exhaust.GetPile(player).Cards.Count;
        int history = CombatManager.Instance.History.CardPlaysFinished.Count();
        EngineSelfTests.Equal(false, card.CanPlay(), id + " cannot play one Qi below its fee");
        var costs = await card.SpendResources();
        EngineSelfTests.Equal((0, 0), costs, id + " insufficient Qi rejects every resource payment");
        await Resolve(fixture, card, true);
        EngineSelfTests.Equal(qi, DuanYuState.Get(player).Qi, id + " rejected play preserves Qi");
        EngineSelfTests.Equal(energy, combat.Energy, id + " rejected play preserves energy");
        EngineSelfTests.Equal(block, player.Creature.Block, id + " rejected play grants no block");
        EngineSelfTests.Equal(health, fixture.Enemy.CurrentHp, id + " rejected play deals no damage");
        EngineSelfTests.Equal(hand, PileType.Hand.GetPile(player).Cards.Count, id + " rejected play preserves hand");
        EngineSelfTests.Equal(draw, PileType.Draw.GetPile(player).Cards.Count, id + " rejected play draws no cards");
        EngineSelfTests.Equal(discard, PileType.Discard.GetPile(player).Cards.Count, id + " rejected play generates no discard cards");
        EngineSelfTests.Equal(exhaust, PileType.Exhaust.GetPile(player).Cards.Count, id + " rejected play exhausts no cards");
        EngineSelfTests.Equal(PileType.Hand, card.Pile!.Type, id + " rejected card stays in hand");
        EngineSelfTests.Equal(history, CombatManager.Instance.History.CardPlaysFinished.Count(), id + " rejected play records no play");
        EngineSelfTests.Equal(0, DuanYuState.Get(player).SpendingCount, id + " rejected play records no spend");
        EngineSelfTests.Equal(0, DuanYuState.Get(player).AttacksPlayed, id + " rejected play records no attack");
        EngineSelfTests.Equal(0, DuanYuState.Get(player).SkillsPlayed, id + " rejected play records no skill");
        EngineSelfTests.Equal(0, DuanYuState.Get(player).SwordPlays, id + " rejected play records no sword");
        await SetQi(player, card.RequiredQi);
        EngineSelfTests.Equal(true, card.CanPlay(), id + " is payable at its exact fee");
        await SetQi(player, card.RequiredQi);
        EngineSelfTests.Equal(true, card.CanPlay(), id + " is playable with its exact fee");
        await SetQi(player, card.RequiredQi + 1);
        EngineSelfTests.Equal(true, card.CanPlay(), id + " is payable above its fee");
        var paid = await card.SpendResources();
        EngineSelfTests.Equal(1, DuanYuState.Get(player).Qi, id + " pays full Qi fee before its effects");
        await Resolve(fixture, card, false, paid.Item1, paid.Item2);
        EngineSelfTests.Equal(1, DuanYuState.Get(player).Qi, id + " does not pay Qi twice");
        EngineSelfTests.Equal(1, DuanYuState.Get(player).SpendingCount, id + " records exactly one resource payment");
        EngineSelfTests.Equal(false, card.Pile?.Type == PileType.Play, id + " completes after payment");
    }

    private static async Task IndependentResource()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        var player = fixture.Player;
        var combat = player.PlayerCombatState!;
        await SetQi(player, 0);
        var resource = CustomResources<QiResource>.Get(combat);
        EngineSelfTests.Equal(true, resource.ShouldShowDisplay(), "zero Qi remains visible for Duan Yu");
        EngineSelfTests.Equal("0/9", resource.GetTextForAmount(0), "Qi counter exposes its own maximum");
        EngineSelfTests.Equal("真气", resource.MakeTip()!.Value.Title, "Qi hover tip uses the resource name");
        EngineSelfTests.Equal(true, resource.TexturePath.Contains("power-cqbzly-dy-qi", StringComparison.OrdinalIgnoreCase), "Qi uses its own resource icon");
        EngineSelfTests.Equal(false, player.Character.ShouldAlwaysShowStarCounter, "Duan Yu does not force the native stars counter");
        combat.Stars = 5;
        await DuanYuState.GainQi(player, 4);
        EngineSelfTests.Equal(4, resource.Amount, "Qi display follows gaining Qi");
        EngineSelfTests.Equal(5, combat.Stars, "gaining Qi preserves native stars");
        combat.Stars = 2;
        EngineSelfTests.Equal(4, resource.Amount, "changing native stars preserves Qi");
        await DuanYuState.SpendQi(player, 3);
        EngineSelfTests.Equal(1, resource.Amount, "Qi display follows spending Qi");
        EngineSelfTests.Equal(2, combat.Stars, "spending Qi preserves native stars");
        await DuanYuState.LoseQi(player, 1);
        EngineSelfTests.Equal(0, resource.Amount, "Qi loss updates the independent display");
        EngineSelfTests.Equal(true, resource.ShouldShowDisplay(), "zeroing Qi keeps its display visible");
        EngineSelfTests.Equal(false, await resource.Spend<QiResource>(fixture.Combat, null, 1, false), "strict resource spending rejects an unaffordable mandatory fee");
        EngineSelfTests.Equal(0, resource.Amount, "failed strict spending preserves zero Qi");
        await DuanYuState.GainQi(player, int.MaxValue);
        EngineSelfTests.Equal(9, resource.Amount, "resource display obeys the Qi cap");
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [player.Creature]);
        EngineSelfTests.Equal(9, resource.Amount, "Qi carries across turn reset");
    }

    private static async Task FreeEnergy()
    {
        foreach (bool combatFree in new[] { false, true })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            var card = EngineSelfTests.AddCard(fixture, "DY_U07", PileType.Hand);
            if (combatFree) card.SetToFreeThisCombat();
            else card.SetToFreeThisTurn();
            EngineSelfTests.Equal(0, card.EnergyCost.GetAmountToSpend(), "free energy applies normally");
            EngineSelfTests.Equal(3, card.RequiredQi, "free energy leaves Qi fee intact");
            await SetQi(fixture.Player, 2);
            EngineSelfTests.Equal(false, card.CanPlay(), "free card remains unplayable without full Qi");
            await SetQi(fixture.Player, 3);
            int energy = fixture.Player.PlayerCombatState!.Energy;
            var paid = await card.SpendResources();
            await Resolve(fixture, card, false, paid.Item1, paid.Item2);
            EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "energy-free card pays full Qi");
            EngineSelfTests.Equal(energy, fixture.Player.PlayerCombatState.Energy, "energy-free card spends no energy");
        }
    }

    private static async Task ReplayPayment()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        var player = fixture.Player;
        await RelicCmd.Obtain<TianlongPrayerBeads>(player);
        await RelicCmd.Obtain<DaliSeal>(player);
        await DuanYuState.AddEffect(player, "DY_U01");
        await DuanYuState.AddEffect(player, "DY_R01", 2, energyAmount: 1);
        await SetQi(player, 3);
        var card = EngineSelfTests.AddCard(fixture, "DY_U07", PileType.Hand);
        card.BaseReplayCount = 2;
        int energy = player.PlayerCombatState!.Energy;
        var paid = await card.SpendResources();
        await Resolve(fixture, card, false, paid.Item1, paid.Item2);
        EngineSelfTests.Equal(3, CombatManager.Instance.History.CardPlaysFinished.Count(entry => ReferenceEquals(entry.CardPlay.Card, card)), "native replay resolves all three bodies");
        EngineSelfTests.Equal(1, DuanYuState.Get(player).SpendingCount, "native replay pays Qi once");
        EngineSelfTests.Equal(2, DuanYuState.Get(player).Qi, "native replay refunds Qi once");
        EngineSelfTests.Equal(energy - paid.Item1 + 2, player.PlayerCombatState.Energy, "native replay grants Beiming and seal energy once each");
        EngineSelfTests.Equal(4, player.Creature.Block, "native replay grants prayer block once");
        EngineSelfTests.Equal(1, PileType.Hand.GetPile(player).Cards.Count, "native replay grants first-spend draw once");
    }

    private static async Task AutoPlayPayment()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        var player = fixture.Player;
        var card = EngineSelfTests.AddCard(fixture, "DY_B05", PileType.Hand);
        await SetQi(player, 1);
        int health = fixture.Enemy.CurrentHp;
        int energy = player.PlayerCombatState!.Energy;
        await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(), card, fixture.Enemy, skipCardPileVisuals: true);
        EngineSelfTests.Equal(health, fixture.Enemy.CurrentHp, "insufficient auto-play deals no damage");
        EngineSelfTests.Equal(0, DuanYuState.Get(player).SpendingCount, "insufficient auto-play records no spend");
        await SetQi(player, 3);
        await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(), card, fixture.Enemy, skipCardPileVisuals: true);
        EngineSelfTests.Equal(1, DuanYuState.Get(player).Qi, "auto-play pays its full Qi cost");
        EngineSelfTests.Equal(14, health - fixture.Enemy.CurrentHp, "paid auto-play resolves the full card");
        EngineSelfTests.Equal(energy, player.PlayerCombatState.Energy, "auto-play leaves native energy free");
        await CardPileCmd.Add(card, PileType.Hand);
        health = fixture.Enemy.CurrentHp;
        await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(), card, fixture.Enemy, skipCardPileVisuals: true);
        EngineSelfTests.Equal(health, fixture.Enemy.CurrentHp, "later auto-play cannot reuse previous payment");
        EngineSelfTests.Equal(1, DuanYuState.Get(player).SpendingCount, "later insufficient auto-play creates no new payment");
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
        await SetQi(first, 7);
        await SetQi(second, 4);
        var firstResource = CustomResources<QiResource>.Get(first.PlayerCombatState!);
        var secondResource = CustomResources<QiResource>.Get(second.PlayerCombatState!);
        await DuanYuState.SpendQi(first, 3);
        EngineSelfTests.Equal(4, firstResource.Amount, "first player payment updates their Qi");
        EngineSelfTests.Equal(4, secondResource.Amount, "first player payment preserves companion Qi");
        await DuanYuState.LoseQi(second, 4);
        EngineSelfTests.Equal(4, firstResource.Amount, "companion Qi loss preserves first player");
        EngineSelfTests.Equal(0, secondResource.Amount, "companion Qi loss changes only companion");
    }

    private static async Task CancelledPayment()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        var card = EngineSelfTests.AddCard(fixture, "DY_B05", PileType.Hand);
        await SetQi(fixture.Player, 3);
        await card.SpendResources();
        EngineSelfTests.Equal(true, DuanYuState.HasCardQiPayment(card), "paid activation has a pending payment");
        await CreatureCmd.SetCurrentHp(fixture.Player.Creature, 0);
        await Resolve(fixture, card, false);
        EngineSelfTests.Equal(false, DuanYuState.HasCardQiPayment(card), "a cancelled activation clears its payment");
    }

    private static async Task SetQi(Player player, int amount)
    {
        await DuanYuState.LoseQi(player, int.MaxValue);
        await DuanYuState.GainQi(player, amount);
    }

    private static Task Resolve(TestFixture fixture, DuanYuCard card, bool autoPlay, int energy = 0, int stars = 0)
    {
        Creature? target = card.TargetType == TargetType.AnyEnemy ? fixture.Enemy : null;
        return card.OnPlayWrapper(new ThrowingPlayerChoiceContext(), target, autoPlay,
            new ResourceInfo { EnergySpent = energy, EnergyValue = energy, StarsSpent = stars, StarValue = stars }, true);
    }
}
#endif
