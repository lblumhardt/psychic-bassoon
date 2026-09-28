using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Team Perks/Isolation Defense")]
public class IsolationDefensePerkSO : TeamPerkSO
{
    [Min(0f)] public float radius = 3f;
    [Min(0f)] public float defenseBonus = 5f;
    public override PerkRuntime CreateRuntime() => new Runtime(radius, defenseBonus);
    private sealed class Runtime : PerkRuntime
    {
        private readonly float radius, bonus;
        public Runtime(float radius, float bonus) { this.radius = radius; this.bonus = bonus; }
        public override float DefenseBonus(CreatureController creature)
        {
            if (creature.Registry == null) return 0f;
            foreach (CreatureController ally in creature.Registry.playerCreatures)
            {
                if (ally == null || ally == creature || ally.IsDead) continue;
                Vector3 delta = ally.transform.position - creature.transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude <= radius * radius) return 0f;
            }
            return bonus;
        }
    }
}
