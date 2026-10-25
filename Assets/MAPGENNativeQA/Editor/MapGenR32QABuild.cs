using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MapGenR32QABuild
{
    public static void Build()
    {
        string folder="Assets/MAPGENNativeQA";
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)throw new System.InvalidOperationException("Missing existing URP shader");
        string materialPath=folder+"/VehicleTemplate.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}
        var camera=new GameObject("Only QA camera").AddComponent<Camera>();camera.tag="MainCamera";camera.farClipPlane=300;
        camera.backgroundColor=new Color(.13f,.18f,.24f);camera.clearFlags=CameraClearFlags.SolidColor;
        var light=new GameObject("QA light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(45,-35,0);
        RenderSettings.ambientLight=new Color(.5f,.5f,.5f);
        new GameObject("Diagnostic runner only").AddComponent<MapGenR32WheelQA>().VehicleTemplate=material;
        string path=folder+"/MAPGEN_R32_WheelQA.unity";EditorSceneManager.SaveScene(scene,path);AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Builds/MAPGEN-R32-QA");
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{path},locationPathName="Builds/MAPGEN-R32-QA/MapgenNativeQA.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        Debug.Log("[MAPGEN QA BUILD] result="+result.summary.result+" errors="+result.summary.totalErrors+" warnings="+result.summary.totalWarnings);
        if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.InvalidOperationException("QA standalone build failed");
    }
}
