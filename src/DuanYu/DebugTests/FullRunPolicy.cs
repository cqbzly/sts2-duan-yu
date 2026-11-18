#if DEBUG
using DuanYu.Cards;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace DuanYu.DebugTests;

internal static class FullRunPolicy
{
    internal static (CardModel? Card, Creature? Target, double Score) ChoosePlay(Player player, CombatState state)
    {
        var choices = new List<(CardModel Card, Creature? Target, double Score)>();
        foreach (var card in PileType.Hand.GetPile(player).Cards.Where(card => card.CanPlay()))
        {
            foreach (var target in Targets(card, player, state))
            {
                if (!card.IsValidTarget(target)) continue;
                var score = CombatValue(card, target, player, state);
                choices.Add((card, target, score));
            }
        }
        var ordered = choices.OrderByDescending(choice => choice.Score)
            .ThenBy(choice => choice.Target is { IsEnemy: true } enemy ? enemy.CurrentHp : int.MaxValue)
            .ToArray();
        var best = ordered.FirstOrDefault();
        if (best.Card != null && best.Score > 0 && best.Card.EnergyCost.GetAmountToSpend() > 0)
        {
            var energyFirst = ordered.FirstOrDefault(choice => choice.Score > 0 && CanPrioritizeEnergyCard(choice.Card, player, state));
            if (energyFirst.Card != null) return energyFirst;
        }
        return best;
    }

    private static bool CanPrioritizeEnergyCard(CardModel card, Player player, CombatState state)
    {
        if (card is not DuanYuCard { DesignId: "DY_U02" or "DY_R04" } energyCard || !player.Creature.IsAlive) return false;
        var cost = Math.Max(0, card.EnergyCost.GetAmountToSpend());
        var gain = EnergyGain(energyCard, DuanYuState.Get(player));
        if (Hook.ModifyEnergyGain(state, player, gain, out _) <= cost) return false;
        if (card.Affliction is Bound or Galvanized or Ringing) return false;
        if (player.Creature.Powers.Any(power => power is SmoggyPower or TenderPower or SlothPower or EncorePower or RingingPower)) return false;
        if (PileType.Hand.GetPile(player).Cards.Any(other => other is Normality)) return false;
        if (player.Relics.Any(relic => !relic.IsMelted && relic.Id.Entry == "VELVET_CHOKER")) return false;
        if (state.Enemies.Where(enemy => enemy.IsAlive).SelectMany(enemy => enemy.Powers)
            .Any(power => power is EnragePower || power is WitheringPresencePower presence && presence.Target?.Player == player)) return false;
        return true;
    }

