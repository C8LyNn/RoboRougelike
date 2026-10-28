// B01-24 / PASS15 — ORIGINAL UNITY QA ONLY, not a canonical gameplay fix.
// Source contract: Codex #310@231005c6d0351945a8ee6684f3856af118f0e94f
// P12_PipelineSafeEnemyTurret.cs blob 8b0cfacbba967ca1a3e3e514e2cd1b49b805946e.
// NEW coverage versus PASS12: the variantGuns > 0 / UpdateVariantWeapons
// branch, NOT the already-authored ordinary turret reacquisition tests.
// No Unity execution has been performed while authoring this source.
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using RogueMachine.P12PipelineSafe;

public sealed class B0124VariantTargetReacquisitionEditModeTests
{
    private readonly List<GameObject> owned = new List<GameObject>();
    private P12PipelineSafeEnemyTurret enemy;
    private GameObject playerRoot;
    private Collider playerTarget;

    private GameObject Track(GameObject value)
    {
        owned.Add(value);
        return value;
    }

    private static P12PipelineSafeDamageable Healthy(GameObject item)
    {
        var health = item.AddComponent<P12PipelineSafeDamageable>();
        health.Configure(100f, false);
        Assert.That(health.IsDead, Is.False, "Control must remain a living part.");
        return health;
    }

    private static FieldInfo PrivateField(string name)
    {
        var result = typeof(P12PipelineSafeEnemyTurret).GetField(
            name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(result, Is.Not.Null, "Original enemy field changed: " + name);
        return result;
    }

    private static MethodInfo PrivateMethod(string name)
    {
        var result = typeof(P12PipelineSafeEnemyTurret).GetMethod(
            name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(result, Is.Not.Null, "Original enemy method changed: " + name);
        return result;
    }

    [SetUp]
    public void SetUp()
    {
        playerRoot = Track(new GameObject("B0124_Variant_PlayerRoot"));
        var part = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
        part.name = "B0124_ActiveInitially";
        part.transform.SetParent(playerRoot.transform, false);
        part.transform.position = new Vector3(0f, 1f, 14.5f);
        part.AddComponent<P12_PipelineSafePlayerBlock>();
        Healthy(part);
        playerTarget = part.GetComponent<Collider>();

        var enemyRoot = Track(new GameObject("B0124_Variant_EnemyRoot"));
        enemy = enemyRoot.AddComponent<P12PipelineSafeEnemyTurret>();
        var core = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
        core.transform.SetParent(enemyRoot.transform, false);
        core.transform.position = new Vector3(-20f, 1f, 0f);
        var coreHp = Healthy(core);

        var support = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
        support.transform.SetParent(enemyRoot.transform, false);
        support.transform.position = new Vector3(-18f, 1f, 0f);
        var supportHp = Healthy(support);

        var gunBlock = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
        gunBlock.transform.SetParent(enemyRoot.transform, false);
        gunBlock.transform.position = new Vector3(-16f, 1f, 0f);
        var gunHp = Healthy(gunBlock);

        var gunPivot = Track(new GameObject("B0124_Variant_Pivot")).transform;
        gunPivot.SetParent(enemyRoot.transform, false);
        gunPivot.position = new Vector3(0f, 1f, 0f);
        var muzzle = Track(new GameObject("B0124_Variant_Muzzle")).transform;
        muzzle.SetParent(gunPivot, false);
        muzzle.position = gunPivot.position;

        // Exact public API + public nested VariantGun signature from original
        // P12PipelineSafeEnemyTurret, not a fake subclass/mirrored selector.
        enemy.Configure(playerRoot.transform, null, null, enemyRoot.transform, coreHp, null);
        var variant = new P12PipelineSafeEnemyTurret.VariantGun
        {
            Block = gunHp,
            Support = supportHp,
            Pivot = gunPivot,
            Muzzle = muzzle,
            NextShot = float.PositiveInfinity
        };
        var guns = (List<P12PipelineSafeEnemyTurret.VariantGun>)
            PrivateField("variantGuns").GetValue(enemy);
        Assert.That(guns, Is.Not.Null);
        guns.Add(variant);
        Assert.That(variant.Online(enemyRoot.transform), Is.True,
            "The test must exercise the ONLINE variant branch, not an early exit.");

        // Zero range excludes projectile emission while leaving the real
        // target-cache and variant combat Update path active.
        PrivateField("maxRange").SetValue(enemy, 0f);
        enemy.SetCombatActive(true);

        // Actual original selection is the test's starting positive control.
        var selected = (Collider)PrivateMethod("SelectTargetCollider").Invoke(enemy, null);
        Assert.That(selected, Is.SameAs(playerTarget),
            "The active living target must first be acquired by original selector.");
        PrivateField("currentTarget").SetValue(enemy, selected);
        PrivateField("nextTargetTime").SetValue(enemy, float.PositiveInfinity);
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) UnityEngine.Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }

    [Test]
    public void ActiveCachedTarget_RemainsSelectableDuringVariantCombatUpdate_Positive()
    {
        Assert.That(playerTarget.gameObject.activeInHierarchy, Is.True);
        PrivateMethod("Update").Invoke(enemy, null);
        var cached = (Collider)PrivateField("currentTarget").GetValue(enemy);
        Assert.That(cached, Is.SameAs(playerTarget),
            "A valid active target must not be spuriously discarded.");
        Assert.That(enemy.VariantWeapons.Count, Is.EqualTo(1));
    }

    [Test]
    public void InactiveCachedTarget_IsDroppedDuringVariantCombatUpdate_Negative()
    {
        // Same alive and collider.enabled target as the positive control,
        // except its GameObject is disabled AFTER it was actually selected.
        Assert.That(playerTarget.enabled, Is.True);
        playerTarget.gameObject.SetActive(false);
        Assert.That(playerTarget.gameObject.activeInHierarchy, Is.False);
        Assert.That(playerTarget.enabled, Is.True);
        Assert.That(enemy.IsDefeated, Is.False);
        Assert.That(enemy.CombatActive, Is.True);
        Assert.That(enemy.VariantWeapons.Count, Is.EqualTo(1));

        // Calls the actual original Update() -> UpdateVariantWeapons path.
        PrivateMethod("Update").Invoke(enemy, null);
        var cached = (Collider)PrivateField("currentTarget").GetValue(enemy);
        TestContext.WriteLine(
            "B0124 VARIANT target eligibility: activeInHierarchy=" +
            playerTarget.gameObject.activeInHierarchy +
            "; collider.enabled=" + playerTarget.enabled +
            "; cached=" + (cached != null ? cached.name : "<null>") +
            "; source=Codex310@231005c6d0351945a8ee6684f3856af118f0e94f");

        // Expected RED on unpatched Codex #310 source: the variant branch
        // only invalidates on null / Collider.enabled=false / 0.8s expiry;
        // it does not check Collider.gameObject.activeInHierarchy.
        // This is an expected-RED acceptance condition, NOT a measured FAIL.
        Assert.That(cached, Is.Null,
            "Variant enemy must immediately drop a cached collider on an inactive player child.");
    }
}
