using System.Collections;
using UnityEngine;

public class FireworkRocket : MonoBehaviour
{
    private CreatureRegistry _registry;
    private Team _team;
    private Vector3 _start;
    private Vector3 _end;
    private float _damage;
    private float _flightSeconds;
    private float _explosionRadius;
    private float _scatterRadius;
    private float _satelliteMultiplier;
    private Material _rocketMaterial;
    private LineRenderer _trail;
    private Transform _rocketVisual;

    public void Initialize(CreatureRegistry registry, Team team, Vector3 start, Vector3 end,
        float damage, float flightSeconds, float explosionRadius, float scatterRadius, float satelliteMultiplier)
    {
        _registry = registry;
        _team = team;
        _start = start;
        _end = new Vector3(end.x, start.y, end.z);
        _damage = Mathf.Max(0f, damage);
        _flightSeconds = Mathf.Max(0.05f, flightSeconds);
        _explosionRadius = Mathf.Max(0.1f, explosionRadius);
        _scatterRadius = Mathf.Max(0f, scatterRadius);
        _satelliteMultiplier = satelliteMultiplier;
        transform.position = start;

        _rocketMaterial = CombatVisuals.MakeMaterial(new Color(1f, 0.52f, 0.12f));
        _rocketVisual = CombatVisuals.MakeShape(PrimitiveType.Cube, "Rocket", transform,
            new Vector3(0f, 1.3f, 0f), new Vector3(0.18f, 0.15f, 0.55f), _rocketMaterial);
        Vector3 direction = _end - _start;
        if (direction.sqrMagnitude > 0.001f) _rocketVisual.rotation = Quaternion.LookRotation(direction);

        _trail = gameObject.AddComponent<LineRenderer>();
        _trail.positionCount = 2;
        _trail.startWidth = 0.09f;
        _trail.endWidth = 0.015f;
        _trail.sharedMaterial = _rocketMaterial;
        StartCoroutine(FlightAndBurst());
    }

    private IEnumerator FlightAndBurst()
    {
        float startTime = Time.time;
        while (Time.time - startTime < _flightSeconds)
        {
            float t = Mathf.Clamp01((Time.time - startTime) / _flightSeconds);
            transform.position = Vector3.Lerp(_start, _end, t);
            _trail.SetPosition(0, Vector3.Lerp(_start, transform.position, 0.72f) + Vector3.up * 1.3f);
            _trail.SetPosition(1, transform.position + Vector3.up * 1.3f);
            yield return null;
        }
        transform.position = _end;
        _rocketVisual.gameObject.SetActive(false);
        _trail.enabled = false;

        StartCoroutine(ShowBurst(_end, _damage));
        int satellites = Random.Range(3, 5);
        for (int i = 0; i < satellites; i++)
        {
            yield return new WaitForSeconds(0.12f);
            Vector2 offset = Random.insideUnitCircle * _scatterRadius;
            Vector3 position = _end + new Vector3(offset.x, 0f, offset.y);
            StartCoroutine(ShowBurst(position, _damage * _satelliteMultiplier));
        }
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    private IEnumerator ShowBurst(Vector3 position, float damage)
    {
        if (_registry != null)
        {
            foreach (CreatureController opponent in _registry.GetOpponents(_team))
            {
                if (opponent == null || opponent.IsDead) continue;
                Vector3 delta = opponent.transform.position - position;
                delta.y = 0f;
                if (delta.sqrMagnitude <= _explosionRadius * _explosionRadius)
                    opponent.GetComponent<StatsComponent>()?.TakeDamage(damage);
            }
        }

        GameObject burst = new GameObject("Firework Burst");
        burst.transform.SetParent(transform, false);
        burst.transform.position = position + Vector3.up * 1.5f;
        Color color = Color.HSVToRGB(Random.value, 0.8f, 1f);
        Material burstMaterial = CombatVisuals.MakeMaterial(color);
        for (int i = 0; i < 8; i++)
        {
            GameObject ray = new GameObject("Spark");
            ray.transform.SetParent(burst.transform, false);
            LineRenderer line = ray.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = 0.11f;
            line.endWidth = 0.015f;
            line.sharedMaterial = burstMaterial;
            float angle = i * Mathf.PI * 0.25f;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            line.SetPosition(0, direction * 0.12f);
            line.SetPosition(1, direction * 0.95f);
        }

        float elapsed = 0f;
        while (elapsed < 0.45f)
        {
            float t = elapsed / 0.45f;
            burst.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 1.3f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(burst);
        Destroy(burstMaterial);
    }

    private void OnDestroy()
    {
        if (_rocketMaterial != null) Destroy(_rocketMaterial);
    }
}
