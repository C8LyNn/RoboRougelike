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
    [UnityTest] public IEnumerator DoubleShotVolleySnapshotKeepsIndependentRollsAndModuleStateAtFireTime()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(1000);
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,R3PartKind.DoubleShot)==null);
        Assert.That(model.Buy(cell,R3PartKind.DoubleShot),Is.True);model.Economy.DoubleShotChancePerUnit=.35f;
        var paletteObject=new GameObject("Snapshot palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var passive=assembly.Parts.Single(p=>p.Kind==R3PartKind.DoubleShot);passive.Tier=2;
        var capture=typeof(R3VehicleAssembly).GetMethod("CaptureDoubleShotRoll",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That(capture,Is.Not.Null,"A volley must capture its bonuses once, then sample each pellet independently.");
        var snapshot=capture.Invoke(assembly,null);var roll=snapshot.GetType().GetMethod("Roll");Assert.That(roll,Is.Not.Null);
        var original=Random.state;
        try
        {
            Random.InitState(913);var expected=new int[64];
            for(int i=0;i<expected.Length;i++)expected[i]=assembly.RollDoubleShotExtras(int.MaxValue);
            Random.InitState(913);var actual=new int[64];
            for(int i=0;i<actual.Length;i++)actual[i]=(int)roll.Invoke(snapshot,new object[]{int.MaxValue});
            Assert.That(actual,Is.EqualTo(expected));Assert.That(actual.Distinct().Count(),Is.EqualTo(2),"Fractional chance is rolled independently per pellet.");
            var exact=snapshot.GetType().GetMethod("RollExact");Assert.That(exact,Is.Not.Null);
            Random.InitState(913);
            for(int i=0;i<expected.Length;i++)
                Assert.That((System.Numerics.BigInteger)exact.Invoke(snapshot,null),Is.EqualTo(new System.Numerics.BigInteger(expected[i])),"The unbounded production roll must preserve ordinary random results.");
            passive.gameObject.SetActive(false);
            var inactive=capture.Invoke(assembly,null);Assert.That((int)roll.Invoke(inactive,new object[]{int.MaxValue}),Is.Zero);
            model.Economy.DoubleShotChancePerUnit=1f;
            Random.InitState(913);
            for(int i=0;i<expected.Length;i++)
                Assert.That((int)roll.Invoke(snapshot,new object[]{int.MaxValue}),Is.EqualTo(expected[i]),"The captured chance must survive later vehicle changes.");
        }
        finally{Random.state=original;Object.Destroy(vehicle.gameObject);Object.Destroy(paletteObject);Object.Destroy(material);}
        yield return null;
    }
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
    [UnityTest] public IEnumerator ProjectileBudgetIsSharedBetweenOwnersAndResumesOnLaterFrames()
    {
        var owners=new[]{new GameObject("Budget owner A"),new GameObject("Budget owner B")};
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var rent=typeof(P17PhysicalShell).GetMethod("RentConventional",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        var queues=new R3ConventionalProjectileQueue[2];
        float oldTimeScale=Time.timeScale;Time.timeScale=0;
        try
        {
            for(int i=0;i<2;i++)
            {
                owners[i].AddComponent<Rigidbody>().isKinematic=true;
                var shell=(P17PhysicalShell)rent.Invoke(null,null);shell.transform.position=new Vector3(1300+i*5,1300,1300);
                shell.GetComponent<R4ProjectileOrigin>().PartId="budget-owner-"+i;shell.gameObject.SetActive(true);
                shell.GetComponent<Rigidbody>().linearVelocity=Vector3.forward*20;shell.Configure(owners[i].transform,1,5,Vector3.forward,null);
                queues[i]=owners[i].AddComponent<R3ConventionalProjectileQueue>();
                typeof(R3ConventionalProjectileQueue).GetField("workBudgetMilliseconds",flags).SetValue(queues[i],1e-12d);
                typeof(R3ConventionalProjectileQueue).GetMethod("Enqueue",flags).Invoke(queues[i],new object[]{shell,2});
            }
            int Count(int i)=>Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Count(p=>p.PartId=="budget-owner-"+i);
            Assert.That(Count(0),Is.EqualTo(2));
            Assert.That(Count(1),Is.EqualTo(1),"Exhausted shared budget must defer the other owner's bonus, not force another shot.");
            var drain=typeof(R3ConventionalProjectileQueue).GetMethod("Drain",flags);
            yield return null;drain.Invoke(queues[0],null);
            yield return null;drain.Invoke(queues[1],null);
            Assert.That(Count(1),Is.EqualTo(2),"The waiting owner must receive a turn.");
            yield return null;drain.Invoke(queues[1],null);
            Assert.That(Count(0),Is.EqualTo(3));Assert.That(Count(1),Is.EqualTo(3),"All due shots must eventually be emitted.");
        }
        finally
        {
            Time.timeScale=oldTimeScale;
            foreach(var shot in Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>p.PartId!=null&&p.PartId.StartsWith("budget-owner-")))
                if(shot!=null){shot.gameObject.SetActive(false);Object.Destroy(shot.gameObject);}
            foreach(var owner in owners)Object.Destroy(owner);
        }
    }
    [UnityTest] public IEnumerator DisabledQueueDoesNotBlockOthersAndResumesWhenReenabled()
    {
        var owners=new[]{new GameObject("Disabled queue owner A"),new GameObject("Disabled queue owner B")};
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var rent=typeof(P17PhysicalShell).GetMethod("RentConventional",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        var queues=new R3ConventionalProjectileQueue[2];float oldTimeScale=Time.timeScale;Time.timeScale=0;
        try
        {
            for(int i=0;i<2;i++)
            {
                owners[i].AddComponent<Rigidbody>().isKinematic=true;
                var shell=(P17PhysicalShell)rent.Invoke(null,null);shell.transform.position=new Vector3(1500+i*5,1500,1500);
                shell.GetComponent<R4ProjectileOrigin>().PartId="disabled-queue-"+i;shell.gameObject.SetActive(true);
                shell.GetComponent<Rigidbody>().linearVelocity=Vector3.forward*20;shell.Configure(owners[i].transform,1,5,Vector3.forward,null);
                queues[i]=owners[i].AddComponent<R3ConventionalProjectileQueue>();
                typeof(R3ConventionalProjectileQueue).GetField("workBudgetMilliseconds",flags).SetValue(queues[i],1e-12d);
                typeof(R3ConventionalProjectileQueue).GetMethod("Enqueue",flags).Invoke(queues[i],new object[]{shell,i==0?2:1});
            }
            int Count(int i)=>Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Count(p=>p.PartId=="disabled-queue-"+i);
            Assert.That(Count(0),Is.EqualTo(2));Assert.That(Count(1),Is.EqualTo(1));
            queues[0].enabled=false;
            yield return null;typeof(R3ConventionalProjectileQueue).GetMethod("Drain",flags).Invoke(queues[1],null);
            Assert.That(Count(1),Is.EqualTo(2),"A disabled queue must not retain the scheduler's turn.");
            Assert.That(Count(0),Is.EqualTo(2));queues[0].enabled=true;
            yield return null;typeof(R3ConventionalProjectileQueue).GetMethod("Drain",flags).Invoke(queues[0],null);
            Assert.That(Count(0),Is.EqualTo(3),"Disabling only the component preserves its queued shot for reenable.");
        }
        finally
        {
            Time.timeScale=oldTimeScale;
            foreach(var shot in Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>p.PartId!=null&&p.PartId.StartsWith("disabled-queue-")))
                if(shot!=null){shot.gameObject.SetActive(false);Object.Destroy(shot.gameObject);}
            foreach(var owner in owners)Object.Destroy(owner);
        }
    }
    [UnityTest] public IEnumerator ProjectileBudgetIsSharedAcrossShellRocketAndDeviceQueues()
    {
        var owners=new[]{new GameObject("Shared shell owner"),new GameObject("Shared rocket owner"),new GameObject("Shared device owner")};
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var rocketTemplate=new GameObject("Shared budget rocket template");rocketTemplate.SetActive(false);
        var shooter=GameObject.CreatePrimitive(PrimitiveType.Cube);shooter.transform.position=new Vector3(1400,1400,1400);
        float oldTimeScale=Time.timeScale;Time.timeScale=0;
        try
        {
            foreach(var owner in owners)owner.AddComponent<Rigidbody>().isKinematic=true;
            var rent=typeof(P17PhysicalShell).GetMethod("RentConventional",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            var shell=(P17PhysicalShell)rent.Invoke(null,null);shell.transform.position=new Vector3(1300,1300,1300);
            shell.GetComponent<R4ProjectileOrigin>().PartId="shared-budget-shell";shell.gameObject.SetActive(true);
            shell.GetComponent<Rigidbody>().linearVelocity=Vector3.forward*20;shell.Configure(owners[0].transform,1,5,Vector3.forward,null);
            var conventional=owners[0].AddComponent<R3ConventionalProjectileQueue>();
            typeof(R3ConventionalProjectileQueue).GetField("workBudgetMilliseconds",flags).SetValue(conventional,1e-12d);
            typeof(R3ConventionalProjectileQueue).GetMethod("Enqueue",flags).Invoke(conventional,new object[]{shell,1});

            rocketTemplate.AddComponent<R4ProjectileOrigin>().PartId="shared-budget-rocket";
            var template=rocketTemplate.AddComponent<RoboRougelike.Mechanics.Rockets.RocketProjectile>();
            var rockets=owners[1].AddComponent<RoboRougelike.WeaponIntegration.RRLRocketVolleyQueue>();
            rockets.GetType().GetField("workBudgetMilliseconds",flags).SetValue(rockets,1e-12d);
            var spec=new RoboRougelike.Mechanics.Rockets.RocketConfig().Snapshot();
            rockets.GetType().GetMethod("Enqueue",flags).Invoke(rockets,new object[]{1,template,spec,owners[1].GetComponent<Rigidbody>(),owners[1].transform,owners[1],
                new Vector3(1350,1350,1350),Quaternion.identity,Vector3.forward,null});

            shooter.AddComponent<Rigidbody>().isKinematic=true;
            var drone=shooter.AddComponent<R3FlyingDrone>();drone.ConfigureTurret(owners[2].transform,.1f,1,40);drone.enabled=false;
            var batch=typeof(R3FlyingDrone).GetMethod("CaptureShot",flags).Invoke(drone,new object[]{Vector3.forward});
            batch.GetType().GetField("Remaining").SetValue(batch,System.Numerics.BigInteger.One);
            var devices=owners[2].AddComponent<R3DeviceProjectileQueue>();
            typeof(R3DeviceProjectileQueue).GetField("workBudgetMilliseconds",flags).SetValue(devices,1e-12d);
            typeof(R3DeviceProjectileQueue).GetMethod("Enqueue",flags).Invoke(devices,new[]{batch});
            int Shells()=>Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Count(p=>p.PartId=="shared-budget-shell");
            int Rockets()=>Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Count(p=>p.PartId=="shared-budget-rocket");
            int Devices()=>Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Count(p=>p.name=="P17_DronePhysicalShell"&&p.OwnerRoot==owners[2].transform);
            Assert.That(Shells(),Is.EqualTo(2));Assert.That(Rockets(),Is.Zero);Assert.That(Devices(),Is.Zero);
            yield return null;rockets.GetType().GetMethod("Drain",flags).Invoke(rockets,null);
            Assert.That(Rockets(),Is.EqualTo(1));Assert.That(Devices(),Is.Zero);
            yield return null;typeof(R3DeviceProjectileQueue).GetMethod("Drain",flags).Invoke(devices,null);
            Assert.That(Devices(),Is.EqualTo(1),"The remaining device shot must be emitted in a later frame.");
        }
        finally
        {
            Time.timeScale=oldTimeScale;
            foreach(var shot in Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>p.PartId!=null&&p.PartId.StartsWith("shared-budget-")))
                if(shot!=null){shot.gameObject.SetActive(false);Object.Destroy(shot.gameObject);}
            foreach(var shot in Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Where(p=>p.name=="P17_DronePhysicalShell"&&p.OwnerRoot==owners[2].transform))
                if(shot!=null){shot.gameObject.SetActive(false);Object.Destroy(shot.gameObject);}
            Object.Destroy(shooter);Object.Destroy(rocketTemplate);foreach(var owner in owners)Object.Destroy(owner);
        }
    }
    [UnityTest] public IEnumerator QueuedRocketBatchesShareEmissionWithoutStarvation()
    {
        var owner=new GameObject("Fair rocket queue owner");var body=owner.AddComponent<Rigidbody>();body.isKinematic=true;
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var templates=new GameObject[2];
        for(int i=0;i<2;i++)
        {
            templates[i]=new GameObject("Fair rocket template");templates[i].SetActive(false);
            templates[i].AddComponent<R4ProjectileOrigin>().PartId="fair-rocket-"+i;
            templates[i].AddComponent<RoboRougelike.Mechanics.Rockets.RocketProjectile>();
        }
        var queue=owner.AddComponent<RoboRougelike.WeaponIntegration.RRLRocketVolleyQueue>();
        queue.GetType().GetField("workBudgetMilliseconds",flags).SetValue(queue,0d);
        var enqueue=queue.GetType().GetMethod("Enqueue",flags);var drain=queue.GetType().GetMethod("FixedUpdate",flags);
        var spec=new RoboRougelike.Mechanics.Rockets.RocketConfig().Snapshot();
        int Count(int i)=>Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Count(p=>p.PartId=="fair-rocket-"+i);
        try
        {
            for(int i=0;i<2;i++)enqueue.Invoke(queue,new object[]{i==0?4:2,templates[i].GetComponent<RoboRougelike.Mechanics.Rockets.RocketProjectile>(),spec,body,owner.transform,owner,
                new Vector3(1000+i*5,1000,1000),Quaternion.identity,Vector3.forward,null});
            Assert.That(Count(0),Is.EqualTo(2));Assert.That(Count(1),Is.Zero);
            drain.Invoke(queue,null);
            Assert.That(Count(1),Is.EqualTo(1),"A later launcher must receive a turn before a large earlier batch finishes.");
            for(int i=0;i<3;i++)drain.Invoke(queue,null);
            Assert.That(Count(0),Is.EqualTo(4));Assert.That(Count(1),Is.EqualTo(2));
        }
        finally
        {
            foreach(var shot in Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>p.PartId!=null&&p.PartId.StartsWith("fair-rocket-")))
                if(shot!=null)Object.Destroy(shot.gameObject);
            foreach(var template in templates)Object.Destroy(template);Object.Destroy(owner);
        }
        yield return null;
    }
    [UnityTest] public IEnumerator UnevenShotgunBonusesEmitInOrderWithoutLosingDirections()
    {
        var owner=new GameObject("Uneven shotgun queue owner");owner.AddComponent<Rigidbody>().isKinematic=true;
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var rent=typeof(P17PhysicalShell).GetMethod("RentConventional",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        var primaries=new GameObject[5];string Prefix(int i)=>"uneven-shotgun-"+i;
        Vector3 Direction(int i)=>Quaternion.AngleAxis(i*10,Vector3.up)*Vector3.forward;
        for(int i=0;i<primaries.Length;i++)
        {
            var shell=(P17PhysicalShell)rent.Invoke(null,null);shell.transform.position=new Vector3(1000+i*5,1000,1000);
            var tag=shell.GetComponent<R4ProjectileOrigin>();tag.PartId=Prefix(i);tag.Weapon=R4WeaponKind.Shotgun;
            shell.gameObject.SetActive(true);shell.GetComponent<Rigidbody>().linearVelocity=Direction(i)*20;
            shell.Configure(owner.transform,1,5,Direction(i),null);primaries[i]=shell.gameObject;
        }
        var queue=owner.AddComponent<R3ConventionalProjectileQueue>();
        typeof(R3ConventionalProjectileQueue).GetField("workBudgetMilliseconds",flags).SetValue(queue,0d);
        var drain=typeof(R3ConventionalProjectileQueue).GetMethod("Drain",flags);
        int Count(int i)=>Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Count(p=>p.PartId==Prefix(i));
        try
        {
            typeof(R3ConventionalProjectileQueue).GetMethod("EnqueueShotgun",flags).Invoke(queue,new object[]{primaries,new[]{0,2,0,1,3}});
            var expectedCounts=new[]{1,1,1,1,1};int[] order={1,3,4,1,4,4};
            for(int turn=0;turn<order.Length;turn++)
            {
                expectedCounts[order[turn]]++;
                for(int p=0;p<primaries.Length;p++)Assert.That(Count(p),Is.EqualTo(expectedCounts[p]),"Each emitted pellet must use the expected original direction.");
                if(turn+1<order.Length)drain.Invoke(queue,null);
            }
            drain.Invoke(queue,null);
            for(int p=0;p<primaries.Length;p++)Assert.That(Count(p),Is.EqualTo(expectedCounts[p]),"A completed batch cannot emit extra pellets.");
            for(int p=0;p<primaries.Length;p++)
                foreach(var tag in Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(item=>item.PartId==Prefix(p)))
                    Assert.That(Vector3.Dot(tag.GetComponent<Rigidbody>().linearVelocity.normalized,Direction(p)),Is.GreaterThan(.9999f));
        }
        finally
        {
            foreach(var tag in Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>p.PartId!=null&&p.PartId.StartsWith("uneven-shotgun-")))
            {tag.gameObject.SetActive(false);Object.Destroy(tag.gameObject);}
            Object.Destroy(owner);
        }
        yield return null;
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
