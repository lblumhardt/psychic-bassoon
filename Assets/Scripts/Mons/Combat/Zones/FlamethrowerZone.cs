using UnityEngine;

public class FlamethrowerZone : MonoBehaviour
{
    private CreatureController _caster;
    private CreatureRegistry _registry;
    private Team _team;
    private FlamethrowerBehaviorSO _settings;
    private SpriteRenderer[] _flames;
    private Vector3[] _points;
    private SpriteRenderer[] _groundFires;
    private Vector3[] _groundPoints;
    private Vector3 _direction;
    private Vector3 _origin;
    private float _range, _damage, _startTime, _nextDamageTime, _groundStart, _groundLifetime;
    private float _surfaceY;
    private int _visibleCount, _wallMask;
    private Camera _camera;
    private Sprite _sprite, _placeholderSprite;
    private Texture2D _placeholderTexture;
    public bool StreamActive { get; private set; }

    public void Initialize(CreatureController caster, FlamethrowerBehaviorSO settings,
        Vector3 direction, float range, float damage, bool plus)
    {
        _caster = caster;
        _registry = caster.Registry;
        _team = caster.Team;
        _settings = settings;
        _direction = direction;
        _range = range;
        _damage = damage;
        _startTime = Time.time;
        _nextDamageTime = Time.time;
        _groundLifetime = Mathf.Max(0.1f, settings.groundFireSeconds) *
            (plus ? Mathf.Max(1f, settings.plusGroundLifetimeMultiplier) : 1f);
        ArenaGenerator arena = FindFirstObjectByType<ArenaGenerator>();
        _surfaceY = arena != null ? arena.FloorSurfaceY + 0.08f : caster.transform.position.y + 0.06f;
        _wallMask = LayerMask.GetMask("Wall");
        _camera = Camera.main;
        _sprite = settings.fireSprite != null ? settings.fireSprite : CreatePlaceholder();
        int count = Mathf.Clamp(settings.flameCount, 2, 32);
        _points = new Vector3[count];
        _flames = new SpriteRenderer[count];
        for (int i = 0; i < count; i++) _flames[i] = CreateFlame("Stream Flame " + (i + 1));
        StreamActive = true;
        UpdateStream(0f);
    }

    private SpriteRenderer CreateFlame(string flameName)
    {
        GameObject flame = new GameObject(flameName);
        flame.transform.SetParent(transform, false);
        SpriteRenderer renderer = flame.AddComponent<SpriteRenderer>();
        renderer.sprite = _sprite;
        renderer.enabled = false;
        return renderer;
    }

    private void Update()
    {
        if (_registry == null) { Destroy(gameObject); return; }
        if (StreamActive)
        {
            if (_caster == null || _caster.IsDead || !_caster.isActiveAndEnabled)
            {
                Destroy(gameObject);
                return;
            }
            UpdateStream(Time.time - _startTime);
        }
        else if (Time.time - _groundStart >= _groundLifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time >= _nextDamageTime)
        {
            DealDamage();
            _nextDamageTime = Time.time + Mathf.Max(0.05f, _settings.damageInterval);
        }
    }

