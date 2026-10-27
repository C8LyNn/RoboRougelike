using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.Mechanics.Booster.Tests
{
    public sealed class BoosterPhysicsTests
    {
        private static (GameObject root, Rigidbody body, VehicleBooster booster) MakeRig(
            string name, float mass, float thrust, float duration = 1f, float cooldown = 2f)
        {
            var root = new GameObject(name);
            var body = root.AddComponent<Rigidbody>();
            body.mass = mass;
            body.useGravity = false;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
            var booster = root.AddComponent<VehicleBooster>();
            booster.Configure(thrust, duration, cooldown, 0f, 0f); // no limiter, no energy
            booster.Bind(body);
            return (root, body, booster);
        }

        [UnityTest]
        public IEnumerator BoostAcceleratesRigidbodyForward()
        {
            var rig = MakeRig("forward boost", 100f, 1000f);
            try
            {
                Assert.IsTrue(rig.booster.TryActivate());
                for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
                Assert.Greater(rig.body.linearVelocity.z, 0.01f);
                Assert.AreEqual(0f, rig.body.linearVelocity.x, 0.01f);
            }
            finally { Object.Destroy(rig.root); }
        }

        [UnityTest]
        public IEnumerator EqualForceAcceleratesLighterBodyMore()
        {
            var light = MakeRig("light", 100f, 1000f);
            var heavy = MakeRig("heavy", 400f, 1000f);
            try
            {
                Assert.IsTrue(light.booster.TryActivate());
                Assert.IsTrue(heavy.booster.TryActivate());
                for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
                Assert.Greater(light.body.linearVelocity.z, heavy.body.linearVelocity.z * 2f);
            }
            finally
            {
                Object.Destroy(light.root);
                Object.Destroy(heavy.root);
            }
        }

        [UnityTest]
        public IEnumerator MountRotationControlsThrustDirection()
        {
            var rig = MakeRig("oriented", 100f, 1000f);
            var mount = new GameObject("mount").transform;
            mount.SetParent(rig.root.transform, false);
            mount.localRotation = Quaternion.Euler(0f, 90f, 0f);
            rig.booster.Bind(rig.body, mount);
            try
            {
                Assert.IsTrue(rig.booster.TryActivate());
                for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
                Assert.Greater(rig.body.linearVelocity.x, 0.01f);
                Assert.AreEqual(0f, rig.body.linearVelocity.z, 0.01f);
            }
            finally { Object.Destroy(rig.root); }
        }

        [UnityTest]
        public IEnumerator DetachedModuleStopsApplyingForce()
        {
            var rig = MakeRig("vehicle", 100f, 1000f);
            var part = new GameObject("booster part");
            part.transform.SetParent(rig.root.transform, false);
            var module = part.AddComponent<VehicleBooster>();
            module.Configure(1000f, 1f, 2f, 0f, 0f);
            module.Bind(rig.body);
            try
            {
                Assert.IsTrue(module.TryActivate());
                for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
                float before = rig.body.linearVelocity.z;
                part.transform.SetParent(null, true); // detach the module hierarchy
                for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
                Assert.IsFalse(module.IsBoosting);
                Assert.AreEqual(before, rig.body.linearVelocity.z, 0.015f);
            }
            finally
            {
                Object.Destroy(part);
                Object.Destroy(rig.root);
            }
        }

        [UnityTest]
        public IEnumerator ActivationEnergyIsRequiredWhenConfigured()
        {
            var rig = MakeRig("energy guard", 100f, 1000f);
            try
            {
                rig.booster.Configure(1000f, 1f, 2f, 0f, 0f, 5f, 0f);
                Assert.IsFalse(rig.booster.TryActivate());
                Assert.AreEqual(VehicleBooster.ActivationFailure.MissingEnergySource,
                    rig.booster.LastActivationFailure);

                var store = rig.root.AddComponent<BoosterEnergyReservoir>();
                store.Configure(10f, 2f, 0f);
                rig.booster.SetEnergySource(store);
                Assert.IsFalse(rig.booster.TryActivate());
                Assert.AreEqual(VehicleBooster.ActivationFailure.InsufficientEnergy,
                    rig.booster.LastActivationFailure);

                store.Refill();
                Assert.IsTrue(rig.booster.TryActivate());
                Assert.AreEqual(5f, store.CurrentEnergy, 0.0001f);
                yield return new WaitForFixedUpdate();
                Assert.AreEqual(5f, store.CurrentEnergy, 0.0001f); // activation cost once
            }
            finally { Object.Destroy(rig.root); }
        }

        [UnityTest]
        public IEnumerator BoosterRespectsSpeedLimitWithoutRewritingVelocity()
        {
            var rig = MakeRig("speed limit", 100f, 1000f);
            try
            {
                rig.booster.Configure(1000f, 1f, 2f, 0f, 1.1f);
                rig.body.linearVelocity = new Vector3(0f, 0f, 1f);
                Assert.IsTrue(rig.booster.TryActivate());
                for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
                Assert.LessOrEqual(rig.body.linearVelocity.z, 1.11f);
                Assert.GreaterOrEqual(rig.body.linearVelocity.z, 1f);
            }
            finally { Object.Destroy(rig.root); }
        }

        [UnityTest]
        public IEnumerator DepletedRunningEnergyCancelsWithoutFreeThrust()
        {
            var rig = MakeRig("running energy", 100f, 1000f);
            try
            {
                var store = rig.root.AddComponent<BoosterEnergyReservoir>();
                store.Configure(0.05f, 0.05f, 0f);
                rig.booster.SetEnergySource(store);
                rig.booster.Configure(1000f, 1f, 2f, 0f, 0f, 0f, 10f);
                Assert.IsTrue(rig.booster.TryActivate());
                for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
                Assert.IsFalse(rig.booster.IsBoosting);
                Assert.AreEqual(0f, rig.body.linearVelocity.z, 0.0001f);
            }
            finally { Object.Destroy(rig.root); }
        }

        [UnityTest]
        public IEnumerator DamageDuringBoostCancelsThrustImmediately()
        {
            var rig = MakeRig("damage interrupt", 100f, 1000f);
            try
            {
                Assert.IsTrue(rig.booster.TryActivate());
                for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
                float speedBeforeDamage = rig.body.linearVelocity.z;
                Assert.Greater(speedBeforeDamage, 0f);
                rig.booster.SetOperational(false);
                for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
                Assert.IsFalse(rig.booster.IsBoosting);
                Assert.AreEqual(speedBeforeDamage, rig.body.linearVelocity.z, 0.015f);
            }
            finally { Object.Destroy(rig.root); }
        }

        [UnityTest]
        public IEnumerator OffsetThrustPositionCanGenerateRotation()
        {
            var rig = MakeRig("physical torque", 100f, 100f);
            var mount = new GameObject("offset nozzle").transform;
            mount.SetParent(rig.root.transform, false);
            mount.localPosition = new Vector3(0.5f, 0f, 0f);
            rig.booster.Configure(100f, 0.5f, 2f, 0f, 0f, 0f, 0f, true);
            rig.booster.Bind(rig.body, mount);
            try
            {
                Assert.IsTrue(rig.booster.TryActivate());
                for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
                Assert.Greater(rig.body.angularVelocity.magnitude, 0.001f);
            }
            finally { Object.Destroy(rig.root); }
        }

        [UnityTest]
        public IEnumerator DisabledModuleCannotBoost()
        {
            var rig = MakeRig("disabled", 100f, 1000f);
            try
            {
                rig.booster.SetOperational(false);
                Assert.IsFalse(rig.booster.TryActivate());
                Assert.AreEqual(VehicleBooster.ActivationFailure.DisabledModule,
                    rig.booster.LastActivationFailure);
                yield return new WaitForFixedUpdate();
                Assert.AreEqual(0f, rig.body.linearVelocity.z, 0.0001f);
            }
            finally { Object.Destroy(rig.root); }
        }
    }
}
