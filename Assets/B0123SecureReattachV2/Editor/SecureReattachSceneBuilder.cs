using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace B01.SecureReattachV2.Editor
{
    public static class SecureReattachSceneBuilder
    {
        private const string Dir="Assets/B0123SecureReattachV2/Generated";
        private const string ScenePath=Dir+"/B0123SecureReattachV2.unity";

        [MenuItem("B01 / PASS17 / W10 Create guarded reattach V2 replacement scene")]
        public static void Create()
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null)
            {
                Debug.LogError("V2 scene exists; refusing overwrite: "+ScenePath);
                return;
            }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets/B0123SecureReattachV2","Generated");
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            GameObject camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));
            camera.tag="MainCamera";
            camera.transform.position=new Vector3(0,13,-17);
            camera.transform.rotation=Quaternion.Euler(36,0,0);
            Camera cam=camera.GetComponent<Camera>();
            cam.orthographic=true;
            cam.orthographicSize=7;
            cam.backgroundColor=new Color(.09f,.11f,.14f);
            cam.clearFlags=CameraClearFlags.SolidColor;
            GameObject sunlight=new GameObject("Directional Light",typeof(Light));
            sunlight.transform.rotation=Quaternion.Euler(40,-25,0);
            sunlight.GetComponent<Light>().type=LightType.Directional;
            GameObject floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name="Isolated safe demo floor";
            floor.transform.position=new Vector3(0,-.15f,0);
            floor.transform.localScale=new Vector3(20,.3f,9);
            GameObject controller=new GameObject("B01-23 SECURE REATTACH V2");
            controller.AddComponent<SecureDebrisPlaygroundV2>();
            // An EMPTY scene: never add legacy DebrisRigPlayground, W08, W09, or Codex.
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene,ScenePath))
                throw new System.InvalidOperationException("Could not save new isolated safe V2 scene");
            Debug.Log("Generated exclusive V2 scene. No legacy W07/W08/W09 components or canonical scenes mutated.");
        }
    }
}