    internal static double CardValue(CardModel card, Player player)
    {
        if (card is DuanYuCard { DesignId: "DY_S02" } qiStatus && player.Creature.CombatState is CombatState statusCombat)
            return CombatValue(qiStatus, null, player, statusCombat);
        if (card.Type is CardType.Curse or CardType.Status) return -100;
        if (card is not DuanYuCard custom)
        {
            var raw = card.DynamicVars.Values.Sum(variable => variable is DamageVar or BlockVar ? (double)variable.BaseValue : variable.Name is "Cards" or "Draw" ? (double)variable.BaseValue * 4 : 0);
            return Math.Clamp(raw / Math.Max(1, card.EnergyCost.Canonical), 3, 22) + (card.Type == CardType.Power ? 5 : 0) + (card.IsUpgraded ? 2 : 0);
        }
        if (DuanYuState.IsSword(custom) && player.Creature.CombatState is CombatState combat)
            return Targets(custom, player, combat).Select(target => CombatValue(custom, target, player, combat)).DefaultIfEmpty(0).Max();
        double rating = custom.DesignId switch
        {
            "DY_B01" => 0, "DY_B02" => 2, "DY_B03" => 10, "DY_B04" => 9, "DY_B05" => 10,
            "DY_C01" => 11, "DY_C02" => 14, "DY_C03" => 14, "DY_C04" => 13, "DY_C05" => 15,
            "DY_C06" => 22, "DY_C07" => 26, "DY_C08" => 27, "DY_C09" => 14, "DY_C10" => 11,
            "DY_C11" => 11, "DY_C12" => 12, "DY_C13" => 23, "DY_C14" => 16, "DY_C15" => 20,
            "DY_C16" => 24, "DY_C17" => 17, "DY_C18" => 26, "DY_C19" => 17, "DY_C20" => 22,
            "DY_U01" => 13, "DY_U02" => 24, "DY_U03" => 17, "DY_U04" => 16, "DY_U05" => 17,
            "DY_U06" => 29, "DY_U07" => 30, "DY_U08" => 35, "DY_U09" => 13, "DY_U10" => 31,
            "DY_U11" => 27, "DY_U12" => 31, "DY_U13" => 16, "DY_U14" => 18, "DY_U15" => 21,
            "DY_U16" => 16, "DY_U17" => 23, "DY_U18" => 23, "DY_U19" => 7, "DY_U20" => 23,
            "DY_U21" => 26, "DY_U22" => 24, "DY_U23" => 13, "DY_U24" => 20, "DY_U25" => 13,
            "DY_U26" => 23, "DY_U27" => 30, "DY_U28" => 13, "DY_U29" => 10, "DY_U30" => 13,
            "DY_U31" => 24, "DY_U32" => 22, "DY_U33" => 35, "DY_U34" => 20, "DY_U35" => 23,
            "DY_R01" => 27, "DY_R02" => 25, "DY_R03" => 28, "DY_R04" => 38, "DY_R05" => 32,
            "DY_R06" => 32, "DY_R07" => 26, "DY_R08" => 23, "DY_R09" => 26, "DY_R10" => 28,
            "DY_R11" => 22, "DY_R12" => 34, "DY_R13" => 38, "DY_R14" => 25, "DY_R15" => 24,
            "DY_R16" => 23, "DY_R17" => 14, "DY_R18" => 22, "DY_R19" => 30, "DY_R20" => 38,
            "DY_T01" => 14, "DY_T02" => 17, "DY_T03" => 13, "DY_T04" => 16, "DY_T05" => 15, "DY_T06" => 18,
            _ => 8
        };
        var duplicates = player.Deck.Cards.Count(existing => existing.Id == custom.Id && !ReferenceEquals(existing, custom));
        var deck = player.Deck.Cards.OfType<DuanYuCard>().Where(existing => !ReferenceEquals(existing, custom)).ToArray();
        var swordSources = deck.Count(existing => GeneratedSwords(existing, 6) > 0);
        var qiSources = deck.Count(existing => QiGain(existing, 0) > 0);
        var spenders = deck.Count(existing => existing.QiCost > 0);
        var damageSources = deck.Sum(existing => existing.DesignId == "DY_R07" ? 2d : existing.Type == CardType.Attack && !existing.DesignId.StartsWith("DY_B", StringComparison.Ordinal) && existing.Numbers.Damage >= 5 ? 1d : GeneratedSwords(existing, 3) > 0 ? 0.6 : 0);
        var damageGap = Math.Max(0, Math.Ceiling(player.Deck.Cards.Count * 0.22) - damageSources);
        var defenders = deck.Count(existing => existing.Numbers.Block >= 8 || existing.DesignId is "DY_C11" or "DY_C15" or "DY_U20" or "DY_U34" or "DY_R02" or "DY_R12");
        var defenseGap = Math.Max(0, Math.Ceiling(player.Deck.Cards.Count * 0.18) - defenders);
        if (custom.Type == CardType.Attack && !custom.DesignId.StartsWith("DY_B", StringComparison.Ordinal) && custom.Numbers.Damage >= 5)
            rating += Math.Min(14, damageGap * (custom.Numbers.Damage >= 7 || custom.DesignId == "DY_C05" ? 4 : 3));
        if (custom.Numbers.Block >= 5 || custom.DesignId is "DY_R02" or "DY_R12") rating += Math.Min(6, defenseGap * 2);
        if (GeneratedSwords(custom, 3) > 0) rating += Math.Min(5, damageGap);
        if (custom.DesignId == "DY_U01") rating += Math.Min(9, spenders * 2) - damageGap * 2;
        if (custom.DesignId == "DY_R01") rating += Math.Min(10, spenders * 2) - Math.Max(0, 2 - spenders) * 5;
        if (custom.DesignId == "DY_R15") rating += Math.Min(8, spenders + swordSources * 2) - Math.Max(0, 2 - spenders - swordSources) * 4;
        if (custom.DesignId == "DY_U05") rating -= Math.Max(0, 3 - qiSources) * 3;
        if (custom.DesignId == "DY_U23") rating += Math.Min(6, deck.Count(existing => LingboGain(existing, 0) > 0 || existing.DesignId == "DY_R02") * 2);
        if (custom.DesignId == "DY_U28") rating += Math.Min(10, swordSources * 2 + deck.Count(existing => existing.DesignId is "DY_U06" or "DY_U35" or "DY_R16" or "DY_R19") * 2);
        if (custom.DesignId is "DY_U09" or "DY_U14" or "DY_R08" or "DY_R18") rating += swordSources == 0 ? -10 : Math.Min(10, swordSources * 2);
        if (custom.DesignId == "DY_R08") rating += deck.Any(existing => existing.DesignId is "DY_R03" or "DY_R10") ? 7 : swordSources < 3 ? -6 : 0;
        if (custom.Type == CardType.Power && custom.DesignId != "DY_R07") rating -= Math.Min(5, damageGap);
        if (custom.Type == CardType.Power) rating -= Math.Min(12, Math.Max(0, deck.Count(existing => existing.Type == CardType.Power) - Math.Max(1, deck.Length * 0.18)) * 3);
        return rating + (custom.IsUpgraded ? 3 : 0) - duplicates * (custom.Type == CardType.Power ? 7 : 2.5);
    }

