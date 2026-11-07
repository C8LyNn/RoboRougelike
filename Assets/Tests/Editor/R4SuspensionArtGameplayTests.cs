using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.MOV009Integration;
using RoboRougelike.Mechanics.MOV009;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4SuspensionArtGameplayTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;}
    [UnityTest] public IEnumerator ModelPreservesTheExistingPhysicalSupportAndMount()
    {
        var fixtures=new GameObject("Suspension art fixture");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixtures.transform);floor.transform.position=new Vector3(0,-.5f,1000);floor.transform.localScale=new Vector3(80,1,80);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Parts.Add(new R3PartState(new Vector3Int(-1,1,0),R3PartKind.Suspension));
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),Object.FindFirstObjectByType<R3VisualPalette>(),new Vector3(0,.7f,1000),Quaternion.identity);
        vehicle.transform.SetParent(fixtures.transform,true);vehicle.Controller.SetGameplayInputEnabled(true);
        var part=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.Suspension);
        var adapter=vehicle.GetComponent<MOV009VehicleAdapter>();Assert.That(adapter,Is.Not.Null);
        Assert.That(part.GetComponentsInChildren<ActiveSuspensionModule>(true).Length,Is.EqualTo(1));
        var art=part.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="R4M_Suspension_ROOT");Assert.That(art,Is.Not.Null);
        var socket=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Socket_Mount");
        Assert.That(Vector3.Distance(socket.position,part.transform.TransformPoint(Vector3.up*.5f)),Is.LessThan(.001f),"The mount belongs on the top face, not the center.");
        Assert.That(Vector3.Dot(-socket.forward,part.transform.up),Is.GreaterThan(.9f));
        Assert.That(art.GetComponentsInChildren<Collider>(true),Is.Empty);
        Assert.That(part.GetComponent<Collider>().enabled,Is.True,"The physical block collider remains active.");
        Assert.That(part.transform.Find("Tłok aktywnego zawieszenia").GetComponent<Renderer>().enabled,Is.False);
        yield return MOVSeriesDeviceTests.FixedStep();Assert.That(adapter.OperationalSupports,Is.EqualTo(1));
        part.Damageable.ApplyDamage(10000,part.transform.position,Vector3.zero);yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(adapter.OperationalSupports,Is.Zero,"The visual model cannot keep a broken support working.");
        Object.Destroy(fixtures);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
