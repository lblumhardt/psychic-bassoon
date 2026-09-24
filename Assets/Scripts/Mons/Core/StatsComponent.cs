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

    public float HealthFraction => _maxHP > 0f ? Mathf.Clamp01(_currentHP / _maxHP) : 0f;
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
    }

    public float TakeDamage(float amount, CreatureController source = null)
    {
        if (MatchFinished || amount <= 0f || IsDead()) return 0f;
        float mitigated = amount * (10f / (10f + _defense));
        float dealt = Mathf.Min(_currentHP, mitigated);
        _currentHP -= dealt;
        DamageTaken += dealt;
        StatsComponent sourceStats = source != null ? source.GetComponent<StatsComponent>() : null;
        if (sourceStats != null) sourceStats.DamageDealt += dealt;
        Damaged?.Invoke(dealt);
        if (_currentHP <= 0f && !_deathReported)
        {
            _deathReported = true;
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
        if (healed > 0f) Healed?.Invoke(healed);
        return healed;
    }

    public bool IsDead() {
        return _currentHP <= 0;
    }
}
