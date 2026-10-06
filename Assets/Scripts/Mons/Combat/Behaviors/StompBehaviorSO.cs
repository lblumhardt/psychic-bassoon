using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Attack Behaviors/Stomp")]
public class StompBehaviorSO : AttackBehaviorSO
{
    [Min(0.1f)] public float size = 1.6f;
    [Min(0f)] public float windupSeconds = 0.25f;
    [Min(0.02f)] public float dropSeconds = 0.18f;
    [Min(0f)] public float dropDistance = 2f;
    [Min(0f)] public float shoveSpeed = 6f;
    [Min(0f)] public float shoveSeconds = 0.2f;
    [Range(0f, 1f)] public float slowMultiplier = 0.8f;
    [Min(0f)] public float slowSeconds = 0.9f;
    [Min(1f)] public float plusDamageMultiplier = 1.5f;
    public override string PlusDescription => $"{plusDamageMultiplier:0.##}x damage";

    public override IEnumerator Execute(AttackContext context)
    {
        if (context.caster == null || context.attackData == null) yield break;
        var caster = context.caster.GetComponent<CreatureController>();
        if (caster == null || caster.IsDead || caster.Registry == null) yield break;
        Vector3 toward = context.target != null ? context.AimPosition - context.caster.position : context.caster.forward;
        toward.y = 0;
        toward = Vector3.ClampMagnitude(toward, Mathf.Max(0f, context.attackData.range));
        Vector3 down = Camera.main != null ? -Camera.main.transform.up : Vector3.back;
        down.y = 0;
        down = down.sqrMagnitude > 0.001f ? down.normalized : Vector3.back;
        Vector3 start = context.caster.position + WindArea.ClampPush(context.caster.position, toward,
            null, Vector3.one * (size * 0.5f));
        var zone = new GameObject("Stomp").AddComponent<StompZone>();
        zone.Initialize(caster, this, start, down, context.Damage * (context.isPlus ? plusDamageMultiplier : 1f));
        while (zone != null) yield return null;
    }
}
