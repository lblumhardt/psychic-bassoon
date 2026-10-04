using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(menuName = "Mons/Battle Move Tag Settings")]
public class BattleMoveTagSettings : ScriptableObject
{
    public bool showTags = true;
    [Min(0.1f)] public float visibleSeconds = 0.9f;
    [Min(0f)] public float fadeSeconds = 0.2f;
    [Tooltip("Panel pixels from the top of the creature. Negative Y moves up.")]
    public Vector2 screenOffset = new Vector2(0f, -28f);
    [Min(0f)] public float risePixels = 10f;
    public bool uppercase = true;
    public bool showPlusSuffix = true;
    [Tooltip("Optional replacement for Resources/BattleMoveTags.uss.")]
    public StyleSheet styleSheet;
}
