using System;

public static class RunShop
{
    public static readonly CreatureInstance[] Creatures = new CreatureInstance[3];
    public static readonly CreatureItemSO[] Items = new CreatureItemSO[2];
    public static readonly bool[] CreatureLocked = new bool[3];
    public static readonly bool[] ItemLocked = new bool[2];
    public static int CreatureStatBonus { get; private set; }
    public static int Money { get; set; }
    public static bool OffersPrepared { get; set; }
    private static int budgetRound = -1;

    public static bool RefreshBudget(int completedRounds, int amount)
    {
        if (budgetRound == completedRounds) return false;
        budgetRound = completedRounds;
        Money = amount;
        return true;
    }

    public static CreatureInstance GenerateCreature(CreatureDataSO species)
    {
        CreatureInstance creature = CreatureInstance.Generate(species);
        creature?.AddStatBonus(CreatureStatBonus, CreatureStatBonus);
        return creature;
    }

    public static void ApplyHelpWantedSign()
    {
        CreatureStatBonus++;
        foreach (CreatureInstance creature in Creatures) creature?.AddStatBonus(1, 1);
    }

    public static int ChooseItem(System.Collections.Generic.IReadOnlyList<CreatureItemSO> items)
    {
        float total = 0f;
        foreach (CreatureItemSO item in items) total += UnityEngine.Mathf.Max(0.01f, item.shopWeight);
        float roll = UnityEngine.Random.value * total;
        for (int i = 0; i < items.Count; i++)
        {
            roll -= UnityEngine.Mathf.Max(0.01f, items[i].shopWeight);
            if (roll <= 0f) return i;
        }
        return items.Count - 1;
    }

    public static void Reset()
    {
        CreatureStatBonus = 0;
        Money = 0; budgetRound = -1;
        OffersPrepared = false;
        Array.Clear(Creatures, 0, Creatures.Length);
        Array.Clear(Items, 0, Items.Length);
        Array.Clear(CreatureLocked, 0, CreatureLocked.Length);
        Array.Clear(ItemLocked, 0, ItemLocked.Length);
    }
}
