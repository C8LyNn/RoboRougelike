using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R1;
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

    [Test]
    public void AllyBehindCannotReverseAnActiveRetreatIntoThePlayer()
    {
        var method=typeof(P12PipelineSafeEnemyTurret).GetMethod("ConstrainRetreatDirection",BindingFlags.Static|BindingFlags.Public);
        Assert.That(method,Is.Not.Null,"A retreat must survive contradictory ally avoidance.");
        var towardPlayer=Vector3.forward;
        var retreat=(-towardPlayer+Vector3.right*.35f).normalized;
        var avoidanceFromAllyBehind=towardPlayer*1.4f;
        var direction=(Vector3)method.Invoke(null,new object[]{retreat+avoidanceFromAllyBehind,towardPlayer});
        Assert.That(Vector3.Dot(direction,towardPlayer),Is.LessThan(0f),"Retreat must not steer back into the player.");
        Assert.That(direction.x,Is.GreaterThan(0f),"Keep a sideways escape component when possible.");
    }

    [Test]
    public void AllScoutRosterGetsRangedEscortInsteadOfAnotherScout()
    {
        var encounter=new R1EncounterDefinition {EnemyCount=1,Roster=new[]{R3EnemyArchetype.Scout}};
        Assert.That(encounter.SpawnCount,Is.EqualTo(2));
        Assert.That(encounter.SpawnArchetype(0),Is.EqualTo(R3EnemyArchetype.Scout));
        Assert.That(encounter.SpawnArchetype(1),Is.EqualTo(R3EnemyArchetype.Gunner));
    }

    [Test]
    public void ExistingDifferentArchetypeStillGetsScoutEscort()
    {
        var encounter=new R1EncounterDefinition {EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner}};
        Assert.That(encounter.SpawnArchetype(1),Is.EqualTo(R3EnemyArchetype.Scout));
    }

    [Test]
    public void CoincidentVehicleRootsStillHaveAUsableEscapeDirection()
    {
        var method=typeof(P12PipelineSafeEnemyTurret).GetMethod("TargetDirection",BindingFlags.Static|BindingFlags.Public);
        Assert.That(method,Is.Not.Null,"Overlapping vehicle origins must not halt enemy steering.");
        var direction=(Vector3)method.Invoke(null,new object[]{Vector3.zero,Vector3.right});
        Assert.That(direction,Is.EqualTo(Vector3.right));
        direction=(Vector3)method.Invoke(null,new object[]{Vector3.zero,Vector3.up});
        Assert.That(direction,Is.EqualTo(Vector3.forward),"Vertical chassis orientation needs a finite fallback.");
    }

    [Test]
    public void EnemyAvoidanceUsesNearestObstructionRegardlessOfPhysicsHitOrder()
    {
        var root=new GameObject("Enemy obstacle probe");
        var far=GameObject.CreatePrimitive(PrimitiveType.Cube);
        var near=GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            var body=root.AddComponent<Rigidbody>();body.isKinematic=true;
            var enemy=root.AddComponent<P12PipelineSafeEnemyTurret>();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(P12PipelineSafeEnemyTurret).GetField("movementBody",flags).SetValue(enemy,body);
            typeof(P12PipelineSafeEnemyTurret).GetField("movementHalfExtents",flags).SetValue(enemy,Vector3.one*.1f);
            near.transform.position=new Vector3(0,.75f,2);near.transform.localScale=new Vector3(4,2,.2f);
            far.transform.position=new Vector3(0,.75f,4);far.transform.localScale=new Vector3(4,2,.2f);
            far.transform.rotation=Quaternion.Euler(0,35,0);Physics.SyncTransforms();
            var hits=Physics.BoxCastAll(Vector3.up*.75f,Vector3.one*.1f,Vector3.forward,Quaternion.identity,6,~0,QueryTriggerInteraction.Ignore);
            TestContext.WriteLine("Hit order: "+string.Join(", ",System.Array.ConvertAll(hits,h=>h.distance.ToString("F2"))));
            var unsorted=new RaycastHit[10];var count=Physics.BoxCastNonAlloc(Vector3.up*.75f,Vector3.one*.1f,Vector3.forward,unsorted,Quaternion.identity,6,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)TestContext.WriteLine("NonAlloc hit "+i+": "+unsorted[i].distance.ToString("F2"));
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            Assert.That(hits.Length,Is.EqualTo(2));
            var expected=Vector3.ProjectOnPlane(hits[0].normal,Vector3.up).normalized;
            var canMove=typeof(P12PipelineSafeEnemyTurret).GetMethod("CanMove",flags);
            Assert.That((bool)canMove.Invoke(enemy,new object[]{Vector3.forward,6f,false}),Is.False);
            var actual=(Vector3)typeof(P12PipelineSafeEnemyTurret).GetField("lastMoveNormal",flags).GetValue(enemy);
            Assert.That(Vector3.Dot(actual,expected),Is.GreaterThan(.999f),"Avoidance must use the nearest surface, independent of physics result order.");
        }
        finally { Object.DestroyImmediate(root);Object.DestroyImmediate(near);Object.DestroyImmediate(far); }
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
