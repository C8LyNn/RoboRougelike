using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class R4ModelIntakeTests
{
    [System.Serializable] sealed class ClipInfo { public string name; public int start,end; public bool loop; }
    [System.Serializable] sealed class AnimationInfo { public int fps; public ClipInfo[] clips; }
    static string[] ModelPaths()=>Directory.GetFiles("Assets/Art/R4Intake","*.fbx",SearchOption.AllDirectories)
        .Select(p=>p.Replace('\\','/')).OrderBy(p=>p).ToArray();
    static string[] AnimatedModelPaths()=>ModelPaths().Where(p=>File.Exists(Path.ChangeExtension(p,null)+"_animation_manifest.json")).ToArray();
    [Test] public void AllFortyNineFbxModelsArePresentAndImported()
    {
        var paths=ModelPaths();Assert.That(paths.Length,Is.EqualTo(49));
        foreach(var path in paths)
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(path),Is.Not.Null,path);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.globalScale,Is.EqualTo(1).Within(.001),path);
            Assert.That(importer.useFileScale&&importer.bakeAxisConversion&&importer.preserveHierarchy,Is.True,path);
            Assert.That(importer.importCameras||importer.importLights||importer.addCollider,Is.False,path);
        }
        Assert.That(AnimatedModelPaths().Length,Is.EqualTo(19));
    }
    [TestCaseSource(nameof(ModelPaths))] public void MountSocketPositionInPrefabSpaceMatchesGrid(string path)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);Assert.That(prefab,Is.Not.Null,path);
        string name=Path.GetFileNameWithoutExtension(path);
        Find(prefab.transform,name+"_ROOT");
        string socketName=name.StartsWith("R4W_")?"Socket_Mount_Bottom":"Socket_Mount";
        float expectedY=name.StartsWith("R4M_")&&name!="R4M_Blink"||name=="R4B_Arch"?.26f:-.26f;
        var socket=Find(prefab.transform,socketName);
        var position=prefab.transform.InverseTransformPoint(socket.position);
        Assert.That(position.x,Is.EqualTo(0).Within(.001),path);
        Assert.That(position.y,Is.EqualTo(expectedY).Within(.001),path);
        Assert.That(position.z,Is.EqualTo(0).Within(.001),path);
    }
    [Test] public void BasicYawUsesTheUnityVerticalAxis()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/R4Intake/Weapons/R4W_Basic/R4W_Basic.fbx");
        Assert.That(prefab,Is.Not.Null);
        var yaw=Find(prefab.transform,"R4W_Basic_Yaw");
        var axis=prefab.transform.InverseTransformDirection(yaw.TransformDirection(Vector3.up));
        Assert.That(Vector3.Dot(axis,Vector3.up),Is.EqualTo(1).Within(.001),"Runtime local-Y yaw must be vertical.");
    }
    [TestCaseSource(nameof(AnimatedModelPaths))] public void AnimationClipsMatchSourceManifest(string path)
    {
        var manifest=JsonUtility.FromJson<AnimationInfo>(File.ReadAllText(Path.ChangeExtension(path,null)+"_animation_manifest.json"));
        Assert.That(manifest.fps,Is.EqualTo(24));
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        Assert.That(importer.clipAnimations.Length,Is.EqualTo(manifest.clips.Length),$"Clip count: {path}");
        foreach(var expected in manifest.clips)
        {
            var clip=importer.clipAnimations.Single(c=>c.name==expected.name);
            Assert.That(clip.firstFrame,Is.EqualTo(expected.start).Within(.001),path+" "+clip.name);
            Assert.That(clip.lastFrame,Is.EqualTo(expected.end).Within(.001),path+" "+clip.name);
            Assert.That(clip.loopTime,Is.EqualTo(expected.loop),path+" "+clip.name);
            var animation=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c=>c.name==expected.name);
            Assert.That(animation,Is.Not.Null,"Imported AnimationClip missing: "+path+" "+expected.name);
            Assert.That(animation.frameRate,Is.EqualTo(manifest.fps).Within(.001),"Actual FPS: "+path+" "+expected.name);
            Assert.That(animation.length,Is.EqualTo((expected.end-expected.start)/(float)manifest.fps).Within(.002),
                "Actual duration: "+path+" "+expected.name);
        }
    }
    [TestCase("Assets/Art/R4Intake/Weapons/R4W_Basic/R4W_Basic.fbx","Socket_Mount_Bottom")]
    [TestCase("Assets/Art/R4Intake/Movement/R4M_Wheel/R4M_Wheel.fbx","Socket_Mount")]
    [TestCase("Assets/Art/R4Intake/Blocks/R4B_Wedge/R4B_Wedge.fbx","Socket_Mount")]
    public void ReportImportedHierarchyAndMountPosition(string path,string socketName)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);Assert.That(prefab,Is.Not.Null);
        var socket=Find(prefab.transform,socketName);
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        var pos=prefab.transform.InverseTransformPoint(socket.position);
        Debug.Log($"R4_IMPORT_DIAGNOSTIC path={path} root={prefab.name} socket={pos.ToString("F6")} " +
            $"socketWorld={socket.position.ToString("F6")} rootRotation={prefab.transform.rotation.eulerAngles.ToString("F3")} " +
            $"scale={importer.globalScale} fileScale={importer.useFileScale} bake={importer.bakeAxisConversion} preserve={importer.preserveHierarchy} " +
            $"clips={string.Join(",",importer.defaultClipAnimations.Select(c=>$"{c.takeName}:{c.firstFrame}-{c.lastFrame}"))}");
        foreach(var node in prefab.GetComponentsInChildren<Transform>(true).Where(t=>t.name.EndsWith("_ROOT")||t.name=="Muzzle"||t.name=="GunPivot"||t.name.EndsWith("_Yaw")||t.name.EndsWith("_Pitch")))
            Debug.Log($"R4_NODE path={path} name={node.name} parent={node.parent?.name} localPosition={node.localPosition.ToString("F6")} localRotation={node.localRotation.eulerAngles.ToString("F3")} worldPosition={node.position.ToString("F6")} worldRotation={node.rotation.eulerAngles.ToString("F3")}");
    }
    static Transform Find(Transform root,string name)
    {
        var node=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
        Assert.That(node,Is.Not.Null,$"Missing FBX node {name}. Imported nodes: "+
            string.Join(", ",root.GetComponentsInChildren<Transform>(true).Select(t=>t.name)));return node;
    }
    [TestCase("Assets/Art/R4Intake/Weapons/R4W_Basic/R4W_Basic.fbx","R4W_Basic_ROOT","Socket_Mount_Bottom",-.26f)]
    [TestCase("Assets/Art/R4Intake/Movement/R4M_Wheel/R4M_Wheel.fbx","R4M_Wheel_ROOT","Socket_Mount",.26f)]
    public void ImportedSocketMatchesUnityMountingGrid(string path,string rootName,string socketName,float expectedY)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(prefab,Is.Not.Null,$"FBX must actually import: {path}");
        var root=Find(prefab.transform,rootName);var socket=Find(root,socketName);
        var position=root.InverseTransformPoint(socket.position);
        Assert.That(position.x,Is.EqualTo(0).Within(.001f));
        Assert.That(position.y,Is.EqualTo(expectedY).Within(.001f));
        Assert.That(position.z,Is.EqualTo(0).Within(.001f));
    }
    [Test] public void BasicWeaponHasForwardMuzzleAndSeparateAimNodes()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/R4Intake/Weapons/R4W_Basic/R4W_Basic.fbx");
        Assert.That(prefab,Is.Not.Null);
        var root=Find(prefab.transform,"R4W_Basic_ROOT");
        Assert.That(Find(root,"R4W_Basic_Yaw"),Is.Not.Null);
        Assert.That(Find(root,"R4W_Basic_Pitch"),Is.Not.Null);
        var muzzle=Find(root,"Muzzle");
        Assert.That(root.InverseTransformPoint(muzzle.position).z,Is.GreaterThan(0));
    }
}
