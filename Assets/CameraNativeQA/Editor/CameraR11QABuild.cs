using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class CameraR11QABuild
{
    public static void Build()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/CameraNativeQA/Template.mat");
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,"Assets/CameraNativeQA/Template.mat");}
        var camera=new GameObject("Only CAMERA QA camera").AddComponent<Camera>();camera.tag="MainCamera";camera.farClipPlane=300;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.2f,.25f);
        var light=new GameObject("QA light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,-35,0);RenderSettings.ambientLight=Color.gray;
        new GameObject("QA runner only").AddComponent<CameraR11NativeQA>().Template=material;
        string scene="Assets/CameraNativeQA/CAMERA_R11_QA.unity";EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),scene);AssetDatabase.SaveAssets();System.IO.Directory.CreateDirectory("Builds/CAMERA-R11-QA");
        var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene},target=BuildTarget.StandaloneWindows64,locationPathName="Builds/CAMERA-R11-QA/CameraNativeQA.exe"});Debug.Log("[CAMERA QA BUILD] "+r.summary.result+" errors="+r.summary.totalErrors+" warnings="+r.summary.totalWarnings);
        if(r.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.InvalidOperationException("Camera QA build failed");
    }
}
