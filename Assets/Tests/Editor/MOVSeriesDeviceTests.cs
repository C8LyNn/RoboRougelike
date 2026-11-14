using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.Booster;
using RoboRougelike.Mechanics.MOV002JumpDrive;
using RoboRougelike.Mechanics.MOV004GyroscopicStabilizer;

public class MOVSeriesDeviceTests
{
    GameObject fixtures;R3VisualPalette palette;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;
        fixtures=new GameObject("Original MOV integration fixtures");palette=UnityEngine.Object.FindFirstObjectByType<R3VisualPalette>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixtures.transform);
        floor.transform.position=new Vector3(0,-.5f,1000);floor.transform.localScale=new Vector3(180,1,180);
    }
    public static IEnumerator Seconds(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}
    // EditMode runner hosts EnterPlayMode fixtures but does not honor runtime yield objects.
    public static IEnumerator FixedStep(){float before=Time.fixedTime;while(Time.fixedTime<=before)yield return null;}
    public static Vector3Int FindFixtureCell(R3VehicleModel model,R3PartKind kind,Vector3Int preferred,Func<Vector3Int,bool> additional=null)
    {
        var available=model.Parts.Where(p=>p.Kind==R3PartKind.Armor||p.Kind==R3PartKind.Core)
            .Select(p=>p.Cell+Vector3Int.up).Distinct()
            .Where(c=>model.At(c)==null&&model.PlacementError(c,kind,true)==null&&(additional==null||additional(c)))
            .OrderBy(c=>(c-preferred).sqrMagnitude).ThenBy(c=>c.x).ThenBy(c=>c.z).ToArray();
        Assert.That(available,Is.Not.Empty,"Device fixture needs a legal supported cell for "+kind);
        return available[0];
    }
    R1VehicleState Build(R3VehicleKind kind,float x,params R3PartKind[] devices)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(kind));
        // Real plans on contiguous top hull cells, without a second damage/ownership model.
        int y=kind==R3VehicleKind.Light?1:2;
        for(int i=0;i<devices.Length;i++)
        {
            var preferred=kind==R3VehicleKind.Standard?new Vector3Int(i%2==0?-2:2,y,i<2?-1:1):new Vector3Int(i%2==0?-1:1,y,i/2);
            // Starter layouts evolved: choose a genuinely free, supported
            // top cell close to the original pose instead of replacing a part.
            var cell=FindFixtureCell(model,devices[i],preferred);
            Assert.That(model.At(cell),Is.Null,"Device fixture cell must be free: "+cell);
            Assert.That(model.At(cell+Vector3Int.down),Is.Not.Null,"Device fixture needs hull support: "+cell);
            model.Parts.Add(new R3PartState(cell,devices[i]));
        }
        Assert.That(model.Parts.Select(p=>p.Cell).Distinct().Count(),Is.EqualTo(model.Parts.Count),"Fixture parts must not share a cell.");
        var v=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(x,.7f,1000),Quaternion.identity);
        v.transform.SetParent(fixtures.transform,true);v.Controller.SetGameplayInputEnabled(true);return v;
    }
    static Component Adapter(R1VehicleState v)
    {
        var type=Type.GetType("RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter, Assembly-CSharp");
        Assert.That(type,Is.Not.Null,"Real vehicle coordinator missing.");var c=v.GetComponent(type);Assert.That(c,Is.Not.Null);return c;
    }
    static bool Action(R1VehicleState v,string method)=>(bool)Adapter(v).GetType().GetMethod(method).Invoke(Adapter(v),null);
    R1VehicleState BuildTieredBooster(int tier,float x)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var cell=FindFixtureCell(model,R3PartKind.Booster,new Vector3Int(1,1,0));
        model.Parts.Add(new R3PartState(cell,R3PartKind.Booster){Tier=tier});
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(x,.7f,1000),Quaternion.identity);
        vehicle.transform.SetParent(fixtures.transform,true);vehicle.Controller.SetGameplayInputEnabled(true);return vehicle;
    }
    R1VehicleState BuildMixedTierBoosters()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        foreach(int tier in new[]{1,3})
        {
            var cell=FindFixtureCell(model,R3PartKind.Booster,new Vector3Int(tier==1?-1:1,1,0));
            model.Parts.Add(new R3PartState(cell,R3PartKind.Booster){Tier=tier});
        }
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,.7f,1000),Quaternion.identity);
        vehicle.transform.SetParent(fixtures.transform,true);vehicle.Controller.SetGameplayInputEnabled(true);return vehicle;
    }
    [UnityTest] public IEnumerator MixedTierBoostersSpendCombinedEnergyAndStopDisabledCopy()
    {
        var v=BuildMixedTierBoosters();yield return Seconds(.6f);
        var bank=v.GetComponent<VehicleEnergyBank>();var boosters=v.GetComponentsInChildren<VehicleBooster>();
        float cost=VehicleEnergyCosts.BoosterActivation*(R3VehicleModel.TierActivationCost(1)+R3VehicleModel.TierActivationCost(3));
        Assert.That(bank.TrySpend(bank.Current-cost+.1f),Is.True);
        float before=bank.Current;Assert.That(Action(v,"TryBoost"),Is.False);
        Assert.That(bank.Current,Is.EqualTo(before).Within(.001));Assert.That(boosters.All(b=>!b.IsBoosting),Is.True);
        bank.Recharge(100);before=bank.Current;Assert.That(Action(v,"TryBoost"),Is.True);
        Assert.That(bank.Current,Is.EqualTo(before-cost).Within(.001));Assert.That(boosters.All(b=>b.IsBoosting),Is.True);
        var high=boosters.Single(b=>b.GetComponent<R3MountedPart>().Tier==3);high.SetOperational(false);
        Assert.That(high.IsBoosting,Is.False);Assert.That(boosters.Single(b=>b!=high).IsBoosting,Is.True);
    }
    [UnityTest] public IEnumerator HigherTierBoosterUsesTierEnergyAndAddsPhysicalThrust()
    {
        var one=BuildTieredBooster(1,-20);var two=BuildTieredBooster(2,20);var three=BuildTieredBooster(3,45);yield return Seconds(.6f);
        var a=one.GetComponent<Rigidbody>();var b=two.GetComponent<Rigidbody>();var c=three.GetComponent<Rigidbody>();
        a.linearVelocity=Vector3.zero;b.linearVelocity=Vector3.zero;c.linearVelocity=Vector3.zero;
        var first=one.GetComponent<VehicleEnergyBank>();var second=two.GetComponent<VehicleEnergyBank>();var third=three.GetComponent<VehicleEnergyBank>();
        float before1=first.Current,before2=second.Current,before3=third.Current;
        Assert.That(Action(one,"TryBoost"),Is.True);Assert.That(Action(two,"TryBoost"),Is.True);Assert.That(Action(three,"TryBoost"),Is.True);
        Assert.That(first.Current,Is.EqualTo(before1-VehicleEnergyCosts.BoosterActivation).Within(.001));
        Assert.That(second.Current,Is.EqualTo(before2-VehicleEnergyCosts.BoosterActivation*R3VehicleModel.TierActivationCost(2)).Within(.001));
        Assert.That(third.Current,Is.EqualTo(before3-VehicleEnergyCosts.BoosterActivation*R3VehicleModel.TierActivationCost(3)).Within(.001));
        yield return Seconds(.25f);
        Assert.That(b.linearVelocity.z,Is.GreaterThan(a.linearVelocity.z+.15f),"A higher tier must increase real thrust, not only its label.");
        Assert.That(c.linearVelocity.z,Is.GreaterThan(b.linearVelocity.z+.15f),"Tier 3 must add physical thrust over tier 2.");
    }
    [UnityTest] public IEnumerator TwoBoostersRequireCombinedEnergyAndBothProducePhysicalThrust()
    {
        var v=Build(R3VehicleKind.Light,0,R3PartKind.Booster,R3PartKind.Booster);yield return Seconds(.6f);
        var bank=v.GetComponent<VehicleEnergyBank>();var boosters=v.GetComponentsInChildren<VehicleBooster>();
        Assert.That(boosters.Length,Is.EqualTo(2));
        float oneCost=VehicleEnergyCosts.BoosterActivation;
        Assert.That(bank.TrySpend(bank.Current-oneCost-1f),Is.True);
        Assert.That(Action(v,"TryBoost"),Is.False,"Energy sufficient for one copy must not start a partial boost.");
        Assert.That(boosters.All(b=>!b.IsBoosting),Is.True);
        bank.Recharge(100);float before=bank.Current;
        Assert.That(Action(v,"TryBoost"),Is.True);Assert.That(boosters.All(b=>b.IsBoosting),Is.True);
        Assert.That(bank.Current,Is.EqualTo(before-2*oneCost).Within(.001f));
        var copy=v.GetComponent<R3VehicleAssembly>().Model.DeepCopy();copy.Parts.RemoveAll(p=>p.Kind==R3PartKind.Booster);
        Assert.That(copy.BoosterReadyAt,Is.GreaterThan(Time.time));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(copy.MakeCheckpoint(Time.time),R3VehicleKind.Light,copy.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.BoosterReadyAt,Is.GreaterThan(Time.time),"Removing every copy must not reset the shared cooldown in a save.");
        float speed=v.GetComponent<Rigidbody>().linearVelocity.z;yield return Seconds(.25f);
        Assert.That(v.GetComponent<Rigidbody>().linearVelocity.z,Is.GreaterThan(speed+.2f),"Real boosters must accelerate the vehicle.");
        Assert.That(Action(v,"TryBoost"),Is.False,"Copies share their activation cycle.");
    }
    [UnityTest] public IEnumerator TwoBoostersAccelerateMoreThanOneOnComparablePhysicalVehicles()
    {
        var one=Build(R3VehicleKind.Light,-20,R3PartKind.Booster,R3PartKind.Booster);
        var two=Build(R3VehicleKind.Light,20,R3PartKind.Booster,R3PartKind.Booster);
        one.GetComponentsInChildren<VehicleBooster>()[1].gameObject.SetActive(false);
        yield return Seconds(.6f);
        var a=one.GetComponent<Rigidbody>();var b=two.GetComponent<Rigidbody>();
        a.linearVelocity=Vector3.zero;b.linearVelocity=Vector3.zero;
        Assert.That(Action(one,"TryBoost"),Is.True);Assert.That(Action(two,"TryBoost"),Is.True);
        yield return Seconds(.3f);
        Assert.That(b.linearVelocity.z,Is.GreaterThan(a.linearVelocity.z+.2f),"A second working copy must add thrust, rather than sharing a fixed total cap.");
    }
    [UnityTest] public IEnumerator ActualPartsBindOneBodyWithoutBecomingWeapons()
    {
        var v=Build(R3VehicleKind.Light,0,R3PartKind.Booster,R3PartKind.JumpDrive,R3PartKind.Gyroscope);
        Adapter(v);Assert.That(v.GetComponentsInChildren<VehicleBooster>().Length,Is.EqualTo(1));
        Assert.That(v.GetComponentsInChildren<JumpDrive>().Single().VehicleBody,Is.SameAs(v.GetComponent<Rigidbody>()));
        Assert.That(v.GetComponentsInChildren<GyroStabilizer>().Single().VehicleBody,Is.SameAs(v.GetComponent<Rigidbody>()));
        Assert.That(v.Modules.ActiveWeaponCount,Is.EqualTo(1));Assert.That(v.GetComponentsInChildren<Rigidbody>().Length,Is.EqualTo(1));
        Assert.That(v.GetComponent<Rigidbody>().useGravity,Is.True);yield return null;
    }
    [UnityTest] public IEnumerator BoosterHasFiniteMassSensitiveThrustAndPersistentCooldown()
    {
        var kinds=new[]{R3VehicleKind.Light,R3VehicleKind.Standard,R3VehicleKind.Heavy};var speeds=new float[3];
        for(int i=0;i<3;i++)
        {
            var v=Build(kinds[i],-40+i*40,R3PartKind.Booster,R3PartKind.Gyroscope);yield return Seconds(.6f);
            Assert.That(Action(v,"TryBoost"),Is.True);Assert.That(Action(v,"TryBoost"),Is.False);yield return Seconds(.8f);
            var body=v.GetComponent<Rigidbody>();speeds[i]=body.linearVelocity.z;
            var boost=v.GetComponentInChildren<VehicleBooster>();var material=v.GetComponentInChildren<Collider>().sharedMaterial;
            Debug.Log($"MOV001 actual {kinds[i]} mass={body.mass:F1} vz={speeds[i]:F3} active={boost.IsBoosting} remain={boost.RemainingBoostSeconds:F3} control={v.Controller.GameplayInputEnabled} up={body.rotation*Vector3.up} friction={material.dynamicFriction} available={boost.IsOperational} pos={body.position}");
            Assert.That(speeds[i],Is.InRange(.3f,10));
            var model=v.GetComponent<R3VehicleAssembly>().Model;var state=model.Parts.Single(p=>p.Kind==R3PartKind.Booster);
            Assert.That(state.DeviceReadyAt,Is.GreaterThan(Time.time));
            v.gameObject.SetActive(false);var rebuilt=R3VehicleAssembly.Build(model.DeepCopy(),new R1RunStats(),palette,new Vector3(-40+i*40,.7f,1020),Quaternion.identity);
            rebuilt.transform.SetParent(fixtures.transform,true);rebuilt.Controller.SetGameplayInputEnabled(true);
            Assert.That(Action(rebuilt,"TryBoost"),Is.False,"Rebuild must not reset cooldown.");rebuilt.gameObject.SetActive(false);
        }
        Assert.That(speeds[0],Is.GreaterThan(speeds[1]));Assert.That(speeds[1],Is.GreaterThan(speeds[2]));
    }
    [UnityTest] public IEnumerator JumpPreservesHorizontalMomentumRejectsAirAndCooperatesWithBoostGyro()
    {
        var v=Build(R3VehicleKind.Standard,0,R3PartKind.JumpDrive,R3PartKind.Gyroscope,R3PartKind.Booster);yield return Seconds(.7f);
        var b=v.GetComponent<Rigidbody>();b.linearVelocity=new Vector3(1,0,2);float before=b.position.y;
        Assert.That(Action(v,"TryJump"),Is.True);Assert.That(b.linearVelocity.x,Is.EqualTo(1).Within(.001));
        Assert.That(b.linearVelocity.z,Is.EqualTo(2).Within(.001));Assert.That(b.linearVelocity.y,Is.InRange(1,5.21f));
        Assert.That(Action(v,"TryBoost"),Is.True);Assert.That(Action(v,"TryJump"),Is.False);
        float peak=before;float end=Time.time+1;while(Time.time<end){peak=Mathf.Max(peak,b.position.y);yield return null;}
        Debug.Log($"MOV002 actual Standard peak={peak-before:F3} vz={b.linearVelocity.z:F3}");
        Assert.That(peak-before,Is.GreaterThan(.2f));Assert.That(b.linearVelocity.z,Is.GreaterThan(2));
        var airborne=Build(R3VehicleKind.Light,30,R3PartKind.JumpDrive);airborne.GetComponent<Rigidbody>().position+=Vector3.up*5;
        airborne.transform.position=airborne.GetComponent<Rigidbody>().position;Physics.SyncTransforms();
        Assert.That(Action(airborne,"TryJump"),Is.False);
    }
    [UnityTest] public IEnumerator GyroReducesRollWithoutCancellingYawAndCanBeToggled()
    {
        var v=Build(R3VehicleKind.Heavy,0,R3PartKind.Gyroscope,R3PartKind.Booster);Adapter(v);
        // Free flight isolates the stabilizer from wheel yaw servo and floor contact.
        v.Controller.enabled=false;var b=v.GetComponent<Rigidbody>();b.useGravity=false;b.position+=Vector3.up*8;
        b.rotation=Quaternion.Euler(0,0,30);b.angularVelocity=Vector3.up*.8f;float initial=30;
        yield return Seconds(1);float tilt=Vector3.Angle(b.rotation*Vector3.up,Vector3.up);
        Debug.Log($"MOV004 Heavy tilt={tilt:F3} yaw={b.angularVelocity.y:F3}");
        Assert.That(tilt,Is.LessThan(initial*.5f));Assert.That(b.angularVelocity.y,Is.GreaterThan(.5f));
        Assert.That(Action(v,"ToggleGyro"),Is.True);yield return FixedStep();
        Assert.That(v.GetComponentInChildren<GyroStabilizer>().LastAngularAcceleration,Is.EqualTo(Vector3.zero));
    }
    [UnityTest] public IEnumerator SameTickDetachDeathAndRebuildUseRealPartHealth()
    {
        var v=Build(R3VehicleKind.Light,0,R3PartKind.Booster,R3PartKind.JumpDrive,R3PartKind.Gyroscope);Adapter(v);
        var parts=v.GetComponent<R3VehicleAssembly>().Parts;var booster=parts.Single(p=>p.Kind==R3PartKind.Booster);
        float mass=v.GetComponent<Rigidbody>().mass;
        booster.Block.MarkDetached();Assert.That(booster.GetComponent<VehicleBooster>().IsOperational,Is.False);
        Assert.That(Action(v,"TryBoost"),Is.False);
        Assert.That(v.GetComponent<Rigidbody>().mass,Is.EqualTo(mass-18).Within(.001),"Graph-detached parts cannot remain in propulsion mass.");
        var gyro=parts.Single(p=>p.Kind==R3PartKind.Gyroscope);gyro.Damageable.ApplyDamage(10000,gyro.transform.position,Vector3.zero);
        Assert.That(gyro.GetComponent<GyroStabilizer>().IsOperational,Is.False);
        var jump=parts.Single(p=>p.Kind==R3PartKind.JumpDrive);jump.transform.SetParent(fixtures.transform,true);
        Assert.That(jump.GetComponent<JumpDrive>().TryActivate(),Is.Not.EqualTo(JumpDriveResult.Activated));
        var model=v.GetComponent<R3VehicleAssembly>().Model;v.GetComponent<R3VehicleAssembly>().Capture();
        model.Award(1000);Assert.That(model.Repair(booster.Cell),Is.True);Assert.That(model.Repair(gyro.Cell),Is.True);
        v.gameObject.SetActive(false);var rebuilt=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(20,.7f,1000),Quaternion.identity);
        rebuilt.transform.SetParent(fixtures.transform,true);rebuilt.Controller.SetGameplayInputEnabled(true);
        Assert.That(Action(rebuilt,"TryBoost"),Is.True);Assert.That(rebuilt.GetComponentInChildren<GyroStabilizer>().IsOperational,Is.True);yield return null;
    }
    [UnityTest] public IEnumerator PhysicalJumpHasMassSensitiveImpulseOnEveryStarter()
    {
        float previous=10;
        foreach(var kind in new[]{R3VehicleKind.Light,R3VehicleKind.Standard,R3VehicleKind.Heavy})
        {
            var v=Build(kind,0,R3PartKind.JumpDrive,R3PartKind.Gyroscope);yield return Seconds(.5f);
            var b=v.GetComponent<Rigidbody>();float y=b.position.y;Assert.That(Action(v,"TryJump"),Is.True);
            float speed=b.linearVelocity.y;Assert.That(speed,Is.InRange(1,5.21f));Assert.That(speed,Is.LessThan(previous));previous=speed;
            float peak=y;float end=Time.time+1;while(Time.time<end){peak=Mathf.Max(peak,b.position.y);yield return null;}
            Debug.Log($"MOV002 actual {kind} mass={b.mass:F0} initialVy={speed:F3} height={peak-y:F3}");
            Assert.That(peak-y,Is.GreaterThan(.10f));Assert.That(Vector3.Angle(b.rotation*Vector3.up,Vector3.up),Is.LessThan(10));v.gameObject.SetActive(false);
        }
    }
    [UnityTest] public IEnumerator DestroyedStructuralBridgeStopsMountedDeviceBeforeNextForce()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        // Keep the device connected only through the bridge on the current
        // starter, including when the old fixed cells are already occupied.
        var occupied=model.Parts.Select(p=>p.Cell).ToHashSet();
        var cells=model.Parts.Where(p=>p.Kind==R3PartKind.Armor||p.Kind==R3PartKind.Core)
            .Select(p=>p.Cell+Vector3Int.up).Distinct()
            .Where(c=>!occupied.Contains(c)&&!occupied.Contains(c+Vector3Int.up)&&
                R3VehicleModel.Neighbours.All(d=>d==Vector3Int.down||!occupied.Contains(c+Vector3Int.up+d))&&
                model.PlacementError(c,R3PartKind.Armor,true)==null)
            .OrderBy(c=>(c-new Vector3Int(0,2,0)).sqrMagnitude).ToArray();
        Assert.That(cells,Is.Not.Empty,"Fixture needs a free, isolated structural bridge.");
        var bridgeCell=cells[0];var boosterCell=bridgeCell+Vector3Int.up;
        model.Parts.Add(new R3PartState(bridgeCell,R3PartKind.Armor));
        Assert.That(model.PlacementError(boosterCell,R3PartKind.Booster,true),Is.Null);
        model.Parts.Add(new R3PartState(boosterCell,R3PartKind.Booster));
        var v=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,.7f,1000),Quaternion.identity);
        v.transform.SetParent(fixtures.transform,true);v.Controller.SetGameplayInputEnabled(true);yield return Seconds(.35f);
        Assert.That(Action(v,"TryBoost"),Is.True);var assembly=v.GetComponent<R3VehicleAssembly>();
        // Prevent the graph's periodic Update from accidentally masking a missing pre-force refresh.
        var nextCheck=v.Structure.GetType().GetField("nextCheckTime",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That(nextCheck,Is.Not.Null);nextCheck.SetValue(v.Structure,Time.time+10);
        var bridge=assembly.Parts.Single(p=>p.Cell==bridgeCell);var boost=assembly.Parts.Single(p=>p.Kind==R3PartKind.Booster).GetComponent<VehicleBooster>();
        bridge.Damageable.ApplyDamage(10000,bridge.transform.position,Vector3.zero);
        yield return FixedStep();
        Assert.That(boost.IsOperational,Is.False);Assert.That(boost.IsBoosting,Is.False);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
