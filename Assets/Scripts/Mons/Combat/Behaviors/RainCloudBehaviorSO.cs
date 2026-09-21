using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Attack Behaviors/Rain Cloud")]
public class RainCloudBehaviorSO : AttackBehaviorSO
{
    [SerializeField] private float windupSeconds = 0.4f;
    [SerializeField] private float lifetimeSeconds = 3f;
    [SerializeField] private float travelSpeed = 3.5f;
    [SerializeField] private float rainRadius = 2.2f;
    [SerializeField] private float damageIntervalSeconds = 0.5f;
    [SerializeField] private float cloudHeight = 3f;

    public override IEnumerator Execute(AttackContext context)
    {
        if (windupSeconds > 0f) yield return new WaitForSeconds(windupSeconds);
        if (context.caster == null || context.target == null) yield break;

        CreatureController caster = context.caster.GetComponent<CreatureController>();
        if (caster == null || caster.Registry == null) yield break;

        Vector3 direction = context.target.position - context.caster.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) direction = context.caster.forward;
        direction.y = 0f;
        direction.Normalize();

        GameObject cloud = new GameObject("Rain Cloud");
        cloud.transform.position = context.caster.position + direction * 1.5f;
        cloud.AddComponent<RainCloudZone>().Initialize(caster,
            context.Damage, direction,
            Mathf.Max(0.1f, lifetimeSeconds),
            Mathf.Max(0f, travelSpeed) * caster.ProjectileSpeedMultiplier,
            Mathf.Max(0.1f, rainRadius), Mathf.Max(0.05f, damageIntervalSeconds), cloudHeight);
    }
}
