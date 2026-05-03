using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Builds a small procedural texture at runtime and assigns it to a quad child mesh.
/// Combines several cheap generators; optional bilateral symmetry applies to every mode.
/// </summary>
public class PixelArtGenerator : MonoBehaviour
{
    public enum GenerationMode
    {
        PerlinSilhouette,
        Metaballs,
        Voronoi,
        CellularAutomata,
        HybridMetaballVoronoi,
        LucasImpl
    }

    [SerializeField] private GenerationMode mode = GenerationMode.HybridMetaballVoronoi;
    [Tooltip("Mirrors the left half onto the right — reads more ‘creature-like’ at almost no cost.")]
    [SerializeField] private bool bilateralSymmetry = true;
    [Tooltip("Clears diagonal wedges in corners so the silhouette reads rounder. With Bilateral Symmetry, only the left corners are cut then mirrored so both sides match.")]
    [SerializeField] private bool roundishShape;
    [Tooltip("Each corner uses a span×span block with a triangular cut (default 10 → 10×10 corners on a 32² texture). Clamped to half resolution.")]
    [SerializeField] [Min(1)] private int roundishCornerSpan = 10;
    [SerializeField] private int resolution = 32;
    [SerializeField] private bool generateOnStart = true;
    [Tooltip("When false, Random.InitState(seed) is used before generation.")]
    [SerializeField] private bool randomizeSeedOnAwake = true;
    [SerializeField] private int randomSeed;

    [Header("Quad")]
    [Tooltip("Assign the quad's Mesh Renderer. If empty, uses the first MeshRenderer in children.")]
    [SerializeField] private MeshRenderer quadRenderer;

    [Header("Palette")]
    [SerializeField] private int paletteSize = 4;

    [Header("Perlin silhouette")]
    [SerializeField] private float perlinScale = 0.14f;
    [SerializeField] [Range(0f, 1f)] private float perlinThreshold = 0.42f;

    [Header("Metaballs")]
    [SerializeField] private int metaballCount = 5;
    [SerializeField] private float metaballThreshold = 1.35f;

    [Header("Voronoi")]
    [SerializeField] private int voronoiSites = 10;

    [Header("Cellular (majority blur)")]
    [SerializeField] private int cellularPasses = 6;

    [Header("Hybrid mask")]
    [SerializeField] private float hybridMetaballThreshold = 1.15f;

    [Header("Lucas impl")]
    [Tooltip("Metaball field above this clears pixels (transparent). Two erase passes use independent blob layouts.")]
    [SerializeField] private float lucasEraseMetaballThreshold = 1.28f;
    [Tooltip("Scales Voronoi RGB before additive clamp-add onto existing pixels.")]
    [SerializeField] [Range(0.05f, 1.5f)] private float lucasAdditiveHybridStrength = 0.55f;

    private Texture2D _runtimeTexture;

    private void Reset()
    {
        quadRenderer = GetComponentInChildren<MeshRenderer>();
    }

    private void Awake()
    {
        if (quadRenderer == null)
        {
            quadRenderer = GetComponentInChildren<MeshRenderer>();
        }
    }

    private void Start()
    {
        if (generateOnStart)
        {
            Generate();
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Generate();
        }
    }

    private void OnDestroy()
    {
        DestroyRuntimeTexture();
    }

    [ContextMenu("Regenerate pixel art")]
    public void Generate()
    {
        if (quadRenderer == null)
        {
            Debug.LogWarning($"{nameof(PixelArtGenerator)} on {name}: no MeshRenderer found.", this);
            return;
        }

        resolution = Mathf.Clamp(resolution, 8, 128);
        paletteSize = Mathf.Clamp(paletteSize, 2, 12);

        if (randomizeSeedOnAwake)
        {
            randomSeed = Random.Range(int.MinValue, int.MaxValue);
        }

        Random.InitState(randomSeed);

        var palette = BuildPalette(paletteSize);
        var pixels = new Color32[resolution * resolution];

        switch (mode)
        {
            case GenerationMode.PerlinSilhouette:
                FillPerlinSilhouette(pixels, palette);
                break;
            case GenerationMode.Metaballs:
                FillMetaballs(pixels, palette);
                break;
            case GenerationMode.Voronoi:
                FillVoronoi(pixels, palette);
                break;
            case GenerationMode.CellularAutomata:
                FillCellularAutomata(pixels, palette);
                break;
            case GenerationMode.HybridMetaballVoronoi:
                FillHybridMetaballVoronoi(pixels, palette);
                break;
            case GenerationMode.LucasImpl:
                FillLucasImpl(pixels, palette);
                break;
        }

        if (roundishShape)
        {
            int span = Mathf.Clamp(roundishCornerSpan, 1, Mathf.Max(1, resolution / 2));
            if (bilateralSymmetry)
            {
                ClearCornerTriangleTopLeft(pixels, palette[0], resolution, span);
                ClearCornerTriangleBottomLeft(pixels, palette[0], resolution, resolution, span);
            }
            else
            {
                ClearCornerTriangleTopLeft(pixels, palette[0], resolution, span);
                ClearCornerTriangleTopRight(pixels, palette[0], resolution, span);
                ClearCornerTriangleBottomLeft(pixels, palette[0], resolution, resolution, span);
                ClearCornerTriangleBottomRight(pixels, palette[0], resolution, resolution, span);
            }
        }

        if (bilateralSymmetry)
        {
            ApplyHorizontalSymmetry(pixels, resolution, resolution);
        }

        DestroyRuntimeTexture();
        _runtimeTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, mipChain: false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"RuntimePixelArt_{randomSeed}"
        };
        _runtimeTexture.SetPixels32(pixels);
        _runtimeTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

