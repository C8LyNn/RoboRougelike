using System;
using System.IO;
using NUnit.Framework;
using RoboRougelike.MapEditor;
using RoboRougelike.R1;
using System.Collections;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class MapStorageIntegrityTests
{
    string dir;
    [SetUp] public void SetUp(){dir=Path.Combine(Path.GetTempPath(),"rrl-cx04-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);}
    [TearDown] public void TearDown(){if(Directory.Exists(dir))Directory.Delete(dir,true);}
    [TestCase(" Tor 1 ")][TestCase("CON")][TestCase("nul")][TestCase("COM1")][TestCase("Lpt9")]
    public void InvalidAndReservedNamesFailBeforeIO(string name)
    {Assert.That(MapStorage.TryPath(dir,name,out _),Is.False);Assert.That(Directory.GetFiles(dir),Is.Empty);}
    [Test] public void ExistingMapIsNotSilentlyOverwrittenAndCaseAliasIsRejected()
    {
        Assert.That(MapStorage.TryPath(dir,"Tor Case",out var path),Is.True);
        var a=new MapDraft();a.Place(MapItemKind.Ground,0,0,0);MapStorage.Save(path,a);var before=File.ReadAllBytes(path);
        var b=new MapDraft();b.Place(MapItemKind.Ground,1,0,0);
        Assert.That(()=>MapStorage.Save(path,b),Throws.InstanceOf<IOException>());
        Assert.That(File.ReadAllBytes(path),Is.EqualTo(before));
        Assert.That(MapStorage.TryPath(dir,"tor case",out _),Is.False);
        Assert.That(()=>MapStorage.Save(Path.Combine(dir,"tor case.json"),b,true),Throws.InstanceOf<IOException>());
        Assert.That(File.ReadAllBytes(path),Is.EqualTo(before));Assert.That(Directory.GetFiles(dir,"*.tmp"),Is.Empty);
    }
    [Test] public void NewValidMapRoundTripsWithoutAliases()
    {
        Assert.That(MapStorage.TryPath(dir,"Tor 1",out var path),Is.True);
        var a=new MapDraft();a.Place(MapItemKind.Ground,0,0,0);MapStorage.Save(path,a);
        var b=new MapDraft();Assert.That(MapStorage.Load(path,b,out var error),Is.True,error);Assert.That(b.Items.Count,Is.EqualTo(1));
    }
    [Test] public void ExplicitOverwriteReplacesOnlyTheConfirmedExactFile()
    {
        Assert.That(MapStorage.TryPath(dir,"Tor",out var path),Is.True);
        var a=new MapDraft();a.Place(MapItemKind.Ground,0,0,0);MapStorage.Save(path,a);
        var b=new MapDraft();b.Place(MapItemKind.Ground,1,0,0);MapStorage.Save(path,b,true);
        var loaded=new MapDraft();Assert.That(MapStorage.Load(path,loaded,out var error),Is.True,error);
        Assert.That(loaded.Items[0].x,Is.EqualTo(1));Assert.That(Directory.GetFiles(dir,"*.tmp"),Is.Empty);
    }
    [Test] public void LoadAlsoRejectsCaseAliasWithoutChangingDraft()
    {
        Assert.That(MapStorage.TryPath(dir,"Tor",out var path),Is.True);
        var saved=new MapDraft();saved.Place(MapItemKind.Ground,0,0,0);MapStorage.Save(path,saved);
        var current=new MapDraft();current.Place(MapItemKind.Ground,3,0,0);
        Assert.That(MapStorage.Load(Path.Combine(dir,"tor.json"),current,out _),Is.False);
        Assert.That(current.Items[0].x,Is.EqualTo(3));
    }
    [Test] public void NativeJsonV1LoadsAndSavesAsV2()
    {
        Assert.That(MapStorage.TryPath(dir,"Stara mapa",out var path),Is.True);
        File.WriteAllText(path,"{\"version\":1,\"items\":[{\"kind\":0,\"x\":3,\"z\":4,\"rotation\":0}]}");
        var draft=new MapDraft();Assert.That(MapStorage.Load(path,draft,out var error),Is.True,error);
        Assert.That(draft.Items[0].scaleX,Is.EqualTo(1));MapStorage.Save(path,draft,true);
        var reloaded=new MapDraft();Assert.That(MapStorage.Load(path,reloaded,out error),Is.True,error);
        Assert.That(reloaded.Items[0].x,Is.EqualTo(3));Assert.That(File.ReadAllText(path),Does.Contain("\"version\": 2"));
    }
    [UnityTest] public IEnumerator RealEditorRequiresExplicitSecondSaveForOverwrite()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        var until=Time.realtimeSinceStartup+15;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenMapEditor(),Is.True);
        run.EditorName="cx04test"+Guid.NewGuid().ToString("N");Assert.That(MapStorage.TryPath(MapStorage.DirectoryPath,run.EditorName,out var path),Is.True);
        try
        {
            Assert.That(run.EditorDraft.Place(MapItemKind.Ground,0,0,0),Is.True);run.SaveEditedMap();var before=File.ReadAllBytes(path);
            Assert.That(run.EditorDraft.Place(MapItemKind.Ground,1,0,0),Is.True);run.SaveEditedMap();
            Assert.That(run.MapOverwritePending,Is.True);Assert.That(File.ReadAllBytes(path),Is.EqualTo(before));
            run.SaveEditedMap();Assert.That(run.MapOverwritePending,Is.False);Assert.That(File.ReadAllBytes(path),Is.Not.EqualTo(before));
            var loaded=new MapDraft();Assert.That(MapStorage.Load(path,loaded,out var error),Is.True,error);Assert.That(loaded.Items.Count,Is.EqualTo(2));
        }
        finally{if(File.Exists(path))File.Delete(path);}
        yield return new ExitPlayMode();
    }
}