    private static double CombatValue(CardModel card, Creature? target, Player player, CombatState combat, int? availableEnergy = null, double additionalBlock = 0, int? availableQi = null)
    {
        if (card is not DuanYuCard duanYuCard) return GenericCombatValue(card, target, player, combat);
        var state = DuanYuState.Get(player);
        var qi = availableQi ?? state.Qi;
        var exhaustTargets = ExhaustTargets(duanYuCard, player);
        var removedEndTurnDamage = exhaustTargets.Sum(removed => HandEndDamage(removed, player, combat));
        var incoming = Math.Max(0, IncomingDamage(player, combat) - removedEndTurnDamage);
        var incomingAfterFacing = Math.Max(0, IncomingDamage(player, combat, target) - removedEndTurnDamage);
        var passiveBlock = EndTurnBlock(player, combat, qi);
        var existingBlock = player.Creature.Block + additionalBlock;
        var neededBlock = Math.Max(0, incoming - existingBlock - passiveBlock);
        var cost = Math.Max(0, card.EnergyCost.GetAmountToSpend());
        var spendsQi = duanYuCard.QiCost > 0 && qi >= duanYuCard.QiCost;
        var swordCombination = TriggersSwordCombination(duanYuCard, state);
        var energyGain = EnergyGain(duanYuCard, state, qi);
        var energyAfter = (availableEnergy ?? player.PlayerCombatState!.Energy) - cost + energyGain;
        var block = ModifiedBlock(duanYuCard, player, combat, qi, state.Lingbo);
        if (duanYuCard.DesignId == "DY_U35") block *= exhaustTargets.Length;
        var exhaustCount = exhaustTargets.Length + (card.Keywords.Contains(CardKeyword.Exhaust) ? 1 : 0);
        block += ExhaustBlock(player, combat) * exhaustCount;
        if (DuanYuState.IsSword(card) && state.SwordPlays < 3)
            block += ModifiedPassiveBlock(player, combat, state.Effects.Where(effect => effect.CardId == "DY_R18").Sum(effect => effect.Amount));
        var attacks = combat.Enemies.Where(enemy => enemy.IsAlive).Sum(enemy => enemy.Monster!.NextMove.Intents.OfType<AttackIntent>().Sum(intent => intent.Repeats));
        var availableLingbo = Math.Max(0, attacks - state.Lingbo);
        block += Math.Min(availableLingbo, LingboGain(duanYuCard, state.Lingbo)) * DuanYuState.LingboReduction;
        if (card.Type == CardType.Skill && state.SkillsPlayed < 2)
            block += Math.Min(availableLingbo, state.Effects.Where(effect => effect.CardId == "DY_R02").Sum(effect => effect.Amount)) * DuanYuState.LingboReduction;
        var gainQi = QiGain(duanYuCard, qi);
        if (duanYuCard.DesignId == "DY_U35") gainQi += exhaustTargets.Length;
        if (duanYuCard.DesignId == "DY_R16") gainQi += exhaustTargets.Length * 2;
        if (spendsQi && state.SpendingCount < 2) gainQi += state.Effects.Where(effect => effect.CardId == "DY_R01").Sum(effect => effect.Amount);
        if (swordCombination) gainQi += state.Effects.Where(effect => effect.CardId == "DY_R08").Sum(effect => effect.SecondaryAmount);
        if (swordCombination && HasPendant(player)) gainQi += state.DistinctSwords.Count == 2 ? 2 : 3;
        gainQi = Math.Min(9 - qi + (spendsQi ? duanYuCard.QiCost : 0), gainQi);
        var qiAfter = qi - (spendsQi ? duanYuCard.QiCost : 0) + gainQi;
        if (card.Type == CardType.Skill && state.SkillsPlayed == 0 && HasPendant(player))
            qiAfter = Math.Min(9, qiAfter + 2);
        block += EndTurnBlock(player, combat, qiAfter) - passiveBlock;
        if (duanYuCard.DesignId == "DY_U05") block += ModifiedPassiveBlock(player, combat, qiAfter / 2 + duanYuCard.Value(0, 2));
        var score = TargetValue(card, target, player, combat, qi);
        var futureSwords = PileType.Hand.GetPile(player).Cards.Count(other => other != card && DuanYuState.IsSword(other));
        if (DuanYuState.IsSword(card) && state.SwordMomentum < DuanYuState.SwordMomentumCap)
            score += Math.Min(DuanYuState.SwordMomentumCap - state.SwordMomentum, 1 + (swordCombination ? 1 : 0)) * (1 + Math.Min(4, futureSwords));
        var momentumGain = duanYuCard.DesignId switch { "DY_U26" => duanYuCard.Value(1, 2), "DY_R14" => duanYuCard.Value(2, 3), "DY_S02" => 2, _ => 0 };
        score += Math.Min(DuanYuState.SwordMomentumCap - state.SwordMomentum, momentumGain) * (1 + Math.Min(4, futureSwords));
        score += exhaustTargets.Sum(removed => ExhaustOpportunity(removed, player));
        var blockWeight = player.Creature.CurrentHp <= neededBlock ? 5 : player.Creature.CurrentHp < player.Creature.MaxHp * 0.35 ? 2.7 : 2;
        score += removedEndTurnDamage * blockWeight;
        score += (neededBlock - Math.Max(0, incomingAfterFacing - existingBlock - passiveBlock - block)) * blockWeight;
        if (block >= neededBlock && neededBlock > 0) score += 1;
        if (block > 0 && combat.Enemies.Any(enemy => enemy.Monster!.Id.Entry == "BOWLBUG_ROCK" && enemy.Monster.IntendsToAttack))
        {
            var rock = combat.Enemies.First(enemy => enemy.Monster!.Id.Entry == "BOWLBUG_ROCK");
            var rockDamage = rock.Monster!.NextMove.Intents.OfType<AttackIntent>().Sum(intent => intent.GetTotalDamage([player.Creature], rock));
            if (existingBlock < rockDamage && existingBlock + block >= rockDamage) score += 13;
        }
        var waitingSpender = PileType.Hand.GetPile(player).Cards.OfType<DuanYuCard>().Any(other => other != card && other.QiCost > qi && other.QiCost <= qiAfter && other.EnergyCost.GetAmountToSpend() <= energyAfter);
        score += gainQi * (waitingSpender ? 4.5 : qi < 5 ? 1.5 : 0.35);
        if (card.Type == CardType.Skill && state.SkillsPlayed == 0 && qi < 9) score += 1.5;
        var draws = DrawAmount(duanYuCard);
        if (duanYuCard.DesignId == "DY_C17" && qi < 1 || duanYuCard.DesignId == "DY_U16" && state.SwordPlays == 0) draws = 0;
        if (duanYuCard.DesignId == "DY_C20" && target?.GetPowerAmount<WeakPower>() > 0) draws = 1;
        if (duanYuCard.DesignId == "DY_U26" && qi >= 5) draws++;
        if (duanYuCard.DesignId == "DY_R16") draws += exhaustTargets.Length;
        if (spendsQi && state.SpendingCount == 0) draws += state.Effects.Where(effect => effect.CardId == "DY_U01").Sum(effect => effect.Amount);
        if (swordCombination) draws += state.Effects.Where(effect => effect.CardId == "DY_R08").Sum(effect => effect.Amount);
        if (swordCombination && state.DistinctSwords.Count == 2 && HasPendant(player)) draws++;
        score += draws * (energyAfter >= 1 ? 4 : 0.7);
        var energyDemand = PileType.Hand.GetPile(player).Cards.Where(other => other != card && !other.Keywords.Contains(CardKeyword.Unplayable)).Sum(other => Math.Max(0, other.EnergyCost.GetAmountToSpend())) + draws;
        var usefulEnergy = Math.Min(energyGain, Math.Max(0, energyDemand - Math.Max(0, energyAfter - energyGain)));
        score += usefulEnergy * 7 + (energyGain - usefulEnergy) * 0.5;
        score += GeneratedSwordValue(duanYuCard, player, combat, energyAfter, additionalBlock + Math.Max(0, block), qiAfter);
        if (duanYuCard.DesignId == "DY_U27") score += ForeignAttackValue(duanYuCard, player, combat);
        if (duanYuCard.DesignId == "DY_U09") score += duanYuCard.Value(3, 5) * PileType.Hand.GetPile(player).Cards.Count(DuanYuState.IsSword);
        if (card.Type == CardType.Power)
        {
            var health = combat.Enemies.Where(enemy => enemy.IsAlive).Sum(enemy => enemy.CurrentHp);
            var survival = Math.Clamp((player.Creature.CurrentHp - Math.Max(0, neededBlock - block)) / Math.Max(12, player.Creature.MaxHp * 0.45), 0.05, 1);
            score += Math.Clamp(health / 45d, 0.6, 3) * AbilityValue(duanYuCard, player) * survival;
        }
        if (duanYuCard.DesignId is "DY_C05" or "DY_U20" or "DY_R20" or "DY_T06")
        {
            var weakened = duanYuCard.DesignId == "DY_R20" ? combat.Enemies.Where(enemy => enemy.IsAlive) : target is { IsEnemy: true } ? [target] : Enumerable.Empty<Creature>();
            var prevented = weakened.Where(enemy => enemy.GetPowerAmount<WeakPower>() == 0 && enemy.GetPowerAmount<ArtifactPower>() == 0)
                .Sum(enemy => enemy.Monster!.NextMove.Intents.OfType<AttackIntent>().Sum(intent => intent.GetTotalDamage([player.Creature], enemy)) * 0.25);
            score += Math.Min(Math.Max(0, neededBlock - Math.Max(0, block)), prevented) * blockWeight;
        }
        if ((duanYuCard.DesignId is "DY_C03" or "DY_T02" || duanYuCard.DesignId is ("DY_U32" or "DY_R06") && qi >= duanYuCard.QiCost) && (target?.GetPowerAmount<VulnerablePower>() ?? 0) == 0 && (target?.GetPowerAmount<ArtifactPower>() ?? 0) == 0)
            score += PileType.Hand.GetPile(player).Cards.Count(other => other != card && other.Type == CardType.Attack && other.EnergyCost.GetAmountToSpend() <= energyAfter) * 3;
        if (duanYuCard.DesignId == "DY_R14") score -= Math.Min(Math.Max(0, incomingAfterFacing - passiveBlock), player.Creature.Block) * blockWeight;
        if (duanYuCard.DesignId == "DY_U04") score -= duanYuCard.Value(3, 2) * 1.8;
        if (duanYuCard.DesignId == "DY_R17") score -= duanYuCard.Value(4, 3) * 1.8;
        if (duanYuCard.DesignId == "DY_U04" && player.Creature.CurrentHp <= duanYuCard.Value(3, 2) || duanYuCard.DesignId == "DY_R17" && player.Creature.CurrentHp <= duanYuCard.Value(4, 3)) return -1000;
        if (duanYuCard.DesignId == "DY_U25") score += 4;
        if (duanYuCard.DesignId == "DY_U34") score += 3;
        if (duanYuCard.DesignId == "DY_C16") score += 2;
        if (duanYuCard.DesignId == "DY_R16") score += player.Creature.GetPowerAmount<PoisonPower>() * 2;
        if (duanYuCard.DesignId == "DY_U08") score -= 2;
        return score / Math.Max(1, cost);
    }

