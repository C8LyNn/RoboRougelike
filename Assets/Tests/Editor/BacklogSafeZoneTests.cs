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
        var enter=typeof(R3VehicleModel).GetMethod("TryEnterSafeZone");
        Assert.That(enter,Is.Not.Null,"The safe-zone visit must be a one-time model transaction");
        Assert.That((bool)enter.Invoke(model,new object[]{1}),Is.False,"A regular stage must not grant a safe-zone refill");
        Assert.That((bool)enter.Invoke(model,new object[]{2}),Is.True);
        Assert.That(model.Energy.Current,Is.EqualTo(model.Energy.Maximum));Assert.That(model.Charges.Current("safe-test"),Is.EqualTo(3));
        Assert.That(model.RepairPulseReadyAt,Is.Zero);Assert.That(armor.Health,Is.EqualTo(.65f).Within(.001));
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
        string damagedId=null;
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
        Assert.That(physical.Damageable.CurrentHp/physical.Damageable.MaxHp,Is.EqualTo(.65f).Within(.001));
        Assert.That(run.Garage.Message,Does.Contain("Bezpieczna strefa"));
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}
        if(!string.IsNullOrEmpty(technologyPath)&&System.IO.File.Exists(technologyPath))System.IO.File.Delete(technologyPath);
    }
}
