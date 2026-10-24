using System.Linq;
using UnityEditor;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.VisualIntegration;

public static class RRLVisualPilotCatalogSetup
{
    const string Folder = "Assets/RRLImportedModels";
    const string ResourceCatalog = "Assets/Resources/RRLPartVisualCatalog.asset";

    [MenuItem("Tools/RoboRougelike/Visual Models/Configure W01 + H01 Pilot")]
    public static void Configure()
    {
        var left = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/W01/W01_R2_Game_L.glb");
        var right = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/W01/W01_R2_Game_R.glb");
        var hover = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/H01/H01_V4_SideDrive_Animated.glb");
        if (left == null || right == null || hover == null)
        {
            Debug.LogError("[RRL Visual] Missing imported W01 L/R or H01 GLB. " +
                "Install glTFast and copy the original GLBs into Assets/RRLImportedModels/{W01,H01}/ first.");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        var catalog = AssetDatabase.LoadAssetAtPath<PartVisualCatalog>(ResourceCatalog);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<PartVisualCatalog>();
            AssetDatabase.CreateAsset(catalog, ResourceCatalog);
        }

        // Leave unrelated customization untouched if this menu is run again.
        Undo.RecordObject(catalog, "Configure RRL Visual Pilot");
        catalog.entries.RemoveAll(e => e != null &&
            (e.kind == R3PartKind.Wheel || e.kind == R3PartKind.HoverEngine));
        catalog.entries.Add(Wheel(left, PartVisualSide.Left));
        catalog.entries.Add(Wheel(right, PartVisualSide.Right));
        catalog.entries.Add(new PartVisualCatalog.Entry
        {
            kind = R3PartKind.HoverEngine, side = PartVisualSide.Any,
            prefab = hover, followLogicalPivot = false,
            localOffset = new Vector3(0f, -0.45f, 0f), localScale = Vector3.one
        });
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log("[RRL Visual] W01 left/right and H01 model registry created. " +
            "Check axis, materials and size in Unity BEFORE marking the pilot PASS.");
        Selection.activeObject = catalog;
    }

    static PartVisualCatalog.Entry Wheel(GameObject prefab, PartVisualSide side) =>
        new PartVisualCatalog.Entry
        {
            kind = R3PartKind.Wheel, side = side, prefab = prefab,
            followLogicalPivot = true,
            localEuler = new Vector3(0f, 0f, -90f),
            localScale = Vector3.one * 1.43f
        };
}
