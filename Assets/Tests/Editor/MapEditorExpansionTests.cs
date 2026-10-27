using System;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.MapEditor;

public sealed class MapEditorExpansionTests
{
    static MethodInfo ScaledPlace => typeof(MapDraft).GetMethod("Place",new[]{typeof(MapItemKind),typeof(int),typeof(int),typeof(int),typeof(int),typeof(int),typeof(int)});
    static bool Place(MapDraft d,MapItemKind k,int x,int z,int r,int sx,int sy,int sz)
    {
        Assert.That(ScaledPlace,Is.Not.Null,"Scaled placement must be available");
        return (bool)ScaledPlace.Invoke(d,new object[]{k,x,z,r,sx,sy,sz});
    }
    [Test] public void FullFloorCanBeUndoneRedoneAndSaved()
    {
        var m=new MapDraft();var set=typeof(MapDraft).GetMethod("SetGlobalFloor");var prop=typeof(MapDraft).GetProperty("GlobalFloor");
        Assert.That(set,Is.Not.Null);Assert.That(prop,Is.Not.Null);set.Invoke(m,new object[]{true});
        m.Place(MapItemKind.Spawn,0,0,0);m.Place(MapItemKind.Finish,2,0,0);Assert.That(m.TestError(),Is.Null);
        var copy=new MapDraft();Assert.That(copy.TryLoad(m.ToJson(),out var error),Is.True,error);
        Assert.That(prop.GetValue(copy),Is.True);Assert.That(copy.Undo(),Is.True);Assert.That(prop.GetValue(copy),Is.False);
        Assert.That(copy.Redo(),Is.True);Assert.That(prop.GetValue(copy),Is.True);
    }
    [Test] public void ScaleOccupiesRotatedCellsAndRemovesFromCoveredCell()
    {
        var m=new MapDraft();Assert.That(Place(m,MapItemKind.Wall,0,0,1,2,3,1),Is.True);
        Assert.That(Place(m,MapItemKind.Obstacle,0,-1,0,1,1,1),Is.False,"Occupied by rotated scaled wall");
        Assert.That(m.Remove(0,-1),Is.True);Assert.That(m.Items.Count,Is.Zero);
        Assert.That(m.Undo(),Is.True);Assert.That(m.Items.Count,Is.EqualTo(1));
    }
    [Test] public void OldMapsLoadAtUnitScaleAndNewScaleRoundTrips()
    {
        var m=new MapDraft();Assert.That(m.TryLoad("{\"version\":1,\"items\":[{\"kind\":1,\"x\":0,\"z\":0,\"rotation\":0}]}",out var e),Is.True,e);
        var sx=typeof(MapItem).GetField("scaleX");Assert.That(sx,Is.Not.Null);Assert.That(sx.GetValue(m.Items[0]),Is.EqualTo(1));
        Assert.That(Place(m,MapItemKind.Ramp,3,0,0,2,3,4),Is.True);
        var copy=new MapDraft();Assert.That(copy.TryLoad(m.ToJson(),out e),Is.True,e);
        Assert.That(sx.GetValue(copy.Items[1]),Is.EqualTo(2));Assert.That(typeof(MapItem).GetField("scaleY").GetValue(copy.Items[1]),Is.EqualTo(3));
        Assert.That(typeof(MapItem).GetField("scaleZ").GetValue(copy.Items[1]),Is.EqualTo(4));
        Assert.That(Enum.GetValues(typeof(MapItemKind)).Length,Is.GreaterThanOrEqualTo(10));
    }
}
