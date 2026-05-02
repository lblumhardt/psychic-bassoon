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

    private bool _hasLaunched;
    private bool _stuck;
    private bool _pendingStick;
    private float _travelDistanceRemaining;
    private float _postHitTravelRemaining;
    private Vector3 _lockedTravelVelocity;
    private Transform _owner;

    public void Launch(Transform owner, Vector3 direction, float speed, float maxDistance)
    {
        if (_hasLaunched)
        {
            return;
        }

        _hasLaunched = true;
        _owner = owner;
        _travelDistanceRemaining = Mathf.Max(0f, maxDistance);

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
        IgnoreOwnerCollisions();
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

        if (_travelDistanceRemaining <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        _travelDistanceRemaining -= rb.linearVelocity.magnitude * Time.fixedDeltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_stuck || _pendingStick || !_hasLaunched)
        {
            return;
        }

        CreatureController ownerCreature = _owner != null ? _owner.GetComponentInParent<CreatureController>() : null;
        CreatureController hitCreature = collision.collider.GetComponentInParent<CreatureController>();
        if (hitCreature != null && hitCreature != ownerCreature)
        {
            ApplyCreatureKnockback(hitCreature);
            Destroy(gameObject);
            return;
        }

        if (collision.collider.GetComponentInParent<RebarProjectile>() != null)
        {
            Destroy(gameObject);
            return;
        }

        if ((wallStickLayers.value & (1 << collision.gameObject.layer)) != 0)
        {
            BeginDelayedStick();
            return;
        }

        Destroy(gameObject);
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
        SetProjectileCollidersTrigger(true);
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
