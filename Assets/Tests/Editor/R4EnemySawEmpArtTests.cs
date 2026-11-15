using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.Mechanics.EMP;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using RoboRougelike.R5;
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
        // Switching disables the previous gun immediately but Unity destroys it
        // at end of frame; select the active art, not the stale inactive model.
        var art=enemy.WeaponBlock.GetComponentsInChildren<R4WeaponArt>(true).SingleOrDefault(a=>a.gameObject.activeInHierarchy);
        Assert.That(art,Is.Not.Null,"Enemy saw must show its supplied model.");
        Assert.That(art.SawRotor,Is.Not.Null);
        Assert.That(typeof(RRLSawBinding).GetField("blade",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(binding),Is.SameAs(center));
        Assert.That(typeof(RRLSawBinding).GetField("rotor",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(binding),Is.SameAs(art.SawRotor));
        Assert.That(Vector3.Distance(art.SawRotor.position,center.position),Is.LessThan(.001f));
        var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;enemy.transform.SetPositionAndRotation(new Vector3(100,4,100),Quaternion.identity);Physics.SyncTransforms();art.Sync();
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=center.position+center.forward*.45f;target.transform.localScale=Vector3.one*.2f;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);enemy.GetComponent<VehicleEnergyBank>()?.Recharge(1000);Physics.SyncTransforms();enemy.SetCombatActive(true);
        var step=typeof(RRLSawBinding).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
        for(int i=0;i<80&&hp.CurrentHp>=500;i++){binding.SetMotorRequested(true);step.Invoke(binding,null);yield return null;}
        Assert.That(hp.CurrentHp,Is.LessThan(500),"Actual saw contact must damage the target.");Object.Destroy(target);
    }
    [UnityTest] public IEnumerator SwitchingToSawBindsActiveRotorAndMissingModelsKeepLegacyGeometry()
    {
        yield return StartCombat();var run=R1RunController.Instance;
        var template=(P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        var def=new R1EncounterDefinition{EnemyCount=2,Roster=new[]{R3EnemyArchetype.Flanker,R3EnemyArchetype.Bruiser},Variants=new[]{R5EnemyVariant.Normal,R5EnemyVariant.Normal}};
        run.Encounter.Spawn(template,run.Player.transform,def,run.Salvage,"r4-saw-switch");run.Encounter.SetCombatActive(false);yield return null;
        var enemies=run.Encounter.Enemies.Take(2).ToArray();Assert.That(enemies.Length,Is.EqualTo(2));
        var enemy=enemies.First(e=>e.WeaponKind==R4WeaponKind.Basic);
        Assert.That(enemy.EquipSaw(),Is.True);
        var binding=enemy.WeaponBlock.GetComponent<RRLSawBinding>();var art=enemy.WeaponBlock.GetComponentsInChildren<R4WeaponArt>(false).Single();
        Assert.That(typeof(RRLSawBinding).GetField("rotor",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(binding),Is.SameAs(art.SawRotor),"Switch must bind the active saw, not the previous inactive gun.");
        var catalog=Resources.Load<R4WeaponArtCatalog>("R4Art/WeaponCatalog");var original=catalog.Models;catalog.Models=(R4WeaponArtEntry[])original.Clone();
        try
        {
            for(int i=0;i<catalog.Models.Length;i++)if(catalog.Models[i].Kind==R4WeaponKind.RotarySaw)
                catalog.Models[i]=new R4WeaponArtEntry{Kind=catalog.Models[i].Kind,Model=null};
            var other=enemies.First(e=>e!=enemy);var legacy=other.transform.Find("Enemy_Yaw/Enemy_Muzzle");
            Assert.That(other.EquipSaw(),Is.True);var fallback=other.WeaponBlock.GetComponent<RRLSawBinding>();
            Assert.That(typeof(RRLSawBinding).GetField("blade",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(fallback),Is.SameAs(legacy));
            Assert.That(typeof(RRLSawBinding).GetField("rotor",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(fallback),Is.Not.Null);
            Assert.That(other.WeaponBlock.GetComponent<Renderer>().enabled,Is.True);
        }
        finally{catalog.Models=original;}
        yield return null;
    }
    [UnityTest] public IEnumerator MissingAuraModelPreservesLegacyRenderAndWeaponBinding()
    {
        yield return StartCombat();var enemy=R1RunController.Instance.Encounter.Enemies.First();
        var catalog=Resources.Load<R4WeaponArtCatalog>("R4Art/WeaponCatalog");Assert.That(catalog,Is.Not.Null);
        var original=catalog.Models;catalog.Models=(R4WeaponArtEntry[])original.Clone();
        try
        {
            for(int i=0;i<catalog.Models.Length;i++)if(catalog.Models[i].Kind==R4WeaponKind.EmpEmitter)
                catalog.Models[i]=new R4WeaponArtEntry{Kind=R4WeaponKind.EmpEmitter,Model=null};
            var legacy=enemy.transform.Find("Enemy_Yaw/Enemy_Muzzle");Assert.That(enemy.EquipEmp(),Is.True);
            var binding=enemy.WeaponBlock.GetComponent<RRLDamageAuraBinding>();
            Assert.That(typeof(P12PipelineSafeEnemyTurret).GetField("muzzle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(enemy),Is.SameAs(legacy));
            Assert.That(binding.Device.Owner,Is.SameAs(enemy.transform));
            Assert.That(enemy.WeaponBlock.GetComponent<Renderer>().enabled,Is.True);
        }
        finally{catalog.Models=original;}
        yield return null;
    }
    [UnityTest] public IEnumerator EnemyAuraModelKeepsArtAndDamagesPlayerWithoutEmp()
    {
        yield return StartCombat();var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        var legacy=enemy.transform.Find("Enemy_Yaw/Enemy_Muzzle");Assert.That(enemy.EquipEmp(),Is.True);
        var binding=enemy.WeaponBlock.GetComponent<RRLDamageAuraBinding>();var art=enemy.WeaponBlock.GetComponentsInChildren<R4WeaponArt>(true).SingleOrDefault(v=>v.gameObject.activeInHierarchy);
        Assert.That(art,Is.Not.Null,"Enemy aura must show its supplied model.");
        Assert.That(art.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4W_EmpEmitter_ROOT"),Is.True);
        Assert.That(typeof(P12PipelineSafeEnemyTurret).GetField("muzzle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(enemy),Is.SameAs(legacy));
        Assert.That(binding.Device.Owner,Is.SameAs(enemy.transform));
        var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;body.position=new Vector3(100,4,100);
        var playerBody=run.Player.GetComponent<Rigidbody>();playerBody.isKinematic=true;playerBody.position=new Vector3(100,4,104);Physics.SyncTransforms();
        var hp=run.Player.GetComponentsInChildren<P12PipelineSafeDamageable>();float before=hp.Sum(p=>p.CurrentHp);
        enemy.GetComponent<VehicleEnergyBank>()?.Recharge(1000);enemy.SetCombatActive(true);
        Assert.That(binding.Fire(),Is.True);for(int i=0;i<8;i++)yield return new WaitForFixedUpdate();
        Assert.That(hp.Sum(p=>p.CurrentHp),Is.LessThan(before),"The actual enemy aura must damage the player.");
        Assert.That(run.Player.GetComponentsInChildren<EmpReceiver>().Any(r=>r.IsDisrupted),Is.False);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
