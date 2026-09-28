using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Include every authored asset in builds, including moves outside any creature's pool.
[InitializeOnLoad]
public class SandboxCatalogBuilder : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    static SandboxCatalogBuilder()
    {
        EditorApplication.delayCall += Refresh;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) Refresh();
        };
    }

    public void OnPreprocessBuild(BuildReport report) => Refresh();

    public static void Refresh()
    {
        const string path = "Assets/Resources/SandboxCatalog.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<SandboxCatalog>(path);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<SandboxCatalog>();
            AssetDatabase.CreateAsset(catalog, path);
        }
        catalog.creatures = Find<CreatureDataSO>();
        catalog.moves = Find<AttackDataSO>();
        catalog.abilities = Find<CreatureAbilitySO>();
        catalog.items = Find<CreatureItemSO>();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }

    private static T[] Find<T>() where T : Object => AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets" })
        .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<T>)
        .Where(asset => asset != null).OrderBy(asset => asset.name).ToArray();
}
