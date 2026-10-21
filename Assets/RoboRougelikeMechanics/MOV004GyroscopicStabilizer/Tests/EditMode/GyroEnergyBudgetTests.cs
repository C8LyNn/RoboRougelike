using NUnit.Framework;
using RoboRougelike.Mechanics.MOV004GyroscopicStabilizer;

namespace RoboRougelike.Mechanics.MOV004GyroscopicStabilizer.Tests
{
    public sealed class GyroEnergyBudgetTests
    {
        [Test]
        public void StartsFullByDefault()
        {
            GyroEnergyBudget budget = new GyroEnergyBudget(50f, 5f);
            Assert.AreEqual(50f, budget.Current, 0.0001f);
        }

        [Test]
        public void CanStartEmpty()
        {
            GyroEnergyBudget budget = new GyroEnergyBudget(50f, 5f, false);
            Assert.AreEqual(0f, budget.Current, 0.0001f);
        }

        [Test]
        public void SuccessfulSpendDebitsOnce()
        {
            GyroEnergyBudget budget = new GyroEnergyBudget(20f, 0f);
            Assert.IsTrue(budget.TrySpend(7f));
            Assert.AreEqual(13f, budget.Current, 0.0001f);
        }

        [Test]
        public void InsufficientFundsDoesNotDebit()
        {
            GyroEnergyBudget budget = new GyroEnergyBudget(10f, 0f);
            Assert.IsFalse(budget.TrySpend(11f));
            Assert.AreEqual(10f, budget.Current, 0.0001f);
        }

        [Test]
        public void RechargeClampsToCapacity()
        {
            GyroEnergyBudget budget = new GyroEnergyBudget(10f, 50f, false);
            budget.Tick(1f);
            Assert.AreEqual(10f, budget.Current, 0.0001f);
        }

        [Test]
        public void RechargeAdvancesWithDeltaTime()
        {
            GyroEnergyBudget budget = new GyroEnergyBudget(10f, 4f, false);
            budget.Tick(0.5f);
            Assert.AreEqual(2f, budget.Current, 0.0001f);
        }

        [Test]
        public void NegativeSpendIsRejected()
        {
            GyroEnergyBudget budget = new GyroEnergyBudget(20f, 0f);
            Assert.IsFalse(budget.TrySpend(-5f));
            Assert.AreEqual(20f, budget.Current, 0.0001f);
        }

        [Test]
        public void NonFiniteSpendIsRejected()
        {
            GyroEnergyBudget budget = new GyroEnergyBudget(20f, 0f);
            Assert.IsFalse(budget.TrySpend(float.NaN));
            Assert.IsFalse(budget.TrySpend(float.PositiveInfinity));
            Assert.AreEqual(20f, budget.Current, 0.0001f);
        }

        [Test]
        public void ZeroSpendSucceedsEvenIfEmpty()
        {
            GyroEnergyBudget budget = new GyroEnergyBudget(10f, 0f, false);
            Assert.IsTrue(budget.TrySpend(0f));
        }

        [Test]
        public void ConfigChangeClampsExistingCurrent()
        {
            GyroEnergyBudget budget = new GyroEnergyBudget(20f, 0f);
            budget.Configure(5f, 1f);
            Assert.AreEqual(5f, budget.Current, 0.0001f);
        }

        [Test]
        public void InvalidTickIsIgnored()
        {
            GyroEnergyBudget budget = new GyroEnergyBudget(20f, 5f, false);
            budget.Tick(-1f);
            budget.Tick(float.NaN);
            Assert.AreEqual(0f, budget.Current, 0.0001f);
        }
    }
}
