// B01-24 W62/PASS12 — QA ONLY, run on an isolated Unity 6000.3.14f1 host.
// Original production under test: Codex #310@231005c6d0351945a8ee6684f3856af118f0e94f.
// This file is NOT a production AI fix and does not alter Codex's branch.
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using RogueMachine.P12PipelineSafe;

public sealed class B0124EnemyAiNegativeEditModeTests
{
    private readonly List<GameObject> owned = new List<GameObject>();
    private GameObject playerRoot;
    private P12PipelineSafeEnemyTurret enemy;

    [SetUp]
    public void SetUp()
    {
        playerRoot = Own(new GameObject("B0124_PlayerRoot"));
        var enemyRoot = Own(new GameObject("B0124_EnemyRoot"));
        enemy = enemyRoot.AddComponent<P12PipelineSafeEnemyTurret>();
        // Selection uses this original production player-root field; this fixture
        // deliberately does not invoke the full encounter/movement integration.
        enemy.Configure(playerRoot.transform, null, null, enemyRoot.transform, null, null);
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) UnityEngine.Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }

    private GameObject Own(GameObject gameObject)
    {
        owned.Add(gameObject);
        return gameObject;
    }

    private Collider PlayerPart(string label, bool active, bool colliderEnabled = true)
    {
        var part = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
        part.name = label;
        part.transform.SetParent(playerRoot.transform, false);
        part.AddComponent<P12_PipelineSafePlayerBlock>();
        var health = part.AddComponent<P12PipelineSafeDamageable>();
        health.Configure(100f, false);
        var collider = part.GetComponent<Collider>();
        collider.enabled = colliderEnabled;
        part.SetActive(active);
        Assert.That(health.IsDead, Is.False, "The test must not confuse inactivity with death.");
        return collider;
    }

    private static MethodInfo Method(string name)
    {
        var method = typeof(P12PipelineSafeEnemyTurret).GetMethod(
            name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, "Production method missing: " + name);
        return method;
    }

    private void Field(string name, object value)
    {
        var field = typeof(P12PipelineSafeEnemyTurret).GetField(
            name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Production field missing: " + name);
        field.SetValue(enemy, value);
    }

    private Collider Select() => (Collider)Method("SelectTargetCollider").Invoke(enemy, null);

    [Test]
    public void ActiveLivingEnabledBlock_IsSelectable_PositiveControl()
    {
        var active = PlayerPart("ActiveAndAlive", true);
        Assert.That(Select(), Is.SameAs(active));
    }

    [Test]
    public void DisabledCollider_IsNeverSelectable_NegativeControl()
    {
        PlayerPart("EnabledFalse", true, false);
        Assert.That(Select(), Is.Null);
    }

    [Test]
    public void InactiveLivingEnabledBlock_IsNeverSelected_Regression()
    {
        var inactive = PlayerPart("InactiveButAlive", false);
        Assert.That(inactive.enabled, Is.True);
        Assert.That(inactive.gameObject.activeInHierarchy, Is.False);
        Assert.That(
            playerRoot.GetComponentsInChildren<P12_PipelineSafePlayerBlock>(true).Length,
            Is.EqualTo(1), "The fixture must expose the inactive candidate to includeInactive enumeration.");
        // Expected RED on unpatched Codex #310: production uses includeInactive=true,
        // checks dead and collider.enabled, but not activeInHierarchy.
        Assert.That(Select(), Is.Null,
            "Enemy AI must not aim at a live-enabled Collider on an inactive GameObject.");
    }

    [Test]
    public void DeactivateAndReactivateBlock_ReacquiresOnlyWhenActive()
    {
        var collider = PlayerPart("Toggled", true);
        Assert.That(Select(), Is.SameAs(collider), "Positive baseline.");
        collider.gameObject.SetActive(false);
        Assert.That(Select(), Is.Null, "Expected RED on unpatched #310.");
        collider.gameObject.SetActive(true);
        Assert.That(Select(), Is.SameAs(collider), "Recovery after activation.");
    }

    [Test]
    [Explicit("Enable only after HQ/B01-10 approves opaque solid cover suppressing enemy shots.")]
    public void SolidOpaqueCover_SuppressesShot_ThenRecovers_ConditionalPolicy()
    {
        // This calls the ORIGINAL enemy Update/FirePhysicalShell, not a JS mirror.
        // It is opt-in because suppression versus intentional cover fire is HQ policy.
        var coreObj = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
        coreObj.transform.SetParent(enemy.transform, false);
        coreObj.transform.position = new Vector3(-30f, 1f, -2f);
        var core = coreObj.AddComponent<P12PipelineSafeDamageable>();
        core.Configure(100f, false);

        var supportObj = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
        supportObj.transform.SetParent(enemy.transform, false);
        supportObj.transform.position = new Vector3(-30f, 1f, -1f);
        var support = supportObj.AddComponent<P12PipelineSafeDamageable>();
        support.Configure(100f, false);

        var gunObj = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
        gunObj.transform.SetParent(enemy.transform, false);
        gunObj.transform.position = new Vector3(-30f, 1f, 0f);
        var gun = gunObj.AddComponent<P12PipelineSafeDamageable>();
        gun.Configure(100f, false);

        var yaw = Own(new GameObject("B0124_Yaw")).transform;
        yaw.SetParent(enemy.transform, false);
        yaw.position = new Vector3(0f, 1f, 0f);
        var muzzle = Own(new GameObject("B0124_Muzzle")).transform;
        muzzle.SetParent(yaw, false);
        muzzle.position = yaw.position;

        var target = PlayerPart("PhysicalTarget", true);
        target.transform.position = new Vector3(0f, 1f, 14.5f);
        yaw.rotation = Quaternion.LookRotation(target.bounds.center - yaw.position);

        var wall = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
        wall.name = "B0124_IndestructibleWorldCover";
        wall.transform.position = new Vector3(0f, 1f, 6f);
        wall.transform.localScale = new Vector3(4f, 3f, 1f);
        var wallCollider = wall.GetComponent<Collider>();

        enemy.Configure(playerRoot.transform, yaw, muzzle, enemy.transform, core, null);
        Field("weaponBlock", gun);
        Field("turretSupport", support);
        Field("currentTarget", target);
        Field("nextTargetTime", float.PositiveInfinity);
        Field("nextFireTime", -1f);
        enemy.SetCombatActive(true);
        Physics.SyncTransforms();

        var direction = (target.bounds.center - muzzle.position).normalized;
        Assert.That(Physics.Raycast(muzzle.position, direction, out var firstHit, 14.5f),
            Is.True, "Fixture must have real Unity physics occlusion.");
        Assert.That(firstHit.collider, Is.SameAs(wallCollider),
            "Opaque wall must be the first actual physical obstacle.");
        var preexisting = new HashSet<int>();
        foreach (var shell in UnityEngine.Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None))
            preexisting.Add(shell.GetInstanceID());

        try
        {
            Method("Update").Invoke(enemy, null);
            int blockedShotCount = 0;
            foreach (var shell in UnityEngine.Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None))
                if (!preexisting.Contains(shell.GetInstanceID())) blockedShotCount++;
            // Expected RED on the current friendly-only fire gate.
            Assert.That(blockedShotCount, Is.Zero, "Opaque wall must suppress normal fire when HQ policy chooses BLOCK.");
            wall.SetActive(false);
            Physics.SyncTransforms();
            Method("Update").Invoke(enemy, null);
            int recoveredShotCount = 0;
            foreach (var shell in UnityEngine.Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None))
                if (!preexisting.Contains(shell.GetInstanceID())) recoveredShotCount++;
            Assert.That(recoveredShotCount, Is.EqualTo(1),
                "Clear LOS must allow the next legally ready normal shot.");
        }
        finally
        {
            foreach (var shell in UnityEngine.Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None))
                if (!preexisting.Contains(shell.GetInstanceID()))
                    UnityEngine.Object.DestroyImmediate(shell.gameObject);
        }
    }
}
