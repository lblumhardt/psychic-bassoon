using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public enum RoundResult
{
    None,
    Win,
    Loss
}

public class BattleManager : MonoBehaviour
{
    [SerializeField] private float resultDisplaySeconds = 2f;
    [SerializeField] private string shopSceneName = "ShopScene";
    [SerializeField] private CreatureDataSO[] opponentMonsterPool;

    private CreatureRegistry _creatureRegistry;
    private ArenaGenerator _arena;
    private bool _roundStarted;
    private bool _roundEnded;
    private VisualElement _battleUiRoot;
    private Label _speedLabel;
    private int _roundReadyFrame;

    public static RoundResult LastResult { get; private set; } = RoundResult.None;

    public static void ResetResult() => LastResult = RoundResult.None;

    private void Awake()
    {
        Time.timeScale = 1f;
        GameObject registryObject = new GameObject("CreatureRegistry");
        registryObject.transform.SetParent(transform);
        _creatureRegistry = registryObject.AddComponent<CreatureRegistry>();
        _arena = gameObject.AddComponent<ArenaGenerator>();
        _arena.GenerateRandomArena();
        LastResult = RoundResult.None;
    }

    private void Start()
    {
        CreatureController[] creatures = FindObjectsByType<CreatureController>(FindObjectsSortMode.None);
        CreatureController playerTemplate = null;
        CreatureController enemyTemplate = null;
        foreach (CreatureController creature in creatures)
        {
            if (creature.Team == Team.Player)
            {
                if (playerTemplate == null) playerTemplate = creature;
                if (RunRoster.Members.Count == 0)
                    RunRoster.TryAdd(CreatureInstance.Generate(creature.creatureData));
            }
            else if (enemyTemplate == null)
            {
                enemyTemplate = creature;
            }
        }

        if (playerTemplate != null && RunRoster.Members.Count > 0)
        {
            CreatureInstance starter = RunRoster.Members[0];
            playerTemplate.Configure(starter.Species, starter.EquippedMoves, starter.Ability,
                starter.Stats, starter.HeldItem, starter.Spray);
            for (int i = 1; i < RunRoster.Members.Count; i++)
            {
                int column = (i - 1) % 3;
                int row = (i - 1) / 3;
                Vector3 offset = new Vector3((column - 1) * 1.8f, 0f, (row + 1) * 1.8f);
                GameObject teammate = Instantiate(playerTemplate.gameObject,
                    playerTemplate.transform.position + offset, playerTemplate.transform.rotation);
                CreatureInstance member = RunRoster.Members[i];
                teammate.name = member.Species.creatureName;
                teammate.GetComponent<CreatureController>().Configure(
                    member.Species, member.EquippedMoves, member.Ability, member.Stats,
                    member.HeldItem, member.Spray);
            }
        }

        List<CreatureDataSO> enemySpecies = new();
        if (opponentMonsterPool != null)
            foreach (CreatureDataSO species in opponentMonsterPool)
                if (species != null) enemySpecies.Add(species);
        if (enemySpecies.Count == 0 && enemyTemplate != null)
            enemySpecies.Add(enemyTemplate.creatureData);

        OpponentRoster.PrepareForRound(enemySpecies, Resources.LoadAll<CreatureItemSO>("Items"));
        if (enemyTemplate != null)
        {
            enemyTemplate.gameObject.SetActive(false);
            Destroy(enemyTemplate.gameObject);
        }
        if (playerTemplate != null && OpponentRoster.Members.Count > 0)
        {
            for (int i = 0; i < OpponentRoster.Members.Count; i++)
            {
                GameObject teammate = Instantiate(playerTemplate.gameObject,
                    Vector3.zero, playerTemplate.transform.rotation);
                CreatureInstance member = OpponentRoster.Members[i];
                teammate.name = $"Opponent {i + 1} - {member.Species.creatureName}";
                CreatureController controller = teammate.GetComponent<CreatureController>();
                controller.SetTeam(Team.Enemy);
                ConfigureCreature(controller, member);
            }
        }

        BuildBattleUi();

        creatures = FindObjectsByType<CreatureController>(FindObjectsSortMode.None);
        int playerSpawnIndex = 0;
        int enemySpawnIndex = 0;
        foreach (CreatureController creature in creatures)
        {
            int spawnIndex = creature.Team == Team.Player ? playerSpawnIndex++ : enemySpawnIndex++;
            creature.transform.position = _arena.GetSpawnPoint(creature.Team, spawnIndex);
            Rigidbody body = creature.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = creature.transform.position;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            creature.SetRegistry(_creatureRegistry);
        }

        _roundStarted = HasTeam(_creatureRegistry.playerCreatures) && HasTeam(_creatureRegistry.enemyCreatures);
        _roundReadyFrame = Time.frameCount + 1;
        if (!_roundStarted)
        {
            Debug.LogWarning("Battle needs at least one player and one enemy creature to start.", this);
        }
    }

