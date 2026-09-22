using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    private IReadOnlyList<AttackDataSO> _equippedMoves;
    private bool _initialized;
    private bool _abilityAssigned;
    private CreatureAbilitySO _ability;
    private CreatureStats _runtimeStats;
    private bool _statsAssigned;
    private CreatureItemSO _heldItem;
    private CreatureItemSO _spray;

    public Team Team => team;
    public bool IsDead => statsComponent != null && statsComponent.IsDead();
    public CreatureRegistry Registry => _registry;
    public CreatureAbilitySO Ability => _ability;
    public CreatureStats Stats => _runtimeStats;
    public float PowerMultiplier => Mathf.Max(0.2f, _runtimeStats.power / 5f) * ItemMultiplier(i => i.damageMultiplier);
    public float AttackSpeedMultiplier => Mathf.Max(0.2f, _runtimeStats.attackSpeed / 5f);
    public float MoveCooldownMultiplier => ItemMultiplier(i => i.moveCooldownMultiplier);
    public float ProjectileSpeedMultiplier =>
        (_ability != null ? _ability.GetProjectileSpeedMultiplier() : 1f) *
        ItemMultiplier(i => i.projectileSpeedMultiplier);
    public float MoveSpeedMultiplier
    {
        get
        {
            float statMultiplier = Mathf.Max(0.2f, _runtimeStats.moveSpeed / 5f);
            float abilityMultiplier = _ability != null
                ? _ability.GetMoveSpeedMultiplier(Time.timeSinceLevelLoad) : 1f;
            return statMultiplier * abilityMultiplier;
        }
    }

    private void Awake()
    {
        // The root primitive is retained for its collider and Rigidbody only.
        // Creature art is rendered by the child sprite quad.
        MeshRenderer rootRenderer = GetComponent<MeshRenderer>();
        if (rootRenderer != null) rootRenderer.enabled = false;
        foreach (MeshCollider spriteCollider in GetComponentsInChildren<MeshCollider>())
            spriteCollider.enabled = false;
        statsComponent = GetComponent<StatsComponent>();
        movementComponent = GetComponent<MovementComponent>();
        combatComponent = GetComponent<CombatComponent>();
        targetingComponent = GetComponent<TargetingComponent>();
    }

    private void Start()
    {
        if (!_abilityAssigned && creatureData != null)
        {
            _ability = creatureData.RollAbility();
            _abilityAssigned = true;
        }
        if (!_statsAssigned && creatureData != null)
        {
            _runtimeStats = creatureData.baseStats;
            _statsAssigned = true;
        }
        _initialized = true;
        ApplyCreatureData();

        if (GetComponent<CreatureHealthBar>() == null)
        {
            gameObject.AddComponent<CreatureHealthBar>();
        }
        if (GetComponent<CreatureVfx>() == null)
        {
            gameObject.AddComponent<CreatureVfx>();
        }
    }

    public void Configure(CreatureDataSO data, IReadOnlyList<AttackDataSO> moves,
        CreatureAbilitySO ability, CreatureStats stats, CreatureItemSO heldItem, CreatureItemSO spray)
    {
        creatureData = data;
        _equippedMoves = moves;
        _ability = ability;
        _abilityAssigned = true;
        _runtimeStats = stats;
        _statsAssigned = true;
        _heldItem = heldItem;
        _spray = spray;
        // Configure can run while BattleManager.Start is still creating the teams.
        // Apply immediately so a newly cloned creature never spends a frame at 0 HP.
        ApplyCreatureData();
    }

    public void SetTeam(Team newTeam)
    {
        team = newTeam;
        GetComponent<CreatureVfx>()?.RefreshTeamVisual();
    }

    private float ItemMultiplier(System.Func<CreatureItemSO, float> selector)
    {
        float result = 1f;
        if (_heldItem != null) result *= selector(_heldItem);
        if (_spray != null) result *= selector(_spray);
        return result;
    }

    private void ApplyCreatureData()
    {
        if (creatureData == null) return;

        PixelArtGenerator visuals = GetComponent<PixelArtGenerator>();
        if (visuals != null) visuals.SetCreatureTexture(creatureData.creatureTexture);
        statsComponent.Initialize(_runtimeStats);
        if (_equippedMoves != null)
        {
            combatComponent.ConfigureMoves(_equippedMoves);
            return;
        }

        List<AttackDataSO> defaults = new(2);
        if (creatureData.movePool != null)
        {
            foreach (AttackDataSO attack in creatureData.movePool)
            {
                if (attack == null || attack.behavior == null) continue;
                defaults.Add(attack);
                if (defaults.Count == 2) break;
            }
        }
        combatComponent.ConfigureMoves(defaults);
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
