using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace B01.DebrisPlayground001.Editor
{
    public static class DebrisPlaygroundSceneBuilder
    {
        private const string SceneDir = "Assets/B0123DebrisPlayground001/Generated";
        private const string ScenePath = SceneDir + "/B0123DebrisPlayground.unity";

        [MenuItem("B01 / PASS17 / Create isolated debris physics playground")]
        public static void CreateScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Debug.LogError("Debris playground scene already exists. Refusing overwrite: " + ScenePath);
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (!AssetDatabase.IsValidFolder(SceneDir))
                AssetDatabase.CreateFolder("Assets/B0123DebrisPlayground001", "Generated");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 13f, -17f);
            cameraGo.transform.rotation = Quaternion.Euler(36f, 0f, 0f);
            Camera camera = cameraGo.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.11f, 0.14f);

            var light = new GameObject("Demo Directional Light", typeof(Light));
            light.transform.rotation = Quaternion.Euler(40f, -25f, 0f);
            light.GetComponent<Light>().type = LightType.Directional;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Isolated floor";
            floor.transform.position = new Vector3(0f, -0.15f, 0f);
            floor.transform.localScale = new Vector3(20f, 0.3f, 9f);

            var controller = new GameObject("B01-23 Detached Physics Demo");
            controller.AddComponent<DebrisRigPlayground>();
            // The two physical rigs are created by the component on entering Play mode.
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new System.InvalidOperationException("Could not save isolated demo scene");
            Debug.Log("Created isolated physics demo: " + ScenePath +
                      " — press Play; no canonical scene or robot edited.");
        }
    }
}
