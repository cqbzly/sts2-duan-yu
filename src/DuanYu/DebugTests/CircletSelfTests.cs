#if DEBUG
using BaseLib.Abstracts;
using DuanYu.Character;
using DuanYu.Relics;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;

namespace DuanYu.DebugTests;

internal static class CircletSelfTests
{
    internal static IEnumerable<(string Name, Func<Task> Action)> Cases
    {
        get
        {
            yield return ("circlet:orobas-native-starter-replacement", OrobasReplacement);
            yield return ("circlet:event-and-player-save-roundtrip", SaveRoundTrip);
            yield return ("circlet:legacy-unclaimed-offer-migration", LegacyOfferMigration);
            yield return ("circlet:vanilla-and-other-character-isolation", VanillaIsolation);
            yield return ("circlet:combat-start-first-turn-and-cap", StartingAndTurnQi);
            yield return ("circlet:late-acquisition-and-no-payment-rewards", LateAcquisitionAndRewards);
            yield return ("circlet:multiplayer-participants-and-melt", PlayerIsolationAndMelt);
            yield return ("circlet:combat-reset-and-inactive-owner", CombatLifetime);
            yield return ("circlet:reward-exclusion-and-icons", RewardPoolAndIcons);
        }
    }

    private static async Task OrobasReplacement()
    {
        var fixture = EngineSelfTests.CreateFixture(combat: false);
        var player = fixture.Player;
        var pendant = player.Relics.OfType<LanghuanPendant>().Single();
        int slot = player.Relics.IndexOf(pendant);
        var touch = (TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
        EngineSelfTests.Equal(true, touch.SetupForPlayer(player), "native Orobas offer accepts the custom starter");
        EngineSelfTests.Equal<ModelId?>(pendant.Id, touch.StarterRelic, "native Orobas offer saves the original pendant");
        EngineSelfTests.Equal<ModelId?>(ModelDb.Relic<LanghuanCirclet>().Id, touch.UpgradedRelic, "native Orobas offer resolves the custom circlet");
        await RelicCmd.Obtain(touch, player);
        EngineSelfTests.Equal(0, player.Relics.OfType<LanghuanPendant>().Count(), "native Orobas obtaining removes the pendant");
        EngineSelfTests.Equal(1, player.Relics.OfType<LanghuanCirclet>().Count(), "native Orobas obtaining gives one custom circlet");
        EngineSelfTests.Equal(ModelDb.Relic<LanghuanCirclet>().Id, player.Relics[slot].Id, "replacement keeps the starter inventory slot");
        EngineSelfTests.Equal(false, player.Relics.Any(relic => relic is Circlet), "native Orobas does not fall back to the vanilla circlet");
        EngineSelfTests.Equal(0, DuanYuState.Get(player).Qi, "obtaining outside combat does not grant combat Qi");
    }

    private static async Task SaveRoundTrip()
    {
        var fixture = EngineSelfTests.CreateFixture(combat: false);
        var touch = (TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
        EngineSelfTests.Equal(true, touch.SetupForPlayer(fixture.Player), "save fixture creates an actual native offer");
        var restoredTouch = (TouchOfOrobas)RelicModel.FromSerializable(touch.ToSerializable());
        EngineSelfTests.Equal(touch.StarterRelic, restoredTouch.StarterRelic, "Orobas saved properties restore the original starter");
        EngineSelfTests.Equal(touch.UpgradedRelic, restoredTouch.UpgradedRelic, "Orobas saved properties restore the custom upgrade");
        await RelicCmd.Obtain(restoredTouch, fixture.Player);
        var restored = Player.FromSerializable(fixture.Player.ToSerializable());
        var circlet = restored.Relics.OfType<LanghuanCirclet>().Single();
        EngineSelfTests.Equal(ModelDb.Relic<LanghuanCirclet>().Id, circlet.Id, "player save restores the custom relic ID");
        EngineSelfTests.Equal(true, ReferenceEquals(restored, circlet.Owner), "restored custom circlet belongs to the restored player");
        EngineSelfTests.Equal(false, circlet.IsMelted, "ordinary upgraded starter remains usable after loading");
        EngineSelfTests.Equal(false, restored.Relics.Any(relic => relic is LanghuanPendant or Circlet), "saved upgrade does not restore a duplicate or vanilla fallback");
        EngineSelfTests.Equal(true, DuanYuState.HasAcquiredRelic(restored, circlet.Id.Entry), "custom relic history survives the player save");
        EngineSelfTests.Equal(0, DuanYuState.Get(restored).Qi, "out-of-combat player save carries no combat Qi");
    }

    private static async Task VanillaIsolation()
    {
        var touch = (TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
        EngineSelfTests.Equal(ModelDb.Relic<BlackBlood>().Id, touch.GetUpgradedStarterRelic(ModelDb.Relic<BurningBlood>()).Id, "native Ironclad starter upgrade stays Black Blood");
        EngineSelfTests.Equal(ModelDb.Relic<Circlet>().Id, touch.GetUpgradedStarterRelic(ModelDb.Relic<Circlet>()).Id, "native fallback remains the vanilla circlet");
        EngineSelfTests.Equal(RelicRarity.None, ModelDb.Relic<Circlet>().Rarity, "vanilla circlet retains its native rarity");
        EngineSelfTests.Equal(true, ModelDb.Relic<Circlet>().IsStackable, "vanilla circlet remains stackable");
        var fixture = EngineSelfTests.CreateFixture(duanYu: false);
        await RelicCmd.Obtain<Circlet>(fixture.Player);
        fixture.Player.PlayerCombatState!.Stars = 4;
        await Hook.BeforeCombatStart(fixture.Run, fixture.Combat);
        await StartTurn(fixture, fixture.Player);
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "vanilla circlet does not grant Qi to another character");
        EngineSelfTests.Equal(4, fixture.Player.PlayerCombatState.Stars, "native circlet and custom resource hooks preserve stars");
        EngineSelfTests.Equal(99, fixture.Player.PlayerCombatState.Energy, "native circlet gains no energy from the custom upgrade");
        EngineSelfTests.Equal(false, fixture.Player.Relics.Any(relic => relic is LanghuanCirclet), "other characters receive no custom circlet implicitly");
    }

    private static async Task LegacyOfferMigration()
    {
        foreach (bool reverse in new[] { false, true })
        foreach (bool relicLoader in new[] { false, true })
        {
            var fixture = EngineSelfTests.CreateFixture(combat: false);
            var starterId = ModelDb.Relic<LanghuanPendant>().Id;
            var vanillaId = ModelDb.Relic<Circlet>().Id;
            var custom = ModelDb.Relic<LanghuanCirclet>();
            var ids = new List<SavedProperties.SavedProperty<ModelId>>
            {
                new(nameof(TouchOfOrobas.StarterRelic), starterId),
                new(nameof(TouchOfOrobas.UpgradedRelic), vanillaId)
            };
            if (reverse) ids.Reverse();
            var originalNames = ids.Select(property => property.name).ToArray();
            var props = new SavedProperties { modelIds = ids };
            TouchOfOrobas touch;
            if (relicLoader)
            {
                touch = (TouchOfOrobas)RelicModel.FromSerializable(new SerializableRelic
                {
                    Id = ModelDb.Relic<TouchOfOrobas>().Id,
                    Props = props
                });
            }
            else
            {
                touch = (TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
                props.Fill(touch);
            }
            EngineSelfTests.Equal<ModelId?>(starterId, touch.StarterRelic, "legacy offer migration retains the original pendant");
            EngineSelfTests.Equal<ModelId?>(custom.Id, touch.UpgradedRelic, "legacy offer migration resolves the custom circlet regardless of property order or loader");
            EngineSelfTests.Equal(custom.Title.GetFormattedText(), ((StringVar)touch.DynamicVars[nameof(TouchOfOrobas.UpgradedRelic)]).StringValue, "legacy offer immediately displays the custom circlet title");
            var tips = touch.HoverTipsExcludingRelic.ToArray();
            EngineSelfTests.Equal(true, tips.Any(tip => tip.CanonicalModel?.Id == custom.Id), "legacy offer hover includes the custom circlet behavior");
            EngineSelfTests.Equal(false, tips.Any(tip => tip.CanonicalModel?.Id == vanillaId), "legacy offer hover contains no stale vanilla circlet");
            EngineSelfTests.Equal(true, ReferenceEquals(ids, props.modelIds), "migration does not replace the original saved property list");
            EngineSelfTests.Equal(true, originalNames.SequenceEqual(props.modelIds!.Select(property => property.name)), "migration preserves original saved property ordering");
            EngineSelfTests.Equal(vanillaId, props.modelIds.Single(property => property.name == nameof(TouchOfOrobas.UpgradedRelic)).value, "migration does not mutate the original saved fallback ID");
            await RelicCmd.Obtain(touch, fixture.Player);
            EngineSelfTests.Equal(1, fixture.Player.Relics.OfType<LanghuanCirclet>().Count(), "claiming a legacy offer actually gives the custom circlet");
            EngineSelfTests.Equal(false, fixture.Player.Relics.Any(relic => relic is LanghuanPendant or Circlet), "claiming a legacy offer removes the pendant without granting a vanilla fallback");
        }

        var nonDuanYuProps = new SavedProperties
        {
            modelIds =
            [
                new(nameof(TouchOfOrobas.UpgradedRelic), ModelDb.Relic<Circlet>().Id),
                new(nameof(TouchOfOrobas.StarterRelic), ModelDb.Relic<BurningBlood>().Id)
            ]
        };
        var otherTouch = (TouchOfOrobas)RelicModel.FromSerializable(new SerializableRelic
        {
            Id = ModelDb.Relic<TouchOfOrobas>().Id,
            Props = nonDuanYuProps
        });
        EngineSelfTests.Equal<ModelId?>(ModelDb.Relic<Circlet>().Id, otherTouch.UpgradedRelic, "migration preserves non-Duan-Yu offers even when they store vanilla Circlet");
        EngineSelfTests.Equal(false, otherTouch.HoverTipsExcludingRelic.Any(tip => tip.CanonicalModel is LanghuanCirclet), "non-Duan-Yu saved offer gains no custom hover");
        var vanilla = (Circlet)ModelDb.Relic<Circlet>().ToMutable();
        nonDuanYuProps.Fill(vanilla);
        EngineSelfTests.Equal(RelicRarity.None, vanilla.Rarity, "migration leaves non-Orobas models unchanged");
        EngineSelfTests.Equal(true, vanilla.IsStackable, "migration leaves the actual vanilla circlet stackable");
    }

    private static async Task StartingAndTurnQi()
    {
        var fixture = EngineSelfTests.CreateFixture();
        var player = fixture.Player;
        await ReplacePendant(player);
        player.PlayerCombatState!.Stars = 4;
        await Hook.BeforeCombatStart(fixture.Run, fixture.Combat);
        AssertQi(player, 5, "custom circlet combat-start grant");
        foreach (int expected in new[] { 6, 7, 8, 9, 9 })
        {
            await StartTurn(fixture, player);
            AssertQi(player, expected, "own turn-start grant including the first turn");
            await Hook.AfterSideTurnEnd(fixture.Combat, CombatSide.Player, [player.Creature]);
            AssertQi(player, expected, "circlet Qi remains stored after turn end");
        }
        EngineSelfTests.Equal(4, player.PlayerCombatState.Stars, "custom circlet never substitutes native stars for Qi");
        EngineSelfTests.Equal(99, player.PlayerCombatState.Energy, "custom circlet does not grant energy");
        EngineSelfTests.Equal(0, DuanYuState.Get(player).SpendingCount, "combat-start and turn-start gains are not payments");
    }

    private static async Task LateAcquisitionAndRewards()
    {
        var fixture = await EngineSelfTests.ReadyFixture();
        var player = fixture.Player;
        AssertQi(player, 3, "ordinary pendant starts with three Qi");
        await ReplacePendant(player);
        AssertQi(player, 3, "obtaining the circlet mid-combat does not repeat its combat-start grant");
        await RelicCmd.Obtain<TianlongPrayerBeads>(player);
        await RelicCmd.Obtain<DaliSeal>(player);
        await DuanYuState.AddEffect(player, "DY_U01");
        await DuanYuState.AddEffect(player, "DY_R01", 1, energyAmount: 1);
        await DuanYuState.LoseQi(player, int.MaxValue);
        player.PlayerCombatState!.Energy = 0;
        int draw = PileType.Draw.GetPile(player).Cards.Count;
        await StartTurn(fixture, player);
        AssertQi(player, 1, "late acquisition receives only the next turn-start grant");
        var state = DuanYuState.Get(player);
        EngineSelfTests.Equal(0, state.SpendingCount, "passive circlet grant records no payment");
        EngineSelfTests.Equal(false, state.SealUsed, "passive circlet grant preserves the first seal trigger");
        EngineSelfTests.Equal(false, state.PrayerBeadsUsedThisTurn, "passive circlet grant preserves the first prayer trigger");
        EngineSelfTests.Equal(0, player.PlayerCombatState.Energy, "passive circlet grant awards no payment energy");
        EngineSelfTests.Equal(0, player.Creature.Block, "passive circlet grant awards no payment block");
        EngineSelfTests.Equal(0, PileType.Hand.GetPile(player).Cards.Count, "passive circlet grant awards no payment draw");
        EngineSelfTests.Equal(draw, PileType.Draw.GetPile(player).Cards.Count, "passive circlet grant preserves the draw pile");
    }

    private static async Task PlayerIsolationAndMelt()
    {
        var fixture = EngineSelfTests.CreateFixture();
        var first = fixture.Player;
        var circlet = await ReplacePendant(first);
        var second = Player.CreateForNewRun(ModelDb.Character<DuanYuCharacter>(), UnlockState.all, 2);
        fixture.Run.AddPlayerDebug(second, -1);
        second.ResetCombatState();
        fixture.Combat.AddPlayer(second);
        await Hook.BeforeCombatStart(fixture.Run, fixture.Combat);
        AssertQi(first, 5, "circlet owner starts with five Qi in multiplayer");
        AssertQi(second, 3, "companion pendant still starts with three Qi");
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [first.Creature, second.Creature]);
        await Hook.BeforeHandDraw(fixture.Combat, first, new ThrowingPlayerChoiceContext());
        await Hook.BeforeHandDraw(fixture.Combat, second, new ThrowingPlayerChoiceContext());
        AssertQi(first, 6, "circlet owner receives its first-turn grant");
        AssertQi(second, 3, "companion receives no circlet turn grant");
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Enemy, [fixture.Enemy]);
        AssertQi(first, 6, "enemy turn grants no owner Qi");
        AssertQi(second, 3, "enemy turn grants no companion Qi");
        await StartTurn(fixture, first);
        AssertQi(first, 7, "an additional owner turn grants one Qi");
        AssertQi(second, 3, "an owner-only turn preserves companion Qi");
        circlet.IsWax = true;
        await RelicCmd.Melt(circlet);
        await DuanYuState.LoseQi(first, int.MaxValue);
        await StartTurn(fixture, first);
        AssertQi(first, 0, "melted circlet grants no turn-start Qi");
        await DuanYuState.StartRelicCombat(first, 9);
        AssertQi(first, 0, "melted circlet grants no combat-start Qi");
        await RelicCmd.Remove(circlet);
        await DuanYuState.StartRelicCombat(first, 9);
        AssertQi(first, 0, "removed circlet cannot grant combat-start Qi");
        AssertQi(second, 3, "melting and removing the owner relic preserves companion Qi");
    }

    private static async Task CombatLifetime()
    {
        var fixture = EngineSelfTests.CreateFixture();
        await ReplacePendant(fixture.Player);
        await Hook.BeforeCombatStart(fixture.Run, fixture.Combat);
        await DuanYuState.GainQi(fixture.Player, 9);
        await Hook.AfterCombatEnd(fixture.Run, fixture.Combat, new CombatRoom(fixture.Combat));
        EngineSelfTests.Equal(0, DuanYuState.Get(fixture.Player).Qi, "combat end clears stored circlet Qi");
        EngineSelfTests.Equal(0, CustomResources<QiResource>.Get(fixture.Player.PlayerCombatState!).Amount, "combat end clears the resource display");
        await Hook.BeforeCombatStart(fixture.Run, fixture.Combat);
        AssertQi(fixture.Player, 5, "next combat resets to five instead of adding to old Qi");
        await StartTurn(fixture, fixture.Player);
        AssertQi(fixture.Player, 6, "next combat first turn receives one additional Qi");
        foreach (bool defeat in new[] { false, true })
        {
            var inactive = EngineSelfTests.CreateFixture();
            await ReplacePendant(inactive.Player);
            await Hook.BeforeCombatStart(inactive.Run, inactive.Combat);
            var victim = defeat ? inactive.Player.Creature : inactive.Enemy;
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), victim, victim.CurrentHp,
                ValueProp.Unblockable | ValueProp.Unpowered, defeat ? inactive.Enemy : inactive.Player.Creature);
            EngineSelfTests.Equal(false, DuanYuState.CanAct(inactive.Player), "dead owner or completed combat cannot receive circlet benefits");
            int qi = DuanYuState.Get(inactive.Player).Qi;
            int resource = CustomResources<QiResource>.Get(inactive.Player.PlayerCombatState!).Amount;
            int power = inactive.Player.Creature.GetPowerAmount<QiPower>();
            await DuanYuState.StartRelicCombat(inactive.Player, 9);
            await DuanYuState.BeginTurn(inactive.Player, new ThrowingPlayerChoiceContext());
            EngineSelfTests.Equal(qi, DuanYuState.Get(inactive.Player).Qi, "inactive circlet hooks preserve the Qi ledger");
            EngineSelfTests.Equal(resource, CustomResources<QiResource>.Get(inactive.Player.PlayerCombatState!).Amount, "inactive circlet hooks preserve the resource display");
            EngineSelfTests.Equal(power, inactive.Player.Creature.GetPowerAmount<QiPower>(), "inactive circlet hooks preserve the power display");
        }
    }

    private static Task RewardPoolAndIcons()
    {
        var fixture = EngineSelfTests.CreateFixture(combat: false);
        var circlet = ModelDb.Relic<LanghuanCirclet>();
        var pool = ModelDb.RelicPool<DuanYuRelicPool>().AllRelics.ToArray();
        EngineSelfTests.Equal(1, pool.Count(relic => relic.Id == circlet.Id), "custom relic pool registers the circlet exactly once");
        EngineSelfTests.Equal(RelicRarity.Starter, circlet.Rarity, "upgraded circlet stays a starter relic");
        fixture.Player.PopulateRelicGrabBagIfNecessary(new Rng(511));
        var bag = fixture.Player.RelicGrabBag.ToSerializable();
        EngineSelfTests.Equal(true, bag.RelicIdLists.Values.Sum(ids => ids.Count) > 0, "native ordinary reward grab bag was actually populated");
        EngineSelfTests.Equal(false, bag.RelicIdLists.Values.SelectMany(ids => ids).Contains(circlet.Id), "native common uncommon rare and shop bags exclude the upgraded starter");
        EngineSelfTests.Equal(false, fixture.Player.RelicGrabBag.Contains(circlet), "native grab bag cannot pull the custom starter upgrade");
        EngineSelfTests.Equal(false, circlet.PackedIconPath == ModelDb.Relic<Circlet>().PackedIconPath, "custom circlet uses an independent icon asset");
        EngineSelfTests.Equal(true, circlet.Icon.GetWidth() > 0, "custom circlet inventory icon loads");
        EngineSelfTests.Equal(true, circlet.IconOutline.GetWidth() > 0, "custom circlet outline icon loads");
        return Task.CompletedTask;
    }

    private static async Task<LanghuanCirclet> ReplacePendant(Player player) =>
        (LanghuanCirclet)await RelicCmd.Replace(player.Relics.OfType<LanghuanPendant>().Single(), ModelDb.Relic<LanghuanCirclet>().ToMutable());

    private static async Task StartTurn(TestFixture fixture, Player player)
    {
        await Hook.BeforeSideTurnStart(fixture.Combat, CombatSide.Player, [player.Creature]);
        await Hook.BeforeHandDraw(fixture.Combat, player, new ThrowingPlayerChoiceContext());
    }

    private static void AssertQi(Player player, int expected, string context)
    {
        EngineSelfTests.Equal(expected, DuanYuState.Get(player).Qi, context + " ledger");
        EngineSelfTests.Equal(expected, CustomResources<QiResource>.Get(player.PlayerCombatState!).Amount, context + " resource display");
        EngineSelfTests.Equal(expected, player.Creature.GetPowerAmount<QiPower>(), context + " power display");
    }
}
#endif
