using UnityEngine;

// Shared XZ hit geometry for wind, including effects with no physics collider.
public readonly struct WindArea
{
    public readonly Vector3 Origin, Direction;
    public readonly float Length, Width;
    public WindArea(Vector3 origin, Vector3 direction, float length, float width)
    {
        direction.y = 0f;
        Origin = origin;
        Direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
        Length = Mathf.Max(0f, length);
        Width = Mathf.Max(0f, width);
    }

    public bool Contains(Vector3 point, float radius = 0f)
    {
        Vector3 delta = point - Origin;
        delta.y = 0f;
        float along = Vector3.Dot(delta, Direction);
        float side = Mathf.Abs(Vector3.Dot(delta, Vector3.Cross(Vector3.up, Direction)));
        return along >= -radius && along <= Length + radius && side <= Width * 0.5f + radius;
    }

    public bool Reaches(Vector3 point, float radius = 0f)
    {
        if (!Contains(point, radius)) return false;
        Vector3 start = Origin; start.y = point.y = 0.6f;
        foreach (RaycastHit hit in Physics.RaycastAll(start, point - start, Vector3.Distance(start, point),
                     LayerMask.GetMask("Wall"), QueryTriggerInteraction.Ignore))
            if (hit.collider.GetComponentInParent<WindMovable>() == null) return false;
        return true;
    }

    public static Vector3 ClampPush(Vector3 point, Vector3 displacement, Transform ignore = null, Vector3? halfExtents = null)
    {
        displacement.y = 0f;
        float distance = displacement.magnitude;
        if (distance < 0.0001f) return Vector3.zero;
        point.y = 0.75f;
        Vector3 extents = halfExtents ?? new Vector3(0.12f, 0.1f, 0.12f);
        extents.y = 0.1f;
        float allowed = distance;
        foreach (RaycastHit hit in Physics.BoxCastAll(point, extents, displacement / distance,
                     Quaternion.identity, distance, LayerMask.GetMask("Wall"), QueryTriggerInteraction.Ignore))
        {
            if (ignore != null && (hit.transform == ignore || hit.transform.IsChildOf(ignore))) continue;
            allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - 0.02f));
        }
        return displacement / distance * allowed;
    }
}
