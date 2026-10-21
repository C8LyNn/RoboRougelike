#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoboRougelike.VFX001.Editor
{
    public static class Vfx001DemoSceneBuilder
    {
        private const string Folder = "Assets/RoboRougelikePackages/VFX001/Demo";
        [MenuItem("Tools/RoboRougelike/VFX001/Build Demo Scene")]
        public static void Build()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) return;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraGO = new GameObject("VFX Demo Camera", typeof(Camera));
            cameraGO.tag = "MainCamera";
            var camera = cameraGO.GetComponent<Camera>();
            cameraGO.transform.position = new Vector3(8f, 8f, -12f);
            cameraGO.transform.LookAt(new Vector3(0f, 1.1f, 0f));
            camera.fieldOfView = 46f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.08f, .10f, .14f);

            var sunGO = new GameObject("Demo Main Light", typeof(Light));
            sunGO.transform.rotation = Quaternion.Euler(50f, -28f, 0f);
            sunGO.GetComponent<Light>().type = LightType.Directional;
            sunGO.GetComponent<Light>().intensity = 1.65f;

            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Showcase Floor";
            plane.transform.localScale = new Vector3(1.5f, 1f, 1.5f);
            plane.transform.position = new Vector3(0f, 0f, 0f);

            var showcase = new GameObject("VFX001 Showcase Controller");
            showcase.AddComponent<Vfx001Showcase>();
            EditorSceneManager.SaveScene(scene, Folder + "/VFX001_Showcase.unity");
            AssetDatabase.Refresh();
            Debug.Log("VFX001: demo scene saved at " + Folder + "/VFX001_Showcase.unity");
        }
    }
}
#endif
