using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildR12DemoBuild
{
    public static void Build()
    {
        var scene=EditorSceneManager.OpenScene("Assets/B01Construction/BUILD001/Demo/BUILD001_Demo.unity",OpenSceneMode.Single);
        new GameObject("Receiver QA observer only").AddComponent<BuildR12DemoObserver>();
        string saved="Assets/BuildNativeQA/BUILD_R12_DemoQA.unity";EditorSceneManager.SaveScene(scene,saved);AssetDatabase.SaveAssets();
        System.IO.Directory.CreateDirectory("Builds/BUILD-R12-QA");
        var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{saved},target=BuildTarget.StandaloneWindows64,locationPathName="Builds/BUILD-R12-QA/BuildNativeQA.exe"});
        Debug.Log("[BUILD R12 QA BUILD] "+r.summary.result+" errors="+r.summary.totalErrors+" warnings="+r.summary.totalWarnings);
        if(r.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.InvalidOperationException("BUILD demo QA build failed");
    }
}
