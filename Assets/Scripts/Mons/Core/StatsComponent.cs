using System;
using UnityEngine;

public class StatsComponent : MonoBehaviour
{
    private float _currentHP;
    private float _maxHP;
    private int _defense;
    private bool _deathReported;

    public event Action<float> Damaged;
    public event Action<float> Healed;
    public event Action Died;
    public event Action Revived;

    public float HealthFraction => _maxHP > 0f ? Mathf.Clamp01(_currentHP / _maxHP) : 0f;
    public float MaxHP => _maxHP;
    public float Shield { get; private set; }
    public float ShieldAbsorbed { get; private set; }
    public float CurrentHP => _currentHP;
    public float DamageDealt { get; private set; }
    public float DamageTaken { get; private set; }
    public float HealingDone { get; private set; }
    public bool MatchFinished { get; set; }

    public void Initialize(CreatureStats stats)
    {
        _maxHP = Mathf.Max(1f, stats.hp);
        _currentHP = _maxHP;
        _defense = Mathf.Max(1, stats.defense);
        _deathReported = false;
        DamageDealt = DamageTaken = HealingDone = 0f;
        MatchFinished = false;
        Shield = ShieldAbsorbed = 0f;
    }

    public float TakeDamage(float amount, CreatureController source = null,
        DamageKind kind = DamageKind.Normal, float projectileDistance = 0f)
    {
        if (MatchFinished || amount <= 0f || IsDead()) return 0f;
        CreatureController owner = GetComponent<CreatureController>();
        amount *= source?.AbilityRuntime?.OutgoingMultiplier(owner, projectileDistance) ?? 1f;
        amount *= owner?.AbilityRuntime?.IncomingMultiplier(kind) ?? 1f;
        if (amount <= 0f) return 0f;
        BattlePerks perks = owner != null && owner.Registry != null ? owner.Registry.Perks : null;
        float defense = _defense + (perks != null ? perks.DefenseBonus(owner) : 0f) + (owner?.AbilityRuntime?.DefenseBonus ?? 0f);
        float mitigated = amount * (10f / (10f + Mathf.Max(0f, defense)));
        mitigated = owner != null ? owner.BlockFirstHit(mitigated) : mitigated;
        float absorbed = Mathf.Min(Shield, mitigated);
        Shield -= absorbed;
        ShieldAbsorbed += absorbed;
        mitigated -= absorbed;
        float dealt = Mathf.Min(_currentHP, mitigated);
        _currentHP -= dealt;
        DamageTaken += dealt;
        StatsComponent sourceStats = source != null ? source.GetComponent<StatsComponent>() : null;
        if (sourceStats != null) sourceStats.DamageDealt += dealt;
        Damaged?.Invoke(dealt);
        owner?.AbilityRuntime?.OnDamaged(dealt);
        source?.AbilityRuntime?.OnHit(owner, dealt);
        if (_currentHP <= 0f && !_deathReported)
        {
            _deathReported = true;
            owner?.BeginRevival();
            if (perks != null) perks.Knockout(owner, source);
            owner?.Registry?.NotifyKnockout(owner, source);
            Died?.Invoke();
        }
        return dealt;
    }

    public float Heal(float amount, CreatureController source = null)
    {
        if (MatchFinished || amount <= 0f || IsDead()) return 0f;
        float healed = Mathf.Min(_maxHP - _currentHP, amount);
        _currentHP += healed;
        StatsComponent sourceStats = source != null ? source.GetComponent<StatsComponent>() : null;
        if (sourceStats != null) sourceStats.HealingDone += healed;
        CreatureController owner = GetComponent<CreatureController>();
        if (owner != null && owner.Registry != null && owner.Registry.Perks != null)
            owner.Registry.Perks.Overheal(owner, this, amount - healed);
        if (healed > 0f) Healed?.Invoke(healed);
        return healed;
    }

    public void AddShield(float amount, float cap)
    {
        if (MatchFinished || IsDead() || amount <= 0f) return;
        Shield = Mathf.Max(Shield, Mathf.Min(cap, Shield + amount));
    }

    public bool IsDead() {
        return _currentHP <= 0;
    }

    public void Revive()
    {
        if (MatchFinished || !IsDead()) return;
        _currentHP = 1f;
        _deathReported = false;
        Revived?.Invoke();
    }
}
