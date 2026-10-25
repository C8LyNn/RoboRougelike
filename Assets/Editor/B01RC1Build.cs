using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class B01RC1Build
{
    public static void BuildAll(){Build(false);Build(true);}
    public static void BuildRelease() => Build(false);
    public static void BuildQA() => Build(true);
    static void Build(bool qa)
    {
        var options=new BuildPlayerOptions {
            scenes=new[]{"Assets/scene 1.unity"},
            locationPathName="Builds/B01-RC1-"+(qa?"QA":"MAPGEN-Experimental")+"/RoboRougelike.exe",
            target=BuildTarget.StandaloneWindows64,
            options=BuildOptions.None,
            extraScriptingDefines=qa?new[]{"MOVSERIES_VISUAL_QA","B01_RC1_QA"}:Array.Empty<string>()
        };
        var report=BuildPipeline.BuildPlayer(options);
        UnityEngine.Debug.Log($"[B01 RC1 BUILD] qa={qa} result={report.summary.result} bytes={report.summary.totalSize} errors={report.summary.totalErrors} warnings={report.summary.totalWarnings}");
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("RC1 Windows build failed");
    }
}
