using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public static class SandboxPage
{
    public static void Show(MonoBehaviour host, Action back)
    {
        SandboxSession.Active = false;
        SandboxSession.OpenEditor = false;
        var catalog = Resources.Load<SandboxCatalog>("SandboxCatalog");
        var root = ToolkitUi.Attach(host, new Color(0.06f, 0.09f, 0.14f));
        root.style.alignItems = Align.Stretch;
        root.style.justifyContent = Justify.FlexStart;
        root.style.paddingLeft = root.style.paddingRight = 20;
        root.style.paddingTop = root.style.paddingBottom = 16;
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.Add(ToolkitUi.Button("Main Menu", back));
        header.Add(ToolkitUi.Label("  Sandbox · Battle debugger", 28, Color.white, true));
        root.Add(header);
        if (catalog == null || catalog.creatures.Length == 0)
        {
            root.Add(ToolkitUi.Label("No sandbox catalog found. Reopen Play Mode to rebuild it.", 18, Color.white));
            return;
        }
        if (SandboxSession.Player.Count == 0) SandboxSession.Player.Add(new SandboxCreature(catalog.creatures[0]));
        if (SandboxSession.Enemy.Count == 0) SandboxSession.Enemy.Add(new SandboxCreature(catalog.creatures[0]));
        var hint = ToolkitUi.Label("Configure both teams. Green moves belong to the creature's pool; every move is selectable. Levels upgrade move 1, then move 2. Run perks are disabled.", 15, Color.white);
        hint.style.whiteSpace = WhiteSpace.Normal;
        hint.style.marginTop = hint.style.marginBottom = 10;
        root.Add(hint);
        var teams = new VisualElement();
        teams.style.flexDirection = FlexDirection.Row;
        teams.style.flexGrow = 1;
        teams.style.minHeight = 0;
        root.Add(teams);
        Action redraw = () => Show(host, back);
        AddTeam(teams, "Your team", SandboxSession.Player, catalog, redraw);
        AddTeam(teams, "Enemy team", SandboxSession.Enemy, catalog, redraw);
        var error = ToolkitUi.Label("", 14, new Color(1f, 0.5f, 0.4f));
        root.Add(error);
        root.Add(ToolkitUi.Button("Start Test Battle", () =>
        {
            if (SandboxSession.Player.Concat(SandboxSession.Enemy).Any(c => c.Moves.Any(m => m == null || m.behavior == null)))
            {
                error.text = "Assign two playable moves to every creature before starting.";
                return;
            }
            if (!Application.CanStreamedLevelBeLoaded("testScene"))
            {
                error.text = "The testScene battle scene is missing from Build Settings.";
                return;
            }
            SandboxSession.Active = true;
            SceneManager.LoadScene("testScene");
        }));
    }

    private static void AddTeam(VisualElement parent, string title, List<SandboxCreature> team, SandboxCatalog catalog, Action redraw)
    {
        var column = new VisualElement();
        column.style.flexGrow = 1;
        column.style.flexBasis = 0;
        column.style.minWidth = 0;
        column.style.marginRight = 10;
        parent.Add(column);
        column.Add(ToolkitUi.Label($"{title} ({team.Count}/{RunRoster.MaxMembers})", 22, Color.white, true));
        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        scroll.style.minHeight = 0;
        column.Add(scroll);
        foreach (var creature in team.ToArray())
        {
            var card = ToolkitUi.Panel(new Color(0.12f, 0.17f, 0.24f));
            card.style.marginBottom = 12;
            scroll.Add(card);
            card.Add(ToolkitUi.CreaturePortrait(creature.Species, 60));
            Choose(card, "Creature", catalog.creatures, creature.Species, s => s.creatureName, selected =>
            {
                team[team.IndexOf(creature)] = new SandboxCreature(selected);
                redraw();
            });
            var level = new DropdownField("Level", new List<string> { "1", "2", "3" }, creature.Level - 1);
            card.Add(level);
            var final = ToolkitUi.Label("", 14, new Color(0.6f, 0.85f, 1f));
            final.style.whiteSpace = WhiteSpace.Normal;
            Action updateStats = () =>
            {
                var s = creature.Stats;
                final.text = $"Battle stats: HP {s.hp} · Power {s.power} · Defense {s.defense} · Move speed {s.moveSpeed} · Attack speed {s.attackSpeed}";
            };
            level.RegisterValueChangedCallback(e => { creature.Level = level.index + 1; updateStats(); });
            Choose(card, "Ability", catalog.abilities, creature.Ability, a => a.DisplayName, a => creature.Ability = a, true);
            Choose(card, "Held item", catalog.items.Where(i => i.slot == CreatureItemSlot.HeldItem).ToArray(), creature.Item, i => i.displayName,
                i => { creature.Item = i; updateStats(); }, true);
            Choose(card, "Spray", catalog.items.Where(i => i.slot == CreatureItemSlot.Spray).ToArray(), creature.Spray, i => i.displayName,
                i => { creature.Spray = i; updateStats(); }, true);
            card.Add(ToolkitUi.Label("Base stats (before level and equipment bonuses)", 14, Color.white));
            Stat(card, "HP", creature.BaseStats.hp, v => { creature.BaseStats.hp = v; updateStats(); });
            Stat(card, "Power", creature.BaseStats.power, v => { creature.BaseStats.power = v; updateStats(); });
            Stat(card, "Defense", creature.BaseStats.defense, v => { creature.BaseStats.defense = v; updateStats(); });
            Stat(card, "Move speed", creature.BaseStats.moveSpeed, v => { creature.BaseStats.moveSpeed = v; updateStats(); });
            Stat(card, "Attack speed", creature.BaseStats.attackSpeed, v => { creature.BaseStats.attackSpeed = v; updateStats(); });
            card.Add(ToolkitUi.Button("Reset Base Stats", () => { creature.BaseStats = creature.Species.baseStats; redraw(); }));
            card.Add(final);
            updateStats();
            var moves = new Foldout { text = "Moves · expand to pick from all moves", value = false };
            card.Add(moves);
            var equipped = ToolkitUi.Label("", 15, Color.white, true);
            equipped.style.whiteSpace = WhiteSpace.Normal;
            card.Add(equipped);
            Action updateMoves = () => equipped.text = $"1: {creature.Moves[0]?.attackName ?? "None"} / 2: {creature.Moves[1]?.attackName ?? "None"}";
            updateMoves();
            foreach (var move in catalog.moves)
            {
                bool native = creature.Species.movePool != null && creature.Species.movePool.Contains(move);
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                if (native) row.style.backgroundColor = new Color(0.12f, 0.32f, 0.23f);
                var label = ToolkitUi.Label(move.attackName + (move.DebugOnly ? " · DEBUG ONLY" : native ? " · IN POOL" : "") + (move.behavior == null ? " · No behavior" : ""), 14,
                    native ? new Color(0.5f, 1f, 0.65f) : Color.white);
                label.style.flexGrow = 1;
                label.style.flexBasis = 0;
                label.style.whiteSpace = WhiteSpace.Normal;
                row.Add(label);
                for (int slot = 0; slot < 2; slot++)
                {
                    int index = slot;
                    var button = ToolkitUi.Button($"Slot {slot + 1}", () => { creature.Moves[index] = move; updateMoves(); });
                    button.style.fontSize = 13;
                    row.Add(button);
                }
                moves.Add(row);
            }
            var remove = ToolkitUi.Button("Remove Creature", () => { team.Remove(creature); redraw(); });
            remove.SetEnabled(team.Count > 1);
            card.Add(remove);
        }
        var add = ToolkitUi.Button("Add Creature", () => { team.Add(new SandboxCreature(catalog.creatures[0])); redraw(); });
        add.SetEnabled(team.Count < RunRoster.MaxMembers);
        column.Add(add);
    }

    private static void Stat(VisualElement parent, string name, int value, Action<int> changed)
    {
        var field = new IntegerField(name) { value = value, isDelayed = true };
        field.RegisterValueChangedCallback(e =>
        {
            int valid = Mathf.Clamp(e.newValue, 1, 1000000);
            field.SetValueWithoutNotify(valid);
            changed(valid);
        });
        parent.Add(field);
    }

    private static void Choose<T>(VisualElement parent, string title, T[] values, T selected, Func<T, string> label, Action<T> changed, bool allowNone = false) where T : UnityEngine.Object
    {
        var options = values.ToList();
        if (allowNone) options.Insert(0, null);
        var labels = options.Select((v, i) => v == null ? "None" : $"{label(v)} [{i + 1}]").ToList();
        var field = new DropdownField(title, labels, Mathf.Max(0, options.IndexOf(selected)));
        if (typeof(CreatureAbilitySO).IsAssignableFrom(typeof(T)))
            ToolkitUi.AbilityTooltip(field, () => field.index >= 0 && field.index < options.Count
                ? options[field.index] as CreatureAbilitySO : null);
        field.RegisterValueChangedCallback(e => { if (field.index >= 0) changed(options[field.index]); });
        parent.Add(field);
    }
}
