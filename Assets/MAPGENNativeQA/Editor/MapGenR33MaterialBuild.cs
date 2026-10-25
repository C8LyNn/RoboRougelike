using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MapGenR33MaterialBuild
{
    public static void Build()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Material QA Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.farClipPlane=400;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.18f,.24f);
        var light=new GameObject("Material QA Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(45,-35,0);
        RenderSettings.ambientLight=new Color(.5f,.5f,.5f);
        new GameObject("Material QA only").AddComponent<MapGenR33MaterialQA>();
        string scene="Assets/MAPGENNativeQA/MAPGEN_R33_MaterialQA.unity";
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),scene);AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Builds/MAPGEN-R33-MaterialQA");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene},target=BuildTarget.StandaloneWindows64,locationPathName="Builds/MAPGEN-R33-MaterialQA/MapgenMaterialQA.exe",options=BuildOptions.None});
        Debug.Log("[R33 MATERIAL BUILD] "+report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.InvalidOperationException("Material QA build failed");
    }
}