    private void Update()
    {
        if (!_roundStarted || _roundEnded || Time.frameCount < _roundReadyFrame) return;

        bool playerAlive = HasLivingCreature(_creatureRegistry.playerCreatures);
        bool enemyAlive = HasLivingCreature(_creatureRegistry.enemyCreatures);
        if (playerAlive && enemyAlive) return;

        // If both teams die during the same frame, count it as a loss.
        LastResult = playerAlive ? RoundResult.Win : RoundResult.Loss;
        _roundEnded = true;
        Time.timeScale = 1f;
        ShowResult();
        StartCoroutine(EndRound());
    }

    private void BuildBattleUi()
    {
        _battleUiRoot = ToolkitUi.Attach(this, Color.clear);

        VisualElement controls = ToolkitUi.Panel(new Color(0.05f, 0.08f, 0.12f, 0.9f));
        controls.style.position = Position.Absolute;
        controls.style.top = 16;
        controls.style.right = 16;
        controls.style.flexDirection = FlexDirection.Row;
        controls.style.alignItems = Align.Center;
        _battleUiRoot.Add(controls);

        _speedLabel = ToolkitUi.Label("Speed 1×", 16, Color.white, true);
        _speedLabel.style.marginRight = 10;
        controls.Add(_speedLabel);

        Label roundLabel = ToolkitUi.Label(
            $"Round {OpponentRoster.RoundNumber} · {_arena.ArenaName} · Enemy {OpponentRoster.Members.Count}/5 · Spent {OpponentRoster.LastSpent}/10",
            14, new Color(0.72f, 0.79f, 0.87f));
        roundLabel.style.marginRight = 12;
        controls.Add(roundLabel);

        AddSpeedButton(controls, 1);
        AddSpeedButton(controls, 2);
        AddSpeedButton(controls, 4);
        AddSpeedButton(controls, 8);
    }

    private void AddSpeedButton(VisualElement controls, int multiplier)
    {
        Button button = ToolkitUi.Button($"{multiplier}×", () => SetBattleSpeed(multiplier));
        button.style.width = 48;
        button.style.height = 34;
        button.style.marginLeft = 4;
        controls.Add(button);
    }

    private void SetBattleSpeed(float multiplier)
    {
        if (_roundEnded) return;
        Time.timeScale = Mathf.Clamp(multiplier, 1f, 8f);
        if (_speedLabel != null) _speedLabel.text = $"Speed {Time.timeScale:0}×";
    }

    private void ShowResult()
    {
        VisualElement root = _battleUiRoot ?? ToolkitUi.Attach(this, Color.clear);
        root.Clear();
        root.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
        root.style.justifyContent = Justify.Center;
        root.style.alignItems = Align.Center;
        Label result = ToolkitUi.Label(LastResult == RoundResult.Win ? "Victory!" : "Defeat!",
            56, Color.white, true);
        root.Add(result);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private IEnumerator EndRound()
    {
        foreach (CreatureController creature in FindObjectsByType<CreatureController>(FindObjectsSortMode.None))
        {
            creature.StopAllCoroutines();
            creature.enabled = false;
            MovementComponent movement = creature.GetComponent<MovementComponent>();
            if (movement != null) movement.enabled = false;
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, resultDisplaySeconds));

        if (!Application.CanStreamedLevelBeLoaded(shopSceneName))
        {
            Debug.LogError($"Shop scene '{shopSceneName}' is missing from Build Settings.", this);
            yield break;
        }

        SceneManager.LoadScene(shopSceneName);
    }

    private static bool HasTeam(System.Collections.Generic.IReadOnlyList<CreatureController> creatures)
    {
        foreach (CreatureController creature in creatures)
        {
            if (creature != null) return true;
        }
        return false;
    }

    private static void ConfigureCreature(CreatureController controller, CreatureInstance member)
    {
        controller.Configure(member.Species, member.EquippedMoves, member.Ability,
            member.Stats, member.HeldItem, member.Spray);
    }

    private static bool HasLivingCreature(System.Collections.Generic.IReadOnlyList<CreatureController> creatures)
    {
        foreach (CreatureController creature in creatures)
        {
            if (creature != null && creature.isActiveAndEnabled && !creature.IsDead) return true;
        }
        return false;
    }
}
