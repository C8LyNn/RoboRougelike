using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.MOVSeries;
using RoboRougelike.Mechanics.Booster;
using RoboRougelike.Mechanics.MOV005OmniDrive;
using RogueMachine.P12PipelineSafe;
using Object=UnityEngine.Object;

public class RRLW34EffectsTests
{
    [UnityTest] public IEnumerator DenseFriendlyProjectilesDoNotHideWallsOrTargetsFromDeviceAim()
    {
        var owner=new GameObject("Device sight owner");owner.transform.SetParent(fixtures.transform);
        var shooterObject=GameObject.CreatePrimitive(PrimitiveType.Cube);shooterObject.transform.SetParent(fixtures.transform);
        shooterObject.transform.position=new Vector3(1000,1000,1000);shooterObject.AddComponent<Rigidbody>().isKinematic=true;
        var shooter=shooterObject.AddComponent<R3FlyingDrone>();shooter.ConfigureTurret(owner.transform,1,10,30);shooter.enabled=false;
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.SetParent(fixtures.transform);
        target.transform.position=shooterObject.transform.position+Vector3.forward*10;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        var enemy=target.AddComponent<P12PipelineSafeEnemyTurret>();enemy.enabled=false;enemy.Configure(owner.transform,null,null,target.transform,hp,null);
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var clear=typeof(R3FlyingDrone).GetMethod("ClearShot",flags);
        var buffer=typeof(R3FlyingDrone).GetField("sightHits",flags);
        Assert.That(clear,Is.Not.Null);Assert.That(buffer,Is.Not.Null,"Repeated sight queries should reuse storage.");
        void AddFriendly(int first,int count)
        {
            for(int i=first;i<first+count;i++)
            {
                var item=new GameObject("Friendly projectile");item.transform.SetParent(fixtures.transform);
                item.transform.position=shooterObject.transform.position+Vector3.forward*(1+i*.02f);
                item.transform.localScale=Vector3.one*.012f;item.AddComponent<BoxCollider>();
                item.AddComponent<P17PhysicalShell>().Configure(owner.transform,1,60,Vector3.forward,null);
            }
            Physics.SyncTransforms();
        }
        bool Visible()=> (bool)clear.Invoke(shooter,new object[]{Vector3.forward,10f,enemy});
        AddFriendly(0,100);Assert.That(Visible(),Is.True);
        var warmed=buffer.GetValue(shooter);Assert.That(Visible(),Is.True);Assert.That(buffer.GetValue(shooter),Is.SameAs(warmed),"A repeated query must reuse its grown buffer.");
        AddFriendly(100,200);Assert.That(Visible(),Is.True,"A full buffer must not hide a target behind 300 friendly projectiles.");
        var current=(RaycastHit[])buffer.GetValue(shooter);Assert.That(current.Length,Is.LessThan(300),"The dense scenario must exercise the complete fallback rather than silently truncate.");
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(fixtures.transform);
        wall.transform.position=shooterObject.transform.position+Vector3.forward*8;Physics.SyncTransforms();
        Assert.That(Visible(),Is.False,"A wall behind the friendly projectiles must still block the shot.");
        wall.transform.position=shooterObject.transform.position+Vector3.forward*11;Physics.SyncTransforms();
        Assert.That(Visible(),Is.True,"A wall behind the target must not block it.");
        wall.transform.position=target.transform.position;Physics.SyncTransforms();
        Assert.That(Visible(),Is.False,"A wall tied with the target must be treated as cover regardless of hit order.");
        yield return null;
    }
    [UnityTest] public IEnumerator DeviceCadenceDoesNotAccumulatePhysicsStepRounding()
    {
        var vehicle=Build(Model());yield return null;
        var shooterObject=GameObject.CreatePrimitive(PrimitiveType.Cube);shooterObject.transform.SetParent(fixtures.transform);
        shooterObject.transform.position=new Vector3(1000,1000,1000);shooterObject.AddComponent<Rigidbody>().useGravity=false;
        var shooter=shooterObject.AddComponent<R3FlyingDrone>();shooter.ConfigureTurret(vehicle.transform,.03f,10,120);
        var enemyObject=GameObject.CreatePrimitive(PrimitiveType.Cube);enemyObject.transform.SetParent(fixtures.transform);
        enemyObject.transform.position=shooterObject.transform.position+Vector3.forward*100;
        var hp=enemyObject.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        var enemy=enemyObject.AddComponent<P12PipelineSafeEnemyTurret>();enemy.enabled=false;enemy.Configure(vehicle.transform,null,null,enemyObject.transform,hp,null);
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        typeof(R3FlyingDrone).GetField("target",flags).SetValue(shooter,enemy);
        typeof(R3FlyingDrone).GetField("aimCollider",flags).SetValue(shooter,enemyObject.GetComponent<Collider>());
        typeof(R3FlyingDrone).GetField("nextScan",flags).SetValue(shooter,Time.time+100);
        Physics.SyncTransforms();float start=Time.fixedTime;
        for(int i=0;i<50;i++)yield return MOVSeriesDeviceTests.FixedStep();
        float elapsed=Time.fixedTime-start;
        var shots=Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Where(p=>p.name=="P17_DronePhysicalShell"&&p.gameObject.activeInHierarchy).ToArray();
        int expected=Mathf.FloorToInt(elapsed/.03f);
        TestContext.WriteLine($"elapsed={elapsed} fixedDelta={Time.fixedDeltaTime} shots={shots.Length} expected={expected}");
        Assert.That(elapsed,Is.GreaterThan(.8f),"The fixture must advance through actual physics steps.");
        Assert.That(shots.Length,Is.InRange(expected-2,expected+2),$"Cadence should not round every 0.03 s interval up to 0.04 s (elapsed={elapsed}).");
        ulong previous=shots.Max(shot=>shot.LaunchSequence);
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(fixtures.transform);
        wall.transform.position=shooterObject.transform.position+Vector3.forward*2;Physics.SyncTransforms();
        for(int i=0;i<20;i++)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Any(p=>p.name=="P17_DronePhysicalShell"&&p.LaunchSequence>previous),Is.False,
            "A wall must prevent further shots while the target is obscured.");
        wall.SetActive(false);Physics.SyncTransforms();
        for(int i=0;i<5&&!Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Any(p=>p.name=="P17_DronePhysicalShell"&&p.LaunchSequence>previous);i++)
            yield return MOVSeriesDeviceTests.FixedStep();
        var resumed=Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Where(p=>p.name=="P17_DronePhysicalShell"&&p.LaunchSequence>previous).ToArray();
        Assert.That(resumed.Length,Is.EqualTo(1),"Reacquiring a target must not bank shots during the obstruction.");
        shooter.enabled=false;Object.Destroy(wall);
        foreach(var shot in shots)if(shot!=null){shot.gameObject.SetActive(false);Object.Destroy(shot.gameObject);}
        foreach(var shot in resumed)if(shot!=null){shot.gameObject.SetActive(false);Object.Destroy(shot.gameObject);}
    }
    [UnityTest] public IEnumerator APreviousProjectileDoesNotBlindDroneOrTurretAndWallsStillBlock()
    {
        var vehicle=Build(Model());yield return null;
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var fire=typeof(R3FlyingDrone).GetMethod("Fire",flags);var tick=typeof(R3FlyingDrone).GetMethod("FixedUpdate",flags);
        var targetField=typeof(R3FlyingDrone).GetField("target",flags);var aimField=typeof(R3FlyingDrone).GetField("aimCollider",flags);
        var nextField=typeof(R3FlyingDrone).GetField("nextShot",flags);var scanField=typeof(R3FlyingDrone).GetField("nextScan",flags);
        for(int i=0;i<2;i++)
        {
            var shooterObject=GameObject.CreatePrimitive(PrimitiveType.Cube);shooterObject.transform.SetParent(fixtures.transform);
            shooterObject.transform.position=new Vector3(1000+i*20,1000,1000);shooterObject.AddComponent<Rigidbody>().useGravity=false;
            var shooter=shooterObject.AddComponent<R3FlyingDrone>();
            if(i==0)shooter.Configure(vehicle.transform,0,.1f,10);else shooter.ConfigureTurret(vehicle.transform,.1f,10);
            shooter.enabled=false;
            var enemyObject=GameObject.CreatePrimitive(PrimitiveType.Cube);enemyObject.transform.SetParent(fixtures.transform);
            enemyObject.transform.position=shooterObject.transform.position+Vector3.forward*4;
            var hp=enemyObject.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
            var enemy=enemyObject.AddComponent<P12PipelineSafeEnemyTurret>();enemy.enabled=false;
            enemy.Configure(vehicle.transform,null,null,enemyObject.transform,hp,null);
            targetField.SetValue(shooter,enemy);aimField.SetValue(shooter,enemyObject.GetComponent<Collider>());
            scanField.SetValue(shooter,Time.time+100);nextField.SetValue(shooter,0f);
            fire.Invoke(shooter,new object[]{Vector3.forward});
            var earlier=Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Single(p=>p.name=="P17_DronePhysicalShell"&&p.gameObject.activeInHierarchy);
            earlier.transform.position=shooterObject.transform.position+Vector3.forward*1.3f;Physics.SyncTransforms();
            tick.Invoke(shooter,null);
            var shots=Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Where(p=>p.name=="P17_DronePhysicalShell"&&p.gameObject.activeInHierarchy).ToArray();
            Assert.That(shots.Length,Is.EqualTo(2),i==0?"A drone must see past the projectile it fired earlier.":"A turret must see past the projectile it fired earlier.");
            float deadline=Time.realtimeSinceStartup+4;
            while(hp.CurrentHp>980.1f&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
            Assert.That(hp.CurrentHp,Is.EqualTo(980).Within(.1),"Both independent physical projectiles must hit.");
            foreach(var shot in shots)if(shot!=null){shot.gameObject.SetActive(false);Object.Destroy(shot.gameObject);}
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(fixtures.transform);
            wall.transform.position=shooterObject.transform.position+Vector3.forward*2;Physics.SyncTransforms();nextField.SetValue(shooter,0f);
            tick.Invoke(shooter,null);
            Assert.That((float)nextField.GetValue(shooter),Is.EqualTo(0),"A wall must still prevent the shot.");
            Object.Destroy(wall);Object.Destroy(enemyObject);Object.Destroy(shooterObject);yield return null;
        }
    }
    [UnityTest] public IEnumerator ScopedFireRateReachesActualDroneAndTurretShots()
    {
        var global=Upgrade(R1UpgradeEffect.Reload,.1f);
        var droneBonus=Upgrade(R1UpgradeEffect.Reload,.2f);droneBonus.Scope=R1BonusScope.Drones;
        var turretBonus=Upgrade(R1UpgradeEffect.Reload,.25f);turretBonus.Scope=R1BonusScope.Turrets;turretBonus.Operation=R1ModifierOperation.More;
        var vehicle=Build(Model(global,droneBonus,turretBonus));yield return null;
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();
        Assert.That(vehicle.Controller.WeaponInterval(R4WeaponKind.Basic),Is.EqualTo(R4WeaponDefinition.Get(R4WeaponKind.Basic).Interval*.9f).Within(.001),"Category bonuses must not change the mounted weapon's interval.");
        var globalPart=assembly.Parts.Single(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeEffect==R1UpgradeEffect.Reload&&p.UpgradeScope==R1BonusScope.Global);
        var dronePart=assembly.Parts.Single(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeEffect==R1UpgradeEffect.Reload&&p.UpgradeScope==R1BonusScope.Drones);
        var turretPart=assembly.Parts.Single(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeEffect==R1UpgradeEffect.Reload&&p.UpgradeScope==R1BonusScope.Turrets);
        var enemyObject=GameObject.CreatePrimitive(PrimitiveType.Cube);enemyObject.transform.SetParent(fixtures.transform);
        var hp=enemyObject.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        var enemy=enemyObject.AddComponent<P12PipelineSafeEnemyTurret>();enemy.enabled=false;
        enemy.Configure(vehicle.transform,null,null,enemyObject.transform,hp,null);
        R3FlyingDrone Shooter(float x,bool turret)
        {
            var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.transform.SetParent(fixtures.transform);
            item.transform.position=new Vector3(x,1000,1000);item.AddComponent<Rigidbody>().useGravity=false;
            var shooter=item.AddComponent<R3FlyingDrone>();
            if(turret)shooter.ConfigureTurret(vehicle.transform,2,10);else shooter.Configure(vehicle.transform,0,2,10);
            shooter.enabled=false;return shooter;
        }
        var drone=Shooter(1000,false);var turret=Shooter(1020,true);
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var target=typeof(R3FlyingDrone).GetField("target",flags);var aim=typeof(R3FlyingDrone).GetField("aimCollider",flags);
        var next=typeof(R3FlyingDrone).GetField("nextShot",flags);var scan=typeof(R3FlyingDrone).GetField("nextScan",flags);
        var tick=typeof(R3FlyingDrone).GetMethod("FixedUpdate",flags);
        void FireAndCheck(R3FlyingDrone shooter,float expected)
        {
            enemyObject.transform.position=shooter.transform.position+Vector3.forward*4;Physics.SyncTransforms();
            target.SetValue(shooter,enemy);aim.SetValue(shooter,enemyObject.GetComponent<Collider>());
            next.SetValue(shooter,0f);scan.SetValue(shooter,Time.time+100);float firedAt=Time.time;tick.Invoke(shooter,null);
            Assert.That((float)next.GetValue(shooter)-firedAt,Is.EqualTo(expected).Within(.001),"The real device must schedule its own next physical shot.");
            var shots=Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Where(p=>p.name=="P17_DronePhysicalShell"&&p.gameObject.activeInHierarchy).ToArray();
            Assert.That(shots.Length,Is.EqualTo(1),"Each check must create a real projectile.");
            shots[0].gameObject.SetActive(false);Object.Destroy(shots[0].gameObject);
        }
        FireAndCheck(drone,1.4f);FireAndCheck(turret,1.35f);
        dronePart.gameObject.SetActive(false);yield return null;
        FireAndCheck(drone,1.8f);FireAndCheck(turret,1.35f);
        turretPart.gameObject.SetActive(false);yield return null;
        FireAndCheck(turret,1.8f);
        globalPart.gameObject.SetActive(false);yield return null;
        FireAndCheck(drone,2f);FireAndCheck(turret,2f);
    }
    [UnityTest] public IEnumerator RepairEfficiencyMultipliesFieldRepairAndStopsAfterLoss()
    {
        Assert.That(Enum.TryParse("RepairEfficiency",out R1UpgradeEffect efficiency),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);Assert.That(run.Progress.BeginTest(),Is.True);
        var additive=Upgrade(efficiency,.5f);var more=Upgrade(efficiency,.2f);more.Operation=R1ModifierOperation.More;
        var model=Model(Upgrade(R1UpgradeEffect.Repair,0),additive,more);var vehicle=Build(model);yield return null;
        vehicle.Core.RestoreHealthRatio(.5f);var effects=Effects(vehicle);
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        void Pulse()
        {
            typeof(R3MountedBonusEffects).GetField("lastRepairPulse",flags).SetValue(effects,Time.time-.5f);
            typeof(R3MountedBonusEffects).GetMethod("ApplyRepairPulse",flags).Invoke(effects,null);
        }
        Pulse();Assert.That(vehicle.Core.CurrentHp/vehicle.Core.MaxHp,Is.EqualTo(.5225f).Within(.001f));
        foreach(var part in vehicle.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.UpgradeEffect==efficiency))part.gameObject.SetActive(false);
        yield return null;vehicle.Core.RestoreHealthRatio(.5f);Pulse();
        Assert.That(vehicle.Core.CurrentHp/vehicle.Core.MaxHp,Is.EqualTo(.5125f).Within(.001f));
    }
    GameObject fixtures;R1RunController run;R3VisualPalette palette;
    readonly List<R1UpgradeDefinition> definitions=new List<R1UpgradeDefinition>();
    static IEnumerator Until(Func<bool> predicate,string message)
    {
        float end=Time.realtimeSinceStartup+12;
        while(!predicate()&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That(predicate(),Is.True,message);
    }
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        yield return Until(()=>R1RunController.Instance?.Progress!=null,"Bootstrap");
        run=R1RunController.Instance;palette=Object.FindFirstObjectByType<R3VisualPalette>();
        fixtures=new GameObject("W34 mounted effect fixtures");Time.timeScale=1;
    }
    R1UpgradeDefinition Upgrade(R1UpgradeEffect effect,float amount=.25f)
    {
        var d=ScriptableObject.CreateInstance<R1UpgradeDefinition>();d.Id=Guid.NewGuid().ToString("N");
        d.Title=effect.ToString();d.Effect=effect;d.Amount=amount;definitions.Add(d);return d;
    }
    static Vector3Int Slot(int i)=>new Vector3Int((i%5)-2,2,i<5?-2:2);
    R3VehicleModel Model(params R1UpgradeDefinition[] bonuses)
    {
        var m=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        Assert.That(m.ReceiveBonuses(bonuses),Is.True);
        var ids=m.Inventory.Select(p=>p.Id).ToArray();
        for(int i=0;i<ids.Length;i++)
        {
            var cell=Slot(i);
            if(m.OwnedPlacementError(ids[i],cell,default)!=null)
                cell=m.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                    .First(c=>m.OwnedPlacementError(ids[i],c,default)==null);
            Assert.That(m.Mount(ids[i],cell),Is.True,"mount "+i);
        }
        return m;
    }
    R1VehicleState Build(R3VehicleModel model,R1RunStats baseline=null,float x=0)
    {
        var v=R3VehicleAssembly.Build(model,baseline??new R1RunStats(),palette,new Vector3(x,20,1000),Quaternion.identity);
        v.transform.SetParent(fixtures.transform,true);v.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll;
        return v;
    }
    static R3MountedBonusEffects Effects(R1VehicleState v)=>v.GetComponent<R3MountedBonusEffects>();
    static R3MountedPart Module(R1VehicleState v,R1UpgradeEffect effect)=>
        v.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeEffect==effect);
    R3VehicleModel ModelWithOneMountedBonus(R1UpgradeEffect effect)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        Assert.That(model.ReceiveBonuses(new[]{Upgrade(effect)}),Is.True);
        var item=model.Inventory.Single();
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.OwnedPlacementError(item.Id,c,default)==null);
        Assert.That(model.Mount(item.Id,cell),Is.True);return model;
    }
    R3VehicleModel ModelWithDamageBonuses(params float[] amounts)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        Assert.That(model.ReceiveBonuses(amounts.Select(amount=>Upgrade(R1UpgradeEffect.Damage,amount))),Is.True);
        foreach(var item in model.Inventory.ToArray())
        {
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.OwnedPlacementError(item.Id,c,default)==null);
            Assert.That(model.Mount(item.Id,cell),Is.True);
        }
        return model;
    }
    [UnityTest] public IEnumerator MountedDamageBonusesAddAndReachPhysicalShotsWithoutOldCap()
    {
        var small=Build(ModelWithDamageBonuses(.25f,.2f),x:-40);
        Assert.That(Effects(small).EffectiveStats.Damage,Is.EqualTo(1.45f).Within(.001));
        Assert.That(small.Controller.PackageDamageScale,Is.EqualTo(1.45f).Within(.001));
        var strong=Build(ModelWithDamageBonuses(1f,1f,1f,1f),x:0);
        Assert.That(Effects(strong).EffectiveStats.Damage,Is.EqualTo(5f).Within(.001));
        Assert.That(strong.Controller.PackageDamageScale,Is.EqualTo(5f).Within(.001));
        var barrel=new GameObject("Additive bonus test muzzle").transform;barrel.SetParent(fixtures.transform);
        barrel.position=new Vector3(40,20,1000);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.SetParent(fixtures.transform);
        target.transform.position=barrel.position+Vector3.forward*4;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        strong.Controller.FireFrom(barrel,R4WeaponKind.Basic,"additive-test");
        yield return Until(()=>hp.CurrentHp<1000,"Physical shot reaches target");
        Assert.That(hp.CurrentHp,Is.EqualTo(1000-R4WeaponDefinition.Get(R4WeaponKind.Basic).Damage*5f).Within(.01));
    }
    [UnityTest] public IEnumerator MountedMobilityBonusesAddWithoutArtificialMultiplierCap()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        Assert.That(model.ReceiveBonuses(new[]{Upgrade(R1UpgradeEffect.Mobility,.25f),Upgrade(R1UpgradeEffect.Mobility,.2f)}),Is.True);
        foreach(var item in model.Inventory.ToArray())
        {
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.OwnedPlacementError(item.Id,c,default)==null);
            Assert.That(model.Mount(item.Id,cell),Is.True);
        }
        var baseline=Build(model.DeepCopy(),x:-40);
        // Keep the same physical mass when comparing movement: disabling parts
        // changes chassis tuning as well as the bonus under test.
        foreach(var part in baseline.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.BonusModule))part.UpgradeAmount=0;
        yield return null;float baseSpeed=baseline.Controller.EffectiveForwardSpeed;
        var vehicle=Build(model);Assert.That(Effects(vehicle).EffectiveStats.Mobility,Is.EqualTo(1.45f).Within(.001));
        Assert.That(vehicle.Controller.EffectiveForwardSpeed,Is.EqualTo(baseSpeed*1.45f).Within(.01));
        foreach(var part in vehicle.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.BonusModule))part.UpgradeAmount=1f;
        yield return null;
        Assert.That(Effects(vehicle).EffectiveStats.Mobility,Is.EqualTo(3f).Within(.001));
        Assert.That(vehicle.Controller.EffectiveForwardSpeed,Is.EqualTo(baseSpeed*3f).Within(.01));
    }
    [UnityTest] public IEnumerator HullAndCoreBonusesAddWithinTheirGroupsAndExceedOldDurabilityCap()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        Assert.That(model.ReceiveBonuses(new[]{Upgrade(R1UpgradeEffect.Hull,1f),Upgrade(R1UpgradeEffect.Hull,1f),
            Upgrade(R1UpgradeEffect.Hull,1f),Upgrade(R1UpgradeEffect.Hull,1f),
            Upgrade(R1UpgradeEffect.Core,1f),Upgrade(R1UpgradeEffect.Core,1f)}),Is.True);
        foreach(var item in model.Inventory.ToArray())
        {
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.OwnedPlacementError(item.Id,c,default)==null);
            Assert.That(model.Mount(item.Id,cell),Is.True);
        }
        var vehicle=Build(model);var stats=Effects(vehicle).EffectiveStats;
        Assert.That(stats.Hull,Is.EqualTo(5f).Within(.001));Assert.That(stats.Core,Is.EqualTo(3f).Within(.001));
        Assert.That(vehicle.Core.MaxHp,Is.EqualTo(1500f).Within(.01),"Independent hull and core groups combine without the old 12x cap.");
        vehicle.Core.RestoreHealthRatio(.4f);
        var hullModule=Module(vehicle,R1UpgradeEffect.Hull);hullModule.gameObject.SetActive(false);yield return null;
        Assert.That(vehicle.Core.MaxHp,Is.EqualTo(1200f).Within(.01));
        Assert.That(vehicle.Core.CurrentHp/vehicle.Core.MaxHp,Is.EqualTo(.4f).Within(.001),"Changing bonuses preserves HP percentage.");
    }
    [UnityTest] public IEnumerator ReloadReductionsAddAndCanReachZeroWithoutNegativeInterval()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        Assert.That(model.ReceiveBonuses(new[]{Upgrade(R1UpgradeEffect.Reload,.2f),Upgrade(R1UpgradeEffect.Reload,.2f)}),Is.True);
        foreach(var item in model.Inventory.ToArray())
        {
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.OwnedPlacementError(item.Id,c,default)==null);
            Assert.That(model.Mount(item.Id,cell),Is.True);
        }
        var vehicle=Build(model);var controller=vehicle.Controller;
        Assert.That(Effects(vehicle).EffectiveStats.FireInterval,Is.EqualTo(.6f).Within(.001));
        Assert.That(controller.WeaponInterval(R4WeaponKind.Basic),Is.EqualTo(R4WeaponDefinition.Get(R4WeaponKind.Basic).Interval*.6f).Within(.001));
        foreach(var part in vehicle.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.BonusModule))part.UpgradeAmount=.75f;
        yield return null;
        Assert.That(Effects(vehicle).EffectiveStats.FireInterval,Is.Zero);
        Assert.That(controller.WeaponInterval(R4WeaponKind.Basic),Is.Zero,"An interval cannot become negative or retain a hidden balance cap.");
    }
    [UnityTest] public IEnumerator InventoryIsInactiveAndAllMountedEffectsReachLiveWeaponsHpAndOmni()
    {
        var m=Model(Upgrade(R1UpgradeEffect.Damage,.25f),Upgrade(R1UpgradeEffect.Damage,.2f),
            Upgrade(R1UpgradeEffect.Hull,.4f),Upgrade(R1UpgradeEffect.Core,.5f),
            Upgrade(R1UpgradeEffect.Reload,.2f),Upgrade(R1UpgradeEffect.Mobility,.3f));
        Assert.That(m.ReceiveBonuses(new[]{Upgrade(R1UpgradeEffect.Damage,1)}),Is.True);
        m.Parts.Add(new R3PartState(new Vector3Int(0,2,1),R3PartKind.OmniDrive));m.SelectedDrive=R3DriveKind.Omni;
        var unboosted=m.DeepCopy();unboosted.Parts.Single(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeEffect==R1UpgradeEffect.Mobility).UpgradeAmount=0;
        var reference=Build(unboosted,x:-40);float unboostedSpeed=reference.Controller.EffectiveForwardSpeed;reference.gameObject.SetActive(false);
        var v=Build(m);var tank=v.Controller;var stats=Effects(v).EffectiveStats;
        Assert.That(stats.Damage,Is.EqualTo(1.45f).Within(.001));Assert.That(stats.Mobility,Is.EqualTo(1.3f).Within(.001));
        Assert.That(tank.EffectiveDamage,Is.EqualTo(58).Within(.001));
        Assert.That(tank.WeaponInterval(R4WeaponKind.Basic),Is.EqualTo(R4WeaponDefinition.Get(R4WeaponKind.Basic).Interval*.8f).Within(.001));
        Assert.That(v.Core.MaxHp,Is.EqualTo(210).Within(.001));
        var gun=v.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Gun);
        Assert.That(gun.Damageable.MaxHp,Is.EqualTo(R4WeaponDefinition.Get(gun.Weapon).Hp*1.4f).Within(.001));
        v.Controller.SetGameplayInputEnabled(true);v.GetComponent<MOVSeriesVehicleAdapter>().TryDrive(1,0,tank.EffectiveForwardSpeed,tank.EffectiveReverseSpeed,tank.EffectiveTurnSpeed);
        Assert.That(v.GetComponentInChildren<OmniDriveModule>().MaximumSpeed,Is.EqualTo(tank.EffectiveForwardSpeed).Within(.001));
        Assert.That(tank.EffectiveForwardSpeed,Is.EqualTo(unboostedSpeed*1.3f).Within(.001));
        var barrel=new GameObject("Actual bonus weapon muzzle").transform;barrel.SetParent(fixtures.transform);
        barrel.position=new Vector3(40,20,1000);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.SetParent(fixtures.transform);
        target.transform.position=barrel.position+Vector3.forward*4;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        tank.FireFrom(barrel,R4WeaponKind.Basic,"w34-live-bonus");
        yield return Until(()=>hp.CurrentHp<1000,"Physical bonus shot reaches target");
        Assert.That(hp.CurrentHp,Is.EqualTo(1000-R4WeaponDefinition.Get(R4WeaponKind.Basic).Damage*1.45f).Within(.01));
        Assert.That(run.Progress.Stats.Damage,Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator DestructionAndBridgeDetachmentRemoveEffectsWithoutHealing()
    {
        var m=Model(Upgrade(R1UpgradeEffect.Hull,.4f),Upgrade(R1UpgradeEffect.Damage,.25f));
        m.Parts.Single(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeEffect==R1UpgradeEffect.Hull).Cell=new Vector3Int(1,2,2);
        m.Parts.Single(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeEffect==R1UpgradeEffect.Damage).Cell=new Vector3Int(1,2,4);
        var v=Build(m);v.Core.RestoreHealthRatio(.4f);var hull=Module(v,R1UpgradeEffect.Hull);
        hull.Damageable.ApplyDamage(10000,hull.transform.position,Vector3.right);
        v.Structure.EvaluateStructure();yield return null;yield return null;
        // Second module was supported exclusively through the first footprint.
        Assert.That(Module(v,R1UpgradeEffect.Damage).Operational,Is.False);
        Assert.That(Effects(v).EffectiveStats.Damage,Is.EqualTo(1));Assert.That(Effects(v).EffectiveStats.Hull,Is.EqualTo(1));
        Assert.That(v.Core.MaxHp,Is.EqualTo(100).Within(.001));Assert.That(v.Core.CurrentHp,Is.EqualTo(40).Within(.001));
        Assert.That(v.Controller.EffectiveDamage,Is.EqualTo(40).Within(.001));
    }
    [UnityTest] public IEnumerator RepeatedCaptureAndRebuildUseOriginalBaselineAndPreserveCondition()
    {
        var baseline=new R1RunStats();baseline.Apply(R1UpgradeEffect.Damage,.1f);
        var m=Model(Upgrade(R1UpgradeEffect.Damage,.25f),Upgrade(R1UpgradeEffect.Hull,.4f));
        var v=Build(m,baseline);v.Core.RestoreHealthRatio(.37f);
        for(int i=0;i<3;i++)
        {
            v.GetComponent<R3VehicleAssembly>().Capture();v.gameObject.SetActive(false);Object.Destroy(v.gameObject);
            v=Build(m.DeepCopy(),baseline);
            Assert.That(v.Controller.EffectiveDamage,Is.EqualTo(40*1.1f*1.25f).Within(.001));
            Assert.That(v.Core.CurrentHp/v.Core.MaxHp,Is.EqualTo(.37f).Within(.001));
            Assert.That(baseline.Damage,Is.EqualTo(1.1f).Within(.001));yield return null;
        }
    }
    [UnityTest] public IEnumerator FieldRepairOnlyHealsLivingConnectedPartsDuringTestAndStopsOnLoss()
    {
        Assert.That(run.OpenSandboxGarage(),Is.True);Time.timeScale=1;
        var model=ModelWithOneMountedBonus(R1UpgradeEffect.Repair);var v=Build(model);v.Core.RestoreHealthRatio(.5f);
        float end=Time.time+.4f;while(Time.time<end)yield return null;
        Assert.That(v.Core.CurrentHp/v.Core.MaxHp,Is.EqualTo(.5f).Within(.001),"Garage never grants passive repair.");
        Assert.That(run.Progress.BeginTest(),Is.True);end=Time.time+.6f;while(Time.time<end)yield return null;
        Assert.That(v.Core.CurrentHp/v.Core.MaxHp,Is.GreaterThan(.505f));
        var module=Module(v,R1UpgradeEffect.Repair);
        module.Damageable.ApplyDamageResolvedTyped(module.Damageable.CurrentHp/.9f+1f,module.transform.position,Vector3.right,
            R3DamagePropagation.DamageKind.Piercing);
        yield return null;yield return null;float ratio=v.Core.CurrentHp/v.Core.MaxHp;
        Assert.That(v.Core.IsDead,Is.False,"The recipient must remain alive when checking that regeneration stopped.");
        end=Time.time+.6f;while(Time.time<end)yield return null;
        Assert.That(v.Core.CurrentHp/v.Core.MaxHp,Is.EqualTo(ratio).Within(.001));
    }
    [UnityTest] public IEnumerator TierThreeFieldRepairAddsAllFourContributions()
    {
        Assert.That(run.OpenSandboxGarage(),Is.True);Assert.That(run.Progress.BeginTest(),Is.True);
        var model=ModelWithOneMountedBonus(R1UpgradeEffect.Repair);
        model.Parts.Single(p=>p.Kind==R3PartKind.BonusModule).Tier=3;
        var vehicle=Build(model);vehicle.Core.RestoreHealthRatio(.5f);
        var effects=Effects(vehicle);var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        typeof(R3MountedBonusEffects).GetField("lastRepairPulse",flags).SetValue(effects,Time.time-.5f);
        typeof(R3MountedBonusEffects).GetMethod("ApplyRepairPulse",flags).Invoke(effects,null);
        Assert.That(vehicle.Core.CurrentHp/vehicle.Core.MaxHp,Is.EqualTo(.55f).Within(.001f),"Four tier contributions must each add 2.5% per second.");
        yield return null;
    }
    [UnityTest] public IEnumerator RangeResetAndReturnPreserveWorkshopModuleIdentityWithoutStacking()
    {
        Assert.That(run.OpenSandboxGarage(),Is.True);
        Assert.That(run.VehicleModel.ReceiveBonuses(new[]{Upgrade(R1UpgradeEffect.Damage)}),Is.True);
        string id=run.VehicleModel.Inventory.Single().Id;
        Assert.That(run.GarageMount(id,new Vector3Int(2,2,-2)),Is.True);
        Assert.That(run.ActiveStats.Damage,Is.EqualTo(1.25f).Within(.001));
        Assert.That(run.EnterTestRange(),Is.True);yield return Until(()=>!run.Transitioning,"Range entry");
        var module=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
        Assert.That(run.ActiveStats.Damage,Is.EqualTo(1.25f).Within(.001));
        module.Damageable.ApplyDamage(10000,module.transform.position,Vector3.right);yield return null;yield return null;
        Assert.That(run.ActiveStats.Damage,Is.EqualTo(1));
        Assert.That(run.ResetTestRange(),Is.True);yield return Until(()=>!run.Transitioning,"Range reset");
        Assert.That(run.ActiveStats.Damage,Is.EqualTo(1.25f).Within(.001));
        Assert.That(run.ReturnFromTestRange(),Is.True);yield return Until(()=>!run.Transitioning,"Workshop restored");
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Kind==R3PartKind.BonusModule).Id,Is.EqualTo(id));
        Assert.That(run.ActiveStats.Damage,Is.EqualTo(1.25f).Within(.001));Assert.That(run.Progress.Stats.Damage,Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator MountedBonusSurvivesNextFightAndMapThenCanBePaidRepairedAndRemounted()
    {
        Assert.That(run.StartRun(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        yield return Until(()=>!run.Transitioning,"First fight");run.Encounter.SetCombatActive(false);
        RRLW2TestFlow.DefeatAll(run);yield return Until(()=>run.Progress.State==R1RunState.Salvage,"Victory");
        var ids=run.Salvage.Items.Where(p=>p.Selectable).Take(3).Select(p=>p.Id).ToArray();
        int expectedLootCount=run.Salvage.Items.Where(p=>ids.Contains(p.Id)).Sum(p=>run.VehicleModel.GrantQuantity(p.Kind,p.ToPart().ArmorShape));
        Assert.That(ids.Length,Is.EqualTo(3));Assert.That(run.ConfirmRewards(ids,new[]{0}),Is.True);
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(expectedLootCount+1));
        foreach(var lootId in ids)Assert.That(run.VehicleModel.Inventory.Any(p=>p.Id==lootId),Is.True);
        var bonus=run.VehicleModel.Inventory.Single(p=>p.Kind==R3PartKind.BonusModule);string id=bonus.Id;
        var mountCell=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>run.VehicleModel.OwnedPlacementError(id,c,default)==null);
        Assert.That(run.GarageMount(id,mountCell),Is.True);
        var expected=run.ActiveStats.Copy();Assert.That(run.StartNextEncounter(),Is.True);
        Assert.That(run.ChooseRoute(0),Is.True);yield return Until(()=>!run.Transitioning,"Next map");run.Encounter.SetCombatActive(false);
        Assert.That(run.ActiveStats.Damage,Is.EqualTo(expected.Damage));Assert.That(run.ActiveStats.Hull,Is.EqualTo(expected.Hull));
        var module=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
        float damage=(module.Damageable.CurrentHp+1)/(1-R3DamagePropagation.TransferFraction(R3DamagePropagation.DamageKind.Kinetic));
        module.Damageable.ApplyDamage(damage,module.transform.position,Vector3.right);yield return null;yield return null;
        Assert.That(run.ActiveStats.Damage,Is.EqualTo(1));Assert.That(run.ActiveStats.Hull,Is.EqualTo(1));
        Assert.That(run.ActiveStats.Mobility,Is.EqualTo(1));Assert.That(run.ActiveStats.Core,Is.EqualTo(1));
        RRLW2TestFlow.DefeatAll(run);yield return Until(()=>run.Progress.State==R1RunState.Salvage,"Second victory");
        Assert.That(run.ConfirmRewards(Array.Empty<string>(),new[]{0}),Is.True);
        var state=run.VehicleModel.Parts.Single(p=>p.Id==id);Assert.That(state.Health,Is.Zero);
        run.VehicleModel.Award(100);int before=run.VehicleModel.Scrap;int repairCost=run.VehicleModel.RepairCost(state);
        Assert.That(repairCost,Is.GreaterThan(0));Assert.That(run.GarageRepair(state.Cell),Is.True);Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before-repairCost));
        Assert.That(run.GarageRemove(state.Cell),Is.True);Assert.That(run.GarageMount(id,mountCell),Is.True);
        Assert.That(run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id).Operational,Is.True);
        Assert.That(run.ActiveStats.Damage,Is.EqualTo(expected.Damage));Assert.That(run.ActiveStats.Hull,Is.EqualTo(expected.Hull));
    }
    [UnityTest] public IEnumerator BoosterForceFollowsRotatedPhysicalMountAroundAllAxes()
    {
        foreach(var turns in new[]{new Vector3Int(0,1,0),new Vector3Int(1,0,0),new Vector3Int(0,0,1),new Vector3Int(-1,2,1)})
        {
            var m=Model();m.Parts.Add(new R3PartState(new Vector3Int(0,2,2),R3PartKind.Booster){RotationQuarterTurns=turns});
            var v=Build(m);var body=v.GetComponent<Rigidbody>();body.constraints=RigidbodyConstraints.FreezeRotation;body.useGravity=false;
            v.Controller.SetGameplayInputEnabled(true);
            var part=v.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.Booster);
            var direction=part.transform.forward;
            Assert.That(v.GetComponent<MOVSeriesVehicleAdapter>().TryBoost(),Is.True);
            float end=Time.fixedTime+.12f;while(Time.fixedTime<end)yield return null;
            Assert.That(Vector3.Dot(body.linearVelocity.normalized,direction),Is.GreaterThan(.95f),"Actual thrust direction "+turns);
            v.gameObject.SetActive(false);Object.Destroy(v.gameObject);yield return null;
        }
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Time.timeScale=1;if(fixtures!=null)Object.Destroy(fixtures);
        foreach(var d in definitions)if(d!=null)Object.Destroy(d);definitions.Clear();
        if(Application.isPlaying)yield return new ExitPlayMode();
    }
}
