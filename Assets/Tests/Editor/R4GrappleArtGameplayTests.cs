using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.MOV006MagneticGrapple;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4GrappleArtGameplayTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;}
    [UnityTest] public IEnumerator RealGrappleKeepsItsMuzzleAndRopeWhileUsingSuppliedModel()
    {
        var fixtures=new GameObject("Grapple art fixture");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixtures.transform);floor.transform.position=new Vector3(0,-.5f,1000);floor.transform.localScale=new Vector3(80,1,80);
        var anchor=GameObject.CreatePrimitive(PrimitiveType.Cube);anchor.transform.SetParent(fixtures.transform);anchor.transform.position=new Vector3(0,2,1015);anchor.transform.localScale=new Vector3(6,4,1);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Parts.Add(new R3PartState(new Vector3Int(-1,1,0),R3PartKind.MagneticGrapple));
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),Object.FindFirstObjectByType<R3VisualPalette>(),new Vector3(0,.7f,1000),Quaternion.identity);
        vehicle.transform.SetParent(fixtures.transform,true);vehicle.Controller.SetGameplayInputEnabled(true);
        var part=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.MagneticGrapple);
        var emitter=part.GetComponent<MagneticGrappleEmitter>();Assert.That(emitter,Is.Not.Null);
        var art=part.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="R4M_MagneticGrapple_ROOT");Assert.That(art,Is.Not.Null);
        var socket=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Socket_Mount");
        var muzzle=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Grapple_Muzzle");
        var cable=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="R4M_MagneticGrapple_Cable");
        Assert.That(Vector3.Distance(socket.position,part.transform.TransformPoint(Vector3.up*.5f)),Is.LessThan(.001f));
        Assert.That(Vector3.Dot(-socket.forward,part.transform.up),Is.GreaterThan(.9f));
        Assert.That(Vector3.Dot(muzzle.forward,part.transform.forward),Is.GreaterThan(.9f));
        Assert.That(art.GetComponentsInChildren<Collider>(true),Is.Empty);
        Assert.That(part.transform.Find("MOV • MagneticGrapple").GetComponent<Collider>().enabled,Is.True);
        Assert.That(Vector3.Distance(emitter.Muzzle.position,part.transform.TransformPoint(Vector3.forward*1.05f)),Is.LessThan(.001f));
        yield return MOVSeriesDeviceTests.Seconds(.5f);Assert.That(cable.gameObject.activeSelf,Is.False,"The game already renders the physical rope.");
        var adapter=vehicle.GetComponent<RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter>();var rope=vehicle.GetComponent<MagneticGrappleController>();
        Assert.That(adapter.TryGrapple(Vector3.forward),Is.True,adapter.Feedback);Assert.That(rope.IsAttached,Is.True);
        adapter.ReleaseGrapples();Assert.That(rope.IsAttached,Is.False);Object.Destroy(fixtures);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
