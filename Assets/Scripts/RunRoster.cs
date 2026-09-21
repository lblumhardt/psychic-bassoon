using System.Collections.Generic;

public static class RunRoster
{
    public const int MaxMembers = 5;
    public const int MoveSlots = 2;
    private static readonly List<CreatureInstance> Creatures = new();

    public static IReadOnlyList<CreatureInstance> Members => Creatures;

    public static void Reset()
    {
        Creatures.Clear();
    }

    public static void InitializeIfEmpty(CreatureDataSO starter)
    {
        if (Creatures.Count == 0 && starter != null)
        {
            TryAdd(CreatureInstance.Generate(starter));
        }
    }

    public static bool TryAdd(CreatureInstance creature)
    {
        if (creature == null || creature.Species == null || Creatures.Count >= MaxMembers) return false;
        Creatures.Add(creature);
        return true;
    }

    public static CreatureInstance SellAt(int index)
    {
        if (Creatures.Count <= 1 || index < 0 || index >= Creatures.Count) return null;
        CreatureInstance sold = Creatures[index];
        Creatures.RemoveAt(index);
        return sold;
    }
}
