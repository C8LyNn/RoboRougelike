#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoboRougelike.Mechanics.HAZ001.Editor
{
    public static class HazardDemoSceneBuilder
    {
        [MenuItem("Tools/RoboRougelike/HAZ-001/Create Additive Demo")]
        private static void CreateDemo()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("HAZ-001: stop Play Mode before creating the demo.");
                return;
            }
            // Never save or edit the existing main scene. Demo is an unsaved additive scene.
            Scene demo = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,
                NewSceneMode.Additive);
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "HAZ001 Demo Floor";
            floor.transform.position = new Vector3(0f, -0.25f, 0f);
            floor.transform.localScale = new Vector3(14f, 0.5f, 14f);
            SceneManager.MoveGameObjectToScene(floor, demo);

            GameObject hazard = new GameObject("HAZ001 Hazard - opt-in demo only");
            SceneManager.MoveGameObjectToScene(hazard, demo);
            var zone = hazard.AddComponent<TelegraphedHazardZone>();
            zone.Configure(1.5f, 4f, 2f, 3f, 1f, 5f, 1 << 8);
            hazard.AddComponent<HazardRingIndicator>();
            hazard.AddComponent<HazardDemoHarness>();

            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = "HAZ001 Demo Target (requests only)";
            target.layer = 8;
            target.transform.position = new Vector3(1.5f, 0.7f, 0f);
            SceneManager.MoveGameObjectToScene(target, demo);
            Selection.activeGameObject = hazard;
            Debug.Log("HAZ-001: unsaved additive demo created. Enter Play to see warning and requests. No damage is applied.");
        }
    }
}
#endif
