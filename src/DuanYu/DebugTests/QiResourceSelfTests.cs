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
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;

namespace DuanYu.DebugTests;

internal static class QiResourceSelfTests
{
    private static readonly string[] FeeCards =
    [
        "DY_B05", "DY_C14", "DY_C15", "DY_C17", "DY_U07", "DY_U17", "DY_U32", "DY_U33",
        "DY_R05", "DY_R06", "DY_R13", "DY_R20",
        "DY_T01", "DY_T02", "DY_T03", "DY_T04", "DY_T05", "DY_T06"
    ];

    internal static IEnumerable<(string Name, Func<Task> Action)> Cases
    {
        get
        {
            foreach (string id in FeeCards)
                foreach (bool upgraded in new[] { false, true })
                    yield return ($"qi:mandatory-payment-{id}{(upgraded ? "+" : "")}", () => MandatoryPayment(id, upgraded));
            foreach (bool upgraded in new[] { false, true })
                yield return ($"qi:x-payment-and-refund{(upgraded ? "+" : "")}", () => QiXPayment(upgraded));
            yield return ("qi:x-zero-payment-captured-before-later-gain", QiXZeroCapture);
            yield return ("qi:x-auto-play-refreshes-payment", QiXAutoPlay);
            yield return ("qi:x-canonical-preview-has-no-owner", QiXCanonicalPreview);
            yield return ("qi:independent-display-and-native-stars", IndependentResource);
            yield return ("qi:uncapped-card-payment-and-refund", UncappedPayment);
            yield return ("qi:uncapped-qi-scaling-attack", UncappedScalingAttack);
            yield return ("qi:uncapped-qi-scaling-turn-end-power", UncappedScalingPower);
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
        var qiCost = CustomResources<QiResource>.Cost(card)
            ?? throw new InvalidOperationException(id + " must retain its resource cost display registration");
        EngineSelfTests.Equal(true, card.RequiredQi > 0, id + " retains a mandatory Qi fee after shortening its body text");
        EngineSelfTests.Equal(card.QiCost, qiCost.Base, id + " resource icon retains its printed base fee");
        EngineSelfTests.Equal(card.QiCost, qiCost.GetWithModifiers(CostModifiers.All), id + " resource icon displays the full Qi fee");
        EngineSelfTests.Equal(card.QiCost, qiCost.GetAmountToSpend(), id + " resource cost still requires its full amount");
        EngineSelfTests.Equal(false, card.GetDescriptionForPile(PileType.Hand).Contains($"需{card.QiCost}点真气", StringComparison.Ordinal),
            id + " body text omits the fee already shown by its resource icon");
        await SetQi(player, card.RequiredQi - 1);
        int qi = DuanYuState.Get(player).Qi;
        EngineSelfTests.Equal(true, card.HoverTips.OfType<HoverTip>().Any(tip => tip.Description.Contains($"当前{qi}/需要{card.RequiredQi}：", StringComparison.Ordinal)),
            id + " hover still explains the current Qi and required fee when unaffordable");
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
        EngineSelfTests.Equal(true, card.HoverTips.OfType<HoverTip>().Any(tip => tip.Description.Contains($"当前真气：{card.RequiredQi}；出牌前支付{card.RequiredQi}点。", StringComparison.Ordinal)),
            id + " hover still explains the current Qi and payment when affordable");
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
        EngineSelfTests.Equal("0", resource.GetTextForAmount(0), "Qi counter displays its current amount without a maximum");
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
        await DuanYuState.GainQi(player, 100);
        AssertQi(player, 100, "resource gain exceeds the former nine-point limit");
        EngineSelfTests.Equal("100", resource.GetTextForAmount(resource.Amount), "high Qi displays its full amount without a maximum");
        EngineSelfTests.Equal(2, combat.Stars, "high Qi preserves native stars");
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [player.Creature]);
        AssertQi(player, 100, "high Qi carries across turn reset");
        await DuanYuState.LoseQi(player, 50);
        AssertQi(player, 50, "explicit loss subtracts from uncapped Qi");
        await DuanYuState.SpendQi(player, 38);
        AssertQi(player, 12, "large payment subtracts in full above nine");
        EngineSelfTests.Equal(false, await resource.Spend<QiResource>(fixture.Combat, null, 13, false), "uncapped Qi still requires a fully affordable fee");
        AssertQi(player, 12, "failed payment preserves high Qi");
        EngineSelfTests.Equal(2, combat.Stars, "high Qi loss and payment preserve native stars");
    }

