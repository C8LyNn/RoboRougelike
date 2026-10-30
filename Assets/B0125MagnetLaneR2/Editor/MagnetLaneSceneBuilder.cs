#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace B0125.MagnetLane.R2.Editor
{
    public static class MagnetLaneSceneBuilder
    {
        public const string Path="Assets/B0125MagnetLaneR2/Scenes/MAGNETLANE001_R2_Demo.unity";

        [MenuItem("B01/PASS17 W15/Create MAGNETLANE001 Standalone Demo")]
        public static void Create()
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Path)!=null||
               System.IO.File.Exists(Path))
                throw new InvalidOperationException("Existing scene not overwritten: "+Path);
            if(!AssetDatabase.IsValidFolder("Assets/B0125MagnetLaneR2/Scenes"))
                AssetDatabase.CreateFolder("Assets/B0125MagnetLaneR2","Scenes");
            Scene before=SceneManager.GetActiveScene();
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var root=new GameObject("MAGNETLANE001 isolated runtime");
                root.AddComponent<MagnetLaneUnityDemo>();
                var camera=new GameObject("MagnetLane Camera");
                camera.AddComponent<Camera>().fieldOfView=58;
                camera.transform.position=new Vector3(0,22,-19);
                camera.transform.LookAt(Vector3.zero);
                var lighting=new GameObject("MagnetLane Light");
                var sun=lighting.AddComponent<Light>();
                sun.type=LightType.Directional;sun.intensity=1.2f;
                lighting.transform.rotation=Quaternion.Euler(55,-25,0);
                if(!EditorSceneManager.SaveScene(scene,Path))
                    throw new InvalidOperationException("Cannot save new scene");
            }
            finally
            {
                if(before.IsValid()&&before.isLoaded)SceneManager.SetActiveScene(before);
                EditorSceneManager.CloseScene(scene,true);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
