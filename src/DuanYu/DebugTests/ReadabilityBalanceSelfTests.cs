#if DEBUG
using DuanYu.Cards;
using DuanYu.Relics;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;

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
                foreach (var id in new[] { "DY_B05", "DY_C04", "DY_U07", "DY_U17", "DY_U33", "DY_R13", "DY_T01", "DY_T03" })
                    yield return ("readability:single-damage-packet-" + id + suffix, () => SingleDamage(id, upgraded));
                yield return ("readability:single-primary-and-secondary-damage" + suffix, () => PrimaryAndSecondaryDamage(upgraded));
                yield return ("readability:qi-x-damage-preview-and-multi-hit" + suffix, () => XDamage(upgraded));
                yield return ("readability:x-block-preview-and-single-packet" + suffix, () => XBlock(upgraded));
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

    private static async Task SingleDamage(string id, bool upgraded)
    {
        var specifications = new (string Id, int BaseDamage, int UpgradedDamage, bool All)[]
        {
            ("DY_B05", 16, 22, false), ("DY_C04", 9, 12, true),
            ("DY_U07", 27, 37, false), ("DY_U17", 20, 27, true),
            ("DY_U33", 16, 22, false), ("DY_R13", 52, 70, false),
            ("DY_T01", 7, 10, false), ("DY_T03", 5, 7, true)
        };
        var specification = specifications.Single(value => value.Id == id);
        var fixture = await EngineSelfTests.ReadyFixture();
        var context = new ThrowingPlayerChoiceContext();
        var secondEnemy = fixture.Combat.CreateCreature(ModelDb.Monster<BigDummy>().ToMutable(), CombatSide.Enemy, null);
        fixture.Combat.AddCreature(secondEnemy);
        await PowerCmd.Apply<StrengthPower>(context, fixture.Player.Creature, 2, fixture.Player.Creature, null);
        await PowerCmd.Apply<ThornsPower>(context, fixture.Enemy, 1, fixture.Enemy, null);
        await PowerCmd.Apply<ThornsPower>(context, secondEnemy, 1, secondEnemy, null);
        var card = EngineSelfTests.AddCard(fixture, id, PileType.Hand, upgraded);
        int printed = upgraded ? specification.UpgradedDamage : specification.BaseDamage;
        EngineSelfTests.Equal((decimal)printed, card.DynamicVars.Damage.BaseValue, id + " canonical preview stores the complete combined damage");
        EngineSelfTests.Equal(false, card.DynamicVars.ContainsKey("ExtraDamage"), id + " no longer exposes a legacy second damage packet");
        EngineSelfTests.Equal(true, card.Localization.Any(entry => entry.Item2.Contains("{Damage:diff()}", StringComparison.Ordinal)), id + " concise text retains a live combined damage preview");
        card.DynamicVars.Damage.UpdateCardPreview(card, CardPreviewMode.Normal, fixture.Enemy, true);
        EngineSelfTests.Equal((decimal)printed + 2, card.DynamicVars.Damage.PreviewValue, id + " preview applies Strength once to combined damage");
        int health = fixture.Enemy.CurrentHp;
        int secondHealth = secondEnemy.CurrentHp;
        int playerHealth = fixture.Player.Creature.CurrentHp;
        await BalanceSelfTests.PaidPlay(fixture, card);
        EngineSelfTests.Equal(printed + 2, health - fixture.Enemy.CurrentHp, id + " combined damage applies Strength once");
        EngineSelfTests.Equal(specification.All ? printed + 2 : 0, secondHealth - secondEnemy.CurrentHp, id + " retains its target selection");
        EngineSelfTests.Equal(specification.All ? 2 : 1, playerHealth - fixture.Player.Creature.CurrentHp, id + " triggers Thorns once per affected target");
        EngineSelfTests.Equal(id == "DY_C04" ? 0 : 1, DuanYuState.Get(fixture.Player).SpendingCount, id + " combined damage retains its mandatory Qi payment");
    }

    private static async Task PrimaryAndSecondaryDamage(bool upgraded)
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        var context = new ThrowingPlayerChoiceContext();
        var other = fixture.Combat.CreateCreature(ModelDb.Monster<BigDummy>().ToMutable(), CombatSide.Enemy, null);
        fixture.Combat.AddCreature(other);
        await PowerCmd.Apply<StrengthPower>(context, fixture.Player.Creature, 2, fixture.Player.Creature, null);
        await PowerCmd.Apply<ThornsPower>(context, fixture.Enemy, 1, fixture.Enemy, null);
        await PowerCmd.Apply<ThornsPower>(context, other, 1, other, null);
        var card = EngineSelfTests.AddCard(fixture, "DY_R05", PileType.Hand, upgraded);
        int health = fixture.Enemy.CurrentHp;
        int otherHealth = other.CurrentHp;
        int playerHealth = fixture.Player.Creature.CurrentHp;
        int primary = upgraded ? 64 : 48;
        int secondary = upgraded ? 26 : 20;
        card.DynamicVars.Damage.UpdateCardPreview(card, CardPreviewMode.Normal, fixture.Enemy, true);
        card.DynamicVars["ExtraDamage"].UpdateCardPreview(card, CardPreviewMode.Normal, other, true);
        EngineSelfTests.Equal((decimal)primary + 2, card.DynamicVars.Damage.PreviewValue, "R05 primary preview includes the combined damage with Strength once");
        EngineSelfTests.Equal((decimal)secondary + 2, card.DynamicVars["ExtraDamage"].PreviewValue, "R05 other-enemy preview retains its separate target amount");
        await BalanceSelfTests.PaidPlay(fixture, card);
        EngineSelfTests.Equal(primary + 2, health - fixture.Enemy.CurrentHp, "R05 primary receives one combined damage packet");
        EngineSelfTests.Equal(secondary + 2, otherHealth - other.CurrentHp, "R05 other enemy receives only its own damage packet");
        EngineSelfTests.Equal(2, playerHealth - fixture.Player.Creature.CurrentHp, "R05 triggers Thorns once per affected enemy");
        EngineSelfTests.Equal(1, DuanYuState.Get(fixture.Player).SpendingCount, "R05 pays Qi once for both distinct target amounts");

        var victory = await EngineSelfTests.ReadyFixture();
        await CreatureCmd.SetCurrentHp(victory.Enemy, 1);
        await BalanceSelfTests.PaidPlay(victory, EngineSelfTests.AddCard(victory, "DY_R05", PileType.Hand, upgraded));
        EngineSelfTests.Equal(false, victory.Enemy.IsAlive, "R05 safely finishes when combined primary damage kills the last enemy");

        var lethalRetaliation = await EngineSelfTests.ReadyFixture();
        var survivor = lethalRetaliation.Combat.CreateCreature(ModelDb.Monster<BigDummy>().ToMutable(), CombatSide.Enemy, null);
        lethalRetaliation.Combat.AddCreature(survivor);
        await PowerCmd.Apply<ThornsPower>(context, lethalRetaliation.Enemy, 1, lethalRetaliation.Enemy, null);
        await CreatureCmd.SetCurrentHp(lethalRetaliation.Player.Creature, 1);
        int survivorHealth = survivor.CurrentHp;
        await BalanceSelfTests.PaidPlay(lethalRetaliation, EngineSelfTests.AddCard(lethalRetaliation, "DY_R05", PileType.Hand, upgraded));
        EngineSelfTests.Equal(false, lethalRetaliation.Player.Creature.IsAlive, "R05 primary Thorns can kill its owner");
        EngineSelfTests.Equal(survivorHealth, survivor.CurrentHp, "R05 stops further enemy damage after its owner dies");
    }

    private static async Task XDamage(bool upgraded)
    {
        foreach (var paidX in new[] { 0, 3, 12 })
        foreach (var energy in new[] { 0, 3 })
        foreach (var chemicalX in new[] { false, true })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            var context = new ThrowingPlayerChoiceContext();
            if (chemicalX) await RelicCmd.Obtain<ChemicalX>(fixture.Player);
            await PowerCmd.Apply<StrengthPower>(context, fixture.Player.Creature, 2, fixture.Player.Creature, null);
            await DuanYuState.GainSwordMomentum(fixture.Player, 3);
            await DuanYuState.AddSwordDamage(fixture.Player, 2);
            await DuanYuState.AddEffect(fixture.Player, "DY_U14", 4);
            await PowerCmd.Apply<ThornsPower>(context, fixture.Enemy, 1, fixture.Enemy, null);
            var second = fixture.Combat.CreateCreature(ModelDb.Monster<BigDummy>().ToMutable(), CombatSide.Enemy, null);
            fixture.Combat.AddCreature(second);
            await PowerCmd.Apply<ThornsPower>(context, second, 1, second, null);
            await DuanYuState.LoseQi(fixture.Player, int.MaxValue);
            await DuanYuState.GainQi(fixture.Player, paidX);
            fixture.Player.PlayerCombatState!.Energy = energy;
            var card = EngineSelfTests.AddCard(fixture, "DY_R21", PileType.Hand, upgraded);
            int perHit = (upgraded ? 8 : 6) + 2 + 3 + 2 + 4;
            EngineSelfTests.Equal(TargetType.AnyEnemy, card.TargetType, "R21 selects one enemy");
            EngineSelfTests.Equal(false, card.EnergyCost.CostsX, "R21 no longer uses energy X");
            EngineSelfTests.Equal(0, card.EnergyCost.GetAmountToSpend(), "R21 uses zero energy at every current energy amount");
            card.DynamicVars.Damage.UpdateCardPreview(card, CardPreviewMode.Normal, fixture.Enemy, true);
            EngineSelfTests.Equal((decimal)perHit, card.DynamicVars.Damage.PreviewValue, "R21 displays damage per hit with sword bonuses");
            EngineSelfTests.Equal(paidX, DuanYuState.Get(fixture.Player).Qi, "R21 preview never spends Qi");
            int health = fixture.Enemy.CurrentHp;
            int secondHealth = second.CurrentHp;
            int playerHealth = fixture.Player.Creature.CurrentHp;
            await BalanceSelfTests.PaidPlay(fixture, card);
            EngineSelfTests.Equal(perHit * paidX, health - fixture.Enemy.CurrentHp, "R21 applies the previewed damage to each of its X hits");
            EngineSelfTests.Equal(0, secondHealth - second.CurrentHp, "R21 never damages the unselected enemy");
            EngineSelfTests.Equal(paidX, playerHealth - fixture.Player.Creature.CurrentHp, "R21 triggers Thorns for each hit on its only target");
            EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "R21 consumes all available Qi");
            EngineSelfTests.Equal(energy, fixture.Player.PlayerCombatState.Energy, "R21 does not spend energy or resolve Chemical X");
            EngineSelfTests.Equal(paidX == 0 ? 0 : 1, DuanYuState.Get(fixture.Player).SpendingCount, "R21 positive Qi payment counts once and zero X does not count");
            EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).SwordPlays, "R21 does not count toward sword play thresholds");
            EngineSelfTests.Equal(3, DuanYuState.Get(fixture.Player).SwordMomentum, "R21 does not generate Sword Momentum");
        }
    }

    private static async Task XBlock(bool upgraded)
    {
        foreach (var paidX in new[] { 0, 3 })
        foreach (var chemicalX in new[] { false, true })
        foreach (var frail in new[] { false, true })
        {
            var fixture = await EngineSelfTests.ReadyFixture();
            var context = new ThrowingPlayerChoiceContext();
            if (chemicalX) await RelicCmd.Obtain<ChemicalX>(fixture.Player);
            await PowerCmd.Apply<DexterityPower>(context, fixture.Player.Creature, 3, fixture.Player.Creature, null);
            if (frail) await PowerCmd.Apply<FrailPower>(context, fixture.Player.Creature, 1, fixture.Enemy, null);
            fixture.Player.PlayerCombatState!.Energy = paidX;
            var card = EngineSelfTests.AddCard(fixture, "DY_U37", PileType.Hand, upgraded);
            int resolvedX = paidX + (chemicalX ? 2 : 0);
            int raw = (upgraded ? 6 : 4) * resolvedX;
            int expected = resolvedX == 0 ? 0 : frail ? (int)decimal.Floor((raw + 3) * 0.75m) : raw + 3;
            card.DynamicVars["XBlock"].UpdateCardPreview(card, CardPreviewMode.Normal, null, true);
            EngineSelfTests.Equal((decimal)expected, decimal.Floor(card.DynamicVars["XBlock"].PreviewValue), "U37 total preview applies Dexterity and Frail once after scaling by resolved X");
            EngineSelfTests.Equal(0, fixture.Player.Creature.Block, "U37 preview does not grant block");
            await BalanceSelfTests.PaidPlay(fixture, card);
            EngineSelfTests.Equal(expected, fixture.Player.Creature.Block, "U37 one combined block packet matches its total preview, including X zero");
            EngineSelfTests.Equal(paidX, card.EnergyCost.CapturedXValue, "U37 captures actual X without Chemical X pretending to spend energy");
        }
    }
}
#endif
