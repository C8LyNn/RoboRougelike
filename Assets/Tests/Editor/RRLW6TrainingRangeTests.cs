using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R6;
using RoboRougelike.Mechanics.EMP;
using RogueMachine.P12PipelineSafe;

public class RRLW6TrainingRangeTests
{
    [UnityTest] public IEnumerator RangeOffersSafeElectronicRocketTargetAndCleansItOnReturn()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);int scrap=run.VehicleModel.Scrap;
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var range=run.GetComponent<R6TestRange>();var target=range.Layout.GetComponentInChildren<P12PipelineSafeEnemyTurret>();
        Assert.That(target,Is.Not.Null,"W6 practice must offer actual enemy electronics and incoming rockets, not only passive allied dummies");
        Assert.That(target.CombatActive,Is.False,"Optional training fire starts off");
        Assert.That(target.GetComponentsInChildren<EmpReceiver>(),Is.Not.Empty);
        Assert.That(target.GetComponentInChildren<RoboRougelike.WeaponIntegration.RRLRocketBinding>(),Is.Not.Null);
        var toggle=typeof(R6TestRange).GetMethod("SetTrainingFire");Assert.That(toggle,Is.Not.Null);toggle.Invoke(range,new object[]{true});Assert.That(target.CombatActive,Is.True);
        toggle.Invoke(range,new object[]{false});Assert.That(target.CombatActive,Is.False);
        Assert.That(run.ReturnFromTestRange(),Is.True);while(run.Transitioning)yield return null;yield return null;
        Assert.That(target==null||!target.gameObject.activeInHierarchy,Is.True);Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
