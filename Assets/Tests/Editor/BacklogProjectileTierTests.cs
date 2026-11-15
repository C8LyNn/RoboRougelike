using System.Linq;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R1;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RailgunFriendlyFixture : MonoBehaviour,IRRLTeamMember
{ public int TeamId=>1; }
public sealed class RailgunWallFixture : MonoBehaviour,IRRLHitReceiver
{
    public int HitCount {get;private set;}
    public bool ReceiveHit(float damage,Vector3 point,Vector3 direction,Transform source){HitCount++;return true;}
}

public sealed class BacklogProjectileTierTests
{
    [TestCase(R4WeaponKind.Shotgun)]
    [TestCase(R4WeaponKind.Railgun)]
    [TestCase(R4WeaponKind.RocketLauncher)]
    public void SameProjectileWeaponFusesAndDoesNotCreateAmmo(R4WeaponKind weapon)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(1000);
        for(int i=0;i<3;i++)Assert.That(model.PurchaseToInventory(R3PartKind.Gun,weapon),Is.True);
        var input=model.Inventory.ToArray();input[0].Ammo=-1;input[1].Ammo=2;input[2].Ammo=5;
        input[1].ReloadReadyAt=50;
        Assert.That(model.FuseInventory(input.Select(p=>p.Id).ToArray()),Is.True);
        var result=model.Inventory.Single();Assert.That(result.Tier,Is.EqualTo(2));
        Assert.That(result.Weapon,Is.EqualTo(weapon));Assert.That(result.Ammo,Is.EqualTo(2));
        Assert.That(result.ReloadReadyAt,Is.EqualTo(50));
    }
}

