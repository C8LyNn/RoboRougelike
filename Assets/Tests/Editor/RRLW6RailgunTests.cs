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
using RoboRougelike.WPN002Railgun;
using RogueMachine.P12PipelineSafe;

public class RRLW6RailgunModelTests
{
    [Test] public void RailgunPenetrationSettingHasNoArtificialTwentyFourPartCeiling()
    {
        var config=new RailgunConfig{maximumDistinctParts=60};Assert.That(config.Snapshot().MaxDistinctParts,Is.EqualTo(60));
        var economy=new R3GarageEconomy();var maximum=typeof(R3GarageEconomy).GetField("RailgunMaxPenetratedParts");
        var energy=typeof(R3GarageEconomy).GetField("RailgunInitialPenetrationEnergy");
        Assert.That(maximum,Is.Not.Null);Assert.That(energy,Is.Not.Null);
        var defaults=Resources.Load<R1RunDefinition>("R1/Run").GaragePrices;
        Assert.That(maximum.GetValue(defaults),Is.EqualTo(6));Assert.That(energy.GetValue(defaults),Is.EqualTo(100f));
        maximum.SetValue(economy,60);energy.SetValue(economy,300f);
        var copy=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light),economy).DeepCopy().Economy;
        Assert.That(maximum.GetValue(copy),Is.EqualTo(60));Assert.That(energy.GetValue(copy),Is.EqualTo(300f));
    }
    [Test] public void RailgunRangeSupportsLargeFiniteValuesAndConfigurationCopy()
    {
        Assert.That(new RailgunConfig{maximumRange=2500}.Snapshot().Range,Is.EqualTo(2500));
        var economy=new R3GarageEconomy();var range=typeof(R3GarageEconomy).GetField("RailgunRange");Assert.That(range,Is.Not.Null);
        range.SetValue(economy,2500f);var copy=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light),economy).DeepCopy();
        Assert.That(range.GetValue(copy.Economy),Is.EqualTo(2500f));
    }
    [Test] public void RailgunCatalogAndRecoveredResourcesUseExistingPaidRepair()
    {
        Assert.That(Enum.TryParse("Railgun",out R4WeaponKind kind),Is.True,"WPN002 missing from live catalog");
        Assert.That((int)kind,Is.EqualTo(4));var spec=new RailgunConfig().Snapshot();
        Assert.That(R4WeaponDefinition.Get(kind).Speed,Is.EqualTo(spec.Speed));
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        var ledger=new R4SalvageLedger(3);
        ledger.Record("rail-enemy",new[]{new R4SalvageItem("rail-id","enemy rail",R3PartKind.Gun,kind,0,4)});
        Assert.That(ledger.Claim(model,new[]{"rail-id"}),Is.True);
        Assert.That(model.Inventory.Single().Health,Is.EqualTo(1));model.Inventory.Single().Health=0; // Subsequent own damage.
        model.Award(500);int before=model.Scrap;
        Assert.That(model.RepairInventory("rail-id"),Is.True);
        Assert.That(model.Scrap,Is.EqualTo(before-23));
        var copy=model.DeepCopy().Inventory.Single();Assert.That(copy.Weapon,Is.EqualTo(kind));
        Assert.That(copy.Ammo,Is.EqualTo(4));Assert.That(copy.Health,Is.EqualTo(1));
    }
}
public class RRLW6RailgunPhysicalTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;run.StartRun();run.ChooseRoute(0);
        while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);
    }
    [UnityTest] public IEnumerator MountedRailgunPenetratesDistinctHpPartsOnceWithOneAmmoDebit()
    {
        Assert.That(Enum.TryParse("Railgun",out R4WeaponKind kind),Is.True,"Live Railgun is absent");
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=kind;
        var run=R1RunController.Instance;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        var gun=vehicle.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Id==state.Id);
        var module=gun.GetComponent<RailgunModule>();Assert.That(module,Is.Not.Null,"Bind original WPN002 on actual physical gun");
        var targets=new P12PipelineSafeDamageable[2];
        for(int i=0;i<2;i++)
        {
            var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.transform.position=gun.Muzzle.position+gun.Muzzle.forward*(6+i*3);
            item.transform.localScale=new Vector3(2,2,.4f);targets[i]=item.AddComponent<P12PipelineSafeDamageable>();targets[i].Configure(500,false);
            if(i==0){var extra=new GameObject("second collider, same HP");extra.transform.SetParent(item.transform,false);extra.AddComponent<BoxCollider>();}
        }
        Physics.SyncTransforms();Assert.That(module.TryFire(),Is.True);Assert.That(state.Ammo,Is.EqualTo(11));
        Assert.That(module.TryFire(),Is.False);Assert.That(state.Ammo,Is.EqualTo(11));
        Assert.That(UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Count(p=>p.PartId==state.Id),Is.EqualTo(1));
        float end=Time.time+.3f;while(Time.time<end)yield return null;
        float first=500-targets[0].CurrentHp,second=500-targets[1].CurrentHp;
        Assert.That(first,Is.InRange(50f,65f));Assert.That(second,Is.GreaterThan(0).And.LessThan(first));
        Assert.That(first,Is.LessThan(75),"Multi-collider logical part must receive one impact");
        gun.Damageable.ApplyDamage(10000,gun.transform.position,Vector3.up);
        Assert.That(module.TryFireAtTime(Time.time+10),Is.False,"Destroyed gun cannot emit or spend ammo");
    }
    [UnityTest] public IEnumerator ConfiguredMountedRailgunCanReachThirtyDistinctTargets()
    {
        var economy=new R3GarageEconomy();
        var maximum=typeof(R3GarageEconomy).GetField("RailgunMaxPenetratedParts");var energy=typeof(R3GarageEconomy).GetField("RailgunInitialPenetrationEnergy");
        Assert.That(maximum,Is.Not.Null);Assert.That(energy,Is.Not.Null);maximum.SetValue(economy,30);energy.SetValue(economy,200f);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard),economy);
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=R4WeaponKind.Railgun;
        var run=R1RunController.Instance;var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;
        var gun=vehicle.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Id==state.Id);
        var targets=new P12PipelineSafeDamageable[30];
        for(int i=0;i<targets.Length;i++)
        {
            var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.transform.position=gun.Muzzle.position+gun.Muzzle.forward*(3+i*.8f);
            item.transform.localScale=new Vector3(1,1,.25f);targets[i]=item.AddComponent<P12PipelineSafeDamageable>();targets[i].Configure(20,false);
        }
        Physics.SyncTransforms();Assert.That(gun.GetComponent<RoboRougelike.WeaponIntegration.RRLRailgunBinding>().Fire(),Is.True);
        float deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&targets.Any(p=>p!=null&&!p.IsDead))yield return null;
        Assert.That(targets.Count(p=>p==null||p.IsDead),Is.EqualTo(30),"Each distinct target should be hit while energy remains.");
        foreach(var target in targets)if(target!=null)UnityEngine.Object.Destroy(target.gameObject);
        if(vehicle!=null)UnityEngine.Object.Destroy(vehicle.gameObject);yield return null;
    }
    [UnityTest] public IEnumerator ConfiguredRailgunCanHitTargetBeyondOldRangeCeiling()
    {
        var economy=new R3GarageEconomy();var range=typeof(R3GarageEconomy).GetField("RailgunRange");Assert.That(range,Is.Not.Null);
        range.SetValue(economy,2500f);economy.RailgunInitialPenetrationEnergy=1000;
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard),economy);
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=R4WeaponKind.Railgun;
        var run=R1RunController.Instance;var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,10,100),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;var gun=vehicle.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Id==state.Id);
        var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.transform.position=gun.Muzzle.position+gun.Muzzle.forward*1600;item.transform.localScale=new Vector3(5,5,1);
        var target=item.AddComponent<P12PipelineSafeDamageable>();target.Configure(500,false);Physics.SyncTransforms();
        var binding=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLRailgunBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(binding.Module.CurrentSpec.Range,Is.EqualTo(2500));
        var projectile=UnityEngine.Object.FindObjectsByType<RoboRougelike.WeaponIntegration.RRLRailPenetration>(FindObjectsSortMode.None).Single();
        projectile.Step(20f);Assert.That(target.CurrentHp,Is.LessThan(500));
        UnityEngine.Object.DestroyImmediate(vehicle.gameObject);
        Assert.DoesNotThrow(()=>projectile.Step(.02f));Assert.That(projectile.gameObject.activeSelf,Is.False);
        if(item!=null)UnityEngine.Object.Destroy(item);yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
