using System.Collections;
using UnityEngine;

[RequireComponent(typeof(StatsComponent), typeof(CreatureController))]
public class CreatureVfx : MonoBehaviour
{
    private StatsComponent _stats;
    private CreatureController _creature;
    private Renderer[] _renderers;
    private MaterialPropertyBlock _properties;
    private Coroutine _flashRoutine;
    private Color _teamColor;
    private bool _knockedOut;

    private void Awake()
    {
        _stats = GetComponent<StatsComponent>();
        _creature = GetComponent<CreatureController>();
        _renderers = GetComponentsInChildren<Renderer>();
        _properties = new MaterialPropertyBlock();
        RefreshTeamColor();
        CreateGroundMarker();
    }

    private void OnEnable()
    {
        _stats.Damaged += OnDamaged;
        _stats.Healed += OnHealed;
        _stats.Died += OnDied;
    }

    private void OnDisable()
    {
        _stats.Damaged -= OnDamaged;
        _stats.Healed -= OnHealed;
        _stats.Died -= OnDied;
    }

    public void PlayAttack(Transform target)
    {
        Vector3 direction = target != null ? target.position - transform.position : transform.forward;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
        Vector3 position = transform.position + Vector3.up * 0.65f + direction.normalized * 0.65f;
        CombatVfxBurst.Spawn(position, _teamColor, 7, 0.9f, 0.18f);
        StartCoroutine(PulseScale());
    }

    public void RefreshTeamVisual()
    {
        RefreshTeamColor();
        Transform marker = transform.Find("Team Ground Marker");
        if (marker != null && marker.TryGetComponent(out Renderer markerRenderer))
            markerRenderer.sharedMaterial = CombatVfxBurst.TeamMarkerMaterial(_teamColor);
    }

    private void RefreshTeamColor()
    {
        _teamColor = _creature.Team == Team.Player
            ? new Color(0.2f, 0.75f, 1f)
            : new Color(1f, 0.3f, 0.25f);
    }

    private void OnDamaged(float amount)
    {
        CombatVfxBurst.Spawn(transform.position + Vector3.up * 0.65f,
            new Color(1f, 0.25f, 0.12f), 10, 1.3f, 0.25f);
        FloatingCombatText.Spawn(transform.position + Vector3.up * 1.4f,
            $"-{amount:0.#}", new Color(1f, 0.3f, 0.2f));
        Flash(new Color(1f, 0.35f, 0.25f));
    }

    private void OnHealed(float amount)
    {
        CombatVfxBurst.Spawn(transform.position + Vector3.up * 0.55f,
            new Color(0.25f, 1f, 0.45f), 9, 0.8f, 0.35f);
        FloatingCombatText.Spawn(transform.position + Vector3.up * 1.4f,
            $"+{amount:0.#}", new Color(0.3f, 1f, 0.45f));
        Flash(new Color(0.35f, 1f, 0.55f));
    }

    private void OnDied()
    {
        if (_knockedOut) return;
        _knockedOut = true;
        StopAllCoroutines();
        _flashRoutine = null;
        CombatVfxBurst.Spawn(transform.position + Vector3.up * 0.6f,
            _teamColor, 28, 2.3f, 0.65f);
        PrepareForKnockout();
        StartCoroutine(KnockoutRoutine());
    }

    private void Flash(Color color)
    {
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine(color));
    }

    private IEnumerator FlashRoutine(Color color)
    {
        SetRendererColor(color);
        yield return new WaitForSeconds(0.09f);
        ClearRendererColor();
        _flashRoutine = null;
    }

    private IEnumerator PulseScale()
    {
        Vector3 original = transform.localScale;
        float elapsed = 0f;
        while (elapsed < 0.12f)
        {
            float pulse = Mathf.Sin(elapsed / 0.12f * Mathf.PI) * 0.08f;
            transform.localScale = original * (1f + pulse);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = original;
    }

    private void PrepareForKnockout()
    {
        _creature.StopAllCoroutines();

        MovementComponent movement = GetComponent<MovementComponent>();
        if (movement != null)
        {
            movement.StopAllCoroutines();
            movement.enabled = false;
        }
        CombatComponent combat = GetComponent<CombatComponent>();
        if (combat != null)
        {
            combat.StopAllCoroutines();
            combat.enabled = false;
        }

        foreach (Collider creatureCollider in GetComponentsInChildren<Collider>())
            creatureCollider.enabled = false;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        Transform healthBar = transform.Find("Health Bar");
        if (healthBar != null) healthBar.gameObject.SetActive(false);
        Transform groundMarker = transform.Find("Team Ground Marker");
        if (groundMarker != null) groundMarker.gameObject.SetActive(false);
    }

    private IEnumerator KnockoutRoutine()
    {
        const float duration = 1.05f;
        const float arcHeight = 6f;
        Vector3 start = transform.position;
        float outsideX = _creature.Team == Team.Player ? -17.2f : 17.2f;
        Vector3 landing = new Vector3(outsideX, 0.05f,
            Mathf.Clamp(start.z, -7.5f, 7.5f) + Random.Range(-0.25f, 0.25f));
        Quaternion startRotation = transform.rotation;
        Vector3 startScale = transform.localScale;
        float spin = _creature.Team == Team.Player ? 540f : -540f;
        Color defeatedColor = new Color(0.24f, 0.24f, 0.28f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            Vector3 position = Vector3.Lerp(start, landing, progress);
            position.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;
            transform.position = position;
            transform.rotation = Quaternion.AngleAxis(spin * progress, Vector3.up) * startRotation;
            transform.localScale = startScale * (1f + Mathf.Sin(progress * Mathf.PI) * 0.65f);
            SetRendererColor(Color.Lerp(Color.white, defeatedColor, Mathf.Min(1f, progress * 2.5f)));
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = landing;
        transform.rotation = Quaternion.AngleAxis(spin, Vector3.up) * startRotation;
        transform.localScale = startScale;
        SetRendererColor(defeatedColor);
        CombatVfxBurst.Spawn(landing + Vector3.up * 0.15f,
            new Color(0.5f, 0.5f, 0.55f), 10, 0.65f, 0.3f);
    }

    private void SetRendererColor(Color color)
    {
        foreach (Renderer renderer in _renderers)
        {
            if (renderer == null || renderer is ParticleSystemRenderer) continue;
            renderer.GetPropertyBlock(_properties);
            _properties.SetColor("_BaseColor", color);
            _properties.SetColor("_Color", color);
            renderer.SetPropertyBlock(_properties);
        }
    }

    private void ClearRendererColor()
    {
        foreach (Renderer renderer in _renderers)
            if (renderer != null && !(renderer is ParticleSystemRenderer))
                renderer.SetPropertyBlock(null);
    }

    private void CreateGroundMarker()
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "Team Ground Marker";
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = new Vector3(0f, 0.025f, 0f);
        marker.transform.localScale = new Vector3(0.72f, 0.012f, 0.72f);
        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null) Destroy(markerCollider);
        marker.GetComponent<Renderer>().sharedMaterial = CombatVfxBurst.TeamMarkerMaterial(_teamColor);
    }
}
