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
using RoboRougelike.WeaponIntegration;
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
    [UnityTest] public IEnumerator DenseBlastStillDamagesOneActualPart()
    {
        var owner=new GameObject("Rocket overflow owner");owner.transform.position=new Vector3(1000,1000,1000);
        var body=owner.AddComponent<Rigidbody>();body.isKinematic=true;
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=owner.transform.position+Vector3.forward;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);target.AddComponent<RRLRocketDamageReceiver>();
        for(int i=0;i<10;i++)target.AddComponent<SphereCollider>().radius=.45f;
        var rocketObject=new GameObject("Dense explosion projectile");rocketObject.transform.position=owner.transform.position;
        var rocket=rocketObject.AddComponent<RocketProjectile>();
        var spec=new RocketConfig{maxBlastColliders=8,maxCastHits=8,explosionRadius=3}.Snapshot();
        Assert.That(rocket.TryInitialize(spec,body,owner.transform,owner,null,Vector3.forward),Is.True);Physics.SyncTransforms();
        typeof(RocketProjectile).GetMethod("Resolve",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
            .Invoke(rocket,new object[]{owner.transform.position,Vector3.up,true});
        Assert.That(hp.CurrentHp,Is.LessThan(500),"A full overlap buffer must not cancel the whole real explosion.");
        Assert.That(500-hp.CurrentHp,Is.LessThanOrEqualTo(spec.CenterDamage),"Multiple colliders cannot multiply damage to the same part.");
        if(target!=null)UnityEngine.Object.Destroy(target);if(owner!=null)UnityEngine.Object.Destroy(owner);yield return null;
    }
    [UnityTest] public IEnumerator DenseBlastStillRespectsShield()
    {
        var owner=new GameObject("Dense blast protected owner");owner.transform.position=new Vector3(1000,1000,1000);
        var body=owner.AddComponent<Rigidbody>();body.isKinematic=true;
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=owner.transform.position+Vector3.forward*2;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);target.AddComponent<RRLRocketDamageReceiver>();
        for(int i=0;i<10;i++)target.AddComponent<SphereCollider>().radius=.4f;
        var shield=new GameObject("Dense blast shield");shield.transform.position=target.transform.position;
        shield.AddComponent<SphereCollider>().isTrigger=true;var shieldHp=shield.AddComponent<P12PipelineSafeDamageable>();shieldHp.Configure(500,false);
        shield.AddComponent<R3ShieldDome>().Configure(2,.8f);
        var rocketObject=new GameObject("Dense protected explosion");rocketObject.transform.position=owner.transform.position;
        rocketObject.AddComponent<R3ShieldRocketPayload>();var rocket=rocketObject.AddComponent<RocketProjectile>();
        var spec=new RocketConfig{maxBlastColliders=8,maxCastHits=8,explosionRadius=3}.Snapshot();
        Assert.That(rocket.TryInitialize(spec,body,owner.transform,owner,null,Vector3.forward),Is.True);Physics.SyncTransforms();
        typeof(RocketProjectile).GetMethod("Resolve",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
            .Invoke(rocket,new object[]{owner.transform.position,Vector3.up,true});
        Assert.That(hp.CurrentHp,Is.EqualTo(500),"A saturated query must not bypass the hostile dome.");
        Assert.That(shieldHp.CurrentHp,Is.LessThan(500));Assert.That(500-shieldHp.CurrentHp,Is.LessThanOrEqualTo(spec.CenterDamage));
        if(shield!=null)UnityEngine.Object.Destroy(shield);if(target!=null)UnityEngine.Object.Destroy(target);if(owner!=null)UnityEngine.Object.Destroy(owner);yield return null;
    }
    [UnityTest] public IEnumerator DenseOwnerCollisionSweepStillHitsEnemy()
    {
        var owner=new GameObject("Crowded rocket owner");owner.transform.position=new Vector3(1000,1000,1000);
        var body=owner.AddComponent<Rigidbody>();body.isKinematic=true;
        for(int i=0;i<10;i++)
        {
            var own=GameObject.CreatePrimitive(PrimitiveType.Cube);own.transform.SetParent(owner.transform,false);
            own.transform.localPosition=Vector3.forward*.2f;own.transform.localScale=Vector3.one*.05f;
        }
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=owner.transform.position+Vector3.forward*.34f;
        target.transform.localScale=Vector3.one*.08f;var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);
        target.AddComponent<RRLRocketDamageReceiver>();
        var rocketObject=new GameObject("Crowded sweep projectile");rocketObject.transform.position=owner.transform.position;
        var rocket=rocketObject.AddComponent<RocketProjectile>();
        var spec=new RocketConfig{maxBlastColliders=8,maxCastHits=8,initialSpeed=18,maxSpeed=18,acceleration=0,explosionRadius=3}.Snapshot();
        Assert.That(rocket.TryInitialize(spec,body,owner.transform,owner,null,Vector3.forward),Is.True);Physics.SyncTransforms();
        typeof(RocketProjectile).GetMethod("FixedUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(rocket,null);
        Assert.That(hp.CurrentHp,Is.LessThan(500),"Ignored owner colliders cannot remove a rocket before it reaches the enemy.");
        if(target!=null)UnityEngine.Object.Destroy(target);if(owner!=null)UnityEngine.Object.Destroy(owner);yield return null;
    }
    [UnityTest] public IEnumerator DenseOwnerLaunchOverlapStillHitsEnemy()
    {
        var owner=new GameObject("Crowded launch owner");owner.transform.position=new Vector3(1000,1000,1000);
        var body=owner.AddComponent<Rigidbody>();body.isKinematic=true;
        for(int i=0;i<10;i++)
        {
            var own=GameObject.CreatePrimitive(PrimitiveType.Cube);own.transform.SetParent(owner.transform,false);
            own.transform.localPosition=Vector3.zero;own.transform.localScale=Vector3.one*.05f;
        }
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=owner.transform.position+Vector3.forward*.34f;
        target.transform.localScale=Vector3.one*.08f;var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);
        target.AddComponent<RRLRocketDamageReceiver>();
        var rocketObject=new GameObject("Crowded launch projectile");rocketObject.transform.position=owner.transform.position;
        var rocket=rocketObject.AddComponent<RocketProjectile>();
        var spec=new RocketConfig{maxBlastColliders=8,maxCastHits=8,initialSpeed=18,maxSpeed=18,acceleration=0,explosionRadius=3}.Snapshot();
        Assert.That(rocket.TryInitialize(spec,body,owner.transform,owner,null,Vector3.forward),Is.True);Physics.SyncTransforms();
        typeof(RocketProjectile).GetMethod("FixedUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(rocket,null);
        Assert.That(hp.CurrentHp,Is.LessThan(500),"Owner colliders at the muzzle cannot cancel a valid launch.");
        if(target!=null)UnityEngine.Object.Destroy(target);if(owner!=null)UnityEngine.Object.Destroy(owner);yield return null;
    }
    [UnityTest] public IEnumerator EmptyPlayerLauncherAutomaticallyReloadsWithoutInput()
    {
        var gun=Build(out var state);var binding=gun.GetComponent<RRLRocketBinding>();
        Assert.That(binding,Is.Not.Null);state.Ammo=0;state.ReloadReadyAt=0;
        yield return null;
        Assert.That(state.ReloadReadyAt,Is.GreaterThan(Time.time),"An empty player launcher must start its own timed reload.");
        Assert.That(state.Ammo,Is.Zero);Assert.That(binding.Fire(),Is.False,"Cannot fire while reloading.");
        float until=Time.realtimeSinceStartup+8;
        while(state.ReloadReadyAt>0&&Time.realtimeSinceStartup<until)yield return null;
        Assert.That(state.ReloadReadyAt,Is.Zero);Assert.That(state.Ammo,Is.EqualTo(RRLRocketBinding.Magazine));
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
        var visual=rocket.transform.Find("Rocket visual");Assert.That(visual,Is.Not.Null);
        Assert.That(visual.localScale.y,Is.GreaterThan(visual.localScale.x*2),"Rocket body should be elongated.");
        Assert.That(rocket.transform.Find("Rocket fin 0"),Is.Not.Null,"Rocket must have visible fins.");
        Assert.That(rocket.GetComponentsInChildren<Collider>().Count(c=>c.enabled),Is.EqualTo(1),"Only the existing rocket physics collider may be active.");
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
        Assert.That(ledger.Claim(model,new[]{"rocket-salvage"}),Is.True);
        Assert.That(model.Inventory.Single().Health,Is.EqualTo(1));model.Inventory.Single().Health=0; // Subsequent own damage.
        model.Award(500);
        Assert.That(model.RepairInventory("rocket-salvage"),Is.True);
        var item=model.DeepCopy().Inventory.Single();Assert.That(item.Id,Is.EqualTo("rocket-salvage"));Assert.That(item.Ammo,Is.Zero);
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
