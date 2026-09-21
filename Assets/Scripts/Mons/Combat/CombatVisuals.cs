using UnityEngine;

public static class CombatVisuals
{
    public static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default");
        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    public static Transform MakeShape(PrimitiveType shape, string name, Transform parent,
        Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part.transform;
    }

    public static Vector3 ScreenArc(float progress, float height)
    {
        Camera camera = Camera.main;
        Vector3 screenUp = camera != null ? camera.transform.up : Vector3.forward;
        Vector3 towardCamera = camera != null ? -camera.transform.forward : Vector3.up;
        float arc = Mathf.Sin(Mathf.PI * progress) * height;
        return screenUp * arc + towardCamera * (arc * 0.3f + 0.15f);
    }
}
