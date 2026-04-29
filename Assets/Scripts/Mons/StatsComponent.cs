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
}
