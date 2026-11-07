using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R6;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogEnemyWheelTierTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
    }
    [UnityTest] public IEnumerator TieredEnemyWheelsRemainPhysicalAndKeepTheirTierInSalvage()
    {
        var run=R1RunController.Instance;Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        for(int i=0;i<300&&run.Transitioning;i++)yield return null;
        var template=(P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(run);
        var def=new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Scout},Variants=new[]{RoboRougelike.R5.R5EnemyVariant.Normal}};
        run.Encounter.Spawn(template,run.Player.transform,def,run.Salvage,"tiered-wheels",null,2);
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();
        var wheels=enemy.GetComponentsInChildren<P12PipelineSafeDamageable>().Where(R6VehicleDrive.IsWheel).ToArray();
        Assert.That(wheels.Length,Is.GreaterThanOrEqualTo(4));
        var states=wheels.Select(w=>w.GetComponent<RRLDeviceBinding>()?.State).ToArray();
        Assert.That(states.All(s=>s!=null&&s.Kind==R3PartKind.Wheel&&s.Tier==2),Is.True);
        Assert.That(states.Select(s=>s.Id).Distinct().Count(),Is.EqualTo(wheels.Length));
        Assert.That(R6VehicleDrive.PartMass(wheels[0]),Is.EqualTo(13.8f).Within(.01));
        Assert.That(wheels[0].MaxHp,Is.GreaterThan(0));
        var other=run.Encounter.Enemies.Last();
        var otherWheel=other.GetComponentsInChildren<P12PipelineSafeDamageable>().First(R6VehicleDrive.IsWheel);
        float t2Max=otherWheel.MaxHp;otherWheel.RestoreHealthRatio(.6f);
        other.SetWheelTiers(3);
        Assert.That(otherWheel.MaxHp,Is.EqualTo(t2Max*1.8f).Within(.01));
        Assert.That(otherWheel.CurrentHp/otherWheel.MaxHp,Is.EqualTo(.6f).Within(.001));
        Assert.That(R6VehicleDrive.PartMass(otherWheel),Is.EqualTo(15.6f).Within(.01));
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.zero);yield return null;
        foreach(var state in states)Assert.That(run.Salvage.Items.Single(p=>p.Id==state.Id).ToPart().Tier,Is.EqualTo(2));
    }
    [UnityTest] public IEnumerator GroundedEnemyUsesTieredWheelTractionOnActualPhysicsSurface()
    {
        var run=R1RunController.Instance;Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        for(int i=0;i<300&&run.Transitioning;i++)yield return null;
        var enemy=run.Encounter.Enemies.First();run.Encounter.SetCombatActive(false);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(60,1,60);
        var body=enemy.GetComponent<Rigidbody>();body.position=new Vector3(100,1,100);body.rotation=Quaternion.identity;
        body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
        var drive=enemy.GetComponent<R6VehicleDrive>();
        float deadline=Time.realtimeSinceStartup+8;
        while(drive.GroundedWheels<2&&Time.realtimeSinceStartup<deadline){yield return new WaitForFixedUpdate();drive.RefreshMass();}
        Assert.That(drive.GroundedWheels,Is.GreaterThanOrEqualTo(2),"Real enemy wheels must touch the surface.");
        enemy.SetWheelTiers(1);drive.RefreshMass();float firstTier=drive.AvailableForce;int contacts=drive.GroundedWheels;
        enemy.SetWheelTiers(2);drive.RefreshMass();
        Assert.That(drive.GroundedWheels,Is.EqualTo(contacts));
        Assert.That(drive.AvailableForce,Is.EqualTo(firstTier*1.35f).Within(.05),"Existing R6 contact solver must apply T2 traction.");
        enemy.SetWheelTiers(3);drive.RefreshMass();
        Assert.That(drive.AvailableForce,Is.EqualTo(firstTier*1.8f).Within(.05),"Existing R6 contact solver must apply T3 traction.");
        Object.Destroy(floor);
    }
    [UnityTest] public IEnumerator ExpiredEliteWheelAndWeaponKeepTierAndIdentityInSalvage()
    {
        var run=R1RunController.Instance;Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        for(int i=0;i<300&&run.Transitioning;i++)yield return null;
        var template=(P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(run);
        var ledger=new R4SalvageLedger(3);
        var def=new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner},Variants=new[]{RoboRougelike.R5.R5EnemyVariant.EliteGunner}};
        run.Encounter.Spawn(template,run.Player.transform,def,ledger,"tier-expiry");run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();enemy.SetWheelTiers(3);enemy.SetOtherWeaponTiers(3);
        var wheels=(List<P12PipelineSafeDamageable>)typeof(P12PipelineSafeEnemyTurret).GetField("driveWheels",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(enemy);
        var wheel=wheels[0];var gun=enemy.VariantWeapons[0].Block;
        var wheelState=wheel.GetComponent<RRLDeviceBinding>().State;var gunState=gun.GetComponent<RRLDeviceBinding>().State;
        Assert.That(wheelState.Tier,Is.EqualTo(3));Assert.That(gunState.Tier,Is.EqualTo(3));
        string wheelId=wheelState.Id,gunId=gunState.Id;
        wheel.ApplyDamage(100000,wheel.transform.position,Vector3.up);gun.ApplyDamage(100000,gun.transform.position,Vector3.up);
        Object.Destroy(wheel.gameObject);Object.Destroy(gun.gameObject);yield return null;yield return null;
        Assert.That(wheel==null&&gun==null,Is.True);Assert.That(enemy.IsDefeated,Is.False);
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        var wheelLoot=ledger.Items.Single(p=>p.Id==wheelId).ToPart();var gunLoot=ledger.Items.Single(p=>p.Id==gunId).ToPart();
        Assert.That(wheelLoot.Health,Is.Zero);Assert.That(wheelLoot.Tier,Is.EqualTo(3));
        Assert.That(gunLoot.Health,Is.Zero);Assert.That(gunLoot.Tier,Is.EqualTo(3));
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
