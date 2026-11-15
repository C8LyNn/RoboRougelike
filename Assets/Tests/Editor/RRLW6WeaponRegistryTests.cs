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
using RoboRougelike.WPN001Shotgun;
using RogueMachine.P12PipelineSafe;

public class RRLW6WeaponRegistryTests
{
    [Test] public void ShotgunPelletDamageCanExceedOldCapAndConfigurationCopies()
    {
        Assert.That(new ShotgunConfig{damagePerPellet=2000}.Snapshot().DamagePerPellet,Is.EqualTo(2000));
        Assert.That(new ShotgunConfig{damagePerPellet=float.NaN}.Snapshot().DamagePerPellet,Is.EqualTo(9));
        Assert.That(new ShotgunConfig{damagePerPellet=-1}.Snapshot().DamagePerPellet,Is.Zero);
        float extreme=new ShotgunConfig{damagePerPellet=float.MaxValue}.Snapshot().DamagePerPellet;
        Assert.That(extreme,Is.EqualTo(float.MaxValue));
        Assert.That(R3MountedBonusEffects.SafeDamageProduct(extreme,2),Is.EqualTo(float.MaxValue),"A later multiplier must not turn finite damage into infinity.");
        var field=typeof(R3GarageEconomy).GetField("ShotgunDamagePerPellet");Assert.That(field,Is.Not.Null);
        Assert.That(field.GetValue(Resources.Load<R1RunDefinition>("R1/Run").GaragePrices),Is.EqualTo(9f));
        var economy=new R3GarageEconomy();field.SetValue(economy,2000f);
        var copy=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light),economy).DeepCopy().Economy;
        Assert.That(field.GetValue(copy),Is.EqualTo(2000f));
    }
    [Test] public void ShotgunIsAnExplicitProfileAfterUnchangedLegacyOrdinals()
    {
        Assert.That((int)R4WeaponKind.Basic,Is.EqualTo(0));
        Assert.That((int)R4WeaponKind.Automatic,Is.EqualTo(1));
        Assert.That((int)R4WeaponKind.Heavy,Is.EqualTo(2));
        Assert.That(Enum.TryParse("Shotgun",out R4WeaponKind kind),Is.True,"WPN001 is not registered in the real weapon catalog");
        var profile=R4WeaponDefinition.Get(kind);
        Assert.That(profile.Damage,Is.EqualTo(new ShotgunConfig().Snapshot().DamagePerPellet));
        Assert.That(profile.Interval,Is.EqualTo(.85f).Within(.001f));
        Assert.That(profile.Speed,Is.EqualTo(58));
        Assert.That(profile.Name,Does.Contain("Shotgun"));
    }

    [Test] public void RecoveredShotgunRetainsIdentityAndAmmoThroughPaidRebuildAndCopy()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        var ledger=new R4SalvageLedger(3);
        Assert.That(ledger.Record("enemy",new[]{new R4SalvageItem("real-shotgun","enemy gun",R3PartKind.Gun,R4WeaponKind.Shotgun,0,5)}),Is.True);
        Assert.That(ledger.Claim(model,new[]{"real-shotgun"}),Is.True);
        var recovered=model.Inventory.Single();
        Assert.That(recovered.Ammo,Is.EqualTo(5));Assert.That(recovered.Health,Is.EqualTo(1));
        recovered.Health=0; // Subsequent own damage continues to require paid repair.
        model.Award(45);int before=model.Scrap;
        Assert.That(model.RepairInventory(recovered.Id),Is.True);
        Assert.That(model.Scrap,Is.EqualTo(before-16));
        var copy=model.DeepCopy().Inventory.Single();
        Assert.That(copy.Id,Is.EqualTo("real-shotgun"));Assert.That(copy.Weapon,Is.EqualTo(R4WeaponKind.Shotgun));
        Assert.That(copy.Health,Is.EqualTo(1));Assert.That(copy.Ammo,Is.EqualTo(5));
    }

}

