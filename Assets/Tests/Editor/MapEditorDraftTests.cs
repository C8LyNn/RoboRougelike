using System;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.MapEditor;
using System.IO;
using System.Linq;

public sealed class MapEditorDraftTests
{
    [Test] public void RuntimeMapDraftExistsForInGameEditing()
    {
        var type=typeof(RoboRougelike.R3.R3VehicleModel).Assembly.GetType("RoboRougelike.MapEditor.MapDraft");
        Assert.That(type,Is.Not.Null,"Map editing requires a real shared data model");
        Assert.That(Activator.CreateInstance(type),Is.Not.Null);
    }
    [Test] public void BuildRotateRemoveUndoRedoAndUniqueMarkers()
    {
        var m=new MapDraft();Assert.That(m.Items,Is.Empty);
        Assert.That(m.Place(MapItemKind.Ground,0,0,0),Is.True);
        Assert.That(m.Place(MapItemKind.Spawn,0,0,1),Is.True);
        Assert.That(m.Place(MapItemKind.Ground,1,0,0),Is.True);
        Assert.That(m.Place(MapItemKind.Ramp,1,0,3),Is.True);
        Assert.That(m.Place(MapItemKind.Spawn,2,0,2),Is.True);
        Assert.That(m.Items.Count(p=>p.kind==MapItemKind.Spawn),Is.EqualTo(1));
        Assert.That(m.Undo(),Is.True);Assert.That(m.Spawn.x,Is.EqualTo(0));
        Assert.That(m.Redo(),Is.True);Assert.That(m.Spawn.x,Is.EqualTo(2));
        Assert.That(m.Remove(1,0),Is.True);Assert.That(m.Items.Any(p=>p.kind==MapItemKind.Ramp),Is.False);
        Assert.That(m.Items.Any(p=>p.kind==MapItemKind.Ground&&p.x==1),Is.True);
        Assert.That(m.Undo(),Is.True);Assert.That(m.Items.Single(p=>p.kind==MapItemKind.Ramp).rotation,Is.EqualTo(3));
        Assert.That(m.Place(MapItemKind.Wall,3,0,0),Is.True);Assert.That(m.CanRedo,Is.False);
    }
    [Test] public void JsonLoadIsTransactionalAndTestsRequireSpawnFinishAndGround()
    {
        var m=new MapDraft();Assert.That(m.TestError(),Is.Not.Null);
        m.Place(MapItemKind.Ground,0,0,0);m.Place(MapItemKind.Spawn,0,0,0);m.Place(MapItemKind.Finish,1,0,0);
        Assert.That(m.TestError(),Is.Null);var json=m.ToJson();var clone=new MapDraft();
        Assert.That(clone.TryLoad(json,out var error),Is.True,error);Assert.That(clone.Items.Count,Is.EqualTo(3));
        Assert.That(clone.TryLoad("{\"version\":1,\"items\":[{\"kind\":99}]}",out error),Is.False);
        Assert.That(clone.Items.Count,Is.EqualTo(3));Assert.That(clone.Undo(),Is.True);Assert.That(clone.Items,Is.Empty);
        Assert.That(m.Place(MapItemKind.Ramp,51,0,0),Is.False);
    }
    [Test] public void LocalFileRoundTripAndNameValidation()
    {
        var directory=Path.Combine(Path.GetTempPath(),"rrl-map-test-"+Guid.NewGuid().ToString("N"));
        try
        {
            Assert.That(MapStorage.TryPath(directory,"../escape",out _),Is.False);
            Assert.That(MapStorage.TryPath(directory,"Tor 1",out var file),Is.True);
            var m=new MapDraft();m.Place(MapItemKind.Ground,0,0,0);MapStorage.Save(file,m);
            m.Place(MapItemKind.Wall,0,0,2);MapStorage.Save(file,m);
            var loaded=new MapDraft();Assert.That(MapStorage.Load(file,loaded,out var error),Is.True,error);
            Assert.That(loaded.Items.Count,Is.EqualTo(2));Assert.That(loaded.Items.Single(p=>p.kind==MapItemKind.Wall).rotation,Is.EqualTo(2));
            File.WriteAllText(file,"{bad");Assert.That(MapStorage.Load(file,loaded,out error),Is.False);Assert.That(loaded.Items.Count,Is.EqualTo(2));
        }
        finally {if(Directory.Exists(directory)){foreach(var f in Directory.GetFiles(directory))File.Delete(f);Directory.Delete(directory);}}
    }
}