    internal static double RemovalValue(CardModel card, Player player)
    {
        if (card.Keywords.Contains(CardKeyword.Eternal)) return -100;
        if (card.Type == CardType.Curse) return 100;
        if (card is not DuanYuCard duanYuCard) return 10 - CardValue(card, player);
        return duanYuCard.DesignId switch
        {
            "DY_B01" => 30 - (card.IsUpgraded ? 6 : 0),
            "DY_B02" => 23 - (card.IsUpgraded ? 7 : 0),
            "DY_B03" or "DY_B04" or "DY_B05" => 7,
            _ when DuanYuState.IsSword(duanYuCard) => 1,
            _ => 15 - CardValue(card, player)
        };
    }

    internal static double ExhaustPriority(CardModel card, Player player, DuanYuCard? source)
    {
        var value = ExhaustOpportunity(card, player);
        if (player.Creature.CombatState is not CombatState combat) return value;
        var neededBlock = Math.Max(0, IncomingDamage(player, combat) - player.Creature.Block - EndTurnBlock(player, combat, DuanYuState.Get(player).Qi));
        var block = ExhaustBlock(player, combat);
        if (source is { DesignId: "DY_U35" })
        {
            block += ModifiedBlock(source, player, combat, DuanYuState.Get(player).Qi, DuanYuState.Get(player).Lingbo);
            if (DuanYuState.Get(player).Qi < 9) value += 1.5;
        }
        return value + Math.Min(neededBlock, block) * (player.Creature.CurrentHp <= neededBlock ? 5 : 2) + HandEndDamage(card, player, combat) * 2;
    }

    private static CardModel[] ExhaustTargets(DuanYuCard source, Player player)
    {
        var cards = PileType.Hand.GetPile(player).Cards.Where(card => !ReferenceEquals(card, source)).ToArray();
        return source.DesignId switch
        {
            "DY_U06" => cards.OrderByDescending(card => ExhaustPriority(card, player, source)).Take(1).ToArray(),
            "DY_U35" => cards.Where(card => ExhaustPriority(card, player, source) > 0).OrderByDescending(card => ExhaustPriority(card, player, source)).Take(source.Value(3, 4)).ToArray(),
            "DY_R16" => cards.Where(card => card.Type == CardType.Status).ToArray(),
            "DY_R19" => cards,
            _ => []
        };
    }

