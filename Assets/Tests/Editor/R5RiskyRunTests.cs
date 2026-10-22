using System.Collections;
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R5;

public class R5RiskyRunTests
{
    private static IEnumerator Until(Func<bool> condition,string message,float timeout=15)
    {float end=Time.realtimeSinceStartup+timeout;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;Assert.That(condition(),Is.True,message);}
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        yield return Until(()=>R1RunController.Instance!=null&&R1RunController.Instance.Progress!=null,"Bootstrap");
    }
    [UnityTest] public IEnumerator RiskyBranchKeepsDamageGeometryInventoryAndCleansEachArena()
    {
        var run=R1RunController.Instance;
        Assert.That(run,Is.Not.Null);
        Assert.That(run.Error,Is.Null);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Ready));
        Assert.That(run.SelectVehicle(RoboRougelike.R3.R3VehicleKind.Light),Is.True);
        run.RequestedSeed=867;
        Assert.That(run.StartRun(),Is.True);
        var model=run.VehicleModel;string retained=null;
        for(int stage=0;stage<6;stage++)
        {
            Assert.That(run.ChooseRoute(run.RouteOffers.Length-1),Is.True);yield return Until(()=>!run.Transitioning,"Risk combat");run.Encounter.SetCombatActive(false);
            Assert.That(run.VehicleModel,Is.SameAs(model));Assert.That(UnityEngine.Object.FindObjectsByType<RoboRougelike.R3.R3VehicleAssembly>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Assert.That(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name=="Arena R5"),Is.EqualTo(1));
            Assert.That(UnityEngine.Object.FindObjectsByType<RogueMachine.P12PipelineSafe.P17PhysicalShell>(FindObjectsSortMode.None),Is.Empty);
            var armor=run.Player.GetComponent<RoboRougelike.R3.R3VehicleAssembly>().Parts.First(p=>p.Kind==RoboRougelike.R3.R3PartKind.Armor);
            if(stage==0){retained=armor.Id;armor.Damageable.ApplyDamage(armor.Damageable.MaxHp*.4f,armor.transform.position,Vector3.up);}
            else Assert.That(model.Parts.Single(p=>p.Id==retained).Health,Is.EqualTo(.6f).Within(.001));
            foreach(var enemy in run.Encounter.Enemies)enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
            yield return Until(()=>run.Progress.State==R1RunState.Salvage,"Risk loot");
            var selected=run.Salvage.Items.Where(p=>p.Recoverable).Take(2).Select(p=>p.Id).ToArray();RRLW2TestFlow.Claim(run,selected);
            if(stage==5)break;
            if(stage==0)Assert.That(run.GarageBuy(new Vector3Int(1,1,0),RoboRougelike.R3.R3PartKind.Armor),Is.True);
            run.StartNextEncounter();
        }
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Won));Assert.That(run.Progress.CompletedEncounters,Is.EqualTo(6));
        Assert.That(model.Inventory.Count,Is.EqualTo(18));Assert.That(model.Parts.Count,Is.EqualTo(21));yield return new ExitPlayMode();
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
