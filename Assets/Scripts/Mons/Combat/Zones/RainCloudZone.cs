using System.Collections.Generic;
using UnityEngine;

public class RainCloudZone : MonoBehaviour
{
    private readonly Dictionary<CreatureController, float> _nextDamageTime = new Dictionary<CreatureController, float>();
    private readonly Transform[] _drops = new Transform[12];
    private CreatureController _caster;
    private Vector3 _direction;
    private float _damage;
    private float _speed;
    private float _radius;
    private float _interval;
    private float _height;
    private float _endTime;
    private Transform _visualRoot;
    private Camera _camera;
    private Material _cloudMaterial;
    private Material _rainMaterial;

    public void Initialize(CreatureController caster, float damage, Vector3 direction, float lifetime,
        float speed, float radius, float interval, float height)
    {
        _caster = caster;
        _damage = damage;
        _direction = direction;
        _speed = speed;
        _radius = radius;
        _interval = interval;
        _height = height;
        _endTime = Time.time + lifetime;

        // Draw the effect in the camera's screen plane. The damage footprint still follows
        // this object's XZ position, but the rain visually falls toward screen bottom.
        _camera = Camera.main;
        _visualRoot = new GameObject("Camera Facing Rain").transform;
        _visualRoot.SetParent(transform, false);
        _visualRoot.localPosition = Vector3.up * _height;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        _cloudMaterial = new Material(shader);
        _cloudMaterial.color = new Color(0.27f, 0.36f, 0.52f);
        _rainMaterial = new Material(shader);
        _rainMaterial.color = new Color(0.37f, 0.76f, 1f);
        for (int i = 0; i < 3; i++)
        {
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = "Cloud Puff";
            puff.transform.SetParent(_visualRoot, false);
            puff.transform.localPosition = new Vector3((i - 1) * radius * 0.43f, i == 1 ? 0.75f : 0.55f, -0.05f);
            puff.transform.localScale = new Vector3(radius * 0.95f, i == 1 ? 0.9f : 0.7f, 0.18f);
            Destroy(puff.GetComponent<Collider>());
            puff.GetComponent<Renderer>().sharedMaterial = _cloudMaterial;
        }

        for (int i = 0; i < _drops.Length; i++)
        {
            GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            drop.name = "Rain Drop";
            drop.transform.SetParent(_visualRoot, false);
            drop.transform.localScale = new Vector3(0.055f, 0.38f, 1f);
            Destroy(drop.GetComponent<Collider>());
            drop.GetComponent<Renderer>().sharedMaterial = _rainMaterial;
            _drops[i] = drop.transform;
        }
    }

    private void FixedUpdate()
    {
        if (Time.time >= _endTime || _caster == null || _caster.Registry == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += _direction * (_speed * Time.fixedDeltaTime);
        float radiusSquared = _radius * _radius;
        foreach (CreatureController opponent in _caster.Registry.GetOpponents(_caster.Team))
        {
            if (opponent == null || opponent.IsDead) continue;
            Vector3 delta = opponent.transform.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > radiusSquared) continue;
            if (_nextDamageTime.TryGetValue(opponent, out float nextTime) && Time.time < nextTime) continue;

            opponent.GetComponent<StatsComponent>()?.TakeDamage(_damage, _caster);
            _nextDamageTime[opponent] = Time.time + _interval;
        }
    }

    private void LateUpdate()
    {
        if (_camera == null) _camera = Camera.main;
        if (_camera != null)
            _visualRoot.rotation = Quaternion.LookRotation(-_camera.transform.forward, _camera.transform.up);

        for (int i = 0; i < _drops.Length; i++)
        {
            if (_drops[i] == null) continue;
            float x = (i % 6 - 2.5f) * _radius * 0.27f;
            float y = 0.2f - Mathf.Repeat(i * 0.31f + Time.time * 3.5f, 1.5f);
            _drops[i].localPosition = new Vector3(x, y, 0.05f);
        }
    }

    private void OnDestroy()
    {
        if (_cloudMaterial != null) Destroy(_cloudMaterial);
        if (_rainMaterial != null) Destroy(_rainMaterial);
    }
}
