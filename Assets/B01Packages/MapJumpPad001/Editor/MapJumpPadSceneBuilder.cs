#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace B01.MapJumpPad001.Editor
{
    public static class MapJumpPadSceneBuilder
    {
        private const string Root = "Assets/B01Packages/MapJumpPad001";
        [MenuItem("B01/Map Jump Pad 001/Generate Prefab And Demo Scene")]
        public static void Generate()
        {
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var pad = new GameObject("MapJumpPad001_Root_1x1m");
            var collider = pad.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = new Vector3(0f, 0.28f, 0f);
            collider.size = new Vector3(1f, 0.55f, 1f);
            pad.AddComponent<MapJumpPad>();
            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "VisualTop_NoGameplayAuthority";
            top.transform.SetParent(pad.transform, false);
            top.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            top.transform.localScale = new Vector3(1f, 0.2f, 1f);
            Object.DestroyImmediate(top.GetComponent<BoxCollider>());
            PrefabUtility.SaveAsPrefabAsset(pad, Root + "/Prefabs/MapJumpPad001.prefab");

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor_WithPadSlot";
            floor.transform.position = new Vector3(3f, -0.4f, 0f);
            floor.transform.localScale = new Vector3(5f, 0.4f, 5f);
            var robot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            robot.name = "HeavyRobot_Physics_10kg";
            robot.transform.position = new Vector3(0f, 2f, 0f);
            var rigid = robot.AddComponent<Rigidbody>();
            rigid.mass = 10f;
            rigid.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/MapJumpPad001_Demo.unity");
            AssetDatabase.Refresh();
        }
    }
}
#endif
