using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(StatsComponent))]
public class CreatureHealthBar : MonoBehaviour
{
    [SerializeField] private float gapAboveSprite = 0.12f;
    [SerializeField] private float depthInFrontOfSprite = 0.05f;
    [SerializeField] private Vector2 size = new Vector2(0.8f, 0.12f);

    private StatsComponent _stats;
    private RectTransform _canvasTransform;
    private RawImage _fill;
    private Camera _camera;
    private Renderer[] _creatureRenderers;

    private void Awake()
    {
        _stats = GetComponent<StatsComponent>();
        _camera = Camera.main;
        _creatureRenderers = GetComponentsInChildren<Renderer>();

        // Cloned teammates may already carry the template's bar child.
        Transform existingBar = transform.Find("Health Bar");
        if (existingBar != null)
        {
            _canvasTransform = existingBar.GetComponent<RectTransform>();
            Transform existingFill = existingBar.Find("Fill");
            if (existingFill != null) _fill = existingFill.GetComponent<RawImage>();
            if (_canvasTransform != null && _fill != null) return;
            Destroy(existingBar.gameObject);
        }

        GameObject canvasObject = new GameObject("Health Bar", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        _canvasTransform = canvasObject.GetComponent<RectTransform>();
        _canvasTransform.localScale = Vector3.one * 0.01f;
        _canvasTransform.sizeDelta = size * 100f;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RawImage background = CreateImage("Background", canvasObject.transform, new Color(0.12f, 0.1f, 0.1f, 0.9f));
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.offsetMin = Vector2.zero;
        background.rectTransform.offsetMax = Vector2.zero;

        _fill = CreateImage("Fill", canvasObject.transform, new Color(0.2f, 0.85f, 0.35f));
        _fill.rectTransform.anchorMin = Vector2.zero;
        _fill.rectTransform.anchorMax = Vector2.one;
        _fill.rectTransform.offsetMin = Vector2.zero;
        _fill.rectTransform.offsetMax = Vector2.zero;
    }

    private static RawImage CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(parent, false);
        RawImage image = imageObject.GetComponent<RawImage>();
        image.texture = Texture2D.whiteTexture;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void LateUpdate()
    {
        if (_camera == null) _camera = Camera.main;
        if (_camera != null)
        {
            Vector3 screenUp = _camera.transform.up;
            Vector3 towardCamera = -_camera.transform.forward;
            float topOfSprite = 0f;
            float frontOfSprite = 0f;
            foreach (Renderer creatureRenderer in _creatureRenderers)
            {
                if (creatureRenderer == null || !creatureRenderer.enabled) continue;
                Bounds bounds = creatureRenderer.bounds;
                Vector3 fromCreature = bounds.center - transform.position;
                topOfSprite = Mathf.Max(topOfSprite, Vector3.Dot(fromCreature, screenUp) + ProjectedExtent(bounds.extents, screenUp));
                frontOfSprite = Mathf.Max(frontOfSprite, Vector3.Dot(fromCreature, towardCamera) + ProjectedExtent(bounds.extents, towardCamera));
            }

            // Screen-up moves above the sprite in a top-down view; camera depth keeps the UI visible.
            _canvasTransform.position = transform.position
                + screenUp * (topOfSprite + gapAboveSprite + size.y * 0.5f)
                + towardCamera * (frontOfSprite + depthInFrontOfSprite);
            _canvasTransform.rotation = Quaternion.LookRotation(-_camera.transform.forward, _camera.transform.up);
        }

        _fill.rectTransform.anchorMax = new Vector2(_stats.HealthFraction, 1f);
        _fill.enabled = _stats.HealthFraction > 0f;
    }

    private static float ProjectedExtent(Vector3 extents, Vector3 axis)
    {
        return Mathf.Abs(axis.x) * extents.x + Mathf.Abs(axis.y) * extents.y + Mathf.Abs(axis.z) * extents.z;
    }
}
