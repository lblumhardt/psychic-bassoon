using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Attack")]
public class AttackDataSO : ScriptableObject
{
    public string attackName;
    public float damage;
    [Min(0f), Tooltip("Relative chance this move is chosen when it is off cooldown.")]
    public float selectionWeight = 1f;
    [Min(0f), Tooltip("This move's independent cooldown. Attack Speed does not shorten it.")]
    public float cooldown;
    public float range;
    public float duration = 0.25f;
    public AttackBehaviorSO behavior;
}
