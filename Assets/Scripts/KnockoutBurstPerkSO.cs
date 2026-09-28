using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Team Perks/Knockout Burst")]
public class KnockoutBurstPerkSO : TeamPerkSO
{
    [Min(1f)] public float speedMultiplier = 1.5f;
    [Min(0f)] public float duration = 5f;
    public override PerkRuntime CreateRuntime() => new Runtime(speedMultiplier, duration);
    private sealed class Runtime : PerkRuntime
    {
        private readonly float multiplier, duration;
        private bool triggered;
        private float until;
        public Runtime(float multiplier, float duration) { this.multiplier = multiplier; this.duration = duration; }
        public override void OnKnockout(BattlePerks battle, CreatureController victim, CreatureController source)
        {
            if (triggered || victim.Team == Team.Player || source == null || source.Team != Team.Player) return;
            triggered = true;
            until = Time.time + duration;
        }
        public override float SpeedMultiplier => triggered && Time.time < until ? multiplier : 1f;
    }
}
