using UnityEngine;
using System.Collections;

public class CreatureController : MonoBehaviour
{
    public CreatureDataSO creatureData;
    
    private StatsComponent statsComponent;
    private MovementComponent movementComponent;
    private CombatComponent combatComponent;
    private TargetingComponent targetingComponent;
    private bool _isExecutingBehavior;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        statsComponent = GetComponent<StatsComponent>();
        movementComponent = GetComponent<MovementComponent>();
        combatComponent = GetComponent<CombatComponent>();
        targetingComponent = GetComponent<TargetingComponent>();

        if (creatureData != null)
        {
            statsComponent.Initialize(creatureData.maxHP);
            combatComponent.attacks = creatureData.attacks;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (statsComponent.IsDead()) {
            return;
        }

        if (_isExecutingBehavior)
        {
            return;
        }

        StartCoroutine(BehaviorRoutine());
    }

    private IEnumerator BehaviorRoutine()
    {
        _isExecutingBehavior = true;

        // Start movement coroutine.
        yield return movementComponent.MoveRoutine();

        // Once movement is done, grab a target from targeting component.
        Transform target = targetingComponent.GetTarget();

        // Once we have a target, start attack coroutine.
        if (target != null)
        {
            yield return combatComponent.AttackRoutine(target);
        }

        _isExecutingBehavior = false;
    }
}
