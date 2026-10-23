#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RoboRougelike.Mechanics.COV001.Editor
{
    public static class COV001DemoPrefabBuilder
    {
        private const string Path =
            "Assets/RoboRougelikeMechanics/COV001DestructibleCover/Demo/COV001_ArenaCover.prefab";

        [MenuItem("Tools/RoboRougelike/COV-001/Create Demo Prefab")]
        public static void CreateDemoPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Path) != null)
            {
                Debug.Log("COV-001: demo prefab already exists; not overwriting.");
                return;
            }
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "COV001_ArenaCover";
            root.transform.localScale = new Vector3(3f, 2f, 0.7f);
            root.AddComponent<DestructibleArenaCover>();
            root.AddComponent<COV001DemoDriver>();
            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, Path);
                Debug.Log("COV-001: demo prefab created at " + Path);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
#endif
