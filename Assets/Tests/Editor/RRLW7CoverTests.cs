using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.COV001;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;

public class RRLW7CoverTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;run.StartRun();run.ChooseRoute(0);
        while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);
    }
    [UnityTest] public IEnumerator RealArenaUsesOriginalCoverDurabilityWithoutVehicleHpAndRebuildsCleanly()
    {
        var run=R1RunController.Instance;
        var covers=run.Arena.Layout.GetComponentsInChildren<DestructibleArenaCover>();
        Assert.That(covers,Is.Not.Empty,"COV001 must be attached to real Gauntlet obstacles");
        Assert.That(covers.All(c=>c.GetComponentInParent<P12PipelineSafeDamageable>()==null),Is.True,"Cover uses original durability without a second HP pool");
        var cover=covers[0];var collider=cover.GetComponent<Collider>();
        Assert.That(RRLHitRouter.Apply(collider,1000,collider.bounds.center,Vector3.forward,run.Player.transform),Is.True);
        Assert.That(cover.Broken,Is.True);Assert.That(collider.enabled,Is.False);
        Assert.That(cover.GetComponent<Renderer>().enabled,Is.False);
        cover.ResetCover();Assert.That(collider.enabled,Is.True);Assert.That(cover.GetComponent<Renderer>().enabled,Is.True);
        var old=run.Arena.Layout;run.Arena.Build(run.Arena.Kind);yield return null;
        Assert.That(old==null||!old.gameObject.activeInHierarchy,Is.True);
        Assert.That(run.Arena.Layout.GetComponentsInChildren<DestructibleArenaCover>().All(c=>!c.Broken&&c.Durability==c.MaxDurability),Is.True);
    }
    [UnityTest] public IEnumerator ActualPlayerProjectileDamagesOnlyContactedCover()
    {
        var run=R1RunController.Instance;
        var cover=run.Arena.Layout.GetComponentsInChildren<DestructibleArenaCover>().OrderBy(c=>Vector3.Distance(run.Player.transform.position,c.transform.position)).FirstOrDefault();
        Assert.That(cover,Is.Not.Null,"Real arena cover binding missing");
        var others=run.Arena.Layout.GetComponentsInChildren<DestructibleArenaCover>().Where(c=>c!=cover).ToArray();
        var gun=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Gun);
        var collider=cover.GetComponent<Collider>();
        gun.GunPivot.rotation=Quaternion.LookRotation(collider.bounds.center-gun.Muzzle.position);
        Physics.SyncTransforms();int accepted=0;cover.DamageAccepted+=amount=>accepted++;
        run.Player.Controller.FireFrom(gun.Muzzle,gun.Weapon,gun.Id);
        float deadline=Time.time+2;while(cover.Durability==cover.MaxDurability&&Time.time<deadline)yield return null;
        Assert.That(cover.Durability,Is.LessThan(cover.MaxDurability));Assert.That(accepted,Is.EqualTo(1));
        Assert.That(others.All(c=>c.Durability==c.MaxDurability),Is.True,"Nearby covers must not receive proxy splash from a normal shell");
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
