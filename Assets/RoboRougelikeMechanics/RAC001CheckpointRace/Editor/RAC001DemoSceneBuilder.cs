#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoboRougelike.Mechanics.RAC001.Editor
{
    public static class RAC001DemoSceneBuilder
    {
        [MenuItem("Tools/RoboRougelike/RAC-001/Open Unsaved Demo (Additive)")]
        public static void OpenDemo()
        {
            // Adds an UNSAVED scene; does not touch scene 1 or any existing scene/asset.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            var root = new GameObject("RAC-001 Demo (unsaved)");
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Sample runner (NOT the player vehicle)";
            cube.transform.SetParent(root.transform);
            cube.transform.position = Vector3.zero;

            Vector3[] positions = { new Vector3(8f, 0f, 0f), new Vector3(8f, 0f, 8f), new Vector3(0f, 0f, 8f) };
            Transform[] checkpoints = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = "Checkpoint " + (i + 1);
                marker.transform.SetParent(root.transform);
                marker.transform.position = positions[i];
                marker.transform.localScale = new Vector3(1.5f, 0.05f, 1.5f);
                checkpoints[i] = marker.transform;
            }

            var cameraObject = new GameObject("RAC-001 Demo Camera");
            cameraObject.transform.SetParent(root.transform);
            cameraObject.transform.position = new Vector3(4f, 17f, -14f);
            cameraObject.transform.LookAt(new Vector3(4f, 0f, 4f));
            cameraObject.AddComponent<Camera>();

            var race = root.AddComponent<CheckpointRaceChallenge>();
            race.Configure(cube.transform, checkpoints, 1.3f, 45f);
            root.AddComponent<CheckpointRaceDemoController>().Configure(race, cube.transform, checkpoints);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root;
            Debug.Log("RAC-001: unsaved additive demo ready. Enter Play, press Start / Restart.");
        }
    }
}

