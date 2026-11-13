using System.Linq;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R1;
using RoboRougelike.R6;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogDeviceTierTests
{
    R3VehicleModel Model()=>new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
    R3PartState FuseThree(R3VehicleModel model,R3PartKind kind)
    {
        model.Award(2000);
        for(int i=0;i<3;i++)Assert.That(model.PurchaseToInventory(kind),Is.True);
        var ids=model.Inventory.Where(p=>p.Kind==kind).Take(3).Select(p=>p.Id).ToArray();
        Assert.That(model.FuseInventory(ids),Is.True);
        return model.Inventory.Single(p=>p.Kind==kind&&p.Tier==2);
    }
    void Mount(R3VehicleModel model,R3PartState part)
    {
        var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
        var cell=cells.First(c=>model.OwnedPlacementError(part.Id,c,default)==null);
        Assert.That(model.Mount(part.Id,cell),Is.True);model.RefreshEnergyLayout();
    }
    [Test] public void TierTwoBatteryProvidesTwoHundredCapacity()
    {
        var model=Model();float baseline=model.Energy.Maximum;var part=FuseThree(model,R3PartKind.Battery);Mount(model,part);
        Assert.That(model.Energy.Maximum,Is.EqualTo(baseline+200));
    }
    [Test] public void TierTwoSalvageOptimizerCanBeMountedAndRestored()
    {
        var model=Model();float baseline=model.Energy.Maximum;var part=FuseThree(model,R3PartKind.SalvageOptimizer);Mount(model,part);
        Assert.That(model.Energy.Maximum,Is.EqualTo(baseline-25));
        var checkpoint=model.MakeCheckpoint(0);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==part.Id).Tier,Is.EqualTo(2));
        Assert.That(restored.Energy.Maximum,Is.EqualTo(baseline-25));
    }
    [Test] public void PointDefenseCanBeFusedAndRestoredAsTierTwo()
    {
        var model=Model();var part=FuseThree(model,R3PartKind.PointDefense);Mount(model,part);
        Assert.That(part.Tier,Is.EqualTo(2));
        var checkpoint=model.MakeCheckpoint(0);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==part.Id).Tier,Is.EqualTo(2));
    }
    [Test] public void TierTwoRegeneratorImprovesRecoveryAndReservesTwentyFive()
    {
        var model=Model();float baseline=model.Energy.Maximum;var part=FuseThree(model,R3PartKind.EnergyRegenerator);Mount(model,part);
        Assert.That(model.Energy.Maximum,Is.EqualTo(baseline-25));
        Assert.That(model.Energy.TrySpend(model.Energy.Current),Is.True);model.Energy.Advance(1);
        Assert.That(model.Energy.Current,Is.EqualTo(20).Within(.001));
    }
    [Test] public void TierThreeRegeneratorHasFourTimesTheEffectAndTwentyFiveReservation()
    {
        var model=Model();model.Award(2000);float baseline=model.Energy.Maximum;
        for(int i=0;i<9;i++)Assert.That(model.PurchaseToInventory(R3PartKind.EnergyRegenerator),Is.True);
        for(int i=0;i<3;i++)Assert.That(model.FuseInventory(model.Inventory.Where(p=>p.Kind==R3PartKind.EnergyRegenerator&&p.Tier==1).Take(3).Select(p=>p.Id).ToArray()),Is.True);
        Assert.That(model.FuseInventory(model.Inventory.Select(p=>p.Id).ToArray()),Is.True);
        var part=model.Inventory.Single();Assert.That(part.Tier,Is.EqualTo(3));Mount(model,part);
        Assert.That(model.Energy.Maximum,Is.EqualTo(baseline-25));
        Assert.That(model.Energy.TrySpend(model.Energy.Current),Is.True);model.Energy.Advance(1);
        Assert.That(model.Energy.Current,Is.EqualTo(30).Within(.001));
    }
}

public sealed class BacklogDeviceTierGameplayTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        run.VehicleModel.Award(500);
    }
    R3PartState FuseAndMount(R3PartKind kind)
    {
        for(int i=0;i<3;i++)Assert.That(run.GaragePurchase(kind),Is.True);
        var ids=run.VehicleModel.Inventory.Where(p=>p.Kind==kind).Take(3).Select(p=>p.Id).ToArray();
        Assert.That(run.GarageFuse(ids),Is.True);
        var part=run.VehicleModel.Inventory.Single(p=>p.Kind==kind&&p.Tier==2);
        var cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
        var cell=cells.First(c=>run.VehicleModel.OwnedPlacementError(part.Id,c,default)==null);
        Assert.That(run.GarageMount(part.Id,cell),Is.True);return part;
    }
    [UnityTest] public IEnumerator SalvageOptimizerConsumesEnergyAndCaptureDropsDestroyedBonus()
    {
        var bank=run.Player.GetComponent<VehicleEnergyBank>();float before=bank.Maximum;
        var part=FuseAndMount(R3PartKind.SalvageOptimizer);
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();bank=run.Player.GetComponent<VehicleEnergyBank>();
        bank.Refresh();Assert.That(bank.Maximum,Is.EqualTo(before-25));
        var mounted=assembly.Parts.Single(p=>p.Id==part.Id);
        var ledger=new RoboRougelike.R4.R4SalvageLedger();
        Assert.That(ledger.Record("foe",new[]{new RoboRougelike.R4.R4SalvageItem("drop","foe/drop",R3PartKind.Wheel,0,1)}),Is.True);
        assembly.Capture();int boosted=ledger.PreviewScrap(run.VehicleModel,System.Array.Empty<string>());
        mounted.Damageable.ApplyDamageResolvedTyped(mounted.Damageable.CurrentHp/.9f+1f,mounted.transform.position,Vector3.up,
            R3DamagePropagation.DamageKind.Piercing);assembly.Capture();
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==part.Id).Health,Is.Zero);
        Assert.That(ledger.PreviewScrap(run.VehicleModel,System.Array.Empty<string>()),Is.LessThan(boosted));
        yield return null;
    }
    [UnityTest] public IEnumerator UpgradedMotorUsesTwoPowerUnitsAndTwentyFiveEnergyReservation()
    {
        var drive=run.Player.GetComponent<R6VehicleDrive>();var bank=run.Player.GetComponent<VehicleEnergyBank>();
        drive.RefreshMass();bank.Refresh();float powerBefore=drive.ActivePowerUnits,energyBefore=bank.Maximum;
        var part=FuseAndMount(R3PartKind.DrivePower);
        drive=run.Player.GetComponent<R6VehicleDrive>();bank=run.Player.GetComponent<VehicleEnergyBank>();
        drive.RefreshMass();bank.Refresh();
        Assert.That(part.Tier,Is.EqualTo(2));Assert.That(drive.ActivePowerUnits,Is.EqualTo(powerBefore+2).Within(.001));
        Assert.That(bank.Maximum,Is.EqualTo(energyBefore-25));yield return null;
    }
    [UnityTest] public IEnumerator UpgradedRepairUsesMoreEnergyAndHealsTwiceAsFast()
    {
        FuseAndMount(R3PartKind.RepairPulse);Assert.That(run.EnterTestRange(),Is.True);
        for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;
        Assert.That(run.CanUseMovement,Is.True);
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var pulse=run.Player.GetComponent<R3RepairPulse>();
        var bank=run.Player.GetComponent<VehicleEnergyBank>();
        var armor=assembly.Parts.First(p=>p.Kind==R3PartKind.Armor&&p.Operational);
        armor.Damageable.RestoreHealthRatio(.5f);float before=bank.Current;
        Assert.That(pulse.TryActivate(),Is.EqualTo(1));
        Assert.That(bank.Current,Is.EqualTo(before-18f).Within(.001));
        typeof(R3RepairPulse).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pulse,new object[]{1f});
        Assert.That(armor.Damageable.CurrentHp/armor.Damageable.MaxHp,Is.EqualTo(.6f).Within(.001));
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
