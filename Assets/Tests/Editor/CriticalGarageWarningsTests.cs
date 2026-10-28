using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R3;
using UnityEngine;

public sealed class CriticalGarageWarningsTests
{
    static string Warnings(R3VehicleModel model)
    {
        var p=typeof(R3VehicleModel).GetProperty("CriticalReadinessMessage",BindingFlags.Public|BindingFlags.Instance);
        Assert.That(p,Is.Not.Null,"Garage needs a combined, always-visible functional readiness warning");
        return (string)p.GetValue(model);
    }
    [Test] public void NoWeaponAndNoDriveAreBothVisibleWhileEditingRemainsAllowed()
    {
        var m=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var gun=m.Parts.First(p=>p.Kind==R3PartKind.Gun);Assert.That(m.Remove(gun.Cell),Is.True);
        foreach(var wheel in m.Parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray())Assert.That(m.Remove(wheel.Cell),Is.True);
        string message=Warnings(m);Assert.That(message,Does.Contain("broni"));Assert.That(message,Does.Contain("napędu"));
        Assert.That(m.CanDeploy,Is.False);Assert.That(m.Inventory.Any(p=>p.Id==gun.Id),Is.True);
    }
    [Test] public void DestroyedGunAndOneWheelSideBothWarn()
    {
        var m=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        m.Parts.First(p=>p.Kind==R3PartKind.Gun).Health=0;
        foreach(var p in m.Parts.Where(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x<0))p.Health=0;
        string message=Warnings(m);Assert.That(message,Does.Contain("broni"));Assert.That(message,Does.Contain("napędu"));
    }
    [Test] public void OperationalVehicleHasNoCriticalWarning()
    {var m=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));Assert.That(Warnings(m),Is.Null.Or.Empty);}
    [Test] public void UndamagedGunBeyondDestroyedBridgeIsNotOperational()
    {
        var design=new R3VehicleDesign();
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.forward,R3PartKind.Armor));
        design.Parts.Add(new R3PartState(Vector3Int.forward*2,R3PartKind.Gun));
        design.Parts.Add(new R3PartState(Vector3Int.left,R3PartKind.Wheel));
        design.Parts.Add(new R3PartState(Vector3Int.right,R3PartKind.Wheel));
        var m=new R3VehicleModel(design);m.At(Vector3Int.forward).Health=0;
        Assert.That(Warnings(m),Does.Contain("broni"));Assert.That(Warnings(m),Does.Not.Contain("napędu"));
    }
}
