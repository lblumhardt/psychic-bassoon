using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Team Perks/Overheal Shield")]
public class OverhealShieldPerkSO : TeamPerkSO
{
    [Range(0f, 1f)] public float conversion = 1f;
    [Range(0f, 1f)] public float maxHealthFraction = 0.5f;
    public override PerkRuntime CreateRuntime() => new Runtime(conversion, maxHealthFraction);
    private sealed class Runtime : PerkRuntime
    {
        private readonly float conversion, cap;
        public Runtime(float conversion, float cap) { this.conversion = conversion; this.cap = cap; }
        public override void OnOverheal(StatsComponent target, float excess) =>
            target.AddShield(excess * conversion, target.MaxHP * cap);
    }
}
