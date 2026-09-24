using UnityEngine;

public class FruitProjectile : MonoBehaviour
{
    private CreatureRegistry _registry;
    private CreatureController _source;
    private Team _team;
    private Vector3 _start;
    private Vector3 _end;
    private float _damage;
    private float _flightSeconds;
    private float _arcHeight;
    private float _impactRadius;
    private float _launchTime;
    private Transform _apple;
    private Transform _shadow;
    private Material _redMaterial;
    private Material _greenMaterial;
    private Material _shadowMaterial;

    public void Initialize(CreatureController source, CreatureRegistry registry, Team team, Vector3 start, Vector3 end,
        float damage, float flightSeconds, float arcHeight, float impactRadius)
    {
        _source = source;
        _registry = registry;
        _team = team;
        _start = start;
        _end = new Vector3(end.x, start.y, end.z);
        _damage = Mathf.Max(0f, damage);
        _flightSeconds = flightSeconds;
        _arcHeight = arcHeight;
        _impactRadius = impactRadius;
        _launchTime = Time.time;
        transform.position = start;

        _redMaterial = CombatVisuals.MakeMaterial(new Color(0.92f, 0.12f, 0.08f));
        _greenMaterial = CombatVisuals.MakeMaterial(new Color(0.18f, 0.62f, 0.14f));
        _shadowMaterial = CombatVisuals.MakeMaterial(new Color(0.16f, 0.12f, 0.12f));
        _apple = new GameObject("Apple Visual").transform;
        _apple.SetParent(transform, false);
        CombatVisuals.MakeShape(PrimitiveType.Sphere, "Apple", _apple,
            new Vector3(0f, 0.22f, 0f), Vector3.one * 0.45f, _redMaterial);
        CombatVisuals.MakeShape(PrimitiveType.Cube, "Leaf", _apple,
            new Vector3(0.14f, 0.48f, 0f), new Vector3(0.23f, 0.055f, 0.13f), _greenMaterial);
        _shadow = CombatVisuals.MakeShape(PrimitiveType.Cylinder, "Shadow", transform,
            new Vector3(0f, 0.035f, 0f), new Vector3(0.42f, 0.01f, 0.42f), _shadowMaterial);
    }

    private void Update()
    {
        float t = Mathf.Clamp01((Time.time - _launchTime) / _flightSeconds);
        transform.position = Vector3.Lerp(_start, _end, t);
        _apple.localPosition = CombatVisuals.ScreenArc(t, _arcHeight);
        _apple.localRotation = Quaternion.Euler(0f, 0f, t * 360f);
        float shadowScale = 1f - Mathf.Sin(Mathf.PI * t) * 0.4f;
        _shadow.localScale = new Vector3(0.42f * shadowScale, 0.01f, 0.42f * shadowScale);
        if (t < 1f) return;

        if (_registry != null)
        {
            foreach (CreatureController opponent in _registry.GetOpponents(_team))
            {
                if (opponent == null || opponent.IsDead) continue;
                Vector3 delta = opponent.transform.position - _end;
                delta.y = 0f;
                if (delta.sqrMagnitude <= _impactRadius * _impactRadius)
                    opponent.GetComponent<StatsComponent>()?.TakeDamage(_damage, _source);
            }
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_redMaterial != null) Destroy(_redMaterial);
        if (_greenMaterial != null) Destroy(_greenMaterial);
        if (_shadowMaterial != null) Destroy(_shadowMaterial);
    }
}
