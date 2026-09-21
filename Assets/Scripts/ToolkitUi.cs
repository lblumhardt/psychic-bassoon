using UnityEngine;
using UnityEngine.UIElements;

public static class ToolkitUi
{
    private static PanelSettings _panelSettings;
    private static Font _font;

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
}
