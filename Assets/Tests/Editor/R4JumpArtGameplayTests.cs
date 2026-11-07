using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.MOV002JumpDrive;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4JumpArtGameplayTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;}
    [UnityTest] public IEnumerator RealJumpTriggersVisualOnceAndKeepsOriginalPhysics()
    {
        var fixtures=new GameObject("Jump art fixture");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixtures.transform);floor.transform.position=new Vector3(0,-.5f,1000);floor.transform.localScale=new Vector3(80,1,80);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        model.Parts.Add(new R3PartState(new Vector3Int(-2,2,-1),R3PartKind.JumpDrive));
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),Object.FindFirstObjectByType<R3VisualPalette>(),new Vector3(0,.7f,1000),Quaternion.identity);
        vehicle.transform.SetParent(fixtures.transform,true);vehicle.Controller.SetGameplayInputEnabled(true);
        var part=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.JumpDrive);
        var jump=part.GetComponent<JumpDrive>();Assert.That(jump,Is.Not.Null);
        var art=part.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="R4M_JumpDrive_ROOT");Assert.That(art,Is.Not.Null);
        var ring=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Jump_BlastRing");
        var socket=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Socket_Mount");
        Assert.That(Vector3.Dot(-socket.forward,part.transform.up),Is.GreaterThan(.9f));
        Assert.That(art.GetComponentsInChildren<Collider>(true),Is.Empty);
        Assert.That(part.transform.Find("MOV • JumpDrive").GetComponent<Collider>().enabled,Is.True);
        yield return MOVSeriesDeviceTests.Seconds(.7f);Assert.That(ring.gameObject.activeSelf,Is.False);
        var body=vehicle.GetComponent<Rigidbody>();body.linearVelocity=new Vector3(1,0,2);
        var adapter=vehicle.GetComponent<RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter>();
        Assert.That(adapter.TryJump(),Is.True);Assert.That(body.linearVelocity.y,Is.GreaterThan(1));
        Assert.That(body.linearVelocity.x,Is.EqualTo(1).Within(.001));Assert.That(ring.gameObject.activeSelf,Is.True);
        Assert.That(adapter.TryJump(),Is.False);yield return MOVSeriesDeviceTests.Seconds(.3f);
        Assert.That(ring.gameObject.activeSelf,Is.False);Object.Destroy(fixtures);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
