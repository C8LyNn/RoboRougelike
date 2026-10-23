using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.WPN001Shotgun.Demo;

namespace RoboRougelike.WPN001Shotgun.Tests.PlayMode
{
    public sealed class WPN001FakeSink : MonoBehaviour, IShotgunVolleySink
    {
        public readonly List<ShotgunPelletSpawn> LastVolley = new List<ShotgunPelletSpawn>();
        public int Calls;
        public bool Accept = true;
        public bool TrySpawnVolley(IReadOnlyList<ShotgunPelletSpawn> pellets)
        {
            Calls++; LastVolley.Clear();
            for (int i = 0; i < pellets.Count; i++) LastVolley.Add(pellets[i]);
            return Accept;
        }
    }

    public sealed class ShotgunModulePlayModeTests
    {
        private readonly List<GameObject> created = new List<GameObject>();
        private Rigidbody body;
        private ShotgunModule module;
        private Transform muzzle;
        private WPN001FakeSink sink;

        private GameObject New(string name)
        {
            var go = new GameObject(name); created.Add(go); return go;
        }

        [SetUp] public void SetUp()
        {
            GameObject owner = New("WPN001 Test Owner");
            body = owner.AddComponent<Rigidbody>();
            body.mass = 100f;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeAll;
            GameObject barrel = New("Muzzle");
            barrel.transform.SetParent(owner.transform);
            barrel.transform.localPosition = Vector3.forward;
            muzzle = barrel.transform;
            GameObject weapon = New("WPN001 Test Weapon");
            weapon.transform.SetParent(owner.transform);
            module = weapon.AddComponent<ShotgunModule>();
            sink = weapon.AddComponent<WPN001FakeSink>();
            module.Bind(body, muzzle, owner);
            module.BindSink(sink);
        }

        [TearDown] public void TearDown()
        {
            foreach (var spawned in Object.FindObjectsByType<ShotgunDemoProjectile>(FindObjectsSortMode.None))
                if (spawned != null && spawned.name.StartsWith("WPN001 DEMO pellet"))
                    Object.DestroyImmediate(spawned.gameObject);
            for (int i = created.Count - 1; i >= 0; i--)
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
        }

