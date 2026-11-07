using System.Collections;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class InitialEnergyRuntimeTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;}
    [UnityTest] public IEnumerator FirstRealEncounterStartsWithAllInstalledBatteryEnergy()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Standard),Is.True);
        Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        Assert.That(run.StartNextEncounter(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        float deadline=Time.realtimeSinceStartup+25;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);var bank=run.Player.GetComponent<VehicleEnergyBank>();Assert.That(bank,Is.Not.Null);
        Assert.That(bank.Maximum,Is.EqualTo(600).Within(.01f));
        Assert.That(bank.Current,Is.EqualTo(bank.Maximum).Within(.01f),"The real initial encounter starts fully charged, including starter batteries.");
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
