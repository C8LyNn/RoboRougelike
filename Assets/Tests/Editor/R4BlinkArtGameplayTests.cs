using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.MOV003Blink;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4BlinkArtGameplayTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;}
    [UnityTest] public IEnumerator BlinkEffectOnlyFollowsSuccessfulPhysicalActivation()
    {
        var fixtures=new GameObject("Blink art fixture");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixtures.transform);floor.transform.position=new Vector3(0,-.5f,1000);floor.transform.localScale=new Vector3(80,1,80);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Parts.Add(new R3PartState(new Vector3Int(-1,1,0),R3PartKind.Blink));
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),Object.FindFirstObjectByType<R3VisualPalette>(),new Vector3(0,.7f,1000),Quaternion.identity);
        vehicle.transform.SetParent(fixtures.transform,true);vehicle.Controller.SetGameplayInputEnabled(true);
        var part=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.Blink);
        Assert.That(part.GetComponent<BlinkDrive>(),Is.Not.Null);
        var art=part.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="R4M_Blink_ROOT");Assert.That(art,Is.Not.Null);
        var socket=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Socket_Mount");
        var ring=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Blink_PhaseRing");
        Assert.That(Vector3.Distance(socket.position,part.transform.TransformPoint(Vector3.down*.5f)),Is.LessThan(.001f),"Blink's source mount is the bottom face.");
        Assert.That(art.GetComponentsInChildren<Collider>(true),Is.Empty);
        Assert.That(part.transform.Find("MOV • Blink").GetComponent<Collider>().enabled,Is.True);
        yield return MOVSeriesDeviceTests.Seconds(.7f);Assert.That(ring.gameObject.activeSelf,Is.False);
        var adapter=vehicle.GetComponent<RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter>();var body=vehicle.GetComponent<Rigidbody>();float before=body.position.z;
        Assert.That(adapter.TryBlink(Vector3.forward),Is.True,adapter.Feedback);Assert.That(body.position.z,Is.EqualTo(before+8).Within(.05));
        Assert.That(ring.gameObject.activeSelf,Is.True);yield return MOVSeriesDeviceTests.Seconds(.3f);
        Assert.That(ring.gameObject.activeSelf,Is.False);Assert.That(adapter.TryBlink(Vector3.forward),Is.False);Assert.That(ring.gameObject.activeSelf,Is.False);
        Object.Destroy(fixtures);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
