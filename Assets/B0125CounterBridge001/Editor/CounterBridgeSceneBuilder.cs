#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace B0125.CounterBridge.Editor
{
    public static class CounterBridgeSceneBuilder
    {
        public const string Target="Assets/B0125CounterBridge001/Scenes/COUNTERBRIDGE001_R1_Demo.unity";
        [MenuItem("B01/PASS17 W19/Create COUNTERBRIDGE001 Standalone Demo")]
        public static void Create()
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Target)!=null||
               System.IO.File.Exists(Target))
                throw new InvalidOperationException("Will NOT overwrite scene "+Target);
            if(!AssetDatabase.IsValidFolder("Assets/B0125CounterBridge001/Scenes"))
                AssetDatabase.CreateFolder("Assets/B0125CounterBridge001","Scenes");
            Scene old=SceneManager.GetActiveScene();
            Scene fresh=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(fresh);
                var root=new GameObject("COUNTERBRIDGE001 standalone physical test");
                root.AddComponent<CounterBridgeUnityDemo>();
                var camera=new GameObject("Overhead diagnostic camera");
                camera.AddComponent<Camera>().fieldOfView=62;
                camera.transform.position=new Vector3(15,21,-14);
                camera.transform.LookAt(Vector3.zero);
                var light=new GameObject("Bridge directional light");
                var sun=light.AddComponent<Light>();
                sun.type=LightType.Directional;sun.intensity=1.1f;
                light.transform.rotation=Quaternion.Euler(55,-35,0);
                if(!EditorSceneManager.SaveScene(fresh,Target))
                    throw new InvalidOperationException("Saving isolated demo failed");
            }
            finally
            {
                if(old.IsValid()&&old.isLoaded)SceneManager.SetActiveScene(old);
                EditorSceneManager.CloseScene(fresh,true);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
