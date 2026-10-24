using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RogueMachine.P12PipelineSafe;

public class B01EnemyGroundingTests
{
    R1RunController run;P12PipelineSafeEnemyTurret enemy;Rigidbody body;R6VehicleDrive drive;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;
        run.SelectVehicle(R3VehicleKind.Light);run.OpenSandboxGarage();run.EnterTestRange();while(run.Transitioning)yield return null;
        enemy=run.GetComponent<R6TestRange>().TrainingTarget;Assert.That(enemy,Is.Not.Null);enemy.enabled=false;
        body=enemy.GetComponent<Rigidbody>();drive=enemy.GetComponent<R6VehicleDrive>();Assert.That(drive,Is.Not.Null);
        // Do not rebuild the wheel cache in the fixture: exercise the actual spawn/configuration path.
        yield return MOVSeriesDeviceTests.FixedStep();
    }
    [UnityTest] public IEnumerator PHY_REG_ActualLegacyEnemyTreadsSupportAiWheelThrust()
    {
        var start=body.position;float end=Time.time+.4f;
        while(Time.time<end){drive.StepWorld(Vector3.forward,5);yield return MOVSeriesDeviceTests.FixedStep();}
        Debug.Log($"PHY actual enemy wheels={drive.Wheels}/{enemy.ActiveWheelCount} contacts={drive.GroundedWheels} gravity={body.useGravity} delta={body.position-start}");
        Assert.That(drive.GroundedWheels,Is.GreaterThan(0));Assert.That(body.position.z-start.z,Is.GreaterThan(.15f));
    }
    [UnityTest] public IEnumerator PHY_REG_EnemyReconfigurationKeepsR6GravityAndActualWheelView()
    {
        enemy.ConfigureMovement(2,10,.5f);yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(body.useGravity,Is.True,"Legacy reconfiguration cannot undo R6 physical ownership");
        Assert.That(body.constraints,Is.EqualTo(RigidbodyConstraints.None));drive.RefreshMass();
        Assert.That(drive.Wheels,Is.EqualTo(enemy.ActiveWheelCount));Assert.That(drive.GroundedWheels,Is.GreaterThan(0));
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
