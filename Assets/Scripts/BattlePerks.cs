using System.Collections.Generic;
using UnityEngine;

public class BattlePerks : MonoBehaviour
{
    private readonly List<PerkRuntime> effects = new();
    public IReadOnlyList<CreatureController> Participants { get; private set; }
    public void Initialize(IReadOnlyList<CreatureController> participants, bool includeRunPerks = true)
    {
        Participants = participants;
        effects.Clear();
        if (includeRunPerks)
            foreach (TeamPerkSO perk in RunPerks.Selected) effects.Add(perk.CreateRuntime());
        foreach (PerkRuntime effect in effects) effect.OnBattleStart(this);
    }
    public float DefenseBonus(CreatureController creature)
    {
        if (creature == null || creature.Team != Team.Player) return 0f;
        float value = 0f;
        foreach (PerkRuntime effect in effects) value += effect.DefenseBonus(creature);
        return value;
    }
    public float SpeedMultiplier(CreatureController creature)
    {
        if (creature == null || creature.Team != Team.Player) return 1f;
        float value = 1f;
        foreach (PerkRuntime effect in effects) value *= effect.SpeedMultiplier;
        return value;
    }
    public void Overheal(CreatureController creature, StatsComponent stats, float excess)
    {
        if (creature == null || creature.Team != Team.Player || excess <= 0f) return;
        foreach (PerkRuntime effect in effects) effect.OnOverheal(stats, excess);
    }
    public void Knockout(CreatureController victim, CreatureController source)
    {
        foreach (PerkRuntime effect in effects) effect.OnKnockout(this, victim, source);
    }
}
