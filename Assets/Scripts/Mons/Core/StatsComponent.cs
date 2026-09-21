using UnityEngine;

public class StatsComponent : MonoBehaviour
{
    private float _currentHP;
    private float _maxHP;
    private int _defense;

    public float HealthFraction => _maxHP > 0f ? Mathf.Clamp01(_currentHP / _maxHP) : 0f;
    public float CurrentHP => _currentHP;

    public void Initialize(CreatureStats stats)
    {
        _maxHP = Mathf.Max(1f, stats.hp);
        _currentHP = _maxHP;
        _defense = Mathf.Max(1, stats.defense);
    }

    public float TakeDamage(float amount)
    {
        if (amount <= 0f || IsDead()) return 0f;
        float mitigated = amount * (10f / (10f + _defense));
        float dealt = Mathf.Min(_currentHP, mitigated);
        _currentHP -= dealt;
        return dealt;
    }

    public float Heal(float amount)
    {
        if (amount <= 0f || IsDead()) return 0f;
        float healed = Mathf.Min(_maxHP - _currentHP, amount);
        _currentHP += healed;
        return healed;
    }

    public bool IsDead() {
        return _currentHP <= 0;
    }
}
