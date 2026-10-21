using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.MOVSeries;
using RoboRougelike.Mechanics.Booster;
using RoboRougelike.Mechanics.MOV004GyroscopicStabilizer;

public class MOVSeriesCombinationTests
{
    GameObject fixtures;R3VisualPalette palette;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;
        fixtures=new GameObject("MOV combined real assemblies");palette=UnityEngine.Object.FindFirstObjectByType<R3VisualPalette>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixtures.transform);floor.transform.position=new Vector3(0,-.5f,1000);floor.transform.localScale=new Vector3(180,1,180);
    }
    R1VehicleState Build(R3VehicleKind kind,float x,R3PartKind drive=R3PartKind.Armor,bool noWheels=false)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(kind));int y=kind==R3VehicleKind.Light?1:2;
        if(drive!=R3PartKind.Armor)
        {
            int w=kind==R3VehicleKind.Light?1:kind==R3VehicleKind.Standard?2:3;
            int l=kind==R3VehicleKind.Light?2:kind==R3VehicleKind.Standard?3:4;
            foreach(int side in new[]{-1,1})foreach(int z in new[]{kind==R3VehicleKind.Standard?-4:-l,l})model.Parts.Add(new R3PartState(new Vector3Int(side*(w+1),0,z),drive));
            model.SelectedDrive=drive==R3PartKind.CrawlerTrack?R3DriveKind.Tracks:drive==R3PartKind.OmniDrive?R3DriveKind.Omni:R3DriveKind.Hover;
        }
        model.Parts.Add(new R3PartState(new Vector3Int(-1,y,0),R3PartKind.Booster));
        model.Parts.Add(new R3PartState(new Vector3Int(1,y,0),R3PartKind.Gyroscope));
        model.Parts.Add(new R3PartState(new Vector3Int(-1,y,1),R3PartKind.JumpDrive));
        if(noWheels)foreach(var wheel in model.Parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray())Assert.That(model.Remove(wheel.Cell),Is.True);
        var v=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(x,.7f,1000),Quaternion.identity);
        v.transform.SetParent(fixtures.transform,true);v.Controller.SetGameplayInputEnabled(true);v.Controller.enabled=false;return v;
    }
    [UnityTest] public IEnumerator TiltedJumpBoosterAndGyroApplyTogetherWithoutPoseCorrection()
    {
        var v=Build(R3VehicleKind.Standard,0);yield return MOVSeriesDeviceTests.Seconds(.5f);var b=v.GetComponent<Rigidbody>();
        b.position=new Vector3(0,.9f,1000);b.rotation=Quaternion.Euler(0,0,8);v.transform.SetPositionAndRotation(b.position,b.rotation);Physics.SyncTransforms();
        var a=v.GetComponent<MOVSeriesVehicleAdapter>();Assert.That(a.TryJump(),Is.True);Assert.That(a.TryBoost(),Is.True);
        yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(v.GetComponentInChildren<GyroStabilizer>().LastAngularAcceleration.sqrMagnitude,Is.GreaterThan(.01f));
        Assert.That(v.GetComponentInChildren<VehicleBooster>().IsBoosting,Is.True);Assert.That(b.linearVelocity.y,Is.GreaterThan(1));
        yield return MOVSeriesDeviceTests.Seconds(.6f);Assert.That(b.linearVelocity.z,Is.GreaterThan(.5f));
        Assert.That(Vector3.Angle(b.rotation*Vector3.up,Vector3.up),Is.LessThan(8));Assert.That(b.linearVelocity.magnitude,Is.LessThan(12));
    }
    [UnityTest] public IEnumerator TracksPlusBoosterProduceExtraFiniteImpulseAgainstEqualRealBaseline()
    {
        var baseline=Build(R3VehicleKind.Standard,-15,R3PartKind.CrawlerTrack);var boosted=Build(R3VehicleKind.Standard,15,R3PartKind.CrawlerTrack);
        yield return MOVSeriesDeviceTests.Seconds(.5f);var a=baseline.GetComponent<MOVSeriesVehicleAdapter>();var b=boosted.GetComponent<MOVSeriesVehicleAdapter>();
        float end=Time.time+.6f;while(Time.time<end){a.SetDriveInput(Vector2.up,0);b.SetDriveInput(Vector2.up,0);yield return MOVSeriesDeviceTests.FixedStep();}
        Assert.That(b.TryBoost(),Is.True);end=Time.time+.5f;
        while(Time.time<end){a.SetDriveInput(Vector2.up,0);b.SetDriveInput(Vector2.up,0);yield return MOVSeriesDeviceTests.FixedStep();}
        float plain=baseline.GetComponent<Rigidbody>().linearVelocity.z,extra=boosted.GetComponent<Rigidbody>().linearVelocity.z;
        Debug.Log($"MOV008+001 real Standard plainVz={plain:F3} boostedVz={extra:F3}");
        Assert.That(boosted.GetComponentInChildren<VehicleBooster>().IsBoosting,Is.True);Assert.That(extra,Is.GreaterThan(plain+.5f));Assert.That(extra,Is.LessThan(20.1f));
    }
    [UnityTest] public IEnumerator OmniAndHoverKeepIndependentDriveWhileGyroActivelyCorrectsTilt()
    {
        foreach(var kind in new[]{R3PartKind.OmniDrive,R3PartKind.HoverEngine})
        {
            var v=Build(R3VehicleKind.Standard,0,kind);yield return MOVSeriesDeviceTests.Seconds(1.5f);var b=v.GetComponent<Rigidbody>();var a=v.GetComponent<MOVSeriesVehicleAdapter>();
            b.rotation=Quaternion.Euler(0,0,8);b.position+=Vector3.up*.22f;v.transform.SetPositionAndRotation(b.position,b.rotation);Physics.SyncTransforms();
            a.SetDriveInput(Vector2.right,.4f);yield return MOVSeriesDeviceTests.FixedStep();
            Assert.That(v.GetComponentInChildren<GyroStabilizer>().LastAngularAcceleration.sqrMagnitude,Is.GreaterThan(.01f));
            float x=b.position.x,end=Time.time+.8f;while(Time.time<end){a.SetDriveInput(Vector2.right,.4f);yield return MOVSeriesDeviceTests.FixedStep();}
            Assert.That(b.position.x,Is.GreaterThan(x+.1f));Assert.That(b.angularVelocity.y,Is.GreaterThan(.1f));
            Assert.That(Vector3.Angle(b.rotation*Vector3.up,Vector3.up),Is.LessThan(12));Assert.That(b.linearVelocity.magnitude,Is.LessThan(15));v.gameObject.SetActive(false);
        }
    }
    [UnityTest] public IEnumerator EachNoWheelDriveLosesAllActualModulesThenRepairRebuildRestoresSameOwnedIds()
    {
        int i=0;
        foreach(var kind in new[]{R3PartKind.CrawlerTrack,R3PartKind.OmniDrive,R3PartKind.HoverEngine})
        {
            var v=Build(R3VehicleKind.Light,-50+i*40,kind,true);i++;yield return MOVSeriesDeviceTests.Seconds(.5f);
            Assert.That(v.Modules.ActiveWheelCount,Is.Zero);Assert.That(v.DefeatReason,Is.Null);var assembly=v.GetComponent<R3VehicleAssembly>();
            var modules=assembly.Parts.Where(p=>p.Kind==kind).ToArray();string[] ids=modules.Select(p=>p.Id).OrderBy(s=>s).ToArray();
            foreach(var module in modules)
            {
                Assert.That(module.Damageable.MaxHp,Is.EqualTo(kind==R3PartKind.CrawlerTrack?140:100));
                module.Damageable.ApplyDamage(10000,module.transform.position,Vector3.zero);
            }
            Assert.That(v.GetComponent<MOVSeriesVehicleAdapter>().OperationalCount(kind),Is.Zero);Assert.That(v.DefeatReason,Is.Not.Null);
            assembly.Capture();var model=assembly.Model;int inventory=model.Inventory.Count;model.Award(1000);
            foreach(var p in model.Parts.Where(p=>p.Kind==kind))Assert.That(model.Repair(p.Cell),Is.True);
            v.gameObject.SetActive(false);var fresh=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(-50+(i-1)*40,.7f,1035),Quaternion.identity);
            fresh.transform.SetParent(fixtures.transform,true);fresh.Controller.SetGameplayInputEnabled(true);fresh.Controller.enabled=false;
            Assert.That(fresh.GetComponent<MOVSeriesVehicleAdapter>().OperationalCount(kind),Is.EqualTo(4));Assert.That(fresh.DefeatReason,Is.Null);
            CollectionAssert.AreEqual(ids,fresh.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==kind).Select(p=>p.Id).OrderBy(s=>s).ToArray());Assert.That(model.Inventory.Count,Is.EqualTo(inventory));
            yield return MOVSeriesDeviceTests.Seconds(1.5f);float z=fresh.transform.position.z,end=Time.time+.7f;
            while(Time.time<end){fresh.GetComponent<MOVSeriesVehicleAdapter>().SetDriveInput(Vector2.up,0);yield return MOVSeriesDeviceTests.FixedStep();}
            Assert.That(fresh.transform.position.z,Is.GreaterThan(z+.1f));fresh.gameObject.SetActive(false);
        }
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
