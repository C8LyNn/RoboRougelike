using NUnit.Framework;

namespace RoboRougelike.Mechanics.MOV003Blink.Tests
{
    public sealed class BlinkEnergyTests
    {
        [Test]
        public void SpendingOnlySucceedsWithSufficientEnergy()
        {
            var pool = new BlinkEnergyPool(50f, 20f);
            Assert.IsTrue(pool.CanSpend(15f));
            Assert.IsTrue(pool.TrySpend(15f));
            Assert.AreEqual(5f, pool.Current);
            Assert.IsFalse(pool.TrySpend(10f));
            Assert.AreEqual(5f, pool.Current);
        }
        [Test]
        public void ChargeCapsAtCapacity()
        {
            var pool = new BlinkEnergyPool(10f, 3f);
            pool.Recharge(100f);
            Assert.AreEqual(10f, pool.Current);
        }
        [Test]
        public void InvalidAndNegativeAmountsDoNotChangeEnergy()
        {
            var pool = new BlinkEnergyPool(20f, 13f);
            Assert.IsFalse(pool.TrySpend(-1f));
            Assert.IsFalse(pool.TrySpend(float.NaN));
            pool.Recharge(float.PositiveInfinity);
            pool.Recharge(-3f);
            Assert.AreEqual(13f, pool.Current);
        }
        [Test]
        public void InitialEnergyClampedToCapacity()
        {
            Assert.AreEqual(10f, new BlinkEnergyPool(10f, 99f).Current);
            Assert.AreEqual(0f, new BlinkEnergyPool(10f, -5f).Current);
        }
    }
}
