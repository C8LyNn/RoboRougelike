using NUnit.Framework;

namespace RoboRougelike.Mechanics.MOV002JumpDrive.Tests
{
    public sealed class JumpDriveEnergyTests
    {
        [Test]
        public void SpendOnlyWhenEnoughEnergy()
        {
            var pool = new JumpDriveEnergyPool(100f, 25f);
            Assert.IsTrue(pool.TrySpend(20f));
            Assert.AreEqual(5f, pool.Current);
            Assert.IsFalse(pool.TrySpend(10f));
            Assert.AreEqual(5f, pool.Current);
        }
        [Test]
        public void RegenerationClampsToCapacity()
        {
            var pool = new JumpDriveEnergyPool(100f, 90f);
            pool.Regenerate(50f);
            Assert.AreEqual(100f, pool.Current);
        }
        [Test]
        public void RefundCannotExceedCapacity()
        {
            var pool = new JumpDriveEnergyPool(100f, 50f);
            pool.Refund(500f);
            Assert.AreEqual(100f, pool.Current);
        }
        [Test]
        public void NegativeAndNaNAmountsRejected()
        {
            var pool = new JumpDriveEnergyPool(50f, 40f);
            Assert.IsFalse(pool.TrySpend(-1f));
            Assert.IsFalse(pool.TrySpend(float.NaN));
            pool.Refund(float.PositiveInfinity);
            Assert.AreEqual(40f, pool.Current);
        }
        [Test]
        public void StartingEnergyClamped()
        {
            Assert.AreEqual(10f, new JumpDriveEnergyPool(10f, 99f).Current);
            Assert.AreEqual(0f, new JumpDriveEnergyPool(10f, -2f).Current);
        }
        [Test]
        public void ResetToFullRestoresCapacity()
        {
            var pool = new JumpDriveEnergyPool(70f, 25f);
            pool.ResetToFull();
            Assert.AreEqual(70f, pool.Current);
        }
    }
}
