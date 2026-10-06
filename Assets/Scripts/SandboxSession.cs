using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SandboxSession
{
    public static bool Active;
    public static bool OpenEditor;
    public static bool ReturnToShop;
    public static readonly List<SandboxCreature> Player = new();
    public static readonly List<SandboxCreature> Enemy = new();
    public static int Seed = 12345;
    public static int TrialCount = 1;
    public static int TrialsCompleted, Wins, Losses, Timeouts;
    public static float Speed = 4f;
    public static float TimeLimit = 60f;
    public static float TotalSeconds, PlayerDamage, EnemyDamage;
    public static string LastBatchSummary = "";

    public static void BeginTrials()
    {
        Active = true;
        TrialsCompleted = Wins = Losses = Timeouts = 0;
        TotalSeconds = PlayerDamage = EnemyDamage = 0f;
    }

    public static bool RecordTrial(RoundResult result, float seconds, IReadOnlyList<CreatureController> participants)
    {
        TrialsCompleted++;
        if (result == RoundResult.Win) Wins++;
        else if (result == RoundResult.Loss) Losses++;
        else Timeouts++;
        TotalSeconds += seconds;
        foreach (CreatureController creature in participants)
        {
            if (creature == null) continue;
            float damage = creature.GetComponent<StatsComponent>()?.DamageDealt ?? 0f;
            if (creature.Team == Team.Player) PlayerDamage += damage;
            else EnemyDamage += damage;
        }
        LastBatchSummary = $"{TrialsCompleted} tests · {Wins} wins / {Losses} losses / {Timeouts} timeouts\n" +
            $"Average: {TotalSeconds / TrialsCompleted:0.0}s game time · Your damage {PlayerDamage / TrialsCompleted:0.0} / Enemy damage {EnemyDamage / TrialsCompleted:0.0}";
        return TrialsCompleted < TrialCount;
    }

    public static void LoadRunTeams()
    {
        Player.Clear(); Enemy.Clear();
        foreach (CreatureInstance creature in RunRoster.Members) Player.Add(SandboxCreature.FromInstance(creature));
        foreach (CreatureInstance creature in OpponentRoster.Members) Enemy.Add(SandboxCreature.FromInstance(creature));
        if (Enemy.Count == 0) foreach (SandboxCreature creature in Player) Enemy.Add(creature.Copy());
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Active = OpenEditor = ReturnToShop = false;
        Player.Clear();
        Enemy.Clear();
        Seed = 12345; TrialCount = 1; Speed = 4f; TimeLimit = 60f;
        TrialsCompleted = Wins = Losses = Timeouts = 0;
        LastBatchSummary = "";
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
    public int AddedPower, AddedDefense;

    public SandboxCreature Copy()
    {
        var copy = (SandboxCreature)MemberwiseClone();
        copy.Moves = (AttackDataSO[])Moves.Clone();
        return copy;
    }

    public static SandboxCreature FromInstance(CreatureInstance creature) => new SandboxCreature(creature.Species)
    {
        Moves = creature.EquippedMoves.ToArray(), Ability = creature.Ability, BaseStats = creature.OriginalStats,
        Item = creature.HeldItem, Spray = creature.Spray, Level = creature.Level,
        AddedPower = creature.AddedPower, AddedDefense = creature.AddedDefense
    };

    public SandboxCreature(CreatureDataSO species)
    {
        Species = species;
        BaseStats = species.baseStats;
        Ability = species.potentialAbilities?.FirstOrDefault(a => a != null);
        var defaults = species.movePool?.Where(m => m != null && !m.DebugOnly).Take(2).ToArray();
        if (defaults != null) System.Array.Copy(defaults, Moves, defaults.Length);
    }

    public CreatureStats Stats
    {
        get
        {
            var stats = BaseStats.AtLevel(Level);
            stats.power += AddedPower;
            stats.defense += AddedDefense;
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
