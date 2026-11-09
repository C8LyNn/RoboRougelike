using System.Collections.Generic;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R3DamagePropagationTests
{
    [UnityTearDown] public IEnumerator Cleanup()
    {if(Application.isPlaying)yield return new ExitPlayMode();}
    static R3DamagePropagation.Node Part(int id,float hp,params int[] neighbours) =>
        new R3DamagePropagation.Node(id,hp,neighbours);

    [Test]
    public void DamageIsSharedEquallyWithoutCreatingOrLosingDamage()
    {
        var graph=new[]{Part(0,100,1,2),Part(1,100,0),Part(2,100,0)};
        var result=R3DamagePropagation.Resolve(graph,0,100,.25f,2);
        Assert.That(result.Damage[0],Is.EqualTo(75).Within(.001f));
        Assert.That(result.Damage[1],Is.EqualTo(12.5f).Within(.001f));
        Assert.That(result.Damage[2],Is.EqualTo(12.5f).Within(.001f));
        Assert.That(result.Unused,Is.EqualTo(0).Within(.001f));
    }

    [Test]
    public void CyclesDoNotRepeatDamageAndConvergentPathsAreCombined()
    {
        var graph=new[]{Part(0,100,1,2),Part(1,100,0,3),Part(2,100,0,3),Part(3,100,1,2)};
        var result=R3DamagePropagation.Resolve(graph,0,100,.25f,3);
        Assert.That(result.Damage[0],Is.EqualTo(75).Within(.001f));
        Assert.That(result.Damage[1],Is.EqualTo(9.375f).Within(.001f));
        Assert.That(result.Damage[2],Is.EqualTo(9.375f).Within(.001f));
        Assert.That(result.Damage[3],Is.EqualTo(6.25f).Within(.001f));
        Assert.That(result.Unused,Is.EqualTo(0).Within(.001f));
    }

    [Test]
    public void HopLimitStopsPropagationAndPreservesBudget()
    {
        var graph=new[]{Part(0,100,1),Part(1,100,0,2),Part(2,100,1)};
        var result=R3DamagePropagation.Resolve(graph,0,100,.25f,1);
        Assert.That(result.Damage[0],Is.EqualTo(75).Within(.001f));
        Assert.That(result.Damage[1],Is.EqualTo(25).Within(.001f));
        Assert.That(result.Damage.ContainsKey(2),Is.False);
    }

    [Test]
    public void OverflowCannotDestroyUnconnectedPartsAndIsAccountedFor()
    {
        var graph=new[]{Part(0,20,1),Part(1,30,0),Part(2,100)};
        var result=R3DamagePropagation.Resolve(graph,0,100,.25f,2);
        Assert.That(result.Damage[0],Is.EqualTo(20).Within(.001f));
        Assert.That(result.Damage[1],Is.EqualTo(30).Within(.001f));
        Assert.That(result.Damage.ContainsKey(2),Is.False);
        Assert.That(result.Unused,Is.EqualTo(50).Within(.001f));
    }

    [Test]
    public void ProjectileProfilesChangeTheSpreadWithoutChangingDamageBudget()
    {
        var graph=new[]{Part(0,100,1),Part(1,100,0)};
        var kinetic=R3DamagePropagation.Resolve(graph,0,100,
            R3DamagePropagation.TransferFraction(R3DamagePropagation.DamageKind.Kinetic),2);
        var piercing=R3DamagePropagation.Resolve(graph,0,100,
            R3DamagePropagation.TransferFraction(R3DamagePropagation.DamageKind.Piercing),2);
        var blast=R3DamagePropagation.Resolve(graph,0,100,
            R3DamagePropagation.TransferFraction(R3DamagePropagation.DamageKind.Blast),2);
        Assert.That(piercing.Damage[1],Is.LessThan(kinetic.Damage[1]));
        Assert.That(blast.Damage[1],Is.GreaterThan(kinetic.Damage[1]));
        foreach(var result in new[]{kinetic,piercing,blast})
            Assert.That(result.Damage.Values.Sum()+result.Unused,Is.EqualTo(100).Within(.001f));
    }

    [Test]
    public void DifferentLayoutsTradeCoreProtectionAcrossImpactTypes()
    {
        // Same five full-health parts; 1 is the core, 0 receives the hit.
        var compact=new[]{Part(0,100,1,2,3),Part(1,100,0),Part(2,100,0,4),Part(3,100,0),Part(4,100,2)};
        var routed=new[]{Part(0,100,2),Part(1,100,2,3),Part(2,100,0,1),Part(3,100,1,4),Part(4,100,3)};
        var kinetic=R3DamagePropagation.TransferFraction(R3DamagePropagation.DamageKind.Kinetic);
        var blast=R3DamagePropagation.TransferFraction(R3DamagePropagation.DamageKind.Blast);
        float Core(R3DamagePropagation.Node[] graph,float fraction)=>R3DamagePropagation.Resolve(graph,0,100,fraction,2).Damage[1];
        Assert.That(Core(routed,kinetic),Is.LessThan(Core(compact,kinetic)),"A narrow route can protect the core from kinetic impacts.");
        Assert.That(Core(routed,blast),Is.GreaterThan(Core(compact,blast)),"The same route has a blast trade-off.");
    }

    [UnityTest]
    public IEnumerator RealPlayerVehicleRoutesOneHitToConnectedParts()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        yield return null;
        ValidatePlayerHit(run);
    }

    static void ValidatePlayerHit(R1RunController run)
    {
        Assert.That(run.Player,Is.Not.Null);
        var parts=run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p!=null&&p.Operational).ToArray();
        Assert.That(parts.Length,Is.GreaterThan(0));
        var before=new Dictionary<string,float>();
        foreach(var part in parts)
        {
            Assert.That(part.Damageable,Is.Not.Null,part.name);
            before.Add(part.Id,part.Damageable.CurrentHp);
        }
        var target=parts.Single(p=>p.Cell==new Vector3Int(1,0,0));
        target.Damageable.ApplyDamage(8,target.transform.position,Vector3.forward);
        Assert.That(before[target.Id]-target.Damageable.CurrentHp,Is.EqualTo(6).Within(.001f),
            "A connected hit retains 75% on the struck part with the prototype profile.");
        Assert.That(parts.Where(p=>p!=target).Sum(p=>before[p.Id]-p.Damageable.CurrentHp),Is.EqualTo(2).Within(.001f),
            "The remaining 25% is applied to connected parts, without duplication.");
        before=parts.ToDictionary(p=>p.Id,p=>p.Damageable.CurrentHp);
        target.Damageable.ApplyDamageResolvedTyped(10,target.transform.position,Vector3.forward,
            R3DamagePropagation.DamageKind.Piercing);
        Assert.That(before[target.Id]-target.Damageable.CurrentHp,Is.EqualTo(9).Within(.001f),"Piercing impacts transfer 10%.");
        Assert.That(parts.Where(p=>p!=target).Sum(p=>before[p.Id]-p.Damageable.CurrentHp),Is.EqualTo(1).Within(.001f));
        before=parts.ToDictionary(p=>p.Id,p=>p.Damageable.CurrentHp);
        var rocket=target.GetComponent<RoboRougelike.WeaponIntegration.RRLRocketDamageReceiver>();
        Assert.That(rocket,Is.Not.Null);
        rocket.ReceiveRocketDamage(10,target.transform.position,Vector3.up,null);
        Assert.That(before[target.Id]-target.Damageable.CurrentHp,Is.EqualTo(6).Within(.001f),"Rocket impacts use the blast profile.");
        Assert.That(parts.Where(p=>p!=target).Sum(p=>before[p.Id]-p.Damageable.CurrentHp),Is.EqualTo(4).Within(.001f));
    }

    [UnityTest]
    public IEnumerator RealEnemyVehicleUsesTheSameConnectedDamageRules()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.StartRun(),Is.True);
        if(run.Progress.State==R1RunState.Route)Assert.That(run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<end)yield return null;
        run.Encounter.SetCombatActive(false);yield return null;
        ValidateEnemyHit(run);
    }

    static void ValidateEnemyHit(R1RunController run)
    {
        var enemy=run.Encounter.Enemies.First();
        var target=enemy.Core;Assert.That(target,Is.Not.Null);
        var parts=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>()
            .Where(p=>p!=null&&!p.IsDead).ToArray();
        var before=parts.ToDictionary(p=>p.GetInstanceID(),p=>p.CurrentHp);
        target.ApplyDamage(8,target.transform.position,Vector3.forward);
        Assert.That(before[target.GetInstanceID()]-target.CurrentHp,Is.EqualTo(6).Within(.001f));
        Assert.That(parts.Where(p=>p!=target).Sum(p=>before[p.GetInstanceID()]-p.CurrentHp),Is.EqualTo(2).Within(.001f));
    }
}
