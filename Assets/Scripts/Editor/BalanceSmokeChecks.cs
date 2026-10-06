using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class BalanceSmokeChecks
{
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly List<UnityEngine.Object> created = new();
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static CreatureController Make(Team team, CreatureDataSO species)
    {
        var obj = new GameObject("Balance check"); created.Add(obj);
        obj.AddComponent<StatsComponent>(); obj.AddComponent<CombatComponent>();
        var targeting = obj.AddComponent<TargetingComponent>();
        obj.AddComponent<BoxCollider>();
        var creature = obj.AddComponent<CreatureController>();
        typeof(CreatureController).GetMethod("Awake", PrivateInstance).Invoke(creature, null);
        typeof(TargetingComponent).GetMethod("Awake", PrivateInstance).Invoke(targeting, null);
        creature.SetTeam(team);
        creature.Configure(species, Array.Empty<AttackDataSO>(), null, new CreatureStats(100, 5, 10, 5, 5), null, null);
        return creature;
    }

    [MenuItem("Tools/Mons/Check Balance Changes")]
    public static void Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Run checks in edit mode.");
        try
        {
            var species = ScriptableObject.CreateInstance<CreatureDataSO>(); created.Add(species);
            var source = Make(Team.Player, species);
            var victim = Make(Team.Enemy, species);
            var friend = Make(Team.Player, species);
            var projectileObject = new GameObject("Rebar check"); created.Add(projectileObject);
            var rebar = projectileObject.AddComponent<RebarProjectile>();
            typeof(RebarProjectile).GetField("_hasLaunched", PrivateInstance).SetValue(rebar, true);
            typeof(RebarProjectile).GetField("_owner", PrivateInstance).SetValue(rebar, source.transform);
            typeof(RebarProjectile).GetField("_damage", PrivateInstance).SetValue(rebar, 5f);
            var hit = typeof(RebarProjectile).GetMethod("HandleTrigger", PrivateInstance);
            hit.Invoke(rebar, new object[] { victim.GetComponent<Collider>() });
            Require(Mathf.Abs(victim.GetComponent<StatsComponent>().CurrentHP - 97.5f) < 0.001f,
                "Rebar must apply its attack damage through defense.");
            hit.Invoke(rebar, new object[] { victim.GetComponent<Collider>() });
            hit.Invoke(rebar, new object[] { friend.GetComponent<Collider>() });
            Require(victim.GetComponent<StatsComponent>().CurrentHP == 97.5f && friend.GetComponent<StatsComponent>().CurrentHP == 100f,
                "Rebar must damage a victim once and ignore teammates.");

            var registryObject = new GameObject("Range registry"); created.Add(registryObject);
            var registry = registryObject.AddComponent<CreatureRegistry>();
            source.SetRegistry(registry); victim.SetRegistry(registry);
            var ready = typeof(CombatComponent).GetMethod("IsReady", PrivateInstance);
            var stomp = AssetDatabase.LoadAssetAtPath<AttackDataSO>("Assets/ScriptableObjects/Attacks/Stomp.asset");
            victim.transform.position = Vector3.right * (stomp.range + 1f);
            Require(!(bool)ready.Invoke(source.GetComponent<CombatComponent>(), new object[] { stomp }),
                "Short-range moves must not be cast at distant targets.");
            victim.transform.position = Vector3.right;
            Require((bool)ready.Invoke(source.GetComponent<CombatComponent>(), new object[] { stomp }),
                "Moves must become available once their target is in range.");

            RunShop.Reset();
            Require(RunShop.RefreshBudget(0, 10), "First visit must grant a budget.");
            RunShop.Money = 3;
            Require(!RunShop.RefreshBudget(0, 10) && RunShop.Money == 3,
                "Returning from sandbox must not refill gold.");
            Require(RunShop.RefreshBudget(1, 10) && RunShop.Money == 10,
                "A new completed round must grant the next budget.");

            SandboxSession.TrialCount = 10;
            SandboxSession.BeginTrials();
            Require(SandboxSession.RecordTrial(RoundResult.Win, 5f, Array.Empty<CreatureController>()), "Batch stopped early.");
            for (int i = 1; i < 9; i++) SandboxSession.RecordTrial(RoundResult.Loss, 6f, Array.Empty<CreatureController>());
            Require(!SandboxSession.RecordTrial(RoundResult.None, 60f, Array.Empty<CreatureController>()) &&
                SandboxSession.Wins == 1 && SandboxSession.Losses == 8 && SandboxSession.Timeouts == 1,
                "Batch must stop at the requested count and track timeouts separately.");
            SandboxSession.BeginTrials();
            Require(SandboxSession.TrialsCompleted == 0 && SandboxSession.Wins == 0 && SandboxSession.PlayerDamage == 0,
                "Replay must start a fresh result set.");
            Debug.Log("ALL BALANCE CHANGE CHECKS PASSED");
        }
        finally
        {
            SandboxSession.Active = false;
            SandboxSession.TrialCount = 1;
            SandboxSession.LastBatchSummary = "";
            RunShop.Reset();
            for (int i = created.Count - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(created[i]);
            created.Clear();
        }
    }
}
