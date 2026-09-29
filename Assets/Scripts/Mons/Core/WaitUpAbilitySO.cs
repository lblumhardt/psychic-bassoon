using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Abilities/Wait Up")]
public class WaitUpAbilitySO : CreatureAbilitySO
{
    [SerializeField, Min(0.1f)] private float rampSeconds = 30f;
    [SerializeField, Min(1f)] private float maximumMultiplier = 2f;
    public override string Description => $"Movement speed gradually increases from 1x to {maximumMultiplier:0.##}x over the first {Mathf.Max(0.1f, rampSeconds):0.#} seconds of the match.";

    public override float GetMoveSpeedMultiplier(float elapsedRoundSeconds)
    {
        return Mathf.Lerp(1f, maximumMultiplier,
            Mathf.Clamp01(elapsedRoundSeconds / Mathf.Max(0.1f, rampSeconds)));
    }
}
