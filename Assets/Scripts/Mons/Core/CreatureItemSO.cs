using UnityEngine;

public enum CreatureItemSlot
{
    HeldItem,
    Spray
}

[CreateAssetMenu(menuName = "Mons/Creature Item")]
public class CreatureItemSO : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
    public CreatureItemSlot slot;
    [Min(0)] public int price = 2;

    [Header("Flat Stat Bonuses")]
    public int hp;
    public int power;
    public int defense;
    public int moveSpeed;
    public int attackSpeed;

    [Header("Combat Multipliers")]
    [Min(0.1f)] public float damageMultiplier = 1f;
    [Min(0.1f)] public float moveCooldownMultiplier = 1f;
    [Min(0.1f)] public float projectileSpeedMultiplier = 1f;

    public CreatureStats ApplyStats(CreatureStats stats)
    {
        return new CreatureStats(
            stats.hp + hp,
            stats.power + power,
            stats.defense + defense,
            stats.moveSpeed + moveSpeed,
            stats.attackSpeed + attackSpeed);
    }
}
