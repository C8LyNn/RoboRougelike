using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogEnemyBatteryTierTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity"); yield return new EnterPlayMode();
        for (int i = 0; i < 300 && R1RunController.Instance?.Progress == null; i++) yield return null;
    }
    [UnityTest] public IEnumerator EnemyTierTwoBatteryHasRealCapacityDurabilityAndDropsItsTier()
    {
        var run = R1RunController.Instance;
        Assert.That(run.StartRun() && run.ChooseRoute(0), Is.True);
        for (int i = 0; i < 300 && run.Transitioning; i++) yield return null;
        var enemy = run.Encounter.Enemies.First();
        run.Encounter.SetCombatActive(false);
        var equip = typeof(P12PipelineSafeEnemyTurret).GetMethod("EquipBattery", new[] {typeof(int)});
        Assert.That(equip, Is.Not.Null, "Enemy equipment must accept the actual battery tier.");
        Assert.That((bool)equip.Invoke(enemy, new object[] {2}), Is.True);
        var battery = enemy.GetComponentInChildren<VehicleEnergyBattery>();
        var hp = battery.GetComponent<P12PipelineSafeDamageable>();
        var bank = enemy.GetComponent<VehicleEnergyBank>(); bank.Refresh();
        Assert.That(hp.MaxHp, Is.EqualTo(200).Within(.01));
        Assert.That(R6VehicleDrive.PartMass(hp), Is.EqualTo(9.2f).Within(.01));
        Assert.That(bank.Maximum, Is.EqualTo(700).Within(.01));
        enemy.Core.ApplyDamage(100000, enemy.Core.transform.position, Vector3.zero);
        yield return null;
        var item = run.Salvage.Items.Single(p => p.Kind == R3PartKind.Battery && p.ToPart().Tier == 2);
        Assert.That(item.ToPart().Tier, Is.EqualTo(2));
    }
    [UnityTest] public IEnumerator EncounterSpawnsFullyChargedTierTwoBatteryAndLosesCapacityWhenDestroyed()
    {
        var run = R1RunController.Instance;
        Assert.That(run.StartRun() && run.ChooseRoute(0), Is.True);
        for (int i = 0; i < 300 && run.Transitioning; i++) yield return null;
        var field = typeof(R1RunController).GetField("enemyTemplate", BindingFlags.NonPublic | BindingFlags.Instance);
        var template = (P12PipelineSafeEnemyTurret)field.GetValue(run);
        run.Encounter.Spawn(template, run.Player.transform, run.CurrentEncounter, run.Salvage, "tier-fixture", null, 2);
        run.Encounter.SetCombatActive(false);
        var enemy = run.Encounter.Enemies.Last();
        var battery = enemy.GetComponentInChildren<VehicleEnergyBattery>();
        var bank = enemy.GetComponent<VehicleEnergyBank>(); bank.Refresh();
        Assert.That(battery.Tier, Is.EqualTo(2));
        Assert.That(bank.Maximum, Is.EqualTo(700));
        Assert.That(bank.Current, Is.EqualTo(700), "Spawned enemies start with a full battery.");
        var hp = battery.GetComponent<P12PipelineSafeDamageable>();
        hp.ApplyDamage(100000, battery.transform.position, Vector3.up);
        bank.Refresh();
        Assert.That(bank.Maximum, Is.EqualTo(500));
        Assert.That(bank.Current, Is.LessThanOrEqualTo(500));
        var other = run.Encounter.Enemies.First();
        Assert.That(other.EquipBattery(3), Is.True);
        var tier3 = other.GetComponentInChildren<VehicleEnergyBattery>();
        var tier3hp = tier3.GetComponent<P12PipelineSafeDamageable>();
        var otherBank = other.GetComponent<VehicleEnergyBank>(); otherBank.Refresh();
        Assert.That(otherBank.Maximum, Is.EqualTo(900));
        Assert.That(tier3hp.MaxHp, Is.EqualTo(360).Within(.01));
        Assert.That(R6VehicleDrive.PartMass(tier3hp), Is.EqualTo(10.4f).Within(.01));
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if (Application.isPlaying) { Time.timeScale = 1; yield return new ExitPlayMode(); }
    }
}
