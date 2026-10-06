using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Attack Behaviors/Gale")]
public class GaleBehaviorSO : AttackBehaviorSO
{
    [Min(0.1f)] public float width = 3.2f;
    [Min(0.1f)] public float channelSeconds = 1.1f;
    [Min(0f)] public float pushSpeed = 5f;
    [Min(1f)] public float plusWidthMultiplier = 1.5f;
    public override string PlusDescription => $"{plusWidthMultiplier:0.##}x wider gale";

    public override IEnumerator Execute(AttackContext context)
    {
        if (context.caster == null || context.attackData == null) yield break;
        CreatureController caster = context.caster.GetComponent<CreatureController>();
        if (caster == null || caster.IsDead || caster.Registry == null) yield break;
        Vector3 direction = context.target != null ? context.AimPosition - context.caster.position : context.caster.forward;
        var zone = new GameObject("Gale").AddComponent<GaleZone>();
        zone.Initialize(caster, direction, context.attackData.range,
            width * (context.isPlus ? plusWidthMultiplier : 1f), pushSpeed, channelSeconds, context.Damage);
        while (zone != null) yield return null;
    }
}
