using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Attack Behaviors/Firework")]
public class FireworkBehaviorSO : AttackBehaviorSO
{
    [SerializeField] private float windupSeconds = 0.3f;
    [SerializeField] private float rocketFlightSeconds = 0.7f;
    [SerializeField] private float explosionRadius = 1.1f;
    [SerializeField] private float scatterRadius = 2.4f;
    [SerializeField, Range(0f, 1f)] private float satelliteDamageMultiplier = 0.5f;

    public override IEnumerator Execute(AttackContext context)
    {
        if (windupSeconds > 0f) yield return new WaitForSeconds(windupSeconds);
        if (context.caster == null || context.target == null) yield break;
        CreatureController caster = context.caster.GetComponent<CreatureController>();
        if (caster == null || caster.Registry == null) yield break;

        GameObject rocket = new GameObject("Firework Rocket");
        rocket.AddComponent<FireworkRocket>().Initialize(caster, caster.Registry, caster.Team,
            context.caster.position, context.target.position,
            context.Damage,
            rocketFlightSeconds / caster.ProjectileSpeedMultiplier,
            explosionRadius, scatterRadius, satelliteDamageMultiplier);
    }
}
