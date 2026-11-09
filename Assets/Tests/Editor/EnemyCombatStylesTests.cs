using NUnit.Framework;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;
using System.Reflection;
using UnityEngine;

public sealed class EnemyCombatStylesTests
{
    [Test]
    public void ArmedRangedEnemiesDoNotRamJustBecausePlayerIsClose()
    {
        Assert.That(P12PipelineSafeEnemyTurret.ShouldRam(true, R3EnemyArchetype.Gunner, 3, 1), Is.False);
        Assert.That(P12PipelineSafeEnemyTurret.ShouldRam(true, R3EnemyArchetype.Flanker, 3, 1), Is.False);
    }

    [Test]
    public void RangedArchetypeWithMeleeWeaponCanStillCloseForContact()
    {
        var selector=typeof(P12PipelineSafeEnemyTurret).GetMethod("ShouldRam",new[]{typeof(bool),typeof(R3EnemyArchetype),typeof(float),typeof(float),typeof(bool)});
        Assert.That(selector,Is.Not.Null,"The chosen weapon must be part of the charge decision.");
        Assert.That((bool)selector.Invoke(null,new object[]{true,R3EnemyArchetype.Gunner,5f,8f,true}),Is.True);
        Assert.That((bool)selector.Invoke(null,new object[]{true,R3EnemyArchetype.Gunner,5f,8f,false}),Is.False);
    }

    [Test]
    public void BruiserHasAttackAndRecoveryPhasesWhileUnarmedEnemiesCanStillRam()
    {
        Assert.That(P12PipelineSafeEnemyTurret.ShouldRam(true, R3EnemyArchetype.Bruiser, 5, 1), Is.True);
        Assert.That(P12PipelineSafeEnemyTurret.ShouldRam(true, R3EnemyArchetype.Bruiser, 5, 8), Is.False);
        Assert.That(P12PipelineSafeEnemyTurret.ShouldRam(false, R3EnemyArchetype.Gunner, 5, 8), Is.True);
    }

    [Test]
    public void ExtendedContactStartsRetreatAndRetreatContinuesAfterSeparation()
    {
        var recovery = new EnemyContactRecovery();
        Assert.That(recovery.Advance(true, 1f), Is.False);
        Assert.That(recovery.Advance(true, .3f), Is.True);
        Assert.That(recovery.Advance(false, 1f), Is.True);
        Assert.That(recovery.Advance(false, 1.3f), Is.False);
    }

    [Test]
    public void BriefContactsDoNotAccumulateAndResetClearsRetreat()
    {
        var recovery = new EnemyContactRecovery();
        recovery.Advance(true, 1f);
        recovery.Advance(false, .4f);
        Assert.That(recovery.Advance(true, 1f), Is.False);
        Assert.That(recovery.Advance(true, .3f), Is.True);
        recovery.Reset();
        Assert.That(recovery.Recovering, Is.False);
    }

    [Test]
    public void BriefGapsInCollisionCallbacksDoNotHideARealPin()
    {
        var recovery=new EnemyContactRecovery();
        Assert.That(recovery.Advance(true, 1f), Is.False);
        Assert.That(recovery.Advance(false, .1f), Is.False);
        Assert.That(recovery.Advance(true, .3f), Is.True);
    }

    [TestCase(R3EnemyArchetype.Gunner, P12FunctionalModuleType.Weapon)]
    [TestCase(R3EnemyArchetype.Flanker, P12FunctionalModuleType.Wheel)]
    public void EnemiesPreferDifferentWorkingModules(R3EnemyArchetype archetype,P12FunctionalModuleType preferred)
    {
        var player=new GameObject("Target vehicle");
        var enemyRoot=new GameObject("Enemy");
        var oldRandom=Random.state;
        try
        {
            var enemy=enemyRoot.AddComponent<P12PipelineSafeEnemyTurret>();
            enemy.ConfigureArchetype(archetype);
            typeof(P12PipelineSafeEnemyTurret).GetField("playerRoot",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemy,player.transform);
            foreach(var kind in new[]{P12FunctionalModuleType.Weapon,P12FunctionalModuleType.Wheel})
            {
                var part=new GameObject(kind.ToString());part.transform.SetParent(player.transform);
                part.AddComponent<BoxCollider>();part.AddComponent<P12PipelineSafeDamageable>().Configure(100,true);
                part.AddComponent<P12PipelineSafeFunctionalModule>().Configure(kind,0,null,part.transform,player.transform);
            }
            var select=typeof(P12PipelineSafeEnemyTurret).GetMethod("SelectTargetCollider",BindingFlags.Instance|BindingFlags.NonPublic);
            Random.InitState(242);
            int picked=0;
            for(int i=0;i<200;i++)
            {
                var collider=(Collider)select.Invoke(enemy,null);
                if(collider.GetComponent<P12PipelineSafeFunctionalModule>().ModuleType==preferred)picked++;
            }
            Assert.That(picked,Is.GreaterThan(140),"The archetype should visibly prefer its intended operational module.");
            player.transform.Find(preferred.ToString()).gameObject.SetActive(false);
            for(int i=0;i<10;i++)
            {
                var fallback=(Collider)select.Invoke(enemy,null);
                Assert.That(fallback,Is.Not.Null);
                Assert.That(fallback.GetComponent<P12PipelineSafeFunctionalModule>().ModuleType,Is.Not.EqualTo(preferred),"Inactive parts cannot remain targets.");
            }
        }
        finally { Random.state=oldRandom;Object.DestroyImmediate(enemyRoot);Object.DestroyImmediate(player); }
    }
}
