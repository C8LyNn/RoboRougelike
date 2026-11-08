using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class EnemyPinnedSelfDestructTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float until=Time.realtimeSinceStartup+20;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)yield return null;
        var run=R1RunController.Instance;Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<until)yield return null;
        Assert.That(run.Transitioning,Is.False);run.Encounter.SetCombatActive(false);
    }
    [UnityTest] public IEnumerator MobileButUnarmedEnemyExplodesNearPlayerAfterWarning()
    {
        var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        var player=run.Player.GetComponent<Rigidbody>();player.isKinematic=true;
        var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        enemy.transform.SetPositionAndRotation(run.Player.transform.position+Vector3.forward*2,Quaternion.identity);Physics.SyncTransforms();
        foreach(var gun in enemy.AdditionalWeapons.Where(g=>g.Block!=null).ToArray())gun.Block.ApplyDamage(100000,gun.Block.transform.position,Vector3.up);
        if(enemy.WeaponBlock!=null)enemy.WeaponBlock.ApplyDamage(100000,enemy.WeaponBlock.transform.position,Vector3.up);
        Assert.That(enemy.WeaponOnline,Is.False);Assert.That(enemy.ActiveWheelCount,Is.GreaterThan(0),"Regression requires a mobile but disarmed enemy.");
        var target=run.Player.GetComponentsInChildren<P12PipelineSafeDamageable>().First(p=>!p.IsDead&&Vector3.Distance(p.transform.position,enemy.Core.transform.position)<6);
        float hp=target.CurrentHp;enemy.SetCombatActive(true);float started=Time.time,deadline=Time.realtimeSinceStartup+15;
        while(Time.time-started<4.6f&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.IsDefeated,Is.False,"The five-second warning must not be skipped.");
        while(!enemy.IsDefeated&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.IsDefeated,Is.True,"Unarmed enemy at close range must no longer push indefinitely.");
        Assert.That(target==null||target.IsDead||target.CurrentHp<hp,Is.True,"The existing blast must affect nearby player parts.");
    }
    [UnityTest] public IEnumerator UnarmedEnemyDetectsNearbyPartOfLargeVehicleOutsideRootRadius()
    {
        var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        var player=run.Player.GetComponent<Rigidbody>();player.isKinematic=true;
        var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        var root=run.Player.transform.position;
        var target=run.Player.GetComponentsInChildren<P12PipelineSafeDamageable>()
            .Where(p=>!p.IsDead).OrderByDescending(p=>Vector3.ProjectOnPlane(p.transform.position-root,Vector3.up).sqrMagnitude).First();
        var direction=Vector3.ProjectOnPlane(target.transform.position-root,Vector3.up).normalized;
        Assert.That(direction.sqrMagnitude,Is.GreaterThan(.9f));
        var desiredCore=target.transform.position+direction*5.5f;
        enemy.transform.position+=desiredCore-enemy.Core.transform.position;
        Physics.SyncTransforms();
        Assert.That(Vector3.Distance(enemy.transform.position,root),Is.GreaterThan(6f),"The vehicle origins must be outside the old trigger radius.");
        Assert.That(Vector3.Distance(enemy.Core.transform.position,target.transform.position),Is.LessThan(6f),"A real player part must be inside the blast radius.");
        foreach(var gun in enemy.AdditionalWeapons.Where(g=>g.Block!=null).ToArray())gun.Block.ApplyDamage(100000,gun.Block.transform.position,Vector3.up);
        if(enemy.WeaponBlock!=null)enemy.WeaponBlock.ApplyDamage(100000,enemy.WeaponBlock.transform.position,Vector3.up);
        Assert.That(enemy.WeaponOnline,Is.False);
        float hp=target.CurrentHp;enemy.SetCombatActive(true);float started=Time.time,deadline=Time.realtimeSinceStartup+15;
        while(Time.time-started<4.6f&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.IsDefeated,Is.False,"The five-second warning must remain.");
        while(!enemy.IsDefeated&&Time.time-started<7f&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.IsDefeated,Is.True,"A nearby part must trigger the same blast even when the vehicle origin is far away.");
        Assert.That(target==null||target.IsDead||target.CurrentHp<hp,Is.True);
    }
    [UnityTest] public IEnumerator UnarmedEnemyWaitsForPlayerAfterWarningWhenFarAway()
    {
        var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        run.Player.GetComponent<Rigidbody>().isKinematic=true;enemy.GetComponent<Rigidbody>().isKinematic=true;
        enemy.transform.position=run.Player.transform.position+Vector3.forward*30;
        foreach(var gun in enemy.AdditionalWeapons.Where(g=>g.Block!=null).ToArray())gun.Block.ApplyDamage(100000,gun.Block.transform.position,Vector3.up);
        if(enemy.WeaponBlock!=null)enemy.WeaponBlock.ApplyDamage(100000,enemy.WeaponBlock.transform.position,Vector3.up);
        Assert.That(enemy.WeaponOnline,Is.False);enemy.SetCombatActive(true);
        float started=Time.time,deadline=Time.realtimeSinceStartup+15;
        while(Time.time-started<5.5f&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.IsDefeated,Is.False,"A distant player must not trigger a blast.");
        enemy.transform.position=run.Player.transform.position+Vector3.forward*2;Physics.SyncTransforms();
        while(!enemy.IsDefeated&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.IsDefeated,Is.True,"Once the warning elapsed, a nearby player triggers the blast.");
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
