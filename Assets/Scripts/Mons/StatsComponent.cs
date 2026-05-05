using UnityEngine;

public class StatsComponent : MonoBehaviour
{
    private float _currentHP;

    public void Initialize(float maxHP)
    {
        _currentHP = maxHP;
    }

    public bool IsDead() {
        return _currentHP <= 0;
    }

    public void TakeDamage(float damage) {
        _currentHP -= damage;
        if (_currentHP <= 0) {
            _currentHP = 0;
        }
    }
}
