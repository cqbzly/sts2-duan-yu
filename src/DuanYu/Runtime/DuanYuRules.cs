namespace DuanYu.Runtime;

public readonly record struct DuanYuPlayOrdinal(int Skill, int Sword, bool ThirdSwordKind, bool SixthSwordKind);

public sealed class DuanYuRunHistory
{
    public HashSet<string> Relics { get; set; } = [];
    public HashSet<string> Events { get; set; } = [];
}

public class DuanYuCombatLedger
{
    public const int LingboReduction = 1;
    public const int SwordMomentumCap = 8;
    private readonly HashSet<int> processedSpendOrdinals = [];
    public int Qi { get; internal set; }
    public int Lingbo { get; internal set; }
    public int SkillsPlayed { get; internal set; }
    public int AttacksPlayed { get; internal set; }
    public int SwordPlays { get; internal set; }
    public HashSet<string> DistinctSwords { get; } = [];
    public int SpendingCount { get; internal set; }
    public int TurnNumber { get; internal set; }
    public int SwordDamage { get; internal set; }
    public int SwordMomentum { get; internal set; }

    public void GainSwordMomentum(int amount)
    {
        if (amount > 0) SwordMomentum = (int)Math.Min(SwordMomentumCap, (long)SwordMomentum + amount);
    }

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

    public bool TryProcessSpend(int ordinal) => ordinal > 0 && ordinal <= SpendingCount && processedSpendOrdinals.Add(ordinal);

    public int GainLingbo(int amount)
    {
        var previous = Lingbo;
        if (amount > 0) Lingbo = (int)Math.Min(3L, (long)Lingbo + amount);
        return Lingbo - previous;
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
        SwordMomentum = 0;
        DistinctSwords.Clear();
        processedSpendOrdinals.Clear();
        return hadSkillWithoutAttack;
    }

    public DuanYuPlayOrdinal RegisterPlay(bool skill, bool attack, string? swordId)
    {
        if (skill) SkillsPlayed += 1;
        if (attack) AttacksPlayed += 1;
        var thirdKind = false;
        var sixthKind = false;
        if (swordId != null)
        {
            SwordPlays += 1;
            var previousCount = DistinctSwords.Count;
            DistinctSwords.Add(swordId);
            thirdKind = previousCount == 2 && DistinctSwords.Count == 3;
            sixthKind = previousCount == 5 && DistinctSwords.Count == 6;
        }
        return new DuanYuPlayOrdinal(skill ? SkillsPlayed : 0, swordId == null ? 0 : SwordPlays, thirdKind, sixthKind);
    }

    public static decimal ReduceDamage(decimal damage, bool hpLoss, int lingbo)
    {
        if (hpLoss || lingbo <= 0 || damage < 1) return damage;
        return Math.Max(0, decimal.Floor(damage) - (long)Math.Min(3, lingbo) * LingboReduction);
    }
}
