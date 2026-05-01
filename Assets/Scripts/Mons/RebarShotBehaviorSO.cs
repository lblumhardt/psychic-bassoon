using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Mons/Attack Behaviors/Rebar Shot")]
public class RebarShotBehaviorSO : AttackBehaviorSO
{
    [SerializeField] private float castWindupSeconds = 1.0f;
    [SerializeField] private float debugLineDurationSeconds = 2.0f;
    [SerializeField] private Color debugLineColor = Color.red;

    public override IEnumerator Execute(AttackContext context)
    {
        Debug.Log("Wow we actually shot rebar");
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
        Vector3 toTarget = targetCenter - origin;
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            toTarget = context.caster.forward;
        }

        Vector3 direction = toTarget.normalized;
        float maxDistance = context.attackData != null ? context.attackData.range : 10f;

        // Placeholder behavior: instant "piercing bolt" ray with debug visualization.
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance))
        {
            Debug.DrawLine(origin, hit.point, debugLineColor, debugLineDurationSeconds, false);
            Debug.Log($"Rebar hit: {hit.collider.name}");
        }
        else
        {
            Debug.DrawLine(origin, origin + direction * maxDistance, debugLineColor, debugLineDurationSeconds, false);
            Debug.Log("Rebar missed");
        }
    }
}
