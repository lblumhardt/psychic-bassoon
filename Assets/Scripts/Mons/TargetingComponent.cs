using UnityEngine;

public class TargetingComponent : MonoBehaviour
{
    private CreatureController _owner;

    private void Awake()
    {
        _owner = GetComponent<CreatureController>();
    }

    public Transform GetTarget()
    {
        if (_owner == null || _owner.Registry == null)
        {
            return null;
        }

        var opponents = _owner.Registry.GetOpponents(_owner.Team);
        CreatureController closestOpponent = null;
        float closestDistanceSqr = float.MaxValue;

        foreach (CreatureController candidate in opponents)
        {
            if (candidate == null || candidate == _owner || candidate.IsDead)
            {
                continue;
            }

            float distanceSqr = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestOpponent = candidate;
            }
        }
        Debug.Log($"Closest opponent: {closestOpponent?.name}");
        return closestOpponent != null ? closestOpponent.transform : null;
    }
}
