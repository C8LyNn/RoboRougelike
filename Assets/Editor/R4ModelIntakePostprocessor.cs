using UnityEditor;

// QA-only import settings for the isolated R4 intake. Gameplay continues to use
// its existing colliders and prefabs until the assets pass native validation.
public sealed class R4ModelIntakePostprocessor : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/Art/R4Intake/", System.StringComparison.Ordinal)) return;
        var model = (ModelImporter)assetImporter;
        model.globalScale = 1f;
        model.useFileScale = true;
        model.bakeAxisConversion = true;
        model.importCameras = false;
        model.importLights = false;
        model.addCollider = false;
        model.optimizeGameObjects = false;
        model.preserveHierarchy = true;
    }
}

public static class R4ModelIntakeReimport
{
    [System.Serializable] sealed class ClipInfo { public string name; public int start,end; public bool loop; }
    [System.Serializable] sealed class AnimationInfo { public int fps; public ClipInfo[] clips; }
    public static void Samples()
    {
        foreach(var path in new[]{"Assets/Art/R4Intake/Weapons/R4W_Basic/R4W_Basic.fbx",
            "Assets/Art/R4Intake/Movement/R4M_Wheel/R4M_Wheel.fbx",
            "Assets/Art/R4Intake/Blocks/R4B_Wedge/R4B_Wedge.fbx"})
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
    }
    public static void SplitAnimationClips()
    {
        int configured=0;
        foreach(var path in System.IO.Directory.GetFiles("Assets/Art/R4Intake","*.fbx",System.IO.SearchOption.AllDirectories))
        {
            string asset=path.Replace('\\','/');
            string manifestPath=System.IO.Path.ChangeExtension(path,null)+"_animation_manifest.json";
            if(!System.IO.File.Exists(manifestPath))continue;
            var data=UnityEngine.JsonUtility.FromJson<AnimationInfo>(System.IO.File.ReadAllText(manifestPath));
            var importer=(ModelImporter)AssetImporter.GetAtPath(asset);
            var takes=importer.defaultClipAnimations;
            if(data==null||data.fps!=24||data.clips==null||data.clips.Length==0||takes.Length!=1)
                throw new System.InvalidOperationException("Unexpected animation manifest or FBX takes: "+asset);
            var take=takes[0];var clips=new ModelImporterClipAnimation[data.clips.Length];
            var names=new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
            for(int i=0;i<clips.Length;i++)
            {
                var source=data.clips[i];
                if(string.IsNullOrWhiteSpace(source.name)||!names.Add(source.name)||source.start<take.firstFrame||
                    source.end>take.lastFrame||source.end<source.start)
                    throw new System.InvalidOperationException("Invalid clip range or name in "+manifestPath);
                clips[i]=new ModelImporterClipAnimation{name=source.name,takeName=take.takeName,
                    firstFrame=source.start,lastFrame=source.end,loopTime=source.loop};
            }
            importer.clipAnimations=clips;importer.SaveAndReimport();configured++;
            UnityEngine.Debug.Log("R4_INTAKE_SPLIT "+asset+" clips="+clips.Length);
        }
        if(configured!=19)throw new System.InvalidOperationException("Expected 19 animated models, found "+configured);
        UnityEngine.Debug.Log("R4_INTAKE_SPLIT_COMPLETE "+configured);
    }
}