        quadRenderer.material.mainTexture = _runtimeTexture;
    }

    private void DestroyRuntimeTexture()
    {
        if (_runtimeTexture != null)
        {
            Destroy(_runtimeTexture);
            _runtimeTexture = null;
        }
    }

    private static Color32[] BuildPalette(int count)
    {
        var palette = new Color32[count];
        float baseHue = Random.value;
        for (int i = 0; i < count; i++)
        {
            float h = Mathf.Repeat(baseHue + i * (0.17f + Random.Range(-0.03f, 0.03f)), 1f);
            float s = Mathf.Clamp01(Random.Range(0.35f, 0.72f));
            float v = Mathf.Clamp01(Random.Range(0.45f, 0.95f));
            Color32 c = Color.HSVToRGB(h, s, v);
            c.a = byte.MaxValue;
            palette[i] = c;
        }

        palette[0].a = 0;
        return palette;
    }

    private void FillPerlinSilhouette(Color32[] pixels, Color32[] palette)
    {
        Color32 body = palette[Mathf.Min(1, palette.Length - 1)];
        Color32 accent = palette[Mathf.Min(2, palette.Length - 1)];
        float ox = Random.Range(0f, 8192f);
        float oy = Random.Range(0f, 8192f);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float nx = ox + x * perlinScale;
                float ny = oy + y * perlinScale;
                float n = Mathf.PerlinNoise(nx, ny);
                bool inside = n > perlinThreshold;
                pixels[y * resolution + x] = inside ? body : palette[0];
            }
        }

        SprinkleAccent(pixels, accent, palette[0]);
    }

    private void FillMetaballs(Color32[] pixels, Color32[] palette)
    {
        var centers = new Vector2[metaballCount];
        var weights = new float[metaballCount];
        SampleMetaballCenters(centers, weights);

        Color32 body = palette[Mathf.Min(1, palette.Length - 1)];
        Color32 accent = palette[Mathf.Min(2, palette.Length - 1)];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float field = SumMetaballField(px, py, centers, weights);

                pixels[y * resolution + x] = field > metaballThreshold ? body : palette[0];
            }
        }

        SprinkleAccent(pixels, accent, palette[0]);
    }

    private void FillVoronoi(Color32[] pixels, Color32[] palette)
    {
        var sites = new Vector2[voronoiSites];
        var colors = new Color32[voronoiSites];
        for (int i = 0; i < voronoiSites; i++)
        {
            sites[i] = new Vector2(Random.Range(0f, resolution), Random.Range(0f, resolution));
            colors[i] = palette[Random.Range(1, palette.Length)];
        }

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                int best = 0;
                float bestD = float.MaxValue;
                for (int i = 0; i < voronoiSites; i++)
                {
                    float dx = px - sites[i].x;
                    float dy = py - sites[i].y;
                    float d = dx * dx + dy * dy;
                    if (d < bestD)
                    {
                        bestD = d;
                        best = i;
                    }
                }

                pixels[y * resolution + x] = colors[best];
            }
        }
    }

    private void FillCellularAutomata(Color32[] pixels, Color32[] palette)
    {
        int len = resolution * resolution;
        var idx = new int[len];
        for (int i = 0; i < len; i++)
        {
            idx[i] = Random.Range(1, palette.Length);
        }

        var next = new int[len];
        var votes = new int[palette.Length];
        for (int pass = 0; pass < cellularPasses; pass++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    for (int k = 0; k < palette.Length; k++)
                    {
                        votes[k] = 0;
                    }

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int yy = Mathf.Clamp(y + dy, 0, resolution - 1);
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int xx = Mathf.Clamp(x + dx, 0, resolution - 1);
                            votes[idx[yy * resolution + xx]]++;
                        }
                    }

                    int best = 1;
                    int bestVotes = votes[1];
                    for (int k = 2; k < palette.Length; k++)
                    {
                        if (votes[k] > bestVotes)
                        {
                            bestVotes = votes[k];
                            best = k;
                        }
                    }

                    next[y * resolution + x] = best;
                }
            }

            (idx, next) = (next, idx);
        }

        for (int i = 0; i < len; i++)
        {
            pixels[i] = palette[idx[i]];
        }
    }

    private void FillHybridMetaballVoronoi(Color32[] pixels, Color32[] palette)
    {
        var metabCenters = new Vector2[metaballCount];
        var metabWeights = new float[metaballCount];
        SampleMetaballCenters(metabCenters, metabWeights);

        var sites = new Vector2[voronoiSites];
        var siteColors = new Color32[voronoiSites];
        for (int i = 0; i < voronoiSites; i++)
        {
            sites[i] = new Vector2(Random.Range(0f, resolution), Random.Range(0f, resolution));
            siteColors[i] = palette[Random.Range(1, palette.Length)];
        }

        Color32 accent = palette[Mathf.Min(2, palette.Length - 1)];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;

                float field = SumMetaballField(px, py, metabCenters, metabWeights);

                if (field <= hybridMetaballThreshold)
                {
                    pixels[y * resolution + x] = palette[0];
                    continue;
                }

                int best = 0;
                float bestD = float.MaxValue;
                for (int i = 0; i < voronoiSites; i++)
                {
                    float dx = px - sites[i].x;
                    float dy = py - sites[i].y;
                    float d = dx * dx + dy * dy;
                    if (d < bestD)
                    {
                        bestD = d;
                        best = i;
                    }
                }

                pixels[y * resolution + x] = siteColors[best];
            }
        }

        SprinkleAccent(pixels, accent, palette[0]);
    }

    /// <summary>
    /// LucasImpl pipeline:
    /// <list type="number">
    /// <item><description>Cellular automata — same majority-blur pass as <see cref="FillCellularAutomata"/> for coherent color blobs.</description></item>
    /// <item><description>Two metaball erase passes — independent random metaball layouts; wherever the scalar field exceeds <see cref="lucasEraseMetaballThreshold"/>, the pixel is forced to transparent (ball-shaped negative space).</description></item>
    /// <item><description>Additive hybrid metaball+Voronoi — a fresh metaball mask selects where to apply Voronoi cell colors; those RGB values are scaled by <see cref="lucasAdditiveHybridStrength"/> and clamp-added onto whatever survived (cellular minus holes), so highlights stack instead of fully replacing the base.</description></item>
    /// </list>
    /// </summary>
    private void FillLucasImpl(Color32[] pixels, Color32[] palette)
    {
        FillCellularAutomata(pixels, palette);
        ErasePixelsWithMetaballMask(pixels, palette[0], lucasEraseMetaballThreshold);
        ErasePixelsWithMetaballMask(pixels, palette[0], lucasEraseMetaballThreshold);
        AdditiveHybridMetaballVoronoiPass(pixels, palette);
    }

    private void ErasePixelsWithMetaballMask(Color32[] pixels, Color32 transparent, float threshold)
    {
        var centers = new Vector2[metaballCount];
        var weights = new float[metaballCount];
        SampleMetaballCenters(centers, weights);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float field = SumMetaballField(x + 0.5f, y + 0.5f, centers, weights);
                if (field > threshold)
                {
                    pixels[y * resolution + x] = transparent;
                }
            }
        }
    }

    private void AdditiveHybridMetaballVoronoiPass(Color32[] pixels, Color32[] palette)
    {
        var metabCenters = new Vector2[metaballCount];
        var metabWeights = new float[metaballCount];
        SampleMetaballCenters(metabCenters, metabWeights);

        var sites = new Vector2[voronoiSites];
        var siteColors = new Color32[voronoiSites];
        for (int i = 0; i < voronoiSites; i++)
        {
            sites[i] = new Vector2(Random.Range(0f, resolution), Random.Range(0f, resolution));
            siteColors[i] = palette[Random.Range(1, palette.Length)];
        }

        float strength = lucasAdditiveHybridStrength;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;

                float field = SumMetaballField(px, py, metabCenters, metabWeights);
                if (field <= hybridMetaballThreshold)
                {
                    continue;
                }

                int best = 0;
                float bestD = float.MaxValue;
                for (int i = 0; i < voronoiSites; i++)
                {
                    float dx = px - sites[i].x;
                    float dy = py - sites[i].y;
                    float d = dx * dx + dy * dy;
                    if (d < bestD)
                    {
                        bestD = d;
                        best = i;
                    }
                }

                int idx = y * resolution + x;
                pixels[idx] = AdditiveClampRgb(pixels[idx], siteColors[best], strength);
            }
        }
    }

    private static Color32 AdditiveClampRgb(Color32 dst, Color32 add, float strength)
    {
        int hr = Mathf.RoundToInt(add.r * strength);
        int hg = Mathf.RoundToInt(add.g * strength);
        int hb = Mathf.RoundToInt(add.b * strength);
        byte a = dst.a > 0 ? (byte)Mathf.Max(dst.a, add.a) : add.a;

        return new Color32(
            (byte)Mathf.Clamp(dst.r + hr, 0, 255),
            (byte)Mathf.Clamp(dst.g + hg, 0, 255),
            (byte)Mathf.Clamp(dst.b + hb, 0, 255),
            a);
    }

    private void SampleMetaballCenters(Vector2[] centers, float[] weights)
    {
        for (int i = 0; i < metaballCount; i++)
        {
            centers[i] = new Vector2(Random.Range(-4f, resolution + 4f), Random.Range(-4f, resolution + 4f));
            weights[i] = Random.Range(resolution * 0.35f, resolution * 0.65f);
        }
    }

    private static float SumMetaballField(float px, float py, Vector2[] centers, float[] weights)
    {
        float field = 0f;
        for (int i = 0; i < centers.Length; i++)
        {
            float dx = px - centers[i].x;
            float dy = py - centers[i].y;
            float d2 = dx * dx + dy * dy + 1e-4f;
            field += weights[i] / d2;
        }

        return field;
    }

    private void SprinkleAccent(Color32[] pixels, Color32 accent, Color32 transparent)
    {
        int dots = Mathf.Max(2, resolution / 8);
        for (int i = 0; i < dots; i++)
        {
            int x = Random.Range(2, resolution - 2);
            int y = Random.Range(2, resolution - 2);
            int idx = y * resolution + x;
            if (pixels[idx].a == 0)
            {
                continue;
            }

            pixels[idx] = accent;
            if (Random.value > 0.5f)
            {
                pixels[idx + 1] = accent;
            }

            if (Random.value > 0.65f)
            {
                pixels[idx - 1] = accent;
            }
        }
    }

    private static void ApplyHorizontalSymmetry(Color32[] pixels, int width, int height)
    {
        int half = (width + 1) / 2;
        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < half; x++)
            {
                pixels[row + (width - 1 - x)] = pixels[row + x];
            }
        }
    }

    /// <summary>Right angle at top-left outer pixel; hypotenuse cuts toward the interior.</summary>
    private static void ClearCornerTriangleTopLeft(Color32[] pixels, Color32 transparent, int w, int span)
    {
        for (int ly = 0; ly < span; ly++)
        {
            for (int lx = 0; lx < span; lx++)
            {
                if (lx + ly <= span - 1)
                {
                    pixels[ly * w + lx] = transparent;
                }
            }
        }
    }

    /// <summary>Right angle at top-right outer pixel; uses distance from the right edge so the cut mirrors the left corner.</summary>
    private static void ClearCornerTriangleTopRight(Color32[] pixels, Color32 transparent, int w, int span)
    {
        for (int y = 0; y < span; y++)
        {
            for (int x = w - span; x < w; x++)
            {
                int u = (w - 1) - x;
                int v = y;
                if (u + v <= span - 1)
                {
                    pixels[y * w + x] = transparent;
                }
            }
        }
    }

    /// <summary>Right angle at bottom-left outer pixel.</summary>
    private static void ClearCornerTriangleBottomLeft(Color32[] pixels, Color32 transparent, int w, int h, int span)
    {
        for (int y = h - span; y < h; y++)
        {
            for (int x = 0; x < span; x++)
            {
                int u = x;
                int v = (h - 1) - y;
                if (u + v <= span - 1)
                {
                    pixels[y * w + x] = transparent;
                }
            }
        }
    }

    /// <summary>Right angle at bottom-right outer pixel.</summary>
    private static void ClearCornerTriangleBottomRight(Color32[] pixels, Color32 transparent, int w, int h, int span)
    {
        for (int y = h - span; y < h; y++)
        {
            for (int x = w - span; x < w; x++)
            {
                int u = (w - 1) - x;
                int v = (h - 1) - y;
                if (u + v <= span - 1)
                {
                    pixels[y * w + x] = transparent;
                }
            }
        }
    }
}
