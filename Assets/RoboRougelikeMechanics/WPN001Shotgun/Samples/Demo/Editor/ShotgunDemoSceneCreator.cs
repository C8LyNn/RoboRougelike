#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RoboRougelike.WPN001Shotgun.Demo;

namespace RoboRougelike.WPN001Shotgun.Editor
{
    public static class ShotgunDemoSceneCreator
    {
        [MenuItem("Tools/RoboRougelike/WPN-001/Create isolated shotgun demo scene")]
        public static void CreateScene()
        {
            if (!EditorUtility.DisplayDialog("WPN-001 isolated demo",
                "Create and open a NEW demo scene? Save current scene changes first. No R6 scenes will be overwritten.",
                "Create", "Cancel")) return;
            Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Directional Light");
            var sun = light.AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(35f, -25f, 0f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Floor"; ground.transform.position = new Vector3(0f, -0.6f, 7f);
            ground.transform.localScale = new Vector3(30f, 1f, 35f);

            GameObject vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vehicle.name = "DEMO vehicle"; vehicle.transform.position = new Vector3(0f, 0.65f, 0f);
            vehicle.transform.localScale = new Vector3(1.4f, 0.6f, 2f);
            Rigidbody body = vehicle.AddComponent<Rigidbody>();
            body.mass = 200f; body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeAll;

            GameObject muzzle = new GameObject("Shotgun muzzle (+Z)");
            muzzle.transform.SetParent(vehicle.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0.12f, 0.73f);

            GameObject weapon = new GameObject("WPN001 Shotgun module");
            weapon.transform.SetParent(vehicle.transform, false);
            ShotgunModule module = weapon.AddComponent<ShotgunModule>();
            var emitter = weapon.AddComponent<ShotgunDemoVolleySink>();
            module.Bind(body, muzzle.transform, vehicle);
            module.BindSink(emitter);

            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "DEMO breakable target";
            target.transform.position = new Vector3(0f, 0.7f, 9f);
            target.transform.localScale = new Vector3(3f, 2f, 0.5f);
            target.AddComponent<ShotgunDemoTargetHealth>().Configure(1000f);

            GameObject input = new GameObject("DEMO OnGUI Fire Button");
            input.AddComponent<ShotgunDemoFireButton>().Bind(module);

            GameObject cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            Camera camera = cam.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 8f, -13f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.7f, 5f) - camera.transform.position);

            const string folder = "Assets/RoboRougelikeMechanics/WPN001Shotgun/Samples/Demo/Scenes";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/RoboRougelikeMechanics/WPN001Shotgun/Samples/Demo", "Scenes");
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/WPN001_Demo.unity");
            EditorSceneManager.SaveScene(s, path);
            Debug.Log("WPN-001 demo scene created: " + path);
        }
    }
}
#endif
