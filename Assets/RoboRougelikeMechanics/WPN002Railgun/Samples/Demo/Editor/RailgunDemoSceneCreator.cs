using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RoboRougelike.WPN002Railgun;

namespace RoboRougelike.WPN002Railgun.Demo.Editor
{
    public static class RailgunDemoSceneCreator
    {
        [MenuItem("RoboRougelike/WPN-002/Create Isolated Railgun Demo Scene")]
        public static void Create()
        {
            const string path = "Assets/RoboRougelikeMechanics/WPN002Railgun/Samples/Demo/Railgun_IsolatedDemo.unity";
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Ground"; floor.transform.position = new Vector3(0f, -0.75f, 12f);
            floor.transform.localScale = new Vector3(14f, 1f, 32f);
            GameObject car = GameObject.CreatePrimitive(PrimitiveType.Cube);
            car.name = "DemoOwner"; car.transform.position = new Vector3(0f, 0.55f, 0f);
            car.transform.localScale = new Vector3(1.8f, 0.6f, 2.6f);
            Rigidbody body = car.AddComponent<Rigidbody>(); body.mass = 250f; body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeAll;
            GameObject muzzle = new GameObject("RailgunMuzzle"); muzzle.transform.SetParent(car.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0f, 1.75f);
            var weapon = car.AddComponent<RailgunModule>();
            var sink = car.AddComponent<RailgunDemoShotSink>();
            var ammo = car.AddComponent<RailgunSimpleAmmo>();
            GameObject projectile = new GameObject("DemoProjectileTemplate_DISABLED");
            projectile.AddComponent<RailgunDemoProjectile>();
            projectile.SetActive(false);
            sink.SetPrefab(projectile.GetComponent<RailgunDemoProjectile>());
            weapon.Bind(body, muzzle.transform, car); weapon.BindSink(sink); weapon.BindResourceBank(ammo);
            for (int i = 0; i < 3; i++)
            {
                GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.name = "DemoBlock_" + i;
                target.transform.position = new Vector3(0f, 0.55f, 5f + i * 3f);
                target.transform.localScale = new Vector3(1.3f, 1.3f, 0.5f);
                RailgunDemoBlock block = target.AddComponent<RailgunDemoBlock>();
                block.Configure(80f, i == 2 ? 60f : 23f, true);
            }
            GameObject ui = new GameObject("DemoGUI");
            ui.AddComponent<RailgunDemoFireGUI>().Bind(weapon);
            Camera cam = Camera.main;
            if (cam == null) { GameObject c = new GameObject("Demo Camera"); cam = c.AddComponent<Camera>(); c.tag = "MainCamera"; }
            cam.transform.position = new Vector3(8f, 7f, -7f);
            cam.transform.rotation = Quaternion.LookRotation(new Vector3(-8f, -5.4f, 16f).normalized);
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();
            Debug.Log("Created isolated WPN-002 demo at " + path + ". Enter Play and click FIRE RAILGUN.");
        }
    }
}
