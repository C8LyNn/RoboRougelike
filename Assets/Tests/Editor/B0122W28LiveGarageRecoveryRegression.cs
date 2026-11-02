// B01-22 / P17 W28 — actual current playable R3 game model acceptance.
// The test exercises REAL R3VehicleModel and R3VehicleCatalog, not ECON V7
// source reducers, fake combat rewards, #797 Save or the unavailable loaner.
// Native Unity 6000.3.14f1 EDITMODE: NOT_RUN until real NUnit XML/Editor.log.
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R3;

public sealed class B0122W28LiveGarageRecoveryRegression
{
    private static R3VehicleModel ReadyLight()
    {
        var rig=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        Assert.That(rig.SelectedDrive,Is.EqualTo(R3DriveKind.Wheels),"actual default drive");
        Assert.That(rig.CanDeploy,Is.True,"real factory vehicle must be deployable");
        Assert.That(rig.Scrap,Is.Zero,"new model does not receive phantom currency");
        return rig;
    }

    private static R3PartState[] DestroyOneDriveSide(R3VehicleModel rig)
    {
        var left=rig.Parts.Where(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x<0).ToArray();
        Assert.That(left.Length,Is.GreaterThan(0),"authored Light has left-wheel side");
        Assert.That(rig.Parts.Any(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x>0),Is.True,
            "authored Light has opposite intact wheel side");
        foreach(var wheel in left)wheel.Health=0;
        Assert.That(rig.Parts.Single(p=>p.Kind==R3PartKind.Core).Health,Is.EqualTo(1),
            "Core remains physically alive");
        return left;
    }

    [Test] public void ActualGarage_BrokenEssentialDriveAndZeroWallet_FailsClosedWithoutMutation()
    {
        var rig=ReadyLight();
        var wheels=DestroyOneDriveSide(rig);
        var before=rig.Parts.Select(p=>p.Id).ToArray();
        Assert.That(rig.CanDeploy,Is.False,"missing essential wheel side must block normal launch");
        Assert.That(rig.DeployError,Does.Contain("koło").IgnoreCase,
            "garage must present actual reason rather than optional armor warning");
        Assert.That(rig.Repair(wheels[0].Cell),Is.False,
            "zero wallet must not authorize paid legacy repair");
        Assert.That(rig.EmergencyRepair(),Is.False,
            "zero wallet cannot issue unauthorized emergency payment");
        Assert.That(rig.RepairAffordable(),Is.Zero,
            "recovery cannot duplicate free repairs at wallet zero");
        Assert.That(rig.Scrap,Is.Zero);
        Assert.That(rig.CanDeploy,Is.False);
        Assert.That(rig.Parts.Select(p=>p.Id),Is.EqualTo(before),
            "failed operations never reassign stable owned part GUIDs");
        Assert.That(wheels.All(p=>p.Health==0),Is.True,
            "destroyed modules cannot heal on failed garage click");
    }

    [Test] public void ActualGarage_FundedLegacyRepair_ImmediatelyRestoresButIsNotV7()
    {
        var rig=ReadyLight();
        var wheels=DestroyOneDriveSide(rig);
        Assert.That(rig.CanDeploy,Is.False);
        int originalPaid=rig.RepairCost(wheels[0]);
        // This is an intentional W28 witness of the CURRENT live R3 model.
        // V7 price 15 => 9 (floor 65%), and restores on NEXT real victory;
        // old R3 formula is ceil(35%*15) = 6 and heals immediately.
        Assert.That(rig.Economy.WheelCost,Is.EqualTo(15));
        Assert.That(originalPaid,Is.EqualTo(6),
            "current playable repair is legacy 35% not approved V7 65%");
        var wheelId=wheels[0].Id;
        rig.Award(originalPaid);
        Assert.That(rig.Repair(wheels[0].Cell),Is.True);
        Assert.That(wheels[0].Health,Is.EqualTo(1),
            "legacy R3 heals immediately; V7 must NOT copy this behavior");
        Assert.That(wheels[0].Id,Is.EqualTo(wheelId));
        Assert.That(rig.Scrap,Is.Zero);
        Assert.That(rig.CanDeploy,Is.True,
            "existing game can recover with sufficient wallet via OLD model");
    }

    [Test] public void ActualGarage_OptionalDestroyedArmorDoesNotBlockCombatLaunch()
    {
        var rig=ReadyLight();
        var part=rig.Parts.First(p=>p.Kind==R3PartKind.Armor&&
            p.Cell.x!=0&&Math.Abs(p.Cell.z)>0);
        var id=part.Id;
        part.Health=0;
        Assert.That(rig.CanDeploy,Is.True,
            "optional armor damage must not block launch if live Core/drives/gun intact");
        Assert.That(rig.DeployError,Is.Null);
        Assert.That(part.Id,Is.EqualTo(id),"damaged optional armor retains identity");
        Assert.That(part.Health,Is.Zero,"optional damage is not silently repaired");
    }

    [Test] public void ActualGarage_DeadCoreCannotBeDeclaredDeployable()
    {
        var rig=ReadyLight();
        var core=rig.Parts.Single(p=>p.Kind==R3PartKind.Core);
        core.Health=0;
        Assert.That(rig.CanDeploy,Is.False);
        Assert.That(rig.DeployError,Does.Contain("Rdzeń"));
        Assert.That(core.Health,Is.Zero);
    }
}
