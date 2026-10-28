// B01-24 PASS15 continuation — QA ONLY, original enemy C# API.
// Original owner source: Codex PR #310@231005c6d0351945a8ee6684f3856af118f0e94f.
// Separate from the inactive PLAYER target cache test #318: validates whether
// an inactive ENEMY weapon/support is incorrectly considered operational.
// Native Unity EditMode: NOT_RUN in B01 authoring environment.
using NUnit.Framework;
using UnityEngine;
using RogueMachine.P12PipelineSafe;

public sealed class B0124VariantGunOnlineEditModeTests
{
    private GameObject enemyRoot;
    private GameObject gunObject;
    private GameObject supportObject;
    private P12PipelineSafeDamageable gunDamageable;
    private P12PipelineSafeDamageable supportDamageable;
    private P12PipelineSafeEnemyTurret.VariantGun weapon;

    private static P12PipelineSafeDamageable HealthyBlock(Transform parent, string label)
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = label;
        block.transform.SetParent(parent, false);
        var hp = block.AddComponent<P12PipelineSafeDamageable>();
        hp.Configure(100f, false);
        Assert.That(hp.IsDead, Is.False, "Source fixture must keep live 100HP blocks.");
        return hp;
    }

    [SetUp]
    public void SetUp()
    {
        enemyRoot = new GameObject("B0124_ActiveEnemyWithVariantGun");
        gunDamageable = HealthyBlock(enemyRoot.transform, "B0124_VariantGunBlock");
        supportDamageable = HealthyBlock(enemyRoot.transform, "B0124_VariantSupportBlock");
        gunObject = gunDamageable.gameObject;
        supportObject = supportDamageable.gameObject;

        var pivot = new GameObject("B0124_RealGunPivot").transform;
        pivot.SetParent(gunObject.transform, false);
        var muzzle = new GameObject("B0124_RealGunMuzzle").transform;
        muzzle.SetParent(pivot, false);

        // This is the actual production nested type and public operational
        // predicate; no mirrored JS/Python interpretation of variant Online.
        weapon = new P12PipelineSafeEnemyTurret.VariantGun
        {
            Block = gunDamageable,
            Support = supportDamageable,
            Pivot = pivot,
            Muzzle = muzzle,
            NextShot = float.PositiveInfinity
        };
        Assert.That(weapon.Online(enemyRoot.transform), Is.True,
            "A healthy active physically mounted variant gun must be online.");
    }

    [TearDown]
    public void TearDown()
    {
        if (enemyRoot != null) Object.DestroyImmediate(enemyRoot);
        enemyRoot = null;
    }

    [Test]
    public void ActiveHealthyWeaponAndSupport_IsOnline_Positive()
    {
        Assert.That(gunObject.activeInHierarchy && supportObject.activeInHierarchy, Is.True);
        Assert.That(weapon.Online(enemyRoot.transform), Is.True);
    }

    [Test]
    public void DisabledGunGameObject_IsNotOnline_Negative()
    {
        gunObject.SetActive(false);
        Assert.That(gunDamageable.IsDead, Is.False);
        Assert.That(gunObject.activeInHierarchy, Is.False);
        Assert.That(gunDamageable.transform.IsChildOf(enemyRoot.transform), Is.True);
        Assert.That(weapon.Online(enemyRoot.transform), Is.False,
            "Expected RED on unchanged #310: disabled physical gun must not be operational.");
    }

    [Test]
    public void DisabledSupportGameObject_IsNotOnline_Negative()
    {
        supportObject.SetActive(false);
        Assert.That(supportDamageable.IsDead, Is.False);
        Assert.That(supportObject.activeInHierarchy, Is.False);
        Assert.That(supportDamageable.transform.IsChildOf(enemyRoot.transform), Is.True);
        Assert.That(weapon.Online(enemyRoot.transform), Is.False,
            "Expected RED on unchanged #310: disabled physical support must disable the gun.");
    }

    [Test]
    public void ReactivatedGunAndSupport_RestoreOnline_Positive()
    {
        gunObject.SetActive(false);
        supportObject.SetActive(false);
        gunObject.SetActive(true);
        supportObject.SetActive(true);
        Assert.That(gunObject.activeInHierarchy && supportObject.activeInHierarchy, Is.True);
        Assert.That(weapon.Online(enemyRoot.transform), Is.True,
            "Restoring active healthy mounted components must restore readiness.");
    }
}
