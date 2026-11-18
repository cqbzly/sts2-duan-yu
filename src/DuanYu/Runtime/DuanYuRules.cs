namespace DuanYu.Runtime;

public readonly record struct DuanYuPlayOrdinal(int Skill, int Sword, bool ThirdSwordKind);
public readonly record struct DuanYuAttackReduction(decimal Damage, bool ConsumesLingbo);

public sealed class DuanYuRunHistory
{
    public HashSet<string> Relics { get; set; } = [];
    public HashSet<string> Events { get; set; } = [];
}

public class DuanYuCombatLedger
{
    public int Qi { get; internal set; }
    public int Lingbo { get; internal set; }
    public int SkillsPlayed { get; internal set; }
    public int AttacksPlayed { get; internal set; }
    public int SwordPlays { get; internal set; }
    public HashSet<string> DistinctSwords { get; } = [];
    public int SpendingCount { get; internal set; }
    public int LingboTriggers { get; internal set; }
    public int TurnNumber { get; internal set; }
    public int SwordDamage { get; internal set; }

    public void GainQi(int amount)
    {
        if (amount > 0) Qi = (int)Math.Min(9L, (long)Qi + amount);
    }

    public bool TrySpendQi(int amount)
    {
        if (amount <= 0 || amount > Qi) return false;
        Qi -= amount;
        SpendingCount += 1;
        return true;
    }

    public void GainLingbo(int amount)
    {
        if (amount > 0) Lingbo = (int)Math.Min(3L, (long)Lingbo + amount);
    }

    public bool ConsumeLingbo()
    {
        if (Lingbo <= 0) return false;
        Lingbo -= 1;
        LingboTriggers += 1;
        return true;
    }

    public bool BeginTurn()
    {
        var hadSkillWithoutAttack = SkillsPlayed > 0 && AttacksPlayed == 0;
        TurnNumber += 1;
        Lingbo = 0;
        SkillsPlayed = 0;
        AttacksPlayed = 0;
        SwordPlays = 0;
        SpendingCount = 0;
        LingboTriggers = 0;
        DistinctSwords.Clear();
        return hadSkillWithoutAttack;
    }

    public DuanYuPlayOrdinal RegisterPlay(bool skill, bool attack, string? swordId)
    {
        if (skill) SkillsPlayed += 1;
        if (attack) AttacksPlayed += 1;
        var thirdKind = false;
        if (swordId != null)
        {
            SwordPlays += 1;
            var previousCount = DistinctSwords.Count;
            DistinctSwords.Add(swordId);
            thirdKind = previousCount == 2 && DistinctSwords.Count == 3;
        }
        return new DuanYuPlayOrdinal(skill ? SkillsPlayed : 0, swordId == null ? 0 : SwordPlays, thirdKind);
    }

    public static DuanYuAttackReduction ReduceAttack(decimal damage, bool attack, int lingbo)
    {
        if (!attack || lingbo <= 0 || damage < 1) return new DuanYuAttackReduction(damage, false);
        return new DuanYuAttackReduction(Math.Max(0, decimal.Floor(damage) - 3), true);
    }
}
