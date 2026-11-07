using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4EnemyRailgunArtTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
    }
    [UnityTest] public IEnumerator EnemyRailgunUsesVisibleMuzzleAndPhysicalProjectile()
    {
        var run=R1RunController.Instance;Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        float end=Time.realtimeSinceStartup+20;while(run.Transitioning&&Time.realtimeSinceStartup<end)yield return null;
        run.Encounter.SetCombatActive(false);var enemy=run.Encounter.Enemies.First();
        Assert.That(enemy.EquipRailgun(),Is.True);
        var binding=enemy.WeaponBlock.GetComponent<RRLRailgunBinding>();Assert.That(binding,Is.Not.Null);
        var art=enemy.WeaponBlock.GetComponentInChildren<R4WeaponArt>(true);
        Assert.That(art,Is.Not.Null,"Enemy railgun must display its supplied model.");
        Assert.That(art.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4W_Railgun_ROOT"),Is.True);
        Assert.That(typeof(RRLRailgunBinding).GetField("muzzle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(binding),Is.SameAs(art.Muzzle));
        Assert.That(enemy.WeaponBlock.GetComponent<Collider>().enabled,Is.True);
        var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;body.position=new Vector3(100,4,100);enemy.transform.rotation=Quaternion.identity;art.Sync();
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=art.Muzzle.position+art.Muzzle.forward*6;
        target.transform.localScale=new Vector3(4,4,1);var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);
        var bank=enemy.GetComponent<VehicleEnergyBank>();bank.Recharge(1000);float before=bank.Current;Physics.SyncTransforms();enemy.SetCombatActive(true);
        Assert.That(binding.Fire(),Is.True);Assert.That(bank.Current,Is.LessThan(before),"Railgun spends the existing energy cost.");
        end=Time.realtimeSinceStartup+5;while(hp.CurrentHp>=500&&Time.realtimeSinceStartup<end)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.LessThan(500),"Actual railgun projectile must hit the target.");
        var rotor=art.transform;enemy.WeaponBlock.ApplyDamage(10000,enemy.WeaponBlock.transform.position,Vector3.up);yield return null;
        Assert.That(rotor.IsChildOf(enemy.WeaponBlock.transform),Is.True,"Visual follows detached weapon.");
        Assert.That(binding.Device.Available,Is.False);Object.Destroy(target);
    }
    [UnityTest] public IEnumerator MissingRailgunModelKeepsOriginalMuzzleAndRenderer()
    {
        var run=R1RunController.Instance;Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        float end=Time.realtimeSinceStartup+20;while(run.Transitioning&&Time.realtimeSinceStartup<end)yield return null;
        var enemy=run.Encounter.Enemies.First();var legacy=enemy.transform.Find("Enemy_Yaw/Enemy_Muzzle");
        var catalog=Resources.Load<R4WeaponArtCatalog>("R4Art/WeaponCatalog");Assert.That(catalog,Is.Not.Null);
        var original=catalog.Models;catalog.Models=(R4WeaponArtEntry[])original.Clone();
        try
        {
            for(int i=0;i<catalog.Models.Length;i++)if(catalog.Models[i].Kind==R4WeaponKind.Railgun)
                catalog.Models[i]=new R4WeaponArtEntry{Kind=R4WeaponKind.Railgun,Model=null};
            Assert.That(enemy.EquipRailgun(),Is.True);var binding=enemy.WeaponBlock.GetComponent<RRLRailgunBinding>();
            Assert.That(typeof(RRLRailgunBinding).GetField("muzzle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(binding),Is.SameAs(legacy));
            Assert.That(enemy.WeaponBlock.GetComponent<Renderer>().enabled,Is.True);
        }
        finally{catalog.Models=original;}
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
