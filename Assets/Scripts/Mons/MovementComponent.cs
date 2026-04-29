using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class MovementComponent : MonoBehaviour
{
    [SerializeField] private float moveDurationSeconds = 5f;
    [SerializeField] private float moveSpeed = 2f;

    private Rigidbody _rigidbody;
    private Vector3 _currentMoveDirection;
    private bool _isMoving;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.useGravity = false;
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
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

        float moveTimer = 0f;
        while (moveTimer < moveDurationSeconds)
        {
            yield return new WaitForFixedUpdate();
            _rigidbody.MovePosition(_rigidbody.position + _currentMoveDirection * moveSpeed * Time.fixedDeltaTime);
            moveTimer += Time.fixedDeltaTime;
        }

        _isMoving = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_isMoving || collision.contactCount == 0)
        {
            return;
        }

        // Bounce off the first contact surface while keeping movement on the XZ plane.
        Vector3 surfaceNormal = collision.GetContact(0).normal;
        _currentMoveDirection = Vector3.Reflect(_currentMoveDirection, surfaceNormal);
        _currentMoveDirection.y = 0f;
        _currentMoveDirection.Normalize();
    }
}
