using DuanYu.Runtime;

var cases = new (string Name, Action Check)[]
{
    ("Qi grows beyond nine without integer overflow", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.GainQi(8);
        ledger.GainQi(3);
        Equal(11, ledger.Qi);
        ledger.GainQi(39);
        Equal(50, ledger.Qi);
        ledger.GainQi(50);
        Equal(100, ledger.Qi);
        ledger.GainQi(0);
        ledger.GainQi(-1);
        Equal(100, ledger.Qi);
        ledger.GainQi(int.MaxValue);
        Equal(int.MaxValue, ledger.Qi);
        ledger.GainQi(1);
        Equal(int.MaxValue, ledger.Qi);
        Equal(true, ledger.TrySpendQi(100));
        Equal(int.MaxValue - 100, ledger.Qi);
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
    ("Sword momentum caps at eight without integer overflow", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.GainSwordMomentum(7);
        ledger.GainSwordMomentum(3);
        Equal(8, ledger.SwordMomentum);
        ledger.GainSwordMomentum(int.MaxValue);
        Equal(8, ledger.SwordMomentum);
        ledger.GainSwordMomentum(0);
        ledger.GainSwordMomentum(-1);
        Equal(8, ledger.SwordMomentum);
    }),
    ("Sword momentum clears through turn reset", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.GainSwordMomentum(7);
        ledger.RegisterPlay(false, true, "DY_T01");
        ledger.GainLingbo(3);
        ledger.BeginTurn();
        Equal(0, ledger.SwordMomentum);
        Equal(0, ledger.SwordPlays);
        Equal(0, ledger.DistinctSwords.Count);
        Equal(0, ledger.Lingbo);
    }),
    ("Sword momentum is independent for every player's ledger", () =>
    {
        var first = new DuanYuCombatLedger();
        var second = new DuanYuCombatLedger();
        first.GainSwordMomentum(4);
        second.GainSwordMomentum(2);
        first.GainSwordMomentum(1);
        second.BeginTurn();
        Equal(5, first.SwordMomentum);
        Equal(0, second.SwordMomentum);
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
    ("Each successful spending ordinal is processed only once", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.GainQi(5);
        Equal(false, ledger.TryProcessSpend(0));
        Equal(false, ledger.TryProcessSpend(-1));
        Equal(false, ledger.TryProcessSpend(1));
        Equal(true, ledger.TrySpendQi(2));
        Equal(true, ledger.TryProcessSpend(1));
        Equal(false, ledger.TryProcessSpend(1));
        Equal(false, ledger.TryProcessSpend(2));
        Equal(true, ledger.TrySpendQi(2));
        Equal(true, ledger.TryProcessSpend(2));
        Equal(false, ledger.TryProcessSpend(1));
        Equal(false, ledger.TryProcessSpend(2));
    }),
    ("Failed spending cannot create a new refund trigger", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.GainQi(1);
        Equal(true, ledger.TrySpendQi(1));
        Equal(true, ledger.TryProcessSpend(ledger.SpendingCount));
        Equal(false, ledger.TrySpendQi(1));
        Equal(1, ledger.SpendingCount);
        Equal(false, ledger.TryProcessSpend(ledger.SpendingCount));
        Equal(false, ledger.TryProcessSpend(2));
    }),
    ("Initial deck resource sequence leaves two Qi", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.GainQi(3);
        ledger.BeginTurn();
        ledger.RegisterPlay(true, false, null);
        ledger.GainQi(1);
        ledger.RegisterPlay(true, false, null);
        ledger.RegisterPlay(false, true, null);
        Equal(true, ledger.TrySpendQi(2));
        Equal(2, ledger.Qi);
        Equal(0, ledger.Lingbo);
        Equal(2, ledger.SkillsPlayed);
    }),
    ("Lingbo reduces every hit without consuming layers", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.GainLingbo(9);
        var block = 4m;
        var hpLost = 0m;
        for (var hit = 0; hit < 5; hit++)
        {
            var damage = DuanYuCombatLedger.ReduceDamage(5, false, ledger.Lingbo);
            var absorbed = Math.Min(block, damage);
            block -= absorbed;
            hpLost += damage - absorbed;
        }
        Equal(6m, hpLost);
        Equal(3, ledger.Lingbo);
    }),
    ("Only a new turn restores the first-skill ordinal", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.BeginTurn();
        Equal(1, ledger.RegisterPlay(true, false, null).Skill);
        Equal(0, ledger.RegisterPlay(false, true, "DY_T01").Skill);
        Equal(0, ledger.RegisterPlay(false, true, "DY_T01").Skill);
        Equal(2, ledger.RegisterPlay(true, false, null).Skill);
        Equal(3, ledger.RegisterPlay(true, false, null).Skill);
        ledger.BeginTurn();
        Equal(1, ledger.RegisterPlay(true, false, null).Skill);
    }),
    ("Fully blocked hits preserve Lingbo", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.GainLingbo(1);
        var damage = DuanYuCombatLedger.ReduceDamage(5, false, ledger.Lingbo);
        Equal(16m, 20 - damage);
        Equal(1, ledger.Lingbo);
    }),
    ("Lingbo floors damage and preserves explicit HP loss", () =>
    {
        Equal(0m, DuanYuCombatLedger.ReduceDamage(0, false, 1));
        Equal(5m, DuanYuCombatLedger.ReduceDamage(5, true, 1));
        Equal(0m, DuanYuCombatLedger.ReduceDamage(1, false, 1));
        Equal(4m, DuanYuCombatLedger.ReduceDamage(5.9m, false, 1));
        Equal(2m, DuanYuCombatLedger.ReduceDamage(5, false, 3));
    }),
    ("Lingbo gains report only actual layers below the cap", () =>
    {
        var ledger = new DuanYuCombatLedger();
        Equal(1, ledger.GainLingbo(1));
        Equal(2, ledger.GainLingbo(int.MaxValue));
        Equal(0, ledger.GainLingbo(1));
        Equal(0, ledger.GainLingbo(0));
        Equal(0, ledger.GainLingbo(-1));
        Equal(3, ledger.Lingbo);
        ledger.BeginTurn();
        Equal(0, ledger.Lingbo);
        Equal(2, ledger.GainLingbo(2));
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
    ("Sword combinations trigger exactly on the third and sixth kinds", () =>
    {
        var ledger = new DuanYuCombatLedger();
        var swordIds = new[] { "DY_T01", "DY_T02", "DY_T03", "DY_T04", "DY_T05", "DY_T06" };
        for (var index = 0; index < swordIds.Length; index++)
        {
            var first = ledger.RegisterPlay(false, true, swordIds[index]);
            Equal(index == 2, first.ThirdSwordKind);
            Equal(index == 5, first.SixthSwordKind);
            var repeated = ledger.RegisterPlay(false, true, swordIds[index]);
            Equal(false, repeated.ThirdSwordKind);
            Equal(false, repeated.SixthSwordKind);
            Equal(index + 1, ledger.DistinctSwords.Count);
        }
        Equal(12, ledger.SwordPlays);
        Equal(6, ledger.DistinctSwords.Count);
    }),
    ("Skills and ordinary attacks cannot advance sword combinations", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.RegisterPlay(false, true, "DY_T01");
        ledger.RegisterPlay(false, true, "DY_T02");
        var skill = ledger.RegisterPlay(true, false, null);
        var attack = ledger.RegisterPlay(false, true, null);
        Equal(false, skill.ThirdSwordKind || skill.SixthSwordKind);
        Equal(false, attack.ThirdSwordKind || attack.SixthSwordKind);
        Equal(2, ledger.DistinctSwords.Count);
        Equal(2, ledger.SwordPlays);
        Equal(true, ledger.RegisterPlay(false, true, "DY_T03").ThirdSwordKind);
    }),
    ("Additional turns reset both sword thresholds and refund deduplication", () =>
    {
        var ledger = new DuanYuCombatLedger();
        ledger.GainQi(9);
        var swordIds = new[] { "DY_T01", "DY_T02", "DY_T03", "DY_T04", "DY_T05", "DY_T06" };
        for (var turn = 1; turn <= 3; turn++)
        {
            ledger.BeginTurn();
            Equal(turn, ledger.TurnNumber);
            Equal(0, ledger.SwordPlays);
            Equal(0, ledger.DistinctSwords.Count);
            Equal(0, ledger.SwordMomentum);
            Equal(0, ledger.SpendingCount);
            Equal(false, ledger.TryProcessSpend(1));
            Equal(true, ledger.TrySpendQi(1));
            Equal(true, ledger.TryProcessSpend(1));
            Equal(false, ledger.TryProcessSpend(1));
            var thirdTriggers = 0;
            var sixthTriggers = 0;
            foreach (var swordId in swordIds)
            {
                var play = ledger.RegisterPlay(false, true, swordId);
                if (play.ThirdSwordKind) thirdTriggers += 1;
                if (play.SixthSwordKind) sixthTriggers += 1;
            }
            Equal(1, thirdTriggers);
            Equal(1, sixthTriggers);
        }
        Equal(6, ledger.Qi);
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
        first.GainQi(50);
        first.RegisterPlay(true, false, null);
        second.BeginTurn();
        Equal(50, first.Qi);
        Equal(1, first.SkillsPlayed);
        Equal(0, second.Qi);
        Equal(0, second.SkillsPlayed);
    }),
    ("Spending deduplication remains isolated between players", () =>
    {
        var first = new DuanYuCombatLedger();
        var second = new DuanYuCombatLedger();
        first.GainQi(1);
        second.GainQi(1);
        Equal(true, first.TrySpendQi(1));
        Equal(true, second.TrySpendQi(1));
        Equal(true, first.TryProcessSpend(1));
        Equal(true, second.TryProcessSpend(1));
        Equal(false, first.TryProcessSpend(1));
        second.BeginTurn();
        Equal(false, first.TryProcessSpend(1));
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
        Equal(false, saved.Contains("SwordMomentum"));
    }),
    ("A new combat ledger does not carry old combat resources", () =>
    {
        var previous = new DuanYuCombatLedger();
        previous.GainQi(9);
        previous.GainLingbo(3);
        previous.GainSwordMomentum(12);
        previous.RegisterPlay(false, true, "DY_T01");
        var next = new DuanYuCombatLedger();
        Equal(0, next.Qi);
        Equal(0, next.Lingbo);
        Equal(0, next.SwordPlays);
        Equal(0, next.SwordMomentum);
        next.GainQi(3);
        Equal(3, next.Qi);
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
