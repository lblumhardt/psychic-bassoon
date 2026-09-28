using UnityEngine;

// Assets contain configuration only. Mutable battle state belongs in PerkRuntime.
public abstract class TeamPerkSO : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
    public abstract PerkRuntime CreateRuntime();
}

public abstract class PerkRuntime
{
    public virtual void OnBattleStart(BattlePerks battle) { }
    public virtual void OnKnockout(BattlePerks battle, CreatureController victim, CreatureController source) { }
    public virtual void OnOverheal(StatsComponent target, float excess) { }
    public virtual float DefenseBonus(CreatureController creature) => 0f;
    public virtual float SpeedMultiplier => 1f;
}
