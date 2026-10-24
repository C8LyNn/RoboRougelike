#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoboRougelike.Mechanics.OBJ001.Editor
{
    public static class OBJ001DemoSceneBuilder
    {
        [MenuItem("Tools/RoboRougelike/OBJ-001/Open Isolated Reactor Demo")]
        public static void OpenDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject reactor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            reactor.name = "OBJ001 Demo Reactor";
            reactor.transform.position = Vector3.zero;
            reactor.transform.localScale = new Vector3(2f, 2f, 2f);
            var objective = reactor.AddComponent<ReactorDefenseObjective>();
            objective.Configure(100f, 25f);
            reactor.AddComponent<ReactorDefenseDemoUI>();
            Selection.activeGameObject = reactor;
            Debug.Log("OBJ-001: new unsaved demo scene. Enter Play Mode and use UI buttons. Do not save over the main scene.");
        }
    }
}
#endif
