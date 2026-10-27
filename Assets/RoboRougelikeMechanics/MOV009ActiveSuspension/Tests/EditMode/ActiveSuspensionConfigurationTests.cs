using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Mechanics.MOV009.Tests
{
    public sealed class ActiveSuspensionConfigurationTests
    {
        private GameObject chassis;
        private GameObject child;
        private ActiveSuspensionController controller;
        private ActiveSuspensionModule module;

        [SetUp]
        public void SetUp()
        {
            chassis = new GameObject("Test chassis");
            chassis.AddComponent<Rigidbody>();
            controller = chassis.AddComponent<ActiveSuspensionController>();
            child = new GameObject("Mount");
            child.transform.SetParent(chassis.transform);
            module = child.AddComponent<ActiveSuspensionModule>();
        }

        [TearDown]
        public void TearDown()
        {
            if (chassis != null) Object.DestroyImmediate(chassis);
            if (child != null) Object.DestroyImmediate(child);
        }

        [Test]
        public void ModuleDiscoveredByRefreshInEditMode()
        {
            // Ordinary MonoBehaviours do not receive OnEnable in EditMode.
            // Runtime automatic registration is exercised by the PlayMode rig.
            controller.RefreshModules();
            Assert.AreEqual(1, controller.RegisteredModuleCount);
        }

        [Test]
        public void ModuleUsesSameChassisRigidBody() => Assert.IsTrue(module.IsAvailableFor(chassis.GetComponent<Rigidbody>()));

        [Test]
        public void SettingNonOperationalDisablesModule()
        {
            module.SetOperational(false);
            Assert.IsFalse(module.IsAvailableFor(chassis.GetComponent<Rigidbody>()));
        }

        [Test]
        public void SuspendedControllerCanBeDisabled()
        {
            controller.SetSuspensionEnabled(false);
            Assert.IsFalse(controller.SuspensionEnabled);
        }

        [Test]
        public void ModeCanBeChanged()
        {
            controller.SetSupportMode(ActiveSuspensionSupportMode.FullSupport);
            Assert.AreEqual(ActiveSuspensionSupportMode.FullSupport, controller.SupportMode);
        }

        [Test]
        public void OffsetIsClamped()
        {
            controller.SetMaximumHeightOffset(0.25f);
            controller.SetRideHeightOffset(100f);
            Assert.AreEqual(0.25f, controller.RequestedRideHeightOffsetMeters, 0.00001f);
            controller.SetRideHeightOffset(-100f);
            Assert.AreEqual(-0.25f, controller.RequestedRideHeightOffsetMeters, 0.00001f);
        }

        [Test]
        public void SpringConfigurationRejectsInvalidValues()
        {
            module.ConfigureSpring(-1f, float.NaN, -100f, -100f);
            Assert.Greater(module.RideHeightMeters, 0f);
        }

        [Test]
        public void NoDuplicateRegistrations()
        {
            controller.Register(module);
            controller.Register(module);
            controller.RefreshModules();
            Assert.AreEqual(1, controller.RegisteredModuleCount);
        }

        [Test]
        public void BatteryDeductsOnlySuccessfulSpend()
        {
            var battery = chassis.AddComponent<ActiveSuspensionEnergyStore>();
            battery.Configure(30f, 0f, 10f);
            Assert.IsTrue(battery.TryConsumeEnergy(7f));
            Assert.IsFalse(battery.TryConsumeEnergy(4f));
            Assert.AreEqual(3f, battery.Energy, 0.00001f);
        }

        [Test]
        public void BatteryRejectsNegativeAndNonFiniteCosts()
        {
            var battery = chassis.AddComponent<ActiveSuspensionEnergyStore>();
            battery.Configure(30f, 0f, 10f);
            Assert.IsFalse(battery.TryConsumeEnergy(-1f));
            Assert.IsFalse(battery.TryConsumeEnergy(float.NaN));
            Assert.AreEqual(10f, battery.Energy, 0.00001f);
        }
    }
}
