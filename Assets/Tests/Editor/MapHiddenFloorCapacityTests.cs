using System.Text;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.MapEditor;
using System.IO;
using UnityEngine;

public sealed class MapHiddenFloorCapacityTests
{
    static string FloorDocument()
    {
        var b=new StringBuilder("{\"version\":2,\"globalFloor\":true,\"items\":[");
        for(int i=0;i<MapDraft.MaxItems;i++)
        {if(i>0)b.Append(',');b.Append("{\"kind\":0,\"x\":").Append(i%101-50).Append(",\"z\":").Append(i/101-50).Append(",\"rotation\":0,\"scaleX\":1,\"scaleY\":1,\"scaleZ\":1}");}
        return b.Append("]}").ToString();
    }
    [Test] public void HiddenFloorTilesDoNotConsumeObjectCapacityOrDisappear()
    {
        var m=new MapDraft();Assert.That(m.TryLoad(FloorDocument(),out var error),Is.True,error);
        Assert.That(m.Items.Count,Is.EqualTo(MapDraft.MaxItems));Assert.That(m.GlobalFloor,Is.True);
        Assert.That(m.Place(MapItemKind.Wall,0,0,0),Is.True,"hidden floor cannot exhaust obstacle capacity");
        var roundTrip=new MapDraft();Assert.That(roundTrip.TryLoad(m.ToJson(),out error),Is.True,error);
        Assert.That(roundTrip.Items.Count,Is.EqualTo(MapDraft.MaxItems+1));
        Assert.That(roundTrip.SetGlobalFloor(false),Is.True);
        Assert.That(roundTrip.Items.Count(p=>p.kind==MapItemKind.Ground),Is.EqualTo(MapDraft.MaxItems));
        Assert.That(roundTrip.Undo(),Is.True);Assert.That(roundTrip.GlobalFloor,Is.True);
        Assert.That(roundTrip.Redo(),Is.True);Assert.That(roundTrip.GlobalFloor,Is.False);
    }
    [TestCase("LD02_ARENA.json",168)][TestCase("LD02_COVER_CHOICE.json",173)]
    public void LatestB01LevelDataStillLoadsAndRoundTrips(string file,int count)
    {
        string path=Path.Combine(Application.dataPath,"../Docs/CX08/Fixtures",file);
        var map=new MapDraft();Assert.That(map.TryLoad(File.ReadAllText(path),out var error),Is.True,error);
        Assert.That(map.Items.Count,Is.EqualTo(count));Assert.That(map.TestError(),Is.Null);
        var copy=new MapDraft();Assert.That(copy.TryLoad(map.ToJson(),out error),Is.True,error);
        Assert.That(copy.Items.Count,Is.EqualTo(count));Assert.That(copy.TestError(),Is.Null);
    }
}
