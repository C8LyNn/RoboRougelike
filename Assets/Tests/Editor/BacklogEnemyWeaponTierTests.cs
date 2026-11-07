using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using RoboRougelike.R6;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogEnemyWeaponTierTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity"); yield return new EnterPlayMode();
        for (int i = 0; i < 300 && R1RunController.Instance?.Progress == null; i++) yield return null;
    }
    [UnityTest] public IEnumerator RealEnemyShotgunUsesTierForEnergyDamageDurabilityAndSalvage()
    {
        var run = R1RunController.Instance;
        Assert.That(run.StartRun() && run.ChooseRoute(0), Is.True);
        for (int i = 0; i < 300 && run.Transitioning; i++) yield return null;
        var enemy = run.Encounter.Enemies.First();
        run.Encounter.SetCombatActive(false);
        Assert.That(enemy.EquipShotgun(), Is.True);
        var shotgun = enemy.GetComponentInChildren<RRLShotgunBinding>();
        Assert.That(shotgun, Is.Not.Null);
        var shotgunArt=shotgun.GetComponentInChildren<R4WeaponArt>(true);
        Assert.That(shotgunArt,Is.Not.Null,"Enemy shotgun must display its supplied model.");
        Assert.That(typeof(RRLShotgunBinding).GetField("muzzle",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(shotgun),Is.SameAs(shotgunArt.Muzzle),
            "The physical shotgun must use the visible muzzle.");
        var hp = shotgun.GetComponent<P12PipelineSafeDamageable>();
        float initialMax = hp.MaxHp;
        hp.RestoreHealthRatio(.7f);
        var method = typeof(P12PipelineSafeEnemyTurret).GetMethod("SetPrimaryWeaponTier", new[] { typeof(int) });
        Assert.That(method, Is.Not.Null, "An actual equipped weapon must accept a tier.");
        Assert.That((bool)method.Invoke(enemy, new object[] { 2 }), Is.True);
        Assert.That(shotgun.State.Tier, Is.EqualTo(2));
        Assert.That(hp.MaxHp, Is.EqualTo(initialMax * 2).Within(.01));
        Assert.That(hp.CurrentHp / hp.MaxHp, Is.EqualTo(.7f).Within(.001), "Tier change cannot repair damage.");
        Assert.That(R6VehicleDrive.PartMass(hp), Is.EqualTo(R4WeaponDefinition.Get(R4WeaponKind.Shotgun).Mass * 1.15f).Within(.01));
        Assert.That((bool)method.Invoke(enemy, new object[] { 4 }), Is.False);
        Assert.That(shotgun.State.Tier, Is.EqualTo(2));
        var damageField = typeof(P12PipelineSafeEnemyTurret).GetField("damage", BindingFlags.NonPublic | BindingFlags.Instance);
        float basePelletDamage = (float)damageField.GetValue(enemy);

        var body = enemy.GetComponent<Rigidbody>(); body.isKinematic = true;
        body.position = new Vector3(100, 4, 100); enemy.transform.rotation = Quaternion.identity;
        var muzzleField = typeof(RRLShotgunBinding).GetField("muzzle", BindingFlags.NonPublic | BindingFlags.Instance);
        var muzzle = (Transform)muzzleField.GetValue(shotgun);
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.transform.position = muzzle.position + muzzle.forward * 6; target.transform.localScale = new Vector3(4, 4, 1);
        var targetHp = target.AddComponent<P12PipelineSafeDamageable>(); targetHp.Configure(500, false);
        var bank = enemy.GetComponent<VehicleEnergyBank>(); bank.Recharge(1000); float before = bank.Current;
        Physics.SyncTransforms(); enemy.SetCombatActive(true);
        Assert.That(shotgun.Fire(), Is.True);
        Assert.That(bank.Current, Is.EqualTo(before - 6).Within(.001));
        float deadline = Time.time + .5f; while (Time.time < deadline) yield return null;
        Assert.That(500-targetHp.CurrentHp, Is.EqualTo(8*basePelletDamage*2).Within(.25),
            "Eight physical pellets must each apply the tier-two damage multiplier.");
        enemy.SetCombatActive(false);
        enemy.Core.ApplyDamage(100000, enemy.Core.transform.position, Vector3.zero);
        yield return null;
        var recovered = run.Salvage.Items.Single(p => p.Id == shotgun.State.Id);
        Assert.That(recovered.ToPart().Tier, Is.EqualTo(2));
        Assert.That(recovered.ToPart().Weapon, Is.EqualTo(R4WeaponKind.Shotgun));
        Object.Destroy(target);
    }
    [UnityTest] public IEnumerator LaterEncounterEquipsTierThreeRealShotgunAndDropsIt()
    {
        var run = R1RunController.Instance;
        Assert.That(run.StartRun() && run.ChooseRoute(0), Is.True);
        for (int i = 0; i < 300 && run.Transitioning; i++) yield return null;
        var field = typeof(R1RunController).GetField("enemyTemplate", BindingFlags.NonPublic | BindingFlags.Instance);
        var template = (P12PipelineSafeEnemyTurret)field.GetValue(run);
        var definition = new R1EncounterDefinition { EnemyCount = 2, HealthMultiplier = 1,
            Roster = new[] {R3EnemyArchetype.Scout, R3EnemyArchetype.Scout},
            Variants = new[] {RoboRougelike.R5.R5EnemyVariant.Normal, RoboRougelike.R5.R5EnemyVariant.Normal} };
        run.Encounter.Spawn(template, run.Player.transform, definition, run.Salvage, "tiered-guns", null, 3);
        run.Encounter.SetCombatActive(false);
        yield return null; // Unity Start subscribes the enemy's real core-death handler.
        var enemy = run.Encounter.Enemies.Last();
        var gun = enemy.GetComponentInChildren<RRLShotgunBinding>();
        Assert.That(gun, Is.Not.Null);
        Assert.That(gun.State.Tier, Is.EqualTo(3));
        var stateId = gun.State.Id;
        enemy.Core.ApplyDamage(100000, enemy.Core.transform.position, Vector3.zero);
        yield return null;
        var recovered = run.Salvage.Items.Single(p => p.Id == stateId).ToPart();
        Assert.That(recovered.Weapon, Is.EqualTo(R4WeaponKind.Shotgun));
        Assert.That(recovered.Tier, Is.EqualTo(3));
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if (Application.isPlaying) { Time.timeScale = 1; yield return new ExitPlayMode(); }
    }
}
