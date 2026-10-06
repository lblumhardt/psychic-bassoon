using System.Collections.Generic;

public static class RunRoster
{
    public const int MaxMembers = 5;
    public const int MoveSlots = 2;
    private static readonly List<CreatureInstance> Creatures = new();

    public static IReadOnlyList<CreatureInstance> Members => Creatures;
    public static bool Initialized { get; private set; }

    public static void Reset()
    {
        Creatures.Clear();
        Initialized = false;
    }

    public static void InitializeIfEmpty(CreatureDataSO starter)
    {
        if (!Initialized && Creatures.Count == 0 && starter != null)
        {
            TryAdd(CreatureInstance.Generate(starter));
        }
    }

    public static bool TryAdd(CreatureInstance creature)
    {
        if (creature == null || creature.IsConsumed || creature.Species == null ||
            Creatures.Contains(creature) || Creatures.Count >= MaxMembers) return false;
        Creatures.Add(creature);
        Initialized = true;
        return true;
    }

    public static bool Remove(CreatureInstance creature) => Creatures.Remove(creature);

    public static void ClearNextRoundBonuses()
    {
        foreach (CreatureInstance creature in Creatures) creature.ClearNextRoundBonuses();
    }

    public static bool ApplyPartyGoop()
    {
        if (Creatures.Count == 0) return false;
        int first = UnityEngine.Random.Range(0, Creatures.Count);
        Creatures[first].AddStatBonus(1, 1);
        if (Creatures.Count > 1)
        {
            int second = UnityEngine.Random.Range(0, Creatures.Count - 1);
            if (second >= first) second++;
            Creatures[second].AddStatBonus(1, 1);
        }
        return true;
    }

    public static CreatureInstance SellAt(int index)
    {
        if (Creatures.Count <= 1 || index < 0 || index >= Creatures.Count) return null;
        CreatureInstance sold = Creatures[index];
        Creatures.RemoveAt(index);
        return sold;
    }

    public static bool TryMerge(CreatureInstance donor, CreatureInstance receiver,
        IReadOnlyList<AttackDataSO> selectedMoves = null)
    {
        if (!Creatures.Contains(donor) || !Creatures.Contains(receiver) ||
            !receiver.TryMerge(donor, selectedMoves)) return false;
        Creatures.Remove(donor);
        return true;
    }
}
