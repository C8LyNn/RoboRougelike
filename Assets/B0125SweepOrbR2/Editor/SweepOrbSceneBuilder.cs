#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using B0125.SweepOrb.R2;

namespace B0125.SweepOrb.Editor
{
    public static class SweepOrbSceneBuilder
    {
        private const string SceneFolder = "Assets/B0125SweepOrbR2/Scenes";
        private const string ScenePath = SceneFolder + "/SWEEPORB_R2_Demo.unity";

        [MenuItem("B01/PASS17 W11/Create SWEEPORB R2 Demo Scene")]
        public static void Create()
        {
            // Never overwrite an existing developer-authored scene.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null ||
                System.IO.File.Exists(ScenePath))
                throw new InvalidOperationException("Refusing to overwrite " + ScenePath);
            if (!AssetDatabase.IsValidFolder(SceneFolder))
                AssetDatabase.CreateFolder("Assets/B0125SweepOrbR2", "Scenes");

            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                if (!SceneManager.SetActiveScene(scene))
                    throw new InvalidOperationException("Cannot activate isolated scene");

                var game = new GameObject("SWEEPORB_R2_Standalone_Physics_Demo");
                game.AddComponent<SweepOrbUnityDemo>();

                var cameraObject = new GameObject("W11 TopDown Camera");
                var cam = cameraObject.AddComponent<Camera>();
                cameraObject.transform.position = new Vector3(32f, 32f, 20f);
                cameraObject.transform.LookAt(new Vector3(32f, 0f, 40f));
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.fieldOfView = 55;

                var light = new GameObject("W11 Sun");
                var source = light.AddComponent<Light>();
                source.type = LightType.Directional;
                source.intensity = 1.2f;
                light.transform.rotation = Quaternion.Euler(50, -20, 0);

                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException("Could not save " + ScenePath);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
