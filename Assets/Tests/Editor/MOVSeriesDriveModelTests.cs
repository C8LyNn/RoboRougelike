using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R3;

public class MOVSeriesDriveModelTests
{
    [TestCase(R3DriveKind.Tracks,R3PartKind.CrawlerTrack,2)]
    [TestCase(R3DriveKind.Omni,R3PartKind.OmniDrive,1)]
    [TestCase(R3DriveKind.Hover,R3PartKind.HoverEngine,1)]
    public void SelectedAlternativeAllowsWheelRemovalDeployCopyAndEmergencyRepair(R3DriveKind drive,R3PartKind kind,int required)
    {
        var m=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));m.Award(500);
        foreach(int x in new[]{-2,2})foreach(int z in new[]{-2,2})Assert.That(m.Buy(new Vector3Int(x,0,z),kind),Is.True);
        m.SelectedDrive=drive;
        foreach(var wheel in m.Parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray())Assert.That(m.Remove(wheel.Cell),Is.True);
        Assert.That(m.CanDeploy,Is.True);Assert.That(m.DeepCopy().SelectedDrive,Is.EqualTo(drive));
        Assert.That(m.Inventory.Count,Is.EqualTo(4));int scrap=m.Scrap;
        foreach(var p in m.Parts.Where(p=>p.Kind==kind))p.Health=0;
        Assert.That(m.CanDeploy,Is.False);Assert.That(m.EmergencyRepair(),Is.True);Assert.That(m.CanDeploy,Is.True);
        Assert.That(m.Scrap,Is.EqualTo(scrap-(drive==R3DriveKind.Tracks?90:drive==R3DriveKind.Omni?40:35)));Assert.That(m.Parts.Count(p=>p.Kind==kind&&p.Health>0),Is.EqualTo(required));
        Assert.That(m.Parts.Where(p=>p.Kind==kind&&p.Health>0).All(p=>p.Health==1f),Is.True);
        var last=m.Parts.First(p=>p.Kind==kind&&p.Health>0);Assert.That(m.Remove(last.Cell),Is.False);
    }
    [Test] public void InvalidOrUnavailableSelectedModeDoesNotThrowEmergencyRepair()
    {
        var m=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));m.SelectedDrive=R3DriveKind.Hover;
        Assert.That(m.CanDeploy,Is.False);Assert.That(m.EmergencyRepair(),Is.False);
        m.SelectedDrive=(R3DriveKind)999;Assert.That(m.CanDeploy,Is.False);Assert.That(m.EmergencyRepair(),Is.False);
    }
}
