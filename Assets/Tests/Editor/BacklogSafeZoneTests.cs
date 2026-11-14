using System.Linq;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R1;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogSafeZoneTests
{
    [Test] public void SafeZoneRefillsPartialMagazinesEvenWhenNotReloading()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var shotgun=model.Parts.First(p=>p.Kind==R3PartKind.Gun);shotgun.Weapon=RoboRougelike.R4.R4WeaponKind.Shotgun;shotgun.Ammo=3;
        var rail=new R3PartState(default,R3PartKind.Gun){Weapon=RoboRougelike.R4.R4WeaponKind.Railgun,Ammo=4};
        var rocket=new R3PartState(default,R3PartKind.Gun){Weapon=RoboRougelike.R4.R4WeaponKind.RocketLauncher,Ammo=5};
        var defense=new R3PartState(default,R3PartKind.PointDefense,0){Ammo=7};
        Assert.That(model.ReceiveSalvage(new[]{rail,rocket,defense}),Is.True);
        Assert.That(model.TryEnterSafeZone(2),Is.True);
        Assert.That(shotgun.Ammo,Is.EqualTo(RoboRougelike.WeaponIntegration.RRLShotgunBinding.MagazineSalvos));
        Assert.That(model.Inventory.Single(p=>p.Id==rail.Id).Ammo,Is.EqualTo(RoboRougelike.WeaponIntegration.RRLRailgunBinding.Magazine));
        Assert.That(model.Inventory.Single(p=>p.Id==rocket.Id).Ammo,Is.EqualTo(RoboRougelike.WeaponIntegration.RRLRocketBinding.Magazine));
        var storedDefense=model.Inventory.Single(p=>p.Id==defense.Id);Assert.That(storedDefense.Ammo,Is.EqualTo(40));Assert.That(storedDefense.Health,Is.Zero);
    }
    [Test] public void SafeZoneRefillsShieldChargeAtItsTierWithoutRevivingWrecks()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var working=new R3PartState(default,R3PartKind.DirectionalShield,.5f){Tier=2,DefenseCharge=17,DefenseRechargeAge=1};
        var wreck=new R3PartState(default,R3PartKind.DirectionalShield,0){Tier=3,DefenseCharge=23,DefenseRechargeAge=0};
        Assert.That(model.ReceiveSalvage(new[]{working,wreck}),Is.True);
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.OwnedPlacementError(working.Id,c,default)==null);
        Assert.That(model.Mount(working.Id,cell),Is.True);Assert.That(model.TryEnterSafeZone(2),Is.True);
        var installed=model.Parts.Single(p=>p.Id==working.Id);var stored=model.Inventory.Single(p=>p.Id==wreck.Id);
        Assert.That(installed.DefenseCharge,Is.EqualTo(200));Assert.That(stored.DefenseCharge,Is.EqualTo(400));
        Assert.That(installed.Health,Is.EqualTo(.75f).Within(.001));Assert.That(stored.Health,Is.Zero);
        Assert.That(model.TryEnterSafeZone(2),Is.False,"Reopening the same zone must not grant a second refill.");
        var checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==working.Id).DefenseCharge,Is.EqualTo(200));
        Assert.That(restored.Inventory.Single(p=>p.Id==wreck.Id).DefenseCharge,Is.EqualTo(400));
    }
    [Test] public void LegacyCheckpointWithoutJumpTimerLoadsAndRejectsInvalidTimer()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        string json=JsonUtility.ToJson(model.MakeCheckpoint(0));
        string legacy=System.Text.RegularExpressions.Regex.Replace(json,",\"(jumpRemaining|blinkRemaining)\":[^,}]+","");
        Assert.That(legacy,Does.Not.Contain("\"jumpRemaining\""));
        Assert.That(legacy,Does.Not.Contain("\"blinkRemaining\""));
        var checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(legacy);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.JumpReadyAt,Is.Zero);Assert.That(restored.BlinkReadyAt,Is.Zero);
        checkpoint.jumpRemaining=float.NaN;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out _,out _),Is.False);
        checkpoint.jumpRemaining=0;checkpoint.blinkRemaining=float.NaN;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out _,out _),Is.False);
    }
    [Test] public void ScannerSharedCooldownSurvivesSaveAndIsRefilledInSafeZone()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        typeof(R3VehicleModel).GetProperty("ScannerPulseReadyAt").SetValue(model,105f);
        var checkpoint=model.MakeCheckpoint(100);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,200,out var restored,out var error),Is.True,error);
        Assert.That(restored.ScannerPulseReadyAt,Is.EqualTo(205).Within(.001));
        Assert.That(restored.DeepCopy().ScannerPulseReadyAt,Is.EqualTo(205).Within(.001));
        Assert.That(restored.TryEnterSafeZone(2),Is.True);Assert.That(restored.ScannerPulseReadyAt,Is.Zero);
        string legacy=System.Text.RegularExpressions.Regex.Replace(JsonUtility.ToJson(checkpoint),",\"scannerPulseRemaining\":[^,}]+","");
        Assert.That(legacy,Does.Not.Contain("\"scannerPulseRemaining\""));
        var oldCheckpoint=JsonUtility.FromJson<R3ModelCheckpoint>(legacy);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(oldCheckpoint,R3VehicleKind.Light,model.Economy,200,out var old,out error),Is.True,error);
        Assert.That(old.ScannerPulseReadyAt,Is.Zero);
        checkpoint.scannerPulseRemaining=float.NaN;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,200,out _,out _),Is.False);
    }
    [Test] public void StructuralScannerCooldownSurvivesSaveCopyAndSafeZone()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        typeof(R3VehicleModel).GetProperty("StructuralScannerReadyAt").SetValue(model,105f);
        var checkpoint=model.MakeCheckpoint(100);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,200,out var restored,out var error),Is.True,error);
        Assert.That(restored.StructuralScannerReadyAt,Is.EqualTo(205).Within(.001));
        Assert.That(restored.DeepCopy().StructuralScannerReadyAt,Is.EqualTo(205).Within(.001));
        Assert.That(restored.TryEnterSafeZone(2),Is.True);Assert.That(restored.StructuralScannerReadyAt,Is.Zero);
        string legacy=System.Text.RegularExpressions.Regex.Replace(JsonUtility.ToJson(checkpoint),",\"structuralScannerRemaining\":[^,}]+","");
        var oldCheckpoint=JsonUtility.FromJson<R3ModelCheckpoint>(legacy);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(oldCheckpoint,R3VehicleKind.Light,model.Economy,200,out var old,out error),Is.True,error);
        Assert.That(old.StructuralScannerReadyAt,Is.Zero);
        checkpoint.structuralScannerRemaining=float.NaN;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,200,out _,out _),Is.False);
    }
    [Test] public void StructuralScannerBalanceIsConfigurableAndCopied()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        foreach(var pair in new[]{("StructuralScanRange",85f),("StructuralScanEnergy",3f),("StructuralScanCooldown",4f)})
        {
            var field=typeof(R3GarageEconomy).GetField(pair.Item1);Assert.That(field,Is.Not.Null,pair.Item1+" must be configurable.");
            field.SetValue(model.Economy,pair.Item2);
        }
        var copy=model.DeepCopy();
        Assert.That(typeof(R3GarageEconomy).GetField("StructuralScanRange").GetValue(copy.Economy),Is.EqualTo(85f));
        Assert.That(typeof(R3GarageEconomy).GetField("StructuralScanEnergy").GetValue(copy.Economy),Is.EqualTo(3f));
        Assert.That(typeof(R3GarageEconomy).GetField("StructuralScanCooldown").GetValue(copy.Economy),Is.EqualTo(4f));
    }
    [Test] public void SafeZoneRestoresResourcesAndPartiallyRepairsOnlyOnceAcrossCheckpoint()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var armor=model.Parts.First(p=>p.Kind==R3PartKind.Armor);armor.Health=.4f;
        var gun=model.Parts.First(p=>p.Kind==R3PartKind.Gun);gun.Weapon=RoboRougelike.R4.R4WeaponKind.RocketLauncher;gun.Ammo=0;gun.ReloadReadyAt=500;
        var wreck=model.Parts.Where(p=>p.Kind==R3PartKind.Wheel)
            .First(p=>R3VehicleModel.Connected(model.Parts.Where(other=>other!=p&&other.Health>0)));wreck.Health=0;
        model.RefreshEnergyLayout();Assert.That(model.Energy.TrySpend(100),Is.True);
        Assert.That(model.Charges.EnsurePool("safe-test",3),Is.True);Assert.That(model.Charges.TrySpend("safe-test",2),Is.True);
        typeof(R3VehicleModel).GetProperty("RepairPulseReadyAt").SetValue(model,500f);
        typeof(R3VehicleModel).GetProperty("JumpReadyAt").SetValue(model,500f);
        typeof(R3VehicleModel).GetProperty("BlinkReadyAt").SetValue(model,500f);
        var enter=typeof(R3VehicleModel).GetMethod("TryEnterSafeZone");
        Assert.That(enter,Is.Not.Null,"The safe-zone visit must be a one-time model transaction");
        Assert.That((bool)enter.Invoke(model,new object[]{1}),Is.False,"A regular stage must not grant a safe-zone refill");
        Assert.That((bool)enter.Invoke(model,new object[]{2}),Is.True);
        Assert.That(model.Energy.Current,Is.EqualTo(model.Energy.Maximum));Assert.That(model.Charges.Current("safe-test"),Is.EqualTo(3));
        Assert.That(model.RepairPulseReadyAt,Is.Zero);Assert.That(model.JumpReadyAt,Is.Zero);Assert.That(model.BlinkReadyAt,Is.Zero);Assert.That(armor.Health,Is.EqualTo(.65f).Within(.001));
        Assert.That(gun.ReloadReadyAt,Is.Zero);Assert.That(gun.Ammo,Is.EqualTo(RoboRougelike.WeaponIntegration.RRLRocketBinding.Magazine));
        Assert.That(wreck.Health,Is.Zero,"Free repair must not revive an unmounted or disconnected wreck");
        Assert.That((bool)enter.Invoke(model,new object[]{2}),Is.False);Assert.That(armor.Health,Is.EqualTo(.65f).Within(.001));
        var saved=UnityEngine.JsonUtility.FromJson<R3ModelCheckpoint>(UnityEngine.JsonUtility.ToJson(model.MakeCheckpoint(100)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,model.Economy,200,out var restored,out var error),Is.True,error);
        Assert.That((bool)enter.Invoke(restored,new object[]{2}),Is.False,"Loading must not pay the same safe zone twice");
        Assert.That(restored.Parts.Single(p=>p.Id==armor.Id).Health,Is.EqualTo(.65f).Within(.001));
    }
}

