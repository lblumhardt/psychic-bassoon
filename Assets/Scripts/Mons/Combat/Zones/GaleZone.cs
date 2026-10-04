using System.Collections.Generic;
using UnityEngine;

public class GaleZone : MonoBehaviour
{
    private CreatureController _caster;
    private Vector3 _direction;
    private float _length, _width, _speed, _ends, _damage;
    private Material _material;
    private LineRenderer _outline;
    private readonly HashSet<FlamethrowerZone> _spread = new();
    private readonly HashSet<CreatureController> _damaged = new();
    private readonly List<CreatureController> _creatureSnapshot = new();

    public void Initialize(CreatureController caster, Vector3 direction, float length, float width, float speed, float seconds, float damage)
    {
        _caster = caster;
        _direction = new WindArea(caster.transform.position, direction, length, width).Direction;
        _length = Mathf.Max(0.1f, length); _width = Mathf.Max(0.1f, width);
        _speed = Mathf.Max(0f, speed); _ends = Time.time + Mathf.Max(0.01f, seconds);
        _damage = Mathf.Max(0f, damage);
        _material = CombatVisuals.MakeMaterial(new Color(0.65f, 0.95f, 1f));
        _outline = gameObject.AddComponent<LineRenderer>();
        _outline.sharedMaterial = _material;
        _outline.startWidth = _outline.endWidth = 0.06f;
        _outline.positionCount = 5;
        Draw();
    }

    private void FixedUpdate()
    {
        if (_caster == null || _caster.IsDead || !_caster.isActiveAndEnabled || _caster.Registry == null || Time.time >= _ends)
        { Destroy(gameObject); return; }
        var area = new WindArea(_caster.transform.position, _direction, _length, _width);
        PushCreatures(_caster.Registry.GetAllies(_caster.Team), area);
        PushCreatures(_caster.Registry.GetOpponents(_caster.Team), area);
        Vector3 step = _direction * (_speed * Time.fixedDeltaTime);
        foreach (var pickup in FindObjectsByType<PackedLunchPickup>(FindObjectsSortMode.None))
            if (area.Reaches(pickup.transform.position, 0.25f)) pickup.ApplyWind(step);
        foreach (var obstacle in FindObjectsByType<WindMovable>(FindObjectsSortMode.None))
            if (area.Reaches(obstacle.transform.position, 0.3f)) obstacle.Push(step);
        foreach (var rebar in FindObjectsByType<RebarProjectile>(FindObjectsSortMode.None))
            if (area.Reaches(rebar.transform.position, 0.2f)) rebar.ApplyWind(step);
        foreach (var fire in FindObjectsByType<FlamethrowerZone>(FindObjectsSortMode.None))
            if (fire.ApplyWind(area, step, !_spread.Contains(fire))) _spread.Add(fire);
        Physics.SyncTransforms();
        Draw();
    }

    private void PushCreatures(IReadOnlyList<CreatureController> creatures, WindArea area)
    {
        // Damage callbacks can remove creatures from the registry, including chained knockouts.
        _creatureSnapshot.Clear();
        _creatureSnapshot.AddRange(creatures);
        foreach (var creature in _creatureSnapshot)
        {
            if (creature == null || creature == _caster || creature.IsDead || !creature.isActiveAndEnabled ||
                !area.Reaches(creature.transform.position, 0.35f)) continue;
            // Tiny chip damage once per enemy per cast, never once per physics tick or on allies.
            if (creature.Team != _caster.Team && _damaged.Add(creature))
                creature.GetComponent<StatsComponent>()?.TakeDamage(_damage, _caster);
            if (creature.IsDead) continue;
            creature.GetComponent<MovementComponent>()?.ApplyKnockback(_direction, _speed, Time.fixedDeltaTime * 1.5f);
        }
    }

    private void Draw()
    {
        Vector3 start = _caster.transform.position;
        start.y = 0.85f;
        Vector3 side = Vector3.Cross(Vector3.up, _direction) * (_width * 0.5f);
        Vector3 end = start + _direction * _length;
        _outline.SetPositions(new[] { start - side, end - side, end + side, start + side, start - side });
    }

    private void OnDestroy() { if (_material != null) Destroy(_material); }
}
