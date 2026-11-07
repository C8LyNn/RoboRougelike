using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4_1;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4EnemyRocketBeamArtTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
    }
    IEnumerator StartCombat()
    {
        var run=R1RunController.Instance;Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        float end=Time.realtimeSinceStartup+20;while(run.Transitioning&&Time.realtimeSinceStartup<end)yield return null;
        run.Encounter.SetCombatActive(false);
    }
    [UnityTest] public IEnumerator EnemyRocketUsesVisibleMuzzleAndDamagesTarget()
    {
        yield return StartCombat();var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        Assert.That(enemy.EquipRocket(),Is.True);var binding=enemy.WeaponBlock.GetComponent<RRLRocketBinding>();
        var art=enemy.WeaponBlock.GetComponentInChildren<R4WeaponArt>(true);Assert.That(art,Is.Not.Null,"Enemy rocket must show its supplied model.");
        Assert.That(art.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4W_RocketLauncher_ROOT"),Is.True);
        Assert.That(typeof(RRLRocketBinding).GetField("muzzle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(binding),Is.SameAs(art.Muzzle));
        Assert.That(enemy.WeaponBlock.GetComponent<Collider>().enabled,Is.True);
        var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;body.position=new Vector3(100,4,100);enemy.transform.rotation=Quaternion.identity;art.Sync();
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=art.Muzzle.position+art.Muzzle.forward*6;
        target.transform.localScale=new Vector3(3,3,1);var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);
        var bank=enemy.GetComponent<VehicleEnergyBank>();bank.Recharge(1000);float before=bank.Current;Physics.SyncTransforms();enemy.SetCombatActive(true);
        Assert.That(binding.Fire(),Is.True);Assert.That(bank.Current,Is.LessThan(before));
        float end=Time.realtimeSinceStartup+6;while(hp.CurrentHp>=500&&Time.realtimeSinceStartup<end)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.LessThan(500),"Physical rocket must damage its target.");Object.Destroy(target);
    }
    [UnityTest] public IEnumerator EnemyBeamUsesVisibleMuzzleAndDamagesTarget()
    {
        yield return StartCombat();var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        Assert.That(enemy.EquipBeam(),Is.True);var binding=enemy.WeaponBlock.GetComponent<RRLBeamBinding>();
        var art=enemy.WeaponBlock.GetComponentInChildren<R4WeaponArt>(true);Assert.That(art,Is.Not.Null,"Enemy beam must show its supplied model.");
        Assert.That(art.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4W_BeamCutter_ROOT"),Is.True);
        Assert.That(binding.Module.GetType().GetField("muzzle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(binding.Module),Is.SameAs(art.Muzzle));
        Assert.That(enemy.WeaponBlock.GetComponent<Collider>().enabled,Is.True);
        var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;body.position=new Vector3(100,4,100);enemy.transform.rotation=Quaternion.identity;art.Sync();
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=art.Muzzle.position+art.Muzzle.forward*6;
        target.transform.localScale=new Vector3(3,3,1);var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);
        var bank=enemy.GetComponent<VehicleEnergyBank>();bank.Recharge(1000);float before=bank.Current;Physics.SyncTransforms();enemy.SetCombatActive(true);
        // Invoke the existing fixed simulation with the trigger held; this uses
        // the real beam raycast and damage/energy adapters, not a fake hit.
        var step=typeof(RRLBeamBinding).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
        for(int i=0;i<30&&hp.CurrentHp>=500;i++){binding.SetTriggerHeld(true);step.Invoke(binding,null);yield return null;}
        Assert.That(hp.CurrentHp,Is.LessThan(500),"Real beam ray must damage its target.");
        Assert.That(bank.Current,Is.LessThan(before),"Beam must spend existing energy.");Object.Destroy(target);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
