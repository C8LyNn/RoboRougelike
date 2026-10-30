#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace B0125.Airlock.Editor
{
    public static class AirlockSceneBuilder
    {
        public const string ScenePath="Assets/B0125Airlock001/Scenes/AIRLOCK001_R1_Demo.unity";
        [MenuItem("B01/PASS17 W21/Create AIRLOCK001 Standalone Demo")]
        public static void Create()
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null||
               System.IO.File.Exists(ScenePath))
                throw new InvalidOperationException("AIRLOCK001 refuses to overwrite "+ScenePath);
            if(!AssetDatabase.IsValidFolder("Assets/B0125Airlock001/Scenes"))
                AssetDatabase.CreateFolder("Assets/B0125Airlock001","Scenes");
            Scene previous=SceneManager.GetActiveScene();
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var root=new GameObject("AIRLOCK001 standalone physical dual-door challenge");
                root.AddComponent<AirlockUnityDemo>();
                var camera=new GameObject("Original airlock diagnostic camera");
                camera.AddComponent<Camera>().fieldOfView=60;
                camera.transform.position=new Vector3(15,22,-15);
                camera.transform.LookAt(Vector3.zero);
                var sun=new GameObject("Directional light");
                var light=sun.AddComponent<Light>();
                light.type=LightType.Directional;light.intensity=1.1f;
                sun.transform.rotation=Quaternion.Euler(55,-35,0);
                if(!EditorSceneManager.SaveScene(scene,ScenePath))
                    throw new InvalidOperationException("Isolated scene creation failed");
            }
            finally
            {
                if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene,true);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
