using UnityEngine;

public struct AttackContext
{
    public Transform caster;
    public Transform target;
    public CombatComponent combatComponent;
    public AttackDataSO attackData;
}
