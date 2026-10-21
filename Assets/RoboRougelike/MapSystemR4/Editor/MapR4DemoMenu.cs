using UnityEditor;
using UnityEngine;
using RoboRougelike.MapSystemR4;

namespace RoboRougelike.MapSystemR4.Editor
{
    public static class MapR4DemoMenu
    {
        [MenuItem("Tools/RoboRougelike/MAP R6/Create Demo In Current Scene")]
        public static void CreateDemo()
        {
            var go = new GameObject("MAP_R4_DEMO_CONTROLLER");
            Undo.RegisterCreatedObjectUndo(go, "Create MAP R4 demo");
            go.AddComponent<MapR4DemoController>();
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            Debug.Log("MAP R6: press Play. Switch mode in Inspector. This is a sandbox, not a production scene.");
        }
    }
}
