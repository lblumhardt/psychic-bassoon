using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Attack Behaviors/Flamethrower")]
public class FlamethrowerBehaviorSO : AttackBehaviorSO
{
    [Header("Art")]
    [Tooltip("One fire sprite, repeated along the stream and on the ground. Uses placeholder art when empty.")]
    public Sprite fireSprite;
    [Min(0.1f)] public float spriteSize = 0.9f;

    [Header("Stream")]
    [Range(2, 32)] public int flameCount = 12;
    [Min(0.01f)] public float secondsBetweenFlames = 0.075f;
    [Min(0f)] public float straightHoldSeconds = 0.45f;
    [Min(0.01f)] public float waveSeconds = 1.1f;
    [Min(0.01f)] public float fadeSeconds = 0.2f;
    [Min(0f)] public float waveAmplitude = 1.2f;
    [Min(0f)] public float waveFrequency = 2f;
    [Min(0.05f)] public float hitRadius = 0.45f;
    [Min(0.05f)] public float damageInterval = 0.3f;

    [Header("Ground fire")]
    [Range(2, 3)] public int groundFireCount = 3;
    [Min(0.1f)] public float groundFireSeconds = 3f;
    [Min(0.1f)] public float groundFireRadius = 0.7f;
    [Min(0f)] public float groundDamageMultiplier = 0.5f;
    [Min(1f)] public float plusGroundLifetimeMultiplier = 1.5f;

    public override string PlusDescription => $"Ground fires last {plusGroundLifetimeMultiplier:0.##}x longer";

    public override IEnumerator Execute(AttackContext context)
    {
        if (context.caster == null || context.target == null || context.attackData == null) yield break;
        CreatureController caster = context.caster.GetComponent<CreatureController>();
        if (caster == null || caster.IsDead || caster.Registry == null) yield break;
        Vector3 direction = context.target.position - context.caster.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) direction = Vector3.forward;
        GameObject effectObject = new GameObject("Flamethrower");
        FlamethrowerZone effect = effectObject.AddComponent<FlamethrowerZone>();
        effect.Initialize(caster, this, direction.normalized, Mathf.Max(1f, context.attackData.range),
            context.Damage, context.isPlus);
        // Channel the stream; the independent ground fires outlive this attack.
        while (effect != null && effect.StreamActive) yield return null;
    }
}
