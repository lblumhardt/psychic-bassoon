using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Abilities/Rally")]
public class RallyAbilitySO : CreatureAbilitySO
{
    [Min(0.1f)] public float radius = 4f;
    [Range(0f, 180f)] public float directionAngle = 35f;
    [Min(1f)] public float speedMultiplier = 1.3f;
    public override string Description => $"Nearby teammates moving within {directionAngle:0} degrees of this creature's direction move {speedMultiplier:0.##}x faster.";

    public static float GetTeamMultiplier(CreatureController recipient)
    {
        if (recipient == null || recipient.IsDead || !recipient.isActiveAndEnabled || recipient.Registry == null) return 1f;
        Vector3 travel = recipient.GetComponent<MovementComponent>()?.TravelDirection ?? Vector3.zero;
        if (travel.sqrMagnitude < 0.001f) return 1f;
        float result = 1f;
        foreach (CreatureController source in recipient.Registry.GetAllies(recipient.Team))
        {
            if (source == null || source == recipient || source.IsDead || !source.isActiveAndEnabled ||
                source.Ability is not RallyAbilitySO rally) continue;
            Vector3 delta = recipient.transform.position - source.transform.position;
            delta.y = 0f;
            Vector3 sourceTravel = source.GetComponent<MovementComponent>()?.TravelDirection ?? Vector3.zero;
            if (delta.sqrMagnitude <= rally.radius * rally.radius && sourceTravel.sqrMagnitude > 0.001f &&
                Vector3.Angle(travel, sourceTravel) <= rally.directionAngle + 0.001f)
                result = Mathf.Max(result, rally.speedMultiplier); // Multiple Rally auras do not stack.
        }
        return result;
    }
}
