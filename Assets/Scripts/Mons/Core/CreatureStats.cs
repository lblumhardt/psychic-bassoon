using System;
using UnityEngine;

[Serializable]
public struct CreatureStats
{
    [Min(1)] public int hp;
    [Min(1)] public int power;
    [Min(1)] public int defense;
    [Min(1)] public int moveSpeed;
    [Min(1)] public int attackSpeed;

    public CreatureStats(int hp, int power, int defense, int moveSpeed, int attackSpeed)
    {
        this.hp = Mathf.Max(1, hp);
        this.power = Mathf.Max(1, power);
        this.defense = Mathf.Max(1, defense);
        this.moveSpeed = Mathf.Max(1, moveSpeed);
        this.attackSpeed = Mathf.Max(1, attackSpeed);
    }

    public CreatureStats RollVariation()
    {
        return new CreatureStats(
            hp + RollDelta(),
            power + RollDelta(),
            defense + RollDelta(),
            moveSpeed + RollDelta(),
            attackSpeed + RollDelta());
    }

    // Each level adds 25% of the original stat (rounded up, minimum one).
    public CreatureStats AtLevel(int level)
    {
        int upgrades = Mathf.Clamp(level, 1, 3) - 1;
        return new CreatureStats(
            hp + upgrades * Mathf.Max(1, Mathf.CeilToInt(hp * 0.25f)),
            power + upgrades * Mathf.Max(1, Mathf.CeilToInt(power * 0.25f)),
            defense + upgrades * Mathf.Max(1, Mathf.CeilToInt(defense * 0.25f)),
            moveSpeed + upgrades * Mathf.Max(1, Mathf.CeilToInt(moveSpeed * 0.25f)),
            attackSpeed + upgrades * Mathf.Max(1, Mathf.CeilToInt(attackSpeed * 0.25f)));
    }

    private static int RollDelta()
    {
        int amount = UnityEngine.Random.Range(1, 3);
        return UnityEngine.Random.value < 0.5f ? -amount : amount;
    }
}
