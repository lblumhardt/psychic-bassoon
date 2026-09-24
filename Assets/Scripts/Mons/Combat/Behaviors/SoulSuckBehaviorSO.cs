using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Attack Behaviors/Soul Suck")]
public class SoulSuckBehaviorSO : AttackBehaviorSO
{
    [SerializeField] private float windupSeconds = 0.3f;
    [SerializeField] private float beamSeconds = 0.45f;

    public override IEnumerator Execute(AttackContext context)
    {
        if (windupSeconds > 0f) yield return new WaitForSeconds(windupSeconds);
        if (context.caster == null || context.target == null) yield break;

        CreatureController caster = context.caster.GetComponent<CreatureController>();
        CreatureController victim = context.target.GetComponent<CreatureController>();
        if (caster == null || victim == null || caster.Team == victim.Team || victim.IsDead) yield break;

        StatsComponent casterStats = caster.GetComponent<StatsComponent>();
        StatsComponent victimStats = victim.GetComponent<StatsComponent>();
        if (casterStats == null || victimStats == null) yield break;

        float damage = context.Damage;
        float drained = victimStats.TakeDamage(damage, caster);
        casterStats.Heal(drained * 0.5f, caster);

        GameObject beamObject = new GameObject("Soul Suck Beam");
        LineRenderer beam = beamObject.AddComponent<LineRenderer>();
        Material beamMaterial = CombatVisuals.MakeMaterial(new Color(0.75f, 0.28f, 1f));
        beam.sharedMaterial = beamMaterial;
        beam.positionCount = 2;
        beam.startWidth = 0.16f;
        beam.endWidth = 0.06f;
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, beamSeconds);
        while (elapsed < duration && caster != null && victim != null)
        {
            beam.SetPosition(0, caster.transform.position + Vector3.up * 1.2f);
            beam.SetPosition(1, victim.transform.position + Vector3.up * 1.2f);
            Color color = new Color(0.75f, 0.28f, 1f, 1f - elapsed / duration);
            beam.startColor = beam.endColor = color;
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(beamObject);
        Destroy(beamMaterial);
    }
}
