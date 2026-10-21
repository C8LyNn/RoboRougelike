using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Mechanics.MOV008.Tests
{
    public sealed class CrawlerTrackModuleTests
    {
        private GameObject chassisObject;
        private GameObject moduleObject;
        private GameObject otherObject;

        [SetUp]
        public void Setup()
        {
            chassisObject = new GameObject("Chassis");
            chassisObject.AddComponent<Rigidbody>();
            chassisObject.AddComponent<CrawlerTrackController>();
            moduleObject = new GameObject("Track");
            moduleObject.transform.SetParent(chassisObject.transform);
            moduleObject.AddComponent<CrawlerTrackModule>();
        }

        [TearDown]
        public void Cleanup()
        {
            if (moduleObject != null) Object.DestroyImmediate(moduleObject);
            if (chassisObject != null) Object.DestroyImmediate(chassisObject);
            if (otherObject != null) Object.DestroyImmediate(otherObject);
        }

        [Test]
        public void AttachedOperationalTrackIsAvailable()
        {
            Assert.IsTrue(moduleObject.GetComponent<CrawlerTrackModule>()
                .IsAvailableFor(chassisObject.GetComponent<Rigidbody>()));
        }

        [Test]
        public void DestroyedTrackCannotContribute()
        {
            var track = moduleObject.GetComponent<CrawlerTrackModule>();
            track.SetOperational(false);
            Assert.IsFalse(track.IsAvailableFor(chassisObject.GetComponent<Rigidbody>()));
        }

        [Test]
        public void DisabledTrackCannotContribute()
        {
            var track = moduleObject.GetComponent<CrawlerTrackModule>();
            track.enabled = false;
            Assert.IsFalse(track.IsAvailableFor(chassisObject.GetComponent<Rigidbody>()));
        }

        [Test]
        public void DetachedTrackCannotContribute()
        {
            var track = moduleObject.GetComponent<CrawlerTrackModule>();
            otherObject = new GameObject("Detached");
            moduleObject.transform.SetParent(otherObject.transform);
            Assert.IsFalse(track.IsAvailableFor(chassisObject.GetComponent<Rigidbody>()));
        }

        [Test]
        public void NestedRigidbodyIsNotSameChassis()
        {
            var track = moduleObject.GetComponent<CrawlerTrackModule>();
            moduleObject.AddComponent<Rigidbody>();
            Assert.IsFalse(track.IsAvailableFor(chassisObject.GetComponent<Rigidbody>()));
        }

        [Test]
        public void SideCanBeConfigured()
        {
            var track = moduleObject.GetComponent<CrawlerTrackModule>();
            track.SetSide(CrawlerTrackSide.Right);
            Assert.AreEqual(CrawlerTrackSide.Right, track.Side);
        }

        [Test]
        public void ControllerCanBeDisabledWithoutRemovingModules()
        {
            var ctrl = chassisObject.GetComponent<CrawlerTrackController>();
            ctrl.SetInput(1f, 1f);
            ctrl.SetDriveEnabled(false);
            Assert.IsFalse(ctrl.DriveEnabled);
            Assert.AreEqual(0f, ctrl.ForwardInput);
            Assert.AreEqual(0f, ctrl.SteeringInput);
        }

        [Test]
        public void ControllerClampsInput()
        {
            var ctrl = chassisObject.GetComponent<CrawlerTrackController>();
            ctrl.SetInput(10f, -10f);
            Assert.AreEqual(1f, ctrl.ForwardInput);
            Assert.AreEqual(-1f, ctrl.SteeringInput);
        }

        [Test]
        public void EnergyTankConsumesAndRejectsOverflow()
        {
            var energy = chassisObject.AddComponent<CrawlerPrototypeEnergyStore>();
            energy.Configure(100f, 0f, 15f);
            Assert.IsTrue(energy.TryConsume(10f));
            Assert.IsFalse(energy.TryConsume(6f));
            Assert.AreEqual(5f, energy.CurrentEnergy, 0.001f);
        }

        [Test]
        public void EnergyTankRejectsInvalidCharges()
        {
            var energy = chassisObject.AddComponent<CrawlerPrototypeEnergyStore>();
            energy.Configure(100f, 0f, 15f);
            Assert.IsFalse(energy.TryConsume(float.NaN));
            Assert.IsFalse(energy.TryConsume(-1f));
            Assert.IsFalse(energy.TryConsume(float.PositiveInfinity));
            Assert.AreEqual(15f, energy.CurrentEnergy, 0.001f);
        }

        [Test]
        public void EmptyEnergyTankPreventsConsumption()
        {
            var energy = chassisObject.AddComponent<CrawlerPrototypeEnergyStore>();
            energy.Configure(100f, 0f, 0f);
            Assert.IsFalse(energy.TryConsume(1f));
        }
    }
}
