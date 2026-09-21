using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Attack Behaviors/Fruit Toss")]
public class FruitTossBehaviorSO : AttackBehaviorSO
{
    [SerializeField] private float windupSeconds = 0.2f;
    [SerializeField] private float flightSeconds = 0.65f;
    [SerializeField] private float screenArcHeight = 1.6f;
    [SerializeField] private float impactRadius = 0.9f;

    public override IEnumerator Execute(AttackContext context)
    {
        if (windupSeconds > 0f) yield return new WaitForSeconds(windupSeconds);
        if (context.caster == null || context.target == null) yield break;
        CreatureController caster = context.caster.GetComponent<CreatureController>();
        if (caster == null || caster.Registry == null) yield break;

        GameObject apple = new GameObject("Fruit Toss Apple");
        apple.AddComponent<FruitProjectile>().Initialize(caster.Registry, caster.Team,
            context.caster.position, context.target.position,
            context.Damage,
            Mathf.Max(0.05f, flightSeconds / caster.ProjectileSpeedMultiplier),
            screenArcHeight, Mathf.Max(0.1f, impactRadius));
    }
}
