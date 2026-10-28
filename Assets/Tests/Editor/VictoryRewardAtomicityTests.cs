using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;

public sealed class VictoryRewardAtomicityTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator SetUp()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        run=R1RunController.Instance;
    }
    [UnityTest] public IEnumerator InvalidRewardCatalogCannotAwardScrapOrRepeatVictory()
    {
        Assert.That(run.StartRun(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        var until=Time.realtimeSinceStartup+20;while((run.Transitioning||run.Encounter.AliveCount==0)&&Time.realtimeSinceStartup<until)yield return null;
        Assert.That(run.Encounter.AliveCount,Is.GreaterThan(0));run.enabled=false;
        var original=run.Definition.Upgrades;run.Definition.Upgrades=original.Take(2).ToArray();
        var wallet=run.VehicleModel.Scrap;var earned=run.EarnedScrap;var completed=run.Progress.CompletedEncounters;
        RRLW2TestFlow.DefeatAll(run);Assert.That(run.Encounter.Cleared,Is.True);
        var update=typeof(R1RunController).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance);
        Exception exception=null;try{update.Invoke(run,null);}catch(Exception e){exception=e;}
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(wallet),"invalid catalog must not pay before victory can be recorded");
        Assert.That(exception,Is.Null,"invalid catalog must not throw during victory");Assert.That(run.EarnedScrap,Is.EqualTo(earned));
        Assert.That(run.Progress.CompletedEncounters,Is.EqualTo(completed));Assert.That(run.Error,Is.Not.Null.And.Not.Empty);
        Assert.That(()=>update.Invoke(run,null),Throws.Nothing);Assert.That(run.VehicleModel.Scrap,Is.EqualTo(wallet));
        Assert.That(run.EarnedScrap,Is.EqualTo(earned));
        run.Definition.Upgrades=original;
    }
    [UnityTearDown] public IEnumerator TearDown(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
