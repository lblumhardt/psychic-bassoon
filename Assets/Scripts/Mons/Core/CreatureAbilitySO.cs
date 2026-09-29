using UnityEngine;

public abstract class CreatureAbilitySO : ScriptableObject
{
    [SerializeField] private string displayName;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public abstract string Description { get; }

    public virtual float GetMoveSpeedMultiplier(float elapsedRoundSeconds) => 1f;
    public virtual float GetProjectileSpeedMultiplier() => 1f;
}