    private static double ExhaustOpportunity(CardModel card, Player player)
    {
        var cleanup = card.Type == CardType.Curse ? 12d : card.Type == CardType.Status ? 8d : card.Rarity == CardRarity.Basic ? 2d : 0d;
        if (card is DuanYuCard { DesignId: "DY_S02" }) cleanup = 0;
        if (card.Keywords.Contains(CardKeyword.Unplayable)) return cleanup;
        if (player.Creature.CombatState is not CombatState combat) return cleanup - Math.Max(0, CardValue(card, player)) * 0.4;
        if (card.EnergyCost.GetAmountToSpend() > player.PlayerCombatState!.Energy) return cleanup - 2;
        var value = card is DuanYuCard custom
            ? Targets(custom, player, combat).Select(target => TargetValue(custom, target, player, combat)).DefaultIfEmpty(0).Max()
              + Math.Min(Math.Max(0, IncomingDamage(player, combat) - player.Creature.Block), ModifiedBlock(custom, player, combat, DuanYuState.Get(player).Qi, DuanYuState.Get(player).Lingbo)) * 2
              + DrawAmount(custom) * 3 + GeneratedSwords(custom, DuanYuState.Get(player).Qi) * 7
              + EnergyGain(custom, DuanYuState.Get(player)) * 5 + QiGain(custom, DuanYuState.Get(player).Qi)
              + (custom.Type == CardType.Power ? AbilityValue(custom, player) : 0)
            : Targets(card, player, combat).Select(target => GenericCombatValue(card, target, player, combat)).DefaultIfEmpty(0).Max();
        return cleanup - Math.Max(0, value) * 0.7;
    }

    private static double HandEndDamage(CardModel card, Player player, CombatState combat)
        => card is Wither or Burn or Toxic or Decay or Infection ? TurnEndCardDamage(card, player, combat) : 0;

    private static double ExhaustBlock(Player player, CombatState combat)
        => DuanYuState.Get(player).Effects.Where(effect => effect.CardId == "DY_U28").Sum(effect => ModifiedPassiveBlock(player, combat, effect.Amount));

    internal static double IncomingDamage(Player player, CombatState state, Creature? facingTarget = null, CardModel? removedHandCard = null)
    {
        var total = PileType.Hand.GetPile(player).Cards
            .Where(card => !ReferenceEquals(card, removedHandCard) && card is Wither or Burn or Toxic or Decay or Infection)
            .Sum(card => TurnEndCardDamage(card, player, state));
        var lingbo = DuanYuState.Get(player).Lingbo;
        var surrounded = player.Creature.GetPower<SurroundedPower>();
        var facing = surrounded?.Facing;
        if (surrounded != null && facingTarget?.HasPower<BackAttackLeftPower>() == true) facing = SurroundedPower.Direction.Left;
        if (surrounded != null && facingTarget?.HasPower<BackAttackRightPower>() == true) facing = SurroundedPower.Direction.Right;
        foreach (var enemy in state.Enemies.Where(enemy => enemy.IsAlive))
        {
            foreach (var intent in enemy.Monster!.NextMove.Intents.OfType<AttackIntent>())
            {
                var damage = Hook.ModifyDamage(player.RunState, state, player.Creature, enemy, intent.DamageCalc?.Invoke() ?? 0,
                    ValueProp.Move, null, null, ModifyDamageHookType.All, CardPreviewMode.Normal, out var modifiers);
                var reducedByLingbo = modifiers.Any(modifier => modifier is LingboPower);
                if (surrounded != null && facing != surrounded.Facing)
                {
                    var currentBackAttack = surrounded.Facing == SurroundedPower.Direction.Right ? enemy.HasPower<BackAttackLeftPower>() : enemy.HasPower<BackAttackRightPower>();
                    var futureBackAttack = facing == SurroundedPower.Direction.Right ? enemy.HasPower<BackAttackLeftPower>() : enemy.HasPower<BackAttackRightPower>();
                    var beforeLingbo = damage + (reducedByLingbo ? DuanYuState.LingboReduction : 0);
                    damage = Math.Max(0, beforeLingbo / (currentBackAttack ? 1.5m : 1m) * (futureBackAttack ? 1.5m : 1m) - (reducedByLingbo ? DuanYuState.LingboReduction : 0));
                }
                for (var hit = 0; hit < intent.Repeats; hit++)
                {
                    var preview = (double)decimal.Floor(damage);
                    if (reducedByLingbo && lingbo <= 0) preview += DuanYuState.LingboReduction;
                    if (reducedByLingbo && lingbo > 0) lingbo--;
                    total += preview;
                }
            }
        }
        return total;
    }

    private static double TurnEndCardDamage(CardModel card, Player player, CombatState state)
    {
        var damage = card.DynamicVars.Damage;
        if (damage.Props.HasFlag(ValueProp.Unblockable)) return 0;
        return Math.Max(0, (double)decimal.Floor(Hook.ModifyDamage(player.RunState, state, player.Creature, player.Creature,
            damage.BaseValue, damage.Props, card, null, ModifyDamageHookType.All, CardPreviewMode.Normal, out _)));
    }

    private static IEnumerable<Creature?> Targets(CardModel card, Player player, CombatState state)
    {
        if (card.TargetType == TargetType.AnyEnemy)
            return state.Enemies.Where(enemy => enemy.IsAlive).Cast<Creature?>().ToArray();
        if (card.TargetType == TargetType.AnyPlayer)
            return [player.Creature];
        return [null];
    }

