using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Run from Tools/Mons/Check Consumables in edit mode, before starting a run.
public static class ConsumableSmokeChecks
{
    private static readonly List<UnityEngine.Object> created = new();
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static CreatureDataSO Species()
    {
        var species = ScriptableObject.CreateInstance<CreatureDataSO>();
        species.baseStats = new CreatureStats(100, 10, 10, 5, 5);
        created.Add(species);
        return species;
    }

    [MenuItem("Tools/Mons/Check Consumables")]
    public static void Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Run consumable checks in edit mode.");
        UnityEngine.Random.State randomState = UnityEngine.Random.state;
        try
        {
            RunRoster.Reset();
            RunShop.Reset();
            var species = Species();
            var creature = CreatureInstance.Generate(species);
            CreatureStats original = creature.Stats;
            creature.AddStatBonus(1, 1);
            creature.AddStatBonus(2, 2);
            creature.AddStatBonus(4, 4, true);
            creature.AddStatBonus(4, 4, true);
            Require(creature.Stats.power == original.power + 11 && creature.Stats.defense == original.defense + 11,
                "Permanent and next-round bonuses must stack.");
            creature.ClearNextRoundBonuses();
            Require(creature.Stats.power == original.power + 3 && creature.Stats.defense == original.defense + 3,
                "Expiry removed permanent bonuses or left temporary bonuses.");
            var twin = creature.CreateTwinDonor();
            Require(twin.CopyCount == 1 && twin.EquippedMoves.Count == creature.EquippedMoves.Count,
                "Twin donor must contribute exactly one copy with existing moves only.");
            Require(creature.TryMerge(twin) && creature.CopyCount == 2 && creature.Level == 1,
                "First twin must add one point without prematurely leveling.");
            Require(creature.TryMerge(creature.CreateTwinDonor()) && creature.Level == 2,
                "Second twin must reach level 2.");
            Require(creature.Stats.power == creature.BaseStats.power + 3 && creature.Stats.defense == creature.BaseStats.defense + 3,
                "Leveling must retain flat permanent bonuses.");

            RunRoster.TryAdd(creature);
            var other = CreatureInstance.Generate(species);
            RunRoster.TryAdd(other);
            int firstPower = creature.Stats.power, secondPower = other.Stats.power;
            Require(RunRoster.ApplyPartyGoop() && creature.Stats.power == firstPower + 1 && other.Stats.power == secondPower + 1,
                "Party Goop must choose two different creatures.");
            RunRoster.Remove(other);
            firstPower = creature.Stats.power;
            Require(RunRoster.ApplyPartyGoop() && creature.Stats.power == firstPower + 1,
                "A sole creature must not receive Party Goop twice.");

            RunShop.Creatures[0] = RunShop.GenerateCreature(species);
            RunShop.CreatureLocked[0] = true;
            int frozenPower = RunShop.Creatures[0].Stats.power;
            firstPower = creature.Stats.power;
            RunShop.ApplyHelpWantedSign(); RunShop.ApplyHelpWantedSign();
            Require(RunShop.Creatures[0].Stats.power == frozenPower + 2 && creature.Stats.power == firstPower,
                "Signs must buff frozen offers, without buffing the existing roster.");
            var future = RunShop.GenerateCreature(species);
            Require(future.Stats.power == future.BaseStats.power + 2 && future.Stats.defense == future.BaseStats.defense + 2,
                "Future offers must inherit every sign bonus.");
            RunRoster.Remove(creature);
            RunRoster.InitializeIfEmpty(species);
            Require(RunRoster.Members.Count == 0 && RunRoster.Initialized,
                "Removing the last creature must not regenerate the starter.");
            Require(!RunRoster.ApplyPartyGoop(), "Empty rosters cannot receive Party Goop.");

            var goop = Resources.Load<CreatureItemSO>("Items/Goop");
            foreach (string name in new[] { "Goop", "HyperGoop", "Botulinum", "PartyGoop", "HelpWantedSign", "DoubleGoop", "TwinBrother" })
            {
                var item = Resources.Load<CreatureItemSO>("Items/" + name);
                Require(item != null && item.slot == CreatureItemSlot.Consumable && item.consumableEffect != ConsumableEffect.None,
                    "Missing or invalid consumable asset: " + name);
                Require(item.shopWeight > 0 && item.price >= 0, "Invalid price or rarity: " + name);
                creature.Equip(item);
                Require(creature.HeldItem == null && creature.Spray == null, "Consumables must never be equipped.");
            }
            Require(Resources.Load<CreatureItemSO>("Items/DoubleGoop").shopWeight < goop.shopWeight,
                "Double Goop must be rarer than Goop.");
            RunShop.Reset();
            Require(RunShop.CreatureStatBonus == 0 && RunShop.Creatures[0] == null,
                "New runs must reset shop bonuses and offers.");
            Debug.Log("ALL CONSUMABLE CHECKS PASSED");
        }
        finally
        {
            foreach (UnityEngine.Object obj in created) UnityEngine.Object.DestroyImmediate(obj);
            created.Clear();
            RunRoster.Reset(); RunShop.Reset();
            UnityEngine.Random.state = randomState;
        }
    }
}
