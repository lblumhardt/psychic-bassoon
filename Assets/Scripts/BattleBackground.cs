using UnityEngine;
using UnityEngine.Rendering;

// A Resources reference keeps the procedural shader available in player builds.
public sealed class BattleBackground : MonoBehaviour
{
    private static int lastPattern = -1;
    private Material material;
    private Transform backdrop;
    private Camera battleCamera;
    public int PatternIndex { get; private set; }

    public Material Initialize()
    {
        if (material != null) return material;
        Shader shader = Resources.Load<Shader>("BattleBackground");
        if (shader == null)
        {
            Debug.LogError("Missing battle background shader.", this);
            return null;
        }
        // Choose uniformly from the other two designs after the first battle.
        PatternIndex = lastPattern < 0 ? Random.Range(0, 3)
            : (lastPattern + Random.Range(1, 3)) % 3;
        lastPattern = PatternIndex;
        material = new Material(shader) { name = "Procedural Battle Background" };
        material.SetFloat("_Pattern", PatternIndex);
        battleCamera = Camera.main;
        if (battleCamera != null)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Animated Battle Backdrop";
            quad.layer = battleCamera.gameObject.layer;
            Destroy(quad.GetComponent<Collider>());
            backdrop = quad.transform;
            backdrop.SetParent(battleCamera.transform, false);
            Renderer renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        LateUpdate();
        return material;
    }

    private void LateUpdate()
    {
        if (material == null) return;
        material.SetFloat("_AnimationTime", Time.unscaledTime);
        if (backdrop == null || battleCamera == null) return;
        float distance = Mathf.Lerp(battleCamera.nearClipPlane, battleCamera.farClipPlane, 0.95f);
        float height = battleCamera.orthographic ? battleCamera.orthographicSize * 2f
            : 2f * distance * Mathf.Tan(battleCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
        backdrop.localPosition = new Vector3(0, 0, distance);
        backdrop.localScale = new Vector3(height * battleCamera.aspect * 1.01f, height * 1.01f, 1);
    }

    private void OnDestroy()
    {
        if (backdrop != null) Destroy(backdrop.gameObject);
        if (material != null) Destroy(material);
    }
}
