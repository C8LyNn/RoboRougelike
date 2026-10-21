using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RoboRougelike.MOVSeries;
using RoboRougelike.Mechanics.MOV005OmniDrive;
using RoboRougelike.Mechanics.MOV007;
using RoboRougelike.Mechanics.MOV008;
using RoboRougelike.Mechanics.MOV009;

public class MOVSeriesDriveTests
{
    GameObject fixtures;R3VisualPalette palette;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;
        fixtures=new GameObject("Original drive integration");palette=UnityEngine.Object.FindFirstObjectByType<R3VisualPalette>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Ground";floor.transform.SetParent(fixtures.transform);
        floor.transform.position=new Vector3(0,-.5f,1000);floor.transform.localScale=new Vector3(200,1,200);
    }
    R1VehicleState Build(R3VehicleKind kind,float x,R3PartKind module,bool suspension=false)
    {
        var m=new R3VehicleModel(R3VehicleCatalog.Create(kind));int w=kind==R3VehicleKind.Light?1:kind==R3VehicleKind.Standard?2:3;
        int l=kind==R3VehicleKind.Light?2:kind==R3VehicleKind.Standard?3:4;
        int back=kind==R3VehicleKind.Standard?-4:-l;
        foreach(int side in new[]{-1,1})foreach(int z in new[]{back,l})m.Parts.Add(new R3PartState(new Vector3Int(side*(w+1),0,z),module));
        m.Parts.Add(new R3PartState(new Vector3Int(-1,kind==R3VehicleKind.Light?1:2,0),R3PartKind.Gyroscope));
        m.Parts.Add(new R3PartState(new Vector3Int(1,kind==R3VehicleKind.Light?1:2,0),R3PartKind.Booster));
        if(suspension)
        {
            foreach(int side in new[]{-1,1})foreach(int z in new[]{-l-1,l+1})m.Parts.Add(new R3PartState(new Vector3Int(side,0,z),R3PartKind.Suspension));
            m.SuspensionHeightOffset=.3f;
        }
        var v=R3VehicleAssembly.Build(m,new R1RunStats(),palette,new Vector3(x,.7f,1000),Quaternion.identity);
        v.transform.SetParent(fixtures.transform,true);v.Controller.SetGameplayInputEnabled(true);v.Controller.enabled=false;return v;
    }
    static bool Mode(R1VehicleState v,R3DriveKind mode)
    {
        var a=v.GetComponent<MOVSeriesVehicleAdapter>();var method=a.GetType().GetMethod("SetDriveMode");Assert.That(method,Is.Not.Null);
        return (bool)method.Invoke(a,new object[]{mode});
    }
    static void Command(R1VehicleState v,float forward,float side,float yaw=0)
    {
        var a=v.GetComponent<MOVSeriesVehicleAdapter>();var method=a.GetType().GetMethod("SetDriveInput");Assert.That(method,Is.Not.Null);
        method.Invoke(a,new object[]{new Vector2(side,forward),yaw});
    }
    IEnumerator Drive(R1VehicleState v,float forward,float side,float yaw,float seconds)
    {
        float end=Time.time+seconds;while(Time.time<end){Command(v,forward,side,yaw);yield return MOVSeriesDeviceTests.FixedStep();}
    }
    [UnityTest] public IEnumerator TracksDriveForwardReverseTurnAndShowMassResponseOnAllClasses()
    {
        float previous=100;
        foreach(var kind in new[]{R3VehicleKind.Light,R3VehicleKind.Standard,R3VehicleKind.Heavy})
        {
            var v=Build(kind,0,R3PartKind.CrawlerTrack);Assert.That(Mode(v,R3DriveKind.Tracks),Is.True);yield return MOVSeriesDeviceTests.Seconds(.5f);
            yield return Drive(v,1,0,0,1);var b=v.GetComponent<Rigidbody>();float speed=b.linearVelocity.z;
            Debug.Log($"MOV008 actual {kind} mass={b.mass:F0} speed={speed:F3}");Assert.That(speed,Is.GreaterThan(.4f));Assert.That(speed,Is.LessThan(previous));previous=speed;
            yield return Drive(v,-1,0,0,2);Assert.That(b.linearVelocity.z,Is.LessThan(-.3f));
            yield return Drive(v,0,1,1,1);Assert.That(Mathf.Abs(b.angularVelocity.y),Is.GreaterThan(.1f));
            Assert.That(v.GetComponent<MOVSeriesVehicleAdapter>().TryBoost(),Is.True);v.gameObject.SetActive(false);
        }
    }
    [UnityTest] public IEnumerator OmniStrafesWithIndependentYawAndGyroOnAllClasses()
    {
        foreach(var kind in new[]{R3VehicleKind.Light,R3VehicleKind.Standard,R3VehicleKind.Heavy})
        {
            var v=Build(kind,0,R3PartKind.OmniDrive);Assert.That(Mode(v,R3DriveKind.Omni),Is.True);yield return MOVSeriesDeviceTests.Seconds(.5f);
            float x=v.transform.position.x;yield return Drive(v,0,1,0,1);
            Assert.That(v.transform.position.x,Is.GreaterThan(x+.2f));Assert.That(Mathf.Abs(Mathf.DeltaAngle(0,v.GetComponent<Rigidbody>().rotation.eulerAngles.y)),Is.LessThan(5));
            yield return Drive(v,1,0,.6f,1);Assert.That(Mathf.Abs(v.GetComponent<Rigidbody>().angularVelocity.y),Is.GreaterThan(.15f));
            Assert.That(Vector3.Angle(v.transform.up,Vector3.up),Is.LessThan(15));v.gameObject.SetActive(false);
        }
    }
    [UnityTest] public IEnumerator HoverLiftsAndStrafesOnAllClassesWithoutSuspensionDoubleLift()
    {
        foreach(var kind in new[]{R3VehicleKind.Light,R3VehicleKind.Standard,R3VehicleKind.Heavy})
        {
            var v=Build(kind,0,R3PartKind.HoverEngine,true);Assert.That(Mode(v,R3DriveKind.Hover),Is.True);yield return MOVSeriesDeviceTests.Seconds(2);
            var b=v.GetComponent<Rigidbody>();var hover=v.GetComponent<HoverVehicleController>();var suspension=v.GetComponent<ActiveSuspensionController>();
            Debug.Log($"MOV007 actual {kind} mass={b.mass:F0} height={b.position.y:F3} pods={hover.LastSupportedPodCount}");
            Assert.That(b.position.y,Is.InRange(.85f,1.25f));Assert.That(hover.LastSupportedPodCount,Is.EqualTo(4));
            Assert.That(suspension.SuspensionEnabled,Is.False);Assert.That(suspension.LastTotalLiftNewtons,Is.Zero);
            Assert.That(v.GetComponent<R3VehicleAssembly>().Model.SuspensionEnabled,Is.True);
            float x=b.position.x;yield return Drive(v,0,1,0,1);Assert.That(b.position.x,Is.GreaterThan(x+.2f));
            Assert.That(v.GetComponent<MOVSeriesVehicleAdapter>().TryBoost(),Is.True);yield return Drive(v,1,0,0,.5f);Assert.That(b.linearVelocity.z,Is.GreaterThan(.2f));
            Assert.That(Mode(v,R3DriveKind.Wheels),Is.True);yield return MOVSeriesDeviceTests.FixedStep();Assert.That(hover.hoverModeEnabled,Is.False);Assert.That(suspension.SuspensionEnabled,Is.True);
            yield return MOVSeriesDeviceTests.Seconds(.8f);Assert.That(suspension.LastTotalLiftNewtons,Is.GreaterThan(0));
            v.gameObject.SetActive(false);
        }
    }
    [UnityTest] public IEnumerator LostSelectedOmniNeverFallsBackToWheelsThroughEitherR6Entry()
    {
        var v=Build(R3VehicleKind.Standard,0,R3PartKind.OmniDrive);Assert.That(Mode(v,R3DriveKind.Omni),Is.True);yield return MOVSeriesDeviceTests.Seconds(.5f);
        foreach(var p in v.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.OmniDrive))p.Block.MarkDetached();
        float z=v.transform.position.z;var drive=v.GetComponent<R6VehicleDrive>();
        float end=Time.time+.5f;while(Time.time<end){drive.Step(1,1,10,6,90);drive.StepWorld(Vector3.forward,10);yield return MOVSeriesDeviceTests.FixedStep();}
        Assert.That(Mathf.Abs(v.transform.position.z-z),Is.LessThan(.05f));Assert.That(v.DefeatReason,Is.Not.Null);
        Assert.That(v.GetComponent<R3VehicleAssembly>().Model.SelectedDrive,Is.EqualTo(R3DriveKind.Omni));
    }
    [UnityTest] public IEnumerator TrackSidesAndHoverCapacityRespondToActualPartLoss()
    {
        var tracks=Build(R3VehicleKind.Light,-20,R3PartKind.CrawlerTrack);Assert.That(Mode(tracks,R3DriveKind.Tracks),Is.True);yield return MOVSeriesDeviceTests.Seconds(.5f);
        foreach(var p in tracks.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.CrawlerTrack&&p.Cell.x<0))p.Block.MarkDetached();
        yield return Drive(tracks,1,0,0,.8f);Assert.That(Mathf.Abs(tracks.GetComponent<Rigidbody>().angularVelocity.y),Is.GreaterThan(.1f));
        var hover=Build(R3VehicleKind.Heavy,20,R3PartKind.HoverEngine,true);Assert.That(Mode(hover,R3DriveKind.Hover),Is.True);yield return MOVSeriesDeviceTests.Seconds(2);
        float y=hover.transform.position.y;
        foreach(var p in hover.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.HoverEngine).Take(2))p.Damageable.ApplyDamage(10000,p.transform.position,Vector3.zero);
        yield return MOVSeriesDeviceTests.Seconds(1);Assert.That(hover.transform.position.y,Is.LessThan(y-.1f));Assert.That(hover.GetComponent<ActiveSuspensionController>().LastTotalLiftNewtons,Is.Zero);
    }
    [UnityTest] public IEnumerator DriveProbesRejectActorsAndSameTickDetachedFlags()
    {
        var omni=Build(R3VehicleKind.Light,-20,R3PartKind.OmniDrive);var tracks=Build(R3VehicleKind.Light,0,R3PartKind.CrawlerTrack);var hover=Build(R3VehicleKind.Light,20,R3PartKind.HoverEngine);
        Assert.That(Mode(omni,R3DriveKind.Omni),Is.True);Assert.That(Mode(tracks,R3DriveKind.Tracks),Is.True);Assert.That(Mode(hover,R3DriveKind.Hover),Is.True);yield return MOVSeriesDeviceTests.Seconds(.5f);
        fixtures.transform.Find("Ground").gameObject.SetActive(false);
        var actor=GameObject.CreatePrimitive(PrimitiveType.Cube);actor.transform.SetParent(fixtures.transform);actor.transform.position=new Vector3(0,-.5f,1000);actor.transform.localScale=new Vector3(180,1,180);actor.AddComponent<Rigidbody>().isKinematic=true;Physics.SyncTransforms();
        Assert.That(omni.GetComponentsInChildren<OmniDriveModule>().All(m=>!m.CanDrive(omni.GetComponent<Rigidbody>())),Is.True);
        foreach(var t in tracks.GetComponentsInChildren<CrawlerTrackModule>())Assert.That(t.TryGroundContact(tracks.GetComponent<Rigidbody>(),out _),Is.False);
        yield return MOVSeriesDeviceTests.FixedStep();Assert.That(hover.GetComponent<HoverVehicleController>().LastSupportedPodCount,Is.Zero);
        actor.SetActive(false);
        var staticActor=GameObject.CreatePrimitive(PrimitiveType.Cube);staticActor.transform.SetParent(fixtures.transform);
        staticActor.transform.position=new Vector3(0,-.5f,1000);staticActor.transform.localScale=new Vector3(180,1,180);
        var hp=staticActor.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(100,false);Physics.SyncTransforms();
        Assert.That(omni.GetComponentsInChildren<OmniDriveModule>().All(m=>!m.CanDrive(omni.GetComponent<Rigidbody>())),Is.True,"Static damageables are not terrain.");
        foreach(var t in tracks.GetComponentsInChildren<CrawlerTrackModule>())Assert.That(t.TryGroundContact(tracks.GetComponent<Rigidbody>(),out _),Is.False);
        yield return MOVSeriesDeviceTests.FixedStep();Assert.That(hover.GetComponent<HoverVehicleController>().LastSupportedPodCount,Is.Zero);
        UnityEngine.Object.DestroyImmediate(hp);staticActor.AddComponent<R3Debris>();
        Assert.That(omni.GetComponentsInChildren<OmniDriveModule>().All(m=>!m.CanDrive(omni.GetComponent<Rigidbody>())),Is.True,"Debris without a body is not terrain.");
        foreach(var t in tracks.GetComponentsInChildren<CrawlerTrackModule>())Assert.That(t.TryGroundContact(tracks.GetComponent<Rigidbody>(),out _),Is.False);
        yield return MOVSeriesDeviceTests.FixedStep();Assert.That(hover.GetComponent<HoverVehicleController>().LastSupportedPodCount,Is.Zero);
        foreach(var v in new[]{omni,tracks,hover})foreach(var p in v.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.OmniDrive||p.Kind==R3PartKind.CrawlerTrack||p.Kind==R3PartKind.HoverEngine))p.Block.MarkDetached();
        Assert.That(omni.GetComponentsInChildren<OmniDriveModule>().All(m=>!m.IsOperational),Is.True);
        Assert.That(tracks.GetComponentsInChildren<CrawlerTrackModule>().All(m=>!m.IsAvailableFor(tracks.GetComponent<Rigidbody>())),Is.True);
        Assert.That(hover.GetComponentsInChildren<HoverLiftPod>().All(m=>!m.IsAvailableFor(hover.GetComponent<Rigidbody>())),Is.True);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
