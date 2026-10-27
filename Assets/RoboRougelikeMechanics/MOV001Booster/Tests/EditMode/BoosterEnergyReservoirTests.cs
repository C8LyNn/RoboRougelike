using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Mechanics.Booster.Tests
{
    public sealed class BoosterEnergyReservoirTests
    {
        private GameObject container;
        private BoosterEnergyReservoir store;

        [SetUp]
        public void SetUp()
        {
            container = new GameObject("Test energy store");
            store = container.AddComponent<BoosterEnergyReservoir>();
            store.Configure(100f, 40f, 5f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(container);
        }

        [Test]
        public void ConsumptionIsAtomic()
        {
            Assert.IsTrue(store.TryConsume(25f));
            Assert.IsFalse(store.TryConsume(20f));
            Assert.AreEqual(15f, store.CurrentEnergy, 0.0001f);
        }

        [Test]
        public void RegenerationStopsAtCapacity()
        {
            store.Regenerate(100f);
            Assert.AreEqual(100f, store.CurrentEnergy, 0.0001f);
        }

        [Test]
        public void ZeroCostWorksWithoutEnergy()
        {
            store.Configure(0f, 0f, 0f);
            Assert.IsTrue(store.TryConsume(0f));
            Assert.IsFalse(store.TryConsume(0.1f));
        }

        [Test]
        public void NegativeOrNanCostCannotIncreaseEnergy()
        {
            Assert.IsFalse(store.TryConsume(-5f));
            Assert.IsFalse(store.TryConsume(float.NaN));
            Assert.AreEqual(40f, store.CurrentEnergy, 0.0001f);
        }

        [Test]
        public void ConfigurationClampsStartingEnergy()
        {
            store.Configure(25f, 60f, 0f);
            Assert.AreEqual(25f, store.CurrentEnergy, 0.0001f);
        }
    }
}
