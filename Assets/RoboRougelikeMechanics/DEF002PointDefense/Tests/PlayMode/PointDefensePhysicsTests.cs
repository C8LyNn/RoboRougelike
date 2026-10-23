using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.Mechanics.PointDefense;

namespace RoboRougelike.Mechanics.PointDefense.Tests
{
    public sealed class PointDefensePhysicsTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private GameObject root, turret, muzzle;
        private Rigidbody body;
        private PointDefenseController defense;
        private PointDefenseInterceptor prototype;
        private PointDefenseConfiguration options;

        private GameObject Track(string name, Vector3 position)
        {
            var result = new GameObject(name);
            result.transform.position = position;
            objects.Add(result);
            return result;
        }
        [SetUp] public void SetUp()
        {
            root = Track("FriendlyVehicle", Vector3.zero);
            root.AddComponent<BoxCollider>().size = new Vector3(1, 1, 1);
            body = root.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            turret = Track("Turret", new Vector3(0, 0, .3f)); turret.transform.SetParent(root.transform, true);
            muzzle = Track("Muzzle", new Vector3(0, 0, 1.1f)); muzzle.transform.SetParent(turret.transform, true);
            defense = turret.AddComponent<PointDefenseController>();
            var prefab = Track("ProjectileTemplate", new Vector3(100, 100, 100));
            prototype = prefab.AddComponent<PointDefenseInterceptor>();
            options = new PointDefenseConfiguration { detectionRadius = 30, scanInterval = .05f,
                threatLookAhead = 10, defendedRadius = 4, traverseDegreesPerSecond = 1000,
                firingToleranceDegrees = 6, shotInterval = .1f, interceptorSpeed = 65,
                interceptorLifetime = 3, interceptorRadius = .2f, interceptorDamage = 1,
                useAmmunition = true, startingAmmunition = 5 };
            defense.Configure(options, prototype);
            defense.Bind(body, root.transform, turret.transform, muzzle.transform, 1);
        }
        [TearDown] public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
            foreach (var p in Object.FindObjectsByType<PointDefenseInterceptor>(FindObjectsSortMode.None))
                if (p != null) Object.DestroyImmediate(p.gameObject);
        }
        private PointDefenseTestThreat CreateMissile(float z = 12, int faction = 2, Vector3? speed = null)
        {
            var obj = Track("IncomingMissile", new Vector3(0, 0, z));
            obj.AddComponent<SphereCollider>().radius = .45f;
            var target = obj.AddComponent<PointDefenseTestThreat>();
            target.speed = speed ?? new Vector3(0, 0, -5);
            target.faction = faction;
            obj.AddComponent<PointDefenseThreatProxy>().Bind(target);
            Physics.SyncTransforms();
            return target;
        }
        [Test] public void Scanning_RecognizesIncomingProjectile()
        {
            var t = CreateMissile();
            defense.ScanNow();
            Assert.AreSame(t.GetComponent<PointDefenseThreatProxy>(), defense.CurrentTarget);
        }
        [Test] public void Scanning_SkipsFriendly()
        {
            CreateMissile(faction: 1); defense.ScanNow();
            Assert.IsNull(defense.CurrentTarget);
        }
        [Test] public void Scanning_SkipsOutgoingProjectile()
        {
            CreateMissile(speed: new Vector3(0, 0, 5)); defense.ScanNow();
            Assert.IsNull(defense.CurrentTarget);
        }
        [Test] public void Scanning_SkipsProjectileThatWillMiss()
        {
            var t = CreateMissile(); t.transform.position = new Vector3(12, 0, 12);
            Physics.SyncTransforms(); defense.ScanNow(); Assert.IsNull(defense.CurrentTarget);
        }
        [Test] public void Scanning_FiltersDuplicateColliders()
        {
            var t = CreateMissile();
            var duplicate = Track("ColliderChild", t.transform.position);
            duplicate.transform.SetParent(t.transform, true);
            duplicate.AddComponent<SphereCollider>().radius = .2f;
            Physics.SyncTransforms(); defense.ScanNow();
            Assert.AreSame(t.GetComponent<PointDefenseThreatProxy>(), defense.CurrentTarget);
        }
        [Test] public void WallBlocksTargetVisibility()
        {
            CreateMissile();
            var wall = Track("Wall", new Vector3(0, 0, 6));
            wall.AddComponent<BoxCollider>().size = new Vector3(5, 5, 1);
            Physics.SyncTransforms(); defense.ScanNow();
            Assert.IsNull(defense.CurrentTarget);
        }
        [Test] public void DeactivatedModule_CannotTrack()
        {
            CreateMissile(); defense.SetOperational(false); defense.ScanNow();
            Assert.IsNull(defense.CurrentTarget);
        }
        [Test] public void DetachedModule_CannotTrack()
        {
            CreateMissile(); turret.transform.SetParent(null, true);
            Physics.SyncTransforms(); defense.ScanNow();
            Assert.IsNull(defense.CurrentTarget);
        }
        [UnityTest] public IEnumerator IncomingMissile_IsInterceptedBeforeReachingVehicle()
        {
            var t = CreateMissile(12);
            for (int i = 0; i < 35 && t.IsActiveThreat; i++) yield return new WaitForFixedUpdate();
            Assert.IsFalse(t.IsActiveThreat, "Physical counter-projectile should collide with incoming missile.");
            Assert.GreaterOrEqual(t.interceptCalls, 1);
            Assert.Less(defense.Ammunition, 5);
        }
        [UnityTest] public IEnumerator ZeroAmmunition_PreventsFiring()
        {
            options.startingAmmunition = 0;
            defense.Configure(options, prototype);
            var t = CreateMissile();
            for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(t.IsActiveThreat);
            Assert.AreEqual(0, defense.Ammunition);
        }
        [UnityTest] public IEnumerator EnergyRequiredWithoutBank_PreventsFiring()
        {
            options.energyPerShot = 10;
            defense.Configure(options, prototype);
            var t = CreateMissile();
            for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(t.IsActiveThreat);
            Assert.AreEqual(5, defense.Ammunition);
        }
        [UnityTest] public IEnumerator InterceptDamageCanRequireMultipleHits()
        {
            var t = CreateMissile(); t.health = 2;
            for (int i = 0; i < 65 && t.IsActiveThreat; i++) yield return new WaitForFixedUpdate();
            Assert.IsFalse(t.IsActiveThreat);
            Assert.GreaterOrEqual(t.interceptCalls, 2);
        }
        [UnityTest] public IEnumerator DestroyedTarget_IsNotDamagedAgain()
        {
            var t = CreateMissile();
            for (int i = 0; i < 35 && t.IsActiveThreat; i++) yield return new WaitForFixedUpdate();
            Assert.IsFalse(t.IsActiveThreat);
            int count = t.interceptCalls;
            for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(count, t.interceptCalls);
        }
        [UnityTest] public IEnumerator WallStopsCounterProjectileInFlight()
        {
            var t = CreateMissile();
            // Allow the controller to fire and only then create an unexpected obstacle.
            for (int i = 0; i < 2; i++) yield return new WaitForFixedUpdate();
            var wall = Track("SuddenObstacle", new Vector3(0, 0, 5));
            wall.AddComponent<BoxCollider>().size = new Vector3(6, 6, .3f);
            Physics.SyncTransforms();
            for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(t.IsActiveThreat);
        }

        [Test] public void Scanning_SelectsSoonerThreat()
        {
            var near = CreateMissile(9);
            CreateMissile(21);
            defense.ScanNow();
            Assert.AreSame(near.GetComponent<PointDefenseThreatProxy>(), defense.CurrentTarget);
        }
        [Test] public void Scanning_RejectsOutOfRangeThreat()
        {
            CreateMissile(60); defense.ScanNow(); Assert.IsNull(defense.CurrentTarget);
        }
        [Test] public void MuzzleInsideForeignCollider_IsNotConsideredClear()
        {
            CreateMissile(12);
            var wall = Track("EnclosingMuzzle", muzzle.transform.position);
            wall.AddComponent<BoxCollider>().size = Vector3.one * .8f;
            Physics.SyncTransforms(); defense.ScanNow();
            Assert.IsNull(defense.CurrentTarget);
        }
        [UnityTest] public IEnumerator SlowRotation_DoesNotFireBeforeAligned()
        {
            options.traverseDegreesPerSecond = 0.1f;
            defense.Configure(options, prototype);
            var incoming = CreateMissile();
            incoming.transform.position = new Vector3(2, 0, 12);
            Physics.SyncTransforms();
            for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(5, defense.Ammunition);
        }
        [UnityTest] public IEnumerator LongCooldown_PreventsSecondShot()
        {
            options.shotInterval = 2f;
            defense.Configure(options, prototype);
            CreateMissile(12);
            CreateMissile(15);
            for (int i = 0; i < 16; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(4, defense.Ammunition);
        }

        [Test] public void Scanning_RecognizesTriggerBasedRocket()
        {
            var t = CreateMissile();
            t.GetComponent<SphereCollider>().isTrigger = true;
            Physics.SyncTransforms(); defense.ScanNow();
            Assert.AreSame(t.GetComponent<PointDefenseThreatProxy>(), defense.CurrentTarget);
        }
        [UnityTest] public IEnumerator TriggerBasedRocket_CanBePhysicallyIntercepted()
        {
            var t = CreateMissile();
            t.GetComponent<SphereCollider>().isTrigger = true;
            Physics.SyncTransforms();
            for (int i = 0; i < 35 && t.IsActiveThreat; i++) yield return new WaitForFixedUpdate();
            Assert.IsFalse(t.IsActiveThreat, "Rocket collider is a trigger, as in WPN-004.");
        }
        [Test] public void AddAmmunition_IncreasesCount()
        { defense.AddAmmunition(8); Assert.AreEqual(13, defense.Ammunition); }
        [Test] public void SetOperationalTrue_AfterFalse_RestoresTracking()
        {
            var t = CreateMissile(); defense.SetOperational(false); defense.ScanNow();
            defense.SetOperational(true); defense.ScanNow();
            Assert.AreSame(t.GetComponent<PointDefenseThreatProxy>(), defense.CurrentTarget);
        }
    }
}
