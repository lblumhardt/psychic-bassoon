using System;
using UnityEngine;
using UnityEngine.UIElements;

public static class PerkDraftUi
{
    public static void Show(VisualElement root, Action refresh)
    {
        if (RunPerks.Choices.Count == 0) return;
        foreach (VisualElement child in root.Children()) child.SetEnabled(false);
        VisualElement overlay = new();
        overlay.style.position = Position.Absolute;
        overlay.style.left = overlay.style.right = overlay.style.top = overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0.03f, 0.05f, 0.08f, 0.98f);
        overlay.style.justifyContent = Justify.Center;
        overlay.style.alignItems = Align.Center;
        root.Add(overlay);
        overlay.Add(ToolkitUi.Label("Choose a team perk", 32, Color.white, true));
        overlay.Add(ToolkitUi.Label("Applies to your entire team for the rest of this run.", 17, Color.white));
        VisualElement cards = new();
        cards.style.flexDirection = FlexDirection.Row;
        cards.style.width = Length.Percent(90);
        cards.style.marginTop = 24;
        overlay.Add(cards);
        foreach (TeamPerkSO perk in RunPerks.Choices)
        {
            VisualElement card = ToolkitUi.Panel(new Color(0.12f, 0.18f, 0.26f));
            card.style.flexGrow = 1;
            card.style.flexBasis = 0;
            card.style.marginLeft = card.style.marginRight = 6;
            cards.Add(card);
            Label title = ToolkitUi.Label(perk.displayName, 22, Color.white, true);
            title.style.whiteSpace = WhiteSpace.Normal;
            card.Add(title);
            Label description = ToolkitUi.Label(perk.description, 17, new Color(0.8f, 0.87f, 0.94f));
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.flexGrow = 1;
            description.style.marginTop = description.style.marginBottom = 20;
            card.Add(description);
            card.Add(ToolkitUi.Button("Choose", () => { if (RunPerks.Choose(perk)) refresh(); }));
        }
    }
}
