using System.Collections.Generic;
using UnityEngine;

public static class OpponentRoster
{
    private const int RoundBudget = 10;
    private const int CreaturePrice = 3;
    private static readonly List<CreatureInstance> Creatures = new();

    public static IReadOnlyList<CreatureInstance> Members => Creatures;
    public static int RoundNumber { get; private set; }
    public static int LastSpent { get; private set; }

    public static void Reset()
    {
        Creatures.Clear();
        RoundNumber = 0;
        LastSpent = 0;
    }

    public static void PrepareForRound(IReadOnlyList<CreatureDataSO> speciesPool,
        IReadOnlyList<CreatureItemSO> itemPool)
    {
        RoundNumber++;
        int money = RoundBudget;

        // Both sides begin a run with one free starter before spending round money.
        if (Creatures.Count == 0)
        {
            CreatureDataSO starter = RandomSpecies(speciesPool);
            if (starter != null) Creatures.Add(CreatureInstance.Generate(starter));
        }

        // Establish three bodies first, then add one per round. Leave room in the
        // same ten-gold budget to invest in the early creatures from round one.
        int growthTarget = Mathf.Min(RunRoster.MaxMembers, Mathf.Max(3, Creatures.Count + 1));
        while (Creatures.Count < growthTarget && money >= CreaturePrice)
        {
            CreatureDataSO species = RandomSpecies(speciesPool);
            if (species == null) break;
            Creatures.Add(CreatureInstance.Generate(species));
            money -= CreaturePrice;
        }

        // Buy actual duplicates and use the normal merge progression. Finish level
        // two across the team before pursuing level three; favor nearly complete levels.
        bool boughtUpgrade = false;
        while (money >= CreaturePrice)
        {
            CreatureInstance receiver = UpgradeTarget();
            if (receiver == null) break;
            int equipmentReserve = CheapestEquipmentPrice(itemPool, money);
            if (boughtUpgrade && money - CreaturePrice < equipmentReserve) break;
            CreatureInstance donor = CreatureInstance.Generate(receiver.Species);
            if (donor == null || !receiver.TryMerge(donor)) break;
            money -= CreaturePrice;
            boughtUpgrade = true;
        }

        // Spend what remains on empty held-item and spray slots. Items persist into
        // later rounds exactly like the player's equipment.
        while (money > 0)
        {
            List<CreatureItemSO> affordable = new();
            if (itemPool != null)
            {
                foreach (CreatureItemSO item in itemPool)
                {
                    if (item != null && item.price <= money && HasOpenSlot(item.slot))
                        affordable.Add(item);
                }
            }
            if (affordable.Count == 0) break;

            CreatureItemSO purchase = affordable[RunShop.ChooseItem(affordable)];
            List<CreatureInstance> recipients = new();
            foreach (CreatureInstance creature in Creatures)
            {
                bool slotOpen = purchase.slot == CreatureItemSlot.HeldItem
                    ? creature.HeldItem == null
                    : creature.Spray == null;
                if (slotOpen) recipients.Add(creature);
            }

            if (recipients.Count == 0) break;
            recipients[Random.Range(0, recipients.Count)].Equip(purchase);
            money -= purchase.price;
        }

        LastSpent = RoundBudget - money;
    }

    private static CreatureInstance UpgradeTarget()
    {
        CreatureInstance best = null;
        foreach (CreatureInstance creature in Creatures)
        {
            if (creature.Level >= CreatureInstance.MaxLevel) continue;
            if (best == null || creature.Level < best.Level ||
                (creature.Level == best.Level && creature.CopyCount > best.CopyCount)) best = creature;
        }
        return best;
    }

    private static int CheapestEquipmentPrice(IReadOnlyList<CreatureItemSO> items, int money)
    {
        int cheapest = int.MaxValue;
        if (items != null)
            foreach (CreatureItemSO item in items)
                if (item != null && item.price <= money && HasOpenSlot(item.slot))
                    cheapest = Mathf.Min(cheapest, item.price);
        return cheapest == int.MaxValue ? 0 : cheapest;
    }

    private static bool HasOpenSlot(CreatureItemSlot slot)
    {
        foreach (CreatureInstance creature in Creatures)
        {
            if (slot == CreatureItemSlot.HeldItem && creature.HeldItem == null) return true;
            if (slot == CreatureItemSlot.Spray && creature.Spray == null) return true;
        }
        return false;
    }

    private static CreatureDataSO RandomSpecies(IReadOnlyList<CreatureDataSO> speciesPool)
    {
        if (speciesPool == null) return null;
        List<CreatureDataSO> valid = new();
        foreach (CreatureDataSO species in speciesPool)
            if (species != null) valid.Add(species);
        return valid.Count > 0 ? valid[Random.Range(0, valid.Count)] : null;
    }
}
