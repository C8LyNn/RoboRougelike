#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace B0125.SinkPlate.R2.Editor
{
    public static class SinkPlateSceneBuilder
    {
        public const string GeneratedScene="Assets/B0125SinkPlateR2/Scenes/SINKPLATE001_R2_Demo.unity";
        [MenuItem("B01/PASS17 W13/Create SINKPLATE001 Standalone Demo")]
        public static void Create()
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(GeneratedScene)!=null||
               System.IO.File.Exists(GeneratedScene))
                throw new InvalidOperationException("Refuse to overwrite scene "+GeneratedScene);
            if(!AssetDatabase.IsValidFolder("Assets/B0125SinkPlateR2/Scenes"))
                AssetDatabase.CreateFolder("Assets/B0125SinkPlateR2","Scenes");
            Scene prior=SceneManager.GetActiveScene();
            Scene isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(isolated);
                var root=new GameObject("SINKPLATE001 Owned Standalone Demo");
                root.AddComponent<SinkPlateUnityDemo>();
                var camera=new GameObject("SinkPlate Camera");
                var component=camera.AddComponent<Camera>();
                camera.transform.position=new Vector3(0,24,-18);
                camera.transform.LookAt(new Vector3(0,0,1));
                component.fieldOfView=55;
                var sun=new GameObject("SinkPlate Sun");
                sun.AddComponent<Light>().type=LightType.Directional;
                sun.transform.rotation=Quaternion.Euler(55,-35,0);
                if(!EditorSceneManager.SaveScene(isolated,GeneratedScene))
                    throw new InvalidOperationException("Failed to persist standalone scene");
            }
            finally
            {
                if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);
                EditorSceneManager.CloseScene(isolated,true);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
