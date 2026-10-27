using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static class R6Build
{
    public static void BuildAll(){BuildRelease();BuildVisualQA();}
    public static void BuildVisualQA()
    {
        var options=new BuildPlayerOptions{scenes=new[]{"Assets/scene 1.unity"},locationPathName="Builds/R6-QA-Windows/RoboRougelike.exe",
            target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,extraScriptingDefines=new[]{"R6_VISUAL_QA"}};
        var report=BuildPipeline.BuildPlayer(options);UnityEngine.Debug.Log("[R6 QA BUILD] "+report.summary.result);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build R6 visual QA failed");
    }
    public static void BuildRelease()
    {
        var options=new BuildPlayerOptions{scenes=new[]{"Assets/scene 1.unity"},locationPathName="Builds/R6-Windows/RoboRougelike.exe",
            target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,extraScriptingDefines=Array.Empty<string>()};
        var report=BuildPipeline.BuildPlayer(options);
        UnityEngine.Debug.Log($"[R6 BUILD] {report.summary.result} bytes={report.summary.totalSize} errors={report.summary.totalErrors} warnings={report.summary.totalWarnings} time={report.summary.totalTime}");
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build R6 failed");
    }
}
