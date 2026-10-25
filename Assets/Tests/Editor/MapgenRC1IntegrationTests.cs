using System.Collections;
using System.Linq;
using System.Reflection;
using B01.MAPGENWORLD001;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;

public sealed class MapgenRC1IntegrationTests
{
    R1RunController run;
    GameObject legacyFloor;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");
        yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;
        Assert.That(run?.Progress,Is.Not.Null);
        legacyFloor=GameObject.Find("P12_Ground");
        Assert.That(legacyFloor,Is.Not.Null);
        run.RequestedSeed=7;
    }
    static object Call(object obj,string name,params object[] args)
    {
        var method=obj.GetType().GetMethod(name,BindingFlags.Public|BindingFlags.Instance);
        Assert.That(method,Is.Not.Null,"Missing RC1 player entry: "+name);
        return method.Invoke(obj,args);
    }
    MapWorldSnapshot World => (MapWorldSnapshot)run.GetType().GetProperty("GeneratedWorld").GetValue(run);
    IEnumerator Prepare(R3VehicleKind kind)
    {
        Assert.That(run.SelectVehicle(kind),Is.True);
        run.RequestedSeed=7;
        Assert.That((bool)Call(run,"PrepareMapgenRun"),Is.True);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
        Assert.That(run.SandboxWorkshop||run.ReactorPrepared,Is.False);
        yield return null;
    }
    IEnumerator Deploy()
    {
        Assert.That((bool)Call(run,"ChooseGeneratedArena"),Is.True);
        for(int i=0;i<300&&run.Transitioning;i++)yield return null;
        Assert.That(run.Transitioning,Is.False);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat));
    }
    [UnityTest] public IEnumerator UnpreparedRouteRejectsGeneratedEntryWithoutPlayer()
    {
        run.RequestedSeed=7;
        Assert.That(run.StartRun(),Is.True);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Route));
        Assert.That(run.Player,Is.Null);
        Assert.That((bool)run.GetType().GetProperty("CanChooseGeneratedArena").GetValue(run),Is.False);
        Assert.That((bool)Call(run,"ChooseGeneratedArena"),Is.False);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Route));
        Assert.That(World,Is.Null);
        yield return null;
    }
    [UnityTest] public IEnumerator MeasuredHeavySpawnsOnGeneratedFloorWithCanonicalEnemies()
    {
        yield return Prepare(R3VehicleKind.Heavy);
        yield return Deploy();
        run.Encounter.SetCombatActive(false);
        var world=World;
        Assert.That(world.Seed,Is.EqualTo(7));
        Assert.That(world.GeneratedRoot.activeInHierarchy,Is.True);
        Assert.That(legacyFloor.activeSelf,Is.False,"A hidden legacy floor must not counterfeit map support");
        Assert.That(run.Arena.Layout==null||!run.Arena.Layout.gameObject.activeSelf,Is.True);
        Assert.That(run.Encounter.AliveCount,Is.EqualTo(2));
        foreach(var enemy in run.Encounter.Enemies)
            Assert.That(world.EnemySpawns.Any(p=>Vector2.Distance(new Vector2(p.x,p.z),new Vector2(enemy.transform.position.x,enemy.transform.position.z))<.5f),Is.True);
        Assert.That(run.Player.GetComponent<Rigidbody>().constraints,Is.EqualTo(RigidbodyConstraints.None));
        yield return new WaitForSeconds(.8f);
        Assert.That(run.Player.GetComponent<R6VehicleDrive>().GroundedWheels,Is.GreaterThan(0));
        Assert.That(Physics.RaycastAll(run.Player.transform.position+Vector3.up*4,Vector3.down,15).Any(h=>h.collider.transform.IsChildOf(world.GeneratedRoot.transform)),Is.True);
        Assert.That(run.ReactorActive,Is.False);
    }
    [UnityTest] public IEnumerator OversizedColliderRejectsWithoutLeavingGarageOrHidingFloor()
    {
        yield return Prepare(R3VehicleKind.Light);
        var collider=run.Player.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(13,1,3);
        Physics.SyncTransforms();
        Assert.That((bool)Call(run,"ChooseGeneratedArena"),Is.False);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
        Assert.That(run.Transitioning,Is.False);
        Assert.That(legacyFloor.activeSelf,Is.True);
        Assert.That(World,Is.Null);
        Assert.That(run.VehicleModel.Scrap,Is.Zero);
    }
    [UnityTest] public IEnumerator VictoryUsesExistingRewardsRepairsAndFreshRegeneration()
    {
        yield return Prepare(R3VehicleKind.Light);yield return Deploy();
        var old=World;string hash=old.GeometryHash;
        run.Encounter.SetCombatActive(false);
        var armor=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Armor);
        armor.Damageable.ApplyDamage(10,armor.transform.position,Vector3.zero);
        var cell=armor.Cell;
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Salvage;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        Assert.That(World,Is.Null);Assert.That(old.GeneratedRoot==null||!old.GeneratedRoot.activeSelf,Is.True);
        Assert.That(legacyFloor.activeSelf,Is.True);
        var loot=run.Salvage.Items.First(p=>p.Selectable);
        Assert.That(run.ConfirmRewards(new[]{loot.Id},new[]{0}),Is.True);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(2));
        Assert.That(run.GarageRepair(cell),Is.True,"Damage captured by canonical assembly is repairable");
        yield return null;
        yield return Deploy();
        Assert.That(World.GeometryHash,Is.EqualTo(hash));
        Assert.That(World.Revision,Is.EqualTo(old.Revision+1));
        Assert.That(old.GeneratedRoot==null,Is.True);
        Assert.That(run.Encounter.AliveCount,Is.GreaterThan(0));
    }
    [UnityTest] public IEnumerator PlayerDefeatClearsWorldAndRestartReturnsToOriginalMenu()
    {
        yield return Prepare(R3VehicleKind.Light);yield return Deploy();
        var old=World.GeneratedRoot;
        run.Player.Core.ApplyDamage(100000,run.Player.Core.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Lost;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Lost));
        Assert.That(World,Is.Null);Assert.That(old==null||!old.activeSelf,Is.True);
        Assert.That(legacyFloor.activeSelf,Is.True);
        var previous=run;run.RestartRun();
        for(int i=0;i<300&&(R1RunController.Instance==previous||R1RunController.Instance?.Progress==null);i++)yield return null;
        run=R1RunController.Instance;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Ready));
        Assert.That(run.GetType().GetProperty("GeneratedWorld").GetValue(run),Is.Null);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
