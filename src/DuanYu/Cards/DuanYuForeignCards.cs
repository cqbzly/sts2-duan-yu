using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace DuanYu.Cards;

public static class DuanYuForeignCards
{
    private static readonly Type[] AttackTypes =
    [
        typeof(Bash), typeof(IronWave), typeof(PommelStrike), typeof(TwinStrike),
        typeof(DaggerSpray), typeof(DaggerThrow), typeof(Slice), typeof(PoisonedStab),
        typeof(BeamCell), typeof(GoForTheEyes), typeof(SweepingBeam), typeof(RipAndTear)
    ];

    public static async Task GenerateAttacks(Player player, PlayerChoiceContext context, int count, bool upgraded)
    {
        if (count <= 0 || !DuanYuState.CanAct(player)) return;
        var candidates = AttackTypes.ToList();
        var generationCount = Math.Min(count, candidates.Count);
        for (var index = 0; index < generationCount; index++)
        {
            if (!DuanYuState.CanAct(player)) return;
            var choice = player.RunState.Rng.CombatCardGeneration.NextInt(candidates.Count);
            var canonical = ModelDb.GetById<CardModel>(ModelDb.GetId(candidates[choice]));
            candidates.RemoveAt(choice);
            var card = player.Creature.CombatState!.CreateCard(canonical, player);
            if (upgraded) CardCmd.Upgrade(card);
            card.EnergyCost.SetThisTurnOrUntilPlayed(0);
            card.AddKeyword(CardKeyword.Exhaust);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        }
    }
}
