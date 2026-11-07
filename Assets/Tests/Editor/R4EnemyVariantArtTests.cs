using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using RoboRougelike.R5;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4EnemyVariantArtTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
    }
    [UnityTest] public IEnumerator EliteHeavyUsesVisibleMuzzleAndAutomaticKeepsFallback()
    {
        var run=R1RunController.Instance;Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        float end=Time.realtimeSinceStartup+20;while(run.Transitioning&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That(run.Transitioning,Is.False);
        var template=(P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        var def=new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner},Variants=new[]{R5EnemyVariant.EliteGunner}};
        run.Encounter.Spawn(template,run.Player.transform,def,run.Salvage,"r4-enemy-variant-art");
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First(e=>e.Variant==R5EnemyVariant.EliteGunner);
        var heavy=enemy.VariantWeapons.Single(w=>w.Kind==R4WeaponKind.Heavy);
        var automatic=enemy.VariantWeapons.Single(w=>w.Kind==R4WeaponKind.Automatic);
        var art=heavy.Block.GetComponent<R4WeaponArt>();
        Assert.That(art,Is.Not.Null,"Elite Heavy must display the supplied weapon model.");
        Assert.That(heavy.Muzzle,Is.SameAs(art.Muzzle),"The physical shell must start at the visible muzzle.");
        Assert.That(heavy.Pivot.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled),Is.True,"The physical gun collider remains.");
        var visualRoot=heavy.Block.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="R4W_Heavy_ROOT");
        Assert.That(visualRoot.GetComponentsInChildren<Collider>(true),Is.Empty);
        Assert.That(automatic.Block.GetComponent<R4WeaponArt>(),Is.Null,"No Automatic source model was supplied.");
        Assert.That(automatic.Pivot.GetComponentsInChildren<Renderer>(true).Any(r=>r.enabled),Is.True);
        var direction=new Vector3(.4f,.15f,1).normalized;heavy.Pivot.rotation=Quaternion.LookRotation(direction);
        art.Sync();Assert.That(Vector3.Dot(heavy.Muzzle.forward,direction),Is.GreaterThan(.99f),"Visible muzzle follows the real turret aim.");
        // Invoke the existing firing path immediately after moving the physical
        // pivot, before LateUpdate, so a one-frame-old visual muzzle cannot pass.
        foreach(var other in run.Encounter.Enemies.Where(e=>e!=enemy))other.gameObject.SetActive(false);
        R1EncounterManager.ClearTransientObjects();
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.SetParent(run.Player.transform,true);
        target.transform.position=heavy.Pivot.position+direction*12;
        var targetCollider=target.GetComponent<Collider>();Physics.SyncTransforms();
        var type=typeof(P12PipelineSafeEnemyTurret);
        type.GetField("currentTarget",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemy,targetCollider);
        type.GetField("nextTargetTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemy,Time.time+30);
        heavy.Pivot.rotation=Quaternion.LookRotation(-direction);art.Sync();
        heavy.Pivot.rotation=Quaternion.LookRotation(direction);heavy.NextShot=0;
        var selected=new System.Collections.Generic.List<P12PipelineSafeEnemyTurret.VariantGun>{heavy};
        type.GetMethod("UpdateVariantWeapons",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(enemy,new object[]{selected,"gun/"});
        var shell=Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Single(p=>p.PartId==enemy.EquipmentInstanceId("gun/0"));
        var expected=heavy.Muzzle.position+(targetCollider.bounds.center-heavy.Muzzle.position).normalized*.25f;
        Assert.That(Vector3.Distance(shell.transform.position,expected),Is.LessThan(.001f),"Real shell starts at the current visible muzzle.");
        Assert.That(Vector3.Dot(heavy.Muzzle.forward,direction),Is.GreaterThan(.99f),"Firing path synchronizes visual aim immediately.");
        Object.Destroy(shell.gameObject);Object.Destroy(target);
        var rotor=visualRoot;heavy.Block.ApplyDamage(10000,heavy.Block.transform.position,Vector3.up);yield return null;
        Assert.That(heavy.Block.IsDead,Is.True);Assert.That(rotor.IsChildOf(heavy.Block.transform),Is.True,"The appearance follows the damaged part.");
        Assert.That(heavy.Online(enemy.transform),Is.False);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
