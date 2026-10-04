using System.Collections.Generic;
using UnityEngine;

public class StompZone : MonoBehaviour
{
    private CreatureController _caster;
    private StompBehaviorSO _settings;
    private Vector3 _start, _down;
    private float _damage, _elapsed;
    private Material _material;
    private readonly HashSet<CreatureController> _damaged = new();
    private readonly HashSet<CreatureController> _slammed = new();

    public void Initialize(CreatureController caster, StompBehaviorSO settings, Vector3 start, Vector3 down, float damage)
    {
        _caster = caster; _settings = settings; _start = start; _down = down; _damage = damage;
        transform.position = start;
        transform.rotation = Quaternion.LookRotation(down, Vector3.up);
        _material = CombatVisuals.MakeMaterial(new Color(1f, 0.85f, 0.35f));
        CombatVisuals.MakeShape(PrimitiveType.Cube, "Hoof hitbox", transform,
            new Vector3(0f, 0.25f, 0f), new Vector3(settings.size, 0.2f, settings.size), _material);
    }

    private void FixedUpdate()
    {
        if (_caster == null || _caster.IsDead || !_caster.isActiveAndEnabled || _caster.Registry == null)
        { Destroy(gameObject); return; }
        _elapsed += Time.fixedDeltaTime;
        bool dropping = _elapsed >= _settings.windupSeconds;
        Vector3 previous = transform.position;
        float progress = Mathf.Clamp01((_elapsed - _settings.windupSeconds) / Mathf.Max(0.02f, _settings.dropSeconds));
        Vector3 destination = _start + _down * (_settings.dropDistance * progress);
        transform.position += WindArea.ClampPush(previous, destination - previous, null,
            Vector3.one * (_settings.size * 0.5f));
        if (dropping) _material.color = new Color(1f, 0.4f, 0.12f);
        ResolveHits(previous, transform.position, dropping);
        if (progress >= 1f) Destroy(gameObject);
    }

    private void ResolveHits(Vector3 from, Vector3 to, bool dropping)
    {
        // Sweep the whole square between frames so the fast downward strike cannot skip enemies.
        var area = new WindArea(from - _down * (_settings.size * 0.5f), _down,
            Vector3.Distance(from, to) + _settings.size, _settings.size);
        foreach (var enemy in _caster.Registry.GetOpponents(_caster.Team))
        {
            if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled || !area.Reaches(enemy.transform.position, 0.3f)) continue;
            if (_damaged.Add(enemy)) enemy.GetComponent<StatsComponent>()?.TakeDamage(_damage, _caster);
            if (dropping && !enemy.IsDead && _slammed.Add(enemy))
            {
                var movement = enemy.GetComponent<MovementComponent>();
                movement?.ApplyKnockback(_down, _settings.shoveSpeed, _settings.shoveSeconds);
                movement?.ApplySlow(_settings.slowMultiplier, _settings.slowSeconds);
            }
        }
    }

    private void OnDestroy() { if (_material != null) Destroy(_material); }
}
