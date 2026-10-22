using System.Collections;
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R5;

public class R5RunTests
{
    private static IEnumerator Until(Func<bool> condition,string message,float timeout=15)
    {float end=Time.realtimeSinceStartup+timeout;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;Assert.That(condition(),Is.True,message);}
    [Test] public void EconomySupportsModerateRepairsAndAnOptionalWeaponWithoutNewCurrency()
    {
        var catalog=JsonUtility.FromJson<R5Catalog>(Resources.Load<TextAsset>("R5/Encounters").text);
        var plan=new R5RunPlan(catalog.Stages,2026);
        foreach(RoboRougelike.R3.R3VehicleKind kind in Enum.GetValues(typeof(RoboRougelike.R3.R3VehicleKind)))
        {
            var model=new RoboRougelike.R3.R3VehicleModel(RoboRougelike.R3.R3VehicleCatalog.Create(kind));
            for(int stage=0;stage<6;stage++)
            {
                model.Award(plan.Offers(stage)[0].ScrapReward);
                foreach(var wheel in model.Parts.Where(p=>p.Kind==RoboRougelike.R3.R3PartKind.Wheel).Take(2))wheel.Health=0;
                foreach(var armor in model.Parts.Where(p=>p.Kind==RoboRougelike.R3.R3PartKind.Armor).Take(8))armor.Health=.5f;
                model.RepairAffordable();Assert.That(model.Parts.All(p=>p.Health>.99f),Is.True,"Moderate damage can be paid each stage.");
                Assert.That(model.CanDeploy,Is.True);
            }
            Assert.That(model.Scrap,Is.GreaterThanOrEqualTo(55));Assert.That(model.Scrap,Is.LessThan(500));
        }
    }
    [Test] public void SeedReplaysAllRoutesRewardsAndUpgradeRandomWithoutReroll()
    {
        var catalog=JsonUtility.FromJson<R5Catalog>(Resources.Load<TextAsset>("R5/Encounters").text);
        var a=new R5RunPlan(catalog.Stages,2026);var b=new R5RunPlan(catalog.Stages,2026);var other=new R5RunPlan(catalog.Stages,867);
        bool different=false;
        Assert.That(a.Count,Is.EqualTo(6));
        for(int stage=0;stage<6;stage++)
        {
            Assert.That(a.Offers(stage).Select(JsonUtility.ToJson),Is.EqualTo(b.Offers(stage).Select(JsonUtility.ToJson)));
            Assert.That(a.Offers(stage).Select(JsonUtility.ToJson),Is.EqualTo(a.Offers(stage).Select(JsonUtility.ToJson)));
            Assert.That(a.UpgradeRandom(stage).Next(),Is.EqualTo(b.UpgradeRandom(stage).Next()));
            different|=a.Offers(stage).Select(JsonUtility.ToJson).SequenceEqual(other.Offers(stage).Select(JsonUtility.ToJson))==false;
            foreach(var item in a.Offers(stage)){Assert.That(item.EnemyCount,Is.InRange(1,3));Assert.That(R5RunPlan.Threat(item),Is.LessThanOrEqualTo(item.ThreatBudget));}
        }
        Assert.That(different,Is.True);Assert.That(a.Offers(1).Length,Is.GreaterThanOrEqualTo(2));Assert.That(a.Offers(3)[0].Id,Is.Not.EqualTo(a.Offers(1)[0].Id));
    }
    [UnityTest] public IEnumerator SixStageBranchPreservesModelAndEndsWithoutGarage()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;run.RequestedSeed=2026;run.StartRun();var model=run.VehicleModel;
        int foes=0;var arenas=new System.Collections.Generic.HashSet<R5ArenaKind>();
        for(int stage=0;stage<6;stage++)
        {
            Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Route));
            Assert.That(run.ChooseRoute(-1),Is.False);Assert.That(run.ChooseRoute(0),Is.True);Assert.That(run.ChooseRoute(0),Is.False);
            while(run.Transitioning)yield return null;
            Assert.That(GameObject.Find("Arena R5"),Is.Not.Null,"Arena lifecycle must create a real layout.");
            arenas.Add(run.CurrentEncounter.Arena);Assert.That(run.VehicleModel,Is.SameAs(model));
            run.Encounter.SetCombatActive(false);foes+=run.Encounter.AliveCount;
            foreach(var enemy in run.Encounter.Enemies)enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
            while(run.Progress.State==R1RunState.Combat)yield return null;
            Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
            var keep=run.Salvage.Items.Where(p=>p.Recoverable).Take(1).Select(p=>p.Id).ToArray();
            Assert.That(RRLW2TestFlow.Claim(run,keep),Is.True);int scrap=model.Scrap;
            Assert.That(RRLW2TestFlow.Claim(run,keep),Is.False);Assert.That(model.Scrap,Is.EqualTo(scrap));
            if(stage==5){Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Won));Assert.That(run.StartNextEncounter(),Is.False);break;}
            Assert.That(run.Progress.Acquired.Count,Is.EqualTo(stage+1));Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
            Assert.That(run.StartNextEncounter(),Is.True);
        }
        Assert.That(arenas.Count,Is.EqualTo(3));Assert.That(run.Progress.CompletedEncounters,Is.EqualTo(6));Assert.That(run.DefeatedEnemies,Is.EqualTo(foes));
        Assert.That(run.RecoveredModules,Is.EqualTo(6));Assert.That(run.EarnedScrap,Is.GreaterThan(500));Assert.That(model.Inventory.Count,Is.EqualTo(6));
        var old=run;run.RestartRun();while(R1RunController.Instance==null||R1RunController.Instance==old||R1RunController.Instance.Progress==null)yield return null;
        run=R1RunController.Instance;Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Ready));run.StartRun();Assert.That(run.VehicleModel.Scrap,Is.Zero);Assert.That(run.VehicleModel.Inventory,Is.Empty);
        Assert.That(run.DefeatedEnemies+run.EarnedScrap+run.RecoveredModules,Is.Zero);yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator NewExpeditionOffersRouteBeforeFirstCombat()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;
        Assert.That(run.Definition.Encounters.Length,Is.EqualTo(6));
        Assert.That(run.StartRun(),Is.True);
        Assert.That(run.Progress.State.ToString(),Is.EqualTo("Route"));
        Assert.That(Time.timeScale,Is.Zero);
        Assert.That(run.StartNextEncounter(),Is.False);
        yield return new ExitPlayMode();
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
