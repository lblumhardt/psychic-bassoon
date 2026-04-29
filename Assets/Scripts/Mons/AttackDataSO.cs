using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Attack")]
public class AttackDataSO : ScriptableObject
{
    public string attackName;
    public float damage;
    public float cooldown;
    public float range;
    public float duration = 0.25f;
    public AttackBehaviorSO behavior;
}