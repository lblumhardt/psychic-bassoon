using UnityEngine;
using UnityEngine.UIElements;

public static class ToolkitUi
{
    private static PanelSettings _panelSettings;
    private static Font _font;

    public static void AbilityTooltip(VisualElement target, System.Func<CreatureAbilitySO> getAbility)
    {
        VisualElement popup = null;
        void Hide()
        {
            popup?.RemoveFromHierarchy();
            popup = null;
        }
        target.RegisterCallback<PointerEnterEvent>(evt =>
        {
            Hide();
            CreatureAbilitySO ability = getAbility();
            if (ability == null || target.panel == null) return;
            VisualElement root = target.panel.visualTree;
            popup = Panel(new Color(0.025f, 0.04f, 0.065f, 0.98f));
            popup.pickingMode = PickingMode.Ignore;
            popup.style.position = Position.Absolute;
            float width = Mathf.Min(300f, root.worldBound.width - 16f);
            popup.style.width = width;
            Label heading = Label(ability.DisplayName, 16, Color.white, true);
            Label description = Label(ability.Description, 14, new Color(0.82f, 0.9f, 1f));
            heading.style.whiteSpace = description.style.whiteSpace = WhiteSpace.Normal;
            heading.pickingMode = description.pickingMode = PickingMode.Ignore;
            popup.Add(heading);
            popup.Add(description);
            Vector2 position = root.WorldToLocal(evt.position);
            popup.style.left = Mathf.Clamp(position.x + 14f, 8f, Mathf.Max(8f, root.worldBound.width - width - 8f));
            popup.style.top = position.y + 18f;
            popup.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (popup != null)
                    popup.style.top = Mathf.Clamp(position.y + 18f, 8f,
                        Mathf.Max(8f, root.worldBound.height - popup.resolvedStyle.height - 8f));
            });
            root.Add(popup);
        });
        target.RegisterCallback<PointerLeaveEvent>(_ => Hide());
        target.RegisterCallback<PointerDownEvent>(_ => Hide());
        target.RegisterCallback<DetachFromPanelEvent>(_ => Hide());
    }

    public static VisualElement Attach(MonoBehaviour host, Color background)
    {
        if (_panelSettings == null)
        {
            _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _panelSettings.name = "Runtime Menu Panel";
            _panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UnityDefaultRuntimeTheme");
            if (_panelSettings.themeStyleSheet == null)
            {
                Debug.LogError("UI Toolkit's DefaultRuntimeTheme could not be loaded.");
            }
        }

        if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        UIDocument document = host.GetComponentInChildren<UIDocument>(true);
        if (document == null)
        {
            GameObject uiObject = new GameObject("UI Toolkit Document");
            uiObject.SetActive(false);
            uiObject.transform.SetParent(host.transform, false);
            document = uiObject.AddComponent<UIDocument>();
            document.panelSettings = _panelSettings;
            uiObject.SetActive(true);
        }

        VisualElement root = document.rootVisualElement;
        root.Clear();
        root.style.flexGrow = 1;
        root.style.width = Length.Percent(100);
        root.style.height = Length.Percent(100);
        root.style.backgroundColor = background;
        return root;
    }

    public static Label Label(string text, int fontSize, Color color, bool bold = false)
    {
        Label label = new Label(text);
        label.style.fontSize = fontSize;
        label.style.color = color;
        if (_font != null) label.style.unityFontDefinition = FontDefinition.FromFont(_font);
        if (bold) label.style.unityFontStyleAndWeight = FontStyle.Bold;
        return label;
    }

    public static Button Button(string text, System.Action onClick)
    {
        Button button = new Button(onClick) { text = text };
        button.style.height = 42;
        button.style.fontSize = 17;
        button.style.unityTextAlign = TextAnchor.MiddleCenter;
        button.style.unityFontStyleAndWeight = FontStyle.Bold;
        if (_font != null) button.style.unityFontDefinition = FontDefinition.FromFont(_font);
        button.style.paddingLeft = 14;
        button.style.paddingRight = 14;
        button.style.backgroundColor = new Color(0.25f, 0.56f, 0.73f);
        button.style.color = Color.white;
        button.style.borderTopLeftRadius = 6;
        button.style.borderTopRightRadius = 6;
        button.style.borderBottomLeftRadius = 6;
        button.style.borderBottomRightRadius = 6;
        return button;
    }

    public static VisualElement Panel(Color background)
    {
        VisualElement panel = new VisualElement();
        panel.style.backgroundColor = background;
        panel.style.paddingTop = 12;
        panel.style.paddingBottom = 12;
        panel.style.paddingLeft = 14;
        panel.style.paddingRight = 14;
        panel.style.borderTopLeftRadius = 8;
        panel.style.borderTopRightRadius = 8;
        panel.style.borderBottomLeftRadius = 8;
        panel.style.borderBottomRightRadius = 8;
        return panel;
    }

    public static VisualElement CreaturePortrait(CreatureDataSO creature, int size = 80)
    {
        VisualElement frame = new();
        frame.style.width = frame.style.height = size;
        frame.style.flexShrink = 0;
        frame.style.backgroundColor = new Color(0.055f, 0.085f, 0.13f);
        frame.style.borderTopLeftRadius = frame.style.borderTopRightRadius = 10;
        frame.style.borderBottomLeftRadius = frame.style.borderBottomRightRadius = 10;
        frame.style.alignItems = Align.Center;
        frame.style.justifyContent = Justify.Center;
        frame.tooltip = creature != null ? creature.creatureName : "Creature";
        frame.pickingMode = PickingMode.Ignore;
        if (creature != null && creature.creatureTexture != null)
        {
            Image image = new Image { image = creature.creatureTexture, scaleMode = ScaleMode.ScaleToFit };
            image.style.width = image.style.height = size - 12;
            image.pickingMode = PickingMode.Ignore;
            frame.Add(image);
        }
        else
        {
            string initial = creature != null && !string.IsNullOrEmpty(creature.creatureName)
                ? creature.creatureName.Substring(0, 1).ToUpperInvariant() : "?";
            frame.Add(Label(initial, size / 3, new Color(0.45f, 0.8f, 1f), true));
        }
        return frame;
    }
}
