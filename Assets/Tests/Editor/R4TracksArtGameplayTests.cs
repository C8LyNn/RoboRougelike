using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.MOVSeries;
using RoboRougelike.Mechanics.MOV008;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4TracksArtGameplayTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;}
    [UnityTest] public IEnumerator RealTracksDriveAndAnimateWithoutAddingPhysicalColliders()
    {
        var fixtures=new GameObject("Track art fixture");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixtures.transform);floor.transform.position=new Vector3(0,-.5f,1000);floor.transform.localScale=new Vector3(80,1,80);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(1000);
        var wheels=model.Parts.Where(p=>p.Kind==R3PartKind.Wheel).Select(p=>p.Cell).ToArray();
        foreach(var cell in wheels)Assert.That(model.Remove(cell),Is.True);
        foreach(var cell in wheels)Assert.That(model.Buy(cell,R3PartKind.CrawlerTrack),Is.True);
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),Object.FindFirstObjectByType<R3VisualPalette>(),new Vector3(0,.7f,1000),Quaternion.identity);
        vehicle.transform.SetParent(fixtures.transform,true);vehicle.Controller.SetGameplayInputEnabled(true);vehicle.Controller.enabled=false;
        var parts=vehicle.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.CrawlerTrack).ToArray();Assert.That(parts.Length,Is.EqualTo(4));
        foreach(var part in parts)
        {
            Assert.That(part.GetComponent<CrawlerTrackModule>(),Is.Not.Null);
            var art=part.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="R4M_CrawlerTrack_ROOT");Assert.That(art,Is.Not.Null);
            var socket=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Socket_Mount");
            Assert.That(Vector3.Distance(socket.position,part.transform.TransformPoint(Vector3.up*.5f)),Is.LessThan(.001f));
            Assert.That(art.GetComponentsInChildren<Collider>(true),Is.Empty);
            Assert.That(part.transform.Find("MOV • CrawlerTrack").GetComponent<Collider>().enabled,Is.True);
        }
        var left=parts.First(p=>p.Cell.x<0);var link=left.GetComponentsInChildren<Transform>(true).First(t=>t.name=="R4M_CrawlerTrack_Link_01");
        var adapter=vehicle.GetComponent<MOVSeriesVehicleAdapter>();Assert.That(adapter.SetDriveMode(R3DriveKind.Tracks),Is.True);
        yield return MOVSeriesDeviceTests.Seconds(.4f);var initial=link.localPosition;var initialRotation=link.localRotation;
        var body=vehicle.GetComponent<Rigidbody>();float start=body.position.z;float end=Time.time+1;float maxChange=0;
        while(Time.time<end){adapter.SetDriveInput(new Vector2(0,1),0);yield return MOVSeriesDeviceTests.FixedStep();maxChange=Mathf.Max(maxChange,Vector3.Distance(initial,link.localPosition)+Quaternion.Angle(initialRotation,link.localRotation)*.01f);}
        Assert.That(body.position.z,Is.GreaterThan(start+.2f));Assert.That(maxChange,Is.GreaterThan(.001f),"The track links should animate while driving.");
        foreach(var part in parts)
        {
            var socket=part.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Socket_Mount");
            Assert.That(Vector3.Distance(socket.position,part.transform.TransformPoint(Vector3.up*.5f)),Is.LessThan(.001f),"Sampling the animation must not shift the mount.");
        }
        Object.Destroy(fixtures);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
