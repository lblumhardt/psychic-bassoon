using UnityEngine;

public struct AttackContext
{
    public Transform caster;
    public Transform target;
    public CombatComponent combatComponent;
    public AttackDataSO attackData;

    public float Damage
    {
        get
        {
            float baseDamage = attackData != null ? attackData.damage : 0f;
            CreatureController creature = caster != null ? caster.GetComponent<CreatureController>() : null;
            return baseDamage * (creature != null ? creature.PowerMultiplier : 1f);
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