    private static double TargetValue(CardModel card, Creature? target, Player player, CombatState state, int? availableQi = null)
    {
        if (card is not DuanYuCard duanYuCard) return 0;
        var qi = availableQi ?? DuanYuState.Get(player).Qi;
        var segments = DamageSegments(duanYuCard, qi).ToArray();
        if (segments.Length == 0) return 0;
        var score = 0d;
        foreach (var enemy in state.Enemies.Where(enemy => enemy.IsAlive))
        {
            var block = duanYuCard.DesignId == "DY_R06" && enemy == target ? 0 : enemy.Block;
            var remainingHp = (double)enemy.CurrentHp;
            var slippery = enemy.GetPowerAmount<SlipperyPower>();
            var enemyValue = 0d;
            foreach (var (amount, allEnemies) in segments)
            {
                if (!allEnemies && enemy != target) continue;
                var damage = (double)decimal.Floor(Hook.ModifyDamage(player.RunState, state, enemy, player.Creature, amount,
                    ValueProp.Move, card, null, ModifyDamageHookType.All, CardPreviewMode.Normal, out _));
                var hpLoss = Math.Min(remainingHp, Math.Max(0, damage - block));
                if (slippery > 0 && hpLoss > 0)
                {
                    hpLoss = Math.Min(1, hpLoss);
                    slippery--;
                    enemyValue += 5;
                }
                var blocked = Math.Min(block, damage);
                block = Math.Max(0, block - (int)damage);
                remainingHp -= hpLoss;
                enemyValue += hpLoss + blocked * 0.35;
                enemyValue -= enemy.GetPowerAmount<ThornsPower>() * 1.5;
                if (remainingHp <= 0)
                {
                    var threat = enemy.Monster!.NextMove.Intents.OfType<AttackIntent>().Sum(intent => intent.GetTotalDamage([player.Creature], enemy));
                    enemyValue += 24 + threat * 1.5;
                    break;
                }
            }
            if (enemy.Monster!.Id.Entry == "BOWLBUG_ROCK" && enemyValue > 0) enemyValue += 2;
            score += enemyValue;
        }
        return score;
    }

    private static IEnumerable<(decimal Damage, bool AllEnemies)> DamageSegments(DuanYuCard card, int qi)
    {
        if (card.Numbers.Damage <= 0) yield break;
        var damage = card.DesignId == "DY_U08" ? card.DynamicVars.CalculationBase.BaseValue + qi * 2 : card.DynamicVars.Damage.BaseValue;
        var allEnemies = card.TargetType == TargetType.AllEnemies;
        yield return (damage, allEnemies);
        if (card.DesignId == "DY_U33") yield return (damage, false);
        if (card.QiCost > 0 && qi >= card.QiCost)
        {
            if (card.DesignId is "DY_B05" or "DY_C02" or "DY_U07" or "DY_U33") yield return (damage, false);
            if (card.DesignId is "DY_U17" or "DY_R05" or "DY_R13" or "DY_T01" or "DY_T03")
                yield return (card.DynamicVars["ExtraDamage"].BaseValue, card.DesignId is "DY_U17" or "DY_R05" or "DY_T03");
        }
    }

    private static double ModifiedBlock(DuanYuCard card, Player player, CombatState state, int qi, int lingbo)
    {
        if (card.Numbers.Block <= 0 || card.DesignId == "DY_C06" && lingbo == 0) return 0;
        var block = (double)decimal.Floor(Hook.ModifyBlock(state, player.Creature, card.DynamicVars.Block.BaseValue, ValueProp.Move, card, null, out _));
        if (card.DesignId == "DY_C15" && qi >= 2 || card.DesignId == "DY_R20" && qi >= 3) block *= 2;
        return block;
    }

    private static double EndTurnBlock(Player player, CombatState combat, int qi)
    {
        return DuanYuState.Get(player).Effects.Sum(effect => effect.CardId switch
        {
            "DY_U05" => ModifiedPassiveBlock(player, combat, qi / 2 * effect.Amount + effect.SecondaryAmount),
            _ => 0d
        });
    }

    private static double ModifiedPassiveBlock(Player player, CombatState combat, int amount)
        => amount <= 0 ? 0 : (double)decimal.Floor(Hook.ModifyBlock(combat, player.Creature, amount, ValueProp.Move, null, null, out _));

    private static double GenericCombatValue(CardModel card, Creature? target, Player player, CombatState state)
    {
        if (card.Id.Entry == "FRANTIC_ESCAPE")
        {
            var sandpit = state.Enemies.Where(enemy => enemy.IsAlive).SelectMany(enemy => enemy.Powers.OfType<SandpitPower>())
                .FirstOrDefault(power => power.Target == player.Creature);
            if (sandpit == null) return 0;
            var urgency = sandpit.Amount switch { <= 1 => 2000, 2 => 60, 3 => 18, 4 => 7, _ => 2 };
            return urgency / Math.Max(1d, card.EnergyCost.GetAmountToSpend());
        }
        var score = 0d;
        var energyAfter = player.PlayerCombatState!.Energy - card.EnergyCost.GetAmountToSpend();
        var currentNeededBlock = Math.Max(0, IncomingDamage(player, state) - player.Creature.Block);
        var neededBlock = Math.Max(0, IncomingDamage(player, state, target, card) - player.Creature.Block);
        score += (currentNeededBlock - neededBlock) * (player.Creature.CurrentHp <= currentNeededBlock ? 5 : 2);
        var randomTarget = card.TargetType == TargetType.RandomEnemy;
        var targets = card.TargetType is TargetType.AllEnemies or TargetType.RandomEnemy ? state.Enemies.Where(enemy => enemy.IsAlive).ToArray() : target is { IsEnemy: true } ? [target] : Array.Empty<Creature>();
        var hits = card.DynamicVars.TryGetValue("HitCount", out var hitCount) ? Math.Max(1, (int)hitCount.BaseValue) : 1;
        foreach (var variable in card.DynamicVars.Values)
        {
            if (variable is DamageVar damage)
            {
                foreach (var enemy in targets)
                {
                    var total = (double)decimal.Floor(Hook.ModifyDamage(player.RunState, state, enemy, player.Creature, damage.BaseValue, damage.Props,
                        card, null, ModifyDamageHookType.All, CardPreviewMode.Normal, out _)) * hits;
                    var damageValue = Math.Min(enemy.CurrentHp, Math.Max(0, total - enemy.Block)) + Math.Min(enemy.Block, total) * 0.35;
                    if (total >= enemy.CurrentHp + enemy.Block) damageValue += 25;
                    score += damageValue / (randomTarget ? Math.Max(1, targets.Length) : 1);
                }
            }
            else if (variable is BlockVar block)
                score += Math.Min(neededBlock, (double)Hook.ModifyBlock(state, player.Creature, block.BaseValue, block.Props, card, null, out _)) * (player.Creature.CurrentHp <= neededBlock ? 5 : 2);
            else if (variable.Name is "Cards" or "Draw") score += (double)variable.BaseValue * (energyAfter > 0 ? 4 : 0.5);
            else if (variable.Name == "Energy") score += (double)variable.BaseValue * 8;
            else if (variable.Name == "Vulnerable" && target is { IsEnemy: true } vulnerableTarget && vulnerableTarget.GetPowerAmount<VulnerablePower>() == 0 && vulnerableTarget.GetPowerAmount<ArtifactPower>() == 0)
                score += PileType.Hand.GetPile(player).Cards.Count(other => !ReferenceEquals(other, card) && other.Type == CardType.Attack && other.EnergyCost.GetAmountToSpend() <= energyAfter) * 3;
            else if (variable.Name == "Weak" && target is { IsEnemy: true } weakTarget && weakTarget.Monster!.IntendsToAttack && weakTarget.GetPowerAmount<WeakPower>() == 0 && weakTarget.GetPowerAmount<ArtifactPower>() == 0)
                score += Math.Min(neededBlock, weakTarget.Monster.NextMove.Intents.OfType<AttackIntent>().Sum(intent => intent.GetTotalDamage([player.Creature], weakTarget)) * 0.25) * 2;
            else if (variable.Name == "Poison" && target is { IsEnemy: true } poisonTarget && poisonTarget.GetPowerAmount<ArtifactPower>() == 0)
                score += Math.Min(poisonTarget.CurrentHp, (double)variable.BaseValue * 2);
            else if (variable.Name.Contains("Strength", StringComparison.Ordinal) || variable.Name.Contains("Dexterity", StringComparison.Ordinal)) score += (double)variable.BaseValue * 6;
        }
        if (card.Type == CardType.Power) score += 15;
        if (card.Keywords.Contains(CardKeyword.Exhaust)) score += Math.Min(neededBlock, ExhaustBlock(player, state)) * (player.Creature.CurrentHp <= neededBlock ? 5 : 2);
        return score / Math.Max(1, card.EnergyCost.GetAmountToSpend());
    }

