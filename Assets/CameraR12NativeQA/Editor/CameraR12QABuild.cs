using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class CameraR12QABuild
{
    public static void Build()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/CameraR12NativeQA/Template.mat");
        if(material==null)throw new System.InvalidOperationException("Missing frozen QA URP material");
        var camera=new GameObject("Only CAMERA R12 QA camera").AddComponent<Camera>();camera.tag="MainCamera";camera.farClipPlane=300;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.2f,.25f);
        var light=new GameObject("QA light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,-35,0);RenderSettings.ambientLight=Color.gray;
        new GameObject("Isolated QA fixture").AddComponent<CameraR12NativeQA>().Template=material;
        string scene="Assets/CameraR12NativeQA/CAMERA_R12_QA.unity";EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),scene);AssetDatabase.SaveAssets();System.IO.Directory.CreateDirectory("Builds/CAMERA-R12-QA");
        var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene},target=BuildTarget.StandaloneWindows64,locationPathName="Builds/CAMERA-R12-QA/CameraR12QA.exe"});
        Debug.Log("[CAMERA R12 QA BUILD] "+r.summary.result+" errors="+r.summary.totalErrors+" warnings="+r.summary.totalWarnings);
        if(r.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.InvalidOperationException("CAMERA QA build failed");
    }
}
