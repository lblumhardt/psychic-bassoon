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
    [SerializeField] private string shopSceneName = "ShopScene";
    [SerializeField] private CreatureDataSO[] opponentMonsterPool;

    private CreatureRegistry _creatureRegistry;
    private ArenaGenerator _arena;
    private bool _roundStarted;
    private bool _roundEnded;
    private VisualElement _battleUiRoot;
    private Label _speedLabel;
    private int _roundReadyFrame;
    private bool _perksReady;
    private readonly List<CreatureController> _participants = new();

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
        if (SandboxSession.Active)
        {
            SpawnSandbox(creatures);
        }
        else
        {
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
                starter.Stats, starter.HeldItem, starter.Spray, starter.Level);
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
                    member.HeldItem, member.Spray, member.Level);
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

        }

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
            _participants.Add(creature);
        }

        _roundStarted = HasTeam(_creatureRegistry.playerCreatures) && HasTeam(_creatureRegistry.enemyCreatures);
        _roundReadyFrame = Time.frameCount + 1;
        if (!_roundStarted)
        {
            Debug.LogWarning("Battle needs at least one player and one enemy creature to start.", this);
        }
        // Wait until all creature Start methods have initialized their fallback loadouts.
        _battleUiRoot = ToolkitUi.Attach(this, Color.clear);
        _battleUiRoot.schedule.Execute(BuildBattleUi);
    }

    private void Update()
    {
        if (!_roundStarted || _roundEnded || Time.frameCount < _roundReadyFrame) return;

        if (!_perksReady)
        {
            _perksReady = true;
            _creatureRegistry.Perks = gameObject.AddComponent<BattlePerks>();
            _creatureRegistry.Perks.Initialize(_participants, !SandboxSession.Active);
        }

        bool playerAlive = HasLivingCreature(_creatureRegistry.playerCreatures);
        bool enemyAlive = HasLivingCreature(_creatureRegistry.enemyCreatures);
        if (playerAlive && enemyAlive) return;

        // If both teams die during the same frame, count it as a loss.
        LastResult = playerAlive ? RoundResult.Win : RoundResult.Loss;
        _roundEnded = true;
        if (!SandboxSession.Active) RunProgress.RecordResult(LastResult);
        EndRound();
        ShowResult();
    }

    private void BuildBattleUi()
    {
        if (_roundEnded) return;
        _battleUiRoot.style.paddingTop = 16;
        _battleUiRoot.style.paddingBottom = 16;
        _battleUiRoot.style.paddingLeft = 16;
        _battleUiRoot.style.paddingRight = 16;

        VisualElement controls = ToolkitUi.Panel(new Color(0.05f, 0.08f, 0.12f, 0.9f));
        controls.style.alignSelf = Align.FlexEnd;
        controls.style.flexShrink = 0;
        controls.style.maxWidth = Length.Percent(100);
        controls.style.flexWrap = Wrap.Wrap;
        controls.style.flexDirection = FlexDirection.Row;
        controls.style.alignItems = Align.Center;
        _battleUiRoot.Add(controls);

        _speedLabel = ToolkitUi.Label("Speed 1×", 16, Color.white, true);
        _speedLabel.style.marginRight = 10;
        controls.Add(_speedLabel);

        Label roundLabel = ToolkitUi.Label(
            SandboxSession.Active ? $"Sandbox · {_arena.ArenaName}" : $"Round {OpponentRoster.RoundNumber} · {_arena.ArenaName}\n{RunProgress.Summary}",
            14, new Color(0.72f, 0.79f, 0.87f));
        roundLabel.style.marginRight = 12;
        controls.Add(roundLabel);

        AddSpeedButton(controls, 1);
        AddSpeedButton(controls, 2);
        AddSpeedButton(controls, 4);
        AddSpeedButton(controls, 8);
        if (SandboxSession.Active)
        {
            controls.Add(ToolkitUi.Button("Edit Teams", ReturnToSandbox));
            controls.Add(ToolkitUi.Button("Restart Test", () => SceneManager.LoadScene(SceneManager.GetActiveScene().name)));
        }
        VisualElement rosters = new VisualElement();
        rosters.style.flexDirection = FlexDirection.Row;
        rosters.style.justifyContent = Justify.SpaceBetween;
        rosters.style.flexGrow = 1;
        rosters.style.minHeight = 0;
        rosters.style.marginTop = 12;
        rosters.pickingMode = PickingMode.Ignore;
        _battleUiRoot.Add(rosters);
        AddTeamRoster(rosters, Team.Player, "Your team", new Color(0.35f, 0.75f, 1f));
        AddTeamRoster(rosters, Team.Enemy, "Opponent team", new Color(1f, 0.45f, 0.4f));
    }

    private void AddTeamRoster(VisualElement parent, Team team, string title, Color color)
    {
        VisualElement panel = ToolkitUi.Panel(new Color(0.05f, 0.08f, 0.12f, 0.94f));
        panel.style.width = Length.Percent(24);
        panel.style.maxWidth = 320;
        panel.style.minHeight = 0;
        parent.Add(panel);
        int count = _participants.FindAll(c => c != null && c.Team == team).Count;
        panel.Add(ToolkitUi.Label($"{title} · {count}", 18, color, true));
        VisualElement viewport = new VisualElement();
        viewport.style.flexGrow = 1;
        viewport.style.minHeight = 0;
        panel.Add(viewport);
        // Lay out compact cards at a known size, then fit the entire roster to
        // the available space. Resizing the game view never hides a teammate.
        const float rosterWidth = 280f;
        float rosterHeight = Mathf.Max(1, count) * 124f;
        VisualElement roster = new VisualElement();
        roster.style.position = Position.Absolute;
        roster.style.width = rosterWidth;
        roster.style.height = rosterHeight;
        roster.style.transformOrigin = new TransformOrigin(0, 0, 0);
        viewport.Add(roster);
        viewport.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            float scale = Mathf.Max(0.01f, Mathf.Min(1f,
                evt.newRect.width / rosterWidth, evt.newRect.height / rosterHeight));
            roster.style.scale = new Scale(new Vector3(scale, scale, 1));
            roster.style.left = Mathf.Max(0, (evt.newRect.width - rosterWidth * scale) / 2);
        });
        foreach (CreatureController creature in _participants)
        {
            if (creature == null || creature.Team != team) continue;
            VisualElement card = ToolkitUi.Panel(new Color(0.12f, 0.17f, 0.24f));
            card.style.marginTop = 4;
            card.style.height = 120;
            card.style.paddingTop = card.style.paddingBottom = 4;
            card.style.paddingLeft = card.style.paddingRight = 6;
            card.style.flexShrink = 0;
            roster.Add(card);
            VisualElement heading = new VisualElement();
            heading.style.flexDirection = FlexDirection.Row;
            heading.style.alignItems = Align.Center;
            card.Add(heading);
            heading.Add(ToolkitUi.CreaturePortrait(creature.creatureData, 32));
            Label name = ToolkitUi.Label($"{creature.creatureData?.creatureName ?? "Creature"} · Lv {creature.Level}", 14, color, true);
            name.style.overflow = Overflow.Hidden;
            name.style.textOverflow = TextOverflow.Ellipsis;
            name.tooltip = name.text;
            name.style.flexShrink = 1;
            name.style.marginLeft = 6;
            Label health = ToolkitUi.Label("", 13, Color.white);
            VisualElement identity = new VisualElement();
            identity.style.flexGrow = 1;
            identity.style.minWidth = 0;
            heading.Add(identity);
            identity.Add(name);
            identity.Add(health);
            CombatComponent combat = creature.GetComponent<CombatComponent>();
            List<string> moves = new List<string>();
            if (combat != null && combat.attacks != null)
                foreach (AttackDataSO move in combat.attacks)
                    if (move != null) moves.Add(move.attackName + (combat.IsPlusMove(move) ? "+" : ""));
            AddRosterDetail(card, "Moves", moves.Count > 0 ? string.Join(" / ", moves) : "None");
            Label abilityLabel = AddRosterDetail(card, "Ability", creature.Ability != null ? creature.Ability.DisplayName : "None");
            ToolkitUi.AbilityTooltip(abilityLabel, () => creature != null ? creature.Ability : null);
            AddRosterDetail(card, "Item", creature.HeldItem != null ? creature.HeldItem.displayName : "None", creature.HeldItem?.description);
            AddRosterDetail(card, "Spray", creature.Spray != null ? creature.Spray.displayName : "None", creature.Spray?.description);
            StatsComponent stats = creature.GetComponent<StatsComponent>();
            void RefreshHealth()
            {
                bool knockedOut = creature == null || creature.IsDead;
                health.text = knockedOut ? "Knocked out" : stats != null
                    ? $"HP {Mathf.CeilToInt(stats.CurrentHP)} / {Mathf.CeilToInt(stats.MaxHP)}" : "HP unavailable";
                card.style.opacity = knockedOut ? 0.55f : 1f;
            }
            RefreshHealth();
            card.schedule.Execute(RefreshHealth).Every(100);
        }
        if (count == 0) roster.Add(ToolkitUi.Label("No creatures", 14, Color.white));
    }

    private static Label AddRosterDetail(VisualElement card, string title, string value, string tooltip = null)
    {
        Label label = ToolkitUi.Label($"{title}: {value}", 13, new Color(0.82f, 0.87f, 0.94f));
        label.style.whiteSpace = WhiteSpace.NoWrap;
        label.style.overflow = Overflow.Hidden;
        label.style.textOverflow = TextOverflow.Ellipsis;
        label.style.height = 18;
        label.style.marginTop = label.style.marginBottom = 0;
        label.tooltip = string.IsNullOrEmpty(tooltip) ? value : $"{value}\n{tooltip}";
        card.Add(label);
        return label;
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
        string title = !SandboxSession.Active && RunProgress.IsOver
            ? (RunProgress.HasWon ? "You Won the Run!" : "Run Over")
            : (LastResult == RoundResult.Win ? "Victory!" : "Defeat!");
        PostMatchSummary.Show(root, title, _participants, SandboxSession.Active ? ReturnToSandbox : ContinueToShop);
        if (SandboxSession.Active)
            root.Add(ToolkitUi.Button("Replay Test", () => SceneManager.LoadScene(SceneManager.GetActiveScene().name)));
    }

    private void ReturnToSandbox()
    {
        SandboxSession.Active = false;
        SandboxSession.OpenEditor = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void SpawnSandbox(CreatureController[] templates)
    {
        if (templates.Length == 0)
        {
            Debug.LogError("Sandbox battle requires a creature template in the battle scene.");
            return;
        }
        foreach (var template in templates) template.gameObject.SetActive(false);
        foreach (Team team in new[] { Team.Player, Team.Enemy })
        {
            var members = team == Team.Player ? SandboxSession.Player : SandboxSession.Enemy;
            foreach (var member in members)
            {
                var clone = Instantiate(templates[0].gameObject, Vector3.zero, templates[0].transform.rotation);
                clone.name = $"Sandbox {team} - {member.Species.creatureName}";
                // Awake initializes component references when the inactive clone is activated.
                clone.SetActive(true);
                var controller = clone.GetComponent<CreatureController>();
                controller.SetTeam(team);
                member.Configure(controller);
            }
        }
        foreach (var template in templates) Destroy(template.gameObject);
    }

    private void ContinueToShop()
    {
        if (!Application.CanStreamedLevelBeLoaded(shopSceneName))
        {
            Debug.LogError($"Shop scene '{shopSceneName}' is missing from Build Settings.", this);
            return;
        }
        if (RunProgress.IsOver) RunProgress.StartNewRun();
        Time.timeScale = 1f;
        SceneManager.LoadScene(shopSceneName);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void EndRound()
    {
        Time.timeScale = 0f;
        foreach (CreatureController creature in _participants)
        {
            if (creature == null) continue;
            StatsComponent stats = creature.GetComponent<StatsComponent>();
            if (stats != null) stats.MatchFinished = true;
            creature.StopAllCoroutines();
            creature.enabled = false;
            MovementComponent movement = creature.GetComponent<MovementComponent>();
            if (movement != null) movement.enabled = false;
        }
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
            member.Stats, member.HeldItem, member.Spray, member.Level);
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
