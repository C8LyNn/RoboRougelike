using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RoboRougelike.MOVSeries;
using RoboRougelike.Mechanics.MOV006MagneticGrapple;
using RoboRougelike.Mechanics.MOV005OmniDrive;
using RoboRougelike.Mechanics.MOV007;
using RogueMachine.P12PipelineSafe;

public class MOVSeriesReviewTests
{
    GameObject fixtures;R3VisualPalette palette;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;
        fixtures=new GameObject("MOV final review regression fixtures");palette=Object.FindFirstObjectByType<R3VisualPalette>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixtures.transform);
        floor.transform.position=new Vector3(0,-.5f,1000);floor.transform.localScale=new Vector3(180,1,180);
    }
    R1VehicleState Build(R3VehicleModel model,float x=0,float z=1000)
    {
        var v=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(x,.7f,z),Quaternion.identity);
        v.transform.SetParent(fixtures.transform,true);v.Controller.SetGameplayInputEnabled(true);v.Controller.enabled=false;return v;
    }
    static R3VehicleModel Model()=>new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
    static R1RunStats UpgradedStats()
    {
        var upgrade=Resources.Load<R1UpgradeDefinition>("R1/Upgrades/mobility");Assert.That(upgrade,Is.Not.Null);
        var stats=new R1RunStats();stats.Apply(upgrade);Assert.That(stats.Mobility,Is.EqualTo(1.15f));return stats;
    }
    static R3VehicleModel DriveModel(R3PartKind kind)
    {
        var model=Model();foreach(int x in new[]{-2,2})foreach(int z in new[]{-2,2})model.Parts.Add(new R3PartState(new Vector3Int(x,0,z),kind));
        model.SelectedDrive=kind==R3PartKind.OmniDrive?R3DriveKind.Omni:kind==R3PartKind.HoverEngine?R3DriveKind.Hover:R3DriveKind.Tracks;return model;
    }
    static float SpeedTarget(R1VehicleState v,R3PartKind kind)
    {
        if(kind==R3PartKind.OmniDrive)return v.GetComponentInChildren<OmniDriveModule>().MaximumSpeed;
        if(kind==R3PartKind.HoverEngine)return v.GetComponent<HoverVehicleController>().forwardSpeedMetersPerSec;
        var controller=v.GetComponent<RoboRougelike.Mechanics.MOV008.CrawlerTrackController>();
        return (float)controller.GetType().GetField("maxForwardSpeed",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(controller);
    }
    [UnityTest] public IEnumerator EarnedR1MobilityModifierSurvivesRebuildAndModeSwitchInEveryAlternativeDrive()
    {
        foreach(var kind in new[]{R3PartKind.OmniDrive,R3PartKind.HoverEngine,R3PartKind.CrawlerTrack})
        {
            var model=DriveModel(kind);var v=Build(model);v.Controller.enabled=true;yield return MOVSeriesDeviceTests.FixedStep();
            float original=SpeedTarget(v,kind);v.Apply(UpgradedStats());yield return MOVSeriesDeviceTests.FixedStep();
            Assert.That(SpeedTarget(v,kind),Is.EqualTo(original*1.15f).Within(.001f),"The existing earned mobility modifier must reach "+kind);
            v.gameObject.SetActive(false);v=Build(model.DeepCopy());v.Apply(UpgradedStats());v.Controller.enabled=true;
            yield return MOVSeriesDeviceTests.FixedStep();Assert.That(SpeedTarget(v,kind),Is.EqualTo(original*1.15f).Within(.001f));
            Assert.That(v.GetComponent<MOVSeriesVehicleAdapter>().SetDriveMode(R3DriveKind.Wheels),Is.True);
            Assert.That(v.Controller.EffectiveForwardSpeed,Is.EqualTo(original*1.15f).Within(.001f));
            v.gameObject.SetActive(false);
        }
    }
    [UnityTest] public IEnumerator UnusedEmitterDestructionKeepsHealthyEmitterRopeAttached()
    {
        foreach(bool detach in new[]{false,true})
        {
        var model=Model();model.Parts.Add(new R3PartState(new Vector3Int(-1,1,0),R3PartKind.MagneticGrapple));
        model.Parts.Add(new R3PartState(new Vector3Int(1,1,0),R3PartKind.MagneticGrapple));var v=Build(model,detach?20:0);
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(fixtures.transform);
        wall.transform.position=new Vector3(detach?20:0,2,1015);wall.transform.localScale=new Vector3(8,4,1);
        yield return MOVSeriesDeviceTests.Seconds(.4f);
        var a=v.GetComponent<MOVSeriesVehicleAdapter>();var rope=v.GetComponent<MagneticGrappleController>();
        bool fired=a.TryGrapple(Vector3.forward);
        if(!fired)Debug.Log($"MOV review spare initialfire detach={detach} origin={a.GrappleAimOrigin} feedback={a.Feedback} control={a.CanControl} gate={a.Model.GrappleReadyAt} clock={Time.time} hits="+
            string.Join(";",Physics.RaycastAll(a.GrappleAimOrigin,Vector3.forward,40).OrderBy(h=>h.distance).Select(h=>$"{h.collider.name}/{h.distance:F3}/body={h.rigidbody?.name}/debris={h.collider.GetComponentInParent<R3Debris>()!=null}")));
        Assert.That(fired,Is.True);var active=rope.ActiveEmitter;
        var unused=v.GetComponentsInChildren<MagneticGrappleEmitter>().Single(e=>e!=active);
        float gate=a.Model.GrappleReadyAt;
        if(detach){unused.GetComponent<R3MountedPart>().Block.MarkDetached();unused.transform.SetParent(fixtures.transform,true);}
        else unused.GetComponent<R3MountedPart>().Damageable.ApplyDamage(10000,unused.transform.position,Vector3.zero);
        Assert.That(rope.IsAttached,Is.True,"Damage to another emitter must not release a healthy active emitter.");
        Assert.That(a.Model.GrappleReadyAt,Is.EqualTo(gate),"Losing a spare must not start another shared cooldown.");
        yield return MOVSeriesDeviceTests.FixedStep();Assert.That(rope.IsAttached,Is.True);Assert.That(rope.ActiveEmitter,Is.SameAs(active));
        active.GetComponent<R3MountedPart>().Damageable.ApplyDamage(10000,active.transform.position,Vector3.zero);
        Assert.That(rope.IsAttached,Is.False,"Damage to the actual active emitter still cancels immediately.");
        v.gameObject.SetActive(false);
        }
    }
    [UnityTest] public IEnumerator TargetBridgeLossRefreshesPlainR6GraphBeforeNextRopeForce()
    {
        var sourceModel=Model();sourceModel.Parts.Add(new R3PartState(new Vector3Int(-1,1,0),R3PartKind.MagneticGrapple));
        var source=Build(sourceModel);var targetModel=Model();
        targetModel.Parts.Add(new R3PartState(new Vector3Int(0,2,0),R3PartKind.Armor));
        targetModel.Parts.Add(new R3PartState(new Vector3Int(0,3,0),R3PartKind.Armor));var target=Build(targetModel,0,1012);
        Assert.That(target.GetComponent<MOVSeriesVehicleAdapter>(),Is.Null,"Plain R6 target deliberately has no source adapter.");
        yield return MOVSeriesDeviceTests.Seconds(.4f);
        var armor=target.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Cell==new Vector3Int(0,3,0));
        var a=source.GetComponent<MOVSeriesVehicleAdapter>();var rope=source.GetComponent<MagneticGrappleController>();
        Assert.That(a.TryGrapple(armor.transform.position-a.GrappleAimOrigin),Is.True);Assert.That(rope.AttachedRigidbody,Is.SameAs(target.GetComponent<Rigidbody>()));
        var next=target.Structure.GetType().GetField("nextCheckTime",BindingFlags.NonPublic|BindingFlags.Instance);
        Assert.That(next,Is.Not.Null);next.SetValue(target.Structure,Time.time+10);
        var bridge=target.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Cell==new Vector3Int(0,2,0));
        bridge.Damageable.ApplyDamage(10000,bridge.transform.position,Vector3.zero);
        yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(rope.IsAttached,Is.False,"A target disconnected from its own core must not receive another rope force.");
    }
    [UnityTest] public IEnumerator PlainP12TargetMarkedDetachedRejectsRopeBeforePhysicalReparent()
    {
        var model=Model();model.Parts.Add(new R3PartState(new Vector3Int(-1,1,0),R3PartKind.MagneticGrapple));
        var source=Build(model);var target=Build(Model(),0,1012);yield return MOVSeriesDeviceTests.Seconds(.4f);
        var a=source.GetComponent<MOVSeriesVehicleAdapter>();var rope=source.GetComponent<MagneticGrappleController>();
        Assert.That(a.TryGrapple(target.transform.position-a.GrappleAimOrigin),Is.True);
        var collider=(Collider)rope.GetType().GetField("anchorCollider",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(rope);
        var block=collider.GetComponentInParent<P12_PipelineSafePlayerBlock>();Assert.That(block,Is.Not.Null);
        Object.Destroy(block.GetComponent<R3MountedPart>());yield return null;
        Assert.That(block.GetComponent<R3MountedPart>(),Is.Null);block.MarkDetached();
        Assert.That(block.GetComponentInParent<Rigidbody>(),Is.SameAs(target.GetComponent<Rigidbody>()),"The detached flag deliberately precedes reparent.");
        yield return MOVSeriesDeviceTests.FixedStep();Assert.That(rope.IsAttached,Is.False);
    }
    [UnityTest] public IEnumerator ExistingDriveCommandSpeedAndTurnLimitsReachAllThreeAlternativeControllers()
    {
        foreach(var kind in new[]{R3PartKind.OmniDrive,R3PartKind.HoverEngine,R3PartKind.CrawlerTrack})
        {
            var model=Model();foreach(int x in new[]{-2,2})foreach(int z in new[]{-2,2})model.Parts.Add(new R3PartState(new Vector3Int(x,0,z),kind));
            model.SelectedDrive=kind==R3PartKind.OmniDrive?R3DriveKind.Omni:kind==R3PartKind.HoverEngine?R3DriveKind.Hover:R3DriveKind.Tracks;
            var slow=Build(model,-18);var fast=Build(model.DeepCopy(),18);
            yield return MOVSeriesDeviceTests.Seconds(1.5f);
            float end=Time.time+2;
            while(Time.time<end){slow.GetComponent<R6VehicleDrive>().Step(1,0,2,1,30);fast.GetComponent<R6VehicleDrive>().Step(1,0,4,2,60);yield return MOVSeriesDeviceTests.FixedStep();}
            float slowSpeed=slow.GetComponent<Rigidbody>().linearVelocity.z,fastSpeed=fast.GetComponent<Rigidbody>().linearVelocity.z;
            Debug.Log($"MOV review command {kind} slow={slowSpeed:F3} fast={fastSpeed:F3}");
            Assert.That(slowSpeed,Is.InRange(.5f,2.2f));Assert.That(fastSpeed,Is.GreaterThan(slowSpeed*1.3f));
            // A fresh pair avoids translating while checking independently commanded rotation.
            slow.gameObject.SetActive(false);fast.gameObject.SetActive(false);slow=Build(model,-18);fast=Build(model.DeepCopy(),18);
            yield return MOVSeriesDeviceTests.Seconds(1.5f);end=Time.time+2;
            while(Time.time<end)
            {
                slow.GetComponent<MOVSeriesVehicleAdapter>().IndependentYaw=1;fast.GetComponent<MOVSeriesVehicleAdapter>().IndependentYaw=1;
                slow.GetComponent<R6VehicleDrive>().Step(0,1,2,1,30);fast.GetComponent<R6VehicleDrive>().Step(0,1,4,2,60);
                yield return MOVSeriesDeviceTests.FixedStep();
            }
            float slowYaw=Mathf.Abs(slow.GetComponent<Rigidbody>().angularVelocity.y),fastYaw=Mathf.Abs(fast.GetComponent<Rigidbody>().angularVelocity.y);
            Debug.Log($"MOV review yaw {kind} slow={slowYaw:F3} fast={fastYaw:F3}");
            Assert.That(slowYaw,Is.GreaterThan(.1f));Assert.That(fastYaw,Is.GreaterThan(slowYaw*1.25f));
            slow.gameObject.SetActive(false);fast.gameObject.SetActive(false);
        }
    }
    [UnityTest] public IEnumerator PositiveStepWorldRespectsExplicitSpeedWithoutWheelForces()
    {
        foreach(var kind in new[]{R3PartKind.OmniDrive,R3PartKind.HoverEngine,R3PartKind.CrawlerTrack})
        {
            var model=DriveModel(kind);var v=Build(model);yield return MOVSeriesDeviceTests.Seconds(1.5f);
            float start=v.GetComponent<Rigidbody>().position.z,end=Time.time+2;
            while(Time.time<end){v.GetComponent<R6VehicleDrive>().StepWorld(Vector3.forward,2);yield return MOVSeriesDeviceTests.FixedStep();}
            Assert.That(v.GetComponent<Rigidbody>().position.z,Is.GreaterThan(start+.5f));
            Assert.That(v.GetComponent<Rigidbody>().linearVelocity.z,Is.InRange(.5f,2.2f));v.gameObject.SetActive(false);
        }
    }
    [UnityTest] public IEnumerator ActualDetachedGraphDebrisBlocksWideBlinkSweepAndDestinationWithoutTransaction()
    {
        var sourceModel=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Heavy));
        sourceModel.Parts.Add(new R3PartState(new Vector3Int(-1,2,0),R3PartKind.Blink));var source=Build(sourceModel);
        var targetModel=Model();targetModel.Parts.Add(new R3PartState(new Vector3Int(0,2,0),R3PartKind.Armor));
        targetModel.Parts.Add(new R3PartState(new Vector3Int(0,3,0),R3PartKind.Armor));var target=Build(targetModel,30);
        yield return MOVSeriesDeviceTests.Seconds(.4f);
        var assembly=target.GetComponent<R3VehicleAssembly>();var detached=assembly.Parts.Single(p=>p.Cell==new Vector3Int(0,3,0));
        var bridge=assembly.Parts.Single(p=>p.Cell==new Vector3Int(0,2,0));bridge.Damageable.ApplyDamage(10000,bridge.transform.position,Vector3.zero);target.Structure.EvaluateStructure();
        Assert.That(detached.Block.DetachedFromVehicle,Is.True);var chunk=detached.GetComponentInParent<Rigidbody>();
        Assert.That(chunk,Is.Not.SameAs(target.GetComponent<Rigidbody>()));Assert.That(chunk.GetComponent<R3Debris>(),Is.Not.Null);chunk.isKinematic=true;
        var body=source.GetComponent<Rigidbody>();var a=source.GetComponent<MOVSeriesVehicleAdapter>();Vector3 pose=body.position;
        body.linearVelocity=new Vector3(.1f,0,.2f);body.angularVelocity=Vector3.up*.1f;
        foreach(var obstruction in new[]{new Vector3(2,1.5f,1004),new Vector3(0,1,1008)})
        {
            chunk.position+=obstruction-detached.transform.position;chunk.transform.position=chunk.position;Physics.SyncTransforms();
            Assert.That(a.TryBlink(Vector3.forward),Is.False);Assert.That(body.position,Is.EqualTo(pose));
            Assert.That(body.linearVelocity,Is.EqualTo(new Vector3(.1f,0,.2f)));Assert.That(body.angularVelocity,Is.EqualTo(Vector3.up*.1f));
            Assert.That(source.GetComponent<R3VehicleAssembly>().Model.Parts.Single(p=>p.Kind==R3PartKind.Blink).DeviceReadyAt,Is.Zero);
        }
        chunk.position+=Vector3.right*20;chunk.transform.position=chunk.position;Physics.SyncTransforms();Vector3 debrisPosition=chunk.position;
        Assert.That(a.TryBlink(Vector3.forward),Is.True);Assert.That(chunk.position,Is.EqualTo(debrisPosition));
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
