using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
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

public sealed class R4EnemyPrimaryArtTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
    }
    [UnityTest] public IEnumerator BasicAndHeavyArtKeepPhysicsAndSwapSafelyToShotgun()
    {
        var run=R1RunController.Instance;Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        float end=Time.realtimeSinceStartup+20;while(run.Transitioning&&Time.realtimeSinceStartup<end)yield return null;
        var template=(P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        var def=new R1EncounterDefinition{EnemyCount=2,Roster=new[]{R3EnemyArchetype.Flanker,R3EnemyArchetype.Bruiser},Variants=new[]{R5EnemyVariant.Normal,R5EnemyVariant.Normal}};
        run.Encounter.Spawn(template,run.Player.transform,def,run.Salvage,"r4-primary-art");run.Encounter.SetCombatActive(false);yield return null;
        var basic=run.Encounter.Enemies.First(e=>e.WeaponKind==R4WeaponKind.Basic);
        var heavy=run.Encounter.Enemies.First(e=>e.WeaponKind==R4WeaponKind.Heavy);
        var muzzleField=typeof(P12PipelineSafeEnemyTurret).GetField("muzzle",BindingFlags.Instance|BindingFlags.NonPublic);
        foreach(var enemy in new[]{basic,heavy})
        {
            var gun=enemy.WeaponBlock;var art=gun.GetComponentInChildren<R4WeaponArt>(true);
            Assert.That(art,Is.Not.Null,enemy.WeaponKind+" must display its source model");
            Assert.That(muzzleField.GetValue(enemy),Is.SameAs(art.Muzzle));
            Assert.That(gun.GetComponent<Collider>().enabled,Is.True,"Original collider remains enabled.");
            Assert.That(gun.GetComponent<Renderer>().enabled,Is.False,"Do not draw the old barrel over the new art.");
            var root=gun.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="R4W_"+enemy.WeaponKind+"_ROOT");
            Assert.That(root.GetComponentsInChildren<Collider>(true),Is.Empty);
        }
        var originalMuzzle=basic.transform.Find("Enemy_Yaw/Enemy_Muzzle");Assert.That(originalMuzzle,Is.Not.Null);
        Assert.That(basic.EquipShotgun(),Is.True,"A later equipment switch remains valid.");
        var binding=basic.WeaponBlock.GetComponent<RRLShotgunBinding>();Assert.That(binding,Is.Not.Null);
        var boundMuzzle=typeof(RRLShotgunBinding).GetField("muzzle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(binding);
        Assert.That(boundMuzzle,Is.SameAs(originalMuzzle),"The new weapon may not reference a destroyed visual muzzle.");
        Assert.That(muzzleField.GetValue(basic),Is.SameAs(originalMuzzle));
        Assert.That(basic.WeaponBlock.GetComponent<Renderer>().enabled,Is.True,"Fallback appears when replacing the model.");
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
