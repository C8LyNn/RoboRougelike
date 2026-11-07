using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.MOVSeries;
using RoboRougelike.Mechanics.MOV005OmniDrive;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4OmniArtGameplayTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;}
    [UnityTest] public IEnumerator OmniArtKeepsPhysicalStrafeAndRollsOnBothSides()
    {
        var fixtures=new GameObject("Omni art fixture");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixtures.transform);floor.transform.position=new Vector3(0,-.5f,1000);floor.transform.localScale=new Vector3(80,1,80);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(1000);
        var wheels=model.Parts.Where(p=>p.Kind==R3PartKind.Wheel).Select(p=>p.Cell).ToArray();
        foreach(var cell in wheels)Assert.That(model.Remove(cell),Is.True);
        foreach(var cell in wheels)Assert.That(model.Buy(cell,R3PartKind.OmniDrive),Is.True);
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),Object.FindFirstObjectByType<R3VisualPalette>(),new Vector3(0,.7f,1000),Quaternion.identity);
        vehicle.transform.SetParent(fixtures.transform,true);vehicle.Controller.SetGameplayInputEnabled(true);vehicle.Controller.enabled=false;
        var parts=vehicle.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.OmniDrive).ToArray();Assert.That(parts.Length,Is.EqualTo(4));
        foreach(var part in parts)
        {
            Assert.That(part.GetComponent<OmniDriveModule>(),Is.Not.Null);
            var art=part.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="R4M_OmniDrive_ROOT");Assert.That(art,Is.Not.Null);
            var socket=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Socket_Mount");
            Assert.That(Vector3.Distance(socket.position,part.transform.TransformPoint(Vector3.up*.5f)),Is.LessThan(.001f));
            Assert.That(art.GetComponentsInChildren<Collider>(true),Is.Empty);
            Assert.That(part.transform.Find("MOV • OmniDrive").GetComponent<Collider>().enabled,Is.True);
        }
        var left=parts.First(p=>p.Cell.x<0);var right=parts.First(p=>p.Cell.x>0);
        var leftMotor=left.GetComponentsInChildren<Transform>(true).First(t=>t.name=="R4M_OmniDrive_MotorPod").GetComponent<Renderer>();
        var rightMotor=right.GetComponentsInChildren<Transform>(true).First(t=>t.name=="R4M_OmniDrive_MotorPod").GetComponent<Renderer>();
        float leftX=left.transform.InverseTransformPoint(leftMotor.bounds.center).x;float rightX=right.transform.InverseTransformPoint(rightMotor.bounds.center).x;
        Assert.That(leftX*rightX,Is.LessThan(0),"Right-hand motor must be mirrored.");
        var adapter=vehicle.GetComponent<MOVSeriesVehicleAdapter>();Assert.That(adapter.SetDriveMode(R3DriveKind.Omni),Is.True);
        yield return MOVSeriesDeviceTests.Seconds(.4f);
        var roller=left.GetComponentsInChildren<Transform>(true).First(t=>t.name=="R4M_OmniDrive_Roller_01");var initial=roller.localRotation;
        var body=vehicle.GetComponent<Rigidbody>();float start=body.position.x;float end=Time.time+1;float angle=0;
        while(Time.time<end){adapter.SetDriveInput(new Vector2(1,0),0);yield return MOVSeriesDeviceTests.FixedStep();angle=Mathf.Max(angle,Quaternion.Angle(initial,roller.localRotation));}
        Assert.That(body.position.x,Is.GreaterThan(start+.2f));Assert.That(angle,Is.GreaterThan(1),"Sideways motion rotates the roller.");
        Object.Destroy(fixtures);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
