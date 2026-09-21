using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Attack Behaviors/Fissure")]
public class FissureBehaviorSO : AttackBehaviorSO
{
    [SerializeField] private float windupSeconds = 0.35f;
    [SerializeField] private float length = 9f;
    [SerializeField] private float width = 0.85f;
    [SerializeField] private float zigzagOffset = 0.7f;
    [SerializeField] private int segments = 7;
    [SerializeField] private float lifetimeSeconds = 4f;
    [SerializeField, Range(0f, 1f)] private float slowMultiplier = 0.45f;
    [SerializeField] private float slowRefreshSeconds = 0.25f;
    [SerializeField] private LayerMask groundLayers = 1;

    public override IEnumerator Execute(AttackContext context)
    {
        if (windupSeconds > 0f) yield return new WaitForSeconds(windupSeconds);
        if (context.caster == null || context.target == null) yield break;

        CreatureController caster = context.caster.GetComponent<CreatureController>();
        if (caster == null || caster.Registry == null) yield break;

        Vector3 start = context.caster.position;
        Vector3 direction = context.target.position - start;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) direction = context.caster.forward;
        direction.y = 0f;
        direction.Normalize();
        start += direction * 0.6f;

        if (Physics.Raycast(start + Vector3.up * 3f, Vector3.down, out RaycastHit ground, 8f, groundLayers, QueryTriggerInteraction.Ignore))
            start.y = ground.point.y + 0.04f;
        else
            start.y -= 0.45f;

        GameObject zone = new GameObject("Fissure");
        zone.transform.position = start;
        zone.AddComponent<FissureZone>().Initialize(caster, context.Damage,
            direction, Mathf.Max(0.1f, length), Mathf.Max(0.05f, width), zigzagOffset,
            Mathf.Max(1, segments), Mathf.Max(0.1f, lifetimeSeconds), slowMultiplier, slowRefreshSeconds);
    }
}
