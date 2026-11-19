#if DEBUG
using DuanYu.Cards;
using DuanYu.Character;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.TestSupport;

namespace DuanYu.DebugTests;

internal static class ForeignPoolSelfTests
{
    internal static IEnumerable<(string Name, Func<Task> Action)> Cases
    {
        get
        {
            yield return ("foreign-pool:all-nonbasic-character-attacks-without-cost-or-resource-exclusions", CompleteSources);
            foreach (bool upgraded in new[] { false, true })
            {
                string suffix = upgraded ? "+" : "";
                foreach (var specification in new[]
                {
                    (Type: typeof(Bury), Damage: 52, DamageUp: 63),
                    (Type: typeof(Coda), Damage: 40, DamageUp: 55),
                    (Type: typeof(BansheesCry), Damage: 33, DamageUp: 33),
                    (Type: typeof(MeteorStrike), Damage: 24, DamageUp: 30)
                })
                    yield return ("foreign-pool:high-cost-native-play-" + specification.Type.Name + suffix,
                        () => HighCostAttack(specification.Type, upgraded ? specification.DamageUp : specification.Damage, upgraded));
                yield return ("foreign-pool:native-star-costs-and-resource-isolation" + suffix, () => StarCosts(upgraded));
                yield return ("foreign-pool:grand-finale-retains-empty-draw-pile-condition" + suffix, () => FinaleCondition(upgraded));
            }
        }
    }

    private static Task CompleteSources()
    {
        var pool = DuanYuForeignCards.GetAttackPool();
        var ids = pool.Select(card => card.Id).ToHashSet();
        EngineSelfTests.Equal(pool.Count, ids.Count, "shared attacks are sampled once per card model rather than once per character");
        EngineSelfTests.Equal(true, pool.All(card => card.Type == CardType.Attack && card is not DuanYuCard), "borrowed pool contains other-character attacks");
        EngineSelfTests.Equal(true, pool.All(card => card.Rarity != CardRarity.Basic), "borrowed pool excludes every Basic-rarity attack");
        foreach (var character in ModelDb.AllCharacters.Where(character => character is not DuanYuCharacter))
        {
            var attacks = character.CardPool.AllCards.Concat(character.StartingDeck).Where(card => card.Type == CardType.Attack && card is not DuanYuCard);
            EngineSelfTests.Equal(true, attacks.Where(card => card.Rarity != CardRarity.Basic).All(card => ids.Contains(card.Id)), character.Id.Entry + " contributes all its non-basic attacks");
            EngineSelfTests.Equal(true, attacks.Where(card => card.Rarity == CardRarity.Basic).All(card => !ids.Contains(card.Id)), character.Id.Entry + " contributes no basic attacks, including starting-deck attacks");
        }
        foreach (var type in new[]
        {
            typeof(Bury), typeof(MeteorStrike), typeof(Coda), typeof(BansheesCry), typeof(Midnight),
            typeof(Whirlwind), typeof(Skewer), typeof(AstralPulse),
            typeof(Squeeze), typeof(Feed), typeof(GrandFinale), typeof(Clash), typeof(Rebound), typeof(RipAndTear)
        })
            EngineSelfTests.Equal(true, ids.Contains(ModelDb.GetId(type)), type.Name + " remains eligible regardless of cost, non-basic rarity, resource, multiplayer or native generation flags");
        foreach (var type in new[] { typeof(Bash), typeof(FallingStar) })
        {
            EngineSelfTests.Equal(CardRarity.Basic, ModelDb.GetById<CardModel>(ModelDb.GetId(type)).Rarity, type.Name + " is a native Basic-rarity attack");
            EngineSelfTests.Equal(false, ids.Contains(ModelDb.GetId(type)), type.Name + " is excluded as a Basic-rarity attack");
        }
        EngineSelfTests.Equal(false, ModelDb.Card<Feed>().CanBeGeneratedInCombat, "coverage includes attacks the generic native random generator excludes");
        EngineSelfTests.Equal(CardMultiplayerConstraint.MultiplayerOnly, ModelDb.Card<Midnight>().MultiplayerConstraint, "coverage includes a multiplayer-only card even in a singleplayer fixture");
        var characterPools = ModelDb.AllCharacters.Where(character => character is not DuanYuCharacter).Select(character => character.CardPool.Id).ToHashSet();
        var associatedAttacks = ModelDb.AllCards.Where(card => card.Type == CardType.Attack && card.Rarity != CardRarity.Basic && characterPools.Contains(card.VisualCardPool.Id));
        EngineSelfTests.Equal(true, associatedAttacks.All(card => ids.Contains(card.Id)), "shared event attacks with another character's visual pool remain included");
        Godot.GD.Print($"DUANYU_FOREIGN_POOL count={pool.Count}");
        return Task.CompletedTask;
    }