public sealed class BacklogProjectileTierPhysicsTests
{
    R1RunController run;
    static int ExplicitSiblingPairs(R4ProjectileOrigin[] shots)
    {
        var colliders=shots.Select(origin=>origin.GetComponent<Collider>()).ToHashSet();
        var field=typeof(P17PhysicalShell).GetField("ignoredForFlight",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That(field,Is.Not.Null);
        return shots.Sum(origin=>((System.Collections.Generic.List<Collider>)field.GetValue(origin.GetComponent<P17PhysicalShell>())).Count(colliders.Contains))/2;
    }
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);
    }
    [UnityTest] public IEnumerator ExperimentalSharedLayerKeepsIndependentPhysicalHits()
    {
        // Isolated experiment: this does not reserve or change any production layer.
        const int layer=29;Vector3 start=new Vector3(1600,1600,1600);
        var owner=new GameObject("Experimental salvo owner");
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=start+Vector3.forward*5;
        target.transform.localScale=new Vector3(4,4,.5f);
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);
        var shells=new System.Collections.Generic.List<GameObject>();
        try
        {
            for(int i=0;i<2;i++)
            {
                var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shells.Add(shell);shell.layer=layer;
                shell.transform.position=start;shell.transform.localScale=Vector3.one*.16f;
                var collider=shell.GetComponent<Collider>();collider.excludeLayers=1<<layer;
                var body=shell.AddComponent<Rigidbody>();body.useGravity=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                body.linearVelocity=Vector3.forward*40;
                shell.AddComponent<P17PhysicalShell>().Configure(owner.transform,20,3,Vector3.forward,null);
            }
            Physics.SyncTransforms();
            Assert.That(Physics.GetIgnoreCollision(shells[0].GetComponent<Collider>(),shells[1].GetComponent<Collider>()),Is.False,
                "Layer filtering must be verified by actual movement and hits, not the pair-ignore API.");
            var seen=Physics.OverlapSphere(start,1,~0,QueryTriggerInteraction.Ignore);
            Assert.That(shells.All(shell=>seen.Contains(shell.GetComponent<Collider>())),Is.True,"Both projectiles must remain visible to physics queries.");
            Assert.That(shells.All(shell=>shell.GetComponent<RRLPointDefenseThreat>()!=null),Is.True);
            float deadline=Time.realtimeSinceStartup+5;
            while(hp.CurrentHp>460.01f&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
            Assert.That(hp.CurrentHp,Is.EqualTo(460).Within(.02),"Two independent physical projectiles must both reach and damage the target.");
        }
        finally{foreach(var shell in shells)if(shell!=null)Object.Destroy(shell);Object.Destroy(target);Object.Destroy(owner);}
    }
    [UnityTest] public IEnumerator ExperimentalSeparateVolleyLayersStillCollide()
    {
        Vector3 center=new Vector3(1700,1700,1700);
        var owners=new[]{new GameObject("Experimental owner A"),new GameObject("Experimental owner B")};
        var shells=new System.Collections.Generic.List<GameObject>();
        try
        {
            for(int i=0;i<2;i++)
            {
                int layer=28+i;float direction=i==0?1:-1;
                var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shells.Add(shell);shell.layer=layer;
                shell.transform.position=center+Vector3.right*(-2*direction);shell.transform.localScale=Vector3.one*.2f;
                shell.GetComponent<Collider>().excludeLayers=1<<layer;
                var body=shell.AddComponent<Rigidbody>();body.useGravity=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                body.linearVelocity=Vector3.right*(direction*20);
                shell.AddComponent<P17PhysicalShell>().Configure(owners[i].transform,20,3,Vector3.right*direction,null);
            }
            Physics.SyncTransforms();float deadline=Time.realtimeSinceStartup+5;
            while(shells.All(shell=>shell!=null&&shell.GetComponent<P17PhysicalShell>().IsActiveProjectile)&&Time.realtimeSinceStartup<deadline)
                yield return new WaitForFixedUpdate();
            Assert.That(shells.Any(shell=>shell==null||!shell.GetComponent<P17PhysicalShell>().IsActiveProjectile),Is.True,
                "Shots from distinct volleys must retain their existing collisions.");
        }
        finally{foreach(var shell in shells)if(shell!=null)Object.Destroy(shell);foreach(var owner in owners)Object.Destroy(owner);}
    }
    [UnityTest] public IEnumerator ExperimentalColliderLayerMustNotHideHostileShield()=>ExperimentalLayerShield(0);
    [UnityTest] public IEnumerator ExperimentalBodyLayerMustNotHideHostileShield()=>ExperimentalLayerShield(1);
    [UnityTest] public IEnumerator ExperimentalMixedLayersMustNotHideHostileShield()=>ExperimentalLayerShield(2);
    IEnumerator ExperimentalLayerShield(int mode)
    {
        const int layer=29;Vector3 start=new Vector3(1800,1800,1800);
        var owner=new GameObject("Experimental hostile owner");owner.AddComponent<P12PipelineSafeEnemyTurret>().enabled=false;
        var dome=new GameObject("Experimental hostile shield");dome.transform.position=start+Vector3.forward*6;
        dome.AddComponent<SphereCollider>().isTrigger=true;
        var hp=dome.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);dome.AddComponent<R3ShieldDome>().Configure(1,1);
        var shells=new System.Collections.Generic.List<GameObject>();
        try
        {
            for(int i=0;i<2;i++)
            {
                var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shells.Add(shell);shell.layer=layer;
                shell.transform.position=start;shell.transform.localScale=Vector3.one*.16f;
                var body=shell.AddComponent<Rigidbody>();body.useGravity=false;body.linearVelocity=Vector3.forward*1000;
                if(mode==1||(mode==2&&i==1))body.excludeLayers=1<<layer;else shell.GetComponent<Collider>().excludeLayers=1<<layer;
                shell.AddComponent<P17PhysicalShell>().Configure(owner.transform,20,3,Vector3.forward,null);
            }
            Physics.SyncTransforms();
            Assert.That(R3ShieldDome.TryFirstHit(start,start+Vector3.forward*20,owner.transform,out _,out _),Is.True);
            typeof(P17PhysicalShell).GetMethod("FixedUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Invoke(shells[0].GetComponent<P17PhysicalShell>(),null);
            Assert.That(hp.CurrentHp,Is.EqualTo(480).Within(.02),"An excluded sibling must not hide a real hostile shield.");
            yield return null;
        }
        finally{foreach(var shell in shells)if(shell!=null)Object.Destroy(shell);Object.Destroy(dome);Object.Destroy(owner);}
    }
    [UnityTest] public IEnumerator RocketLauncherCopiesFireBeforeBonusRocketsConsumeRemainingSlots()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(2000);
        var firstState=model.Parts.First(p=>p.Kind==R3PartKind.Gun);firstState.Weapon=R4WeaponKind.RocketLauncher;
        var gunCell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,R3PartKind.Gun,false,R4WeaponKind.RocketLauncher)==null);
        Assert.That(model.Buy(gunCell,R3PartKind.Gun,R4WeaponKind.RocketLauncher),Is.True);
        var bonusCell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,R3PartKind.DoubleShot)==null);
        Assert.That(model.Buy(bonusCell,R3PartKind.DoubleShot),Is.True);model.Economy.DoubleShotChancePerUnit=100;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        var body=vehicle.GetComponent<Rigidbody>();body.isKinematic=true;var assembly=vehicle.GetComponent<R3VehicleAssembly>();
        var guns=assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun&&p.Weapon==R4WeaponKind.RocketLauncher).ToArray();Assert.That(guns.Length,Is.EqualTo(2));
        for(int i=0;i<guns.Length;i++)
        {
            var muzzle=new GameObject("Unblocked rocket muzzle").transform;muzzle.SetParent(guns[i].transform,false);
            muzzle.position=new Vector3(1000+i*8,1000,1000);muzzle.rotation=Quaternion.identity;guns[i].GunPivot=muzzle;guns[i].Muzzle=muzzle;
            var binding=guns[i].GetComponent<RRLRocketBinding>();Assert.That(binding,Is.Not.Null);
            typeof(RRLRocketBinding).GetField("muzzle",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(binding,muzzle);
            binding.Module.Bind(body,vehicle.transform,muzzle);
        }
        var targets=guns.Select(g=>{var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=g.Muzzle.position+Vector3.forward*5;
            target.transform.localScale=new Vector3(2,2,.5f);var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);target.AddComponent<RRLRocketDamageReceiver>();return hp;}).ToArray();
        Physics.SyncTransforms();Assert.That(guns.All(assembly.ClearProjectilePath),Is.True);
        var fixtureOwner=new GameObject("Rocket pool fixture owner");var fixtureBody=fixtureOwner.AddComponent<Rigidbody>();fixtureBody.isKinematic=true;
        var dummies=new System.Collections.Generic.List<GameObject>();
        try
        {
            var spec=new RoboRougelike.Mechanics.Rockets.RocketConfig().Snapshot();
            for(int i=0;i<28;i++)
            {
                var dummy=new GameObject("Rocket pool fixture");dummies.Add(dummy);dummy.transform.position=new Vector3(2000+i*3,2000,2000);
                var rocket=dummy.AddComponent<RoboRougelike.Mechanics.Rockets.RocketProjectile>();
                Assert.That(rocket.TryInitialize(spec,fixtureBody,fixtureOwner.transform,fixtureOwner,null,Vector3.forward),Is.True);
            }
            int slot=System.Array.FindIndex(model.GetWeaponSlots(),w=>w==R4WeaponKind.RocketLauncher);Assert.That(slot,Is.GreaterThanOrEqualTo(0));
            float before=vehicle.GetComponent<VehicleEnergyBank>().Current;assembly.FireSlot(slot);
            Assert.That(guns.All(g=>g.NextFireTime>Time.time),Is.True,"Each ready launcher must fire before bonus rockets fill the pool.");
            Assert.That(guns.All(g=>g.GetComponent<RRLRocketBinding>().State.Ammo==RRLRocketBinding.Magazine-1),Is.True);
            Assert.That(before-vehicle.GetComponent<VehicleEnergyBank>().Current,Is.EqualTo(2*VehicleEnergyCosts.Rocket).Within(.001));
            var origins=Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>guns.Any(g=>g.Id==p.PartId)).ToArray();
            foreach(var gun in guns)Assert.That(origins.Count(p=>p.PartId==gun.Id),Is.EqualTo(2),"Bonus rockets must be shared across launchers.");
            float deadline=Time.realtimeSinceStartup+5;
            while(Time.realtimeSinceStartup<deadline&&targets.Any(target=>target.CurrentHp>=500))yield return MOVSeriesDeviceTests.FixedStep();
            Assert.That(targets.All(target=>target.CurrentHp<500),Is.True,"Both launchers must physically hit their targets.");
        }
        finally{foreach(var target in targets)if(target!=null)Object.Destroy(target.gameObject);foreach(var dummy in dummies)if(dummy!=null)Object.Destroy(dummy);if(fixtureOwner!=null)Object.Destroy(fixtureOwner);if(vehicle!=null)Object.Destroy(vehicle.gameObject);}
        yield return null;
    }
    [UnityTest] public IEnumerator ShotgunCopiesFireBeforeBonusPelletsConsumeRemainingSlots()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(2000);
        var first=model.Parts.First(p=>p.Kind==R3PartKind.Gun);first.Weapon=R4WeaponKind.Shotgun;
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,R3PartKind.Gun,false,R4WeaponKind.Shotgun)==null);
        Assert.That(model.Buy(cell,R3PartKind.Gun,R4WeaponKind.Shotgun),Is.True);
        cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,R3PartKind.DoubleShot)==null);
        Assert.That(model.Buy(cell,R3PartKind.DoubleShot),Is.True);model.Economy.DoubleShotChancePerUnit=100;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        var body=vehicle.GetComponent<Rigidbody>();body.isKinematic=true;var assembly=vehicle.GetComponent<R3VehicleAssembly>();
        var guns=assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun&&p.Weapon==R4WeaponKind.Shotgun).ToArray();Assert.That(guns.Length,Is.EqualTo(2));
        for(int i=0;i<guns.Length;i++)
        {
            var muzzle=new GameObject("Unblocked shotgun muzzle").transform;muzzle.SetParent(guns[i].transform,false);
            muzzle.position=new Vector3(1000+i*8,1000,1000);muzzle.rotation=Quaternion.identity;guns[i].GunPivot=muzzle;guns[i].Muzzle=muzzle;
            var binding=guns[i].GetComponent<RRLShotgunBinding>();Assert.That(binding,Is.Not.Null);
            typeof(RRLShotgunBinding).GetField("muzzle",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(binding,muzzle);
            binding.Module.Bind(body,muzzle,vehicle.gameObject);
        }
        var targets=guns.Select(g=>{var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=g.Muzzle.position+Vector3.forward*5;
            target.transform.localScale=new Vector3(3,3,.5f);var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);return hp;}).ToArray();
        Physics.SyncTransforms();Assert.That(guns.All(assembly.ClearProjectilePath),Is.True);
        var dummies=new System.Collections.Generic.List<GameObject>();
        try
        {
            while(P17PhysicalShell.AvailableSlots>20){var dummy=new GameObject("Projectile pool fixture");dummies.Add(dummy);dummy.AddComponent<P17PhysicalShell>();Assert.That(dummies.Count,Is.LessThan(200));}
            int slot=System.Array.FindIndex(model.GetWeaponSlots(),w=>w==R4WeaponKind.Shotgun);Assert.That(slot,Is.GreaterThanOrEqualTo(0));
            float before=vehicle.GetComponent<VehicleEnergyBank>().Current;assembly.FireSlot(slot);
            Assert.That(guns.All(g=>g.NextFireTime>Time.time),Is.True,"Every shotgun must fire its full primary volley before bonus pellets fill the pool.");
            Assert.That(guns.All(g=>g.GetComponent<RRLShotgunBinding>().State.Ammo==RRLShotgunBinding.MagazineSalvos-1),Is.True);
            Assert.That(before-vehicle.GetComponent<VehicleEnergyBank>().Current,Is.EqualTo(2*VehicleEnergyCosts.Shotgun).Within(.001));
            var origins=Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>guns.Any(g=>g.Id==p.PartId)).ToArray();
            foreach(var gun in guns)
            {
                var shots=origins.Where(p=>p.PartId==gun.Id).ToArray();Assert.That(shots.Length,Is.EqualTo(10),"Bonus pellets must be shared across shotguns.");
                for(int i=0;i<shots.Length;i++)for(int j=i+1;j<shots.Length;j++)
                    Assert.That(Physics.GetIgnoreCollision(shots[i].GetComponent<Collider>(),shots[j].GetComponent<Collider>()),Is.True);
            }
            float deadline=Time.realtimeSinceStartup+5;
            while(Time.realtimeSinceStartup<deadline&&targets.Any(target=>target.CurrentHp>=500))yield return MOVSeriesDeviceTests.FixedStep();
            Assert.That(targets.All(target=>target.CurrentHp<500),Is.True,"Both shotguns must physically hit their targets.");
        }
        finally{foreach(var target in targets)if(target!=null)Object.Destroy(target.gameObject);foreach(var dummy in dummies)if(dummy!=null){dummy.SetActive(false);Object.Destroy(dummy);}if(vehicle!=null)Object.Destroy(vehicle.gameObject);}
        yield return null;
    }
    [UnityTest] public IEnumerator RailgunCopiesFireBeforeBonusShotsConsumeRemainingSlots()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(2000);
        var first=model.Parts.First(p=>p.Kind==R3PartKind.Gun);first.Weapon=R4WeaponKind.Railgun;
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,R3PartKind.Gun,false,R4WeaponKind.Railgun)==null);
        Assert.That(model.Buy(cell,R3PartKind.Gun,R4WeaponKind.Railgun),Is.True);
        cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,R3PartKind.DoubleShot)==null);
        Assert.That(model.Buy(cell,R3PartKind.DoubleShot),Is.True);model.Economy.DoubleShotChancePerUnit=100;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        var body=vehicle.GetComponent<Rigidbody>();body.isKinematic=true;var assembly=vehicle.GetComponent<R3VehicleAssembly>();
        var guns=assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun&&p.Weapon==R4WeaponKind.Railgun).ToArray();Assert.That(guns.Length,Is.EqualTo(2));
        for(int i=0;i<guns.Length;i++)
        {
            var muzzle=new GameObject("Unblocked rail muzzle").transform;muzzle.SetParent(guns[i].transform,false);
            muzzle.position=new Vector3(1000+i*8,1000,1000);muzzle.rotation=Quaternion.identity;guns[i].GunPivot=muzzle;guns[i].Muzzle=muzzle;
            var binding=guns[i].GetComponent<RRLRailgunBinding>();Assert.That(binding,Is.Not.Null);
            typeof(RRLRailgunBinding).GetField("muzzle",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(binding,muzzle);
            binding.Module.Bind(body,muzzle,vehicle.gameObject);
        }
        var targets=guns.Select(g=>{var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=g.Muzzle.position+Vector3.forward*5;
            target.transform.localScale=new Vector3(2,2,.5f);var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);return hp;}).ToArray();
        Physics.SyncTransforms();Assert.That(guns.All(assembly.ClearProjectilePath),Is.True);
        var dummies=new System.Collections.Generic.List<GameObject>();
        try
        {
            while(P17PhysicalShell.AvailableSlots>4){var dummy=new GameObject("Projectile pool fixture");dummies.Add(dummy);dummy.AddComponent<P17PhysicalShell>();Assert.That(dummies.Count,Is.LessThan(200));}
            int slot=System.Array.FindIndex(model.GetWeaponSlots(),w=>w==R4WeaponKind.Railgun);Assert.That(slot,Is.GreaterThanOrEqualTo(0));
            float before=vehicle.GetComponent<VehicleEnergyBank>().Current;assembly.FireSlot(slot);
            Assert.That(guns.All(g=>g.NextFireTime>Time.time),Is.True,"Every ready railgun must fire before bonus shots fill the pool.");
            Assert.That(guns.All(g=>g.GetComponent<RRLRailgunBinding>().State.Ammo==RRLRailgunBinding.Magazine-1),Is.True);
            Assert.That(before-vehicle.GetComponent<VehicleEnergyBank>().Current,Is.EqualTo(2*VehicleEnergyCosts.Railgun).Within(.001));
            var origins=Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>guns.Any(g=>g.Id==p.PartId)).ToArray();
            foreach(var gun in guns)Assert.That(origins.Count(p=>p.PartId==gun.Id),Is.EqualTo(2),"Bonus shots must be shared across railguns.");
            float deadline=Time.realtimeSinceStartup+5;
            while(Time.realtimeSinceStartup<deadline&&targets.Any(target=>target.CurrentHp>=500))yield return MOVSeriesDeviceTests.FixedStep();
            Assert.That(targets.All(target=>target.CurrentHp<500),Is.True,"Both railguns must physically hit their targets.");
        }
        finally{foreach(var target in targets)if(target!=null)Object.Destroy(target.gameObject);foreach(var dummy in dummies)if(dummy!=null){dummy.SetActive(false);Object.Destroy(dummy);}if(vehicle!=null)Object.Destroy(vehicle.gameObject);}
        yield return null;
    }
    [UnityTest] public IEnumerator ConventionalWeaponCopiesFireBeforeExtraShotsConsumeRemainingSlots()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(2000);
        var firstState=model.Parts.First(p=>p.Kind==R3PartKind.Gun);firstState.Weapon=R4WeaponKind.Automatic;
        var gunCell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,R3PartKind.Gun,false,R4WeaponKind.Automatic)==null);
        Assert.That(model.Buy(gunCell,R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
        var bonusCell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,R3PartKind.DoubleShot)==null);
        Assert.That(model.Buy(bonusCell,R3PartKind.DoubleShot),Is.True);model.Economy.DoubleShotChancePerUnit=100;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;var assembly=vehicle.GetComponent<R3VehicleAssembly>();
        var guns=assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun&&p.Weapon==R4WeaponKind.Automatic).ToArray();Assert.That(guns.Length,Is.EqualTo(2));
        for(int i=0;i<guns.Length;i++)
        {var muzzle=new GameObject("Unblocked gun muzzle").transform;muzzle.SetParent(guns[i].transform,false);muzzle.position=new Vector3(1000+i*3,1000,1000);muzzle.rotation=Quaternion.identity;guns[i].GunPivot=muzzle;guns[i].Muzzle=muzzle;}
        var targets=guns.Select(g=>{var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=g.Muzzle.position+Vector3.forward*5;
            target.transform.localScale=new Vector3(2,2,.5f);var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);return hp;}).ToArray();
        Physics.SyncTransforms();Assert.That(guns.All(assembly.ClearProjectilePath),Is.True);
        var dummies=new System.Collections.Generic.List<GameObject>();
        try
        {
            while(P17PhysicalShell.AvailableSlots>4){var dummy=new GameObject("Projectile pool fixture");dummies.Add(dummy);dummy.AddComponent<P17PhysicalShell>();Assert.That(dummies.Count,Is.LessThan(200));}
            Assert.That(P17PhysicalShell.AvailableSlots,Is.EqualTo(4));
            int slot=System.Array.FindIndex(model.GetWeaponSlots(),w=>w==R4WeaponKind.Automatic);Assert.That(slot,Is.GreaterThanOrEqualTo(0));
            float before=vehicle.GetComponent<VehicleEnergyBank>().Current;assembly.FireSlot(slot);
            Assert.That(guns.All(g=>g.NextFireTime>Time.time),Is.True,"Each ready physical gun must fire before bonus shots consume the pool.");
            Assert.That(before-vehicle.GetComponent<VehicleEnergyBank>().Current,Is.EqualTo(2*VehicleEnergyCosts.ConventionalShot(R4WeaponKind.Automatic)).Within(.001));
            var origins=Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>guns.Any(g=>g.Id==p.PartId)).ToArray();
            Assert.That(origins.Select(p=>p.PartId).Distinct().Count(),Is.EqualTo(2));
            foreach(var gun in guns)
            {
                var shots=origins.Where(p=>p.PartId==gun.Id).ToArray();Assert.That(shots.Length,Is.EqualTo(2),"Available bonus shots must be shared across physical guns.");
                Assert.That(Physics.GetIgnoreCollision(shots[0].GetComponent<Collider>(),shots[1].GetComponent<Collider>()),Is.True,"A bonus projectile must not collide with its own primary shot.");
            }
            float deadline=Time.realtimeSinceStartup+5;
            while(Time.realtimeSinceStartup<deadline&&targets.Any(target=>target.CurrentHp>=500))yield return MOVSeriesDeviceTests.FixedStep();
            foreach(var target in targets)Assert.That(target.CurrentHp,Is.EqualTo(500-2*R4WeaponDefinition.Get(R4WeaponKind.Automatic).Damage).Within(.2f),"Both the primary and bonus projectile must physically reach their target.");
        }
        finally{foreach(var target in targets)if(target!=null)Object.Destroy(target.gameObject);foreach(var item in dummies)if(item!=null){item.SetActive(false);Object.Destroy(item);}if(vehicle!=null)Object.Destroy(vehicle.gameObject);}
        yield return null;
    }
    [UnityTest] public IEnumerator PrimaryShotsStillWorkWhenShellBonusBudgetIsFull()
    {
        var dummies=new System.Collections.Generic.List<GameObject>();
        R3VehicleAssembly assembly=null;GameObject target=null;
        try
        {
            while(P17PhysicalShell.AvailableSlots>0)
            {
                var dummy=new GameObject("Projectile pool fixture");dummies.Add(dummy);dummy.AddComponent<P17PhysicalShell>();
                Assert.That(dummies.Count,Is.LessThan(200));
            }
            foreach(var kind in new[]{R4WeaponKind.Automatic,R4WeaponKind.Shotgun,R4WeaponKind.Railgun})
            {
                var gun=Build(kind,out var bank);assembly=gun.GetComponentInParent<R3VehicleAssembly>();
                var muzzle=new GameObject("Unblocked QA muzzle").transform;muzzle.SetParent(gun.transform,false);
                muzzle.position=new Vector3(1000,1000,1000);muzzle.rotation=Quaternion.identity;gun.GunPivot=muzzle;gun.Muzzle=muzzle;
                var body=assembly.GetComponent<Rigidbody>();
                if(kind==R4WeaponKind.Shotgun)
                {
                    var binding=gun.GetComponent<RRLShotgunBinding>();
                    typeof(RRLShotgunBinding).GetField("muzzle",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(binding,muzzle);
                    binding.Module.Bind(body,muzzle,assembly.gameObject);
                }
                if(kind==R4WeaponKind.Railgun)
                {
                    var binding=gun.GetComponent<RRLRailgunBinding>();
                    typeof(RRLRailgunBinding).GetField("muzzle",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(binding,muzzle);
                    binding.Module.Bind(body,muzzle,assembly.gameObject);
                }
                target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=muzzle.position+Vector3.forward*5;
                target.transform.localScale=new Vector3(3,3,.5f);var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);
                Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(gun),Is.True);
                float before=bank.Current;int slot=System.Array.FindIndex(assembly.Model.GetWeaponSlots(),w=>w==kind);
                Assert.That(slot,Is.GreaterThanOrEqualTo(0));assembly.FireSlot(slot);
                Assert.That(bank.Current,Is.LessThan(before),$"{kind} must pay for its actual primary shot.");
                Assert.That(gun.NextFireTime,Is.GreaterThan(Time.time),$"{kind} must start its own cooldown.");
                int expected=kind==R4WeaponKind.Shotgun?8:1;
                Assert.That(Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Count(p=>p.PartId==gun.Id),Is.EqualTo(expected));
                float deadline=Time.realtimeSinceStartup+5;
                while(Time.realtimeSinceStartup<deadline&&hp.CurrentHp>=500)yield return MOVSeriesDeviceTests.FixedStep();
                Assert.That(hp.CurrentHp,Is.LessThan(500),$"{kind} must physically hit even when the bonus budget is full.");
                Object.Destroy(target);target=null;Object.Destroy(assembly.gameObject);assembly=null;yield return null;
            }
        }
        finally
        {
            foreach(var item in dummies)if(item!=null){item.SetActive(false);Object.Destroy(item);}
            if(target!=null)Object.Destroy(target);
            if(assembly!=null)Object.Destroy(assembly.gameObject);
        }
        yield return null;
    }
    [UnityTest] public IEnumerator ConventionalShellCanBeReusedAfterHitWithoutStaleOwnerOrDamage()
    {
        var firstGun=Build(R4WeaponKind.Automatic,out _);var firstAssembly=firstGun.GetComponentInParent<R3VehicleAssembly>();
        var firstMuzzle=new GameObject("First reused shell muzzle").transform;firstMuzzle.SetParent(firstGun.transform,false);
        firstMuzzle.position=new Vector3(1000,1000,1000);firstMuzzle.rotation=Quaternion.identity;
        var firstTarget=GameObject.CreatePrimitive(PrimitiveType.Cube);firstTarget.transform.position=firstMuzzle.position+Vector3.forward*5;
        firstTarget.transform.localScale=new Vector3(2,2,.5f);var firstHp=firstTarget.AddComponent<P12PipelineSafeDamageable>();firstHp.Configure(500,false);
        R3VehicleAssembly secondAssembly=null;GameObject secondTarget=null;
        try
        {
            Physics.SyncTransforms();
            var first=firstAssembly.GetComponent<P12PipelineSafeTankController>().FirePrimaryFrom(firstMuzzle,R4WeaponKind.Automatic,firstGun.Id);
            Assert.That(first,Is.Not.Null);ulong firstLaunch=first.GetComponent<P17PhysicalShell>().LaunchSequence;
            Assert.That(firstLaunch,Is.GreaterThan(0));var firstOwnerCollider=firstAssembly.GetComponentsInChildren<Collider>().First(c=>c.enabled&&!c.isTrigger);
            Assert.That(Physics.GetIgnoreCollision(first,firstOwnerCollider),Is.True);
            float deadline=Time.realtimeSinceStartup+5;
            while(Time.realtimeSinceStartup<deadline&&firstHp.CurrentHp>=500)yield return MOVSeriesDeviceTests.FixedStep();
            Assert.That(firstHp.CurrentHp,Is.EqualTo(476).Within(.2f));yield return null;yield return null;
            Assert.That(first,Is.Not.Null,"A returned projectile should be available for reuse after its hit.");
            var secondGun=Build(R4WeaponKind.Automatic,out _);secondGun.Tier=1;secondAssembly=secondGun.GetComponentInParent<R3VehicleAssembly>();
            var secondMuzzle=new GameObject("Second reused shell muzzle").transform;secondMuzzle.SetParent(secondGun.transform,false);
            secondMuzzle.position=new Vector3(1010,1000,1000);secondMuzzle.rotation=Quaternion.identity;
            secondTarget=GameObject.CreatePrimitive(PrimitiveType.Cube);secondTarget.transform.position=secondMuzzle.position+Vector3.forward*5;
            secondTarget.transform.localScale=new Vector3(2,2,.5f);var secondHp=secondTarget.AddComponent<P12PipelineSafeDamageable>();secondHp.Configure(500,false);
            Physics.SyncTransforms();
            var second=secondAssembly.GetComponent<P12PipelineSafeTankController>().FirePrimaryFrom(secondMuzzle,R4WeaponKind.Automatic,secondGun.Id);
            Assert.That(second,Is.SameAs(first));
            Assert.That(second.GetComponent<P17PhysicalShell>().LaunchSequence,Is.Not.EqualTo(firstLaunch),"Every reused physical object must represent a new identifiable shot.");
            Assert.That(second.GetComponent<P17PhysicalShell>().OwnerRoot,Is.EqualTo(secondAssembly.transform));
            Assert.That(second.GetComponent<R4ProjectileOrigin>().PartId,Is.EqualTo(secondGun.Id));
            var secondOwnerCollider=secondAssembly.GetComponentsInChildren<Collider>().First(c=>c.enabled&&!c.isTrigger);
            Assert.That(Physics.GetIgnoreCollision(second,secondOwnerCollider),Is.True,"The new owner must remain protected.");
            Assert.That(Physics.GetIgnoreCollision(second,firstOwnerCollider),Is.False,"The previous owner's filter must not leak to the new shot.");
            deadline=Time.realtimeSinceStartup+5;
            while(Time.realtimeSinceStartup<deadline&&secondHp.CurrentHp>=500)yield return MOVSeriesDeviceTests.FixedStep();
            Assert.That(secondHp.CurrentHp,Is.EqualTo(488).Within(.2f),"Reused shots must use the new gun's tier and damage.");
        }
        finally
        {
            if(firstTarget!=null)Object.Destroy(firstTarget);if(secondTarget!=null)Object.Destroy(secondTarget);
            if(firstAssembly!=null)Object.Destroy(firstAssembly.gameObject);if(secondAssembly!=null)Object.Destroy(secondAssembly.gameObject);
        }
        yield return null;
    }
    [UnityTest] public IEnumerator CachedShellLifetimePausesAndExpiredShellCanBeReused()
    {
        var gun=Build(R4WeaponKind.Automatic,out _);var assembly=gun.GetComponentInParent<R3VehicleAssembly>();
        var muzzle=new GameObject("Lifetime shell muzzle").transform;muzzle.SetParent(gun.transform,false);
        muzzle.position=new Vector3(1000,1000,1000);muzzle.rotation=Quaternion.identity;
        var controller=assembly.GetComponent<P12PipelineSafeTankController>();
        typeof(P12PipelineSafeTankController).GetField("projectileLifetime",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(controller,.5f);
        var first=controller.FirePrimaryFrom(muzzle,R4WeaponKind.Automatic,gun.Id);
        Assert.That(first,Is.Not.Null);run.TogglePause();Assert.That(Time.timeScale,Is.Zero);
        float pauseDeadline=Time.realtimeSinceStartup+.8f;
        while(Time.realtimeSinceStartup<pauseDeadline)yield return null;
        Assert.That(first.gameObject.activeInHierarchy,Is.True,"Pausing must not expire a projectile.");
        run.TogglePause();float deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&first.gameObject.activeInHierarchy)yield return null;
        Assert.That(first.gameObject.activeInHierarchy,Is.False,"The projectile must expire after its scaled lifetime.");
        yield return null;
        var second=controller.FirePrimaryFrom(muzzle,R4WeaponKind.Automatic,gun.Id);
        Assert.That(second,Is.SameAs(first),"An expired projectile may be reused after its retirement frame.");
        R1EncounterManager.ClearTransientObjects();if(assembly!=null)Object.Destroy(assembly.gameObject);yield return null;
    }
    [UnityTest] public IEnumerator PointDefenseCanInterceptAReusedConventionalShell()
    {
        var gun=Build(R4WeaponKind.Automatic,out _);var assembly=gun.GetComponentInParent<R3VehicleAssembly>();
        var muzzle=new GameObject("Defense shell muzzle").transform;muzzle.SetParent(gun.transform,false);
        muzzle.position=new Vector3(1000,1000,1000);muzzle.rotation=Quaternion.identity;
        var controller=assembly.GetComponent<P12PipelineSafeTankController>();
        yield return null; // Allow the builder's disabled visual colliders to be destroyed.
        var first=controller.FirePrimaryFrom(muzzle,R4WeaponKind.Automatic,gun.Id);
        var threat=first.GetComponent<RRLPointDefenseThreat>();Assert.That(threat.IsActiveThreat,Is.True);
        Assert.That(threat.TryIntercept(1),Is.True);Assert.That(threat.IsActiveThreat,Is.False);
        yield return null;yield return null;
        var second=controller.FirePrimaryFrom(muzzle,R4WeaponKind.Automatic,gun.Id);
        Assert.That(second,Is.SameAs(first));var secondThreat=second.GetComponent<RRLPointDefenseThreat>();
        Assert.That(secondThreat.IsActiveThreat,Is.True);Assert.That(secondThreat.TryIntercept(1),Is.True);
        Assert.That(secondThreat.IsActiveThreat,Is.False);
        R1EncounterManager.ClearTransientObjects();if(assembly!=null)Object.Destroy(assembly.gameObject);yield return null;
    }
    [UnityTest] public IEnumerator ReturnedShellWaitsAFrameAndEncounterCleanupClearsTheCache()
    {
        var gun=Build(R4WeaponKind.Automatic,out _);var assembly=gun.GetComponentInParent<R3VehicleAssembly>();
        var muzzle=new GameObject("Same frame shell muzzle").transform;muzzle.SetParent(gun.transform,false);
        muzzle.position=new Vector3(1000,1000,1000);muzzle.rotation=Quaternion.identity;
        var controller=assembly.GetComponent<P12PipelineSafeTankController>();
        var first=controller.FirePrimaryFrom(muzzle,R4WeaponKind.Automatic,gun.Id);
        Assert.That(first.GetComponent<P17PhysicalShell>().Intercept(1),Is.True);
        var second=controller.FirePrimaryFrom(muzzle,R4WeaponKind.Automatic,gun.Id);
        Assert.That(second,Is.Not.SameAs(first),"A callback may still hold the previous shot for the rest of this frame.");
        Assert.That(second.GetComponent<P17PhysicalShell>().Intercept(1),Is.True);
        R1EncounterManager.ClearTransientObjects();yield return null;
        Assert.That(first==null&&second==null,Is.True,"Encounter cleanup must also destroy inactive cached projectiles.");
        if(assembly!=null)Object.Destroy(assembly.gameObject);yield return null;
    }
    [UnityTest] public IEnumerator ShellWithInactiveCollisionCounterpartIsDiscardedInsteadOfReused()
    {
        var gun=Build(R4WeaponKind.Automatic,out _);var assembly=gun.GetComponentInParent<R3VehicleAssembly>();
        var muzzle=new GameObject("Inactive counterpart muzzle").transform;muzzle.SetParent(gun.transform,false);
        muzzle.position=new Vector3(1000,1000,1000);muzzle.rotation=Quaternion.identity;
        var controller=assembly.GetComponent<P12PipelineSafeTankController>();
        var first=controller.FirePrimaryFrom(muzzle,R4WeaponKind.Automatic,gun.Id);
        var own=assembly.GetComponentsInChildren<Collider>().First(c=>c.enabled&&!c.isTrigger&&Physics.GetIgnoreCollision(first,c));
        own.enabled=false;
        Assert.That(first.GetComponent<P17PhysicalShell>().Intercept(1),Is.True);
        own.enabled=true;yield return null;yield return null;
        Assert.That(first==null,Is.True,"A shell with an unsafe retained collision filter must be destroyed.");
        var second=controller.FirePrimaryFrom(muzzle,R4WeaponKind.Automatic,gun.Id);
        Assert.That(Physics.GetIgnoreCollision(second,own),Is.True,"A fresh shell must still ignore the current owner.");
        R1EncounterManager.ClearTransientObjects();if(assembly!=null)Object.Destroy(assembly.gameObject);yield return null;
    }
    [UnityTest] public IEnumerator ProjectileCacheRetentionDoesNotLimitFuturePrimaryShots()
    {
        var gun=Build(R4WeaponKind.Automatic,out _);var assembly=gun.GetComponentInParent<R3VehicleAssembly>();
        var muzzle=new GameObject("Cache capacity muzzle").transform;muzzle.SetParent(gun.transform,false);
        muzzle.position=new Vector3(1000,1000,1000);muzzle.rotation=Quaternion.identity;
        var controller=assembly.GetComponent<P12PipelineSafeTankController>();yield return null;
        R1EncounterManager.ClearTransientObjects();
        for(int i=0;i<260;i++)
        {
            var shot=controller.FirePrimaryFrom(muzzle,R4WeaponKind.Automatic,gun.Id);
            Assert.That(shot,Is.Not.Null);Assert.That(shot.GetComponent<P17PhysicalShell>().Intercept(1),Is.True);
        }
        yield return null;
        var live=new System.Collections.Generic.HashSet<int>();
        for(int i=0;i<260;i++)
        {
            var shot=controller.FirePrimaryFrom(muzzle,R4WeaponKind.Automatic,gun.Id);
            Assert.That(shot,Is.Not.Null);Assert.That(live.Add(shot.GetInstanceID()),Is.True,"Simultaneous primary shots must remain independent.");
        }
        Assert.That(Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Count(p=>p.PartId==gun.Id),Is.EqualTo(260));
        R1EncounterManager.ClearTransientObjects();if(assembly!=null)Object.Destroy(assembly.gameObject);yield return null;
    }
    [UnityTest] public IEnumerator MoreThanNinetySixRealPrimaryShellsCanHitSeparateTargets()
    {
        var gun=Build(R4WeaponKind.Automatic,out _);var assembly=gun.GetComponentInParent<R3VehicleAssembly>();
        var controller=assembly.GetComponent<P12PipelineSafeTankController>();
        var muzzle=new GameObject("Many projectile muzzle").transform;muzzle.SetParent(gun.transform,false);muzzle.rotation=Quaternion.identity;
        var targets=new System.Collections.Generic.List<P12PipelineSafeDamageable>();
        try
        {
            for(int i=0;i<110;i++)
            {
                muzzle.position=new Vector3(1000+i*2,1000,1000);
                var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=muzzle.position+Vector3.forward*5;
                target.transform.localScale=new Vector3(.8f,.8f,.5f);var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(100,false);targets.Add(hp);
                Assert.That(controller.FirePrimaryFrom(muzzle,R4WeaponKind.Automatic,gun.Id),Is.Not.Null);
            }
            Physics.SyncTransforms();
            Assert.That(Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Count(p=>p.PartId==gun.Id),Is.EqualTo(110));
            float deadline=Time.realtimeSinceStartup+8;
            while(Time.realtimeSinceStartup<deadline&&targets.Any(t=>t.CurrentHp>=100))yield return MOVSeriesDeviceTests.FixedStep();
            Assert.That(targets.Count(t=>t.CurrentHp<100),Is.EqualTo(110),"All simultaneous real projectiles must reach their targets.");
        }
        finally
        {
            foreach(var target in targets)if(target!=null)Object.Destroy(target.gameObject);
            if(assembly!=null)Object.Destroy(assembly.gameObject);
        }
        yield return null;
    }
    [UnityTest] public IEnumerator PrimaryRocketStillWorksWhenRocketBonusBudgetIsFull()
    {
        var gun=Build(R4WeaponKind.RocketLauncher,out var bank);var assembly=gun.GetComponentInParent<R3VehicleAssembly>();
        var muzzle=new GameObject("Unblocked rocket muzzle").transform;muzzle.SetParent(gun.transform,false);
        muzzle.position=new Vector3(1000,1000,1000);muzzle.rotation=Quaternion.identity;gun.GunPivot=muzzle;gun.Muzzle=muzzle;
        var binding=gun.GetComponent<RRLRocketBinding>();
        typeof(RRLRocketBinding).GetField("muzzle",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(binding,muzzle);
        binding.Module.Bind(assembly.GetComponent<Rigidbody>(),assembly.transform,muzzle);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=muzzle.position+Vector3.forward*5;
        target.transform.localScale=new Vector3(2,2,.5f);var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);target.AddComponent<RRLRocketDamageReceiver>();
        var fixtureOwner=new GameObject("Rocket pool fixture owner");var fixtureBody=fixtureOwner.AddComponent<Rigidbody>();fixtureBody.isKinematic=true;
        var dummies=new System.Collections.Generic.List<GameObject>();
        try
        {
            var spec=new RoboRougelike.Mechanics.Rockets.RocketConfig().Snapshot();
            for(int i=0;i<32;i++)
            {
                var dummy=new GameObject("Rocket pool fixture");dummies.Add(dummy);dummy.transform.position=new Vector3(2000+i*3,2000,2000);
                var rocket=dummy.AddComponent<RoboRougelike.Mechanics.Rockets.RocketProjectile>();
                Assert.That(rocket.TryInitialize(spec,fixtureBody,fixtureOwner.transform,fixtureOwner,null,Vector3.forward),Is.True);
            }
            Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(gun),Is.True);
            float before=bank.Current;int slot=System.Array.FindIndex(assembly.Model.GetWeaponSlots(),w=>w==R4WeaponKind.RocketLauncher);
            Assert.That(slot,Is.GreaterThanOrEqualTo(0));assembly.FireSlot(slot);
            Assert.That(bank.Current,Is.LessThan(before));Assert.That(gun.NextFireTime,Is.GreaterThan(Time.time));
            Assert.That(binding.State.Ammo,Is.EqualTo(RRLRocketBinding.Magazine-1));
            Assert.That(Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Count(p=>p.PartId==gun.Id),Is.EqualTo(1));
            float deadline=Time.realtimeSinceStartup+5;
            while(Time.realtimeSinceStartup<deadline&&hp.CurrentHp>=500)yield return MOVSeriesDeviceTests.FixedStep();
            Assert.That(hp.CurrentHp,Is.LessThan(500),"The primary rocket must hit even when the bonus budget is full.");
        }
        finally
        {
            foreach(var dummy in dummies)if(dummy!=null)Object.Destroy(dummy);
            if(fixtureOwner!=null)Object.Destroy(fixtureOwner);if(target!=null)Object.Destroy(target);
            if(assembly!=null)Object.Destroy(assembly.gameObject);
        }
        yield return null;
    }
    [UnityTest] public IEnumerator ConventionalProjectileLaunchedInsideOwnerReachesTargetWithoutDamagingOwner()
    {
        var gun=Build(R4WeaponKind.Basic,out _);var assembly=gun.GetComponentInParent<R3VehicleAssembly>();
        var core=assembly.GetComponent<R1VehicleState>().Core;float initial=core.CurrentHp;
        var muzzle=new GameObject("Owner interior muzzle").transform;muzzle.SetParent(core.transform,false);
        muzzle.position=core.transform.position;muzzle.rotation=Quaternion.identity;
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=muzzle.position+Vector3.forward*6;
        target.transform.localScale=new Vector3(3,3,.5f);
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);Physics.SyncTransforms();
        assembly.GetComponent<P12PipelineSafeTankController>().FireFrom(muzzle,R4WeaponKind.Basic,"owner-safety");
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>=500&&Time.realtimeSinceStartup<deadline)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(hp.CurrentHp,Is.LessThan(500),"A shot must be able to leave its own physical construction.");
        Assert.That(core.CurrentHp,Is.EqualTo(initial),"The shooter's core must remain undamaged.");
        Object.Destroy(target);Object.Destroy(assembly.gameObject);yield return null;
    }
    R3MountedPart Build(R4WeaponKind weapon,out VehicleEnergyBank bank,bool doubleShot=false,R1UpgradeDefinition bonus=null)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=weapon;state.Tier=2;
        if(bonus!=null)
        {
            Assert.That(model.ReceiveBonuses(new[]{bonus}),Is.True);var id=model.Inventory.Single().Id;
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct().First(c=>model.OwnedPlacementError(id,c,default)==null);
            Assert.That(model.Mount(id,cell),Is.True);
        }
        if(doubleShot)
        {
            model.Economy.DoubleShotChancePerUnit=1;model.Award(1000);
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.PlacementError(c,R3PartKind.DoubleShot)==null);
            Assert.That(model.Buy(cell,R3PartKind.DoubleShot),Is.True);
        }
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;bank=vehicle.GetComponent<VehicleEnergyBank>();
        return vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
    }
    P12PipelineSafeDamageable Target(R3MountedPart gun,float distance,float width=4)
    {
        var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.transform.position=gun.Muzzle.position+gun.Muzzle.forward*distance;
        item.transform.localScale=new Vector3(width,4,1);
        var hp=item.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);Physics.SyncTransforms();return hp;
    }
    [UnityTest] public IEnumerator CargoUplinkCollectsPhysicalLootAtExtendedRangeWithoutDuplicatingIt()
    {
        Assert.That(System.Enum.TryParse("CargoUplink",out R3PartKind kind),Is.True);
        var model=run.VehicleModel;
        // Arrange a mounted module after the common fixture has already entered combat.
        // The normal garage-only installation gate is covered by the model/garage tests.
        typeof(R3VehicleModel).GetMethod("SetInstallationGate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
            .Invoke(model,new object[]{null});
        model.Award(1000);Assert.That(model.PurchaseToInventory(kind),Is.True);
        var owned=model.Inventory.Last(p=>p.Kind==kind);
        var candidates=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .Select(c=>new { Cell=c, Error=model.OwnedPlacementError(owned.Id,c,default) }).ToArray();
        Assert.That(candidates.Any(c=>c.Error==null),Is.True,"No legal mounting cell: "+
            string.Join("; ",candidates.GroupBy(c=>c.Error).Select(g=>$"{g.Key}: {g.Count()}")));
        var cell=candidates.First(c=>c.Error==null).Cell;
        Assert.That(model.Mount(owned.Id,cell),Is.True);
        var previous=run.Player;previous.gameObject.SetActive(false);
        var player=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,10,100),Quaternion.identity);
        typeof(R1RunController).GetProperty("Player").GetSetMethod(true).Invoke(run,new object[]{player});
        var assembly=player.GetComponent<R3VehicleAssembly>();var body=player.GetComponent<Rigidbody>();body.isKinematic=true;
        var cargo=assembly.Parts.Single(p=>p.Id==owned.Id);
        var radius=typeof(R3VehicleAssembly).GetProperty("CargoPickupRadius");Assert.That(radius,Is.Not.Null);
        Assert.That((float)radius.GetValue(assembly),Is.EqualTo(2.1f).Within(.01));
        var cargoEmp=cargo.GetComponent<RoboRougelike.Mechanics.EMP.EmpReceiver>();Assert.That(cargoEmp,Is.Not.Null);
        cargoEmp.ApplyEmp(3);Assert.That((float)radius.GetValue(assembly),Is.EqualTo(1.1f).Within(.01));
        cargoEmp.ClearEmp();Assert.That((float)radius.GetValue(assembly),Is.EqualTo(2.1f).Within(.01));
        var item=new R4SalvageItem("cargo-test-part","enemy",R3PartKind.Gun,R4WeaponKind.Basic,.6f);
        Assert.That(run.Salvage.Record("cargo-test-enemy",new[]{item}),Is.True);
        Physics.SyncTransforms();var edge=player.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger)
            .OrderByDescending(c=>c.bounds.max.x).First().bounds;
        R3PhysicalLoot.Spawn(edge.center+Vector3.right*(edge.extents.x+1.6f),run.Salvage,item.Id);
        var pickup=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None).Single(p=>p.ItemId==item.Id);
        pickup.transform.position=edge.center+Vector3.right*(edge.extents.x+1.6f);pickup.GetComponent<Rigidbody>().isKinematic=true;
        cargo.gameObject.SetActive(false);Physics.SyncTransforms();
        for(int i=0;i<8;i++)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(model.Owns(item.Id),Is.False,"A disabled module cannot extend pickup range.");
        cargo.gameObject.SetActive(true);Physics.SyncTransforms();float deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&!model.Owns(item.Id))yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(model.Owns(item.Id),Is.True,"The physical loot is collected when the module extends the range.");
        Assert.That(model.Inventory.Count(p=>p.Id==item.Id),Is.EqualTo(1));Assert.That(run.Salvage.Items.Any(p=>p.Id==item.Id),Is.False);
    }
    [UnityTest] public IEnumerator PhysicalRadarAndMinimapDetectOnlyInRangeAndStopOnLoss()
    {
        var model=run.VehicleModel;
        typeof(R3VehicleModel).GetMethod("SetInstallationGate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
            .Invoke(model,new object[]{null});
        model.Award(1000);
        foreach(var kind in new[]{R3PartKind.RadarModule,R3PartKind.MinimapModule})
        {
            Assert.That(model.PurchaseToInventory(kind),Is.True);var part=model.Inventory.Last(p=>p.Kind==kind);
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.OwnedPlacementError(part.Id,c,default)==null);
            Assert.That(model.Mount(part.Id,cell),Is.True);
        }
        var previous=run.Player;previous.gameObject.SetActive(false);
        var player=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,10,100),Quaternion.identity);
        player.GetComponent<Rigidbody>().isKinematic=true;
        var assembly=player.GetComponent<R3VehicleAssembly>();var radar=assembly.Parts.Single(p=>p.Kind==R3PartKind.RadarModule);
        var map=assembly.Parts.Single(p=>p.Kind==R3PartKind.MinimapModule);
        Assert.That(assembly.PassiveDetectionRange(R3PartKind.RadarModule),Is.EqualTo(50));
        Assert.That(assembly.PassiveDetectionRange(R3PartKind.MinimapModule),Is.EqualTo(50));
        foreach(var e in run.Encounter.Enemies)e.gameObject.SetActive(false);
        var enemy=run.Encounter.Enemies.First();enemy.gameObject.SetActive(true);var enemyBody=enemy.GetComponent<Rigidbody>();enemyBody.isKinematic=true;
        enemyBody.position=player.transform.position+Vector3.forward*40;enemy.transform.position=enemyBody.position;Physics.SyncTransforms();
        var contacts=new System.Collections.Generic.List<Vector3>();R3PassiveRadar.Collect(run,assembly,contacts);
        Assert.That(contacts.Count,Is.EqualTo(1));Assert.That(R3PassiveRadar.MapOffset(player.transform.position,Vector3.forward,contacts[0],50).y,Is.LessThan(0));
        enemyBody.position=player.transform.position+Vector3.forward*60;enemy.transform.position=enemyBody.position;Physics.SyncTransforms();
        R3PassiveRadar.Collect(run,assembly,contacts);Assert.That(contacts,Is.Empty);
        radar.Tier=2;Assert.That(assembly.PassiveDetectionRange(R3PartKind.RadarModule),Is.EqualTo(100));
        R3PassiveRadar.Collect(run,assembly,contacts);Assert.That(contacts.Count,Is.EqualTo(1));
        var receiver=radar.GetComponent<RoboRougelike.Mechanics.EMP.EmpReceiver>();Assert.That(receiver,Is.Not.Null);
        receiver.ApplyEmp(3);R3PassiveRadar.Collect(run,assembly,contacts);Assert.That(contacts,Is.Empty);
        receiver.ClearEmp();R3PassiveRadar.Collect(run,assembly,contacts);Assert.That(contacts.Count,Is.EqualTo(1));
        radar.gameObject.SetActive(false);R3PassiveRadar.Collect(run,assembly,contacts);Assert.That(contacts,Is.Empty);
        Assert.That(assembly.PassiveDetectionRange(R3PartKind.MinimapModule),Is.EqualTo(50));
        map.gameObject.SetActive(false);Assert.That(assembly.PassiveDetectionRange(R3PartKind.MinimapModule),Is.Zero);
        yield return null;
    }
    [UnityTest] public IEnumerator DefeatedEnemyDropsPhysicalPartThatPlayerActuallyCollects()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        var drops=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None);
        Assert.That(drops.Length,Is.GreaterThan(0),"Defeating the real enemy must spawn an owned ledger drop");
        Assert.That(drops.Length,Is.EqualTo(run.Salvage.Items.Count(p=>p.PhysicalLootEligible)),"100% must create one pickup per eligible physical item");
        string id=drops[0].ItemId;Assert.That(run.Salvage.Items.Any(p=>p.Id==id),Is.True);
        for(int i=1;i<drops.Length;i++)drops[i].transform.position+=Vector3.right*(20+i*2);
        var player=run.Player.GetComponent<Rigidbody>();player.isKinematic=true;player.position=drops[0].transform.position;
        Physics.SyncTransforms();float deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&!run.VehicleModel.Owns(id))yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(run.VehicleModel.Owns(id),Is.True,"Touching the physical pickup must grant its actual part");
        Assert.That(run.Salvage.Items.Any(p=>p.Id==id),Is.False,"Collected identity must no longer be offered in rewards");
        Assert.That(run.VehicleModel.Inventory.Count(p=>p.Id==id),Is.EqualTo(1));
        R1EncounterManager.ClearTransientObjects();yield return null;
        Assert.That(Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None),Is.Empty,"Uncollected world objects must be removed on encounter cleanup");
    }
    [UnityTest] public IEnumerator ZeroDropChanceKeepsExistingRewardLedgerWithoutWorldLoot()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=0;
        var enemy=run.Encounter.Enemies.First();run.Encounter.SetCombatActive(true);
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);yield return null;
        Assert.That(Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None),Is.Empty);
        Assert.That(run.Salvage.Items.Count,Is.GreaterThan(0),"Zero world-drop chance must preserve ordinary rewards");
    }
    [UnityTest] public IEnumerator MissedDropCanBePaidForThenBonusClaimedWithoutDuplicatingItem()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;run.Encounter.SetCombatActive(true);
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        var drops=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None);Assert.That(drops.Length,Is.GreaterThan(0));
        string id=drops[0].ItemId;Assert.That(run.TryRecoverMissedLoot(id),Is.False,"Paid recovery is available only after victory");
        var ordinary=new R3PartState(default,R3PartKind.Gun){Id=System.Guid.NewGuid().ToString("N"),Weapon=R4WeaponKind.Basic,Source="test/ordinary"};
        Assert.That(run.Salvage.Record("test/ordinary-enemy",new[]{new R4SalvageItem(ordinary,1)},R3EnemyArchetype.Scout),Is.True);
        yield return null;
        typeof(R1RunController).GetField("pickupGraceDeadline",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(run,Time.time);
        float deadline=Time.realtimeSinceStartup+8;
        while(Time.realtimeSinceStartup<deadline&&run.Progress.State!=R1RunState.Salvage)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        Assert.That(run.Salvage.MissedPhysicalItems.Any(p=>p.Id==id),Is.True);
        Assert.That(run.RecoveryWindow.VisibleOptions.Any(p=>p.InstanceId==id),Is.False);
        Assert.That(run.RecoveryWindow.VisibleOptions.Any(p=>p.InstanceId==ordinary.Id),Is.True);
        Assert.That(run.VehicleModel.TrySpendScrap(run.VehicleModel.Scrap),Is.True);
        var previousWindow=run.RecoveryWindow;
        Assert.That(run.TryRecoverMissedLoot(id),Is.False,"Insufficient funds must leave recovery and rewards intact");
        Assert.That(run.RecoveryWindow,Is.SameAs(previousWindow));Assert.That(run.Salvage.MissedPhysicalItems.Any(p=>p.Id==id),Is.True);
        run.VehicleModel.Award(50);int before=run.VehicleModel.Scrap;
        var quarantine=previousWindow.GetType().GetField("quarantined",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        quarantine.SetValue(previousWindow,true);Assert.That(run.TryRecoverMissedLoot(id),Is.False,"Recovery must not bypass a quarantined reward transaction");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before));quarantine.SetValue(previousWindow,false);
        Assert.That(run.TryRecoverMissedLoot(id),Is.True);Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before-run.VehicleModel.Economy.MissedLootRecoveryCost));
        Assert.That(run.TryRecoverMissedLoot(id),Is.False);Assert.That(run.VehicleModel.Inventory.Count(p=>p.Id==id),Is.EqualTo(1));
        Assert.That(run.RecoveryWindow.VisibleOptions.Any(p=>p.InstanceId==ordinary.Id),Is.True,"Recovering missed loot must preserve ordinary reward offers");
        Assert.That(run.ConfirmRewards(new[]{ordinary.Id},new[]{0}),Is.True);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));Assert.That(run.VehicleModel.Inventory.Count(p=>p.Id==id),Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator DestroyedGunDropsBeforeCoreAndIsNotDuplicatedAtVictory()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        var gun=enemy.WeaponBlock;Assert.That(gun,Is.Not.Null);
        gun.ApplyDamageResolvedTyped(gun.CurrentHp/.9f+1,gun.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        Assert.That(enemy.IsDefeated,Is.False,"The enemy core must still be alive");
        var drop=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None).FirstOrDefault();
        Assert.That(drop,Is.Not.Null,"Destroying an enemy gun must produce a pickup before the core dies");
        string id=drop.ItemId;var player=run.Player.GetComponent<Rigidbody>();player.isKinematic=true;player.position=drop.transform.position;
        Physics.SyncTransforms();float deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&!run.VehicleModel.Owns(id))yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(run.VehicleModel.Owns(id),Is.True);
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        Assert.That(run.Salvage.Items.Any(p=>p.Id==id),Is.False,"Wreck capture must not recreate the already collected gun");
        Assert.That(Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None).Any(p=>p.ItemId==id),Is.False,"Wreck capture must not spawn a second pickup for an early drop");
        Assert.That(run.VehicleModel.Inventory.Count(p=>p.Id==id),Is.EqualTo(1));
        Assert.That(run.Salvage.Items.Count,Is.GreaterThan(0),"Remaining enemy parts must still be recorded");
    }
    [UnityTest] public IEnumerator DestroyedEnemyArmorDropsCollectibleFivePiecePackBeforeVictory()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        var baseRoot=enemy.transform.Find("Base");Assert.That(baseRoot,Is.Not.Null);
        var armor=baseRoot.GetComponentsInChildren<P12PipelineSafeDamageable>()
            .Where(p=>p.name=="P12_EnemyBlock"&&p!=enemy.Core).OrderByDescending(p=>(p.transform.position-enemy.Core.transform.position).sqrMagnitude).First();
        armor.ApplyDamageResolvedTyped(armor.CurrentHp/.75f+.1f,armor.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        Assert.That(enemy.IsDefeated,Is.False,"The armor drop must be available before core defeat.");
        var item=run.Salvage.Items.FirstOrDefault(p=>p.Kind==R3PartKind.Armor);Assert.That(item,Is.Not.Null,"Destroyed enemy armor must enter the canonical loot ledger.");
        var drop=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None).FirstOrDefault(p=>p.ItemId==item.Id);Assert.That(drop,Is.Not.Null);
        var player=run.Player.GetComponent<Rigidbody>();player.isKinematic=true;player.position=drop.transform.position;Physics.SyncTransforms();
        float deadline=Time.realtimeSinceStartup+5;while(Time.realtimeSinceStartup<deadline&&!run.VehicleModel.Owns(item.Id))yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(run.VehicleModel.Owns(item.Id),Is.True);
        Assert.That(run.VehicleModel.Inventory.Count(p=>p.Kind==R3PartKind.Armor&&p.Source==item.Source),Is.EqualTo(5));
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        Assert.That(run.Salvage.Items.Any(p=>p.Id==item.Id),Is.False,"Victory cannot duplicate collected armor.");
    }
    [UnityTest] public IEnumerator SurvivingEnemyArmorCanDropWhenCoreIsDefeated()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        enemy.Core.ApplyDamageResolvedTyped(enemy.Core.CurrentHp/.75f+.1f,enemy.Core.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        Assert.That(enemy.IsDefeated,Is.True);
        var armor=run.Salvage.Items.Where(p=>p.Kind==R3PartKind.Armor&&p.Health>0).ToArray();
        Assert.That(armor,Is.Not.Empty,"Surviving armor should be eligible for a physical drop when the core dies.");
        var drops=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None);
        Assert.That(armor.All(p=>drops.Count(d=>d.ItemId==p.Id)==1),Is.True,"Each dropped block has a single world object.");
        Assert.That(run.Salvage.CreateBasketOffers(1,0).SelectMany(b=>b.Items).Any(p=>p.Kind==R3PartKind.Armor),Is.False);
        yield return null;
    }
    [UnityTest] public IEnumerator UncollectedEarlyArmorDropIsNotDuplicatedAtCoreDefeat()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        var armor=enemy.transform.Find("Base").GetComponentsInChildren<P12PipelineSafeDamageable>()
            .Where(p=>p.name=="P12_EnemyBlock"&&p!=enemy.Core).OrderByDescending(p=>(p.transform.position-enemy.Core.transform.position).sqrMagnitude).First();
        armor.ApplyDamageResolvedTyped(armor.CurrentHp/.75f+.1f,armor.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        var item=run.Salvage.Items.First(p=>p.Kind==R3PartKind.Armor);string id=item.Id;
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        Assert.That(Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None).Count(p=>p.ItemId==id),Is.EqualTo(1));
        Assert.That(run.Salvage.Items.Count(p=>p.Id==id),Is.EqualTo(1));
        yield return null;
    }
    [UnityTest] public IEnumerator UncollectedEarlyDropIsNotSpawnedAgainWhenEnemyDies()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        var gun=enemy.WeaponBlock;gun.ApplyDamageResolvedTyped(gun.CurrentHp/.9f+1,gun.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        Assert.That(enemy.IsDefeated,Is.False);
        var drop=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None).First();string id=drop.ItemId;
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        Assert.That(Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None).Count(p=>p.ItemId==id),Is.EqualTo(1));
        Assert.That(run.Salvage.Items.Count(p=>p.Id==id),Is.EqualTo(1));
        yield return null;
    }
    [UnityTest] public IEnumerator DestroyedEnemyWheelDropsItsTwoPartPackageBeforeCoreDies()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        var wheel=enemy.GetComponentsInChildren<P12PipelineSafeDamageable>().First(p=>p.gameObject.name=="Koło przeciwnika");
        wheel.ApplyDamageResolvedTyped(wheel.CurrentHp/.75f+1,wheel.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        Assert.That(wheel.IsDead,Is.True,"The test impact must actually destroy the wheel");
        Assert.That(enemy.IsDefeated,Is.False);
        var drop=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None)
            .First(p=>run.Salvage.Items.Any(item=>item.Id==p.ItemId&&item.Kind==R3PartKind.Wheel));
        string id=drop.ItemId;var player=run.Player.GetComponent<Rigidbody>();player.isKinematic=true;player.position=drop.transform.position;
        Physics.SyncTransforms();float deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&!run.VehicleModel.Owns(id))yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(run.VehicleModel.Owns(id),Is.True);
        Assert.That(run.VehicleModel.Inventory.Count(p=>p.Kind==R3PartKind.Wheel),Is.EqualTo(2));
    }
    [UnityTest] public IEnumerator DestroyedEnemyBatteryDropsBeforeCoreDefeat()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.Last();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        var battery=enemy.GetComponentInChildren<VehicleEnergyBattery>();Assert.That(battery,Is.Not.Null,"The scenario must contain a physically mounted enemy battery");
        var hp=battery.GetComponent<P12PipelineSafeDamageable>();hp.ApplyDamageResolvedTyped(hp.CurrentHp/.75f+1,hp.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        Assert.That(hp.IsDead,Is.True);Assert.That(enemy.IsDefeated,Is.False);
        var drop=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None)
            .FirstOrDefault(p=>run.Salvage.Items.Any(item=>item.Id==p.ItemId&&item.Kind==R3PartKind.Battery));
        Assert.That(drop,Is.Not.Null,"The destroyed battery must be recoverable before the core dies");
        string id=drop.ItemId;var player=run.Player.GetComponent<Rigidbody>();player.isKinematic=true;player.position=drop.transform.position;Physics.SyncTransforms();
        float deadline=Time.realtimeSinceStartup+5;while(Time.realtimeSinceStartup<deadline&&!run.VehicleModel.Owns(id))yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(run.VehicleModel.Owns(id),Is.True);Assert.That(run.VehicleModel.Inventory.Single(p=>p.Id==id).Kind,Is.EqualTo(R3PartKind.Battery));
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        Assert.That(run.Salvage.Items.Any(p=>p.Id==id),Is.False);
        Assert.That(run.VehicleModel.Inventory.Count(p=>p.Id==id),Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator DestroyedEnemyShieldDropsBeforeCoreDefeat()=>VerifyEnemyModuleDrop(R3PartKind.DirectionalShield);
    [UnityTest] public IEnumerator DestroyedEnemyPointDefenseDropsBeforeCoreDefeat()=>VerifyEnemyModuleDrop(R3PartKind.PointDefense);
    IEnumerator VerifyEnemyModuleDrop(R3PartKind kind)
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        Assert.That(kind==R3PartKind.PointDefense?enemy.EquipPointDefense():enemy.EquipDirectionalShield(),Is.True);
        var hp=kind==R3PartKind.PointDefense?enemy.GetComponentInChildren<RRLPointDefenseBinding>().Device.Damageable:
            enemy.GetComponentInChildren<RRLShieldBinding>().Device.Damageable;
        hp.RestoreHealthRatio(.01f);hp.ApplyDamageResolvedTyped(hp.CurrentHp/.9f+1,hp.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Piercing);
        Assert.That(hp.IsDead,Is.True);Assert.That(enemy.IsDefeated,Is.False);
        var drop=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None)
            .FirstOrDefault(p=>run.Salvage.Items.Any(item=>item.Id==p.ItemId&&item.Kind==kind));
        Assert.That(drop,Is.Not.Null,"The destroyed module must drop before the core dies");
        string id=drop.ItemId;var player=run.Player.GetComponent<Rigidbody>();player.isKinematic=true;player.position=drop.transform.position;Physics.SyncTransforms();
        float deadline=Time.realtimeSinceStartup+5;while(Time.realtimeSinceStartup<deadline&&!run.VehicleModel.Owns(id))yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(run.VehicleModel.Owns(id),Is.True);enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        Assert.That(run.Salvage.Items.Any(p=>p.Id==id),Is.False);Assert.That(run.VehicleModel.Inventory.Count(p=>p.Id==id),Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator ScopedAutomaticFireRateControlsARealShot()
    {
        var bonus=BacklogBonusTierTests.OperationBonus("actual-auto-rate",.2f,"Increased",R1UpgradeEffect.Reload);bonus.Scope=R1BonusScope.AutomaticWeapon;
        var gun=Build(R4WeaponKind.Automatic,out _,false,bonus);var hp=Target(gun,6);
        var assembly=gun.GetComponentInParent<R3VehicleAssembly>();float firedAt=Time.time;
        int slot=System.Array.FindIndex(assembly.Model.GetWeaponSlots(),weapon=>weapon==R4WeaponKind.Automatic);
        Assert.That(slot,Is.GreaterThanOrEqualTo(0));assembly.FireSlot(slot);
        Assert.That(gun.NextFireTime-firedAt,Is.EqualTo(R4WeaponDefinition.Get(R4WeaponKind.Automatic).Interval*.8f).Within(.002));
        float deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=500)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.LessThan(500),"The automatic must fire a real projectile with the scoped cooldown.");
        Object.Destroy(assembly.gameObject);Object.Destroy(hp.gameObject);Object.Destroy(bonus);
    }
    [UnityTest] public IEnumerator ScopedFireRateReachesEachPackagedPhysicalGun()
    {
        var cases=new[]{(R4WeaponKind.Shotgun,R1BonusScope.ShotgunWeapon,.85f),
            (R4WeaponKind.Railgun,R1BonusScope.RailgunWeapon,2.4f),
            (R4WeaponKind.RocketLauncher,R1BonusScope.RocketWeapon,1.7f)};
        foreach(var (kind,scope,interval) in cases)
        {
            var bonus=BacklogBonusTierTests.OperationBonus("fire-rate-"+kind,.2f,"Increased",R1UpgradeEffect.Reload);bonus.Scope=scope;
            var gun=Build(kind,out _,false,bonus);var hp=Target(gun,6);float firedAt=Time.time;
            bool fired=kind==R4WeaponKind.Shotgun?gun.GetComponent<RRLShotgunBinding>().Fire():
                kind==R4WeaponKind.Railgun?gun.GetComponent<RRLRailgunBinding>().Fire():gun.GetComponent<RRLRocketBinding>().Fire();
            Assert.That(fired,Is.True,kind.ToString());
            var state=gun.GetComponent<RRLDeviceBinding>().State;
            Assert.That(state.DeviceReadyAt-firedAt,Is.EqualTo(interval*.8f).Within(.002),"The physical weapon must use its scoped interval.");
            float deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=500)yield return new WaitForFixedUpdate();
            Assert.That(hp.CurrentHp,Is.LessThan(500),"The scoped shot must actually hit.");
            Object.Destroy(gun.GetComponentInParent<R3VehicleAssembly>().gameObject);Object.Destroy(hp.gameObject);Object.Destroy(bonus);yield return null;
        }
    }
    [UnityTest] public IEnumerator TwoLargeAutomaticSalvosRemainIndependentAndShareBonusCapacity()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(2000);
        model.Parts.First(p=>p.Kind==R3PartKind.Gun).Weapon=R4WeaponKind.Automatic;
        var gunCell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,R3PartKind.Gun,false,R4WeaponKind.Automatic)==null);
        Assert.That(model.Buy(gunCell,R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
        var bonusCell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,R3PartKind.DoubleShot)==null);
        Assert.That(model.Buy(bonusCell,R3PartKind.DoubleShot),Is.True);model.Economy.DoubleShotChancePerUnit=100;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;var assembly=vehicle.GetComponent<R3VehicleAssembly>();
        var guns=assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun&&p.Weapon==R4WeaponKind.Automatic).ToArray();Assert.That(guns.Length,Is.EqualTo(2));
        for(int i=0;i<guns.Length;i++)
        {var muzzle=new GameObject("Separate grouped muzzle").transform;muzzle.SetParent(guns[i].transform,false);muzzle.position=new Vector3(1000+i*8,1000,1000);muzzle.rotation=Quaternion.identity;guns[i].GunPivot=muzzle;guns[i].Muzzle=muzzle;}
        var targets=guns.Select(g=>{var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=g.Muzzle.position+Vector3.forward*5;
            target.transform.localScale=new Vector3(2,2,.5f);var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(5000,false);return hp;}).ToArray();
        try
        {
            Physics.SyncTransforms();Assert.That(guns.All(assembly.ClearProjectilePath),Is.True);
            int slot=System.Array.FindIndex(model.GetWeaponSlots(),w=>w==R4WeaponKind.Automatic);Assert.That(slot,Is.GreaterThanOrEqualTo(0));assembly.FireSlot(slot);
            var origins=Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>guns.Any(g=>g.Id==p.PartId)).ToArray();
            Assert.That(guns.All(g=>g.NextFireTime>Time.time),Is.True);
            var groups=guns.Select(g=>origins.Where(p=>p.PartId==g.Id).ToArray()).ToArray();
            Assert.That(groups.Select(g=>g.Length),Is.EqualTo(new[]{48,48}),"Both primary shots fire and the available bonuses are shared.");
            Assert.That(groups.All(g=>g[0].gameObject.layer>=24&&g.All(p=>p.gameObject.layer==g[0].gameObject.layer)),Is.True);
            Assert.That(groups[0][0].gameObject.layer,Is.Not.EqualTo(groups[1][0].gameObject.layer),"Separate guns must retain separate collision groups.");
            Assert.That(groups.All(g=>ExplicitSiblingPairs(g)==496),Is.True);
            float expected=5000-48*R4WeaponDefinition.Get(R4WeaponKind.Automatic).Damage;float deadline=Time.realtimeSinceStartup+5;
            while(Time.realtimeSinceStartup<deadline&&targets.Any(target=>target.CurrentHp>expected+.2f))yield return new WaitForFixedUpdate();
            foreach(var target in targets)Assert.That(target.CurrentHp,Is.EqualTo(expected).Within(.5),"Each physical shot must reach its own target.");
        }
        finally{foreach(var target in targets)if(target!=null)Object.Destroy(target.gameObject);if(vehicle!=null)Object.Destroy(vehicle.gameObject);}
        yield return null;
    }
    [UnityTest] public IEnumerator LargeAutomaticSalvoKeepsIndependentHitsWithGroupedCollisionFiltering()
    {
        var gun=Build(R4WeaponKind.Automatic,out _,true);var assembly=gun.GetComponentInParent<R3VehicleAssembly>();
        assembly.Model.Economy.DoubleShotChancePerUnit=50;var core=assembly.GetComponent<R1VehicleState>().Core;float coreHp=core.CurrentHp;
        var hp=Target(gun,6);hp.Configure(2000,false);int slot=System.Array.FindIndex(assembly.Model.GetWeaponSlots(),weapon=>weapon==R4WeaponKind.Automatic);
        Assert.That(slot,Is.GreaterThanOrEqualTo(0));assembly.FireSlot(slot);
        var shots=Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(origin=>origin.PartId==gun.Id)
            .OrderBy(origin=>origin.GetComponent<P17PhysicalShell>().LaunchSequence).ToArray();
        Assert.That(shots.Length,Is.EqualTo(51),"The primary and 50 bonus shots must all exist separately.");
        int layer=shots[0].gameObject.layer;Assert.That(layer,Is.InRange(24,31));
        Assert.That(shots.All(origin=>origin.gameObject.layer==layer&&(origin.GetComponent<Collider>().excludeLayers.value&(1<<layer))!=0),Is.True);
        Assert.That(ExplicitSiblingPairs(shots),Is.EqualTo(496),"Only the first 32 shots need explicit pair filters.");
        Assert.That(shots.All(origin=>Physics.GetIgnoreCollision(origin.GetComponent<Collider>(),core.GetComponent<Collider>())),Is.True);
        Assert.That(shots[0].GetComponent<RRLPointDefenseThreat>().TryIntercept(1),Is.True);
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>800.1f&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(800).Within(.5),"The 50 surviving shots must each deliver physical damage.");
        Assert.That(core.CurrentHp,Is.EqualTo(coreHp));
        Object.Destroy(assembly.gameObject);Object.Destroy(hp.gameObject);
    }
    [UnityTest] public IEnumerator LargeAutomaticSalvoFallsBackForConflictingSceneFilter()
    {
        var blocker=new GameObject("Automatic layer compatibility fixture");blocker.transform.position=new Vector3(3000,3000,3000);
        blocker.AddComponent<BoxCollider>().excludeLayers=1;blocker.SetActive(false);
        var gun=Build(R4WeaponKind.Automatic,out _,true);var assembly=gun.GetComponentInParent<R3VehicleAssembly>();
        assembly.Model.Economy.DoubleShotChancePerUnit=50;var hp=Target(gun,6);hp.Configure(2000,false);
        try
        {
            int slot=System.Array.FindIndex(assembly.Model.GetWeaponSlots(),weapon=>weapon==R4WeaponKind.Automatic);assembly.FireSlot(slot);
            var shots=Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(origin=>origin.PartId==gun.Id).ToArray();
            Assert.That(shots.Length,Is.EqualTo(51));Assert.That(shots.All(origin=>origin.gameObject.layer==0),Is.True);
            Assert.That(ExplicitSiblingPairs(shots),Is.EqualTo(1275),"Fallback must preserve all original pair filters.");
            float deadline=Time.realtimeSinceStartup+5;
            while(hp.CurrentHp>776.1f&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
            Assert.That(hp.CurrentHp,Is.EqualTo(776).Within(.5));
        }
        finally{Object.Destroy(blocker);Object.Destroy(assembly.gameObject);Object.Destroy(hp.gameObject);}
    }
    [UnityTest] public IEnumerator GroupedAutomaticShotsDamageHostileDomeIndividually()
    {
        var gun=Build(R4WeaponKind.Automatic,out _,true);var assembly=gun.GetComponentInParent<R3VehicleAssembly>();
        assembly.Model.Economy.DoubleShotChancePerUnit=50;var target=Target(gun,6);
        var dome=new GameObject("Grouped automatic shield");dome.transform.position=gun.Muzzle.position+gun.Muzzle.forward*6;
        dome.AddComponent<SphereCollider>().isTrigger=true;var shield=dome.AddComponent<P12PipelineSafeDamageable>();shield.Configure(2000,false);
        dome.AddComponent<R3ShieldDome>().Configure(2,2);Physics.SyncTransforms();
        try
        {
            int slot=System.Array.FindIndex(assembly.Model.GetWeaponSlots(),weapon=>weapon==R4WeaponKind.Automatic);assembly.FireSlot(slot);
            var shots=Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(origin=>origin.PartId==gun.Id).ToArray();
            Assert.That(shots.Length,Is.EqualTo(51));Assert.That(shots.All(origin=>origin.gameObject.layer>=24),Is.True);
            float deadline=Time.realtimeSinceStartup+5;
            while(shield.CurrentHp>776.1f&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
            Assert.That(shield.CurrentHp,Is.EqualTo(776).Within(.5));Assert.That(target.CurrentHp,Is.EqualTo(500).Within(.01));
        }
        finally{Object.Destroy(dome);Object.Destroy(target.gameObject);Object.Destroy(assembly.gameObject);}
    }
    [UnityTest] public IEnumerator LargeShotgunSalvoKeepsIndividualImpactsWithGroupedCollisionFiltering()
    {
        var gun=Build(R4WeaponKind.Shotgun,out _,true);var assembly=gun.GetComponentInParent<R3VehicleAssembly>();
        assembly.Model.Economy.DoubleShotChancePerUnit=5;
        var core=assembly.GetComponent<R1VehicleState>().Core;float coreHp=core.CurrentHp;
        var hp=Target(gun,6);hp.Configure(2000,false);
        Assert.That(gun.GetComponent<RRLShotgunBinding>().Fire(),Is.True);
        var shots=Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(origin=>origin.PartId==gun.Id)
            .OrderBy(origin=>origin.GetComponent<P17PhysicalShell>().LaunchSequence).ToArray();
        Assert.That(shots.Length,Is.EqualTo(48),"Eight primary pellets and five extra copies of each must exist separately.");
        int layer=shots[0].gameObject.layer;
        Assert.That(layer,Is.InRange(24,31),"A large salvo should use a reserved group layer.");
        Assert.That(shots.All(origin=>origin.gameObject.layer==layer&&(origin.GetComponent<Collider>().excludeLayers.value&(1<<layer))!=0),Is.True);
        Assert.That(shots.All(origin=>Physics.GetIgnoreCollision(origin.GetComponent<Collider>(),core.GetComponent<Collider>())),Is.True,
            "Changing the salvo layer must preserve every projectile's owner filter.");
        Assert.That(Physics.GetIgnoreCollision(shots[0].GetComponent<Collider>(),shots[shots.Length-1].GetComponent<Collider>()),Is.False,
            "The group must avoid a separate physics pair for every extra pellet.");
        Assert.That(ExplicitSiblingPairs(shots),Is.EqualTo(496),"Only the first 32 pellets should need explicit pair filters.");
        Assert.That(shots[0].GetComponent<RRLPointDefenseThreat>().TryIntercept(1),Is.True,"One real pellet must remain independently interceptable.");
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>1154.1f&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(1154).Within(.5),"Every surviving pellet must deal its own physical hit.");
        Assert.That(core.CurrentHp,Is.EqualTo(coreHp),"The firing vehicle must not damage itself.");
        Object.Destroy(assembly.gameObject);Object.Destroy(hp.gameObject);
    }
    [UnityTest] public IEnumerator LargeShotgunSalvoFallsBackWhenExistingFilterDistinguishesDefaultLayer()=>LargeShotgunSalvoFilterFallback(false);
    [UnityTest] public IEnumerator LargeShotgunSalvoFallsBackForInactiveConflictingCollider()=>LargeShotgunSalvoFilterFallback(true);
    IEnumerator LargeShotgunSalvoFilterFallback(bool inactive)
    {
        var blocker=new GameObject("Layer compatibility fixture");blocker.transform.position=new Vector3(3000,3000,3000);
        blocker.AddComponent<BoxCollider>().excludeLayers=1;
        if(inactive)blocker.SetActive(false);
        var gun=Build(R4WeaponKind.Shotgun,out _,true);var assembly=gun.GetComponentInParent<R3VehicleAssembly>();
        assembly.Model.Economy.DoubleShotChancePerUnit=5;var hp=Target(gun,6);hp.Configure(2000,false);Physics.SyncTransforms();
        try
        {
            Assert.That(gun.GetComponent<RRLShotgunBinding>().Fire(),Is.True);
            var shots=Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(origin=>origin.PartId==gun.Id)
                .OrderBy(origin=>origin.GetComponent<P17PhysicalShell>().LaunchSequence).ToArray();
            Assert.That(shots.Length,Is.EqualTo(48));Assert.That(shots.All(origin=>origin.gameObject.layer==0),Is.True);
            Assert.That(Physics.GetIgnoreCollision(shots[0].GetComponent<Collider>(),shots[shots.Length-1].GetComponent<Collider>()),Is.True,
                "When a scene filter differs, keep the existing pairwise behavior.");
            Assert.That(ExplicitSiblingPairs(shots),Is.EqualTo(1128),"Fallback keeps every original pair filter.");
            float deadline=Time.realtimeSinceStartup+5;
            while(hp.CurrentHp>1136.1f&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
            Assert.That(hp.CurrentHp,Is.EqualTo(1136).Within(.5),"All 48 pellets must still land independently.");
        }
        finally{Object.Destroy(blocker);Object.Destroy(assembly.gameObject);Object.Destroy(hp.gameObject);}
    }
    [UnityTest] public IEnumerator GroupedShotgunPelletsDamageHostileDomeIndividually()
    {
        var gun=Build(R4WeaponKind.Shotgun,out _,true);var assembly=gun.GetComponentInParent<R3VehicleAssembly>();
        assembly.Model.Economy.DoubleShotChancePerUnit=5;var target=Target(gun,6);
        var dome=new GameObject("Grouped salvo shield");dome.transform.position=gun.Muzzle.position+gun.Muzzle.forward*6;
        dome.AddComponent<SphereCollider>().isTrigger=true;var shield=dome.AddComponent<P12PipelineSafeDamageable>();shield.Configure(2000,false);
        dome.AddComponent<R3ShieldDome>().Configure(2,2);Physics.SyncTransforms();
        try
        {
            Assert.That(gun.GetComponent<RRLShotgunBinding>().Fire(),Is.True);
            var shots=Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(origin=>origin.PartId==gun.Id).ToArray();
            Assert.That(shots.Length,Is.EqualTo(48));Assert.That(shots.All(origin=>origin.gameObject.layer>=24),Is.True);
            float deadline=Time.realtimeSinceStartup+5;
            while(shield.CurrentHp>1136.1f&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
            Assert.That(shield.CurrentHp,Is.EqualTo(1136).Within(.5),"Every pellet must hit the hostile shield exactly once.");
            Assert.That(target.CurrentHp,Is.EqualTo(500).Within(.01),"The shield must protect the target behind it.");
        }
        finally{Object.Destroy(dome);Object.Destroy(target.gameObject);Object.Destroy(assembly.gameObject);}
    }
    [UnityTest] public IEnumerator VolleyLayerIsHeldUntilLastShotAndRestoredBeforeReuse()
    {
        var gun=Build(R4WeaponKind.Shotgun,out _,true);var assembly=gun.GetComponentInParent<R3VehicleAssembly>();
        assembly.Model.Economy.DoubleShotChancePerUnit=5;var hp=Target(gun,6);var binding=gun.GetComponent<RRLShotgunBinding>();
        yield return null; // Allow temporary wheel-visual colliders to be destroyed before testing the cache.
        R4ProjectileOrigin[] ActiveShots()=>Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(origin=>origin.PartId==gun.Id).ToArray();
        try
        {
            Assert.That(binding.Fire(),Is.True);var first=ActiveShots();Assert.That(first.Length,Is.EqualTo(48));
            var held=first[0];int firstLayer=held.gameObject.layer;held.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;
            foreach(var origin in first.Skip(1))Assert.That(origin.GetComponent<RRLPointDefenseThreat>().TryIntercept(1),Is.True);
            while(Time.time<binding.State.DeviceReadyAt+.02f)yield return null;
            Assert.That(held!=null&&held.gameObject.activeInHierarchy,Is.True);
            Assert.That(binding.Fire(),Is.True);
            var second=ActiveShots().Where(origin=>origin!=held).ToArray();Assert.That(second.Length,Is.EqualTo(48));
            int secondLayer=second[0].gameObject.layer;Assert.That(secondLayer,Is.Not.EqualTo(firstLayer),"A live shot retains its group's layer.");
            var heldObject=held.gameObject;Assert.That(held.GetComponent<RRLPointDefenseThreat>().TryIntercept(1),Is.True);
            Assert.That(heldObject.layer,Is.Zero);Assert.That(heldObject.GetComponent<Collider>().excludeLayers.value&(1<<firstLayer),Is.Zero);
            foreach(var origin in second)Assert.That(origin.GetComponent<RRLPointDefenseThreat>().TryIntercept(1),Is.True);
            while(Time.time<binding.State.DeviceReadyAt+.02f)yield return null;
            Assert.That(binding.Fire(),Is.True);var third=ActiveShots();Assert.That(third.Length,Is.EqualTo(48));
            Assert.That(third.All(origin=>origin.gameObject.layer==firstLayer),Is.True,"The released layer must become reusable.");
            foreach(var origin in third)origin.GetComponent<RRLPointDefenseThreat>().TryIntercept(1);
        }
        finally{Object.Destroy(assembly.gameObject);Object.Destroy(hp.gameObject);}
    }
    [UnityTest] public IEnumerator ExhaustedVolleyLayersFallBackWithoutLimitingProjectiles()
    {
        var rent=typeof(P17PhysicalShell).GetMethod("RentConventional",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        var group=typeof(P17PhysicalShell).GetMethod("TryGroupVolley",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        var ignore=typeof(P17PhysicalShell).GetMethod("IgnoreProjectilePair",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        Assert.That(rent,Is.Not.Null);Assert.That(group,Is.Not.Null);Assert.That(ignore,Is.Not.Null);
        var owner=new GameObject("Volley layer exhaustion owner");var all=new System.Collections.Generic.List<P17PhysicalShell>();
        System.Collections.Generic.List<GameObject> Pair(int index)
        {
            var pair=new System.Collections.Generic.List<GameObject>();
            for(int i=0;i<2;i++)
            {
                var shell=(P17PhysicalShell)rent.Invoke(null,null);all.Add(shell);
                shell.transform.position=new Vector3(2000+index*4+i,2000,2000);shell.gameObject.SetActive(true);
                shell.Configure(owner.transform,1,30,Vector3.forward,null);pair.Add(shell.gameObject);
            }
            return pair;
        }
        try
        {
            var groups=new System.Collections.Generic.List<System.Collections.Generic.List<GameObject>>();
            for(int i=0;i<8;i++)
            {
                var pair=Pair(i);
                if(i==0)
                {
                    var duplicate=new System.Collections.Generic.List<GameObject>{pair[0],pair[0]};
                    Assert.That((bool)group.Invoke(null,new object[]{duplicate}),Is.False,"One projectile cannot occupy a lease twice.");
                    Assert.That(pair[0].layer,Is.EqualTo(0));
                }
                Assert.That((bool)group.Invoke(null,new object[]{pair}),Is.True);
                Assert.That(pair[0].layer,Is.EqualTo(24+i));groups.Add(pair);
            }
            var overflow=Pair(8);Assert.That((bool)group.Invoke(null,new object[]{overflow}),Is.False);
            Assert.That(overflow.All(shot=>shot.layer==0&&shot.activeInHierarchy),Is.True,"A missing optimization layer must not suppress shots.");
            ignore.Invoke(null,new object[]{overflow[0].GetComponent<Collider>(),overflow[1].GetComponent<Collider>()});
            Assert.That(Physics.GetIgnoreCollision(overflow[0].GetComponent<Collider>(),overflow[1].GetComponent<Collider>()),Is.True);
            Assert.That(groups[0][0].GetComponent<RRLPointDefenseThreat>().TryIntercept(1),Is.True);
            Assert.That((bool)group.Invoke(null,new object[]{overflow}),Is.False,"A group remains leased while one shot survives.");
            Assert.That(groups[0][1].GetComponent<RRLPointDefenseThreat>().TryIntercept(1),Is.True);
            Assert.That((bool)group.Invoke(null,new object[]{overflow}),Is.True,"The released optimization layer must be reusable.");
            Assert.That(overflow.All(shot=>shot.layer==24),Is.True);
        }
        finally
        {
            foreach(var shell in all)if(shell!=null&&shell.IsActiveProjectile)shell.Intercept(1);
            Object.Destroy(owner);
        }
        yield return null;
    }
    [UnityTest] public IEnumerator VolleyLeaseSurvivesTemporaryDisableAndReleasesOnDestroy()
    {
        var rent=typeof(P17PhysicalShell).GetMethod("RentConventional",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        var group=typeof(P17PhysicalShell).GetMethod("TryGroupVolley",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        var owner=new GameObject("Volley disable and destroy owner");var all=new System.Collections.Generic.List<P17PhysicalShell>();
        System.Collections.Generic.List<GameObject> Pair(int index)
        {
            var pair=new System.Collections.Generic.List<GameObject>();
            for(int i=0;i<2;i++)
            {
                var shell=(P17PhysicalShell)rent.Invoke(null,null);all.Add(shell);
                shell.transform.position=new Vector3(2200+index*4+i,2000,2000);shell.gameObject.SetActive(true);
                shell.Configure(owner.transform,1,30,Vector3.forward,null);pair.Add(shell.gameObject);
            }
            return pair;
        }
        try
        {
            var first=Pair(0);Assert.That((bool)group.Invoke(null,new object[]{first}),Is.True);
            int firstLayer=first[0].layer;first[0].SetActive(false);
            var second=Pair(1);Assert.That((bool)group.Invoke(null,new object[]{second}),Is.True);
            Assert.That(second[0].layer,Is.Not.EqualTo(firstLayer),"Temporarily disabled shots still own their layer.");
            first[0].SetActive(true);Assert.That(first[0].layer,Is.EqualTo(firstLayer));
            Assert.That((first[0].GetComponent<Collider>().excludeLayers.value&(1<<firstLayer))!=0,Is.True);
            Object.Destroy(first[0]);Object.Destroy(first[1]);yield return null;
            var third=Pair(2);Assert.That((bool)group.Invoke(null,new object[]{third}),Is.True);
            Assert.That(third[0].layer,Is.EqualTo(firstLayer),"Destroying the last member must release the layer.");
        }
        finally
        {
            foreach(var shell in all)if(shell!=null&&shell.IsActiveProjectile)shell.Intercept(1);
            Object.Destroy(owner);
        }
        yield return null;
    }
    [UnityTest] public IEnumerator ShotgunTierTwoConsumesSixEnergyAndDamagesWithRealPellets()
    {
        var gun=Build(R4WeaponKind.Shotgun,out var energy);var hp=Target(gun,6);float before=energy.Current;
        var binding=gun.GetComponent<RRLShotgunBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-6).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(11));
        float end=Time.time+.35f;while(Time.time<end)yield return null;
        Assert.That(hp.CurrentHp,Is.EqualTo(356).Within(.2),"Eight real pellets each deal 18 damage.");
    }
    [UnityTest] public IEnumerator ProjectileWeaponScopesAffectOnlyMatchingPhysicalWeaponAndSurviveSave()
    {
        var cases=new[]{(R4WeaponKind.Heavy,"HeavyWeapon","damage_heavy_weapon"),
            (R4WeaponKind.Shotgun,"ShotgunWeapon","damage_shotgun_weapon"),
            (R4WeaponKind.Railgun,"RailgunWeapon","damage_railgun_weapon"),
            (R4WeaponKind.RocketLauncher,"RocketWeapon","damage_rocket_weapon")};
        foreach(var entry in cases)
        {
            Assert.That(System.Enum.TryParse(entry.Item2,out R1BonusScope scope),Is.True,"Missing scope "+entry.Item2);
            var asset=Resources.Load<R1RunDefinition>("R1/Run").Upgrades.Single(p=>p!=null&&p.Id==entry.Item3);
            Assert.That(asset.Scope,Is.EqualTo(scope));Assert.That(asset.Amount,Is.EqualTo(.3f));
            var bonus=ScriptableObject.CreateInstance<R1UpgradeDefinition>();bonus.Id="test-"+entry.Item3;
            bonus.Effect=R1UpgradeEffect.Damage;bonus.Scope=scope;bonus.Amount=.3f;
            var plain=Build(entry.Item1,out _);var boosted=Build(entry.Item1,out _,bonus:bonus);
            var plainRoot=plain.GetComponentInParent<R3VehicleAssembly>();var boostedRoot=boosted.GetComponentInParent<R3VehicleAssembly>();
            boostedRoot.transform.position+=Vector3.right*20;
            var plainTarget=Target(plain,6);var boostedTarget=Target(boosted,6);Physics.SyncTransforms();
            var checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(boostedRoot.Model.MakeCheckpoint(0)));
            Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,boostedRoot.Model.Economy,0,out var restored,out var error),Is.True,error);
            Assert.That(restored.Parts.Any(p=>p.UpgradeScope==scope),Is.True);
            var effects=boostedRoot.GetComponent<R3MountedBonusEffects>();
            Assert.That(effects.AutomaticDamageMultiplier,Is.EqualTo(1).Within(.001),"Other weapon categories must not gain this bonus.");
            bool Fire(R3MountedPart gun)=>entry.Item1==R4WeaponKind.Shotgun?gun.GetComponent<RRLShotgunBinding>().Fire():
                entry.Item1==R4WeaponKind.Railgun?gun.GetComponent<RRLRailgunBinding>().Fire():
                entry.Item1==R4WeaponKind.RocketLauncher?gun.GetComponent<RRLRocketBinding>().Fire():
                gun.GetComponentInParent<P12PipelineSafeTankController>().FirePrimaryFrom(gun.Muzzle,entry.Item1,gun.Id)!=null;
            Assert.That(Fire(plain),Is.True);Assert.That(Fire(boosted),Is.True);
            float deadline=Time.realtimeSinceStartup+5;
            while((plainTarget.CurrentHp>=500||boostedTarget.CurrentHp>=500)&&Time.realtimeSinceStartup<deadline)yield return null;
            float baseline=500-plainTarget.CurrentHp,enhanced=500-boostedTarget.CurrentHp;
            Assert.That(baseline,Is.GreaterThan(0),entry.Item2);
            Assert.That(enhanced,Is.EqualTo(baseline*1.3f).Within(.3f),entry.Item2);
            var module=boostedRoot.Parts.Single(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeScope==scope);
            module.gameObject.SetActive(false);yield return null;
            float nextShot=Time.time+2.6f;while(Time.time<nextShot)yield return null;
            boostedTarget.RestoreHealthRatio(1);Assert.That(Fire(boosted),Is.True,"Second shot: "+entry.Item2);
            deadline=Time.realtimeSinceStartup+5;while(boostedTarget.CurrentHp>=500&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(500-boostedTarget.CurrentHp,Is.EqualTo(baseline).Within(.3f),"Disabled "+entry.Item2);
            Object.Destroy(plainTarget.gameObject);Object.Destroy(boostedTarget.gameObject);Object.Destroy(plainRoot.gameObject);Object.Destroy(boostedRoot.gameObject);Object.Destroy(bonus);
            yield return null;
        }
    }
    [UnityTest] public IEnumerator DoubleShotDuplicatesShotgunPelletsWithoutExtraAmmoOrEnergy()
    {
        var gun=Build(R4WeaponKind.Shotgun,out var energy,true);var hp=Target(gun,6);float before=energy.Current;
        var binding=gun.GetComponent<RRLShotgunBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-6).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(11));
        float end=Time.time+.4f;while(Time.time<end)yield return null;
        Assert.That(hp.CurrentHp,Is.EqualTo(212).Within(.2),"Sixteen pellets deal 18 damage each, without recursive procs.");
    }
    [UnityTest] public IEnumerator RailgunTierTwoConsumesEnergyAndPenetratesRealTarget()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var hp=Target(gun,6);float before=energy.Current;
        var binding=gun.GetComponent<RRLRailgunBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-10.8f).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(11));
        float end=Time.time+.35f;while(Time.time<end)yield return null;
        Assert.That(500-hp.CurrentHp,Is.InRange(100f,130f));
    }
    [UnityTest] public IEnumerator RailgunCanReuseShotAndLaterBecomeOrdinaryProjectile()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var hp=Target(gun,6);
        yield return null; // Finish deferred destruction of the vehicle's helper colliders.
        var binding=gun.GetComponent<RRLRailgunBinding>();Assert.That(binding.Fire(),Is.True);
        var first=Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Single(p=>p.PartId==gun.Id);
        int id=first.gameObject.GetInstanceID();float deadline=Time.realtimeSinceStartup+6;
        while((hp.CurrentHp>=500||Time.time<binding.State.DeviceReadyAt+.02f)&&Time.realtimeSinceStartup<deadline)yield return null;
        float afterFirst=hp.CurrentHp;Assert.That(afterFirst,Is.LessThan(500));
        Assert.That(binding.Fire(),Is.True);
        var second=Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Single(p=>p.PartId==gun.Id);
        Assert.That(second.gameObject.GetInstanceID(),Is.EqualTo(id),"Retired rail shots should be reusable.");
        deadline=Time.realtimeSinceStartup+4;while(hp.CurrentHp>=afterFirst&&Time.realtimeSinceStartup<deadline)yield return null;
        float afterSecond=hp.CurrentHp;
        Assert.That(afterSecond,Is.LessThan(afterFirst),"A reused rail must clear its list of previously penetrated targets.");
        deadline=Time.realtimeSinceStartup+5;while(second!=null&&second.gameObject.activeInHierarchy&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(second==null||!second.gameObject.activeInHierarchy,Is.True,"The rail must finish its flight before reuse.");
        yield return null;
        var controller=gun.GetComponentInParent<P12PipelineSafeTankController>();
        var basic=controller.FirePrimaryFrom(gun.Muzzle,R4WeaponKind.Basic,"rail-to-basic fixture");
        Assert.That(basic,Is.Not.Null);Assert.That(basic.gameObject.GetInstanceID(),Is.EqualTo(id));
        Assert.That(basic.isTrigger,Is.False);Assert.That(basic.GetComponent<Rigidbody>().isKinematic,Is.False);
        deadline=Time.realtimeSinceStartup+4;while(hp.CurrentHp>=afterSecond&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(hp.CurrentHp,Is.LessThan(afterSecond),"The recycled conventional shot must still hit.");
        float afterBasic=hp.CurrentHp;
        deadline=Time.realtimeSinceStartup+5;
        while(((basic!=null&&basic.gameObject.activeInHierarchy)||Time.time<binding.State.DeviceReadyAt+.02f)&&Time.realtimeSinceStartup<deadline)yield return null;
        yield return null;
        Assert.That(binding.Fire(),Is.True);
        var third=Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Single(p=>p.PartId==gun.Id);
        Assert.That(third.gameObject.GetInstanceID(),Is.EqualTo(id));
        Assert.That(third.GetComponent<Collider>().isTrigger,Is.True);Assert.That(third.GetComponent<Rigidbody>().isKinematic,Is.True);
        deadline=Time.realtimeSinceStartup+4;while(hp.CurrentHp>=afterBasic&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(hp.CurrentHp,Is.LessThan(afterBasic),"The same object must become an effective rail shot again.");
    }
    [UnityTest] public IEnumerator RailgunHitsHostileDomeBeforeProtectedTarget()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var target=Target(gun,8);
        var dome=new GameObject("shield rail fixture");dome.transform.position=gun.Muzzle.position+gun.Muzzle.forward*5+gun.Muzzle.up;
        dome.AddComponent<Rigidbody>().isKinematic=true;dome.AddComponent<SphereCollider>().isTrigger=true;
        var hp=dome.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);dome.AddComponent<R3ShieldDome>().Configure(2,2);
        Physics.SyncTransforms();Assert.That(gun.GetComponent<RRLRailgunBinding>().Fire(),Is.True);
        float end=Time.time+.35f;while(Time.time<end)yield return null;
        Assert.That(hp.CurrentHp,Is.LessThan(500),"The physical rail shot must damage the boundary");
        Assert.That(target.CurrentHp,Is.EqualTo(500),"The protected target must remain undamaged");
        Object.Destroy(dome);
    }
    [UnityTest] public IEnumerator RailgunHitsWallBeforeDomeWithoutDamagingIt()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var target=Target(gun,8);
        var dome=new GameObject("shield behind wall");dome.transform.position=gun.Muzzle.position+gun.Muzzle.forward*6+gun.Muzzle.up;
        dome.AddComponent<Rigidbody>().isKinematic=true;dome.AddComponent<SphereCollider>().isTrigger=true;
        var hp=dome.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);dome.AddComponent<R3ShieldDome>().Configure(2,1.5f);
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=gun.Muzzle.position+gun.Muzzle.forward*2;
        wall.transform.localScale=new Vector3(3,3,.2f);var receiver=wall.AddComponent<RailgunWallFixture>();Physics.SyncTransforms();
        Assert.That(gun.GetComponent<RRLRailgunBinding>().Fire(),Is.True);
        float end=Time.time+.35f;while(Time.time<end)yield return null;
        Assert.That(receiver.HitCount,Is.GreaterThan(0));Assert.That(hp.CurrentHp,Is.EqualTo(500));Assert.That(target.CurrentHp,Is.EqualTo(500));
        Object.Destroy(dome);Object.Destroy(wall);
    }
    [UnityTest] public IEnumerator DoubleShotDuplicatesRailgunWithoutExtraAmmoOrEnergy()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy,true);var hp=Target(gun,6);float before=energy.Current;
        var binding=gun.GetComponent<RRLRailgunBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-10.8f).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(11));
        float end=Time.time+.4f;while(Time.time<end)yield return null;
        Assert.That(500-hp.CurrentHp,Is.InRange(200f,260f),"Two real rail projectiles must hit without recursive procs.");
    }
    [UnityTest] public IEnumerator RailgunDoesNotVanishAmongManyIgnoredFriendlyColliders()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var hp=Target(gun,6);
        var fixture=new GameObject("Ignored friendly colliders");
        fixture.transform.position=gun.Muzzle.position+gun.Muzzle.forward*2.5f;
        fixture.AddComponent<RailgunFriendlyFixture>();
        for(int i=0;i<150;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.05f;
        Physics.SyncTransforms();
        int crowded=Physics.SphereCastAll(gun.Muzzle.position,.075f,gun.Muzzle.forward,3f,~0,QueryTriggerInteraction.Ignore)
            .Count(h=>h.collider.gameObject==fixture);
        Assert.That(crowded,Is.EqualTo(150),"The real cast must overflow the old 128-hit buffer.");
        try
        {
            Assert.That(gun.GetComponent<RRLRailgunBinding>().Fire(),Is.True);
            float end=Time.time+.5f;while(Time.time<end)yield return null;
            Assert.That(hp.CurrentHp,Is.LessThan(500),"Ignored friendly colliders must not destroy the projectile before it reaches a valid target.");
        }
        finally{Object.Destroy(fixture);}
    }
    [UnityTest] public IEnumerator CrowdedRailgunStillStopsAtWorldWallBeforeTarget()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var hp=Target(gun,6);
        var fixture=new GameObject("Ignored friendly colliders");
        fixture.transform.position=gun.Muzzle.position+gun.Muzzle.forward*2.5f;
        fixture.AddComponent<RailgunFriendlyFixture>();
        for(int i=0;i<150;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.05f;
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name="Solid wall";wall.transform.position=gun.Muzzle.position+gun.Muzzle.forward*4f;
        var receiver=wall.AddComponent<RailgunWallFixture>();
        wall.transform.localScale=new Vector3(4,4,.25f);Physics.SyncTransforms();
        try
        {
            Assert.That(gun.GetComponent<RRLRailgunBinding>().Fire(),Is.True);
            float end=Time.time+.5f;while(Time.time<end)yield return null;
            Assert.That(receiver.HitCount,Is.EqualTo(1),"The projectile must actually reach the wall.");
            Assert.That(hp.CurrentHp,Is.EqualTo(500),"Complete queries must still process the first solid obstacle before the target.");
        }
        finally{Object.Destroy(fixture);Object.Destroy(wall);}
    }
    [UnityTest] public IEnumerator RailgunFallsBackToCompleteQueryBeyondMaximumBuffer()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var hp=Target(gun,6);
        var fixture=new GameObject("Extreme friendly collider crowd");
        fixture.transform.position=gun.Muzzle.position+gun.Muzzle.forward*2.5f;
        fixture.AddComponent<RailgunFriendlyFixture>();
        for(int i=0;i<4100;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.05f;
        Physics.SyncTransforms();
        Assert.That(Physics.SphereCastAll(gun.Muzzle.position,.075f,gun.Muzzle.forward,3f,~0,QueryTriggerInteraction.Ignore)
            .Count(h=>h.collider.gameObject==fixture),Is.EqualTo(4100));
        try
        {
            Assert.That(gun.GetComponent<RRLRailgunBinding>().Fire(),Is.True);
            float end=Time.time+.5f;while(Time.time<end)yield return null;
            Assert.That(hp.CurrentHp,Is.LessThan(500),"The bounded buffer must fall back to a complete query without losing the shot.");
        }
        finally{Object.Destroy(fixture);}
    }
    [UnityTest] public IEnumerator RocketTierTwoConsumesEnergyAndExplodesAgainstRealTarget()
    {
        var gun=Build(R4WeaponKind.RocketLauncher,out var energy);var hp=Target(gun,6,2);float before=energy.Current;
        var binding=gun.GetComponent<RRLRocketBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-9f).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(9));
        float end=Time.time+.8f;while(Time.time<end)yield return null;
        Assert.That(500-hp.CurrentHp,Is.InRange(100f,120f));
    }
    [UnityTest] public IEnumerator BlastScopedBonusReachesActualRocketExplosion()
    {
        Assert.That(System.Enum.GetNames(typeof(R1BonusScope)),Does.Contain("BlastDamage"));
        var bonus=ScriptableObject.CreateInstance<R1UpgradeDefinition>();bonus.Id="rocket-blast";bonus.Effect=R1UpgradeEffect.Damage;
        bonus.Scope=(R1BonusScope)System.Enum.Parse(typeof(R1BonusScope),"BlastDamage");bonus.Amount=.4f;
        var ordinary=Build(R4WeaponKind.RocketLauncher,out _);var stronger=Build(R4WeaponKind.RocketLauncher,out _,bonus:bonus);
        var strongRoot=stronger.GetComponentInParent<R3VehicleAssembly>();strongRoot.transform.position+=Vector3.right*20;Physics.SyncTransforms();
        var target1=Target(ordinary,6,2);var target2=Target(stronger,6,2);
        Assert.That(ordinary.GetComponent<RRLRocketBinding>().Fire(),Is.True);Assert.That(stronger.GetComponent<RRLRocketBinding>().Fire(),Is.True);
        float deadline=Time.time+1;while(Time.time<deadline)yield return null;
        float baseline=500-target1.CurrentHp,boosted=500-target2.CurrentHp;
        Assert.That(baseline,Is.GreaterThan(0));Assert.That(boosted,Is.EqualTo(baseline*1.4f).Within(.2f),"Only the explosion receives the blast bonus.");
        Object.Destroy(bonus);Object.Destroy(target1.gameObject);Object.Destroy(target2.gameObject);
    }
    [UnityTest] public IEnumerator PiercingScopedBonusReachesActualRailgunWithoutBuffingBasicCannon()
    {
        Assert.That(System.Enum.GetNames(typeof(R1BonusScope)),Does.Contain("PiercingDamage"));
        var bonus=ScriptableObject.CreateInstance<R1UpgradeDefinition>();bonus.Id="rail-piercing";bonus.Effect=R1UpgradeEffect.Damage;
        bonus.Scope=(R1BonusScope)System.Enum.Parse(typeof(R1BonusScope),"PiercingDamage");bonus.Amount=.4f;
        var ordinary=Build(R4WeaponKind.Railgun,out _);var stronger=Build(R4WeaponKind.Railgun,out _,bonus:bonus);
        var strongRoot=stronger.GetComponentInParent<R3VehicleAssembly>();strongRoot.transform.position+=Vector3.right*20;Physics.SyncTransforms();
        Assert.That(strongRoot.GetComponent<P12PipelineSafeTankController>().EffectiveDamage,Is.EqualTo(40).Within(.01),"Basic kinetic shots must not gain piercing damage.");
        var target1=Target(ordinary,6);var target2=Target(stronger,6);
        Assert.That(ordinary.GetComponent<RRLRailgunBinding>().Fire(),Is.True);Assert.That(stronger.GetComponent<RRLRailgunBinding>().Fire(),Is.True);
        float deadline=Time.time+.8f;while(Time.time<deadline)yield return null;
        float baseline=500-target1.CurrentHp,boosted=500-target2.CurrentHp;
        Assert.That(baseline,Is.GreaterThan(0));Assert.That(boosted,Is.EqualTo(baseline*1.4f).Within(.2f),"The real rail shot must receive its piercing bonus.");
        Object.Destroy(bonus);Object.Destroy(target1.gameObject);Object.Destroy(target2.gameObject);
    }
    [UnityTest] public IEnumerator KineticScopedBonusReachesActualShotgunPellets()
    {
        Assert.That(System.Enum.GetNames(typeof(R1BonusScope)),Does.Contain("KineticDamage"));
        var bonus=ScriptableObject.CreateInstance<R1UpgradeDefinition>();bonus.Id="shotgun-kinetic";bonus.Effect=R1UpgradeEffect.Damage;
        bonus.Scope=(R1BonusScope)System.Enum.Parse(typeof(R1BonusScope),"KineticDamage");bonus.Amount=.4f;
        var ordinary=Build(R4WeaponKind.Shotgun,out _);var stronger=Build(R4WeaponKind.Shotgun,out _,bonus:bonus);
        stronger.GetComponentInParent<R3VehicleAssembly>().transform.position+=Vector3.right*20;Physics.SyncTransforms();
        var target1=Target(ordinary,6);var target2=Target(stronger,6);
        Assert.That(ordinary.GetComponent<RRLShotgunBinding>().Fire(),Is.True);Assert.That(stronger.GetComponent<RRLShotgunBinding>().Fire(),Is.True);
        float deadline=Time.time+.8f;while(Time.time<deadline)yield return null;
        float baseline=500-target1.CurrentHp,boosted=500-target2.CurrentHp;
        Assert.That(baseline,Is.GreaterThan(0));Assert.That(boosted,Is.EqualTo(baseline*1.4f).Within(.2f),"Every real pellet must receive the kinetic bonus.");
        Object.Destroy(bonus);Object.Destroy(target1.gameObject);Object.Destroy(target2.gameObject);
    }
    [UnityTest] public IEnumerator DoubleShotDuplicatesRocketWithoutExtraAmmoOrEnergy()
    {
        var gun=Build(R4WeaponKind.RocketLauncher,out var energy,true);var hp=Target(gun,6,2);float before=energy.Current;
        var binding=gun.GetComponent<RRLRocketBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-9f).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(9));
        float end=Time.time+1f;while(Time.time<end)yield return null;
        Assert.That(500-hp.CurrentHp,Is.InRange(200f,240f),"Two real rockets must explode without recursive procs.");
    }
    [UnityTest] public IEnumerator RocketBlastDamagesTargetAmongHundredsOfUnrelatedColliders()
    {
        var gun=Build(R4WeaponKind.RocketLauncher,out var energy);var hp=Target(gun,6,2);
        var fixture=new GameObject("Crowded explosion fixture");
        fixture.transform.position=gun.Muzzle.position+gun.Muzzle.forward*6f+gun.Muzzle.right*2f;
        for(int i=0;i<300;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.02f;
        Physics.SyncTransforms();
        Assert.That(Physics.OverlapSphere(fixture.transform.position,1).Count(c=>c.gameObject==fixture),Is.EqualTo(300));
        try
        {
            Assert.That(gun.GetComponent<RRLRocketBinding>().Fire(),Is.True);
            float end=Time.time+1f;while(Time.time<end)yield return null;
            Assert.That(hp.CurrentHp,Is.LessThan(500),"Unrelated nearby colliders must not erase an otherwise valid explosion.");
        }
        finally{Object.Destroy(fixture);}
    }
    [UnityTest] public IEnumerator RocketBlastHitsTargetBeyondInitialQueryBuffer()
    {
        var gun=Build(R4WeaponKind.RocketLauncher,out var energy);var hp=Target(gun,6,2);
        var fixture=new GameObject("Extreme explosion saturation fixture");
        fixture.transform.position=gun.Muzzle.position+gun.Muzzle.forward*6f+gun.Muzzle.right*2f;
        for(int i=0;i<1030;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.02f;
        Physics.SyncTransforms();
        try
        {
            Assert.That(gun.GetComponent<RRLRocketBinding>().Fire(),Is.True);
            float end=Time.time+1f;while(Time.time<end)yield return null;
            Assert.That(hp.CurrentHp,Is.LessThan(500),"The complete retry must still apply damage beyond the initial overlap buffer.");
        }
        finally{Object.Destroy(fixture);}
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