    private static int GeneratedSwords(DuanYuCard card, int qi) => card.DesignId switch
    {
        "DY_C07" or "DY_C08" or "DY_C18" or "DY_C19" or "DY_U10" or "DY_U11" or "DY_U12" or "DY_R07" or "DY_R09" => 1,
        "DY_U13" or "DY_U15" => 2,
        "DY_R10" or "DY_R19" => 3,
        "DY_R03" when qi >= 3 => 6,
        _ => 0
    };

    private static double GeneratedSwordValue(DuanYuCard card, Player player, CombatState combat, int energyAfter, double additionalBlock, int qiAfter)
    {
        var state = DuanYuState.Get(player);
        var hand = PileType.Hand.GetPile(player).Cards;
        var exhausted = card.DesignId == "DY_R19" ? hand.Count(other => !ReferenceEquals(other, card)) : 0;
        var availableSlots = Math.Max(0, CardPile.MaxCardsInHand - hand.Count + (hand.Contains(card) ? 1 : 0) + exhausted);
        var count = Math.Min(Math.Min(card.DesignId == "DY_R19" ? exhausted : GeneratedSwords(card, state.Qi), availableSlots), qiAfter);
        if (count == 0) return 0;
        int[] numbers = card.DesignId switch
        {
            "DY_C07" => [1],
            "DY_C08" => [2],
            "DY_U10" => [3],
            "DY_C18" => [4],
            "DY_U11" => [5],
            "DY_U12" => [6],
            _ => [1, 2, 3, 4, 5, 6]
        };
        var upgraded = card.DesignId == "DY_R09" || card.IsUpgraded && card.DesignId is "DY_C08" or "DY_C19" or "DY_U10" or "DY_U12" or "DY_U13" or "DY_U15" or "DY_R03" or "DY_R07" or "DY_R10" or "DY_R19";
        var options = numbers.Select(number =>
        {
            var sword = (DuanYuCard)DuanYuCard.Canonical($"DY_T{number:00}").ToMutable();
            sword.Owner = player;
            if (upgraded) sword.UpgradeInternal();
            return sword;
        }).ToList();
        return count * options.Average(sword => Targets(sword, player, combat).Where(sword.IsValidTarget)
            .Select(target => Math.Clamp(CombatValue(sword, target, player, combat, energyAfter, additionalBlock, qiAfter), 0, 30)).DefaultIfEmpty(0).Max());
    }

    private static double ForeignAttackValue(DuanYuCard source, Player player, CombatState combat)
    {
        var hand = PileType.Hand.GetPile(player).Cards;
        var slots = Math.Max(0, CardPile.MaxCardsInHand - hand.Count + (hand.Contains(source) ? 1 : 0));
        var candidates = new CardModel[]
        {
            ModelDb.Card<Bash>(), ModelDb.Card<IronWave>(), ModelDb.Card<PommelStrike>(), ModelDb.Card<TwinStrike>(),
            ModelDb.Card<DaggerSpray>(), ModelDb.Card<DaggerThrow>(), ModelDb.Card<Slice>(), ModelDb.Card<PoisonedStab>(),
            ModelDb.Card<BeamCell>(), ModelDb.Card<GoForTheEyes>(), ModelDb.Card<SweepingBeam>(), ModelDb.Card<RipAndTear>()
        };
        var average = candidates.Average(canonical =>
        {
            var generated = (CardModel)canonical.ToMutable();
            generated.Owner = player;
            if (source.IsUpgraded) generated.UpgradeInternal();
            generated.EnergyCost.SetThisTurnOrUntilPlayed(0);
            generated.AddKeyword(CardKeyword.Exhaust);
            return Targets(generated, player, combat).Where(generated.IsValidTarget)
                .Select(target => Math.Max(0, GenericCombatValue(generated, target, player, combat))).DefaultIfEmpty(0).Max();
        });
        return Math.Min(2, slots) * average;
    }

