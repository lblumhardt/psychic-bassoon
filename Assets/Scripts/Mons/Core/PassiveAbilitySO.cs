using UnityEngine;

public enum PassiveAbilityKind
{
    HotFeet, ThickSkin, SecondWind, Aftershock, CloseQuarters, LongShot,
    Fireproof, StickySituation, Bodyguard, LastStand, Scavenger, ChainReaction
}

[CreateAssetMenu(menuName = "Mons/Abilities/Combat Passive")]
public class PassiveAbilitySO : CreatureAbilitySO
{
    public PassiveAbilityKind effect;
    [Min(0f)] public float strength;
    [Min(0f)] public float duration;
    [Min(0f)] public float radius;

    public override string Description => effect switch
    {
        PassiveAbilityKind.HotFeet => $"Taking damage increases movement speed by {strength:P0} for {duration:0.#} seconds. Further hits refresh the duration.",
        PassiveAbilityKind.ThickSkin => $"Repeated hits within {duration:0.#} seconds deal {strength:P0} less damage. The first hit deals normal damage.",
        PassiveAbilityKind.SecondWind => $"Once per match, surviving a hit below 30% health restores {strength:P0} of maximum health.",
        PassiveAbilityKind.Aftershock => $"Using a move primes your next damaging hit to slow its target by {strength:P0} for {duration:0.#} seconds. Does not stack.",
        PassiveAbilityKind.CloseQuarters => $"Deal {strength:P0} more damage to enemies within {radius:0.#} units.",
        PassiveAbilityKind.LongShot => $"Projectile damage increases with travel distance, up to {strength:P0} extra at {radius:0.#} units.",
        PassiveAbilityKind.Fireproof => $"Take {strength:P0} less fire damage and no damage from lingering ground fires.",
        PassiveAbilityKind.StickySituation => $"Hitting a slowed enemy heals you for {strength:P0} of damage dealt.",
        PassiveAbilityKind.Bodyguard => $"Gain {strength:0.#} defense while within {radius:0.#} units of a living ally with a lower percentage of health.",
        PassiveAbilityKind.LastStand => $"Gain {strength:P0} attack speed while you are your team's last living creature.",
        PassiveAbilityKind.Scavenger => $"An enemy knocked out within {radius:0.#} units restores {strength:P0} of your maximum health.",
        PassiveAbilityKind.ChainReaction => $"Your knockouts trigger a burst within {radius:0.#} units, dealing {strength:0.#} base damage scaled by Power. Bursts can chain.",
        _ => "No effect."
    };
}
