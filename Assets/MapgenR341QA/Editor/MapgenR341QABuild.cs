using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MapgenR341QABuild
{
    static Camera Scene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Only R341 QA camera").AddComponent<Camera>();camera.tag="MainCamera";camera.farClipPlane=400;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.18f,.24f);
        var light=new GameObject("QA light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(45,-35,0);RenderSettings.ambientLight=new Color(.5f,.5f,.5f);return camera;
    }
    static void Build(string label)
    {
        string scene="Assets/MapgenR341QA/"+label+".unity";EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),scene);AssetDatabase.SaveAssets();
        string dir="Builds/MAPGEN-R341-"+label;Directory.CreateDirectory(dir);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene},target=BuildTarget.StandaloneWindows64,locationPathName=dir+"/MapgenQA.exe"});
        Debug.Log("[R341 QA BUILD] "+label+" "+report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.InvalidOperationException("QA build failed: "+label);
    }
    public static void Materials(){Scene();new GameObject("Minimal original map host").AddComponent<MapgenR341MaterialsQA>();Build("Materials");}
    public static void Wheels()
    {
        Scene();string path="Assets/MapgenR341QA/VehicleTemplate.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
        new GameObject("Real R3 R6 diagnostic").AddComponent<MapgenR341WheelsQA>().VehicleTemplate=material;Build("Wheels");
    }
}
