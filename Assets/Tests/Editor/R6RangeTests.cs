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
        for(int y=0;y<=5&&model.Parts.Count<180;y++)for(int x=-6;x<=6&&model.Parts.Count<180;x++)for(int z=-7;z<=7&&model.Parts.Count<180;z++)
            model.Buy(new Vector3Int(x,y,z),R3PartKind.Armor);
        Assert.That(model.Parts.Count,Is.EqualTo(180));Assert.That(model.CanDeploy,Is.True);
        run.SendMessage("RebuildGarageVehicle");int budget=model.Scrap;
        run.EnterTestRange();while(run.Transitioning)yield return null;
        Assert.That(run.Player.GetComponent<R3VehicleAssembly>().Parts.Count,Is.EqualTo(180));
        var target=GameObject.Find("Poligon R6").GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().First(p=>p.name=="Rdzeń celu"&&Mathf.Abs(p.transform.position.x)<.1f);
        var gun=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Gun);gun.Muzzle.LookAt(target.transform.position);
        run.Player.Controller.FireFrom(gun.Muzzle,gun.Weapon,gun.Id);
        float end=Time.time+3;while(Time.time<end&&target.CurrentHp==target.MaxHp)yield return null;
        Assert.That(target.CurrentHp,Is.LessThan(target.MaxHp),"Maximum-size construction must use a real projectile to damage a range target.");
        run.ReturnFromTestRange();while(run.Transitioning)yield return null;
        Assert.That(run.VehicleModel,Is.SameAs(model));Assert.That(model.Scrap,Is.EqualTo(budget));Assert.That(model.Parts.Count,Is.EqualTo(180));
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
