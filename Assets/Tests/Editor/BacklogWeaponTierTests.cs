using System.Linq;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R1;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogWeaponTierTests
{
    [Test] public void ThreeIdenticalBasicGunsFuseToOneTierTwoGun()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(500);
        for(int i=0;i<3;i++)Assert.That(model.PurchaseToInventory(R3PartKind.Gun,R4WeaponKind.Basic),Is.True);
        Assert.That(model.FuseInventory(model.Inventory.Select(p=>p.Id).ToArray()),Is.True);
        var gun=model.Inventory.Single();Assert.That(gun.Weapon,Is.EqualTo(R4WeaponKind.Basic));Assert.That(gun.Tier,Is.EqualTo(2));
    }
    [Test] public void DifferentWeaponsAndUnsupportedPartsNeverFuse()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(1000);
        Assert.That(model.PurchaseToInventory(R3PartKind.Gun,R4WeaponKind.Basic),Is.True);
        Assert.That(model.PurchaseToInventory(R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
        Assert.That(model.PurchaseToInventory(R3PartKind.Gun,R4WeaponKind.Heavy),Is.True);
        var mixed=model.Inventory.Select(p=>p.Id).ToArray();
        Assert.That(model.FuseInventory(mixed),Is.False);Assert.That(model.Inventory.Select(p=>p.Id),Is.EqualTo(mixed));
        Assert.That(R3VehicleModel.SupportsFusion(R3PartKind.IndependentSteering),Is.False);
    }
}

public sealed class BacklogWeaponTierPhysicsTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();}
    [UnityTest] public IEnumerator AlreadyQueuedWeaponBatchesShareEmissionWithoutLosingShots()
    {
        var owner=new GameObject("Fair bonus queue owner");owner.AddComponent<Rigidbody>().isKinematic=true;
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var rent=typeof(P17PhysicalShell).GetMethod("RentConventional",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        P17PhysicalShell Primary(string id,float x)
        {
            var shell=(P17PhysicalShell)rent.Invoke(null,null);shell.transform.position=new Vector3(x,1000,1000);
            shell.GetComponent<R4ProjectileOrigin>().PartId=id;shell.gameObject.SetActive(true);
            shell.GetComponent<Rigidbody>().linearVelocity=Vector3.forward*20;shell.Configure(owner.transform,1,5,Vector3.forward,null);return shell;
        }
        var first=Primary("fair-A",1000);var second=Primary("fair-B",1010);
        var queue=owner.AddComponent<R3ConventionalProjectileQueue>();
        typeof(R3ConventionalProjectileQueue).GetField("workBudgetMilliseconds",flags).SetValue(queue,0d);
        var enqueue=typeof(R3ConventionalProjectileQueue).GetMethod("Enqueue",flags);
        enqueue.Invoke(queue,new object[]{first,3});enqueue.Invoke(queue,new object[]{second,3});
        int Count(string id)=>Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Count(p=>p.PartId==id);
        Assert.That(Count("fair-A"),Is.EqualTo(3));Assert.That(Count("fair-B"),Is.EqualTo(1));
        var drain=typeof(R3ConventionalProjectileQueue).GetMethod("FixedUpdate",flags);drain.Invoke(queue,null);
        Assert.That(Count("fair-A"),Is.EqualTo(3));Assert.That(Count("fair-B"),Is.EqualTo(2),"The waiting weapon must receive a turn before the first batch finishes.");
        drain.Invoke(queue,null);drain.Invoke(queue,null);drain.Invoke(queue,null);
        Assert.That(Count("fair-A"),Is.EqualTo(4));Assert.That(Count("fair-B"),Is.EqualTo(4));
        foreach(var tag in Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>p.PartId=="fair-A"||p.PartId=="fair-B"))
        {tag.gameObject.SetActive(false);Object.Destroy(tag.gameObject);}
        Object.Destroy(owner);yield return null;
    }
    [UnityTest] public IEnumerator ShotgunBonusCountsBeyondInt32RemainQueuedWithoutIteration()
    {
        var owner=new GameObject("Large shotgun queue owner");owner.AddComponent<Rigidbody>().isKinematic=true;
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var rent=typeof(P17PhysicalShell).GetMethod("RentConventional",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        var primaries=new GameObject[2];
        for(int i=0;i<primaries.Length;i++)
        {
            var shell=(P17PhysicalShell)rent.Invoke(null,null);shell.transform.position=new Vector3(1000,1000,1000);
            var tag=shell.GetComponent<R4ProjectileOrigin>();tag.PartId="huge-shotgun-queue";tag.Weapon=R4WeaponKind.Shotgun;
            shell.gameObject.SetActive(true);shell.GetComponent<Rigidbody>().linearVelocity=Vector3.forward*20;
            shell.Configure(owner.transform,1,5,Vector3.forward,null);primaries[i]=shell.gameObject;
        }
        var queue=owner.AddComponent<R3ConventionalProjectileQueue>();
        typeof(R3ConventionalProjectileQueue).GetField("workBudgetMilliseconds",flags).SetValue(queue,0d);
        typeof(R3ConventionalProjectileQueue).GetMethod("EnqueueShotgun",flags).Invoke(queue,new object[]{primaries,new[]{int.MaxValue,int.MaxValue}});
        var batches=(System.Collections.IEnumerable)typeof(R3ConventionalProjectileQueue).GetField("batches",flags).GetValue(queue);
        var iterator=batches.GetEnumerator();Assert.That(iterator.MoveNext(),Is.True);var batch=iterator.Current;
        var pending=(System.Numerics.BigInteger)batch.GetType().GetField("ShotgunRemaining").GetValue(batch);
        Assert.That(pending,Is.EqualTo((System.Numerics.BigInteger)int.MaxValue*2-1),"Only the one emitted bonus may be removed from the exact count.");
        Assert.That(Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Count(p=>p.PartId=="huge-shotgun-queue"),Is.EqualTo(3));
        foreach(var tag in Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>p.PartId=="huge-shotgun-queue"))
        {tag.gameObject.SetActive(false);Object.Destroy(tag.gameObject);}
        Object.Destroy(owner);yield return null;
    }
    [UnityTest] public IEnumerator DoubleShotTriggersOnceWithoutExtraEnergyOrRecursiveShots()
    {
        Assert.That(System.Enum.TryParse("DoubleShot",out R3PartKind kind),Is.True);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(1000);
        var chance=typeof(R3GarageEconomy).GetField("DoubleShotChancePerUnit");Assert.That(chance,Is.Not.Null);chance.SetValue(model.Economy,1f);
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct().First(c=>model.PlacementError(c,kind)==null);
        Assert.That(model.Buy(cell,kind),Is.True);
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*4f;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        var bank=vehicle.GetComponent<VehicleEnergyBank>();float before=bank.Current;Physics.SyncTransforms();
        Assert.That(assembly.ClearProjectilePath(gun),Is.True);assembly.FireGroup(false);
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>920.01f&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(920).Within(.01),"One original and exactly one extra 40-damage projectile must hit.");
        Assert.That(bank.Current,Is.EqualTo(before).Within(.001),"Extra basic shot cannot spend energy.");
        var module=assembly.Parts.Single(p=>p.Kind==kind);
        module.Damageable.RestoreHealthRatio(.01f);Assert.That(assembly.RollDoubleShotExtras(4),Is.EqualTo(1),"A damaged live passive keeps its full effect.");
        module.Tier=2;Assert.That(assembly.RollDoubleShotExtras(4),Is.EqualTo(2),"Chances above 100% produce guaranteed additional projectiles.");
        Assert.That(assembly.RollDoubleShotExtras(1),Is.EqualTo(1),"Existing projectile capacity cannot be exceeded.");module.Tier=1;
        module.gameObject.SetActive(false);Assert.That(assembly.RollDoubleShotExtras(4),Is.Zero);
        var randomState=Random.state;
        try
        {
            Random.InitState(817);float expected=Random.value;Random.InitState(817);
            Assert.That(assembly.RollDoubleShotExtras(4),Is.Zero);
            Assert.That(Random.value,Is.EqualTo(expected),"An absent bonus must not advance the game's random stream.");
        }
        finally{Random.state=randomState;}
        module.gameObject.SetActive(true);
        var receiver=module.GetComponent<RoboRougelike.Mechanics.EMP.EmpReceiver>();Assert.That(receiver,Is.Not.Null);
        receiver.ApplyEmp(2);Assert.That(assembly.RollDoubleShotExtras(4),Is.Zero);
        Object.Destroy(vehicle.gameObject);Object.Destroy(target);Object.Destroy(paletteObject);Object.Destroy(material);
    }
    [UnityTest] public IEnumerator TierTwoBasicGunDealsDoublePhysicalDamageWithoutEnergy()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var gunState=model.Parts.First(p=>p.Kind==R3PartKind.Gun);gunState.Tier=2;
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        var body=vehicle.GetComponent<Rigidbody>();body.isKinematic=true;
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.Single(p=>p.Id==gunState.Id);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*4f;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        var bank=vehicle.GetComponent<VehicleEnergyBank>();Assert.That(bank.TrySpend(bank.Current),Is.True);
        Physics.SyncTransforms();assembly.FireGroup(false);
        Assert.That(bank.Current,Is.Zero,"The basic gun must remain free at every tier.");
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>=1000&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(920).Within(.01),"A real projectile deals 2× the 40 HP base damage.");
        UnityEngine.Object.Destroy(vehicle.gameObject);UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(paletteObject);UnityEngine.Object.Destroy(material);
    }
    [UnityTest] public IEnumerator TierTwoAutomaticGunSpendsThreeEnergyAndDealsDoubleDamage()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var gunState=model.Parts.First(p=>p.Kind==R3PartKind.Gun);gunState.Weapon=R4WeaponKind.Automatic;gunState.Tier=2;
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.Single(p=>p.Id==gunState.Id);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*4f;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        var bank=vehicle.GetComponent<VehicleEnergyBank>();float before=bank.Current;
        Physics.SyncTransforms();assembly.FireGroup(false);
        Assert.That(bank.Current,Is.EqualTo(before-3f).Within(.001));
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>=1000&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(976).Within(.01),"A real projectile deals 2× the 12 HP base damage.");
        UnityEngine.Object.Destroy(vehicle.gameObject);UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(paletteObject);UnityEngine.Object.Destroy(material);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
