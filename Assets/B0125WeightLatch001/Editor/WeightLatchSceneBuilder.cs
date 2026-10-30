#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace B0125.WeightLatch.Editor
{
    public static class WeightLatchSceneBuilder
    {
        public const string ScenePath="Assets/B0125WeightLatch001/Scenes/WEIGHTLATCH001_R1_Demo.unity";
        [MenuItem("B01/PASS17 W20/Create WEIGHTLATCH001 Standalone Demo")]
        public static void Create()
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null||
               System.IO.File.Exists(ScenePath))
                throw new InvalidOperationException("Will not overwrite "+ScenePath);
            if(!AssetDatabase.IsValidFolder("Assets/B0125WeightLatch001/Scenes"))
                AssetDatabase.CreateFolder("Assets/B0125WeightLatch001","Scenes");
            Scene before=SceneManager.GetActiveScene();
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var root=new GameObject("WEIGHTLATCH001 standalone weight gate");
                root.AddComponent<WeightLatchUnityDemo>();
                var cam=new GameObject("Diagnostic camera");
                cam.AddComponent<Camera>().fieldOfView=60;
                cam.transform.position=new Vector3(14,20,-15);
                cam.transform.LookAt(Vector3.zero);
                var light=new GameObject("Directional lighting");
                var sun=light.AddComponent<Light>();
                sun.type=LightType.Directional;sun.intensity=1.2f;
                light.transform.rotation=Quaternion.Euler(55,-30,0);
                if(!EditorSceneManager.SaveScene(scene,ScenePath))
                    throw new InvalidOperationException("Could not save original scene");
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
