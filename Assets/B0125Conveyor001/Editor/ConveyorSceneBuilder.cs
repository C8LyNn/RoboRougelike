#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace B0125.Conveyor.Editor
{
    public static class ConveyorSceneBuilder
    {
        public const string ScenePath="Assets/B0125Conveyor001/Scenes/CONVEYOR001_R1_Demo.unity";
        [MenuItem("B01/PASS17 W22/Create CONVEYOR001 Standalone Demo")]
        public static void Create()
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null||
               System.IO.File.Exists(ScenePath))
                throw new InvalidOperationException("Refusing scene overwrite "+ScenePath);
            if(!AssetDatabase.IsValidFolder("Assets/B0125Conveyor001/Scenes"))
                AssetDatabase.CreateFolder("Assets/B0125Conveyor001","Scenes");
            Scene old=SceneManager.GetActiveScene();
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var root=new GameObject("CONVEYOR001 actual physical challenge");
                root.AddComponent<ConveyorUnityDemo>();
                var eye=new GameObject("Belt diagnostic camera");
                eye.AddComponent<Camera>().fieldOfView=60;
                eye.transform.position=new Vector3(15,22,-15);
                eye.transform.LookAt(Vector3.zero);
                var sun=new GameObject("Belt directional sunlight");
                var light=sun.AddComponent<Light>();
                light.type=LightType.Directional;light.intensity=1.1f;
                sun.transform.rotation=Quaternion.Euler(55,-35,0);
                if(!EditorSceneManager.SaveScene(scene,ScenePath))
                    throw new InvalidOperationException("Scene save failed");
            }
            finally
            {
                if(old.IsValid()&&old.isLoaded)SceneManager.SetActiveScene(old);
                EditorSceneManager.CloseScene(scene,true);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
