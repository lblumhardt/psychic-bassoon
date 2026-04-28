using UnityEngine;

public class CreatureMovement : MonoBehaviour
{
    [SerializeField] private float moveDurationSeconds = 5f;
    [SerializeField] private float pauseDurationSeconds = 1f;
    [SerializeField] private float moveSpeed = 2f;

    private void Start()
    {
        StartCoroutine(BehaviorLoop());
    }

    private System.Collections.IEnumerator BehaviorLoop()
    {
        while (true)
        {
            Vector2 random2DDirection = Random.insideUnitCircle.normalized;

            // Fallback in the unlikely event we roll a zero vector.
            if (random2DDirection == Vector2.zero)
            {
                random2DDirection = Vector2.right;
            }

            Vector3 moveDirection = new Vector3(random2DDirection.x, 0f, random2DDirection.y);

            float moveTimer = 0f;
            while (moveTimer < moveDurationSeconds)
            {
                transform.position += moveDirection * moveSpeed * Time.deltaTime;
                moveTimer += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(pauseDurationSeconds);

            // TODO: Attack placeholder.
        }
    }
}
