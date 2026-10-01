using UnityEngine;

public enum DamageKind { Normal, Fire, GroundFire }

// Per-creature state: shared ability assets never store timers or consumed charges.
public sealed class CreatureAbilityRuntime
{
    private readonly CreatureController owner;
    private readonly PassiveAbilitySO ability;
    private float hotFeetUntil = float.NegativeInfinity;
    private float lastHit = float.NegativeInfinity;
    private bool secondWindUsed, aftershockReady;
    public CreatureAbilityRuntime(CreatureController owner)
    {
        this.owner = owner;
        ability = owner.Ability as PassiveAbilitySO;
    }
    private bool Is(PassiveAbilityKind kind) => ability != null && ability.effect == kind;
    private static float Distance(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0;
        return Vector3.Distance(a, b);
    }
    public float MovementMultiplier => Is(PassiveAbilityKind.HotFeet) && Time.time < hotFeetUntil ? 1f + ability.strength : 1f;
    public float AttackSpeedMultiplier
    {
        get
        {
            if (!Is(PassiveAbilityKind.LastStand) || owner.Registry == null || owner.IsDead) return 1f;
            foreach (var ally in owner.Registry.GetAllies(owner.Team))
                if (ally != null && ally != owner && ally.isActiveAndEnabled && !ally.IsDead) return 1f;
            return 1f + ability.strength;
        }
    }
    public float DefenseBonus
    {
        get
        {
            if (!Is(PassiveAbilityKind.Bodyguard) || owner.Registry == null) return 0f;
            float health = owner.GetComponent<StatsComponent>().HealthFraction;
            foreach (var ally in owner.Registry.GetAllies(owner.Team))
                if (ally != null && ally != owner && ally.isActiveAndEnabled && !ally.IsDead &&
                    Distance(owner.transform.position, ally.transform.position) <= ability.radius &&
                    ally.GetComponent<StatsComponent>().HealthFraction < health) return ability.strength;
            return 0f;
        }
    }
    public float OutgoingMultiplier(CreatureController target, float projectileDistance)
    {
        if (Is(PassiveAbilityKind.CloseQuarters) && target != null &&
            Distance(owner.transform.position, target.transform.position) <= ability.radius) return 1f + ability.strength;
        if (Is(PassiveAbilityKind.LongShot) && projectileDistance > 0f)
            return 1f + ability.strength * Mathf.Clamp01(projectileDistance / Mathf.Max(0.1f, ability.radius));
        return 1f;
    }
    public float IncomingMultiplier(DamageKind kind)
    {
        if (Is(PassiveAbilityKind.Fireproof))
        {
            if (kind == DamageKind.GroundFire) return 0f;
            if (kind == DamageKind.Fire) return 1f - Mathf.Clamp01(ability.strength);
        }
        if (Is(PassiveAbilityKind.ThickSkin) && Time.time - lastHit <= ability.duration)
            return 1f - Mathf.Clamp01(ability.strength);
        return 1f;
    }
    public void OnMoveUsed()
    {
        if (Is(PassiveAbilityKind.Aftershock)) aftershockReady = true;
    }
    public void OnDamaged(float damage)
    {
        if (damage <= 0f) return;
        lastHit = Time.time;
        if (owner.IsDead) return;
        if (Is(PassiveAbilityKind.HotFeet)) hotFeetUntil = Time.time + ability.duration;
        var stats = owner.GetComponent<StatsComponent>();
        if (Is(PassiveAbilityKind.SecondWind) && !secondWindUsed && stats.HealthFraction < 0.3f)
        {
            secondWindUsed = true;
            stats.Heal(stats.MaxHP * ability.strength, owner);
        }
    }
    public void OnHit(CreatureController target, float damage)
    {
        if (damage <= 0f || target == null || owner.IsDead) return;
        var movement = target.GetComponent<MovementComponent>();
        if (Is(PassiveAbilityKind.StickySituation) && movement != null && movement.IsSlowed)
            owner.GetComponent<StatsComponent>().Heal(damage * ability.strength, owner);
        if (Is(PassiveAbilityKind.Aftershock) && aftershockReady)
        {
            aftershockReady = false;
            if (!target.IsDead) movement?.ApplySlow(1f - Mathf.Clamp01(ability.strength), ability.duration);
        }
    }
    public void OnKnockout(CreatureController victim, CreatureController source)
    {
        if (victim == null || victim.Team == owner.Team || owner.IsDead || !owner.isActiveAndEnabled) return;
        if (Is(PassiveAbilityKind.Scavenger) && Distance(owner.transform.position, victim.transform.position) <= ability.radius)
        {
            var stats = owner.GetComponent<StatsComponent>();
            stats.Heal(stats.MaxHP * ability.strength, owner);
        }
        if (Is(PassiveAbilityKind.ChainReaction) && source == owner && owner.Registry != null)
        {
            if (Application.isPlaying)
                CombatVfxBurst.Spawn(victim.transform.position + Vector3.up * 0.5f, new Color(1f, 0.5f, 0.12f), 18, ability.radius * 2f, 0.4f);
            // Snapshot allows nested knockouts without invalidating the collection.
            var opponents = new System.Collections.Generic.List<CreatureController>(owner.Registry.GetOpponents(owner.Team));
            foreach (var opponent in opponents)
                if (opponent != null && !opponent.IsDead && opponent.isActiveAndEnabled &&
                    Distance(victim.transform.position, opponent.transform.position) <= ability.radius)
                    opponent.GetComponent<StatsComponent>()?.TakeDamage(ability.strength * owner.PowerMultiplier, owner);
        }
    }
}