public sealed class BacklogSafeZoneGameplayTests
{
    R1RunController run;string technologyPath;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        technologyPath=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"rrl-safezone-tech-"+System.Guid.NewGuid().ToString("N")+".json");
        typeof(R1RunController).GetProperty("Technology").GetSetMethod(true).Invoke(run,new object[]{new R1TechnologyProgress(technologyPath)});
        Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        for(int i=0;i<300&&run.Transitioning;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat));
    }
    [UnityTest] public IEnumerator SecondVictoryOpensGarageWithRefilledVehicleAndPartialRepair()
    {
        string damagedId=null,shieldId=null,defenseId=null;
        for(int stage=1;stage<=2;stage++)
        {
            Assert.That(run,Is.Not.Null);Assert.That(run.Progress,Is.Not.Null);Assert.That(run.Encounter,Is.Not.Null);Assert.That(run.Encounter.Enemies,Is.Not.Null);
            Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat));Assert.That(run.Player,Is.Not.Null);run.Encounter.SetCombatActive(false);
            if(stage==2)
            {
                var assembly=run.Player.GetComponent<R3VehicleAssembly>();
                var part=assembly.Parts.First(p=>p.Kind==R3PartKind.Armor&&p.Operational);
                damagedId=part.Id;part.Damageable.RestoreHealthRatio(.4f);
                var bank=run.Player.GetComponent<VehicleEnergyBank>();Assert.That(bank.TrySpend(100),Is.True);
                var shield=assembly.Parts.Single(p=>p.Id==shieldId).GetComponent<RoboRougelike.Defenses.DirectionalShield.DirectionalShieldGenerator>();
                Assert.That(shield.Capacity,Is.EqualTo(200).Within(.001));shield.RestoreState(17,0);
                var defense=assembly.Parts.Single(p=>p.Id==defenseId).GetComponent<RoboRougelike.WeaponIntegration.RRLPointDefenseBinding>();
                defense.Controller.RestoreState(7,4);defense.CaptureState();
                Assert.That(run.VehicleModel.Charges.EnsurePool("safe-test",3),Is.True);
                Assert.That(run.VehicleModel.Charges.TrySpend("safe-test",2),Is.True);
                typeof(R3VehicleModel).GetProperty("RepairPulseReadyAt").SetValue(run.VehicleModel,Time.time+100);
            }
            foreach(var enemy in run.Encounter.Enemies.ToArray())
            {
                Assert.That(enemy,Is.Not.Null);Assert.That(enemy.Core,Is.Not.Null);enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.up);
            }
            for(int i=0;i<600&&run.Progress.State!=R1RunState.Salvage;i++)
            {
                if(run.PickupGraceRemaining>0)typeof(R1RunController).GetField("pickupGraceDeadline",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(run,Time.time);
                yield return null;
            }
            Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
            Assert.That(run.ConfirmRewards(System.Array.Empty<string>(),new[]{0}),Is.True);
            Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
            if(stage==1)
            {
                Assert.That(run.VehicleModel.SafeZoneLastCompleted,Is.Zero);
                var shield=new R3PartState(default,R3PartKind.DirectionalShield){Tier=2};shieldId=shield.Id;
                Assert.That(run.VehicleModel.ReceiveSalvage(new[]{shield}),Is.True);
                var shieldCell=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                    .First(c=>run.VehicleModel.OwnedPlacementError(shield.Id,c,default)==null);
                Assert.That(run.GarageMount(shield.Id,shieldCell),Is.True);
                var defense=new R3PartState(default,R3PartKind.PointDefense);defenseId=defense.Id;
                Assert.That(run.VehicleModel.ReceiveSalvage(new[]{defense}),Is.True);
                var defenseCell=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                    .First(c=>run.VehicleModel.OwnedPlacementError(defense.Id,c,default)==null);
                Assert.That(run.GarageMount(defense.Id,defenseCell),Is.True);
                Assert.That(run.StartNextEncounter()&&run.ChooseRoute(0),Is.True);
                for(int i=0;i<300&&run.Transitioning;i++)yield return null;
                yield return null;
            }
        }
        Assert.That(run.VehicleModel.SafeZoneLastCompleted,Is.EqualTo(2));
        Assert.That(run.VehicleModel.Energy.Current,Is.EqualTo(run.VehicleModel.Energy.Maximum).Within(.001));
        Assert.That(run.VehicleModel.Charges.Current("safe-test"),Is.EqualTo(3));Assert.That(run.VehicleModel.RepairPulseReadyAt,Is.Zero);
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==damagedId).Health,Is.EqualTo(.65f).Within(.001));
        var physical=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==damagedId);
        var restoredShield=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==shieldId)
            .GetComponent<RoboRougelike.Defenses.DirectionalShield.DirectionalShieldGenerator>();
        Assert.That(restoredShield.Charge,Is.EqualTo(200).Within(.001));
        var restoredDefense=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==defenseId)
            .GetComponent<RoboRougelike.WeaponIntegration.RRLPointDefenseBinding>();
        Assert.That(restoredDefense.Controller.Ammunition,Is.EqualTo(40));
        Assert.That(restoredDefense.Controller.CooldownRemaining,Is.Zero);
        Assert.That(physical.Damageable.CurrentHp/physical.Damageable.MaxHp,Is.EqualTo(.65f).Within(.001));
        Assert.That(run.Garage.Message,Does.Contain("Bezpieczna strefa"));
        Assert.That(run.SafeVendorAvailable,Is.True);run.VehicleModel.Award(1000);
        int count=run.VehicleModel.Inventory.Count;var offer=run.VehicleModel.SafeZoneVendorOffers()[0];
        Assert.That(run.GarageBuyVendorOffer(offer.Index),Is.True);
        Assert.That(run.VehicleModel.Inventory.Count-count,Is.EqualTo(run.VehicleModel.GrantQuantity(offer.Kind)));
        Assert.That(run.GarageBuyVendorOffer(offer.Index),Is.False);
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}
        if(!string.IsNullOrEmpty(technologyPath)&&System.IO.File.Exists(technologyPath))System.IO.File.Delete(technologyPath);
    }
}
