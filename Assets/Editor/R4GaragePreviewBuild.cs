using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class R4GaragePreviewBuild
{
    public static void Build()
    {
        const string output="Builds/R4-Cube-Preview-Windows/RoboRougelike.exe";
        if (Directory.Exists(Path.GetDirectoryName(output)))
            throw new InvalidOperationException("Refusing to overwrite an existing preview build directory.");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{"Assets/scene 1.unity"},locationPathName=output,
            target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,
            extraScriptingDefines=Array.Empty<string>()
        });
        UnityEngine.Debug.Log($"[R4 Garage Preview] {report.summary.result} bytes={report.summary.totalSize} errors={report.summary.totalErrors} warnings={report.summary.totalWarnings}");
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("R4 Garage preview build failed");
    }
}
