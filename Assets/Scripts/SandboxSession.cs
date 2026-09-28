using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SandboxSession
{
    public static bool Active;
    public static bool OpenEditor;
    public static readonly List<SandboxCreature> Player = new();
    public static readonly List<SandboxCreature> Enemy = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Active = OpenEditor = false;
        Player.Clear();
        Enemy.Clear();
    }
}

public class SandboxCreature
{
    public CreatureDataSO Species;
    public AttackDataSO[] Moves = new AttackDataSO[2];
    public CreatureAbilitySO Ability;
    public CreatureStats BaseStats;
    public CreatureItemSO Item;
    public CreatureItemSO Spray;
    public int Level = 1;

    public SandboxCreature(CreatureDataSO species)
    {
        Species = species;
        BaseStats = species.baseStats;
        Ability = species.potentialAbilities?.FirstOrDefault(a => a != null);
        var defaults = species.movePool?.Where(m => m != null).Take(2).ToArray();
        if (defaults != null) System.Array.Copy(defaults, Moves, defaults.Length);
    }

    public CreatureStats Stats
    {
        get
        {
            var stats = BaseStats.AtLevel(Level);
            if (Item != null) stats = Item.ApplyStats(stats);
            if (Spray != null) stats = Spray.ApplyStats(stats);
            return stats;
        }
    }

    public void Configure(CreatureController controller)
    {
        controller.Configure(Species, Moves, Ability, Stats, Item, Spray, Level);
    }
}