    private void UpdateStream(float elapsed)
    {
        float interval = Mathf.Max(0.01f, _settings.secondsBetweenFlames);
        float extendSeconds = (_flames.Length - 1) * interval;
        float waveStart = extendSeconds + Mathf.Max(0f, _settings.straightHoldSeconds);
        float waveDuration = Mathf.Max(0.01f, _settings.waveSeconds);
        float fadeStart = waveStart + waveDuration;
        float fadeDuration = Mathf.Max(0.01f, _settings.fadeSeconds);
        if (elapsed >= fadeStart + fadeDuration)
        {
            StartGroundFires();
            return;
        }

        _origin = _caster.transform.position;
        _origin.y = _surfaceY;
        Vector3 sideways = Vector3.Cross(Vector3.up, _direction);
        float waveTime = Mathf.Clamp(elapsed - waveStart, 0f, waveDuration);
        float ramp = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(waveTime / 0.2f));
        int requestedCount = Mathf.Min(_flames.Length, 1 + Mathf.FloorToInt(elapsed / interval));
        _visibleCount = 0;
        Vector3 previous = _origin;
        bool blocked = false;
        for (int i = 0; i < _flames.Length; i++)
        {
            float t = (i + 1f) / _flames.Length;
            // A wave travels back from the tip; the mouth stays nearly still.
            float wave = Mathf.Sin(waveTime * Mathf.PI * 2f * _settings.waveFrequency - (1f - t) * Mathf.PI * 2f);
            Vector3 point = _origin + _direction * (_range * t) + sideways *
                (wave * ramp * t * t * _settings.waveAmplitude);
            if (i < requestedCount && !blocked)
                blocked = Physics.Linecast(previous + Vector3.up * 0.4f, point + Vector3.up * 0.4f,
                    _wallMask, QueryTriggerInteraction.Ignore);
            _flames[i].enabled = i < requestedCount && !blocked;
            if (!_flames[i].enabled) continue;
            _points[i] = point;
            previous = point;
            _visibleCount++;
            _flames[i].color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((elapsed - fadeStart) / fadeDuration));
        }
    }

    private void StartGroundFires()
    {
        StreamActive = false;
        foreach (SpriteRenderer flame in _flames) flame.enabled = false;
        int count = Mathf.Min(Mathf.Clamp(_settings.groundFireCount, 2, 3), _visibleCount);
        _groundFires = new SpriteRenderer[count];
        _groundPoints = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            int index = Mathf.Clamp(Mathf.FloorToInt((i + 1f) * _visibleCount / (count + 1f)), 0, _visibleCount - 1);
            _groundPoints[i] = _points[index];
            _groundFires[i] = CreateFlame("Lingering Ground Fire " + (i + 1));
            _groundFires[i].enabled = true;
        }
        _groundStart = Time.time;
        _nextDamageTime = Time.time;
    }

    private void DealDamage()
    {
        foreach (CreatureController opponent in _registry.GetOpponents(_team))
        {
            if (opponent == null || opponent.IsDead || !opponent.isActiveAndEnabled) continue;
            Vector3 position = opponent.transform.position;
            position.y = _surfaceY;
            bool hit = false;
            Vector3 closest = position;
            if (StreamActive)
            {
                for (int i = 0; i < _visibleCount && !hit; i++)
                {
                    Vector3 a = i == 0 ? _origin : _points[i - 1];
                    Vector3 segment = _points[i] - a;
                    float t = Mathf.Clamp01(Vector3.Dot(position - a, segment) / Mathf.Max(0.0001f, segment.sqrMagnitude));
                    closest = a + segment * t;
                    hit = (position - closest).sqrMagnitude <= _settings.hitRadius * _settings.hitRadius;
                }
            }
            else
            {
                foreach (Vector3 point in _groundPoints)
                {
                    if ((position - point).sqrMagnitude > _settings.groundFireRadius * _settings.groundFireRadius) continue;
                    closest = point;
                    hit = true;
                    break;
                }
            }
            if (!hit || Physics.Linecast(closest + Vector3.up * 0.4f, position + Vector3.up * 0.4f,
                _wallMask, QueryTriggerInteraction.Ignore)) continue;
            // Overlapping flames from this cast never multiply the tick damage.
            opponent.GetComponent<StatsComponent>()?.TakeDamage(
                _damage * (StreamActive ? 1f : _settings.groundDamageMultiplier), _caster);
        }
    }

    private void LateUpdate()
    {
        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;
        if (StreamActive)
        {
            for (int i = 0; i < _visibleCount; i++) DrawFlame(_flames[i], _points[i], i, 1f);
        }
        else if (_groundFires != null)
        {
            float alpha = Mathf.Clamp01((_groundLifetime - (Time.time - _groundStart)) / 0.4f);
            for (int i = 0; i < _groundFires.Length; i++)
            {
                _groundFires[i].color = new Color(1f, 1f, 1f, alpha);
                DrawFlame(_groundFires[i], _groundPoints[i], i, 0.8f);
            }
        }
    }

    private void DrawFlame(SpriteRenderer flame, Vector3 point, int index, float sizeMultiplier)
    {
        float flicker = 1f + Mathf.Sin(Time.time * 17f + index * 1.7f) * 0.06f;
        float size = Mathf.Max(0.1f, _settings.spriteSize) * sizeMultiplier * flicker;
        flame.transform.rotation = _camera.transform.rotation;
        // Normalize arbitrary sprite dimensions and anchor its bottom above the floor.
        Vector3 boundsSize = _sprite.bounds.size;
        float scale = size / Mathf.Max(0.01f, boundsSize.y);
        flame.transform.localScale = Vector3.one * scale;
        Vector3 center = point + Vector3.up * (size * 0.5f + 0.04f);
        flame.transform.position = center - flame.transform.rotation * (_sprite.bounds.center * scale);
    }

    private Sprite CreatePlaceholder()
    {
        // A small code-generated flame keeps the attack usable until art is assigned.
        const int width = 16, height = 24;
        _placeholderTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        _placeholderTexture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float t = y / (height - 1f);
            float center = 7.5f + Mathf.Sin(t * 8f) * t * 2.5f;
            float halfWidth = Mathf.Sin(Mathf.Pow(t, 0.65f) * Mathf.PI) * 7f;
            for (int x = 0; x < width; x++)
            {
                float distance = Mathf.Abs(x - center);
                if (distance > halfWidth) continue;
                pixels[y * width + x] = distance < halfWidth * 0.45f && t < 0.55f
                    ? new Color(1f, 0.94f, 0.35f) : distance < halfWidth * 0.8f
                    ? new Color(1f, 0.55f, 0.05f) : new Color(1f, 0.18f, 0.02f);
            }
        }
        _placeholderTexture.SetPixels(pixels);
        _placeholderTexture.Apply();
        _placeholderSprite = Sprite.Create(_placeholderTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), height);
        return _placeholderSprite;
    }

    private void OnDestroy()
    {
        if (_placeholderSprite != null) Destroy(_placeholderSprite);
        if (_placeholderTexture != null) Destroy(_placeholderTexture);
    }
}
