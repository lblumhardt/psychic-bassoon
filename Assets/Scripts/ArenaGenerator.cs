using System.Collections.Generic;
using UnityEngine;

public class ArenaGenerator : MonoBehaviour
{
    private const int SpawnCountPerTeam = 5;
    private const float WallHeight = 1.5f;
    private const float WallThickness = 0.55f;

    private readonly List<Vector3> _playerSpawns = new();
    private readonly List<Vector3> _enemySpawns = new();
    private Material _wallMaterial;

    public int ArenaIndex { get; private set; }
    public string ArenaName { get; private set; }

    public void GenerateRandomArena()
    {
        DisableSceneWalls();
        ArenaIndex = Random.Range(0, 10);
        BuildArena(ArenaIndex);
        Debug.Log($"Arena selected: {ArenaName}", this);
    }

    public Vector3 GetSpawnPoint(Team team, int index)
    {
        List<Vector3> points = team == Team.Player ? _playerSpawns : _enemySpawns;
        if (points.Count == 0) return team == Team.Player ? Vector3.left * 8f : Vector3.right * 8f;
        return points[Mathf.Clamp(index, 0, points.Count - 1)];
    }

    private void BuildArena(int index)
    {
        GameObject root = new GameObject($"Generated Arena {index + 1}");
        root.transform.SetParent(transform, false);

        switch (index)
        {
            case 0:
                ArenaName = "Open Field";
                RectangleBoundary(root.transform, 30f, 18f);
                SetSpawns(11f, 6f);
                break;
            case 1:
                ArenaName = "The Gate";
                RectangleBoundary(root.transform, 30f, 18f);
                Wall(root.transform, new Vector3(0f, 0f, -5.5f), new Vector3(0f, 0f, -1.5f));
                Wall(root.transform, new Vector3(0f, 0f, 1.5f), new Vector3(0f, 0f, 5.5f));
                SetSpawns(11f, 6f);
                break;
            case 2:
                ArenaName = "Crossroads";
                RectangleBoundary(root.transform, 30f, 18f);
                Wall(root.transform, new Vector3(-4.5f, 0f, 0f), new Vector3(-1.3f, 0f, 0f));
                Wall(root.transform, new Vector3(1.3f, 0f, 0f), new Vector3(4.5f, 0f, 0f));
                Wall(root.transform, new Vector3(0f, 0f, -5f), new Vector3(0f, 0f, -1.3f));
                Wall(root.transform, new Vector3(0f, 0f, 1.3f), new Vector3(0f, 0f, 5f));
                SetSpawns(11f, 6f);
                break;
            case 3:
                ArenaName = "Four Corners";
                RectangleBoundary(root.transform, 30f, 18f);
                Pillar(root.transform, new Vector3(-4f, 0f, -4f));
                Pillar(root.transform, new Vector3(-4f, 0f, 4f));
                Pillar(root.transform, new Vector3(4f, 0f, -4f));
                Pillar(root.transform, new Vector3(4f, 0f, 4f));
                SetSpawns(11f, 6f);
                break;
            case 4:
                ArenaName = "Zigzag";
                RectangleBoundary(root.transform, 30f, 18f);
                Wall(root.transform, new Vector3(-6f, 0f, -6f), new Vector3(-1f, 0f, -2f));
                Wall(root.transform, new Vector3(-1f, 0f, -2f), new Vector3(4f, 0f, -6f));
                Wall(root.transform, new Vector3(-4f, 0f, 6f), new Vector3(1f, 0f, 2f));
                Wall(root.transform, new Vector3(1f, 0f, 2f), new Vector3(6f, 0f, 6f));
                SetSpawns(11f, 5.5f);
                break;
            case 5:
                ArenaName = "Diamond";
                PolygonBoundary(root.transform, new[]
                {
                    new Vector3(-15f, 0f, 0f), new Vector3(0f, 0f, 9f),
                    new Vector3(15f, 0f, 0f), new Vector3(0f, 0f, -9f)
                });
                SetDiamondSpawns();
                break;
            case 6:
                ArenaName = "Octagon";
                PolygonBoundary(root.transform, new[]
                {
                    new Vector3(-12f, 0f, -9f), new Vector3(12f, 0f, -9f),
                    new Vector3(15f, 0f, -6f), new Vector3(15f, 0f, 6f),
                    new Vector3(12f, 0f, 9f), new Vector3(-12f, 0f, 9f),
                    new Vector3(-15f, 0f, 6f), new Vector3(-15f, 0f, -6f)
                });
                Pillar(root.transform, Vector3.zero, 2.4f);
                SetSpawns(11f, 5.5f);
                break;
            case 7:
                ArenaName = "Twin Lanes";
                RectangleBoundary(root.transform, 30f, 18f);
                Wall(root.transform, new Vector3(-7f, 0f, -2f), new Vector3(7f, 0f, -2f));
                Wall(root.transform, new Vector3(-7f, 0f, 2f), new Vector3(7f, 0f, 2f));
                SetSpawns(11f, 6f);
                break;
            case 8:
                ArenaName = "The Box";
                RectangleBoundary(root.transform, 30f, 18f);
                Wall(root.transform, new Vector3(-4f, 0f, -4f), new Vector3(4f, 0f, -4f));
                Wall(root.transform, new Vector3(-4f, 0f, 4f), new Vector3(4f, 0f, 4f));
                Wall(root.transform, new Vector3(-4f, 0f, -4f), new Vector3(-4f, 0f, -1f));
                Wall(root.transform, new Vector3(-4f, 0f, 1f), new Vector3(-4f, 0f, 4f));
                Wall(root.transform, new Vector3(4f, 0f, -4f), new Vector3(4f, 0f, -1f));
                Wall(root.transform, new Vector3(4f, 0f, 1f), new Vector3(4f, 0f, 4f));
                SetSpawns(11f, 6f);
                break;
            default:
                ArenaName = "Pinball";
                RectangleBoundary(root.transform, 30f, 18f);
                Wall(root.transform, new Vector3(-7f, 0f, -5f), new Vector3(-2f, 0f, -1f));
                Wall(root.transform, new Vector3(-7f, 0f, 5f), new Vector3(-2f, 0f, 1f));
                Wall(root.transform, new Vector3(7f, 0f, -5f), new Vector3(2f, 0f, -1f));
                Wall(root.transform, new Vector3(7f, 0f, 5f), new Vector3(2f, 0f, 1f));
                Pillar(root.transform, Vector3.zero, 1.8f);
                SetSpawns(11f, 6f);
                break;
        }
    }

