using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// One non-interactive screen overlay per battle, with one reusable label per caster.
public class BattleMoveTags : MonoBehaviour
{
    private sealed class Tag
    {
        public CombatComponent caster;
        public CreatureController creature;
        public Renderer[] renderers;
        public Label label;
        public float started;
    }

    private static BattleMoveTags _instance;
    private readonly List<Tag> _tags = new();
    private BattleMoveTagSettings _settings;
    private VisualElement _root;
    private Camera _camera;

    public static void Show(CombatComponent caster, AttackDataSO move)
    {
        if (caster == null || move == null) return;
        if (_instance == null)
        {
            var host = new GameObject("Battle Move Tags");
            _instance = host.AddComponent<BattleMoveTags>();
        }
        _instance.ShowMove(caster, move);
    }

    private void Awake()
    {
        _instance = this;
        _settings = Resources.Load<BattleMoveTagSettings>("BattleMoveTagSettings");
        _root = ToolkitUi.Attach(this, Color.clear);
        GetComponentInChildren<UIDocument>().sortingOrder = 5;
        _root.pickingMode = PickingMode.Ignore;
        _root.AddToClassList("battle-move-tags");
        StyleSheet sheet = _settings != null && _settings.styleSheet != null
            ? _settings.styleSheet : Resources.Load<StyleSheet>("BattleMoveTags");
        if (sheet != null) _root.styleSheets.Add(sheet);
    }

    private void ShowMove(CombatComponent caster, AttackDataSO move)
    {
        if (_settings != null && !_settings.showTags) return;
        Tag tag = _tags.Find(t => t.caster == caster);
        if (tag == null)
        {
            tag = new Tag
            {
                caster = caster,
                creature = caster.GetComponent<CreatureController>(),
                renderers = caster.GetComponentsInChildren<Renderer>(),
                label = new Label { pickingMode = PickingMode.Ignore }
            };
            tag.label.AddToClassList("battle-move-tag");
            _root.Add(tag.label);
            _tags.Add(tag);
        }
        string name = string.IsNullOrWhiteSpace(move.attackName) ? move.name : move.attackName;
        tag.label.text = _settings == null || _settings.uppercase ? name.ToUpperInvariant() : name;
        bool plus = caster.IsPlusMove(move);
        if (plus && (_settings == null || _settings.showPlusSuffix)) tag.label.text += "+";
        tag.label.EnableInClassList("battle-move-tag--player", tag.creature != null && tag.creature.Team == Team.Player);
        tag.label.EnableInClassList("battle-move-tag--enemy", tag.creature != null && tag.creature.Team == Team.Enemy);
        tag.label.EnableInClassList("battle-move-tag--plus", plus);
        tag.started = Time.time;
        tag.label.style.opacity = 1f;
        // Positioned in LateUpdate before the next rendered frame.
        tag.label.style.visibility = Visibility.Hidden;
    }

    private void LateUpdate()
    {
        if (_camera == null) _camera = Camera.main;
        float duration = Mathf.Max(0.1f, _settings != null ? _settings.visibleSeconds : 0.9f);
        float fade = Mathf.Clamp(_settings != null ? _settings.fadeSeconds : 0.2f, 0f, duration);
        for (int i = _tags.Count - 1; i >= 0; i--)
        {
            Tag tag = _tags[i];
            if (tag.caster == null || !tag.caster.isActiveAndEnabled ||
                (tag.creature != null && tag.creature.IsDead))
            {
                tag.label.RemoveFromHierarchy();
                _tags.RemoveAt(i);
                continue;
            }
            float age = Time.time - tag.started;
            tag.label.style.visibility = Visibility.Hidden;
            if (age >= duration || _camera == null || _root.panel == null ||
                (_settings != null && !_settings.showTags)) continue;

            Vector3 anchor = tag.caster.transform.position;
            float top = 0f;
            foreach (Renderer renderer in tag.renderers)
            {
                if (renderer == null || !renderer.enabled) continue;
                Bounds bounds = renderer.bounds;
                Vector3 up = _camera.transform.up;
                float extent = Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(up.x), Mathf.Abs(up.y), Mathf.Abs(up.z)));
                top = Mathf.Max(top, Vector3.Dot(bounds.center - anchor, up) + extent);
            }
            anchor += _camera.transform.up * top;
            Vector3 screen = _camera.WorldToScreenPoint(anchor);
            if (screen.z <= 0f || screen.x < 0f || screen.x > Screen.width || screen.y < 0f || screen.y > Screen.height) continue;
            Vector2 position = _root.WorldToLocal(RuntimePanelUtils.ScreenToPanel(_root.panel,
                new Vector2(screen.x, Screen.height - screen.y)));
            position += _settings != null ? _settings.screenOffset : new Vector2(0f, -28f);
            position.y -= (_settings != null ? _settings.risePixels : 10f) * Mathf.Clamp01(age / duration);
            float width = tag.label.resolvedStyle.width;
            float height = tag.label.resolvedStyle.height;
            if (float.IsNaN(width) || float.IsNaN(height)) continue;
            tag.label.style.left = Mathf.Clamp(position.x - width * 0.5f, 4f, Mathf.Max(4f, _root.contentRect.width - width - 4f));
            tag.label.style.top = Mathf.Clamp(position.y - height, 4f, Mathf.Max(4f, _root.contentRect.height - height - 4f));
            tag.label.style.opacity = fade > 0f ? Mathf.Clamp01((duration - age) / fade) : 1f;
            tag.label.style.visibility = Visibility.Visible;
        }
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}
