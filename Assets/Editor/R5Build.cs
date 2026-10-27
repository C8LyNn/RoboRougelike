using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static class R5Build
{
    public static void BuildAll(){Build("Builds/R5-Windows/RoboRougelike.exe",false);Build("Builds/R5-QA-Windows/RoboRougelike.exe",true);}
    private static void Build(string path,bool qa)
    {
        var options=new BuildPlayerOptions{scenes=new[]{"Assets/scene 1.unity"},locationPathName=path,target=BuildTarget.StandaloneWindows64,
            options=qa?BuildOptions.Development:BuildOptions.None,extraScriptingDefines=qa?new[]{"R5_STANDALONE_QA"}:Array.Empty<string>()};
        var report=BuildPipeline.BuildPlayer(options);UnityEngine.Debug.Log("[R5 BUILD] "+path+" "+report.summary.result+" bytes="+report.summary.totalSize+" time="+report.summary.totalTime);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+path);
    }
}
