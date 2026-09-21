using UnityEngine;

[CreateAssetMenu(menuName = "Mons/Abilities/Nice Arm")]
public class NiceArmAbilitySO : CreatureAbilitySO
{
    [SerializeField, Min(1f)] private float projectileSpeedMultiplier = 1.5f;

    public override float GetProjectileSpeedMultiplier()
    {
        return Mathf.Max(1f, projectileSpeedMultiplier);
    }
}
