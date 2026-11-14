using System;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;

public sealed class BacklogDeployableSlotTests
{
    [Test] public void DroneAssignmentsChargesAndCooldownsSurviveCheckpoint()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(5000);
        var kind=R3PartKind.FlyingDroneGenerator;Assert.That(model.PurchaseToInventory(kind),Is.True);
        var part=model.Inventory.Last(p=>p.Kind==kind);
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.OwnedPlacementError(part.Id,c,default)==null);
        Assert.That(model.Mount(part.Id,cell),Is.True);Assert.That(model.AssignDeployableSlot(kind,2),Is.True);
        Assert.That(model.Charges.EnsurePool(R3DeployableService.FlyingDronePool,3),Is.True);
        Assert.That(model.Charges.TrySpend(R3DeployableService.FlyingDronePool,1),Is.True);
        typeof(R3VehicleModel).GetProperty("FlyingDroneReadyAt").SetValue(model,123f);
        Assert.That(model.PurchaseToInventory(R3PartKind.GroundDroneGenerator),Is.True);
        var ground=model.Inventory.Last(p=>p.Kind==R3PartKind.GroundDroneGenerator);
        var groundCell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.OwnedPlacementError(ground.Id,c,default)==null);
        Assert.That(model.Mount(ground.Id,groundCell),Is.True);Assert.That(model.AssignDeployableSlot(ground.Kind,3),Is.True);
        Assert.That(model.Charges.EnsurePool(R3DeployableService.GroundDronePool,3),Is.True);
        Assert.That(model.Charges.TrySpend(R3DeployableService.GroundDronePool,2),Is.True);
        typeof(R3VehicleModel).GetProperty("GroundDroneReadyAt").SetValue(model,130f);
        var checkpoint=UnityEngine.JsonUtility.FromJson<R3ModelCheckpoint>(UnityEngine.JsonUtility.ToJson(model.MakeCheckpoint(100)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,200,out var restored,out var error),Is.True,error);
        Assert.That(restored.GetDeployableSlots()[2],Is.EqualTo(kind));
        Assert.That(restored.Charges.Current(R3DeployableService.FlyingDronePool),Is.EqualTo(2));
        Assert.That(restored.FlyingDroneReadyAt,Is.EqualTo(223).Within(.001));
        Assert.That(restored.GetDeployableSlots()[3],Is.EqualTo(ground.Kind));
        Assert.That(restored.Charges.Current(R3DeployableService.GroundDronePool),Is.EqualTo(1));
        Assert.That(restored.GroundDroneReadyAt,Is.EqualTo(230).Within(.001));
    }
    [Test] public void CheckpointCannotRestoreFiveMountedDeployableTypes()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(5000);
        foreach(var kind in new[]{R3PartKind.RampDeployer,R3PartKind.WallDeployer,R3PartKind.JumpPadDeployer,R3PartKind.BlastTrapDeployer})
        {
            Assert.That(model.PurchaseToInventory(kind),Is.True);var part=model.Inventory.Last(p=>p.Kind==kind);
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.OwnedPlacementError(part.Id,c,default)==null);
            Assert.That(model.Mount(part.Id,cell),Is.True);
        }
        var validCheckpoint=model.MakeCheckpoint(0);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(validCheckpoint,R3VehicleKind.Light,model.Economy,0,out _,out var error),Is.True,error);
        // Creative mode is the actual supported way to construct more than four kinds.
        typeof(R3VehicleModel).GetProperty("CreativeMode").SetValue(model,true);
        Assert.That(model.PurchaseToInventory(R3PartKind.EmpTrapDeployer),Is.True);
        var fifth=model.Inventory.Last(p=>p.Kind==R3PartKind.EmpTrapDeployer);
        var valid=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.OwnedPlacementError(fifth.Id,c,default)==null);
        Assert.That(model.Mount(fifth.Id,valid),Is.True);
        var saved=model.MakeCheckpoint(0);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,model.Economy,0,out _,out _),Is.False,
            "A modified save must not bypass the shared four-type limit");
    }
    [Test] public void FifthDistinctDeployableIsRejectedButExtraCopiesUseTheExistingSlot()
    {
        Assert.That(Enum.TryParse("BlastTrapDeployer",out R3PartKind blast),Is.True);
        Assert.That(Enum.TryParse("EmpTrapDeployer",out R3PartKind emp),Is.True);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(5000);
        foreach(var kind in new[]{R3PartKind.RampDeployer,R3PartKind.WallDeployer,R3PartKind.JumpPadDeployer,blast})
        {
            Assert.That(model.PurchaseToInventory(kind),Is.True);var part=model.Inventory.Last(p=>p.Kind==kind);
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.OwnedPlacementError(part.Id,c,default)==null);
            Assert.That(model.Mount(part.Id,cell),Is.True);
        }
        Assert.That(model.GetDeployableSlots().Count(p=>p.HasValue),Is.EqualTo(4));
        Assert.That(model.PurchaseToInventory(emp),Is.True);var fifth=model.Inventory.Last(p=>p.Kind==emp);
        Assert.That(model.PurchaseToInventory(blast),Is.True);var copy=model.Inventory.Last(p=>p.Kind==blast);
        var valid=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.OwnedPlacementError(copy.Id,c,default)==null);
        Assert.That(model.OwnedPlacementError(fifth.Id,valid,default),Does.Contain("4"));
        Assert.That(model.Mount(copy.Id,valid),Is.True);Assert.That(model.GetDeployableSlots().Count(p=>p.HasValue),Is.EqualTo(4));
    }
    [Test] public void RampWallAndJumpPadShareOneFourSlotPool()
    {
        Assert.That(Enum.TryParse("WallDeployer",out R3PartKind wall),Is.True);
        Assert.That(Enum.TryParse("JumpPadDeployer",out R3PartKind jump),Is.True);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(3000);
        foreach(var kind in new[]{R3PartKind.RampDeployer,wall,jump})
        {
            Assert.That(model.PurchaseToInventory(kind),Is.True);var part=model.Inventory.Last(p=>p.Kind==kind);
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.OwnedPlacementError(part.Id,c,default)==null);
            Assert.That(model.Mount(part.Id,cell),Is.True);
        }
        var slots=model.GetDeployableSlots();Assert.That(slots.Count(p=>p.HasValue),Is.EqualTo(3));
        Assert.That(slots.Where(p=>p.HasValue).Select(p=>p.Value).Distinct().Count(),Is.EqualTo(3));
        Assert.That(model.AssignDeployableSlot(jump,3),Is.True);
        var checkpoint=model.MakeCheckpoint(0);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.GetDeployableSlots()[3],Is.EqualTo(jump));
    }
    [Test] public void RampGeneratorUsesItsOwnSlotPoolAndSurvivesAJsonCheckpoint()
    {
        Assert.That(Enum.TryParse("RampDeployer",out R3PartKind kind),Is.True,"Missing ramp generator");
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(2000);
        for(int i=0;i<2;i++)Assert.That(model.PurchaseToInventory(kind),Is.True);
        foreach(var part in model.Inventory.Where(p=>p.Kind==kind).ToArray())
        {
            var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            var cell=cells.First(c=>model.OwnedPlacementError(part.Id,c,default)==null);
            Assert.That(model.Mount(part.Id,cell),Is.True);
        }
        var get=typeof(R3VehicleModel).GetMethod("GetDeployableSlots");var assign=typeof(R3VehicleModel).GetMethod("AssignDeployableSlot");
        Assert.That(get,Is.Not.Null);Assert.That(assign,Is.Not.Null);
        var slots=(R3PartKind?[])get.Invoke(model,null);
        Assert.That(slots.Count(s=>s==kind),Is.EqualTo(1),"Copies share one deployable slot");
        Assert.That(assign.Invoke(model,new object[]{kind,2}),Is.EqualTo(true));
        Assert.That(((R3PartKind?[])get.Invoke(model,null))[2],Is.EqualTo(kind));
        Assert.That(model.GetActiveSlots().Contains(kind),Is.False,"Deployables do not consume active module slots");
        var saved=UnityEngine.JsonUtility.FromJson<R3ModelCheckpoint>(UnityEngine.JsonUtility.ToJson(model.MakeCheckpoint(1)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,model.Economy,1,out var restored,out var error),Is.True,error);
        Assert.That(((R3PartKind?[])get.Invoke(restored,null))[2],Is.EqualTo(kind));
        foreach(var part in restored.Parts.Where(p=>p.Kind==kind).Reverse().ToArray())Assert.That(restored.Remove(part.Cell),Is.True);
        Assert.That(((R3PartKind?[])get.Invoke(restored,null)).All(p=>!p.HasValue),Is.True);
        var owned=restored.Inventory.First(p=>p.Kind==kind);
        var remount=restored.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>restored.OwnedPlacementError(owned.Id,c,default)==null);
        Assert.That(restored.Mount(owned.Id,remount),Is.True);
        Assert.That(((R3PartKind?[])get.Invoke(restored,null))[2],Is.EqualTo(kind),"The slot preference survives removing and remounting every copy");
        saved.deployableSlots[0]=(int)kind;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,model.Economy,1,out _,out _),Is.False,
            "Duplicate deployable slots must not be accepted from a corrupt save");
    }
}
