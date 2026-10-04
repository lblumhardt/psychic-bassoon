using UnityEngine;

// Explicit opt-in for interior scenery. Arena boundary walls never receive this component.
public class WindMovable : MonoBehaviour
{
    public void Push(Vector3 displacement)
    {
        Collider shape = GetComponent<Collider>();
        Vector3 point = shape != null ? shape.bounds.center : transform.position;
        Vector3 extent = shape != null ? shape.bounds.extents * 0.98f : Vector3.one * 0.1f;
        transform.position += WindArea.ClampPush(point, displacement, transform, extent);
    }
}
