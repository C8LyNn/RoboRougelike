using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class B01RC2Build
{
    public static void BuildRelease()
    {
        // One application/scene. Acceptance hooks remain dormant without their
        // explicit diagnostic command-line argument; no alternate final games.
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{"Assets/scene 1.unity"},
            locationPathName="Builds/B01-UNIFIED-RC2/RoboRougelike.exe",
            target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,
            extraScriptingDefines=new[]{"B01_UNIFIED_RC2_ACCEPTANCE"}
        });
        UnityEngine.Debug.Log($"[B01 UNIFIED RC2 BUILD] {report.summary.result} bytes={report.summary.totalSize} errors={report.summary.totalErrors} warnings={report.summary.totalWarnings}");
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Unified RC2 Windows build failed");
    }
}
