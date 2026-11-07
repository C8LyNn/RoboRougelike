using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.MOV007;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4HoverArtGameplayTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;}
    [UnityTest] public IEnumerator HoverAppearanceShowsRealLiftAndKeepsOriginalPhysics()
    {
        var fixtures=new GameObject("Hover art fixture");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixtures.transform);floor.transform.position=new Vector3(0,-.5f,1000);floor.transform.localScale=new Vector3(80,1,80);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Parts.Add(new R3PartState(new Vector3Int(-1,1,0),R3PartKind.HoverEngine));
        model.Parts.Add(new R3PartState(new Vector3Int(1,1,0),R3PartKind.HoverEngine));model.SelectedDrive=R3DriveKind.Hover;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),Object.FindFirstObjectByType<R3VisualPalette>(),new Vector3(0,.7f,1000),Quaternion.identity);
        vehicle.transform.SetParent(fixtures.transform,true);vehicle.Controller.SetGameplayInputEnabled(true);
        var part=vehicle.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.HoverEngine);
        var pod=part.GetComponent<HoverLiftPod>();Assert.That(pod,Is.Not.Null);
        var art=part.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="R4M_HoverEngine_ROOT");Assert.That(art,Is.Not.Null);
        var socket=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Socket_Mount");
        var nozzle=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Thruster_Nozzle");
        var glow=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Hover_ThrustGlow");
        Assert.That(Vector3.Distance(socket.position,part.transform.TransformPoint(Vector3.up*.5f)),Is.LessThan(.001f));
        Assert.That(Vector3.Dot(-nozzle.forward,part.transform.up),Is.LessThan(-.9f),"Imported nozzle empty uses negative forward for exhaust.");
        Assert.That(Vector3.Dot(nozzle.position-socket.position,part.transform.up),Is.LessThan(0),"Nozzle sits below the mount.");
        Assert.That(art.GetComponentsInChildren<Collider>(true),Is.Empty);
        Assert.That(part.transform.Find("MOV • HoverEngine").GetComponent<Collider>().enabled,Is.True);
        float deadline=Time.realtimeSinceStartup+8;
        while(pod.LastLiftForceNewtons<=0&&Time.realtimeSinceStartup<deadline)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(pod.LastLiftForceNewtons,Is.GreaterThan(0),"The real pod must be applying lift.");yield return null;
        Assert.That(glow.gameObject.activeSelf,Is.True,"Glow follows real lift, not just selected mode.");
        var adapter=vehicle.GetComponent<RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter>();Assert.That(adapter.SetDriveMode(R3DriveKind.Wheels),Is.True);
        yield return MOVSeriesDeviceTests.FixedStep();yield return null;
        Assert.That(pod.LastLiftForceNewtons,Is.Zero);Assert.That(glow.gameObject.activeSelf,Is.False);
        Object.Destroy(fixtures);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
