using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Mons/Attack Behaviors/Rebar Shot")]
public class RebarShotBehaviorSO : AttackBehaviorSO
{
    [SerializeField] private float castWindupSeconds = 0.15f;

    public override IEnumerator Execute(AttackContext context)
    {
        if (castWindupSeconds > 0f)
        {
            yield return new WaitForSeconds(castWindupSeconds);
        }

        if (context.target == null)
        {
            yield break;
        }

        Vector3 origin = context.caster.position + Vector3.up * 0.5f;
        Vector3 targetCenter = context.target.position + Vector3.up * 0.5f;
        Vector3 direction = (targetCenter - origin).normalized;
        float maxDistance = context.attackData != null ? context.attackData.range : 10f;

        // Placeholder behavior: instant "piercing bolt" ray with debug visualization.
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance))
        {
            Debug.DrawLine(origin, hit.point, Color.gray, 0.5f);
        }
        else
        {
            Debug.DrawRay(origin, direction * maxDistance, Color.gray, 0.5f);
        }
    }
}