    private static async Task QiXPayment(bool upgraded)
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        var player = fixture.Player;
        await RelicCmd.Obtain<DaliSeal>(player);
        await DuanYuState.AddEffect(player, "DY_R01", 1, energyAmount: 1);
        await SetQi(player, 6);
        await EngineSelfTests.PaidPlay(fixture, EngineSelfTests.AddCard(fixture, "DY_T01", PileType.Hand));
        await SetQi(player, 12);
        player.PlayerCombatState!.Energy = 0;
        var card = EngineSelfTests.AddCard(fixture, "DY_R21", PileType.Hand, upgraded);
        card.BaseReplayCount = 1;
        var qiCost = CustomResources<QiResource>.Cost(card) ?? throw new InvalidOperationException("Qi-X cost is missing");
        EngineSelfTests.Equal(true, qiCost.CostsX, "Qi-X uses the custom resource X fee");
        EngineSelfTests.Equal(12, card.RequiredQi, "Qi-X current required payment is all held Qi");
        EngineSelfTests.Equal(true, card.CanPlay(), "Qi-X can play without energy");
        int health = fixture.Enemy.CurrentHp;
        var paid = await card.SpendResources();
        AssertQi(player, 0, "Qi-X pays all twelve Qi before resolving effects or refunds");
        EngineSelfTests.Equal(12, qiCost.CapturedXValue, "Qi-X captures actual payment before any refund");
        EngineSelfTests.Equal(0, player.PlayerCombatState.Energy, "Qi-X payment spends zero energy");
        await Resolve(fixture, card, false, paid.Item1, paid.Item2);
        EngineSelfTests.Equal(24 * ((upgraded ? 8 : 6) + 1), health - fixture.Enemy.CurrentHp, "each replay body hits twelve times with the same Sword Momentum bonus on every hit");
        AssertQi(player, 1, "second payment receives one Beiming refund despite replay");
        EngineSelfTests.Equal(2, player.PlayerCombatState.Energy, "second payment receives Beiming and the empty-Qi seal energy once each");
        EngineSelfTests.Equal(2, DuanYuState.Get(player).SpendingCount, "Qi-X activation counts one payment after the priming sword");
        EngineSelfTests.Equal(PileType.Exhaust, card.Pile!.Type, "Qi-X still exhausts");
    }

    private static async Task QiXZeroCapture()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        await SetQi(fixture.Player, 0);
        fixture.Player.PlayerCombatState!.Energy = 0;
        await DuanYuState.AddEffect(fixture.Player, "DY_R01", 1, energyAmount: 1);
        var card = EngineSelfTests.AddCard(fixture, "DY_R21", PileType.Hand);
        EngineSelfTests.Equal(true, card.CanPlay(), "zero-Qi X remains playable");
        int health = fixture.Enemy.CurrentHp;
        var paid = await card.SpendResources();
        var cost = CustomResources<QiResource>.Cost(card)!;
        EngineSelfTests.Equal(0, cost.CapturedXValue, "zero-Qi payment captures zero");
        await DuanYuState.GainQi(fixture.Player, 5);
        await Resolve(fixture, card, false, paid.Item1, paid.Item2);
        EngineSelfTests.Equal(health, fixture.Enemy.CurrentHp, "Qi gained after a zero payment cannot increase its damage");
        AssertQi(fixture.Player, 5, "Qi gained after a zero payment is not charged retroactively");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SpendingCount, "zero payment does not register a successful spend");
        EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Energy, "zero payment does not trigger Beiming energy");
    }

    private static async Task QiXAutoPlay()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        await SetQi(fixture.Player, 4);
        fixture.Player.PlayerCombatState!.Energy = 3;
        var card = EngineSelfTests.AddCard(fixture, "DY_R21", PileType.Hand);
        int health = fixture.Enemy.CurrentHp;
        await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(), card, fixture.Enemy, skipCardPileVisuals: true);
        EngineSelfTests.Equal(24, health - fixture.Enemy.CurrentHp, "auto-play pays and uses all four Qi");
        AssertQi(fixture.Player, 0, "auto-play actually deducts its custom X resource");
        EngineSelfTests.Equal(4, CustomResources<QiResource>.Cost(card)!.CapturedXValue, "first auto-play captures its actual Qi payment");
        await CardPileCmd.Add(card, PileType.Hand);
        await DuanYuState.GainQi(fixture.Player, 2);
        health = fixture.Enemy.CurrentHp;
        await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(), card, fixture.Enemy, skipCardPileVisuals: true);
        EngineSelfTests.Equal(12, health - fixture.Enemy.CurrentHp, "independent auto-play refreshes X instead of reusing four");
        AssertQi(fixture.Player, 0, "independent auto-play pays newly gained Qi");
        EngineSelfTests.Equal(2, CustomResources<QiResource>.Cost(card)!.CapturedXValue, "second auto-play captures its new payment");
        EngineSelfTests.Equal(2, DuanYuState.Get(fixture.Player).SpendingCount, "independent positive auto-plays each count one payment");
        await CardPileCmd.Add(card, PileType.Hand);
        health = fixture.Enemy.CurrentHp;
        await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(), card, fixture.Enemy, skipCardPileVisuals: true);
        EngineSelfTests.Equal(health, fixture.Enemy.CurrentHp, "zero-Qi auto-play cannot reuse a previous payment");
        EngineSelfTests.Equal(0, CustomResources<QiResource>.Cost(card)!.CapturedXValue, "zero-Qi auto-play refreshes X to zero");
        EngineSelfTests.Equal(2, DuanYuState.Get(fixture.Player).SpendingCount, "zero auto-play does not create a positive payment");
        EngineSelfTests.Equal(3, fixture.Player.PlayerCombatState.Energy, "all auto-plays preserve native energy");
    }

    private static Task QiXCanonicalPreview()
    {
        var card = (DuanYuCard)DuanYuCard.Canonical("DY_R21");
        var cost = CustomResources<QiResource>.Cost(card) ?? throw new InvalidOperationException("canonical Qi-X cost is missing");
        EngineSelfTests.Equal(true, cost.CostsX, "canonical card registers a true Qi-X cost");
        EngineSelfTests.Equal(0, card.RequiredQi, "canonical Qi-X has no owned resource to spend");
        EngineSelfTests.Equal(true, card.GetDescriptionForPile(PileType.None).Length > 0, "canonical Qi-X description renders without an owner");
        return Task.CompletedTask;
    }

    private static async Task UncappedPayment()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        var player = fixture.Player;
        await RelicCmd.Obtain<TianlongPrayerBeads>(player);
        await RelicCmd.Obtain<DaliSeal>(player);
        await DuanYuState.AddEffect(player, "DY_R01", 1, energyAmount: 1);
        EngineSelfTests.Equal(true, await DuanYuState.SpendQi(player, 1), "high-Qi refund fixture records its first payment");
        await DuanYuState.AfterQiSpent(player, new ThrowingPlayerChoiceContext());
        await SetQi(player, 50);
        var card = EngineSelfTests.AddCard(fixture, "DY_U07", PileType.Hand);
        card.BaseReplayCount = 2;
        EngineSelfTests.Equal(true, card.CanPlay(), "high Qi can pay the card's full mandatory cost");
        EngineSelfTests.Equal(true, card.HoverTips.OfType<HoverTip>().Any(tip => tip.Description.Contains("当前真气：50；出牌前支付3点。", StringComparison.Ordinal)),
            "card hover shows high current Qi without the former maximum");
        int energy = player.PlayerCombatState!.Energy;
        var paid = await card.SpendResources();
        AssertQi(player, 47, "high-Qi card pays its full fee before any refund or effect");
        await Resolve(fixture, card, false, paid.Item1, paid.Item2);
        AssertQi(player, 48, "replaying a high-Qi card does not repay or repeat its refund");
        EngineSelfTests.Equal(3, CombatManager.Instance.History.CardPlaysFinished.Count(entry => ReferenceEquals(entry.CardPlay.Card, card)), "high-Qi replay resolves all three bodies");
        EngineSelfTests.Equal(2, DuanYuState.Get(player).SpendingCount, "high-Qi replay counts one full payment after the priming payment");
        EngineSelfTests.Equal(false, DuanYuState.Get(player).SealUsed, "a high balance never falsely triggers an empty-Qi seal reward");
        EngineSelfTests.Equal(energy - paid.Item1 + 1, player.PlayerCombatState.Energy, "high-Qi payment grants only one printed energy refund");
        EngineSelfTests.Equal(4, player.Creature.Block, "high-Qi payment grants prayer block once");
    }

    private static async Task UncappedScalingAttack()
    {
        foreach (bool upgraded in new[] { false, true })
        foreach (int qi in new[] { 100, 101 })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            await SetQi(fixture.Player, qi);
            var card = EngineSelfTests.AddCard(fixture, "DY_U08", PileType.Hand, upgraded);
            int health = fixture.Enemy.CurrentHp;
            await EngineSelfTests.PaidPlay(fixture, card);
            EngineSelfTests.Equal((upgraded ? 23 : 16) + qi, health - fixture.Enemy.CurrentHp, "Qi-scaling attack uses the full stored amount above nine");
            AssertQi(fixture.Player, qi, "Qi-scaling attack reads the resource without spending it");
            EngineSelfTests.Equal(1, PileType.Discard.GetPile(fixture.Player).Cards.OfType<DuanYuCard>().Count(candidate => candidate.DesignId == "DY_S01"), "high-Qi attack retains its printed status drawback");
        }
    }

    private static async Task UncappedScalingPower()
    {
        foreach (bool upgraded in new[] { false, true })
        foreach (int qi in new[] { 100, 1001 })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            await SetQi(fixture.Player, qi);
            var card = EngineSelfTests.AddCard(fixture, "DY_U05", PileType.Hand, upgraded);
            await EngineSelfTests.PaidPlay(fixture, card);
            EngineSelfTests.Equal(0, fixture.Player.Creature.Block, "Qi Guard waits for turn end");
            await Hook.BeforeSideTurnEnd(fixture.Combat, CombatSide.Player, [fixture.Player.Creature]);
            EngineSelfTests.Equal(qi / 2 + (upgraded ? 2 : 0), fixture.Player.Creature.Block, "Qi Guard scales from the full stored amount and rounds down");
            AssertQi(fixture.Player, qi, "high-Qi guard does not spend the stored resource");
        }
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
        EngineSelfTests.Equal(0, DuanYuState.Get(player).Qi, "native replay's first payment gives no Beiming refund");
        EngineSelfTests.Equal(energy - paid.Item1 + 1, player.PlayerCombatState.Energy, "native replay grants seal energy once without a first-payment Beiming reward");
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
        EngineSelfTests.Equal(16, health - fixture.Enemy.CurrentHp, "paid auto-play resolves the full card");
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
        await SetQi(first, 50);
        await SetQi(second, 12);
        var firstResource = CustomResources<QiResource>.Get(first.PlayerCombatState!);
        var secondResource = CustomResources<QiResource>.Get(second.PlayerCombatState!);
        await DuanYuState.SpendQi(first, 3);
        EngineSelfTests.Equal(47, firstResource.Amount, "first player payment updates their uncapped Qi");
        EngineSelfTests.Equal(12, secondResource.Amount, "first player payment preserves companion uncapped Qi");
        await DuanYuState.LoseQi(second, 12);
        EngineSelfTests.Equal(47, firstResource.Amount, "companion Qi loss preserves first player");
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

    private static void AssertQi(Player player, int expected, string message)
    {
        EngineSelfTests.Equal(expected, DuanYuState.Get(player).Qi, message + " ledger");
        EngineSelfTests.Equal(expected, CustomResources<QiResource>.Get(player.PlayerCombatState!).Amount, message + " resource");
        EngineSelfTests.Equal(expected, (int)(player.Creature.GetPower<QiPower>()?.Amount ?? 0), message + " power");
    }

    private static Task Resolve(TestFixture fixture, DuanYuCard card, bool autoPlay, int energy = 0, int stars = 0)
    {
        Creature? target = card.TargetType == TargetType.AnyEnemy ? fixture.Enemy : null;
        return card.OnPlayWrapper(new ThrowingPlayerChoiceContext(), target, autoPlay,
            new ResourceInfo { EnergySpent = energy, EnergyValue = energy, StarsSpent = stars, StarValue = stars }, true);
    }
}
#endif
