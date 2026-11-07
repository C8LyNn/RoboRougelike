using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4WheelArtGameplayTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;}
    [UnityTest] public IEnumerator SuppliedWheelVisualRollsWhileExistingPhysicalWheelsDrive()
    {
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.5f,1000);floor.transform.localScale=new Vector3(80,1,80);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,.7f,1000),Quaternion.identity);
        // As in B01GroundedDriveTests, do not let controller zero-input oppose
        // the explicit R6 drive commands used by this physical fixture.
        vehicle.Controller.SetGameplayInputEnabled(true);vehicle.Controller.enabled=false;
        var body=vehicle.GetComponent<Rigidbody>();body.position=new Vector3(0,.65f,1000);body.useGravity=false;
        body.constraints=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
        body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;Physics.SyncTransforms();
        foreach(var b in vehicle.GetComponents<MonoBehaviour>())if(b.GetType().Name=="R6CoreRecovery")b.enabled=false;
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var drive=vehicle.GetComponent<R6VehicleDrive>();
        var wheels=assembly.Parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray();Assert.That(wheels.Length,Is.EqualTo(4));
        foreach(var wheel in wheels)
        {
            var rotor=wheel.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="R4M_Wheel_Rotor");
            Assert.That(rotor,Is.Not.Null,"Every physical wheel has the supplied visual.");
            Assert.That(rotor.GetComponentsInChildren<Collider>(true).Length,Is.Zero,"The art does not add colliders.");
            var original=wheel.transform.Find("Koło");Assert.That(original,Is.Not.Null);
            Assert.That(original.GetComponent<Renderer>().enabled,Is.False);
            Assert.That(original.GetComponent<MeshCollider>().enabled,Is.True,"The existing tire collider must stay active.");
        }
        float deadline=Time.realtimeSinceStartup+8;
        while(drive.GroundedWheels<4&&Time.realtimeSinceStartup<deadline){drive.Step(0,0,10,5,90);yield return MOVSeriesDeviceTests.FixedStep();}
        Assert.That(drive.GroundedWheels,Is.EqualTo(4));
        var actualRotor=wheels[0].GetComponentsInChildren<Transform>(true).First(t=>t.name=="R4M_Wheel_Rotor");
        var rotation=actualRotor.localRotation;float maxAngle=0;float startZ=vehicle.transform.position.z;
        float end=Time.time+1f;
        while(Time.time<end)
        {
            drive.Step(1,0,10,5,90);yield return MOVSeriesDeviceTests.FixedStep();
            maxAngle=Mathf.Max(maxAngle,Quaternion.Angle(rotation,actualRotor.localRotation));
        }
        Assert.That(vehicle.transform.position.z,Is.GreaterThan(startZ+.2f),"The actual R6 vehicle must move.");
        Assert.That(maxAngle,Is.GreaterThan(2),"The imported rotor should visibly roll while driving.");
        Object.Destroy(vehicle.gameObject);Object.Destroy(floor);Object.Destroy(paletteObject);Object.Destroy(material);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
