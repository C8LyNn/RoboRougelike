using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static class R4Build
{
    public static void BuildAll()
    {
        Build("Builds/R4-Windows/RoboRougelike.exe",false);
        Build("Builds/R4-QA-Windows/RoboRougelike.exe",true);
    }
    private static void Build(string path,bool qa)
    {
        var options=new BuildPlayerOptions{scenes=new[]{"Assets/scene 1.unity"},locationPathName=path,
            target=BuildTarget.StandaloneWindows64,options=qa?BuildOptions.Development:BuildOptions.None,
            extraScriptingDefines=qa?new[]{"R4_STANDALONE_QA"}:Array.Empty<string>()};
        var report=BuildPipeline.BuildPlayer(options);
        UnityEngine.Debug.Log("[R4 BUILD] "+path+" "+report.summary.result);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+path);
    }
}
