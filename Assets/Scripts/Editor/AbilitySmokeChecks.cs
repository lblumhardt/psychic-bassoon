using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Run in an isolated empty project/scene; does not depend on art or authored assets.
public static class AbilitySmokeChecks
{
    private static readonly List<UnityEngine.Object> created = new();
    private static CreatureRegistry registry;
    private static CreatureController Make(Team team, PassiveAbilityKind? kind = null,
        float strength = 0, float duration = 0, float radius = 0, CreatureItemSO spray = null)
    {
        var obj = new GameObject("Ability check");
        created.Add(obj);
        obj.AddComponent<StatsComponent>();
        obj.AddComponent<MovementComponent>();
        obj.AddComponent<CombatComponent>();
        obj.AddComponent<TargetingComponent>();
        var creature = obj.AddComponent<CreatureController>();
        // Edit-mode checks initialize references without dispatching engine lifecycle messages.
        typeof(CreatureController).GetMethod("Awake", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic).Invoke(creature, null);
        var species = ScriptableObject.CreateInstance<CreatureDataSO>();
        created.Add(species);
        PassiveAbilitySO ability = null;
        if (kind.HasValue)
        {
            ability = ScriptableObject.CreateInstance<PassiveAbilitySO>();
            created.Add(ability);
            ability.effect = kind.Value;
            ability.strength = strength;
            ability.duration = duration;
            ability.radius = radius;
            Require(!string.IsNullOrEmpty(ability.Description), "Missing description");
        }
        creature.Configure(species, Array.Empty<AttackDataSO>(), ability, new CreatureStats(100, 5, 10, 5, 5), null, spray);
        creature.SetTeam(team);
        creature.SetRegistry(registry);
        return creature;
    }
    private static StatsComponent Stats(CreatureController c) => c.GetComponent<StatsComponent>();
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static void Near(float actual, float expected, string message) => Require(Mathf.Abs(actual - expected) < 0.001f, $"{message}: {actual} != {expected}");
    private static void Check(string name, Action test)
    {
        try
        {
            var root = new GameObject("Test registry");
            created.Add(root);
            registry = root.AddComponent<CreatureRegistry>();
            test();
            Debug.Log("ABILITY PASS: " + name);
        }
        finally
        {
            for (int i = created.Count - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(created[i]);
            created.Clear();
        }
    }
    public static void Run()
    {
        try
        {
            Check("Hot Feet and independent state", () => {
                var a = Make(Team.Player, PassiveAbilityKind.HotFeet, 0.5f, 2);
                var b = Make(Team.Player, PassiveAbilityKind.HotFeet, 0.5f, 2);
                Stats(a).TakeDamage(10);
                Near(a.MoveSpeedMultiplier, 1.5f, "Speed boost");
                Near(b.MoveSpeedMultiplier, 1, "State leaked");
            });
            Check("Thick Skin", () => {
                var a = Make(Team.Player, PassiveAbilityKind.ThickSkin, 0.35f, 0.75f);
                Near(Stats(a).TakeDamage(10), 5, "First hit");
                Near(Stats(a).TakeDamage(10), 3.25f, "Repeated hit");
            });
            Check("Second Wind once and no resurrection", () => {
                var a = Make(Team.Player, PassiveAbilityKind.SecondWind, 0.3f);
                Stats(a).TakeDamage(150);
                Near(Stats(a).CurrentHP, 55, "Heal threshold");
                Stats(a).TakeDamage(70);
                Near(Stats(a).CurrentHP, 20, "Healed twice");
                var b = Make(Team.Player, PassiveAbilityKind.SecondWind, 0.3f);
                Stats(b).TakeDamage(300);
                Require(b.IsDead, "Resurrected");
            });
            Check("Aftershock single charge", () => {
                var a = Make(Team.Player, PassiveAbilityKind.Aftershock, 0.3f, 1.5f);
                var b = Make(Team.Enemy); var c = Make(Team.Enemy);
                a.AbilityRuntime.OnMoveUsed();
                Stats(b).TakeDamage(10, a); Stats(c).TakeDamage(10, a);
                Require(b.GetComponent<MovementComponent>().IsSlowed, "No slow");
                Require(!c.GetComponent<MovementComponent>().IsSlowed, "Charge reused");
            });
            Check("Close Quarters", () => {
                var a = Make(Team.Player, PassiveAbilityKind.CloseQuarters, 0.25f, 0, 3);
                var b = Make(Team.Enemy);
                Near(Stats(b).TakeDamage(10, a), 6.25f, "Near bonus");
                b.transform.position = Vector3.right * 10;
                Near(Stats(b).TakeDamage(10, a), 5, "Far bonus");
            });
            Check("Long Shot", () => {
                var a = Make(Team.Player, PassiveAbilityKind.LongShot, 0.5f, 0, 10);
                var b = Make(Team.Enemy);
                Near(Stats(b).TakeDamage(10, a, projectileDistance: 5), 6.25f, "Distance scaling");
                Near(Stats(b).TakeDamage(10, a, projectileDistance: 20), 7.5f, "Bonus cap");
                Near(Stats(b).TakeDamage(10, a), 5, "Non projectile bonus");
            });
            Check("Fireproof", () => {
                var a = Make(Team.Player, PassiveAbilityKind.Fireproof, 0.5f);
                Near(Stats(a).TakeDamage(10, kind: DamageKind.GroundFire), 0, "Ground immunity");
                Near(Stats(a).TakeDamage(10, kind: DamageKind.Fire), 2.5f, "Fire resistance");
                Near(Stats(a).TakeDamage(10), 5, "Normal damage");
            });
            Check("Sticky Situation", () => {
                var a = Make(Team.Player, PassiveAbilityKind.StickySituation, 0.25f);
                var b = Make(Team.Enemy); Stats(a).TakeDamage(40);
                Stats(b).TakeDamage(10, a); Near(Stats(a).CurrentHP, 80, "Unslowed healing");
                b.GetComponent<MovementComponent>().ApplySlow(0.5f, 5);
                Stats(b).TakeDamage(10, a); Near(Stats(a).CurrentHP, 81.25f, "Lifesteal");
            });
            Check("Bodyguard", () => {
                var a = Make(Team.Player, PassiveAbilityKind.Bodyguard, 5, 0, 3);
                var b = Make(Team.Player); Stats(b).TakeDamage(100);
                Near(Stats(a).TakeDamage(10), 4, "Defense bonus");
                b.transform.position = Vector3.right * 10;
                Near(Stats(a).TakeDamage(10), 5, "Out of range");
            });
            Check("Last Stand", () => {
                var a = Make(Team.Player, PassiveAbilityKind.LastStand, 0.5f);
                var b = Make(Team.Player);
                Near(a.AttackSpeedMultiplier, 1, "Living ally");
                Stats(b).TakeDamage(300);
                Near(a.AttackSpeedMultiplier, 1.5f, "Last survivor");
            });
            Check("Scavenger one notification", () => {
                var a = Make(Team.Player, PassiveAbilityKind.Scavenger, 0.15f, 0, 5);
                var b = Make(Team.Enemy); Stats(a).TakeDamage(100);
                Stats(b).TakeDamage(300); Stats(b).TakeDamage(300);
                Near(Stats(a).CurrentHP, 65, "Scavenger heal");
            });
            Check("Chain Reaction", () => {
                var a = Make(Team.Player, PassiveAbilityKind.ChainReaction, 3, 0, 2.5f);
                var b = Make(Team.Enemy); var c = Make(Team.Enemy); var d = Make(Team.Enemy);
                c.transform.position = Vector3.right * 2; d.transform.position = Vector3.right * 4;
                Stats(c).TakeDamage(198); Stats(d).TakeDamage(198);
                Stats(b).TakeDamage(300, a);
                Require(b.IsDead && c.IsDead && d.IsDead, "Burst did not chain");
                Near(Stats(a).CurrentHP, 100, "Friendly fire");
            });
            Check("Spray assets and stat bonuses", () => {
                var rage = Resources.Load<CreatureItemSO>("Items/RageConcentrate");
                var mirror = Resources.Load<CreatureItemSO>("Items/LiquidMirror");
                Near(rage.ApplyStats(new CreatureStats(100, 5, 10, 5, 5)).power, 8, "Rage attack");
                Near(mirror.ApplyStats(new CreatureStats(100, 5, 10, 5, 5)).defense, 12, "Mirror defense");
                foreach (string name in new[] { "AngelAsh", "FlowerScent", "CourageCologne", "ConfidenceCologne" })
                    Require(Resources.Load<CreatureItemSO>("Items/" + name).slot == CreatureItemSlot.Spray, "Wrong slot");
            });
            Check("Courage blocks only the first hit", () => {
                var spray = Resources.Load<CreatureItemSO>("Items/CourageCologne");
                var a = Make(Team.Player, spray: spray);
                var b = Make(Team.Player, spray: spray);
                Near(Stats(a).TakeDamage(10), 0, "First small hit");
                Near(Stats(a).TakeDamage(100), 50, "Second hit");
                Near(Stats(b).TakeDamage(100), 30, "Independent first hit");
            });
            Check("Confidence applies once per move", () => {
                var a = Make(Team.Player, spray: Resources.Load<CreatureItemSO>("Items/ConfidenceCologne"));
                var move = ScriptableObject.CreateInstance<AttackDataSO>(); created.Add(move); move.damage = 10;
                var context = new AttackContext { caster = a.transform, attackData = move, bonusDamage = a.ConsumeAttackBonus() };
                Near(context.Damage, 30, "First move");
                Near(context.Damage, 30, "Damage remains fixed for multihit move");
                Near(a.ConsumeAttackBonus(), 0, "Consumed bonus");
                context.randomCast = true;
                Near(context.Damage, 15, "Half damage copy");
            });
            Check("Angel Ash delays defeat and revives once", () => {
                var a = Make(Team.Player, spray: Resources.Load<CreatureItemSO>("Items/AngelAsh"));
                Stats(a).TakeDamage(1000);
                Require(a.IsDead && a.IsReviving, "Revival not pending");
                Near(Stats(a).TakeDamage(100), 0, "Damage while dead");
                var living = typeof(BattleManager).GetMethod("HasLivingCreature", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                Require((bool)living.Invoke(null, new object[] { registry.playerCreatures }), "Round ended while revival pending");
                typeof(CreatureController).GetField("_reviveAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(a, Time.time - 1f);
                typeof(CreatureController).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(a, null);
                Near(Stats(a).CurrentHP, 1, "Revived HP");
                Require(!a.IsReviving && !a.IsDead, "Revival did not complete");
                Stats(a).TakeDamage(1000);
                Require(a.IsDead && !a.IsReviving, "Revived twice");
            });
            Debug.Log("ALL ABILITY AND SPRAY CHECKS PASSED");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }
}
