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
    [Test] public void SavedMapListShowsOnlyLoadableNamesWithoutChangingFiles()
    {
        var list=typeof(MapStorage).GetMethod("ListSavedNames");Assert.That(list,Is.Not.Null);
        Assert.That((string[])list.Invoke(null,new object[]{Path.Combine(dir,"missing")}),Is.Empty);
        var draft=new MapDraft();draft.Place(MapItemKind.Ground,0,0,0);
        foreach(string name in new[]{"Zeta","Alfa 1"})
        {Assert.That(MapStorage.TryPath(dir,name,out var path),Is.True);MapStorage.Save(path,draft);}
        File.WriteAllText(Path.Combine(dir,"niepoprawna!.json"),"{}");
        File.WriteAllText(Path.Combine(dir,"inne.txt"),"{}");
        var before=Directory.GetFiles(dir);
        var names=(string[])list.Invoke(null,new object[]{dir});
        Assert.That(names,Is.EqualTo(new[]{"Alfa 1","Zeta"}));
        foreach(var name in names)
        {Assert.That(MapStorage.TryPath(dir,name,out var path),Is.True);Assert.That(MapStorage.Load(path,new MapDraft(),out _),Is.True);}
        Assert.That(Directory.GetFiles(dir),Is.EquivalentTo(before));
    }
    [UnityTest] public IEnumerator SelectingSavedMapPreservesUnsavedChangesUntilConfirmed()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        var until=Time.realtimeSinceStartup+15;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenMapEditor(),Is.True);
        string name="browser"+Guid.NewGuid().ToString("N");Assert.That(MapStorage.TryPath(MapStorage.DirectoryPath,name,out var path),Is.True);
        var saved=new MapDraft();saved.Place(MapItemKind.Ramp,2,2,0);MapStorage.Save(path,saved);
        try
        {
            Assert.That(run.EditorDraft.Place(MapItemKind.Ground,0,0,0),Is.True);
            string before=run.EditorDraft.ToJson();var hud=run.GetComponent<R1RunHUD>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var open=typeof(R1RunHUD).GetMethod("OpenMapBrowser",flags);var select=typeof(R1RunHUD).GetMethod("LoadMapFromBrowser",flags);
            Assert.That(open,Is.Not.Null);Assert.That(select,Is.Not.Null);
            open.Invoke(hud,new object[]{run});Assert.That(run.EditorTextFocused,Is.True);
            run.HandleEscape();Assert.That(run.Paused,Is.False);Assert.That(run.MapEditing,Is.True);Assert.That(run.EditorTextFocused,Is.False);
            yield return null;
            open.Invoke(hud,new object[]{run});select.Invoke(hud,new object[]{run,name});
            Assert.That(run.MapLoadPending,Is.True);Assert.That(run.EditorDraft.ToJson(),Is.EqualTo(before));
            run.LoadEditedMap();Assert.That(run.MapLoadPending,Is.False);
            Assert.That(run.EditorDraft.ToJson(),Is.EqualTo(saved.ToJson()));
        }
        finally{if(File.Exists(path))File.Delete(path);}
        yield return new ExitPlayMode();
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
            // A changed draft must not reuse the prior overwrite confirmation.
            Assert.That(run.EditorDraft.Place(MapItemKind.Ground,2,0,0),Is.True);run.SaveEditedMap();
            Assert.That(run.MapOverwritePending,Is.True);Assert.That(File.ReadAllBytes(path),Is.EqualTo(before));
            run.SaveEditedMap();Assert.That(run.MapOverwritePending,Is.False);Assert.That(File.ReadAllBytes(path),Is.Not.EqualTo(before));
            var loaded=new MapDraft();Assert.That(MapStorage.Load(path,loaded,out var error),Is.True,error);Assert.That(loaded.Items.Count,Is.EqualTo(3));
        }
        finally{if(File.Exists(path))File.Delete(path);}
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator LoadingAStoredMapRequiresConfirmationForUnsavedChanges()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        var until=Time.realtimeSinceStartup+15;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenMapEditor(),Is.True);
        run.EditorName="loadconfirm"+Guid.NewGuid().ToString("N");Assert.That(MapStorage.TryPath(MapStorage.DirectoryPath,run.EditorName,out var path),Is.True);
        try
        {
            Assert.That(run.EditorDraft.Place(MapItemKind.Ground,0,0,0),Is.True);run.SaveEditedMap();
            Assert.That(run.EditorDraft.Place(MapItemKind.Ground,1,0,0),Is.True);
            run.LoadEditedMap();Assert.That(run.MapLoadPending,Is.True);Assert.That(run.EditorDraft.Items.Count,Is.EqualTo(2));
            // A changed draft is a different operation and must be confirmed again.
            Assert.That(run.EditorDraft.Place(MapItemKind.Ground,2,0,0),Is.True);
            run.LoadEditedMap();Assert.That(run.MapLoadPending,Is.True);Assert.That(run.EditorDraft.Items.Count,Is.EqualTo(3));
            run.LoadEditedMap();Assert.That(run.MapLoadPending,Is.False);Assert.That(run.EditorDraft.Items.Count,Is.EqualTo(1));
            run.LoadEditedMap();Assert.That(run.MapLoadPending,Is.False);
        }
        finally{if(File.Exists(path))File.Delete(path);}
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator ReturningToMenuFromMapGarageWarnsAboutUnsavedDraft()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        var until=Time.realtimeSinceStartup+15;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenMapEditor(),Is.True);
        Assert.That(run.EditorDraft.Place(MapItemKind.Ground,0,0,0),Is.True);
        run.MapEditorGarage();Assert.That(run.MapEditing,Is.False);
        run.RestartRun();Assert.That(run.MapExitPending,Is.True);Assert.That(run.EditorDraft.Items.Count,Is.EqualTo(1));
        Assert.That(run.EditorMessage,Does.Contain("Niezapisane"));
        run.TogglePause();run.TogglePause();Assert.That(run.MapExitPending,Is.False,"Resuming must cancel the warning.");
        run.RestartRun();Assert.That(run.MapExitPending,Is.True);
        // A changed draft invalidates the confirmation even when modified outside the HUD.
        Assert.That(run.EditorDraft.Place(MapItemKind.Ground,1,0,0),Is.True);
        run.RestartRun();Assert.That(run.MapExitPending,Is.True);Assert.That(run.EditorDraft.Items.Count,Is.EqualTo(2));
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator QuitAndReturnRequireSeparateConfirmationForUnsavedMap()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        var until=Time.realtimeSinceStartup+15;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenMapEditor(),Is.True);
        Assert.That(run.EditorDraft.Place(MapItemKind.Ground,0,0,0),Is.True);
        run.MapEditorGarage();run.RestartRun();
        Assert.That(run.MapReturnPending,Is.True);Assert.That(run.QuitRequested,Is.False);
        run.RequestQuitFromMenu();Assert.That(run.MapQuitPending,Is.True);Assert.That(run.QuitRequested,Is.False);
        run.RestartRun();Assert.That(run.MapReturnPending,Is.True);Assert.That(run.QuitRequested,Is.False);
        run.RequestQuitFromMenu();Assert.That(run.MapQuitPending,Is.True);Assert.That(run.QuitRequested,Is.False);
        run.TogglePause();run.TogglePause();Assert.That(run.MapExitPending,Is.False);
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator NativeCloseProtectsDirtyMapAndRequiresFreshConfirmationAfterEdits()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        var until=Time.realtimeSinceStartup+15;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)yield return null;
        var run=R1RunController.Instance;Assert.That(run,Is.Not.Null);
        var handler=typeof(R1RunController).GetMethod("HandleNativeQuit",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That(handler,Is.Not.Null,"Native window close must check for an unsaved map.");
        Assert.That((bool)handler.Invoke(run,null),Is.True,"Normal game exit must remain available.");
        Assert.That(run.OpenMapEditor(),Is.True);
        Assert.That((bool)handler.Invoke(run,null),Is.True,"A clean map must not block closing.");
        Assert.That(run.EditorDraft.Place(MapItemKind.Ground,0,0,0),Is.True);
        Assert.That((bool)handler.Invoke(run,null),Is.False);
        Assert.That(run.MapQuitPending,Is.True);Assert.That(run.Paused,Is.True);
        Assert.That(run.QuitRequested,Is.False);Assert.That(run.EditorMessage,Does.Contain("Niezapisane"));
        Assert.That(run.EditorMessage,Does.Contain("Zamknij okno ponownie"));
        Assert.That((bool)handler.Invoke(run,null),Is.False,"Duplicate close events in one frame must not confirm.");
        yield return null;
        Assert.That(run.EditorDraft.Place(MapItemKind.Ground,1,0,0),Is.True);
        Assert.That((bool)handler.Invoke(run,null),Is.False,"An edited map needs a fresh confirmation.");
        yield return null;
        Assert.That((bool)handler.Invoke(run,null),Is.True,"A repeated close may confirm the unchanged draft.");
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator QuitActionOutsidePauseShowsUnsavedMapWarningBeforeClosing()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        var until=Time.realtimeSinceStartup+15;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenMapEditor(),Is.True);
        Assert.That(run.EditorDraft.Place(MapItemKind.Ground,0,0,0),Is.True);
        Assert.That(run.Paused,Is.False);run.RequestQuitFromMenu();
        Assert.That(run.QuitRequested,Is.False);Assert.That(run.MapQuitPending,Is.True);
        Assert.That(run.Paused,Is.True,"A quit action outside pause must make the warning visible.");
        Assert.That(run.EditorMessage,Does.Contain("Niezapisane"));
        yield return new ExitPlayMode();
    }
}
