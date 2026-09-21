using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Attack Behaviors/Auto Gun")]
public class AutoGunBehaviorSO : AttackBehaviorSO
{
    private static Material _tracerMaterial;
    [SerializeField, Min(1)] private int shotsPerBurst = 8;
    [SerializeField, Min(0.01f)] private float secondsBetweenShots = 0.12f;
    [SerializeField] private float muzzleHeight = 0.5f;

    public override IEnumerator Execute(AttackContext context)
    {
        if (context.caster == null || context.attackData == null) yield break;

        CreatureController shooter = context.caster.GetComponent<CreatureController>();
        TargetingComponent targeting = context.caster.GetComponent<TargetingComponent>();
        if (shooter == null || targeting == null) yield break;

        GameObject tracerObject = new GameObject("Auto Gun Tracer");
        tracerObject.transform.SetParent(context.caster, false);
        LineRenderer tracer = tracerObject.AddComponent<LineRenderer>();
        tracer.positionCount = 2;
        tracer.startWidth = 0.045f;
        tracer.endWidth = 0.02f;
        tracer.startColor = Color.yellow;
        tracer.endColor = Color.yellow;
        if (_tracerMaterial == null)
        {
            Shader tracerShader = Shader.Find("Sprites/Default");
            if (tracerShader != null) _tracerMaterial = new Material(tracerShader);
        }
        if (_tracerMaterial != null) tracer.sharedMaterial = _tracerMaterial;
        tracer.enabled = false;

        for (int shot = 0; shot < shotsPerBurst && !shooter.IsDead; shot++)
        {
            Transform target = targeting.GetTarget();
            if (target == null) break;

            CreatureController victim = target.GetComponent<CreatureController>();
            if (victim == null || victim.Team == shooter.Team || victim.IsDead) break;

            Vector3 origin = context.caster.position + Vector3.up * muzzleHeight;
            Collider targetCollider = target.GetComponent<Collider>();
            Vector3 aimPoint = targetCollider != null ? targetCollider.bounds.center : target.position + Vector3.up * muzzleHeight;
            Vector3 toTarget = aimPoint - origin;
            float distance = toTarget.magnitude;
            float range = Mathf.Max(0f, context.attackData.range);

            if (distance > 0.001f && distance <= range)
            {
                Vector3 direction = toTarget / distance;
                RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance + 0.1f,
                    ~0, QueryTriggerInteraction.Ignore);
                float nearestDistance = float.MaxValue;
                CreatureController firstCreature = null;
                bool blocked = false;
                foreach (RaycastHit hit in hits)
                {
                    CreatureController hitCreature = hit.collider.GetComponentInParent<CreatureController>();
                    if (hitCreature == shooter || hit.distance >= nearestDistance) continue;
                    nearestDistance = hit.distance;
                    firstCreature = hitCreature;
                    blocked = hitCreature == null;
                }

                if (!blocked && firstCreature == victim)
                {
                    victim.GetComponent<StatsComponent>()?.TakeDamage(context.Damage);
                }

                tracer.SetPosition(0, origin);
                tracer.SetPosition(1, origin + direction * Mathf.Min(distance, nearestDistance));
                tracer.enabled = true;
            }

            if (shot < shotsPerBurst - 1)
            {
                yield return new WaitForSeconds(secondsBetweenShots);
                tracer.enabled = false;
            }
        }

        Destroy(tracerObject);
    }
}