    private void DisableSceneWalls()
    {
        foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (candidate.parent == null && candidate.name.StartsWith("Wall"))
                candidate.gameObject.SetActive(false);
        }
    }

    private void RectangleBoundary(Transform root, float width, float depth)
    {
        float halfWidth = width * 0.5f;
        float halfDepth = depth * 0.5f;
        Vector3 a = new Vector3(-halfWidth, 0f, -halfDepth);
        Vector3 b = new Vector3(halfWidth, 0f, -halfDepth);
        Vector3 c = new Vector3(halfWidth, 0f, halfDepth);
        Vector3 d = new Vector3(-halfWidth, 0f, halfDepth);
        Wall(root, a, b);
        Wall(root, b, c);
        Wall(root, c, d);
        Wall(root, d, a);
    }

    private void PolygonBoundary(Transform root, IReadOnlyList<Vector3> points)
    {
        for (int i = 0; i < points.Count; i++) Wall(root, points[i], points[(i + 1) % points.Count]);
    }

    private void Wall(Transform root, Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;
        if (length < 0.01f) return;

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Arena Wall";
        wall.layer = LayerMask.NameToLayer("Wall");
        wall.transform.SetParent(root, false);
        wall.transform.position = (start + end) * 0.5f + Vector3.up * WallHeight * 0.5f;
        wall.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        wall.transform.localScale = new Vector3(WallThickness, WallHeight, length + WallThickness);
        wall.GetComponent<Renderer>().sharedMaterial = WallMaterial();
    }

    private void Pillar(Transform root, Vector3 position, float size = 2f)
    {
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pillar.name = "Arena Pillar";
        pillar.layer = LayerMask.NameToLayer("Wall");
        pillar.transform.SetParent(root, false);
        pillar.transform.position = position + Vector3.up * WallHeight * 0.5f;
        pillar.transform.localScale = new Vector3(size, WallHeight, size);
        pillar.GetComponent<Renderer>().sharedMaterial = WallMaterial();
    }

    private Material WallMaterial()
    {
        if (_wallMaterial != null) return _wallMaterial;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        _wallMaterial = new Material(shader) { color = new Color(0.22f, 0.3f, 0.42f) };
        return _wallMaterial;
    }

    private void SetSpawns(float x, float zExtent)
    {
        _playerSpawns.Clear();
        _enemySpawns.Clear();
        for (int i = 0; i < SpawnCountPerTeam; i++)
        {
            float z = Mathf.Lerp(-zExtent, zExtent, i / 4f);
            _playerSpawns.Add(new Vector3(-x, 0f, z));
            _enemySpawns.Add(new Vector3(x, 0f, -z));
        }
    }

    private void SetDiamondSpawns()
    {
        _playerSpawns.Clear();
        _enemySpawns.Clear();
        float[] zValues = { -2.6f, -1.3f, 0f, 1.3f, 2.6f };
        foreach (float z in zValues)
        {
            _playerSpawns.Add(new Vector3(-9f, 0f, z));
            _enemySpawns.Add(new Vector3(9f, 0f, -z));
        }
    }

    private void OnDestroy()
    {
        if (_wallMaterial != null) Destroy(_wallMaterial);
    }
}
