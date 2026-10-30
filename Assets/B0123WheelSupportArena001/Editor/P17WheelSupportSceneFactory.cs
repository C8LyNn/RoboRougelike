// Create a brand-new isolated scene using the real runtime builder. Never overwrite an existing scene.
#if UNITY_EDITOR
using System.IO;
using B01.P17WheelSupportArena001;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace B01.P17WheelSupportArena001.Editor
{
    public static class P17WheelSupportSceneFactory
    {
        [MenuItem("B01/P17 W09/Create Real Wheel Support Arena (New Scene)")]
        public static void Create()
        {
            const string folder="Assets/B0123WheelSupportArena001Generated";
            const string path=folder+"/P17_W09_REAL_WHEEL_SUPPORT_ARENA.unity";
            if(File.Exists(path))
            {
                Debug.LogWarning("B01-23 W09 refusal to overwrite "+path);
                return;
            }
            if(!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets","B0123WheelSupportArena001Generated");

            var scene=EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Single);
            var runner=new GameObject("P17 W09 actual 2-rig physics build + QA console");
            runner.AddComponent<P17WheelSupportArena>();
            var camera=Camera.main;
            if(camera!=null)
            {
                camera.transform.position=new Vector3(0,9,-16);
                camera.transform.rotation=Quaternion.Euler(26,0,0);
            }
            if(!EditorSceneManager.SaveScene(scene,path,false))
            {
                Debug.LogError("B01-23 W09 scene SAVE_FAILED "+path);
                return;
            }
            AssetDatabase.Refresh();
            Debug.Log("B01-23 W09 new scratch scene saved; press Play to instantiate real colliders. NATIVE_PHYSX_UNTESTED until actual run: "+path);
        }
    }
}
#endif
