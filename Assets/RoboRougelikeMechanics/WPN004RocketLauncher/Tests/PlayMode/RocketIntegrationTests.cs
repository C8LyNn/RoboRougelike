using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.Mechanics.Rockets;

namespace RoboRougelike.WPN004.Tests.PlayMode
{
    public sealed class TestRocketReceiver : MonoBehaviour, IRocketDamageReceiver
    {
        public int Hits;
        public float TotalDamage;
        public Vector3 LastImpulse;
        public void ReceiveRocketDamage(float damage, Vector3 point, Vector3 impulse, GameObject attacker)
        {
            Hits++;
            TotalDamage += damage;
            LastImpulse = impulse;
        }
    }

    public sealed class RocketIntegrationTests
    {
        private GameObject owner;
        private GameObject projectileSource;
        private RocketLauncher launcher;
        private Rigidbody vehicleBody;
        private Transform muzzle;
        private RocketConfig config;
        private RocketProjectile prefab;

        [SetUp]
        public void Setup()
        {
            owner = new GameObject("OWNER");
            vehicleBody = owner.AddComponent<Rigidbody>();
            vehicleBody.useGravity = false;
            vehicleBody.constraints = RigidbodyConstraints.FreezeAll;
            var launcherObject = new GameObject("Launcher");
            launcherObject.transform.SetParent(owner.transform, false);
            launcher = launcherObject.AddComponent<RocketLauncher>();
            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(launcherObject.transform, false);
            muzzle.localPosition = Vector3.forward;
            projectileSource = new GameObject("ProjectilePrefabSource");
            projectileSource.transform.position = new Vector3(1000, 1000, 1000);
            var sphere = projectileSource.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            var body = projectileSource.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            prefab = projectileSource.AddComponent<RocketProjectile>();
            config = new RocketConfig { initialSpeed = 18f, maxSpeed = 18f, acceleration = 0f,
                maxLifetime = 2f, explosionRadius = 2f, cooldown = 0f, recoilImpulse = 0f };
            launcher.Bind(vehicleBody, owner.transform, muzzle);
            launcher.SetConfig(config);
            launcher.SetPrefab(prefab);
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var missile in Object.FindObjectsByType<RocketProjectile>(FindObjectsSortMode.None))
                if (missile != null && missile.gameObject != projectileSource) Object.DestroyImmediate(missile.gameObject);
            if (owner != null) Object.DestroyImmediate(owner);
            if (projectileSource != null) Object.DestroyImmediate(projectileSource);
            foreach (var target in Object.FindObjectsByType<TestRocketReceiver>(FindObjectsSortMode.None))
                if (target != null) Object.DestroyImmediate(target.gameObject);
            var wall = GameObject.Find("OcclusionWall");
            if (wall != null) Object.DestroyImmediate(wall);
        }

