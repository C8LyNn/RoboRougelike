using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R6;
using RoboRougelike.VFX001;
using RoboRougelike.VFX001Integration;
using RoboRougelike.Mechanics.COV001;
using RoboRougelike.Mechanics.HAZ001;
using RoboRougelike.Mechanics.Rockets;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;

public class RRLW7VfxTests
{
    R1RunController run;
    Vfx001R6LiveBridge bridge;
    VfxPoolManager pool;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        run=R1RunController.Instance;run.StartRun();run.ChooseRoute(0);
        while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);R1EncounterManager.ClearTransientObjects();
        foreach(var zone in run.Arena.Layout.GetComponentsInChildren<TelegraphedHazardZone>())zone.enabled=false;
        bridge=run.GetComponent<Vfx001R6LiveBridge>();Assert.That(bridge,Is.Not.Null,"Real run must own original VFX pool");
        pool=bridge.GetComponentInChildren<VfxPoolManager>();pool.ClearAll();
    }
    P12PipelineSafeDamageable Armor()=>run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Armor).Damageable;
    void NoVisualPhysics()
    {
        Assert.That(pool.GetComponentsInChildren<Rigidbody>(true),Is.Empty);
        Assert.That(pool.GetComponentsInChildren<Collider>(true),Is.Empty,"VFX fragments and shockwaves are cosmetic only");
    }
    bool HasWave(float green)
    {
        var properties=new MaterialPropertyBlock();
        foreach(var ring in pool.GetComponentsInChildren<VfxShockwaveRing>())
        {
            if(!ring.IsRunning)continue;ring.GetComponent<Renderer>().GetPropertyBlock(properties);
            if(Mathf.Abs(properties.GetColor("_Tint").g-green)<.001f)return true;
        }
        return false;
    }
    [UnityTest] public IEnumerator SettledPartHitImmediatelyEmitsOnceWithoutLegacySphereOrPhysicalMutation()
    {
        var part=Armor();var body=run.Player.GetComponent<Rigidbody>();float hp=part.CurrentHp,mass=body.mass;
        var position=body.position;var rotation=body.rotation;var velocity=body.linearVelocity;var spin=body.angularVelocity;
        int oldFlashes=R6CombatFeedback.Ensure().transform.childCount;
        part.ApplyDamage(10,part.transform.position,Vector3.forward);
        Assert.That(pool.ActiveCount,Is.EqualTo(1),"Read resolved damage directly; do not poll legacy marker names next frame");
        Assert.That(part.CurrentHp,Is.EqualTo(hp-10));Assert.That(body.mass,Is.EqualTo(mass));
        Assert.That(body.position,Is.EqualTo(position));Assert.That(body.rotation,Is.EqualTo(rotation));
        Assert.That(body.linearVelocity,Is.EqualTo(velocity));Assert.That(body.angularVelocity,Is.EqualTo(spin));
        Assert.That(R6CombatFeedback.Ensure().transform.childCount,Is.EqualTo(oldFlashes),"One detailed burst replaces the legacy hit sphere");
        NoVisualPhysics();yield return null;
    }
    [UnityTest] public IEnumerator MuzzleFlashDoesNotBecomeAHitOrExplosion()
    {
        var gun=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Gun);
        R6CombatFeedback.Muzzle(gun.Muzzle,gun.Weapon);yield return null;yield return null;
        Assert.That(pool.ActiveCount,Is.Zero,"Firing without an impact must not create damage VFX");
    }
    [UnityTest] public IEnumerator EnemyCoreDestructionUsesCoreShockwaveWhileOtherEnemyRemains()
    {
        Assert.That(run.Encounter.Enemies.Count,Is.GreaterThan(1));var core=run.Encounter.Enemies[0].Core;
        core.ApplyDamage(100000,core.transform.position,Vector3.up);yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat));
        Assert.That(HasWave(.38f),Is.True,"Actual designated enemy core must use original core preset, not marker-size explosion inference");
        NoVisualPhysics();
    }
    [UnityTest] public IEnumerator ActualCoverAndFragmentEventsRespectDisabledEffectsWithoutReplayingHistory()
    {
        var cover=run.Arena.Layout.GetComponentsInChildren<DestructibleArenaCover>()[0];var collider=cover.GetComponent<Collider>();
        Assert.That(RRLHitRouter.Apply(collider,10,collider.bounds.center,Vector3.forward,run.Player.transform),Is.True);
        Assert.That(cover.Durability,Is.EqualTo(40));Assert.That(pool.ActiveCount,Is.EqualTo(1),"Real cover durability must drive hit feedback");
        var fragment=GameObject.CreatePrimitive(PrimitiveType.Cube);fragment.transform.position=new Vector3(100,4,100);
        var hp=fragment.AddComponent<P12PipelineSafeDamageable>();hp.Configure(20,true);
        int count=pool.ActiveCount;hp.ApplyDamage(25,fragment.transform.position,Vector3.up);
        Assert.That(fragment.GetComponent<R3Debris>(),Is.Not.Null);Assert.That(pool.ActiveCount,Is.EqualTo(count+2),"Destruction and actual separated fragment each emit once");
        bridge.SetVisualEffectsEnabled(false);Assert.That(pool.ActiveCount,Is.Zero);
        Assert.That(RRLHitRouter.Apply(collider,10,collider.bounds.center,Vector3.forward,run.Player.transform),Is.True);
        Assert.That(cover.Durability,Is.EqualTo(30));yield return null;
        bridge.SetVisualEffectsEnabled(true);yield return null;
        Assert.That(pool.ActiveCount,Is.Zero,"Enabling must not replay old impacts or detached debris");
        NoVisualPhysics();
    }
    [UnityTest] public IEnumerator LeavingCombatClearsEffectsAndSceneRestartReinstallsExactlyOneBridge()
    {
        var part=Armor();part.ApplyDamage(10,part.transform.position,Vector3.forward);
        var gun=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Gun);R6CombatFeedback.Muzzle(gun.Muzzle,gun.Weapon);
        foreach(var enemy in run.Encounter.Enemies)enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        while(run.Progress.State==R1RunState.Combat)yield return null;yield return null;
        Assert.That(pool.ActiveCount,Is.Zero,"Reward/garage must not freeze old combat particles");
        Assert.That(R6CombatFeedback.Ensure().transform.Cast<Transform>().Count(t=>t.gameObject.activeSelf),Is.Zero,"Clear old muzzle flashes too");
        run.RestartRun();yield return null;
        while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;
        Assert.That(run.GetComponents<Vfx001R6LiveBridge>().Length,Is.EqualTo(1),"Scene restart must reinstall exactly once");
        pool=run.GetComponentInChildren<VfxPoolManager>();Assert.That(pool.ActiveCount,Is.Zero);
        run.StartRun();run.ChooseRoute(0);while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);
        part=Armor();part.ApplyDamage(10,part.transform.position,Vector3.up);Assert.That(pool.ActiveCount,Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator RealRocketDetonationShowsExplosionEvenWhenDurablePartSurvives()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=R4WeaponKind.RocketLauncher;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        var gun=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*6;target.transform.localScale=new Vector3(2,2,.5f);
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);float ownerHp=gun.Damageable.CurrentHp;
        Physics.SyncTransforms();Assert.That(gun.GetComponent<RocketLauncher>().TryFire(),Is.EqualTo(RocketFireResult.Fired));
        float deadline=Time.time+.6f;while(hp.CurrentHp==500&&Time.time<deadline)yield return null;
        Assert.That(hp.CurrentHp,Is.InRange(440,450));Assert.That(hp.IsDead,Is.False);
        Assert.That(gun.Damageable.CurrentHp,Is.EqualTo(ownerHp));Assert.That(HasWave(.72f),Is.True,"Actual nonlethal rocket blast must produce its own explosion feedback");NoVisualPhysics();
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
