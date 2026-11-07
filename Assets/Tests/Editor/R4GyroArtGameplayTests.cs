using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.MOV004GyroscopicStabilizer;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4GyroArtGameplayTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;}
    [UnityTest] public IEnumerator ModelFollowsRealGyroOperationalStateWithoutAddingColliders()
    {
        var fixtures=new GameObject("Gyro fixture");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixtures.transform);
        floor.transform.position=new Vector3(0,-.5f,1000);floor.transform.localScale=new Vector3(80,1,80);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Parts.Add(new R3PartState(new Vector3Int(-1,1,0),R3PartKind.Gyroscope));
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),Object.FindFirstObjectByType<R3VisualPalette>(),new Vector3(0,.7f,1000),Quaternion.identity);
        vehicle.transform.SetParent(fixtures.transform,true);vehicle.Controller.SetGameplayInputEnabled(true);
        var part=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.Gyroscope);
        var gyro=part.GetComponent<GyroStabilizer>();Assert.That(gyro,Is.Not.Null);
        var art=part.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="R4M_Gyroscope_ROOT");Assert.That(art,Is.Not.Null);
        var rotor=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="R4M_Gyroscope_Rotor");
        var socket=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Socket_Mount");
        Assert.That(Vector3.Distance(socket.position,part.transform.TransformPoint(Vector3.up*.5f)),Is.LessThan(.001f));
        Assert.That(Vector3.Dot(-socket.forward,part.transform.up),Is.GreaterThan(.9f),"Imported mount normal must follow the block.");
        Assert.That(art.GetComponentsInChildren<Collider>(true),Is.Empty);
        Assert.That(part.transform.Find("MOV • Gyroscope").GetComponent<Collider>().enabled,Is.True);
        var start=rotor.localRotation;yield return MOVSeriesDeviceTests.Seconds(.2f);
        Assert.That(Quaternion.Angle(start,rotor.localRotation),Is.GreaterThan(1));
        gyro.SetOperational(false);yield return null;var stopped=rotor.localRotation;yield return MOVSeriesDeviceTests.Seconds(.2f);
        Assert.That(Quaternion.Angle(stopped,rotor.localRotation),Is.LessThan(.01f),"Destroyed gyro must stop its visual rotor.");
        Object.Destroy(fixtures);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