        private TestRocketReceiver CreateTarget(Vector3 center)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Target_Block";
            go.transform.position = center;
            return go.AddComponent<TestRocketReceiver>();
        }

        private RocketProjectile SpawnedRocket()
        {
            foreach (var rocket in Object.FindObjectsByType<RocketProjectile>(FindObjectsSortMode.None))
                if (rocket != prefab && rocket.IsInitialized) return rocket;
            return null;
        }

        [Test]
        public void LauncherRejectsDisabledModule()
        {
            launcher.SetOperational(false);
            Assert.AreEqual(RocketFireResult.Disabled, launcher.TryFire());
        }

        [Test]
        public void LauncherRejectsMissingPrefab()
        {
            launcher.SetPrefab(null);
            Assert.AreEqual(RocketFireResult.MissingPrefab, launcher.TryFire());
        }

        [Test]
        public void LauncherRejectsMissingMuzzle()
        {
            launcher.Bind(vehicleBody, owner.transform, null);
            Assert.AreEqual(RocketFireResult.MissingMuzzle, launcher.TryFire());
        }

        [Test]
        public void LauncherRejectsDetachedModule()
        {
            launcher.transform.SetParent(null, true);
            Assert.AreEqual(RocketFireResult.Disabled, launcher.TryFire());
            launcher.transform.SetParent(owner.transform, true);
        }

        [Test]
        public void LauncherRejectsOutOfAmmoWithoutFiring()
        {
            var ammo = owner.AddComponent<RocketAmmoMagazine>();
            ammo.SetRounds(0);
            launcher.SetAmmoSource(ammo);
            Assert.AreEqual(RocketFireResult.OutOfAmmo, launcher.TryFire());
            Assert.IsNull(SpawnedRocket());
        }

        [Test]
        public void LauncherRefundsAmmoAfterEnergyFailure()
        {
            var ammo = owner.AddComponent<RocketAmmoMagazine>();
            ammo.SetRounds(2);
            var energy = owner.AddComponent<RocketEnergyStore>();
            energy.SetStored(0);
            config.energyCost = 5;
            launcher.SetAmmoSource(ammo);
            launcher.SetEnergySource(energy);
            Assert.AreEqual(RocketFireResult.OutOfEnergy, launcher.TryFire());
            Assert.AreEqual(2, ammo.Rounds);
            Assert.IsNull(SpawnedRocket());
        }

        [Test]
        public void RocketSpawnsWithConfiguredLifetime()
        {
            Assert.AreEqual(RocketFireResult.Fired, launcher.TryFire());
            Assert.NotNull(SpawnedRocket());
        }

        [UnityTest]
        public IEnumerator RocketMovesForwardInPhysicsTime()
        {
            Assert.AreEqual(RocketFireResult.Fired, launcher.TryFire());
            var projectile = SpawnedRocket();
            Vector3 before = projectile.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.Greater(projectile.transform.position.z, before.z);
        }

        [UnityTest]
        public IEnumerator RocketHitsReceiverAndDamagesIt()
        {
            var target = CreateTarget(new Vector3(0, 0, 3.5f));
            Assert.AreEqual(RocketFireResult.Fired, launcher.TryFire());
            for (int i = 0; i < 18 && target.Hits == 0; i++)
                yield return new WaitForFixedUpdate();
            Assert.Greater(target.TotalDamage, 0f);
        }

        [UnityTest]
        public IEnumerator RocketHitsSolidWallWithoutHittingBlockedTarget()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "OcclusionWall";
            wall.transform.position = new Vector3(0, 0, 2.6f);
            wall.transform.localScale = new Vector3(5f, 5f, .3f);
            var target = CreateTarget(new Vector3(0, 0, 4.4f));
            Assert.AreEqual(RocketFireResult.Fired, launcher.TryFire());
            for (int i = 0; i < 14; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, target.Hits);
        }

        [UnityTest]
        public IEnumerator RocketCanBeInterceptedInFlight()
        {
            Assert.AreEqual(RocketFireResult.Fired, launcher.TryFire());
            var missile = SpawnedRocket();
            Assert.NotNull(missile);
            missile.ApplyInterceptionDamage(100f);
            Assert.IsTrue(missile.IsResolved);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MissileExpiresWithoutHittingAnything()
        {
            config.maxLifetime = .045f;
            Assert.AreEqual(RocketFireResult.Fired, launcher.TryFire());
            var missile = SpawnedRocket();
            for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(missile == null || missile.IsResolved);
        }

        [Test]
        public void BlastDamagesOneReceiverOnceForMultipleColliders()
        {
            TestRocketReceiver receiver = CreateTarget(new Vector3(0, 0, 1f));
            receiver.gameObject.AddComponent<SphereCollider>().radius = .5f;
            var spec = config.Snapshot();
            Physics.SyncTransforms();
            RocketBlastResult result = RocketBlastResolver.Resolve(Vector3.zero, Vector3.forward, spec,
                null, owner, new Collider[64], new RaycastHit[64]);
            Assert.IsFalse(result.Overflow);
            Assert.AreEqual(1, receiver.Hits);
        }

        [Test]
        public void BlastFailsClosedOnOverlapOverflow()
        {
            TestRocketReceiver receiver = CreateTarget(new Vector3(0, 0, 1f));
            for (int i = 0; i < 10; i++) receiver.gameObject.AddComponent<SphereCollider>();
            Physics.SyncTransforms();
            RocketBlastResult result = RocketBlastResolver.Resolve(Vector3.zero, Vector3.up,
                config.Snapshot(), null, owner, new Collider[2], new RaycastHit[64]);
            Assert.IsTrue(result.Overflow);
            Assert.AreEqual(0, receiver.Hits);
        }

        [Test]
        public void BlastDoesNotDamageOwnVehicleByDefault()
        {
            var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.transform.SetParent(owner.transform);
            child.transform.localPosition = new Vector3(0, 0, 2f);
            var receiver = child.AddComponent<TestRocketReceiver>();
            Physics.SyncTransforms();
            RocketBlastResolver.Resolve(new Vector3(0, 0, 2f), Vector3.up, config.Snapshot(),
                owner.transform, owner, new Collider[64], new RaycastHit[64]);
            Assert.AreEqual(0, receiver.Hits);
        }

        [Test]
        public void BlastCanDamageOwnVehicleWhenEnabled()
        {
            var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.transform.SetParent(owner.transform);
            child.transform.localPosition = new Vector3(0, 0, 2f);
            var receiver = child.AddComponent<TestRocketReceiver>();
            config.damageOwner = true;
            Physics.SyncTransforms();
            RocketBlastResolver.Resolve(new Vector3(0, 0, 2f), Vector3.up, config.Snapshot(),
                owner.transform, owner, new Collider[64], new RaycastHit[64]);
            Assert.Greater(receiver.Hits, 0);
        }

        [Test]
        public void BlastHasNoDamageBeyondRadius()
        {
            var receiver = CreateTarget(new Vector3(0, 0, 15f));
            Physics.SyncTransforms();
            RocketBlastResolver.Resolve(Vector3.zero, Vector3.up, config.Snapshot(),
                null, owner, new Collider[64], new RaycastHit[64]);
            Assert.AreEqual(0, receiver.Hits);
        }
    }
}