public class RRLW6ShotgunPhysicalTests
{
    [UnityTest] public IEnumerator ConfiguredShotgunDealsDamageAboveOldPelletCap()
    {
        var field=typeof(R3GarageEconomy).GetField("ShotgunDamagePerPellet");Assert.That(field,Is.Not.Null);
        var economy=new R3GarageEconomy();field.SetValue(economy,2000f);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard),economy);
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=R4WeaponKind.Shotgun;
        var run=R1RunController.Instance;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,10,100),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;
        var gun=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*6;
        target.transform.localScale=new Vector3(4,4,1);var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000000,false);
        Physics.SyncTransforms();
        try
        {
            var binding=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>();Assert.That(binding.Fire(),Is.True);
            Assert.That(binding.Module.CurrentSpec.DamagePerPellet,Is.EqualTo(2000));
            float deadline=Time.realtimeSinceStartup+5;
            while(1000000f-hp.CurrentHp<15999f&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(1000000f-hp.CurrentHp,Is.EqualTo(16000f).Within(1f),"All eight independent pellets must retain their configured damage.");
        }
        finally{if(target!=null)UnityEngine.Object.Destroy(target);if(vehicle!=null)UnityEngine.Object.Destroy(vehicle.gameObject);}
    }
    [Test] public void ShotgunLongRangeSettingsRemainFiniteAndCopy()
    {
        var spec=new RoboRougelike.WPN001Shotgun.ShotgunConfig{maxTravelDistance=1200,projectileLifetime=25}.Snapshot();
        Assert.That(spec.MaxTravelDistance,Is.EqualTo(1200));Assert.That(spec.ProjectileLifetime,Is.EqualTo(25));
        var economy=new R3GarageEconomy();var range=typeof(R3GarageEconomy).GetField("ShotgunRange");Assert.That(range,Is.Not.Null);
        Assert.That(range.GetValue(Resources.Load<R1RunDefinition>("R1/Run").GaragePrices),Is.EqualTo(40f));
        var invalid=new RoboRougelike.WPN001Shotgun.ShotgunConfig{maxTravelDistance=float.NaN,projectileLifetime=float.PositiveInfinity}.Snapshot();
        Assert.That(invalid.MaxTravelDistance,Is.EqualTo(40));Assert.That(invalid.ProjectileLifetime,Is.EqualTo(2));
        range.SetValue(economy,1200f);var copy=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light),economy).DeepCopy();
        Assert.That(range.GetValue(copy.Economy),Is.EqualTo(1200));
    }
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        Assert.That(R1RunController.Instance?.Progress,Is.Not.Null);
        var run=R1RunController.Instance;
        Assert.That(run.StartRun(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        while(run.Transitioning)yield return null;
        run.Encounter.SetCombatActive(false);
        Assert.That(Time.timeScale,Is.EqualTo(1),"Real combat must advance physics, unlike the paused menu");
    }
    [UnityTest] public IEnumerator MountedShotgunFiresEightPhysicalPelletsAgainstExistingHp()
    {
        var run=R1RunController.Instance;
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        model.Parts.First(p=>p.Kind==R3PartKind.Gun).Weapon=(R4WeaponKind)3;
        var palette=run.GetComponent<R3VisualPalette>();
        // Catalog admission is part of the contract; never silently clamp a new ID to Heavy.
        Assert.That(Enum.IsDefined(typeof(R4WeaponKind),3),Is.True,"Shotgun catalog entry missing");
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(100,1,100),Quaternion.identity);
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();
        Assert.That(assembly,Is.Not.Null,"Built vehicle root="+vehicle.gameObject.name+" components="+
            string.Join(",",vehicle.GetComponents<Component>().Select(c=>c==null?"missing":c.GetType().FullName)));
        Assert.That(assembly.Parts,Is.Not.Null,"Assembly part collection after Build");
        Assert.That(assembly.Parts,Is.Not.Empty,"Assembly part collection after Build");
        var gun=assembly.Parts.FirstOrDefault(p=>p!=null&&p.Kind==R3PartKind.Gun);
        Assert.That(gun,Is.Not.Null,"Physical gun missing from new assembly");
        var shotgun=gun.GetComponent<ShotgunModule>();
        Assert.That(shotgun,Is.Not.Null,"Original WPN001 module must be bound to the real gun anchor");
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.name="W6 actual HP target";
        target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*6;
        target.transform.localScale=new Vector3(4,4,1);
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);
        Physics.SyncTransforms();
        var binding=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>();
        Assert.That(binding.State.Ammo,Is.EqualTo(12));
        Assert.That(binding.Fire(),Is.True);Assert.That(binding.State.Ammo,Is.EqualTo(11));
        Assert.That(UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None)
            .Count(p=>p.PartId==gun.Id),Is.EqualTo(8));
        Assert.That(binding.Fire(),Is.False,"No second volley during original cooldown");
        Assert.That(binding.State.Ammo,Is.EqualTo(11),"A rejected shot must not consume another salvo");
        float startTime=Time.time,startFixed=Time.fixedTime;
        var fired=UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>p.PartId==gun.Id).ToArray();
        string starts=string.Join(";",fired.Take(2).Select(p=>$"{p.transform.position} v={p.GetComponent<Rigidbody>().linearVelocity}"));
        // EditMode's enumerator does not schedule WaitForSeconds even after EnterPlayMode.
        // Advance actual game frames, as the existing R6 physics fixtures do.
        while(Time.time<startTime+.3f)yield return null;
        string ends=string.Join(";",fired.Take(2).Select(p=>p==null?"destroyed":$"{p.transform.position} v={p.GetComponent<Rigidbody>().linearVelocity}"));
        Assert.That(hp.CurrentHp,Is.EqualTo(428).Within(.1f),$"Eight9-damage pellets; physics={Physics.simulationMode}, time={startTime}->{Time.time}, fixed={startFixed}->{Time.fixedTime}, target={target.transform.position}, ignoreLayers={Physics.GetIgnoreLayerCollision(0,0)}, starts={starts}, ends={ends}");
        gun.Damageable.ApplyDamage(10000,gun.transform.position,Vector3.up);
        Assert.That(shotgun.TryFire(),Is.False,"Destroyed or detached weapon cannot fire");
    }
    [UnityTest] public IEnumerator ConfiguredShotgunCanHitBeyondOldRangeCeiling()
    {
        var range=typeof(R3GarageEconomy).GetField("ShotgunRange");Assert.That(range,Is.Not.Null);
        var run=R1RunController.Instance;var economy=new R3GarageEconomy();range.SetValue(economy,1200f);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard),economy);
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=R4WeaponKind.Shotgun;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,10,100),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;var gun=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*600;
        target.transform.localScale=new Vector3(15,15,1);var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);Physics.SyncTransforms();
        var binding=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(binding.Module.CurrentSpec.MaxTravelDistance,Is.EqualTo(1200));Assert.That(binding.Module.CurrentSpec.ProjectileLifetime,Is.GreaterThan(20));
        foreach(var pellet in UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>p.PartId==state.Id))
            pellet.GetComponent<Rigidbody>().linearVelocity=gun.Muzzle.forward*1000;
        float deadline=Time.realtimeSinceStartup+5;while(hp.CurrentHp>=500&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(hp.CurrentHp,Is.LessThan(500));if(target!=null)UnityEngine.Object.Destroy(target);if(vehicle!=null)UnityEngine.Object.Destroy(vehicle.gameObject);yield return null;
    }
    [UnityTest] public IEnumerator ConsecutiveShotgunVolleysReusePelletsAndBothDamageTarget()
    {
        var run=R1RunController.Instance;
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=R4WeaponKind.Shotgun;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,10,100),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;
        var gun=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*6;target.transform.localScale=new Vector3(4,4,1);
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);Physics.SyncTransforms();
        yield return null; // Finish deferred destruction of builder helper colliders.
        var binding=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>();
        Assert.That(binding.Fire(),Is.True);
        var first=UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>p.PartId==state.Id).Select(p=>p.gameObject.GetInstanceID()).ToArray();
        Assert.That(first.Length,Is.EqualTo(8));
        float deadline=Time.realtimeSinceStartup+4;
        while((hp.CurrentHp>428.1f||Time.time<binding.State.DeviceReadyAt+.02f)&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(hp.CurrentHp,Is.EqualTo(428).Within(.1));
        Assert.That(binding.Fire(),Is.True);
        var second=UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>p.PartId==state.Id).Select(p=>p.gameObject.GetInstanceID()).ToArray();
        Assert.That(second.Length,Is.EqualTo(8));
        Assert.That(second.Intersect(first).Count(),Is.GreaterThan(0),"A later real volley can reuse retired pellets.");
        deadline=Time.realtimeSinceStartup+4;while(hp.CurrentHp>356.1f&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(hp.CurrentHp,Is.EqualTo(356).Within(.1),"Reused pellets must all damage the target.");
        UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(vehicle.gameObject);yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}
    }
}
