using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;

public class R6RangeTests
{
    R1RunController run;
    bool Action(string name)
    {
        var method=run.GetType().GetMethod(name);
        Assert.That(method,Is.Not.Null,"Missing playable range action: "+name);
        return (bool)method.Invoke(run,null);
    }
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;
    }
    [UnityTest] public IEnumerator MenuRangeUsesEditableStarterAndResetDoesNotChargeGarage()
    {
        run.SelectVehicle(R3VehicleKind.Light);Assert.That(Action("OpenSandboxGarage"),Is.True);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));Assert.That(run.VehicleModel.Design.Name,Is.EqualTo(R3VehicleCatalog.Create(R3VehicleKind.Light).Name));
        var original=run.VehicleModel;int scrap=original.Scrap;
        Assert.That(Action("EnterTestRange"),Is.True);while(run.Transitioning)yield return null;
        Assert.That(run.Progress.State.ToString(),Is.EqualTo("TestDrive"));Assert.That(run.VehicleModel,Is.Not.SameAs(original));
        Assert.That(GameObject.Find("Poligon R6"),Is.Not.Null);
        run.Player.Core.ApplyDamage(100000,run.Player.transform.position,Vector3.up);yield return null;
        Assert.That(run.Progress.State.ToString(),Is.EqualTo("TestDrive"));
        Assert.That(Action("ResetTestRange"),Is.True);while(run.Transitioning)yield return null;
        Assert.That(run.Player.Core.IsDead,Is.False);
        Assert.That(Action("ReturnFromTestRange"),Is.True);while(run.Transitioning)yield return null;
        Assert.That(run.VehicleModel,Is.SameAs(original));Assert.That(original.Scrap,Is.EqualTo(scrap));
        Assert.That(original.Parts.All(p=>p.Health==1),Is.True);Assert.That(run.Progress.CompletedEncounters,Is.Zero);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
    }
    [UnityTest] public IEnumerator ExpeditionRangePreservesInventoryUpgradesAndMoneyAcrossRepeatedVisits()
    {
        run.StartRun();run.ChooseRoute(0);while(run.Transitioning)yield return null;
        run.Encounter.SetCombatActive(false);foreach(var foe in run.Encounter.Enemies)foe.Core.ApplyDamage(100000,foe.Core.transform.position,Vector3.up);
        while(run.Progress.State==R1RunState.Combat)yield return null;
        RRLW2TestFlow.Claim(run,run.Salvage.Items.Where(i=>i.Recoverable).Take(1).Select(i=>i.Id));
        var original=run.VehicleModel;string before=Snapshot(original);int earned=run.EarnedScrap,kills=run.DefeatedEnemies,upgrades=run.Progress.Acquired.Count;
        for(int visit=0;visit<2;visit++)
        {
            Assert.That(Action("EnterTestRange"),Is.True);while(run.Transitioning)yield return null;
            run.VehicleModel.Award(999);run.VehicleModel.Parts.First().Health=.2f;
            if(run.VehicleModel.Inventory.Count>0)run.VehicleModel.Dismantle(run.VehicleModel.Inventory[0].Id);
            Assert.That(Action("ReturnFromTestRange"),Is.True);while(run.Transitioning)yield return null;
            Assert.That(Snapshot(original),Is.EqualTo(before));Assert.That(run.VehicleModel,Is.SameAs(original));
            Assert.That(run.EarnedScrap,Is.EqualTo(earned));Assert.That(run.DefeatedEnemies,Is.EqualTo(kills));Assert.That(run.Progress.Acquired.Count,Is.EqualTo(upgrades));
        }
        Assert.That(run.StartNextEncounter(),Is.True);Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Route));
    }
    static string Snapshot(R3VehicleModel model)=>model.Scrap+"|"+string.Join(";",model.Parts.Concat(model.Inventory).Select(p=>JsonUtility.ToJson(p)));
    [UnityTest] public IEnumerator MaximumConstructionFiresPhysicalShotAndReturnsWithItsOriginalBudget()
    {
        run.SelectVehicle(R3VehicleKind.Heavy);Assert.That(Action("OpenSandboxGarage"),Is.True);
        var model=run.VehicleModel;
        // Build outward through the real garage, leaving the upper gun lanes clear.
        // Each successful purchase rebuilds the physical assembly used by the next validation.
        var candidates=(from y in Enumerable.Range(0,2)
                        from x in Enumerable.Range(-6,13)
                        from z in Enumerable.Range(-7,15)
                        orderby y,Math.Abs(x)+Math.Abs(z)
                        select new Vector3Int(x,y,z)).ToArray();
        bool progress;
        do
        {
            progress=false;
            foreach(var cell in candidates)
            {
                if(model.ConstructionBudgetUsed+model.Economy.Construction.BlockCost>model.ConstructionBudgetLimit)break;
                if(model.At(cell)!=null)continue;
                if(run.GarageBuy(cell,R3PartKind.Armor)){progress=true;yield return null;}
            }
        }while(progress&&model.ConstructionBudgetUsed+model.Economy.Construction.BlockCost<=model.ConstructionBudgetLimit);
        Assert.That(model.ConstructionBudgetUsed,Is.LessThanOrEqualTo(model.ConstructionBudgetLimit));
        Assert.That(model.ConstructionBudgetLimit-model.ConstructionBudgetUsed,Is.LessThan(model.Economy.Construction.BlockCost));
        Assert.That(model.CanDeploy,Is.True);
        Assert.That(model.PlacementError(new Vector3Int(0,5,0),R3PartKind.Armor),Does.Contain("Budżet konstrukcji"));
        int parts=model.Parts.Count;
        int budget=model.Scrap;
        Assert.That(run.EnterTestRange(),Is.True,"Maximum construction must be deployable to the test range.");while(run.Transitioning)yield return null;
        Assert.That(run.Player.GetComponent<R3VehicleAssembly>().Parts.Count,Is.EqualTo(parts));
        var target=GameObject.Find("Poligon R6").GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().First(p=>p.name=="Rdzeń celu"&&Mathf.Abs(p.transform.position.x)<.1f);
        var gun=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Gun);gun.Muzzle.LookAt(target.transform.position);
        run.Player.Controller.FireFrom(gun.Muzzle,gun.Weapon,gun.Id);
        float end=Time.time+3;while(Time.time<end&&target.CurrentHp==target.MaxHp)yield return null;
        Assert.That(target.CurrentHp,Is.LessThan(target.MaxHp),"Maximum-size construction must use a real projectile to damage a range target.");
        run.ReturnFromTestRange();while(run.Transitioning)yield return null;
        Assert.That(run.VehicleModel,Is.SameAs(model));Assert.That(model.Scrap,Is.EqualTo(budget));Assert.That(model.Parts.Count,Is.EqualTo(parts));
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
