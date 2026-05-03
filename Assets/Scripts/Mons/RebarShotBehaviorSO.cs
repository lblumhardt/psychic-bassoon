using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Mons/Attack Behaviors/Rebar Shot")]
public class RebarShotBehaviorSO : AttackBehaviorSO
{
    [SerializeField] private float castWindupSeconds = 1.0f;
    [SerializeField] private GameObject rebarProjectilePrefab;
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float spawnHeightOffset = 0.5f;
    [SerializeField] private float spawnForwardOffset = 1.25f;
    [SerializeField] private float spawnProbeRadius = 0.15f;
    [SerializeField] private LayerMask spawnCollisionMask = ~0;

    public override IEnumerator Execute(AttackContext context)
    {
        if (castWindupSeconds > 0f)
        {
            yield return new WaitForSeconds(castWindupSeconds);
        }

        if (context.caster == null || context.target == null || rebarProjectilePrefab == null)
        {
            yield break;
        }

        Vector3 origin = context.caster.position + Vector3.up * spawnHeightOffset;
        Vector3 targetCenter = context.target.position + Vector3.up * 0.5f;
        Vector3 toTarget = targetCenter - origin;
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            toTarget = context.caster.forward;
        }

        Vector3 direction = toTarget.normalized;
        Vector3 spawnPosition = origin + direction * spawnForwardOffset;

        // If something is very close in front of the caster, pull spawn point back to avoid clipping.
        if (spawnForwardOffset > 0f &&
            Physics.SphereCast(origin, spawnProbeRadius, direction, out RaycastHit hit, spawnForwardOffset, spawnCollisionMask, QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(0.05f, hit.distance - 0.05f);
            spawnPosition = origin + direction * safeDistance;
        }

        GameObject projectileObject = Instantiate(rebarProjectilePrefab, spawnPosition, Quaternion.LookRotation(direction));
        RebarProjectile projectile = projectileObject.GetComponent<RebarProjectile>();

        if (projectile != null)
        {
            // No max travel distance — lifetime and collisions are handled on RebarProjectile.
            projectile.Launch(context.caster, direction, projectileSpeed, maxDistance: 0f);
        }
    }
}
