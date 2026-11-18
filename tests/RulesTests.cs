using DuanYu.Runtime;

var cases = new (string Name, Action Check)[]
{
    ("Qi caps at nine and does not overflow", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.GainQi(8);
        ledger.GainQi(3);
        Equal(9, ledger.Qi);
        ledger.GainQi(int.MaxValue);
        Equal(9, ledger.Qi);
    }),
    ("Failed spending preserves resources and counters", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.GainQi(1);
        Equal(false, ledger.TrySpendQi(2));
        Equal(false, ledger.TrySpendQi(0));
        Equal(false, ledger.TrySpendQi(-1));
        Equal(1, ledger.Qi);
        Equal(0, ledger.SpendingCount);
        ledger.GainQi(1);
        Equal(0, ledger.SpendingCount);
    }),
    ("Payment records the empty resource before refunds", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.GainQi(3);
        Equal(true, ledger.TrySpendQi(3));
        var wasEmptyAtPayment = ledger.Qi == 0;
        ledger.GainQi(1);
        Equal(true, wasEmptyAtPayment);
        Equal(1, ledger.Qi);
        Equal(1, ledger.SpendingCount);
    }),
    ("Initial deck resource sequence leaves three Qi", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.GainQi(2);
        ledger.BeginTurn();
        var firstSkill = ledger.RegisterPlay(true, false, null);
        ledger.GainQi(2);
        if (firstSkill.Skill == 1) ledger.GainQi(1);
        ledger.RegisterPlay(true, false, null);
        ledger.GainLingbo(1);
        ledger.RegisterPlay(false, true, null);
        Equal(true, ledger.TrySpendQi(2));
        Equal(3, ledger.Qi);
        Equal(1, ledger.Lingbo);
        Equal(2, ledger.SkillsPlayed);
    }),
    ("Lingbo consumes exactly three layers across five hits", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.GainLingbo(9);
        var block = 4m;
        var hpLost = 0m;
        for (var hit = 0; hit < 5; hit++)
        {
            var reduction = DuanYuCombatLedger.ReduceAttack(5, true, ledger.Lingbo);
            if (reduction.ConsumesLingbo) ledger.ConsumeLingbo();
            var absorbed = Math.Min(block, reduction.Damage);
            block -= absorbed;
            hpLost += reduction.Damage - absorbed;
        }
        Equal(12m, hpLost);
        Equal(0, ledger.Lingbo);
        Equal(3, ledger.LingboTriggers);
    }),
    ("Fully blocked attacks still consume Lingbo", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.GainLingbo(1);
        var reduction = DuanYuCombatLedger.ReduceAttack(5, true, ledger.Lingbo);
        if (reduction.ConsumesLingbo) ledger.ConsumeLingbo();
        Equal(18m, 20 - reduction.Damage);
        Equal(0, ledger.Lingbo);
    }),
    ("Zero attacks and non-attack damage preserve Lingbo", () =>
    {
        Equal(new DuanYuAttackReduction(0, false), DuanYuCombatLedger.ReduceAttack(0, true, 1));
        Equal(new DuanYuAttackReduction(5, false), DuanYuCombatLedger.ReduceAttack(5, false, 1));
        Equal(new DuanYuAttackReduction(0, true), DuanYuCombatLedger.ReduceAttack(1, true, 1));
        Equal(new DuanYuAttackReduction(2, true), DuanYuCombatLedger.ReduceAttack(5.9m, true, 1));
    }),
    ("Upgrades and duplicate sword plays share one identity", () =>
    {
        var ledger = new DuanYuCombatLedger();
        Equal(false, ledger.RegisterPlay(false, true, "DY_T01").ThirdSwordKind);
        Equal(false, ledger.RegisterPlay(false, true, "DY_T01").ThirdSwordKind);
        Equal(false, ledger.RegisterPlay(false, true, "DY_T02").ThirdSwordKind);
        Equal(true, ledger.RegisterPlay(false, true, "DY_T04").ThirdSwordKind);
        Equal(false, ledger.RegisterPlay(false, true, "DY_T03").ThirdSwordKind);
        Equal(5, ledger.SwordPlays);
        Equal(5, ledger.AttacksPlayed);
        Equal(4, ledger.DistinctSwords.Count);
    }),
    ("Turn reset preserves Qi and clears old evasion and counts", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.GainQi(7);
        ledger.GainLingbo(3);
        ledger.RegisterPlay(true, false, null);
        ledger.TrySpendQi(2);
        Equal(true, ledger.BeginTurn());
        Equal(5, ledger.Qi);
        Equal(0, ledger.Lingbo);
        Equal(0, ledger.SkillsPlayed);
        Equal(0, ledger.SpendingCount);
        ledger.GainLingbo(1);
        Equal(1, ledger.Lingbo);
        Equal(1, ledger.TurnNumber);
    }),
    ("An attack cancels the peaceful previous-turn condition", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.RegisterPlay(true, false, null);
        ledger.RegisterPlay(false, true, "DY_T05");
        Equal(false, ledger.BeginTurn());
        Equal(0, ledger.DistinctSwords.Count);
    }),
    ("Player ledgers remain isolated", () =>
    {
        var first = new DuanYuCombatLedger();
        var second = new DuanYuCombatLedger();
        first.GainQi(9);
        first.RegisterPlay(true, false, null);
        second.BeginTurn();
        Equal(9, first.Qi);
        Equal(1, first.SkillsPlayed);
        Equal(0, second.Qi);
        Equal(0, second.SkillsPlayed);
    }),
    ("Run history round-trips without combat resources", () =>
    {
        var history = new DuanYuRunHistory();
        history.Relics.Add("CQBZLY_DY_RELIC08");
        history.Events.Add("CQBZLY_DY_EVENT01");
        var saved = System.Text.Json.JsonSerializer.Serialize(history);
        var restored = System.Text.Json.JsonSerializer.Deserialize<DuanYuRunHistory>(saved)!;
        Equal(true, restored.Relics.Contains("CQBZLY_DY_RELIC08"));
        Equal(true, restored.Events.Contains("CQBZLY_DY_EVENT01"));
        Equal(false, saved.Contains("Qi"));
        Equal(false, saved.Contains("Lingbo"));
    }),
    ("A new combat ledger does not carry old combat resources", () =>
    {
        var previous = new DuanYuCombatLedger();
        previous.GainQi(9);
        previous.GainLingbo(3);
        previous.RegisterPlay(false, true, "DY_T01");
        var next = new DuanYuCombatLedger();
        Equal(0, next.Qi);
        Equal(0, next.Lingbo);
        Equal(0, next.SwordPlays);
        next.GainQi(2);
        Equal(2, next.Qi);
    })
};

foreach (var test in cases)
{
    test.Check();
    Console.WriteLine($"PASS {test.Name}");
}
Console.WriteLine($"{cases.Length} runtime rules cases passed. Engine hooks, UI, networking, and saves require separate integration testing.");

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
}
