using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FissureZone : MonoBehaviour
{
    private readonly List<Vector3> _points = new List<Vector3>();
    private CreatureController _caster;
    private float _width;
    private float _slowMultiplier;
    private float _slowRefresh;
    private Material _lineMaterial;

    public void Initialize(CreatureController caster, float damage, Vector3 direction, float length,
        float width, float zigzagOffset, int segments, float lifetime, float slowMultiplier, float slowRefresh)
    {
        _caster = caster;
        _width = width;
        _slowMultiplier = slowMultiplier;
        _slowRefresh = slowRefresh;
        Vector3 sideways = Vector3.Cross(Vector3.up, direction);
        for (int i = 0; i <= segments; i++)
        {
            float lateral = i == 0 || i == segments ? 0f : (i % 2 == 0 ? 1f : -1f) * zigzagOffset;
            _points.Add(transform.position + direction * (length * i / segments) + sideways * lateral);
        }

        LineRenderer line = gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = _points.Count;
        line.SetPositions(_points.ToArray());
        line.startWidth = width * 0.7f;
        line.endWidth = width * 0.7f;
        _lineMaterial = new Material(Shader.Find("Sprites/Default"));
        line.sharedMaterial = _lineMaterial;
        line.startColor = new Color(0.14f, 0.04f, 0.02f);
        line.endColor = new Color(0.85f, 0.25f, 0.04f);

        // The opening rupture deals damage once; crossing it later only applies slow.
        AffectOpponents(damage);
        StartCoroutine(Lifetime(lifetime));
    }

    private void OnDestroy()
    {
        if (_lineMaterial != null) Destroy(_lineMaterial);
    }

    private IEnumerator Lifetime(float seconds)
    {
        float end = Time.time + seconds;
        while (Time.time < end)
        {
            AffectOpponents(0f);
            yield return new WaitForFixedUpdate();
        }
        Destroy(gameObject);
    }

    private void AffectOpponents(float damage)
    {
        if (_caster == null || _caster.Registry == null) return;
        foreach (CreatureController opponent in _caster.Registry.GetOpponents(_caster.Team))
        {
            if (opponent == null || opponent.IsDead) continue;
            Vector3 position = opponent.transform.position;
            position.y = 0f;
            for (int i = 1; i < _points.Count; i++)
            {
                Vector3 a = _points[i - 1];
                Vector3 b = _points[i];
                a.y = b.y = 0f;
                Vector3 segment = b - a;
                float t = Mathf.Clamp01(Vector3.Dot(position - a, segment) / segment.sqrMagnitude);
                if ((position - (a + segment * t)).sqrMagnitude > _width * _width) continue;

                if (damage > 0f) opponent.GetComponent<StatsComponent>()?.TakeDamage(damage, _caster);
                opponent.GetComponent<MovementComponent>()?.ApplySlow(_slowMultiplier, _slowRefresh);
                break;
            }
        }
    }
}