        [Test] public void FiringEmitsEightPellets()
        {
            Assert.IsTrue(module.TryFireAtTime(100f));
            Assert.AreEqual(8, sink.LastVolley.Count);
        }
        [Test] public void PelletDamageIsPerPellet()
        {
            module.TryFireAtTime(100f);
            foreach (var p in sink.LastVolley) Assert.That(p.Damage, Is.EqualTo(9f));
        }
        [Test] public void PelletOwnerIdentityIsPreserved()
        {
            module.TryFireAtTime(100f);
            foreach (var p in sink.LastVolley)
            {
                Assert.AreSame(body, p.OwnerBody);
                Assert.AreSame(body.gameObject, p.OwnerRoot);
            }
        }
        [Test] public void PelletForwardFollowsMuzzleRotation()
        {
            muzzle.rotation = Quaternion.Euler(0f, 90f, 0f);
            module.TryFireAtTime(100f);
            foreach (var p in sink.LastVolley) Assert.LessOrEqual(Vector3.Angle(Vector3.right, p.Direction), 7.1f);
        }
        [Test] public void CooldownRejectsAnotherShot()
        {
            Assert.IsTrue(module.TryFireAtTime(100f));
            Assert.IsFalse(module.TryFireAtTime(100.1f));
            Assert.AreEqual(ShotgunFireFailure.Cooldown, module.LastFailure);
            Assert.AreEqual(1, sink.Calls);
        }
        [Test] public void CooldownEventuallyExpires()
        {
            Assert.IsTrue(module.TryFireAtTime(100f));
            Assert.IsTrue(module.TryFireAtTime(101f));
            Assert.AreEqual(2u, module.SuccessfulVolleys);
        }
        [Test] public void ShotSequenceOnlyAdvancesOnSuccessfulShot()
        {
            sink.Accept = false;
            Assert.IsFalse(module.TryFireAtTime(100f));
            Assert.AreEqual(0u, module.SuccessfulVolleys);
            sink.Accept = true;
            Assert.IsTrue(module.TryFireAtTime(100f));
            Assert.AreEqual(0u, sink.LastVolley[0].VolleyIndex);
        }
        [Test] public void RejectedVolleyDoesNotSpendAmmo()
        {
            var ammo = module.gameObject.AddComponent<ShotgunSimpleAmmo>();
            ammo.Configure(1, false); module.BindResourceBank(ammo);
            sink.Accept = false;
            Assert.IsFalse(module.TryFireAtTime(100f));
            Assert.AreEqual(1, ammo.Rounds);
            Assert.AreEqual(ShotgunFireFailure.EmissionRejected, module.LastFailure);
        }
        [Test] public void AmmoRunsOutAfterFirstVolley()
        {
            var ammo = module.gameObject.AddComponent<ShotgunSimpleAmmo>();
            ammo.Configure(1, false); module.BindResourceBank(ammo);
            Assert.IsTrue(module.TryFireAtTime(100f));
            Assert.IsFalse(module.TryFireAtTime(101f));
            Assert.AreEqual(ShotgunFireFailure.ResourceUnavailable, module.LastFailure);
        }
        [Test] public void DeactivatedModuleWillNotFire()
        {
            module.SetOperational(false);
            Assert.IsFalse(module.TryFireAtTime(100f));
            Assert.AreEqual(ShotgunFireFailure.ModuleInactive, module.LastFailure);
        }
        [Test] public void DisabledModuleWillNotFire()
        {
            module.enabled = false;
            Assert.IsFalse(module.TryFireAtTime(100f));
        }
        [Test] public void DisabledSinkWillNotFire()
        {
            sink.enabled = false;
            Assert.IsFalse(module.TryFireAtTime(100f));
            Assert.AreEqual(ShotgunFireFailure.MissingSink, module.LastFailure);
        }
        [Test] public void DetachedWeaponCannotFireWithoutLifecycleRebind()
        {
            module.transform.SetParent(null);
            Assert.IsFalse(module.TryFireAtTime(100f));
            Assert.AreEqual(ShotgunFireFailure.Detached, module.LastFailure);
        }
        [Test] public void InheritVehiclePointVelocity()
        {
            body.constraints = RigidbodyConstraints.None;
            body.linearVelocity = new Vector3(3f, 0f, 4f);
            Assert.IsTrue(module.TryFireAtTime(100f));
            Assert.That(sink.LastVolley[0].InheritedVelocity.x, Is.EqualTo(3f).Within(0.01f));
            Assert.That(sink.LastVolley[0].InheritedVelocity.z, Is.EqualTo(4f).Within(0.01f));
        }
        [Test] public void SeedIsStableAcrossSeparateModules()
        {
            GameObject other = New("Other weapon");
            other.transform.SetParent(body.transform);
            ShotgunModule weapon2 = other.AddComponent<ShotgunModule>();
            WPN001FakeSink sink2 = other.AddComponent<WPN001FakeSink>();
            weapon2.Bind(body, muzzle, body.gameObject);
            weapon2.BindSink(sink2);
            Assert.IsTrue(module.TryFireAtTime(100f));
            Assert.IsTrue(weapon2.TryFireAtTime(100f));
            for (int i = 0; i < sink.LastVolley.Count; i++)
                Assert.AreEqual(sink.LastVolley[i].Direction, sink2.LastVolley[i].Direction);
        }
        [Test] public void MuzzlePositionUsedNotVehicleCenter()
        {
            muzzle.localPosition = new Vector3(0.5f, 0.4f, 1.2f);
            Assert.IsTrue(module.TryFireAtTime(100f));
            Assert.AreEqual(muzzle.position, sink.LastVolley[0].Origin);
        }
        [UnityTest] public IEnumerator RecoilImpulseMovesDynamicBodyBackwards()
        {
            body.constraints = RigidbodyConstraints.None;
            module.Configure(new ShotgunConfig { pelletCount = 1, fullConeAngleDegrees = 0f, recoilImpulse = 5f });
            Assert.IsTrue(module.TryFireAtTime(100f));
            yield return new WaitForFixedUpdate();
            Assert.Less(body.linearVelocity.z, -0.001f);
        }
        [UnityTest] public IEnumerator DemoPhysicalPelletsDamageDemoTarget()
        {
            Object.DestroyImmediate(sink);
            var demo = module.gameObject.AddComponent<ShotgunDemoVolleySink>();
            module.BindSink(demo);
            var target = New("WPN001 Test Target");
            target.transform.position = new Vector3(0f, 0f, 5f);
            var collider = target.AddComponent<BoxCollider>();
            collider.size = new Vector3(3f, 3f, 0.6f);
            var health = target.AddComponent<ShotgunDemoTargetHealth>();
            health.Configure(1000f);
            Assert.IsTrue(module.TryFireAtTime(100f));
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            Assert.Greater(health.Hits, 0, "Physical pellets should collide with demo target");
            Assert.Less(health.Health, 1000f);
        }
    }
}
