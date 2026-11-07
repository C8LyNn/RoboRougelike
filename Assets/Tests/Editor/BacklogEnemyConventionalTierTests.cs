using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogEnemyConventionalTierTests
{
    R1RunController run;
    P12PipelineSafeEnemyTurret template;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity"); yield return new EnterPlayMode();
        for (int i=0; i<300 && R1RunController.Instance?.Progress==null; i++) yield return null;
        run=R1RunController.Instance; Assert.That(run.StartRun()&&run.ChooseRoute(0), Is.True);
        for (int i=0; i<300 && run.Transitioning; i++) yield return null;
        template=(P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate", BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
    }
    R1EncounterDefinition Definition(RoboRougelike.R5.R5EnemyVariant variant) => new R1EncounterDefinition {
        EnemyCount=1, Roster=new[]{R3EnemyArchetype.Gunner}, Variants=new[]{variant} };
    [UnityTest] public IEnumerator PrimaryAndExtraWeaponsGetRealTierAndPreserveItInSalvage()
    {
        var def=Definition(RoboRougelike.R5.R5EnemyVariant.Normal);
        run.Encounter.Spawn(template,run.Player.transform,def,run.Salvage,"baseline",null,1);
        run.Encounter.SetCombatActive(false); yield return null;
        float baseHp=run.Encounter.Enemies.First().WeaponBlock.MaxHp;
        run.Encounter.Spawn(template,run.Player.transform,def,run.Salvage,"tiered",null,2);
        run.Encounter.SetCombatActive(false); yield return null;
        var enemy=run.Encounter.Enemies.First();
        var primary=enemy.WeaponBlock.GetComponent<RRLDeviceBinding>();
        Assert.That(primary,Is.Not.Null,"Primary physical gun has canonical state.");
        Assert.That(primary.State.Tier,Is.EqualTo(2));
        Assert.That(enemy.WeaponBlock.MaxHp,Is.EqualTo(baseHp*2).Within(.01));
        var extra=enemy.AdditionalWeapons.Single().Block.GetComponent<RRLDeviceBinding>();
        Assert.That(extra,Is.Not.Null);Assert.That(extra.State.Tier,Is.EqualTo(2));
        string mainId=primary.State.Id,extraId=extra.State.Id;
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.zero); yield return null;
        Assert.That(run.Salvage.Items.Single(p=>p.Id==mainId).ToPart().Tier,Is.EqualTo(2));
        Assert.That(run.Salvage.Items.Single(p=>p.Id==extraId).ToPart().Tier,Is.EqualTo(2));
    }
    [UnityTest] public IEnumerator EliteWeaponsUsePhysicalTierAndKeepDistinctLootIdentities()
    {
        var def=Definition(RoboRougelike.R5.R5EnemyVariant.EliteGunner);
        run.Encounter.Spawn(template,run.Player.transform,def,run.Salvage,"elite-tiered",null,3);
        run.Encounter.SetCombatActive(false); yield return null;
        var enemy=run.Encounter.Enemies.First();
        var guns=enemy.VariantWeapons.ToArray(); Assert.That(guns.Length,Is.EqualTo(2));
        var states=guns.Select(g=>g.Block.GetComponent<RRLDeviceBinding>()?.State).ToArray();
        Assert.That(states.All(s=>s!=null&&s.Tier==3),Is.True);
        Assert.That(states.Select(s=>s.Id).Distinct().Count(),Is.EqualTo(2));
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.zero); yield return null;
        foreach(var state in states) Assert.That(run.Salvage.Items.Single(p=>p.Id==state.Id).ToPart().Tier,Is.EqualTo(3));
    }
    [UnityTest] public IEnumerator TierTwoConventionalGunsFireRealProjectilesWithTierDamageAndEnergy()
    {
        var def=Definition(RoboRougelike.R5.R5EnemyVariant.Normal);
        run.Encounter.Spawn(template,run.Player.transform,def,run.Salvage,"shots-tiered",null,2);
        run.Encounter.SetCombatActive(false); yield return null;
        var enemy=run.Encounter.Enemies.First();
        var enemyBody=enemy.GetComponent<Rigidbody>();enemyBody.isKinematic=true;enemyBody.position=new Vector3(100,4,100);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var muzzle=(Transform)typeof(P12PipelineSafeEnemyTurret).GetField("muzzle",flags).GetValue(enemy);
        float baseDamage=(float)typeof(P12PipelineSafeEnemyTurret).GetField("damage",flags).GetValue(enemy);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=muzzle.position+muzzle.forward*6;
        target.transform.localScale=new Vector3(3,3,1);
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        Physics.SyncTransforms();
        typeof(P12PipelineSafeEnemyTurret).GetMethod("FirePhysicalShell",flags).Invoke(enemy,new object[]{target.transform.position});
        float deadline=Time.time+.8f;while(Time.time<deadline&&hp.CurrentHp>=1000)yield return null;
        Assert.That(1000-hp.CurrentHp,Is.EqualTo(baseDamage*2).Within(.1),"Primary physical projectile uses T2 damage.");
        Object.Destroy(target); yield return null;

        var gun=enemy.AdditionalWeapons.Single();
        var extraTarget=GameObject.CreatePrimitive(PrimitiveType.Cube);
        extraTarget.transform.position=gun.Muzzle.position+gun.Muzzle.forward*6;extraTarget.transform.localScale=new Vector3(3,3,1);
        var extraHp=extraTarget.AddComponent<P12PipelineSafeDamageable>();extraHp.Configure(1000,false);
        enemy.RetargetEncounter(extraTarget.transform);
        typeof(P12PipelineSafeEnemyTurret).GetField("currentTarget",flags).SetValue(enemy,extraTarget.GetComponent<Collider>());
        typeof(P12PipelineSafeEnemyTurret).GetField("nextTargetTime",flags).SetValue(enemy,float.MaxValue);
        gun.Pivot.rotation=Quaternion.LookRotation(extraTarget.transform.position-gun.Pivot.position);gun.NextShot=0;
        var bank=enemy.GetComponent<VehicleEnergyBank>();bank.Recharge(1000);float before=bank.Current;
        var list=typeof(P12PipelineSafeEnemyTurret).GetField("additionalGuns",flags).GetValue(enemy);
        Physics.SyncTransforms();typeof(P12PipelineSafeEnemyTurret).GetMethod("UpdateVariantWeapons",flags).Invoke(enemy,new[]{list,"extra-gun/"});
        Assert.That(bank.Current,Is.EqualTo(before-3f).Within(.001),"Automatic T2 uses its physical energy cost.");
        deadline=Time.time+.8f;while(Time.time<deadline&&extraHp.CurrentHp>=1000)yield return null;
        float expected=R4WeaponDefinition.Get(gun.Kind).Damage*baseDamage/R4WeaponDefinition.Get(enemy.WeaponKind).Damage*2;
        Assert.That(1000-extraHp.CurrentHp,Is.EqualTo(expected).Within(.1),"Extra physical projectile uses T2 damage.");
        Object.Destroy(extraTarget);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