    private static bool TriggersSwordCombination(DuanYuCard card, PlayerRuntimeState state) =>
        DuanYuState.IsSword(card) && state.DistinctSwords.Count is 2 or 5 && !state.DistinctSwords.Contains(card.DesignId);

    private static bool HasPendant(Player player) => player.Relics.Any(relic => !relic.IsMelted && relic.Id.Entry == "CQBZLY_DY_RELIC01");

    private static int EnergyGain(DuanYuCard card, PlayerRuntimeState state, int? availableQi = null)
    {
        var qi = availableQi ?? state.Qi;
        var energy = card.DesignId switch
        {
            "DY_U02" => 2,
            "DY_R04" => card.Value(2, 3),
            "DY_U04" => 1,
            "DY_R14" => 1,
            "DY_C14" when qi >= 3 => 1,
            _ => 0
        };
        if (card.QiCost > 0 && qi >= card.QiCost && state.SpendingCount == 0)
            energy += state.Effects.Where(effect => effect.CardId == "DY_R01").Sum(effect => effect.EnergyAmount);
        if (DuanYuState.IsSword(card) && state.SwordPlays == 0)
            energy += state.Effects.Where(effect => effect.CardId == "DY_R15").Sum(effect => effect.EnergyAmount);
        if (TriggersSwordCombination(card, state))
            energy += state.Effects.Where(effect => effect.CardId == "DY_R08").Sum(effect => effect.EnergyAmount);
        if (HasPendant(card.Owner))
        {
            if (card.Type == CardType.Skill && state.SkillsPlayed == 0) energy++;
            if (TriggersSwordCombination(card, state) && state.DistinctSwords.Count == 5) energy++;
        }
        return energy;
    }

    private static int QiGain(DuanYuCard card, int qi) => card.DesignId switch
    {
        "DY_B03" => Math.Min(9 - qi, card.Value(3, 4)),
        "DY_C01" or "DY_U01" => Math.Min(9 - qi, 2),
        "DY_C09" => Math.Min(9 - qi, 3),
        "DY_C19" => Math.Min(9 - qi, 1),
        "DY_C10" => Math.Min(9 - qi, card.IsUpgraded ? 3 : 2),
        "DY_C16" => 0,
        "DY_U02" => Math.Min(9 - qi, card.IsUpgraded ? 6 : 4),
        "DY_U03" => Math.Min(9 - qi, card.IsUpgraded ? 5 : 4),
        "DY_U04" => Math.Min(9 - qi, card.IsUpgraded ? 4 : 3),
        "DY_U09" => Math.Min(9 - qi, card.IsUpgraded ? 2 : 1),
        "DY_U22" => Math.Min(9 - qi, 1),
        "DY_U06" or "DY_U26" => Math.Min(9 - qi, card.Value(2, 3)),
        "DY_R14" => Math.Min(9 - qi, card.Value(3, 4)),
        "DY_S02" => Math.Min(9 - qi, 3),
        "DY_U10" => Math.Min(9 - qi, 1),
        "DY_U29" => Math.Min(9 - qi, card.Value(1, 2)),
        "DY_R04" => Math.Min(9 - qi, card.IsUpgraded ? 8 : 6),
        _ => 0
    };

    private static int LingboGain(DuanYuCard card, int lingbo) => card.DesignId switch
    {
        "DY_B04" or "DY_U34" => Math.Min(3 - lingbo, 1),
        "DY_C11" or "DY_C12" or "DY_U18" or "DY_U19" or "DY_U25" => Math.Min(3 - lingbo, card.IsUpgraded ? 2 : 1),
        "DY_U21" => Math.Min(3 - lingbo, 2),
        "DY_R11" => Math.Min(3 - lingbo, 3),
        _ => 0
    };

    private static int DrawAmount(DuanYuCard card) => card.DesignId switch
    {
        "DY_C08" or "DY_C13" or "DY_U16" or "DY_U30" or "DY_T05" or "DY_S02" => 1,
        "DY_C17" or "DY_U21" => card.IsUpgraded ? 2 : 1,
        "DY_U24" => 2,
        "DY_C14" or "DY_U26" or "DY_R04" => card.IsUpgraded ? 3 : 2,
        "DY_U06" or "DY_R14" => card.IsUpgraded ? 4 : 3,
        _ => 0
    };

    private static double AbilityValue(DuanYuCard card, Player player)
    {
        var deck = player.Deck.Cards.OfType<DuanYuCard>().ToArray();
        var swordSources = deck.Count(existing => GeneratedSwords(existing, 6) > 0);
        var spenders = deck.Count(existing => existing.QiCost > 0);
        return card.DesignId switch
        {
            "DY_U01" => Math.Min(9, spenders * 2),
            "DY_U05" => card.IsUpgraded ? 11 : 7,
            "DY_U23" => Math.Min(8, deck.Count(existing => LingboGain(existing, 0) > 0 || existing.DesignId == "DY_R02") * 2),
            "DY_R02" => 8,
            "DY_R08" => deck.Any(existing => existing.DesignId is "DY_R03" or "DY_R10") ? card.Value(12, 15) : Math.Max(0, Math.Min(12, (swordSources - 1) * 3)),
            "DY_R12" => 14,
            "DY_R15" => Math.Min(10, spenders * 2 + swordSources * 3),
            "DY_R18" => Math.Min(18, swordSources * card.Value(5, 7)),
            "DY_U14" => Math.Min(18, swordSources * card.Value(3, 4)),
            "DY_R01" => Math.Min(12, spenders * 3 + swordSources),
            "DY_R07" => card.Value(12, 15),
            "DY_U28" => Math.Min(24, (swordSources * 2 + deck.Count(existing => existing.Keywords.Contains(CardKeyword.Exhaust) || existing.DesignId is "DY_U06" or "DY_R19")) * card.Value(2, 3)),
            _ => 0
        };
    }
}
#endif
