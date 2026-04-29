using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Creature")]
public class CreatureDataSO : ScriptableObject
{
    public string creatureName;
    public float maxHP;
    public float attack;
    public float defense;
    public float speed;

    public List<AttackDataSO> attacks;
}