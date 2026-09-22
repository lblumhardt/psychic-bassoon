using System.Collections;
using UnityEngine;

public class FloatingCombatText : MonoBehaviour
{
    private TextMesh _text;
    private Color _color;

    public static void Spawn(Vector3 position, string message, Color color)
    {
        GameObject label = new GameObject("Combat Number");
        label.transform.position = position + Random.insideUnitSphere * 0.12f;
        TextMesh text = label.AddComponent<TextMesh>();
        text.text = message;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 48;
        text.characterSize = 0.035f;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.GetComponent<MeshRenderer>().sortingOrder = 20;
        label.AddComponent<FloatingCombatText>().Initialize(text, color);
    }

    private void Initialize(TextMesh text, Color color)
    {
        _text = text;
        _color = color;
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;
        const float duration = 0.75f;
        Vector3 start = transform.position;
        while (elapsed < duration)
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                transform.rotation = Quaternion.LookRotation(-camera.transform.forward, camera.transform.up);
                transform.position = start + camera.transform.up * (elapsed * 0.9f);
            }
            Color faded = _color;
            faded.a = 1f - elapsed / duration;
            _text.color = faded;
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}
