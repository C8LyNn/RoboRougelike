using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class InitialEnergyRuntimeTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;}
    IEnumerator CheckFirstEncounter(R3VehicleKind kind,float expected)
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(kind),Is.True);
        Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        Assert.That(run.StartNextEncounter(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        float deadline=Time.realtimeSinceStartup+25;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);var bank=run.Player.GetComponent<VehicleEnergyBank>();Assert.That(bank,Is.Not.Null);
        Assert.That(bank.Maximum,Is.EqualTo(expected).Within(.01f));
        Assert.That(bank.Current,Is.EqualTo(bank.Maximum).Within(.01f),"The real initial encounter starts fully charged, including starter batteries.");
    }
    [UnityTest] public IEnumerator LightStartsAtFullEnergy()=>CheckFirstEncounter(R3VehicleKind.Light,500);
    [UnityTest] public IEnumerator StandardStartsAtFullEnergy()=>CheckFirstEncounter(R3VehicleKind.Standard,600);
    [UnityTest] public IEnumerator HeavyStartsAtFullEnergy()=>CheckFirstEncounter(R3VehicleKind.Heavy,800);
    [UnityTest] public IEnumerator RangeBeforeFirstBattleDoesNotConsumeInitialChargeForNewBattery()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Standard),Is.True);
        Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        Assert.That(run.EnterTestRange(),Is.True);
        float deadline=Time.realtimeSinceStartup+30;
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);Assert.That(run.Progress.State,Is.EqualTo(R1RunState.TestDrive));
        var rangeBank=run.Player.GetComponent<VehicleEnergyBank>();
        Assert.That(rangeBank.Current,Is.EqualTo(rangeBank.Maximum).Within(.01f),"The test copy starts fully charged.");
        Assert.That(run.ReturnFromTestRange(),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
        run.VehicleModel.Award(100);
        var batteryCell=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>run.VehicleModel.PlacementError(c,R3PartKind.Battery)==null);
        Assert.That(run.GarageBuy(batteryCell,R3PartKind.Battery),Is.True);
        Assert.That(run.StartNextEncounter(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);
        var bank=run.Player.GetComponent<VehicleEnergyBank>();Assert.That(bank.Maximum,Is.EqualTo(700).Within(.01f));
        Assert.That(bank.Current,Is.EqualTo(bank.Maximum).Within(.01f),"The first real battle must include the battery installed after the test range.");
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
