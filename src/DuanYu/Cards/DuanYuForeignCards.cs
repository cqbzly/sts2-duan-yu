using DuanYu.Character;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace DuanYu.Cards;

public static class DuanYuForeignCards
{
    internal static IReadOnlyList<CardModel> GetAttackPool()
    {
        var characters = ModelDb.AllCharacters.Where(character => character is not DuanYuCharacter).ToArray();
        var poolIds = characters.Select(character => character.CardPool.Id).ToHashSet();
        var characterCards = characters.SelectMany(character => character.CardPool.AllCards.Concat(character.StartingDeck));
        // Some character attacks live in a shared event pool but retain their character's visual pool.
        var affiliatedAttacks = ModelDb.AllCards.Where(card => card.Type == CardType.Attack
            && card is not DuanYuCard && poolIds.Contains(card.VisualCardPool.Id));
        return characterCards.Concat(affiliatedAttacks)
            .Where(card => card.Type == CardType.Attack && card.Rarity != CardRarity.Basic && card is not DuanYuCard)
            .DistinctBy(card => card.Id)
            .OrderBy(card => card.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
    }

    public static async Task GenerateAttacks(Player player, PlayerChoiceContext context, int count, bool upgraded)
    {
        if (count <= 0 || !DuanYuState.CanAct(player)) return;
        var candidates = GetAttackPool().ToList();
        var generationCount = Math.Min(count, candidates.Count);
        for (var index = 0; index < generationCount; index++)
        {
            if (!DuanYuState.CanAct(player)) return;
            var choice = player.RunState.Rng.CombatCardGeneration.NextInt(candidates.Count);
            var canonical = candidates[choice];
            candidates.RemoveAt(choice);
            var card = player.Creature.CombatState!.CreateCard(canonical, player);
            if (upgraded) CardCmd.Upgrade(card);
            card.EnergyCost.SetThisTurnOrUntilPlayed(0);
            card.AddKeyword(CardKeyword.Exhaust);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        }
    }
}
