using NUnit.Framework;

namespace RoboRougelike.Defenses.DirectionalShield.Tests.EditMode
{
    public class ShieldStateTests
    {
        private class TestEnergy : IShieldEnergySource
        {
            public float Amount;
            public int Calls;
            public TestEnergy(float amount) { Amount = amount; }
            public bool TryConsume(float amount)
            {
                Calls++;
                if (amount < 0f || Amount < amount) return false;
                Amount -= amount;
                return true;
            }
        }

        [Test] public void FullInitially() => Assert.AreEqual(100f, new ShieldState(100f).Charge);
        [Test] public void NegativeMaximumBecomesZero() => Assert.AreEqual(0f, new ShieldState(-10f).Maximum);
        [Test] public void NanMaximumBecomesZero() => Assert.AreEqual(0f, new ShieldState(float.NaN).Maximum);
        [Test] public void DamageConsumesCapacity()
        {
            var s = new ShieldState(100); Assert.AreEqual(20f, s.Absorb(20, 100, 0)); Assert.AreEqual(80f, s.Charge);
        }
        [Test] public void CannotOverAbsorb()
        {
            var s = new ShieldState(10); Assert.AreEqual(10, s.Absorb(99, 100, 0)); Assert.AreEqual(0, s.Charge);
        }
        [Test] public void HitLimitIsEnforced() => Assert.AreEqual(7, new ShieldState(100).Absorb(50, 7, 0));
        [Test] public void ZeroDamageIgnored() => Assert.AreEqual(0, new ShieldState(100).Absorb(0, 10, 0));
        [Test] public void NegativeDamageIgnored() => Assert.AreEqual(0, new ShieldState(100).Absorb(-5, 10, 0));
        [Test] public void InfiniteDamageIgnored() => Assert.AreEqual(0, new ShieldState(100).Absorb(float.PositiveInfinity, 10, 0));
        [Test] public void NegativeHitLimitIgnored() => Assert.AreEqual(0, new ShieldState(100).Absorb(20, -1, 0));
        [Test] public void NonFiniteEnergyMultiplierIgnored() => Assert.AreEqual(0, new ShieldState(100).Absorb(10, 10, float.NaN));
        [Test] public void EnergyRequirementHonored()
        {
            var e = new TestEnergy(50); var s = new ShieldState(100);
            Assert.AreEqual(10, s.Absorb(10, 100, 2f, e)); Assert.AreEqual(30, e.Amount);
        }
        [Test] public void NoEnergyMeansNoChargeLost()
        {
            var e = new TestEnergy(5); var s = new ShieldState(100);
            Assert.AreEqual(0, s.Absorb(20, 100, 1, e)); Assert.AreEqual(100, s.Charge);
            Assert.AreEqual(5, e.Amount);
        }
        [Test] public void MissingEnergySourceDoesNotConsume()
        {
            var s = new ShieldState(100); Assert.AreEqual(0, s.Absorb(20, 100, 1)); Assert.AreEqual(100, s.Charge);
        }
        [Test] public void WithoutEnergyCostWorksWithoutSource() => Assert.AreEqual(20, new ShieldState(100).Absorb(20,100,0));
        [Test] public void DelayBeforeRegeneration()
        {
            var s = new ShieldState(100); s.Absorb(50,100,0);
            Assert.AreEqual(0, s.Advance(1,10,2)); Assert.AreEqual(50, s.Charge);
        }
        [Test] public void RegenerationAfterDelay()
        {
            var s = new ShieldState(100); s.Absorb(50,100,0);
            s.Advance(2,10,2); Assert.AreEqual(10, s.Advance(1,10,2));
        }
        [Test] public void RegenerationCrossingDelayOnlyPartialStep()
        {
            var s = new ShieldState(100); s.Absorb(50,100,0);
            Assert.AreEqual(5, s.Advance(2.5f,10,2));
        }
        [Test] public void RegenerationClampedToMaximum()
        {
            var s = new ShieldState(100); s.Absorb(5,100,0);
            Assert.AreEqual(5, s.Advance(20,10,0)); Assert.AreEqual(100, s.Charge);
        }
        [Test] public void RegenerationWithEnergy()
        {
            var s = new ShieldState(100); var e = new TestEnergy(100); s.Absorb(50,100,0);
            Assert.AreEqual(10, s.Advance(1,10,0,2,e)); Assert.AreEqual(80, e.Amount);
        }
        [Test] public void RegenerationRejectedWithInsufficientEnergy()
        {
            var s = new ShieldState(100); var e = new TestEnergy(5); s.Absorb(50,100,0);
            Assert.AreEqual(0, s.Advance(1,10,0,2,e)); Assert.AreEqual(50, s.Charge);
        }
        [Test] public void MissingEnergyPreventsPaidRegeneration()
        {
            var s = new ShieldState(100); s.Absorb(50,100,0);
            Assert.AreEqual(0, s.Advance(1,10,0,2));
        }
        [Test] public void NegativeDeltaIgnored()
        {
            var s = new ShieldState(100); s.Absorb(20,100,0);
            Assert.AreEqual(0, s.Advance(-1,10,0)); Assert.AreEqual(80, s.Charge);
        }
        [Test] public void NanDeltaIgnored()
        {
            var s = new ShieldState(100); s.Absorb(20,100,0);
            Assert.AreEqual(0, s.Advance(float.NaN,10,0));
        }
        [Test] public void ResetRestoresFullCharge()
        {
            var s = new ShieldState(100); s.Absorb(99,100,0); s.ResetFull(); Assert.AreEqual(100,s.Charge);
        }
        [Test] public void SetChargeClamps()
        {
            var s = new ShieldState(100); s.SetCharge(-50); Assert.AreEqual(0,s.Charge);
            s.SetCharge(999); Assert.AreEqual(100,s.Charge);
        }
        [Test] public void SetChargeIgnoresNan()
        {
            var s = new ShieldState(100); s.SetCharge(float.NaN); Assert.AreEqual(100,s.Charge);
        }
        [Test] public void RegenerationStartsImmediatelyOnNewReducedState()
        {
            var s = new ShieldState(100); s.SetCharge(0);
            Assert.AreEqual(5, s.Advance(1,5,3));
        }
    }
}
