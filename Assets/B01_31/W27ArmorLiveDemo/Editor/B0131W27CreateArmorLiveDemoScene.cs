#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RogueMachine.P12PipelineSafe;
using Object = UnityEngine.Object;

namespace RoboRougelike.B0131.Art.Editor
{
    /// <summary>Manual only: builds a playable real P12->W25->W24 armor demo .unity scene.
    /// Never edits original P12, W24, W25, current player data or Codex paths.</summary>
    public static class B0131W27CreateArmorLiveDemoScene
    {
        private const string W24Prefab =
            "Assets/B01_31/W24ArmorStates/Generated/B0131_W24_ArmorVisual_ONLY.prefab";
        private const string W27Folder = "Assets/B01_31/W27ArmorLiveDemo";
        private const string Output = W27Folder + "/Generated/B0131_W27_P12_ArmorLiveDemo_ONLY.unity";

        [MenuItem("Tools/B01-31/W27/Create Isolated P12 Armor Live Demo Scene...")]
        public static void CreateWithExplicitConsent()
        {
            if (!EditorUtility.DisplayDialog("B01-31 / W27 — actual P12 armor sandbox",
                "Only in a disposable/approved Unity project. Creates one NEW isolated " +
                "demo scene from the existing W24 3-OBJ visual prefab, W25 binder, " +
                "and real original P12 Damageable. Requires prior W24 and W25 import. " +
                "Current scenes may be affected by Editor play state; no merge or Save writes. " +
                "Never overwrites an existing scene. Proceed?", "Create demo", "Cancel"))
                return;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[B01-31 W27] Refuse while entering or running Play mode.");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Output) != null ||
                System.IO.File.Exists(Output) ||
                System.IO.File.Exists(Output + ".meta"))
            {
                Debug.LogError("[B01-31 W27] Refuse existing/foreign scene at " + Output);
                return;
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(W24Prefab);
            if (prefab == null ||
                prefab.GetComponent<B0131W24ArmorDamageVisual>() == null)
            {
                Debug.LogError("[B01-31 W27] W24 visual prefab missing. " +
                    "Manually approve W24 model import/prefab before generating W27.");
                return;
            }
            if (!AssetDatabase.IsValidFolder(W27Folder))
            {
                Debug.LogError("[B01-31 W27] W27 owning folder missing; no changes made.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[B01-31 W27] User cancelled modified scene handling.");
                return;
            }

            // Additive new scene: do not replace an existing scene or switch editor
            // working scene and never touch current original game hierarchy.
            var previous = SceneManager.GetActiveScene();
            Scene created = default;
            bool sceneCreated = false;
            try
            {
                created = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                sceneCreated = true;
                SceneManager.SetActiveScene(created);
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, created);
                if (visual == null) throw new InvalidOperationException("W24 prefab instantiate failed");
                visual.name = "B0131_W27_THREE_STATE_ARMOR_VISUAL";
                visual.transform.position = new Vector3(0f, 1.4f, 0f);
                var visualController = visual.GetComponent<B0131W24ArmorDamageVisual>();
                if (visualController == null) throw new InvalidOperationException("W24 visual script absent");
                var binder = visual.AddComponent<B0131W25P12DamageVisualBinding>();

                // Source is a deliberately SEPARATE GameObject so P12's genuine
                // Destroy(gameObject) after death does not delete the visual display.
                var livePart = new GameObject("B0131_W27_REAL_P12_TEST_COMPONENT");
                SceneManager.MoveGameObjectToScene(livePart, created);
                livePart.transform.position = new Vector3(0f, 0.2f, 0f);
                var hitbox = livePart.AddComponent<BoxCollider>();
                hitbox.isTrigger = true;
                var damageable = livePart.AddComponent<P12PipelineSafeDamageable>();
                damageable.Configure(100f, false);
                if (!binder.Bind(damageable, visualController))
                    throw new InvalidOperationException("Actual P12 to W24 W25 bind failed");

                var demo = visual.AddComponent<B0131W27ArmorLiveDemoDriver>();
                if (!demo.BindDemo(damageable, binder))
                    throw new InvalidOperationException("Demo P12 drive binding failed");

                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "B0131_W27_VISUAL_ONLY_FLOOR";
                SceneManager.MoveGameObjectToScene(floor, created);
                floor.transform.position = new Vector3(0f, -0.25f, 0f);
                floor.transform.localScale = new Vector3(8f, 0.5f, 8f);

                var cam = new GameObject("B0131_W27_DEMO_CAMERA");
                SceneManager.MoveGameObjectToScene(cam, created);
                cam.transform.position = new Vector3(5f, 4f, -7f);
                cam.transform.LookAt(visual.transform.position);
                var camera = cam.AddComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 3.8f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.07f, 0.10f, 0.16f);

                var lightObject = new GameObject("B0131_W27_DEMO_LIGHT");
                SceneManager.MoveGameObjectToScene(lightObject, created);
                lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.2f;

                if (!AssetDatabase.IsValidFolder(W27Folder + "/Generated"))
                    AssetDatabase.CreateFolder(W27Folder, "Generated");

                if (System.IO.File.Exists(Output) ||
                    !EditorSceneManager.SaveScene(created, Output, false))
                    throw new InvalidOperationException("Scene output collision / SaveScene failed");
                Debug.Log("[B01-31 W27] REAL P12/W24/W25 isolated scene saved: " +
                    Output + ". OPEN MANUALLY for Play Mode / GUI tests.");
            }
            catch (Exception e)
            {
                // Fail closed. No best-effort deletes: an incomplete scene needs
                // explicit human inspection and preserved evidence.
                Debug.LogError("[B01-31 W27] Scene construction failed (manual review): " + e);
                throw;
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
                if (sceneCreated && created.IsValid() && created.isLoaded)
                    EditorSceneManager.CloseScene(created, true);
            }
        }
    }
}
#endif
