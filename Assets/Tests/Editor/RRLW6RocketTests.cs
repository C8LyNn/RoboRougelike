using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.Mechanics.Rockets;
using RogueMachine.P12PipelineSafe;

public class RRLW6RocketPhysicalTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;run.StartRun();run.ChooseRoute(0);
        while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);
    }
    R3MountedPart Build(out R3PartState state)
    {
        Assert.That(Enum.TryParse("RocketLauncher",out R4WeaponKind kind),Is.True,"WPN004 missing from player catalog");
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        var gunState=model.Parts.First(p=>p.Kind==R3PartKind.Gun);gunState.Weapon=kind;state=gunState;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),R1RunController.Instance.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        return vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==gunState.Id);
    }
    static bool Fire(R3MountedPart gun)
    {
        var binding=gun.GetComponents<MonoBehaviour>().Single(p=>p.GetType().Name=="RRLRocketBinding");
        return (bool)binding.GetType().GetMethod("Fire").Invoke(binding,null);
    }
    [UnityTest] public IEnumerator MountedRocketDamagesRealHpOncePerPartAndConsumesOneRound()
    {
        var gun=Build(out var state);var launcher=gun.GetComponent<RocketLauncher>();Assert.That(launcher,Is.Not.Null);
        var targets=new P12PipelineSafeDamageable[2];
        for(int i=0;i<2;i++)
        {
            var target=GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*6+gun.Muzzle.right*(i*1.8f);
            target.transform.localScale=new Vector3(i==0?2:.5f,2,.5f);
            targets[i]=target.AddComponent<P12PipelineSafeDamageable>();targets[i].Configure(500,false);
            if(i==0){var extra=new GameObject("same-part collider");extra.transform.SetParent(target.transform,false);extra.AddComponent<BoxCollider>();}
        }
        Physics.SyncTransforms();float ownerHp=gun.Damageable.CurrentHp;
        Assert.That(Fire(gun),Is.True);Assert.That(state.Ammo,Is.EqualTo(9));
        Assert.That(Fire(gun),Is.False);Assert.That(state.Ammo,Is.EqualTo(9));
        var rocket=UnityEngine.Object.FindObjectsByType<RocketProjectile>(FindObjectsSortMode.None).Single(p=>p.IsInitialized);
        Assert.That(rocket.gameObject.activeInHierarchy,Is.True,"A real rocket must run physics, not merely be instantiated inactive");
        float end=Time.time+.6f;while(Time.time<end)yield return null;
        Assert.That(500-targets[0].CurrentHp,Is.InRange(50f,60f),"One original AOE budget per logical P12 part");
        Assert.That(500-targets[1].CurrentHp,Is.GreaterThan(0).And.LessThan(500-targets[0].CurrentHp));
        Assert.That(gun.Damageable.CurrentHp,Is.EqualTo(ownerHp),"Owner is excluded from its blast");
        gun.Damageable.ApplyDamage(10000,gun.transform.position,Vector3.up);
        Assert.That(launcher.TryFire(),Is.EqualTo(RocketFireResult.Disabled));
    }
    [UnityTest] public IEnumerator TransitionCleansLiveRocketAndRecoveredLauncherKeepsEmptyAmmo()
    {
        var gun=Build(out var state);Assert.That(Fire(gun),Is.True);
        R1EncounterManager.ClearTransientObjects();
        Assert.That(UnityEngine.Object.FindObjectsByType<RocketProjectile>(FindObjectsSortMode.None).Any(p=>p.IsInitialized),Is.False);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        var ledger=new R4SalvageLedger(3);ledger.Record("rocket-enemy",new[]{new R4SalvageItem("rocket-salvage","enemy",R3PartKind.Gun,state.Weapon,0,0)});
        Assert.That(ledger.Claim(model,new[]{"rocket-salvage"}),Is.True);model.Award(500);
        Assert.That(model.RepairInventory("rocket-salvage"),Is.True);
        var item=model.DeepCopy().Inventory.Single();Assert.That(item.Id,Is.EqualTo("rocket-salvage"));Assert.That(item.Ammo,Is.Zero);
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
