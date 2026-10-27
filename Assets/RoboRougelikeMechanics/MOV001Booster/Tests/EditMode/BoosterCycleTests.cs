using System;
using NUnit.Framework;

namespace RoboRougelike.Mechanics.Booster.Tests
{
    public sealed class BoosterCycleTests
    {
        [Test]
        public void StartsReady()
        {
            var cycle = new BoosterCycle(1.5f, 3f);
            Assert.IsTrue(cycle.IsReady);
            Assert.AreEqual(0f, cycle.BoostSecondsRemaining);
        }

        [Test]
        public void ActivateTwice_DoesNotRestartDuration()
        {
            var cycle = new BoosterCycle(1f, 3f);
            Assert.IsTrue(cycle.TryActivate());
            cycle.Advance(0.4f);
            Assert.IsFalse(cycle.TryActivate());
            Assert.AreEqual(0.6f, cycle.BoostSecondsRemaining, 0.0001f);
        }

        [Test]
        public void Advance_ReturnsOnlyActiveFraction()
        {
            var cycle = new BoosterCycle(0.2f, 1f);
            cycle.TryActivate();
            Assert.AreEqual(0.2f, cycle.Advance(0.5f), 0.0001f);
            Assert.AreEqual(BoosterCycle.CyclePhase.CoolingDown, cycle.Phase);
            Assert.AreEqual(0.7f, cycle.CooldownSecondsRemaining, 0.0001f);
        }

        [Test]
        public void Cooldown_PreventsEarlyActivation()
        {
            var cycle = new BoosterCycle(1f, 2f);
            cycle.TryActivate();
            cycle.Advance(1f);
            cycle.Advance(1.5f);
            Assert.IsFalse(cycle.TryActivate());
            cycle.Advance(0.5f);
            Assert.IsTrue(cycle.TryActivate());
        }

        [Test]
        public void Cancel_TransitionsToCooldown()
        {
            var cycle = new BoosterCycle(2f, 3f);
            cycle.TryActivate();
            cycle.Advance(0.2f);
            cycle.Cancel();
            Assert.IsFalse(cycle.IsBoosting);
            Assert.AreEqual(3f, cycle.CooldownSecondsRemaining, 0.0001f);
        }

        [Test]
        public void Cancel_WhenReady_DoesNotStartCooldown()
        {
            var cycle = new BoosterCycle(2f, 3f);
            cycle.Cancel();
            Assert.IsTrue(cycle.IsReady);
        }

        [Test]
        public void ZeroCooldown_ReturnsReadyAfterBoost()
        {
            var cycle = new BoosterCycle(0.1f, 0f);
            cycle.TryActivate();
            cycle.Advance(0.2f);
            Assert.IsTrue(cycle.IsReady);
        }

        [Test]
        public void Reset_MakesAvailableAgain()
        {
            var cycle = new BoosterCycle(1f, 8f);
            cycle.TryActivate();
            cycle.Advance(1f);
            cycle.Reset();
            Assert.IsTrue(cycle.TryActivate());
        }

        [Test]
        public void EqualElapsedTime_IsIndependentOfStepSize()
        {
            var coarse = new BoosterCycle(0.4f, 0.5f);
            var fine = new BoosterCycle(0.4f, 0.5f);
            coarse.TryActivate();
            fine.TryActivate();
            coarse.Advance(1f);
            for (int i = 0; i < 10; i++) fine.Advance(0.1f);
            Assert.AreEqual(coarse.Phase, fine.Phase);
            Assert.AreEqual(coarse.CooldownSecondsRemaining, fine.CooldownSecondsRemaining, 0.0001f);
        }

        [Test]
        public void AdvanceWhileReady_NeverGeneratesThrustTime()
        {
            var cycle = new BoosterCycle(1f, 3f);
            Assert.AreEqual(0f, cycle.Advance(10f));
        }

        [Test]
        public void InvalidConstructorArgumentsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoosterCycle(0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoosterCycle(float.NaN, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoosterCycle(1f, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoosterCycle(1f, float.PositiveInfinity));
        }

        [Test]
        public void InvalidTimeStepIsRejected()
        {
            var cycle = new BoosterCycle(1f, 1f);
            Assert.Throws<ArgumentOutOfRangeException>(() => cycle.Advance(-0.01f));
            Assert.Throws<ArgumentOutOfRangeException>(() => cycle.Advance(float.NaN));
        }
    }
}
