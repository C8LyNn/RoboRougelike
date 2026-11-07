using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.Mechanics.EMP;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4_1;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4EnemySawEmpArtTests
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
    [UnityTest] public IEnumerator EnemySawModelSharesExistingPhysicalCuttingCenter()
    {
        yield return StartCombat();var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        var center=enemy.transform.Find("Enemy_Yaw/Enemy_Muzzle");Assert.That(center,Is.Not.Null);
        Assert.That(enemy.EquipSaw(),Is.True);var binding=enemy.WeaponBlock.GetComponent<RRLSawBinding>();
        var art=enemy.WeaponBlock.GetComponentInChildren<R4WeaponArt>(true);Assert.That(art,Is.Not.Null,"Enemy saw must show its supplied model.");
        Assert.That(art.SawRotor,Is.Not.Null);
        Assert.That(typeof(RRLSawBinding).GetField("blade",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(binding),Is.SameAs(center));
        Assert.That(typeof(RRLSawBinding).GetField("rotor",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(binding),Is.SameAs(art.SawRotor));
        Assert.That(Vector3.Distance(art.SawRotor.position,center.position),Is.LessThan(.001f));
        var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;body.position=new Vector3(100,4,100);enemy.transform.rotation=Quaternion.identity;art.Sync();
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=center.position+center.forward*.45f;target.transform.localScale=Vector3.one*.2f;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);enemy.GetComponent<VehicleEnergyBank>()?.Recharge(1000);Physics.SyncTransforms();enemy.SetCombatActive(true);
        var step=typeof(RRLSawBinding).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
        for(int i=0;i<80&&hp.CurrentHp>=500;i++){binding.SetMotorRequested(true);step.Invoke(binding,null);yield return null;}
        Assert.That(hp.CurrentHp,Is.LessThan(500),"Actual saw contact must damage the target.");Object.Destroy(target);
    }
    [UnityTest] public IEnumerator EnemyEmpModelKeepsPhysicalPulseOriginAndDisruptsTarget()
    {
        yield return StartCombat();var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        var legacy=enemy.transform.Find("Enemy_Yaw/Enemy_Muzzle");Assert.That(enemy.EquipEmp(),Is.True);
        var binding=enemy.WeaponBlock.GetComponent<RRLEmpBinding>();var art=enemy.WeaponBlock.GetComponentInChildren<R4WeaponArt>(true);
        Assert.That(art,Is.Not.Null,"Enemy EMP must show its supplied model.");
        Assert.That(art.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4W_EmpEmitter_ROOT"),Is.True);
        Assert.That(typeof(P12PipelineSafeEnemyTurret).GetField("muzzle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(enemy),Is.SameAs(legacy));
        Assert.That(binding.Module.GetType().GetField("pulseOrigin",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(binding.Module),Is.SameAs(enemy.WeaponBlock.transform));
        var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;body.position=new Vector3(100,4,100);Physics.SyncTransforms();
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=enemy.WeaponBlock.transform.position+Vector3.up*4;
        var receiver=target.AddComponent<EmpReceiver>();receiver.ConfigureOwnership(target.transform,1234,1);
        enemy.GetComponent<VehicleEnergyBank>()?.Recharge(1000);Physics.SyncTransforms();enemy.SetCombatActive(true);
        Assert.That(binding.Fire(),Is.True);Assert.That(receiver.IsDisrupted,Is.True,"Real pulse must disrupt the visible receiver.");
        Assert.That(binding.Fire(),Is.False,"Committed cooldown remains active.");Object.Destroy(target);yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
