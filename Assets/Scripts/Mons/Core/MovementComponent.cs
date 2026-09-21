using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class MovementComponent : MonoBehaviour
{
    [SerializeField] private float moveDurationSeconds = 5f;
    [SerializeField] private float moveDurationVarianceSeconds = 2f;
    [SerializeField] private float moveSpeed = 2f;

    private Rigidbody _rigidbody;
    private CreatureController _creature;
    private Vector3 _currentMoveDirection;
    private bool _isMoving;
    private float _knockbackTimeRemaining;
    private Vector3 _knockbackVelocityXZ;
    private float _slowMultiplier = 1f;
    private float _slowUntil;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _creature = GetComponent<CreatureController>();
        _rigidbody.useGravity = false;
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        Vector3 delta = Vector3.zero;

        if (_knockbackTimeRemaining > 0f)
        {
            _knockbackTimeRemaining -= Time.fixedDeltaTime;
            delta = _knockbackVelocityXZ * Time.fixedDeltaTime;
        }
        else if (_isMoving)
        {
            float abilityMultiplier = _creature != null ? _creature.MoveSpeedMultiplier : 1f;
            delta = _currentMoveDirection * moveSpeed * abilityMultiplier
                * (Time.time < _slowUntil ? _slowMultiplier : 1f) * Time.fixedDeltaTime;
        }

        if (delta.sqrMagnitude > 0f)
        {
            _rigidbody.MovePosition(_rigidbody.position + delta);
        }
    }

    /// <summary>
    /// Brief scripted shove on XZ; overrides wander movement while active. Clears physics velocity from impacts (e.g. rebar).
    /// </summary>
    public void ApplyKnockback(Vector3 worldDirectionXZ, float speed, float durationSeconds)
    {
        Vector3 dir = worldDirectionXZ;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            return;
        }

        dir.Normalize();
        _knockbackVelocityXZ = dir * Mathf.Max(0f, speed);
        _knockbackTimeRemaining = Mathf.Max(0f, durationSeconds);

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }

    public IEnumerator MoveRoutine()
    {
        Vector2 random2DDirection = Random.insideUnitCircle.normalized;

        // Fallback in the unlikely event we roll a zero vector.
        if (random2DDirection == Vector2.zero)
        {
            random2DDirection = Vector2.right;
        }

        _currentMoveDirection = new Vector3(random2DDirection.x, 0f, random2DDirection.y);
        _isMoving = true;

        float durationOffset = Random.Range(-moveDurationVarianceSeconds, moveDurationVarianceSeconds);
        float moveDurationThisCycle = Mathf.Max(0.1f, moveDurationSeconds + durationOffset);

        float moveTimer = 0f;
        while (moveTimer < moveDurationThisCycle)
        {
            yield return new WaitForFixedUpdate();
            moveTimer += Time.fixedDeltaTime;
        }

        _isMoving = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision);
    }

    public void ApplySlow(float speedMultiplier, float durationSeconds)
    {
        if (durationSeconds <= 0f)
        {
            return;
        }

        float until = Time.time + durationSeconds;
        if (Time.time >= _slowUntil)
        {
            _slowMultiplier = Mathf.Clamp01(speedMultiplier);
        }
        else
        {
            _slowMultiplier = Mathf.Min(_slowMultiplier, Mathf.Clamp01(speedMultiplier));
        }

        _slowUntil = Mathf.Max(_slowUntil, until);
    }

    private void OnCollisionStay(Collision collision)
    {
        HandleCollision(collision);
    }

    private void HandleCollision(Collision collision)
    {
        if (collision.contactCount == 0)
        {
            return;
        }

        Vector3 surfaceNormal = collision.GetContact(0).normal;
        surfaceNormal.y = 0f;
        if (surfaceNormal.sqrMagnitude < 0.0001f)
        {
            return;
        }

        surfaceNormal.Normalize();
        if (_knockbackTimeRemaining > 0f && Vector3.Dot(_knockbackVelocityXZ, surfaceNormal) < -0.01f)
        {
            // Stop forcing the creature into a wall and steer its next wander step away.
            _knockbackTimeRemaining = 0f;
            _rigidbody.linearVelocity = Vector3.zero;
        }

        if (_isMoving && Vector3.Dot(_currentMoveDirection, surfaceNormal) < -0.01f)
        {
            _currentMoveDirection = Vector3.Reflect(_currentMoveDirection, surfaceNormal).normalized;
        }
    }
}
