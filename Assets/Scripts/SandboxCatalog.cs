using UnityEngine;

public class SandboxCatalog : ScriptableObject
{
    public CreatureDataSO[] creatures;
    public AttackDataSO[] moves;
    public CreatureAbilitySO[] abilities;
    public CreatureItemSO[] items;
}
