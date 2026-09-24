using System.Collections.Generic;
using UnityEngine;

public class PackedLunchPickup : MonoBehaviour
{
    private CreatureRegistry _registry;
    private CreatureController _source;
    private Team _ownerTeam;
    private Vector3 _start;
    private Vector3 _end;
    private float _heal;
    private float _damage;
    private float _lobSeconds;
    private float _arcHeight;
    private float _pickupRadius;
    private float _spawnTime;
    private float _expiryTime;
    private Transform _lunchVisual;
    private Material _boxMaterial;
    private Material _outlineMaterial;

    public void Initialize(CreatureController source, CreatureRegistry registry, Team ownerTeam, Vector3 start, Vector3 end,
        float heal, float damage, float lobSeconds, float arcHeight, float lifetime, float pickupRadius)
    {
        _source = source;
        _registry = registry;
        _ownerTeam = ownerTeam;
        _start = start;
        _end = end;
        _heal = Mathf.Max(0f, heal);
        _damage = Mathf.Max(0f, damage);
        _lobSeconds = Mathf.Max(0.05f, lobSeconds);
        _arcHeight = arcHeight;
        _pickupRadius = pickupRadius;
        _spawnTime = Time.time;
        _expiryTime = _spawnTime + _lobSeconds + lifetime;
        transform.position = start;

        _boxMaterial = CombatVisuals.MakeMaterial(new Color(0.9f, 0.7f, 0.32f));
        _outlineMaterial = CombatVisuals.MakeMaterial(ownerTeam == Team.Player
            ? new Color(0.2f, 1f, 0.25f) : new Color(1f, 0.2f, 0.2f));
        _lunchVisual = new GameObject("Lunch Visual").transform;
        _lunchVisual.SetParent(transform, false);
        CombatVisuals.MakeShape(PrimitiveType.Cube, "Lunch Box", _lunchVisual,
            new Vector3(0f, 0.22f, 0f), new Vector3(0.55f, 0.32f, 0.42f), _boxMaterial);
        CombatVisuals.MakeShape(PrimitiveType.Cube, "Lid", _lunchVisual,
            new Vector3(0f, 0.4f, 0f), new Vector3(0.6f, 0.06f, 0.47f), _outlineMaterial);

        LineRenderer outline = gameObject.AddComponent<LineRenderer>();
        outline.useWorldSpace = false;
        outline.loop = true;
        outline.positionCount = 24;
        outline.startWidth = outline.endWidth = 0.06f;
        outline.sharedMaterial = _outlineMaterial;
        for (int i = 0; i < outline.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / outline.positionCount;
            outline.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.48f, 0.05f, Mathf.Sin(angle) * 0.48f));
        }
        outline.enabled = false;
    }

    private void FixedUpdate()
    {
        if (Time.time >= _expiryTime || _registry == null)
        {
            Destroy(gameObject);
            return;
        }

        float t = Mathf.Clamp01((Time.time - _spawnTime) / _lobSeconds);
        transform.position = Vector3.Lerp(_start, _end, t);
        _lunchVisual.localPosition = CombatVisuals.ScreenArc(t, _arcHeight);
        if (t < 1f) return;

        GetComponent<LineRenderer>().enabled = true;
        IReadOnlyList<CreatureController> allies = _ownerTeam == Team.Player
            ? _registry.playerCreatures : _registry.enemyCreatures;
        IReadOnlyList<CreatureController> opponents = _registry.GetOpponents(_ownerTeam);
        if (TryConsume(allies, true) || TryConsume(opponents, false)) Destroy(gameObject);
    }

    private bool TryConsume(IReadOnlyList<CreatureController> creatures, bool ally)
    {
        foreach (CreatureController creature in creatures)
        {
            if (creature == null || creature.IsDead) continue;
            Vector3 delta = creature.transform.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > _pickupRadius * _pickupRadius) continue;
            StatsComponent stats = creature.GetComponent<StatsComponent>();
            if (stats == null) continue;
            if (ally)
            {
                if (stats.Heal(_heal, _source) > 0f) return true;
            }
            else
            {
                stats.TakeDamage(_damage, _source);
                return true;
            }
        }
        return false;
    }

    private void OnDestroy()
    {
        if (_boxMaterial != null) Destroy(_boxMaterial);
        if (_outlineMaterial != null) Destroy(_outlineMaterial);
    }
}
