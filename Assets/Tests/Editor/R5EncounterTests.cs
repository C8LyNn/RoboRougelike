using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R4;
using RogueMachine.P12PipelineSafe;

public class R5EncounterTests
{
    private R1RunController Run=>R1RunController.Instance;
    private static IEnumerator Until(Func<bool> condition,string message,float timeout=15)
    {float end=Time.realtimeSinceStartup+timeout;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;Assert.That(condition(),Is.True,message);}
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        yield return Until(()=>Run?.Progress!=null,"Bootstrap");
    }
    private IEnumerator Reach(int target,int eliteRoute=0)
    {
        Run.StartRun();
        for(int stage=0;stage<=target;stage++)
        {
            Run.ChooseRoute(stage==3?eliteRoute:0);yield return Until(()=>!Run.Transitioning,"Combat");Run.Encounter.SetCombatActive(false);
            if(stage==target)break;
            foreach(var enemy in Run.Encounter.Enemies)enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
            yield return Until(()=>Run.Progress.State==R1RunState.Salvage,"Loot");Run.ConfirmSalvage(Array.Empty<string>());Run.ChooseUpgrade(0);Run.StartNextEncounter();
        }
    }
    [UnityTest] public IEnumerator BossHasTwoIndependentDestructibleWeaponsAndNormalSalvage()
    {
        yield return Reach(5);var boss=Run.Encounter.Enemies.Single();
        var guns=boss.GetComponentsInChildren<P12PipelineSafeFunctionalModule>().Where(m=>m.ModuleType==P12FunctionalModuleType.Weapon).ToArray();
        Assert.That(guns.Length,Is.EqualTo(2),"Boss needs two real independent weapon modules.");
        var structure=boss.GetComponent<P12PipelineSafeVehicleStructure>();
        guns[0].GetComponent<P12PipelineSafeDamageable>().ApplyDamage(10000,guns[0].transform.position,Vector3.up);structure.EvaluateStructure();
        Assert.That(guns[0].IsOperational(structure),Is.False);Assert.That(guns[1].IsOperational(structure),Is.True);Assert.That(boss.IsDefeated,Is.False);
        boss.Core.ApplyDamage(10000,boss.Core.transform.position,Vector3.up);yield return Until(()=>Run.Progress.State==R1RunState.Salvage,"Boss loot");
        Assert.That(Run.Salvage.Items.Count(p=>p.Kind==RoboRougelike.R3.R3PartKind.Gun),Is.EqualTo(2));
        Assert.That(Run.Salvage.Items.Count(p=>p.Kind==RoboRougelike.R3.R3PartKind.Gun&&p.Recoverable),Is.EqualTo(1));
        Run.ConfirmSalvage(Array.Empty<string>());Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Won));
    }
    [UnityTest] public IEnumerator SummaryIncludesEnemyDestroyedDuringLostEncounter()
    {
        yield return Reach(1);var enemy=Run.Encounter.Enemies[0];enemy.Core.ApplyDamage(10000,enemy.Core.transform.position,Vector3.up);
        Run.Player.Core.ApplyDamage(10000,Run.Player.Core.transform.position,Vector3.up);
        yield return Until(()=>Run.Progress.State==R1RunState.Lost,"Loss summary");
        Assert.That(Run.DefeatedEnemies,Is.EqualTo(2),"One earlier victory plus the kill before losing.");
    }
    [UnityTest] public IEnumerator SummaryCountsInventoryDismantlingAsEarnedScrap()
    {
        yield return Reach(0);var enemy=Run.Encounter.Enemies.Single();enemy.Core.ApplyDamage(10000,enemy.Core.transform.position,Vector3.up);
        yield return Until(()=>Run.Progress.State==R1RunState.Salvage,"Loot");
        var item=Run.Salvage.Items.First(p=>p.Recoverable);Run.ConfirmSalvage(new[]{item.Id});Run.ChooseUpgrade(0);
        int earned=Run.EarnedScrap;int value=Run.VehicleModel.DismantleValue(Run.VehicleModel.Inventory.Single());
        Assert.That(Run.GarageDismantle(item.Id),Is.True);Assert.That(Run.EarnedScrap,Is.EqualTo(earned+value));
    }
    [UnityTest] public IEnumerator BossFiresTwoPhysicalBatteriesAndDestroyedBatteryStops()
    {
        yield return Reach(5);var boss=Run.Encounter.Enemies.Single();
        var seen=new System.Collections.Generic.HashSet<string>();Run.Encounter.SetCombatActive(true);
        float end=Time.time+8;
        while(Time.time<end)
        {
            foreach(var origin in UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None))
                if(origin.PartId.StartsWith("OBLĘŻNIK/")){seen.Add(origin.PartId);Assert.That(origin.GetComponent<P17PhysicalShell>(),Is.Not.Null);}
            yield return null;
        }
        Assert.That(seen.Count,Is.EqualTo(2));Run.Encounter.SetCombatActive(false);
        var gun=boss.VariantWeapons[0];gun.Block.ApplyDamage(10000,gun.Block.transform.position,Vector3.up);
        R1EncounterManager.ClearTransientObjects();seen.Clear();Run.Encounter.SetCombatActive(true);end=Time.time+7;
        while(Time.time<end){foreach(var origin in UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None))if(origin.PartId.StartsWith("OBLĘŻNIK/"))seen.Add(origin.PartId);yield return null;}
        Assert.That(seen.Count,Is.EqualTo(1));Assert.That(seen.Single(),Does.Contain("Bateria 1"));
    }
    [UnityTest] public IEnumerator BossSupportSeveranceDropsPhysicalWingAndLeavesOtherGunOperational()
    {
        yield return Reach(5);var boss=Run.Encounter.Enemies.Single();
        var support=boss.GetComponentsInChildren<P12PipelineSafeDamageable>().FirstOrDefault(p=>p.name=="Podpora baterii -1");
        Assert.That(support,Is.Not.Null);
        var guns=boss.GetComponentsInChildren<P12PipelineSafeFunctionalModule>().Where(m=>m.ModuleType==P12FunctionalModuleType.Weapon).OrderBy(m=>m.transform.localPosition.x).ToArray();
        var wing=guns[0];support.ApplyDamage(10000,support.transform.position,Vector3.up);
        var structure=boss.GetComponent<P12PipelineSafeVehicleStructure>();structure.EvaluateStructure();
        Assert.That(wing.transform.IsChildOf(boss.transform),Is.False);Assert.That(wing.GetComponentInParent<Rigidbody>(),Is.Not.Null);
        Assert.That(guns[1].IsOperational(structure),Is.True);Assert.That(boss.IsDefeated,Is.False);
    }
    [UnityTest] public IEnumerator BothEliteArchetypesHaveChangedGeometryAndBetterEquipment()
    {
        for(int route=0;route<2;route++)
        {
            if(route>0){var old=Run;Run.RestartRun();yield return Until(()=>Run!=null&&Run!=old&&Run.Progress!=null,"Bootstrap");}
            yield return Reach(3,route);var elite=Run.Encounter.Enemies.Single();
            Assert.That(elite.GetComponentsInChildren<P12PipelineSafeFunctionalModule>().Count(m=>m.ModuleType==P12FunctionalModuleType.Weapon),Is.EqualTo(2));
            Assert.That(elite.GetComponentsInChildren<P12_PipelineSafePlayerBlock>().Length,Is.GreaterThan(route==0?56:35));
            Assert.That(elite.GetComponentsInChildren<P12PipelineSafeDamageable>().Any(p=>p.name.StartsWith("Podpora baterii")),Is.True);
        }
    }
    [UnityTest] public IEnumerator VariantProjectileSpeedUsesEncounterConfiguration()
    {
        yield return Reach(5);var boss=Run.Encounter.Enemies.Single();
        boss.ConfigureEncounter(Run.Player.transform,1,1,1,1.5f);Run.Encounter.SetCombatActive(true);
        R4ProjectileOrigin shot=null;
        yield return Until(()=>{shot=UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).FirstOrDefault(p=>p.PartId.StartsWith("OBLĘŻNIK/"));return shot!=null;},"Configured boss projectile");
        Assert.That(shot.GetComponent<Rigidbody>().linearVelocity.magnitude,Is.EqualTo(R4WeaponDefinition.Get(shot.Weapon).Speed*.8f*1.5f).Within(.1f));
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
