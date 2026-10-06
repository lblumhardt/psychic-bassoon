using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Attack Behaviors/Packed Lunch")]
public class PackedLunchBehaviorSO : AttackBehaviorSO
{
    [SerializeField, Min(1f)] private float plusHealingMultiplier = 1.5f;
    public override string PlusDescription => $"{plusHealingMultiplier:0.##}x healing";
    [SerializeField] private float windupSeconds = 0.25f;
    [SerializeField] private float lobSeconds = 0.65f;
    [SerializeField] private float arcHeight = 1.3f;
    [SerializeField] private float pickupLifetimeSeconds = 8f;
    [SerializeField] private float pickupRadius = 0.7f;
    [SerializeField] private float opponentDamage = 1f;

    public override IEnumerator Execute(AttackContext context)
    {
        if (windupSeconds > 0f) yield return new WaitForSeconds(windupSeconds);
        if (context.caster == null) yield break;
        CreatureController caster = context.caster.GetComponent<CreatureController>();
        if (caster == null || caster.Registry == null) yield break;

        IReadOnlyList<CreatureController> allies = caster.Team == Team.Player
            ? caster.Registry.playerCreatures : caster.Registry.enemyCreatures;
        CreatureController injuredAlly = null;
        float lowestHealth = 1f;
        foreach (CreatureController ally in allies)
        {
            if (ally == null || ally.IsDead) continue;
            StatsComponent stats = ally.GetComponent<StatsComponent>();
            if (stats != null && stats.HealthFraction < lowestHealth)
            {
                lowestHealth = stats.HealthFraction;
                injuredAlly = ally;
            }
        }

        Vector3 start = context.caster.position;
        Vector3 end;
        if (injuredAlly != null && !context.randomCast)
            end = injuredAlly.transform.position;
        else
        {
            Vector3 forward = context.target != null ? context.AimPosition - start : context.caster.forward;
            forward.y = 0f;
            end = start + forward.normalized * 2.5f;
        }
        end.y = start.y;

        GameObject lunch = new GameObject("Packed Lunch");
        lunch.AddComponent<PackedLunchPickup>().Initialize(caster, caster.Registry, caster.Team, start, end,
            context.Damage * (context.isPlus ? plusHealingMultiplier : 1f), opponentDamage * (context.randomCast ? 0.5f : 1f),
            lobSeconds / caster.ProjectileSpeedMultiplier,
            arcHeight, pickupLifetimeSeconds, pickupRadius);
    }
}
