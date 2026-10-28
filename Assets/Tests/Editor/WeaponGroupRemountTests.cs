using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;

public sealed class WeaponGroupRemountTests
{
    [Test] public void RemountSameOwnedGunPreservesBThroughIntermediateReadsAndCopy()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var gun=model.Parts.First(p=>p.Kind==R3PartKind.Gun);var id=gun.Id;var cell=gun.Cell;var rot=gun.RotationQuarterTurns;
        model.SwapWeaponGroups();Assert.That(model.WeaponGroupA,Is.Null);Assert.That(model.WeaponGroupB,Is.EqualTo(gun.Weapon));
        Assert.That(model.Remove(cell),Is.True);Assert.That(model.WeaponGroupB,Is.Null);
        var copy=model.DeepCopy();Assert.That(copy.WeaponGroupA,Is.Null);Assert.That(copy.WeaponGroupB,Is.Null);
        Assert.That(copy.Mount(id,cell,rot),Is.True);Assert.That(copy.WeaponGroupA,Is.Null);
        Assert.That(copy.WeaponGroupB,Is.EqualTo(gun.Weapon),"the same physical gun belongs to B");
    }
}
