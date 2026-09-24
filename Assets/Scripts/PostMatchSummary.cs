using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class PostMatchSummary
{
    public static void Show(VisualElement root, string result,
        IReadOnlyList<CreatureController> participants, Action continueToShop)
    {
        VisualElement panel = ToolkitUi.Panel(new Color(0.06f, 0.09f, 0.14f, 0.94f));
        panel.style.width = Length.Percent(88);
        panel.style.maxWidth = 1000;
        panel.style.height = Length.Percent(85);
        panel.style.paddingLeft = panel.style.paddingRight = 24;
        panel.style.paddingTop = panel.style.paddingBottom = 20;
        root.Add(panel);

        panel.Add(ToolkitUi.Label("Post match summary", 28, Color.white, true));
        Color resultColor = BattleManager.LastResult == RoundResult.Win
            ? new Color(0.45f, 0.95f, 0.65f) : new Color(1f, 0.55f, 0.48f);
        panel.Add(ToolkitUi.Label(result, 32, resultColor, true));
        panel.Add(ToolkitUi.Label($"Round {OpponentRoster.RoundNumber} · {RunProgress.Summary}",
            18, Color.white));
        Label explanation = ToolkitUi.Label("Actual damage after defense; overkill and excess healing excluded.",
            14, new Color(0.72f, 0.79f, 0.87f));
        explanation.style.whiteSpace = WhiteSpace.Normal;
        explanation.style.marginTop = 8;
        explanation.style.marginBottom = 12;
        panel.Add(explanation);

        ScrollView scroll = new();
        scroll.style.flexGrow = 1;
        scroll.style.minHeight = 0;
        panel.Add(scroll);
        AddTeam(scroll, participants, Team.Player, "Your team", new Color(0.4f, 0.75f, 1f));
        AddTeam(scroll, participants, Team.Enemy, "Opponent", new Color(1f, 0.55f, 0.48f));

        Button next = ToolkitUi.Button(RunProgress.IsOver ? "Start New Run" : "Continue to Shop", continueToShop);
        next.style.marginTop = 16;
        next.style.flexShrink = 0;
        panel.Add(next);
    }

    private static void AddTeam(VisualElement parent, IReadOnlyList<CreatureController> participants,
        Team team, string title, Color color)
    {
        float total = 0f;
        foreach (CreatureController creature in participants)
            if (creature != null && creature.Team == team)
                total += creature.GetComponent<StatsComponent>()?.DamageDealt ?? 0f;

        Label heading = ToolkitUi.Label($"{title} · {total:0.0} damage dealt", 21, color, true);
        heading.style.marginTop = 12;
        heading.style.marginBottom = 8;
        parent.Add(heading);
        VisualElement headers = new();
        headers.style.flexDirection = FlexDirection.Row;
        AddCell(headers, "Creature", 40, true);
        AddCell(headers, "Dealt", 20, true);
        AddCell(headers, "Taken", 20, true);
        AddCell(headers, "Healing", 20, true);
        parent.Add(headers);

        int index = 0;
        foreach (CreatureController creature in participants)
        {
            if (creature == null || creature.Team != team) continue;
            StatsComponent stats = creature.GetComponent<StatsComponent>();
            if (stats == null) continue;
            index++;
            VisualElement row = ToolkitUi.Panel(new Color(0.12f, 0.17f, 0.24f, 0.9f));
            row.style.marginBottom = 6;
            VisualElement values = new();
            values.style.flexDirection = FlexDirection.Row;
            string name = creature.creatureData != null ? creature.creatureData.creatureName : creature.name;
            AddCell(values, $"{index}. {name}\n{(stats.IsDead() ? "Knocked out" : "Survived")}", 40);
            AddCell(values, $"{stats.DamageDealt:0.0}", 20);
            AddCell(values, $"{stats.DamageTaken:0.0}", 20);
            AddCell(values, $"{stats.HealingDone:0.0}", 20);
            row.Add(values);
            VisualElement bar = new();
            bar.style.height = 4;
            bar.style.marginTop = 6;
            bar.style.width = Length.Percent(total > 0f ? stats.DamageDealt / total * 100f : 0f);
            bar.style.backgroundColor = color;
            row.Add(bar);
            parent.Add(row);
        }
    }

    private static void AddCell(VisualElement parent, string text, float width, bool bold = false)
    {
        Label label = ToolkitUi.Label(text, 16, Color.white, bold);
        label.style.width = Length.Percent(width);
        label.style.whiteSpace = WhiteSpace.Normal;
        label.style.flexShrink = 0;
        parent.Add(label);
    }
}
