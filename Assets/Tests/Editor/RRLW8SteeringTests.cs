using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RoboRougelike.Mechanics.IndependentSteering;
using RoboRougelike.Mechanics.EMP;

public class RRLW8SteeringTests
{
    R1RunController run;R3PartKind kind;R3MountedPart module;MonoBehaviour binding;
    IndependentSteeringController source;Rigidbody body;R6VehicleDrive drive;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;
    }
    IEnumerator Build(R3VehicleKind chassis=R3VehicleKind.Light,bool withSuspension=false)
    {
        Assert.That(run.SelectVehicle(chassis),Is.True);Assert.That(run.OpenSandboxGarage(),Is.True);
        Assert.That(run.GarageBuy(new Vector3Int(0,chassis==R3VehicleKind.Light?1:2,-2),kind),Is.True);
        if(withSuspension)foreach(var cell in MOV009ModelTests.Cells(chassis))Assert.That(run.GarageBuy(cell,R3PartKind.Suspension),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        module=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==kind);
        binding=module.GetComponents<MonoBehaviour>().SingleOrDefault(c=>c.GetType().Name=="RRLIndependentSteeringBinding");Assert.That(binding,Is.Not.Null);
        source=module.GetComponent<IndependentSteeringController>();Assert.That(source,Is.Not.Null);
        body=run.Player.GetComponent<Rigidbody>();drive=run.Player.GetComponent<R6VehicleDrive>();
        run.Player.Controller.SetGameplayInputEnabled(true);run.Player.Controller.enabled=false;
        // Planar force fixture isolates existing R6 thrust/yaw from practice terrain. Native tests use normal suspension.
        if(!withSuspension){body.useGravity=false;body.constraints=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;}
    }
    bool SetMode(SteeringMode mode)=>(bool)binding.GetType().GetMethod("SetMode").Invoke(binding,new object[]{mode});
    bool Gui(Event key)=>(bool)binding.GetType().GetMethod("HandleGuiInput").Invoke(binding,new object[]{key});
    bool Supported=>(bool)binding.GetType().GetProperty("GeometrySupported").GetValue(binding);
    IEnumerator Drive(float seconds)
    {
        float end=Time.time+seconds;
        while(Time.time<end){drive.Step(1,1,10,5,105);yield return MOVSeriesDeviceTests.FixedStep();}
    }
    [UnityTest] public IEnumerator OwnedLightModuleBindsFourActualCornersWithoutDemoDriveOrExtraBody()
    {
        Assert.That(Enum.TryParse("IndependentSteering",out kind),Is.True,"MOV011 must be an owned physical garage module");yield return Build();
        Assert.That(Supported,Is.True);Assert.That(source.VehicleBody,Is.SameAs(body));
        Assert.That(run.Player.GetComponentsInChildren<Rigidbody>().Length,Is.EqualTo(1));
        Assert.That(run.Player.GetComponentsInChildren<MonoBehaviour>().Any(c=>c.GetType().Name=="SteeringDemoDrive"),Is.False);
        var wheels=run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray();
        Assert.That(wheels.Length,Is.EqualTo(4));foreach(var wheel in wheels)Assert.That(wheel.GetComponent<SteeringWheelModule>(),Is.Not.Null);
        var payload=wheels[0].transform.Find("Koło");var collider=payload.GetComponent<Collider>();var physicalPose=payload.localRotation;
        drive.Step(1,1,10,5,105);
        var expected=SteeringMath.Solve(source.CurrentSolution.frontCenterDegrees,source.CurrentSolution.rearCenterDegrees,new SteeringGeometry{wheelbaseMeters=1.1f,trackMeters=2.2f});
        Assert.That(source.CurrentSolution.curvatureInverseMeters,Is.EqualTo(expected.curvatureInverseMeters).Within(.0001));
        Assert.That(source.CurrentSolution.frontRightDegrees,Is.GreaterThan(source.CurrentSolution.frontLeftDegrees));
        Assert.That(collider.enabled,Is.True);Assert.That(payload.localRotation,Is.EqualTo(physicalPose));Assert.That(payload.GetComponent<MeshRenderer>().enabled,Is.False);
        Assert.That(wheels[0].transform.Find("MOV011 visual yaw").GetComponentsInChildren<Collider>(),Is.Empty);
        Assert.That(R3VehicleModel.OccupiedCells(run.VehicleModel.Parts.Single(p=>p.Id==module.Id)).Count(),Is.EqualTo(4));
    }
    [UnityTest] public IEnumerator CrabAndCounterPhaseChangeActualR6TravelThroughTheSameForcePath()
    {
        Assert.That(Enum.TryParse("IndependentSteering",out kind),Is.True);yield return Build();Assert.That(SetMode(SteeringMode.Crab),Is.True);
        Vector3 start=body.position;yield return Drive(.8f);
        Assert.That(body.position.x-start.x,Is.GreaterThan(.12f));Assert.That(body.position.z-start.z,Is.GreaterThan(.2f));
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(0,body.rotation.eulerAngles.y)),Is.LessThan(3));Assert.That(Mathf.Abs(source.CurrentSolution.curvatureInverseMeters),Is.LessThan(.001));
        Assert.That(SetMode(SteeringMode.CounterPhase),Is.True);yield return Drive(.6f);
        Assert.That(source.CurrentSolution.frontCenterDegrees,Is.GreaterThan(0));Assert.That(source.CurrentSolution.rearCenterDegrees,Is.LessThan(0));
        Assert.That(body.angularVelocity.y,Is.GreaterThan(.1f));
        Assert.That(SetMode(SteeringMode.RearOnly),Is.True);yield return Drive(.4f);
        Assert.That(source.CurrentSolution.frontCenterDegrees,Is.Zero.Within(.001));Assert.That(source.CurrentSolution.rearCenterDegrees,Is.LessThan(0));Assert.That(body.angularVelocity.y,Is.GreaterThan(.1f));
    }
    [UnityTest] public IEnumerator ActualLostFrontAxleAndEmpCannotRemainSteeringPowerOrEraseMomentum()
    {
        Assert.That(Enum.TryParse("IndependentSteering",out kind),Is.True);yield return Build();Assert.That(SetMode(SteeringMode.CounterPhase),Is.True);
        yield return Drive(.3f);var wheels=run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.Wheel&&p.Cell.z>0).ToArray();
        foreach(var wheel in wheels)wheel.Damageable.ApplyDamage(100000,wheel.transform.position,Vector3.up);
        yield return Drive(.3f);Assert.That(source.CurrentSolution.frontCenterDegrees,Is.Zero.Within(.001));Assert.That(source.CurrentSolution.rearCenterDegrees,Is.LessThan(0));
        var velocity=body.linearVelocity;var spin=body.angularVelocity;module.GetComponent<EmpReceiver>().ApplyEmp(1);
        Assert.That(body.linearVelocity,Is.EqualTo(velocity));Assert.That(body.angularVelocity,Is.EqualTo(spin));
        drive.Step(1,1,10,5,105);Assert.That(source.CurrentSolution.curvatureInverseMeters,Is.Zero.Within(.001));
        Assert.That(source.GetWheel(SteeringWheelPosition.RearLeft).CurrentAngleDegrees,Is.Zero.Within(.001));
    }
    [UnityTest] public IEnumerator CrabWorksWithFourActualMov009SupportsAndUnconstrainedGravityBody()
    {
        Assert.That(Enum.TryParse("IndependentSteering",out kind),Is.True);yield return Build(R3VehicleKind.Light,true);
        var suspension=run.Player.GetComponent<RoboRougelike.Mechanics.MOV009.ActiveSuspensionController>();
        Assert.That(suspension,Is.Not.Null);Assert.That(suspension.RegisteredModuleCount,Is.EqualTo(4));
        Assert.That(body.useGravity,Is.True);Assert.That(body.constraints,Is.EqualTo(RigidbodyConstraints.None));Assert.That(drive.SuspensionPhysics,Is.True);
        float until=Time.time+.8f;while(Time.time<until)yield return null;
        Assert.That(SetMode(SteeringMode.Crab),Is.True);var start=body.position;float yaw=body.rotation.eulerAngles.y;yield return Drive(.8f);
        Assert.That(body.position.x-start.x,Is.GreaterThan(.1f));Assert.That(body.position.z-start.z,Is.GreaterThan(.2f));
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(yaw,body.rotation.eulerAngles.y)),Is.LessThan(8));Assert.That(suspension.LastTotalLiftNewtons,Is.GreaterThan(0));
        Assert.That(Vector3.Angle(run.Player.transform.up,Vector3.up),Is.LessThan(15));
    }
    [UnityTest] public IEnumerator DestroyedControllerKeepsModeIdentityAndRotationThroughPaidRepairAndCopy()
    {
        Assert.That(Enum.TryParse("IndependentSteering",out kind),Is.True);yield return Build();Assert.That(SetMode(SteeringMode.Crab),Is.True);
        string id=module.Id;var assembly=run.Player.GetComponent<R3VehicleAssembly>();module.Damageable.ApplyDamage(100000,module.transform.position,Vector3.up);assembly.Capture();
        var copy=run.VehicleModel.DeepCopy();var state=copy.Parts.Single(p=>p.Id==id);Assert.That(state.Health,Is.Zero);
        int scrap=copy.Scrap;Assert.That(copy.Repair(state.Cell),Is.True);Assert.That(copy.Scrap,Is.LessThan(scrap));
        Assert.That(state.GetType().GetField("SteeringMode").GetValue(state),Is.EqualTo(SteeringMode.Crab));
        Assert.That(R3VehicleModel.OccupiedCells(state.Cell,kind,new Vector3Int(1,0,1)).Distinct().Count(),Is.EqualTo(4));
        var restored=R3VehicleAssembly.Build(copy,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        Assert.That(restored.GetComponentsInChildren<IndependentSteeringController>().Single().Mode,Is.EqualTo(SteeringMode.Crab));yield return null;
    }
    [UnityTest] public IEnumerator TwelveWheelLayoutAndAlternateDriveKeepFallbackWithoutCompetingSolver()
    {
        Assert.That(Enum.TryParse("IndependentSteering",out kind),Is.True);yield return Build(R3VehicleKind.Standard);
        Assert.That(Supported,Is.False);Assert.That(SetMode(SteeringMode.Crab),Is.False);yield return Drive(.3f);
        Assert.That(source.CurrentSolution.curvatureInverseMeters,Is.Zero);Assert.That(body.angularVelocity.y,Is.GreaterThan(.1f),"Standard R6 fallback remains playable");
        run.VehicleModel.SelectedDrive=R3DriveKind.Hover;drive.Step(1,1,10,5,105);Assert.That(source.CurrentSolution.curvatureInverseMeters,Is.Zero);
        Assert.That(run.Player.GetComponentsInChildren<Rigidbody>().Length,Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator RotatedOrNonrectangularWheelLayoutCannotSilentlyChooseVirtualCorners()
    {
        Assert.That(Enum.TryParse("IndependentSteering",out kind),Is.True);yield return Build();
        var model=run.VehicleModel.DeepCopy();var wheel=model.Parts.First(p=>p.Kind==R3PartKind.Wheel);wheel.RotationQuarterTurns=new Vector3Int(1,0,0);
        var rotated=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        var rotatedBinding=rotated.GetComponentsInChildren<MonoBehaviour>().Single(c=>c.GetType().Name=="RRLIndependentSteeringBinding");
        Assert.That(rotatedBinding.GetType().GetProperty("GeometrySupported").GetValue(rotatedBinding),Is.False);
        wheel.RotationQuarterTurns=Vector3Int.zero;wheel.Cell+=Vector3Int.forward;
        var skewed=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(150,4,100),Quaternion.identity);
        var skewedBinding=skewed.GetComponentsInChildren<MonoBehaviour>().Single(c=>c.GetType().Name=="RRLIndependentSteeringBinding");
        Assert.That(skewedBinding.GetType().GetProperty("GeometrySupported").GetValue(skewedBinding),Is.False);yield return null;
    }
    [UnityTest] public IEnumerator PlayerGuiMCyclesOwnedModesAndRejectsPauseAndControllerLoss()
    {
        Assert.That(Enum.TryParse("IndependentSteering",out kind),Is.True);yield return Build();
        var key=new Event{type=EventType.KeyDown,keyCode=KeyCode.None,character='m'};
        run.TogglePause();Assert.That(Gui(key),Is.False);Assert.That(source.Mode,Is.EqualTo(SteeringMode.FrontOnly));run.TogglePause();
        foreach(var mode in new[]{SteeringMode.RearOnly,SteeringMode.CounterPhase,SteeringMode.Crab,SteeringMode.FrontOnly})
        {Assert.That(Gui(key),Is.True);Assert.That(source.Mode,Is.EqualTo(mode));Assert.That(Gui(key),Is.False,"Raw and GUI delivery in one frame must not double-cycle");yield return null;}
        module.Damageable.ApplyDamage(100000,module.transform.position,Vector3.up);Assert.That(Gui(key),Is.False);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
