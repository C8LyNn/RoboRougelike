#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RoboRougelike.Mechanics.Rockets;
using RoboRougelike.Mechanics.Rockets.Demo;

namespace RoboRougelike.Mechanics.Rockets.Editor
{
    public static class RocketDemoSceneBuilder
    {
        [MenuItem("Tools/RoboRougelike/WPN-004/Create Rocket Demo (new scene)")]
        private static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            string folder = "Assets/RoboRougelikeMechanics/WPN004RocketLauncher/Samples/Generated";
            string root = "Assets/RoboRougelikeMechanics/WPN004RocketLauncher/Samples";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(root, "Generated");
            string prefabPath = folder + "/DemoRocket.prefab";
            var rocket = new GameObject("WPN004_DemoRocket_Prefab");
            var sphere = rocket.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.12f;
            var rigid = rocket.AddComponent<Rigidbody>();
            rigid.isKinematic = true;
            rigid.useGravity = false;
            rocket.AddComponent<RocketProjectile>();
            GameObject rocketAsset = PrefabUtility.SaveAsPrefabAsset(rocket, prefabPath);
            Object.DestroyImmediate(rocket);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0, -1f, 15f);
            ground.transform.localScale = new Vector3(22, 1f, 42);
            for (int i = 0; i < 3; i++)
            {
                GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.name = "Target_Block_" + i;
                target.transform.position = new Vector3((i - 1) * 2.5f, 0f, 15f);
                target.transform.localScale = Vector3.one * 1.3f;
                target.AddComponent<DemoRocketBlock>();
            }

            GameObject tank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tank.name = "DemoTank_RigidBody";
            tank.transform.position = new Vector3(0f, 0f, 0f);
            tank.transform.localScale = new Vector3(1.6f, 1f, 2.5f);
            Rigidbody body = tank.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            var launcherObj = new GameObject("RocketLauncher_WeaponModule");
            launcherObj.transform.SetParent(tank.transform, false);
            launcherObj.transform.localPosition = new Vector3(0f, 0.6f, 1.2f);
            var muzzle = new GameObject("Muzzle_ForwardZ").transform;
            muzzle.SetParent(launcherObj.transform, false);
            muzzle.localPosition = new Vector3(0f, 0f, 0.5f);
            RocketLauncher launcher = launcherObj.AddComponent<RocketLauncher>();
            launcher.Bind(body, tank.transform, muzzle);
            launcher.SetPrefab(rocketAsset.GetComponent<RocketProjectile>());
            RocketAmmoMagazine ammo = tank.AddComponent<RocketAmmoMagazine>();
            launcher.SetAmmoSource(ammo);
            var gui = tank.AddComponent<DemoRocketUI>();
            gui.Bind(launcher, ammo);

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 13f, -10f);
                camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, 11f) - camera.transform.position);
            }
            EditorSceneManager.SaveScene(scene, folder + "/WPN004_Demo.unity");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = tank;
            Debug.Log("WPN-004: demo saved. Play scene and press FIRE ROCKET in GUI.");
        }
    }
}
#endif
