using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Team Perks/Team Bonus")]
public class TeamBonusPerkSO : TeamPerkSO
{
    [Min(0f)] public float defenseBonus;
    [Min(1f)] public float speedMultiplier = 1f;
    [Range(0f, 1f)] public float openingShieldFraction;
    public override PerkRuntime CreateRuntime() => new Runtime(defenseBonus, speedMultiplier, openingShieldFraction);
    private sealed class Runtime : PerkRuntime
    {
        private readonly float defense, speed, shield;
        public Runtime(float defense, float speed, float shield) { this.defense = defense; this.speed = speed; this.shield = shield; }
        public override float DefenseBonus(CreatureController creature) => defense;
        public override float SpeedMultiplier => speed;
        public override void OnBattleStart(BattlePerks battle)
        {
            foreach (CreatureController creature in battle.Participants)
            {
                if (creature == null || creature.Team != Team.Player) continue;
                StatsComponent stats = creature.GetComponent<StatsComponent>();
                if (stats != null) stats.AddShield(stats.MaxHP * shield, stats.MaxHP);
            }
        }
    }
}
