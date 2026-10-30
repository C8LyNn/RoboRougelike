#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace B0125.SafeRoute.Editor
{
    public static class SafeRouteSceneFactory
    {
        public const string Target="Assets/B0125SafeRoute001/Scenes/SAFEROUTE001_R1_Demo.unity";
        [MenuItem("B01/PASS17 W23/Create SAFEROUTE001 Standalone Scenario")]
        public static void Create()
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Target)!=null||
               System.IO.File.Exists(Target))
                throw new InvalidOperationException("SAFEROUTE refuses scene overwrite");
            if(!AssetDatabase.IsValidFolder("Assets/B0125SafeRoute001/Scenes"))
                AssetDatabase.CreateFolder("Assets/B0125SafeRoute001","Scenes");
            Scene prior=SceneManager.GetActiveScene();
            Scene created=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(created);
                var root=new GameObject("SAFEROUTE001 physical level + deterministic guide");
                root.AddComponent<SafeRouteUnityDemo>();
                var cam=new GameObject("Safe route diagnostics camera");
                cam.AddComponent<Camera>().fieldOfView=55;
                cam.transform.position=new Vector3(19,29,-22);
                cam.transform.LookAt(Vector3.zero);
                var light=new GameObject("Safe route directional sunlight");
                light.AddComponent<Light>().type=LightType.Directional;
                light.transform.rotation=Quaternion.Euler(55,-35,0);
                if(!EditorSceneManager.SaveScene(created,Target))
                    throw new InvalidOperationException("Source Unity scene save rejected");
            }
            finally
            {
                if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);
                EditorSceneManager.CloseScene(created,true);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