    private static async Task HighCostAttack(Type attackType, int expectedDamage, bool upgraded)
    {
        var (fixture, card) = await BorrowAttack(attackType, upgraded);
        int beforeHp = fixture.Enemy.CurrentHp;
        await Play(fixture, card);
        EngineSelfTests.Equal(expectedDamage, beforeHp - fixture.Enemy.CurrentHp, attackType.Name + " executes its full native damage while its energy fee is temporarily zero");
        EngineSelfTests.Equal(PileType.Exhaust, card.Pile!.Type, "high-cost borrowed attacks exhaust after playing");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "foreign attacks do not borrow or replace the character's Qi");
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SwordPlays, "foreign attacks do not become sword tokens");
        if (card is MeteorStrike)
        {
            var queue = fixture.Player.PlayerCombatState!.OrbQueue;
            EngineSelfTests.Equal(1, queue.Capacity, "native channeling grants one orb slot to a character that started with none");
            EngineSelfTests.Equal(1, queue.Orbs.Count, "three plasma channels evoke the first two and retain the third in one native slot");
            EngineSelfTests.Equal(true, queue.Orbs.Single() is PlasmaOrb, "Meteor Strike retains its final native plasma orb");
            EngineSelfTests.Equal(4, fixture.Player.PlayerCombatState.Energy, "two native plasma evocations each grant two energy");
        }
        else
            EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState!.Energy, "borrowed fixed-cost attack is playable with no remaining energy");
    }

    private static async Task StarCosts(bool upgraded)
    {
        foreach (var specification in new[] { (Type: typeof(AstralPulse), Stars: 3) })
        {
            var (fixture, card) = await BorrowAttack(specification.Type, upgraded);
            EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState!.Stars, "generation grants no free native stars");
            EngineSelfTests.Equal(specification.Stars, card.GetStarCostWithModifiers(), "energy-free borrowing preserves the native star cost");
            EngineSelfTests.Equal(false, card.CanPlay(), "zero native stars cannot pay a borrowed star-cost attack");
            await DuanYuState.GainQi(fixture.Player, 9);
            EngineSelfTests.Equal(false, card.CanPlay(), "Qi never substitutes for the borrowed card's native stars");
            fixture.Player.PlayerCombatState.Stars = specification.Stars;
            EngineSelfTests.Equal(true, card.CanPlay(), "the attack becomes playable when its real native star cost is available");
            await Play(fixture, card);
            EngineSelfTests.Equal(0, fixture.Player.PlayerCombatState.Stars, "playing the borrowed attack pays its native star cost");
            EngineSelfTests.Equal(9, DuanYuState.Get(fixture.Player).Qi, "native star payment leaves independent Qi unchanged");
        }
    }

    private static async Task FinaleCondition(bool upgraded)
    {
        var (fixture, card) = await BorrowAttack(typeof(GrandFinale), upgraded);
        EngineSelfTests.Equal(false, card.CanPlay(), "generating Grand Finale does not bypass its nonempty draw-pile restriction");
        PileType.Draw.GetPile(fixture.Player).Clear();
        EngineSelfTests.Equal(true, card.CanPlay(), "Grand Finale is usable when its native empty draw-pile requirement is met");
        int beforeHp = fixture.Enemy.CurrentHp;
        await Play(fixture, card);
        EngineSelfTests.Equal(upgraded ? 75 : 60, beforeHp - fixture.Enemy.CurrentHp, "Grand Finale keeps its native payoff after satisfying the condition");
        EngineSelfTests.Equal(PileType.Exhaust, card.Pile!.Type, "borrowed Grand Finale receives the normal borrowed exhaust keyword");
    }

    private static async Task<(TestFixture Fixture, CardModel Card)> BorrowAttack(Type attackType, bool upgraded)
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        await DuanYuState.LoseQi(fixture.Player, int.MaxValue);
        fixture.Player.PlayerCombatState!.Stars = 0;
        await DuanYuForeignCards.GenerateAttacks(fixture.Player, new ThrowingPlayerChoiceContext(), int.MaxValue, upgraded);
        var card = CombatManager.Instance.History.Entries.OfType<CardGeneratedEntry>().Select(entry => entry.Card).Single(candidate => candidate.GetType() == attackType);
        foreach (var existing in PileType.Hand.GetPile(fixture.Player).Cards.ToArray())
            await CardPileCmd.Add(existing, PileType.Discard);
        await CardPileCmd.Add(card, PileType.Hand);
        fixture.Player.PlayerCombatState.Energy = 0;
        return (fixture, card);
    }

    private static async Task Play(TestFixture fixture, CardModel card)
    {
        EngineSelfTests.Equal(true, card.CanPlay(), card.Id.Entry + " satisfies its own native resource and play conditions");
        int energyValue = card.EnergyCost.GetAmountToSpend();
        var (energySpent, starsSpent) = await card.SpendResources();
        await card.OnPlayWrapper(new ThrowingPlayerChoiceContext(), card.TargetType == TargetType.AnyEnemy ? fixture.Enemy : null, false,
            new ResourceInfo { EnergySpent = energySpent, EnergyValue = energyValue, StarsSpent = starsSpent, StarValue = starsSpent }, true);
    }
}
#endif
