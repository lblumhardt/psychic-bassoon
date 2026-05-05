using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RebarProjectile : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [Tooltip("Layers this rebar is allowed to physically interact with (walls, creatures, other rebars). Leave empty to use Project layer matrix only.")]
    [SerializeField] private LayerMask collideableLayers;
    [SerializeField] private LayerMask wallStickLayers = ~0;
    [SerializeField] private float postHitTravelSeconds = 0.1f;
    [SerializeField] private float maxLifetimeSeconds = 8f;
    [SerializeField] private float stuckLifetimeSeconds = 10f;
    [SerializeField] private float creatureKnockbackSpeed = 12f;
    [SerializeField] private float creatureKnockbackDurationSeconds = 0.18f;
    [Tooltip("Two trigger colliders never raise OnTriggerEnter against each other; use overlap to destroy when rebars touch.")]
    [SerializeField] private float otherRebarOverlapRadius = 0.4f;
    [Tooltip("Layers used only for overlap checks vs other rebars. Leave empty to use this object's layer.")]
    [SerializeField] private LayerMask otherRebarOverlapMask;
    private float _damage = 5f;

    private readonly Collider[] _overlapScratch = new Collider[16];
    private LayerMask _resolvedOtherRebarMask;

    private bool _hasLaunched;
    private bool _stuck;
    private bool _pendingStick;
    private bool _limitTravelDistance;
    private float _travelDistanceRemaining;
    private float _postHitTravelRemaining;
    private Vector3 _lockedTravelVelocity;
    private Transform _owner;
    private readonly HashSet<CreatureController> _knockedCreatures = new HashSet<CreatureController>();

    public bool HasLaunched => _hasLaunched;

    public void Launch(Transform owner, Vector3 direction, float speed, float maxDistance)
    {
        if (_hasLaunched)
        {
            return;
        }

        _hasLaunched = true;
        _owner = owner;
        // maxDistance <= 0: no distance cap — projectile lives until maxLifetimeSeconds (and collisions / stick flow).
        _limitTravelDistance = maxDistance > 0f;
        _travelDistanceRemaining = _limitTravelDistance ? maxDistance : 0f;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = direction * Mathf.Max(0f, speed);

        ApplyCollisionLayerFilter();
        SetProjectileCollidersTrigger(true);
        IgnoreOwnerCollisions();
        _resolvedOtherRebarMask = otherRebarOverlapMask.value != 0
            ? otherRebarOverlapMask
            : (LayerMask)(1 << gameObject.layer);
        Destroy(gameObject, maxLifetimeSeconds);
    }

    private void ApplyCollisionLayerFilter()
    {
        if (collideableLayers.value == 0)
        {
            return;
        }

        LayerMask exclude = (LayerMask)~collideableLayers.value;
        Collider[] projectileColliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < projectileColliders.Length; i++)
        {
            Collider col = projectileColliders[i];
            if (col != null)
            {
                col.excludeLayers = exclude;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!_hasLaunched || _stuck || rb == null)
        {
            return;
        }

        if (_pendingStick)
        {
            if (_postHitTravelRemaining > 0f)
            {
                rb.linearVelocity = _lockedTravelVelocity;
                _postHitTravelRemaining -= Time.fixedDeltaTime;
            }
            else
            {
                FreezeInPlace();
            }

            return;
        }

        TryResolveOtherRebarOverlap();

        if (_limitTravelDistance)
        {
            if (_travelDistanceRemaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            _travelDistanceRemaining -= rb.linearVelocity.magnitude * Time.fixedDeltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_stuck || _pendingStick || !_hasLaunched)
        {
            return;
        }

        CreatureController ownerCreature = _owner != null ? _owner.GetComponentInParent<CreatureController>() : null;
        CreatureController hitCreature = other.GetComponentInParent<CreatureController>();
        if (hitCreature != null && hitCreature != ownerCreature)
        {
            if (_knockedCreatures.Add(hitCreature))
            {
                ApplyDamage(hitCreature);
                ApplyCreatureKnockback(hitCreature);
            }

            return;
        }

        RebarProjectile otherRebar = other.GetComponentInParent<RebarProjectile>();
        if (otherRebar != null && otherRebar != this && otherRebar.HasLaunched)
        {
            Destroy(gameObject);
            return;
        }

        if ((wallStickLayers.value & (1 << other.gameObject.layer)) != 0)
        {
            BeginDelayedStick();
            return;
        }

        Destroy(gameObject);
    }

    private void TryResolveOtherRebarOverlap()
    {
        if (otherRebarOverlapRadius <= 0f || rb == null)
        {
            return;
        }

        int hitCount = Physics.OverlapSphereNonAlloc(
            rb.position,
            otherRebarOverlapRadius,
            _overlapScratch,
            _resolvedOtherRebarMask,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider c = _overlapScratch[i];
            if (c == null)
            {
                continue;
            }

            RebarProjectile otherRebar = c.GetComponentInParent<RebarProjectile>();
            if (otherRebar != null && otherRebar != this && otherRebar.HasLaunched)
            {
                Destroy(gameObject);
                return;
            }
        }
    }

    private void ApplyDamage(CreatureController creature) {
        StatsComponent stats = creature.GetComponent<StatsComponent>();
        if (stats != null) {
            stats.TakeDamage(_damage);
        }
    }

    private void ApplyCreatureKnockback(CreatureController creature)
    {
        Vector3 travel = rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f
            ? rb.linearVelocity
            : transform.forward;
        travel.y = 0f;
        if (travel.sqrMagnitude < 0.0001f)
        {
            travel = transform.forward;
            travel.y = 0f;
        }

        travel.Normalize();
        Vector3 sideways = Vector3.Cross(Vector3.up, travel).normalized;
        if (sideways.sqrMagnitude < 0.0001f)
        {
            sideways = Vector3.right;
        }

        Vector3 toCreature = creature.transform.position - transform.position;
        toCreature.y = 0f;
        float sign = Vector3.Dot(sideways, toCreature) >= 0f ? 1f : -1f;
        Vector3 blastDirection = sideways * sign;

        MovementComponent movement = creature.GetComponent<MovementComponent>();
        if (movement != null)
        {
            movement.ApplyKnockback(blastDirection, creatureKnockbackSpeed, creatureKnockbackDurationSeconds);
        }
    }

    private void BeginDelayedStick()
    {
        _pendingStick = true;
        _postHitTravelRemaining = Mathf.Max(0f, postHitTravelSeconds);
        _lockedTravelVelocity = rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f
            ? rb.linearVelocity
            : transform.forward;
    }

    private void FreezeInPlace()
    {
        if (_stuck)
        {
            return;
        }

        _stuck = true;
        _pendingStick = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        SetProjectileCollidersTrigger(false);

        Destroy(gameObject, stuckLifetimeSeconds);
    }

    private void IgnoreOwnerCollisions()
    {
        if (_owner == null)
        {
            return;
        }

        Collider[] ownerColliders = _owner.GetComponentsInChildren<Collider>();
        Collider[] projectileColliders = GetComponentsInChildren<Collider>();

        for (int i = 0; i < ownerColliders.Length; i++)
        {
            Collider ownerCollider = ownerColliders[i];
            if (ownerCollider == null)
            {
                continue;
            }

            for (int j = 0; j < projectileColliders.Length; j++)
            {
                Collider projectileCollider = projectileColliders[j];
                if (projectileCollider == null)
                {
                    continue;
                }

                Physics.IgnoreCollision(ownerCollider, projectileCollider, true);
            }
        }
    }

    private void SetProjectileCollidersTrigger(bool isTrigger)
    {
        Collider[] projectileColliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < projectileColliders.Length; i++)
        {
            Collider projectileCollider = projectileColliders[i];
            if (projectileCollider == null)
            {
                continue;
            }

            projectileCollider.isTrigger = isTrigger;
        }
    }
}
