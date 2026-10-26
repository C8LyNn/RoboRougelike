using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static class FullMergeBuild
{
    public static void BuildRelease()
    {
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/scene 1.unity"},
            locationPathName="Builds/FULL-MERGE270/RoboRougelike.exe",target=BuildTarget.StandaloneWindows64,
            options=BuildOptions.None,extraScriptingDefines=new[]{"B01_UNIFIED_RC2_ACCEPTANCE"}});
        UnityEngine.Debug.Log($"[Full270 build] {report.summary.result} bytes={report.summary.totalSize} errors={report.summary.totalErrors} warnings={report.summary.totalWarnings}");
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Full270 Windows build failed");
    }
}
