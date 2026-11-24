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

    public static async Task GenerateAttacks(Player player, PlayerChoiceContext context, int count, bool upgraded, bool addExhaust = true)
    {
        foreach (var canonical in SampleAttacks(player, count))
        {
            var card = CreateBorrowedAttack(player, canonical, upgraded, addExhaust);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        }
    }

    public static async Task ChooseAttack(Player player, PlayerChoiceContext context, bool upgraded)
    {
        var combat = player.Creature.CombatState;
        if (combat == null) return;

        var options = SampleAttacks(player, 3)
            .Select(canonical => CreateBorrowedAttack(player, canonical, upgraded, addExhaust: true))
            .ToArray();
        if (options.Length == 0) return;

        CardModel? addedCard = null;
        try
        {
            var selected = await CardSelectCmd.FromChooseACardScreen(context, options, player);
            if (selected != null && DuanYuState.CanAct(player))
            {
                await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, player);
                addedCard = selected;
            }
        }
        finally
        {
            foreach (var option in options)
            {
                if (option != addedCard && option.Pile == null && combat.ContainsCard(option))
                    combat.RemoveCard(option);
            }
        }
    }

    private static IEnumerable<CardModel> SampleAttacks(Player player, int count)
    {
        if (count <= 0 || !DuanYuState.CanAct(player)) yield break;
        var candidates = GetAttackPool().ToList();
        var generationCount = Math.Min(count, candidates.Count);
        for (var index = 0; index < generationCount; index++)
        {
            if (!DuanYuState.CanAct(player)) yield break;
            var choice = player.RunState.Rng.CombatCardGeneration.NextInt(candidates.Count);
            var canonical = candidates[choice];
            candidates.RemoveAt(choice);
            yield return canonical;
        }
    }

    private static CardModel CreateBorrowedAttack(Player player, CardModel canonical, bool upgraded, bool addExhaust)
    {
        var card = player.Creature.CombatState!.CreateCard(canonical, player);
        if (upgraded) CardCmd.Upgrade(card);
        card.EnergyCost.SetThisTurnOrUntilPlayed(0);
        if (addExhaust) card.AddKeyword(CardKeyword.Exhaust);
        return card;
    }
}
