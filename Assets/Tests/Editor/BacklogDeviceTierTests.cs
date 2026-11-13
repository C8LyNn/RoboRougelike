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
    [TestCase(R3PartKind.DoubleShot,25)][TestCase(R3PartKind.DoubleShotOverdrive,0)]
    public void TierTwoProjectileModulesFuseAndSurviveCheckpoint(R3PartKind kind,float reservation)
    {
        var model=Model();float baseline=model.Energy.Maximum;var part=FuseThree(model,kind);Mount(model,part);
        Assert.That(model.Energy.Maximum,Is.EqualTo(baseline-reservation));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(model.MakeCheckpoint(0),R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==part.Id).Tier,Is.EqualTo(2));
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
    [UnityTest] public IEnumerator OverdriveCombinesMountedCopiesAndCostsEnergyOnce()
    {
        Assert.That(System.Enum.TryParse("Overdrive",out R3PartKind kind),Is.True);
        for(int i=0;i<2;i++)
        {
            Assert.That(run.GaragePurchase(kind),Is.True);
            var owned=run.VehicleModel.Inventory.Last(p=>p.Kind==kind);
            var cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            var cell=cells.First(c=>run.VehicleModel.OwnedPlacementError(owned.Id,c,default)==null);
            Assert.That(run.GarageMount(owned.Id,cell),Is.True);
        }
        Assert.That(run.EnterTestRange(),Is.True);
        for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;
        Assert.That(run.CanUseMovement,Is.True);
        var componentType=System.Type.GetType("RoboRougelike.R3.R3OffensiveDevices, Assembly-CSharp");
        Assert.That(componentType,Is.Not.Null);
        var component=run.Player.GetComponent(componentType);Assert.That(component,Is.Not.Null);
        var activate=componentType.GetMethod("TryActivate");Assert.That(activate,Is.Not.Null);
        var bank=run.Player.GetComponent<VehicleEnergyBank>();
        Assert.That(bank.TrySpend(bank.Current-20),Is.True);
        Assert.That((int)activate.Invoke(component,null),Is.Zero,"Two copies must not partially activate with insufficient energy.");
        Assert.That(bank.Current,Is.EqualTo(20).Within(.01f));bank.Recharge(100);
        float before=bank.Current;
        Assert.That((int)activate.Invoke(component,null),Is.EqualTo(2));
        Assert.That(bank.Current,Is.EqualTo(before-30).Within(.01f));
        var effects=run.Player.GetComponent<R3MountedBonusEffects>();
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1.4f).Within(.001f));
        Assert.That((int)activate.Invoke(component,null),Is.Zero,"A running shared effect cannot be activated again.");
        var copy=run.VehicleModel.DeepCopy();copy.Parts.RemoveAll(p=>p.Kind==kind);
        float now=Time.time;var checkpoint=copy.MakeCheckpoint(now);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,copy.Economy,now,out var restored,out var error),Is.True,error);
        Assert.That(restored.OverdriveReadyAt,Is.GreaterThan(now),"Removing all copies cannot reset the shared cooldown.");
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==kind);
        part.gameObject.SetActive(false);yield return null;
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1.2f).Within(.001f));
        var remaining=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==kind&&p!=part);
        var receiver=remaining.GetComponent<RoboRougelike.Mechanics.EMP.EmpReceiver>();Assert.That(receiver,Is.Not.Null);
        receiver.ApplyEmp(2);yield return null;
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1f).Within(.001f));
        Assert.That((int)activate.Invoke(component,null),Is.Zero,"Shared cooldown remains after all copies stop working.");
    }
    [UnityTest] public IEnumerator OverdriveSlotRaisesActualBasicProjectileDamage()
    {
        Assert.That(run.GaragePurchase(R3PartKind.Overdrive),Is.True);
        var owned=run.VehicleModel.Inventory.Last(p=>p.Kind==R3PartKind.Overdrive);
        var cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
        var cell=cells.First(c=>run.VehicleModel.OwnedPlacementError(owned.Id,c,default)==null);
        Assert.That(run.GarageMount(owned.Id,cell),Is.True);
        var reward=ScriptableObject.CreateInstance<R1UpgradeDefinition>();
        reward.Id="overdrive-synergy";reward.Effect=R1UpgradeEffect.Damage;reward.Amount=.25f;
        Assert.That(run.VehicleModel.ReceiveBonuses(new[]{reward}),Is.True);
        var bonus=run.VehicleModel.Inventory.Single(p=>p.Kind==R3PartKind.BonusModule);
        cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
        var bonusCell=cells.First(c=>run.VehicleModel.OwnedPlacementError(bonus.Id,c,default)==null);
        Assert.That(run.GarageMount(bonus.Id,bonusCell),Is.True);
        Assert.That(run.VehicleModel.ActiveSlotLabel(R3PartKind.Overdrive),Is.Not.EqualTo("—"));
        Assert.That(run.EnterTestRange(),Is.True);
        for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;
        Assert.That(run.CanUseMovement,Is.True);
        var input=run.Player.GetComponent<RoboRougelike.MOVSeries.MOVSeriesInput>();
        typeof(RoboRougelike.MOVSeries.MOVSeriesInput).GetMethod("Activate",BindingFlags.Instance|BindingFlags.NonPublic)
            .Invoke(input,new object[]{run,R3PartKind.Overdrive});
        var effects=run.Player.GetComponent<R3MountedBonusEffects>();Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1.45f).Within(.001f));
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun);
        run.Player.GetComponent<Rigidbody>().isKinematic=true;
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*4f;
        var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(1000,false);
        Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(gun),Is.True);
        assembly.FireGroup(false);
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>=1000&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(942).Within(.1f),"20% Overdrive and 25% passive damage must add before the shot.");
        Object.Destroy(target);Object.Destroy(reward);
    }
    [UnityTest] public IEnumerator RapidFireCopiesShareCostAndShortenActualWeaponInterval()
    {
        Assert.That(System.Enum.TryParse("RapidFire",out R3PartKind kind),Is.True);
        for(int i=0;i<2;i++)
        {
            Assert.That(run.GaragePurchase(kind),Is.True);
            var owned=run.VehicleModel.Inventory.Last(p=>p.Kind==kind);
            var cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            var cell=cells.First(c=>run.VehicleModel.OwnedPlacementError(owned.Id,c,default)==null);
            Assert.That(run.GarageMount(owned.Id,cell),Is.True);
        }
        Assert.That(run.EnterTestRange(),Is.True);
        for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;
        Assert.That(run.CanUseMovement,Is.True);
        var type=System.Type.GetType("RoboRougelike.R3.R3OffensiveDevices, Assembly-CSharp");
        Assert.That(type,Is.Not.Null);var component=run.Player.GetComponent(type);Assert.That(component,Is.Not.Null);
        var activate=type.GetMethod("TryActivateRapidFire");Assert.That(activate,Is.Not.Null);
        var bank=run.Player.GetComponent<VehicleEnergyBank>();
        Assert.That(bank.TrySpend(bank.Current-20),Is.True);
        Assert.That((int)activate.Invoke(component,null),Is.Zero);
        Assert.That(bank.Current,Is.EqualTo(20).Within(.01f));bank.Recharge(100);float before=bank.Current;
        Assert.That((int)activate.Invoke(component,null),Is.EqualTo(2));
        Assert.That(bank.Current,Is.EqualTo(before-30).Within(.01f));
        var effects=run.Player.GetComponent<R3MountedBonusEffects>();Assert.That(effects.EffectiveStats.FireInterval,Is.EqualTo(.6f).Within(.001f));
        Assert.That(run.Player.Controller.WeaponInterval(RoboRougelike.R4.R4WeaponKind.Basic),Is.EqualTo(.38f*.6f).Within(.001f));
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun);
        Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(gun),Is.True);
        float firedAt=Time.time;assembly.FireGroup(false);
        Assert.That(gun.NextFireTime-firedAt,Is.EqualTo(.38f*.6f).Within(.001f),"The fired physical gun must use the reduced interval.");
        var copy=run.VehicleModel.DeepCopy();copy.Parts.RemoveAll(p=>p.Kind==kind);float now=Time.time;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(copy.MakeCheckpoint(now),R3VehicleKind.Light,copy.Economy,now,out var restored,out var error),Is.True,error);
        var ready=typeof(R3VehicleModel).GetProperty("RapidFireReadyAt");Assert.That(ready,Is.Not.Null);
        Assert.That((float)ready.GetValue(restored),Is.GreaterThan(now));
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==kind);
        part.gameObject.SetActive(false);yield return null;
        Assert.That(effects.EffectiveStats.FireInterval,Is.EqualTo(.8f).Within(.001f));
    }
    [UnityTest] public IEnumerator RapidFireAndOverdriveHaveIndependentSlotsAndCooldowns()
    {
        foreach(var kind in new[]{R3PartKind.Overdrive,R3PartKind.RapidFire})
        {
            Assert.That(run.GaragePurchase(kind),Is.True);
            var part=run.VehicleModel.Inventory.Last(p=>p.Kind==kind);
            var cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            Assert.That(run.GarageMount(part.Id,cells.First(c=>run.VehicleModel.OwnedPlacementError(part.Id,c,default)==null)),Is.True);
        }
        Assert.That(run.VehicleModel.ActiveSlotLabel(R3PartKind.Overdrive),Is.Not.EqualTo(run.VehicleModel.ActiveSlotLabel(R3PartKind.RapidFire)));
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;
        Assert.That(run.CanUseMovement,Is.True);
        var devices=run.Player.GetComponent<R3OffensiveDevices>();var effects=run.Player.GetComponent<R3MountedBonusEffects>();
        Assert.That(devices.TryActivate(),Is.EqualTo(1));Assert.That(devices.TryActivateRapidFire(),Is.EqualTo(1));
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1.2f).Within(.001f));
        Assert.That(effects.EffectiveStats.FireInterval,Is.EqualTo(.8f).Within(.001f));
        Assert.That(run.VehicleModel.OverdriveReadyAt,Is.GreaterThan(Time.time));
        Assert.That(run.VehicleModel.RapidFireReadyAt,Is.GreaterThan(Time.time));
        yield return null;
    }
    [UnityTest] public IEnumerator DoubleShotOverdriveAddsOneProjectileToPassiveShotWithoutRecursion()
    {
        Assert.That(System.Enum.TryParse("DoubleShotOverdrive",out R3PartKind boostKind),Is.True);
        foreach(var kind in new[]{R3PartKind.DoubleShot,boostKind})
        {
            Assert.That(run.GaragePurchase(kind),Is.True);
            var part=run.VehicleModel.Inventory.Last(p=>p.Kind==kind);
            var cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            Assert.That(run.GarageMount(part.Id,cells.First(c=>run.VehicleModel.OwnedPlacementError(part.Id,c,default)==null)),Is.True);
        }
        run.VehicleModel.Economy.DoubleShotChancePerUnit=1;
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;
        Assert.That(run.CanUseMovement,Is.True);
        var devices=run.Player.GetComponent<R3OffensiveDevices>();
        var activate=typeof(R3OffensiveDevices).GetMethod("TryActivateDoubleShotOverdrive");Assert.That(activate,Is.Not.Null);
        var bank=run.Player.GetComponent<VehicleEnergyBank>();float before=bank.Current;
        Assert.That((int)activate.Invoke(devices,null),Is.EqualTo(1));Assert.That(bank.Current,Is.EqualTo(before-15).Within(.001f));
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun);
        run.Player.GetComponent<Rigidbody>().isKinematic=true;
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*4;
        var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(1000,false);
        Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(gun),Is.True);assembly.FireGroup(false);
        Assert.That(bank.Current,Is.EqualTo(before-15).Within(.01f),"Extra projectiles do not spend additional energy.");
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>880.01f&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(880).Within(.1f),"One original plus one passive and one active extra, without recursion.");
        var copy=run.VehicleModel.DeepCopy();copy.Parts.RemoveAll(p=>p.Kind==boostKind);float now=Time.time;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(copy.MakeCheckpoint(now),R3VehicleKind.Light,copy.Economy,now,out var restored,out var error),Is.True,error);
        Assert.That(restored.DoubleShotOverdriveReadyAt,Is.GreaterThan(now),"Cooldown survives removing every active copy.");
        var booster=assembly.Parts.Single(p=>p.Kind==boostKind);booster.gameObject.SetActive(false);
        Assert.That(assembly.RollDoubleShotExtras(8),Is.EqualTo(1),"Losing the active device immediately removes only its contribution.");
        Object.Destroy(target);
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
