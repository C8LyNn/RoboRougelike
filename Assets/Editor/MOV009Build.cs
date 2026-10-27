using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class MOV009Build
{
    public static void BuildAll(){Build(false);Build(true);}
    static void Build(bool qa)
    {
        var options=new BuildPlayerOptions{
            scenes=new[]{"Assets/scene 1.unity"},
            locationPathName=qa?"Builds/MOV009-QA-Windows/RoboRougelike.exe":"Builds/MOV009-Windows/RoboRougelike.exe",
            target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,
            extraScriptingDefines=qa?new[]{"MOV009_VISUAL_QA"}:Array.Empty<string>()};
        var report=BuildPipeline.BuildPlayer(options);
        UnityEngine.Debug.Log($"[MOV009 BUILD] qa={qa} result={report.summary.result} bytes={report.summary.totalSize} errors={report.summary.totalErrors} warnings={report.summary.totalWarnings}");
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("MOV009 build failed");
    }
}
