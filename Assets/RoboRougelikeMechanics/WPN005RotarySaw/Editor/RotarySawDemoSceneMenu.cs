#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoboRougelike.Weapons.RotarySaw.Editor
{
    public static class RotarySawDemoSceneMenu
    {
        [MenuItem("Tools/RoboRougelike/WPN-005/Create isolated Rotary Saw demo scene")]
        public static void CreateDemo()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            GameObject owner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            owner.name = "DEMO ONLY - vehicle";
            owner.transform.position = new Vector3(0f, 0.5f, -0.8f);
            owner.transform.localScale = new Vector3(0.6f, 0.5f, 0.75f);
            Rigidbody body = owner.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            GameObject cutter = new GameObject("WPN005 Rotary Saw");
            cutter.transform.SetParent(owner.transform);
            cutter.transform.localPosition = new Vector3(0f, 0f, 0.8f);
            RotarySawModule saw = cutter.AddComponent<RotarySawModule>();
            RotarySawDemoDamageSink sink = cutter.AddComponent<RotarySawDemoDamageSink>();
            cutter.AddComponent<RotarySawDemoDriver>();
            saw.Bind(body, owner.transform, cutter.transform);
            saw.SetDamageSink(sink);

            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "DEMO ONLY - destructible target";
            target.transform.position = cutter.transform.position + new Vector3(0f, 0f, 0.62f);
            target.transform.localScale = new Vector3(0.55f, 0.55f, 0.4f);
            target.AddComponent<RotarySawDemoTarget>();

            GameObject camera = new GameObject("Demo camera");
            Camera view = camera.AddComponent<Camera>();
            camera.transform.position = new Vector3(2.6f, 2.5f, -4.2f);
            camera.transform.LookAt(target.transform.position);
            view.clearFlags = CameraClearFlags.SolidColor;

            GameObject lighting = new GameObject("Demo light");
            Light light = lighting.AddComponent<Light>();
            light.type = LightType.Directional;
            lighting.transform.rotation = Quaternion.Euler(55f, -20f, 0f);

            string scenePath = "Assets/RoboRougelikeMechanics/WPN005RotarySaw/Demo/RotarySawDemo.unity";
            scenePath = AssetDatabase.GenerateUniqueAssetPath(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();
            Debug.Log("WPN-005 demo saved to " + scenePath + ". Press Play to start motor.");
        }
    }
}
#endif
