using UnityEngine;
using System.Collections;

public class CreatureController : MonoBehaviour
{
    public CreatureDataSO creatureData;
    [SerializeField] private Team team;

    private StatsComponent statsComponent;
    private MovementComponent movementComponent;
    private CombatComponent combatComponent;
    private TargetingComponent targetingComponent;
    private bool _isExecutingBehavior;
    private CreatureRegistry _registry;

    public Team Team => team;
    public bool IsDead => statsComponent != null && statsComponent.IsDead();
    public CreatureRegistry Registry => _registry;

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

    private void OnEnable()
    {
        _registry?.Register(this);
    }

    private void OnDisable()
    {
        _registry?.Unregister(this);
    }

    // Update is called once per frame
    void Update()
    {
        if (statsComponent.IsDead()) {
            Debug.Log($"{name} is dead");
            _registry?.Unregister(this);
            Destroy(gameObject);
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

    public void SetRegistry(CreatureRegistry registry)
    {
        if (_registry == registry)
        {
            return;
        }

        _registry?.Unregister(this);
        _registry = registry;
        _registry?.Register(this);
    }
}
