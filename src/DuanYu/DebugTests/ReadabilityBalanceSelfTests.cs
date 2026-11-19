#if DEBUG
using DuanYu.Cards;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace DuanYu.DebugTests;

internal static class ReadabilityBalanceSelfTests
{
    internal static IEnumerable<(string Name, Func<Task> Action)> Cases
    {
        get
        {
            foreach (var upgraded in new[] { false, true })
            {
                var suffix = upgraded ? "+" : "";
                foreach (var id in new[] { "DY_C15", "DY_R20" })
                    yield return ("readability:single-block-preview-" + id + suffix, () => SingleBlock(id, upgraded));
                yield return ("readability:concise-multihit-retains-strength" + suffix, () => MultiHitStrength(upgraded));
            }
        }
    }

    private static async Task SingleBlock(string id, bool upgraded)
    {
        foreach (var frail in new[] { false, true })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            await DuanYuState.LoseQi(fixture.Player, int.MaxValue);
            await DuanYuState.GainQi(fixture.Player, id == "DY_C15" ? 2 : 3);
            var context = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<DexterityPower>(context, fixture.Player.Creature, 3, fixture.Player.Creature, null);
            if (frail) await PowerCmd.Apply<FrailPower>(context, fixture.Player.Creature, 1, fixture.Enemy, null);
            var card = EngineSelfTests.AddCard(fixture, id, PileType.Hand, upgraded);
            int printed = id == "DY_C15" ? upgraded ? 16 : 12 : upgraded ? 22 : 16;
            int expected = frail ? (int)decimal.Floor((printed + 3) * 0.75m) : printed + 3;
            EngineSelfTests.Equal((decimal)printed, card.DynamicVars.Block.BaseValue, id + " has one combined canonical block amount");
            EngineSelfTests.Equal(true, card.Localization.Any(entry => entry.Item2.Contains("{Block:diff()}", StringComparison.Ordinal)), id + " description retains a live block preview instead of a fixed numeral");
            card.DynamicVars.Block.UpdateCardPreview(card, CardPreviewMode.Normal, null, true);
            EngineSelfTests.Equal((decimal)expected, decimal.Floor(card.DynamicVars.Block.PreviewValue), id + " displayed preview applies Dexterity and Frail once");
            EngineSelfTests.Equal(0, fixture.Player.Creature.Block, id + " preview grants no actual block");
            await BalanceSelfTests.PaidPlay(fixture, card);
            EngineSelfTests.Equal(expected, fixture.Player.Creature.Block, id + " actual combined block matches preview without a second Dexterity trigger");
            EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, id + " merged block retains the required Qi payment");
        }
    }

    private static async Task MultiHitStrength(bool upgraded)
    {
        var specifications = new (string Id, int BaseDamage, int UpgradedDamage, int Hits)[]
        {
            ("DY_B05", 10, 14, 2), ("DY_C02", 10, 14, 2),
            ("DY_U07", 15, 21, 2), ("DY_U33", 11, 15, 3)
        };
        foreach (var specification in specifications)
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), fixture.Player.Creature, 2, fixture.Player.Creature, null);
            var card = EngineSelfTests.AddCard(fixture, specification.Id, PileType.Hand, upgraded);
            EngineSelfTests.Equal(true, card.Localization.Any(entry => entry.Item2.Contains("{Damage:diff()}", StringComparison.Ordinal)), specification.Id + " concise text retains its live per-hit damage preview");
            int health = fixture.Enemy.CurrentHp;
            await BalanceSelfTests.PaidPlay(fixture, card);
            int perHit = (upgraded ? specification.UpgradedDamage : specification.BaseDamage) + 2;
            EngineSelfTests.Equal(perHit * specification.Hits, health - fixture.Enemy.CurrentHp, specification.Id + " concise repetition text preserves Strength on every actual hit");
            EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).SpendingCount, specification.Id + " repeated hits pay their Qi cost once");
        }
    }
}
#endif
