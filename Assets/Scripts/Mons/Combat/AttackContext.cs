using UnityEngine;

public struct AttackContext
{
    public Transform caster;
    public Transform target;
    public CombatComponent combatComponent;
    public AttackDataSO attackData;
    public bool isPlus;
    public float bonusDamage;
    public bool randomCast;
    public Vector3 randomDirection;
    public Vector3 AimPosition => randomCast
        ? caster.position + randomDirection * Mathf.Max(1f, attackData != null ? attackData.range : 1f)
        : target != null ? target.position : caster.position + caster.forward;

    // Direct-target moves must hit along the random ray rather than reuse their normal victim.
    public Transform ResolveTarget()
    {
        if (!randomCast) return target;
        RaycastHit[] hits = Physics.RaycastAll(caster.position + Vector3.up * 0.5f,
            randomDirection, Mathf.Max(0f, attackData.range), ~0, QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        Transform result = null;
        foreach (RaycastHit hit in hits)
        {
            CreatureController creature = hit.collider.GetComponentInParent<CreatureController>();
            if (creature != null && (creature.transform == caster || creature.IsDead)) continue;
            if (hit.distance >= nearest) continue;
            nearest = hit.distance;
            result = creature != null ? creature.transform : null;
        }
        return result;
    }

    public float Damage
    {
        get
        {
            float baseDamage = attackData != null ? attackData.damage : 0f;
            CreatureController creature = caster != null ? caster.GetComponent<CreatureController>() : null;
            return (baseDamage * (creature != null ? creature.PowerMultiplier : 1f) + bonusDamage) * (randomCast ? 0.5f : 1f);
        }
    }

    public float AttackSpeedMultiplier
    {
        get
        {
            CreatureController creature = caster != null ? caster.GetComponent<CreatureController>() : null;
            return creature != null ? creature.AttackSpeedMultiplier : 1f;
        }
    }
}
