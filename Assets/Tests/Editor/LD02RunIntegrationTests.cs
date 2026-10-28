using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.MapEditor;
using RogueMachine.P12PipelineSafe;

public sealed class LD02RunIntegrationTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");
        yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
    }
    IEnumerator Prepare(R3VehicleKind kind)
    {
        Assert.That(run.SelectVehicle(kind),Is.True);
        Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
    }
    IEnumerator Deploy()
    {
        var entry=run.GetType().GetMethod("ChooseLD02Arena",BindingFlags.Public|BindingFlags.Instance);
        Assert.That(entry,Is.Not.Null,"LD02 must be a real player activity");
        Assert.That((bool)entry.Invoke(run,null),Is.True);
        for(int i=0;i<300&&run.Transitioning;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat));
        Assert.That(run.Encounter.AliveCount,Is.GreaterThan(0));
        Assert.That(GameObject.Find("LD02 arena"),Is.Not.Null);
    }
    [UnityTest] public IEnumerator LightWinsTwoFightsWithSingleRewardsAndRepair()
    {
        yield return Prepare(R3VehicleKind.Light);yield return Deploy();
        var ids=run.VehicleModel.Parts.Select(p=>p.Id).ToArray();
        int before=run.VehicleModel.Scrap;
        run.Encounter.SetCombatActive(false);
        var armor=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Armor);
        var damagedCell=armor.Cell;armor.Damageable.ApplyDamage(10,armor.transform.position,Vector3.zero);
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Salvage;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        int reward=run.LastScrapReward;Assert.That(reward,Is.GreaterThan(0));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before+reward));
        yield return null;yield return null;Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before+reward));
        var loot=run.Salvage.Items.First(p=>p.Selectable);
        Assert.That(run.ConfirmRewards(new[]{loot.Id},new[]{0}),Is.True);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
        Assert.That(run.VehicleModel.Parts.Select(p=>p.Id),Is.EquivalentTo(ids));
        Assert.That(run.GarageRepair(damagedCell),Is.True,"Damage from the encounter can be repaired in the garage");
        yield return null;yield return Deploy();
        Assert.That(run.Progress.EncounterIndex,Is.EqualTo(1));
        AssertSpawnClear();
        int secondBefore=run.VehicleModel.Scrap;
        run.Encounter.SetCombatActive(false);
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Salvage;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        Assert.That(run.Progress.CompletedEncounters,Is.EqualTo(2));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(secondBefore+run.LastScrapReward));
        int after=run.VehicleModel.Scrap;yield return null;yield return null;
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(after),"The second victory must not pay twice");
    }
    [UnityTest] public IEnumerator HeavyCanDeployAndDefeatEndsRun()
    {
        yield return Prepare(R3VehicleKind.Heavy);yield return Deploy();
        run.Encounter.SetCombatActive(false);
        run.Player.Core.ApplyDamage(100000,run.Player.Core.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Lost;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Lost));
    }
    [UnityTest] public IEnumerator LightSpawnHasNoArenaOrEnemyPenetration()
    {yield return Prepare(R3VehicleKind.Light);yield return Deploy();AssertSpawnClear();}
    [UnityTest] public IEnumerator HeavySpawnHasNoArenaOrEnemyPenetration()
    {yield return Prepare(R3VehicleKind.Heavy);yield return Deploy();AssertSpawnClear();}
    void AssertSpawnClear()
    {
        Physics.SyncTransforms();
        var world=GameObject.Find("LD02 arena");
        var obstacles=world.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger&&
            c.GetComponentInParent<MapItemReference>()?.Item.kind!=MapItemKind.Ground).ToArray();
        var actors=run.Encounter.Enemies.Select(e=>e.transform).Concat(new[]{run.Player.transform}).ToArray();
        var colliders=actors.Select(a=>a.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray()).ToArray();
        for(int i=0;i<actors.Length;i++)foreach(var part in colliders[i])
        {
            foreach(var obstacle in obstacles)AssertSeparated(part,obstacle);
            for(int j=i+1;j<actors.Length;j++)foreach(var other in colliders[j])AssertSeparated(part,other);
        }
    }
    static void AssertSeparated(Collider a,Collider b)
    {
        if(!a.bounds.Intersects(b.bounds))return;
        if(Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out _,out var distance))
            Assert.That(distance,Is.LessThan(.03f),$"{a.name} intersects {b.name} at spawn");
    }
    [UnityTest] public IEnumerator EnemiesFireRealProjectilesAtPlayer()
    {
        yield return Prepare(R3VehicleKind.Light);yield return Deploy();
        bool shot=false;float deadline=Time.realtimeSinceStartup+12;
        while(Time.realtimeSinceStartup<deadline&&!shot&&run.Progress.State==R1RunState.Combat)
        {
            shot=Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None)
                .Any(s=>run.Encounter.Enemies.Any(e=>e!=null&&s.OwnerRoot==e.transform));
            yield return null;
        }
        Assert.That(shot,Is.True,"Active enemy must create a real physical shell");
    }
    [UnityTest] public IEnumerator ArenaWallStopsPhysicalShellBeforeTarget()
    {
        yield return Prepare(R3VehicleKind.Light);yield return Deploy();
        run.Encounter.SetCombatActive(false);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.transform.position=new Vector3(20,1.5f,-3);
        var hp=target.AddComponent<P12PipelineSafeDamageable>();
        var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shell.transform.position=new Vector3(20,1.5f,4);shell.transform.localScale=Vector3.one*.2f;
        var body=shell.AddComponent<Rigidbody>();body.useGravity=false;
        body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.linearVelocity=Vector3.back*20;
        shell.AddComponent<P17PhysicalShell>().Configure(run.Player.transform,40,3,Vector3.back,null);
        Physics.SyncTransforms();
        var hits=Physics.RaycastAll(shell.transform.position,Vector3.back,6,~0,QueryTriggerInteraction.Ignore);
        Debug.Log("[LD02 cover] ray="+string.Join(",",hits.Select(h=>$"{h.collider.name}:{h.distance:0.00}")));
        for(int i=0;i<100&&shell!=null;i++)
        {float previous=Time.fixedTime;while(Time.fixedTime<=previous)yield return null;}
        Debug.Log($"[LD02 cover] projectile={(shell==null?"destroyed":shell.transform.position.ToString())} targetHp={hp.CurrentHp} timeScale={Time.timeScale}");
        Assert.That(shell==null,Is.True,"Real wall consumes the physical shell");
        Assert.That(hp.CurrentHp,Is.EqualTo(100),"Target behind wall must not take damage");
        Object.Destroy(target);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
