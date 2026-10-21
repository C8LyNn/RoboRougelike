using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.Mechanics.MOV004GyroscopicStabilizer;

namespace RoboRougelike.Mechanics.MOV004GyroscopicStabilizer.Tests
{
    public sealed class GyroStabilizerPlayModeTests
    {
        private GameObject root;
        private GameObject module;
        private Rigidbody body;
        private GyroStabilizer gyro;

        [SetUp]
        public void Setup()
        {
            root = new GameObject("MOV004_vehicle");
            root.transform.rotation = Quaternion.Euler(0f, 0f, 30f);
            body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.mass = 80f;
            body.angularDamping = 0f;
            body.linearDamping = 0f;
            body.maxAngularVelocity = 30f;
            root.AddComponent<BoxCollider>();
            module = new GameObject("MOV004_module");
            module.transform.SetParent(root.transform, false);
            gyro = module.AddComponent<GyroStabilizer>();
            gyro.Configure(16f, 8f, 12f);
        }

        [TearDown]
        public void TearDown()
        {
            if (module != null) Object.DestroyImmediate(module);
            if (root != null) Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator BindsToVehicleAncestor()
        {
            yield return null;
            Assert.AreSame(body, gyro.VehicleBody);
        }

        [UnityTest]
        public IEnumerator CorrectsTiltWithoutSettingPoseDirectly()
        {
            float initial = Vector3.Angle(body.transform.up, Vector3.up);
            for (int i = 0; i < 65; i++) yield return new WaitForFixedUpdate();
            float after = Vector3.Angle(body.transform.up, Vector3.up);
            Assert.Less(after, initial - 10f, "Gyro should materially recover from tilt.");
        }

        [UnityTest]
        public IEnumerator DoesNotDeliberatelyBrakeYaw()
        {
            body.rotation = Quaternion.identity;
            body.angularVelocity = Vector3.up * 2f;
            yield return new WaitForFixedUpdate();
            Assert.Less(gyro.LastAngularAcceleration.magnitude, 0.001f);
            Assert.AreEqual(2f, body.angularVelocity.y, 0.2f);
        }

        [UnityTest]
        public IEnumerator DisablingRequestedStateStopsCorrections()
        {
            gyro.SetRequestedActive(false);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(Vector3.zero, gyro.LastAngularAcceleration);
        }

        [UnityTest]
        public IEnumerator BrokenModuleStopsCorrections()
        {
            gyro.SetOperational(false);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(Vector3.zero, gyro.LastAngularAcceleration);
        }

        [UnityTest]
        public IEnumerator DetachedModuleStopsCorrections()
        {
            module.transform.SetParent(null, true);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(Vector3.zero, gyro.LastAngularAcceleration);
        }

        [UnityTest]
        public IEnumerator KinematicBodyIsNotForced()
        {
            body.isKinematic = true;
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(Vector3.zero, gyro.LastAngularAcceleration);
        }

        [UnityTest]
        public IEnumerator PositiveEnergyCostWithoutProviderFailsClosed()
        {
            gyro.SetEnergyCostPerSecond(10f);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(Vector3.zero, gyro.LastAngularAcceleration);
        }

        [UnityTest]
        public IEnumerator EnoughEnergyAllowsCorrection()
        {
            GameObject store = new GameObject("energy");
            try
            {
                GyroPrototypeEnergyStore provider = store.AddComponent<GyroPrototypeEnergyStore>();
                gyro.SetEnergySource(provider);
                // Spend more than the prototype store's 10/s recharge so use stays observable.
                gyro.SetEnergyCostPerSecond(20f);
                yield return new WaitForFixedUpdate();
                Assert.Greater(gyro.LastAngularAcceleration.sqrMagnitude, 0.001f);
                Assert.Less(provider.CurrentEnergy, provider.Capacity);
            }
            finally { Object.DestroyImmediate(store); }
        }

        [UnityTest]
        public IEnumerator EmptyEnergyStoreBlocksCorrection()
        {
            GameObject store = new GameObject("energy");
            try
            {
                GyroPrototypeEnergyStore provider = store.AddComponent<GyroPrototypeEnergyStore>();
                provider.SetEnergy(0f);
                gyro.SetEnergySource(provider);
                gyro.SetEnergyCostPerSecond(100000f);
                yield return new WaitForFixedUpdate();
                Assert.AreEqual(Vector3.zero, gyro.LastAngularAcceleration);
            }
            finally { Object.DestroyImmediate(store); }
        }

        [UnityTest]
        public IEnumerator MultipleModulesCanShareTheSameBody()
        {
            GameObject second = new GameObject("second_gyro");
            second.transform.SetParent(root.transform, false);
            try
            {
                GyroStabilizer other = second.AddComponent<GyroStabilizer>();
                other.Configure(5f, 3f, 5f);
                yield return new WaitForFixedUpdate();
                Assert.AreSame(body, other.VehicleBody);
                Assert.Greater(gyro.LastAngularAcceleration.sqrMagnitude, 0.001f);
                Assert.Greater(other.LastAngularAcceleration.sqrMagnitude, 0.001f);
            }
            finally { Object.DestroyImmediate(second); }
        }

        [UnityTest]
        public IEnumerator CanRebindAfterMovingModuleToAnotherVehicle()
        {
            GameObject otherRoot = new GameObject("other_vehicle");
            try
            {
                Rigidbody otherBody = otherRoot.AddComponent<Rigidbody>();
                otherBody.useGravity = false;
                module.transform.SetParent(otherRoot.transform, true);
                Assert.IsTrue(gyro.BindToVehicle(otherBody));
                Assert.AreSame(otherBody, gyro.VehicleBody);
                yield return new WaitForFixedUpdate();
            }
            finally { Object.DestroyImmediate(otherRoot); module = null; }
        }

        [UnityTest]
        public IEnumerator InvalidBindingCannotStealOtherVehicle()
        {
            GameObject other = new GameObject("other");
            try
            {
                Rigidbody foreignBody = other.AddComponent<Rigidbody>();
                Assert.IsFalse(gyro.BindToVehicle(foreignBody));
                Assert.AreSame(body, gyro.VehicleBody);
                yield return null;
            }
            finally { Object.DestroyImmediate(other); }
        }
    }
}
