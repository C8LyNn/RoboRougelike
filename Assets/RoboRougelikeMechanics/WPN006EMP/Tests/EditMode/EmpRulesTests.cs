using NUnit.Framework;
using UnityEngine;
using RoboRougelike.Mechanics.EMP;

namespace RoboRougelike.Mechanics.EMP.Tests
{
    public sealed class EmpRulesTests
    {
        [TestCase(0f, 3f)]
        [TestCase(5f, 2.025f)]
        [TestCase(10f, 1.05f)]
        public void Duration_FallsOffWithDistance(float distance, float expected)
        {
            Assert.That(EmpPulseMath.CalculateDuration(3f, distance, 10f, 0f, .35f),
                Is.EqualTo(expected).Within(.0001f));
        }

        [TestCase(0f, 3f)]
        [TestCase(.25f, 2.25f)]
        [TestCase(.5f, 1.5f)]
        [TestCase(1f, 0f)]
        public void Duration_Resistance(float resistance, float expected)
        {
            Assert.That(EmpPulseMath.CalculateDuration(3f, 0f, 10f, resistance, .35f),
                Is.EqualTo(expected).Within(.0001f));
        }

        [Test]
        public void Duration_OutsideRadius_IsZero() =>
            Assert.That(EmpPulseMath.CalculateDuration(3f, 10.1f, 10f, 0f, .35f), Is.Zero);

        [Test]
        public void Duration_ZeroRadius_IsZero() =>
            Assert.That(EmpPulseMath.CalculateDuration(3f, 0f, 0f, 0f, .35f), Is.Zero);

        [Test]
        public void Duration_NegativeDistance_IsZero() =>
            Assert.That(EmpPulseMath.CalculateDuration(3f, -1f, 10f, 0f, .35f), Is.Zero);

        [Test]
        public void Duration_Nan_IsZero() =>
            Assert.That(EmpPulseMath.CalculateDuration(3f, float.NaN, 10f, 0f, .35f), Is.Zero);

        [Test]
        public void Duration_Infinity_IsZero() =>
            Assert.That(EmpPulseMath.CalculateDuration(3f, float.PositiveInfinity, 10f, 0f, .35f), Is.Zero);

        [Test]
        public void Duration_ClampsResistanceAndEdgeFalloff()
        {
            Assert.That(EmpPulseMath.CalculateDuration(3f, 10f, 10f, -.1f, -1f), Is.Zero);
            Assert.That(EmpPulseMath.CalculateDuration(3f, 0f, 10f, 1.5f, 1f), Is.Zero);
        }

        [Test]
        public void Disruption_IsInitiallyInactive()
        {
            var timer = new EmpDisruptionTimer();
            Assert.False(timer.IsDisrupted);
            Assert.That(timer.Remaining, Is.Zero);
        }

        [Test]
        public void Disruption_FirstApplySignalsStart()
        {
            var timer = new EmpDisruptionTimer();
            Assert.True(timer.Apply(2f));
            Assert.True(timer.IsDisrupted);
        }

        [Test]
        public void Disruption_ReapplyShorterDoesNotStack()
        {
            var timer = new EmpDisruptionTimer();
            timer.Apply(3f);
            Assert.False(timer.Apply(1f));
            Assert.That(timer.Remaining, Is.EqualTo(3f));
        }

        [Test]
        public void Disruption_ReapplyLongerRefreshesToMaximum()
        {
            var timer = new EmpDisruptionTimer();
            timer.Apply(2f);
            timer.Tick(.5f);
            Assert.False(timer.Apply(4f));
            Assert.That(timer.Remaining, Is.EqualTo(4f));
        }

        [Test]
        public void Disruption_TickExpiresExactlyOnce()
        {
            var timer = new EmpDisruptionTimer();
            timer.Apply(.5f);
            Assert.False(timer.Tick(.25f));
            Assert.True(timer.Tick(.25f));
            Assert.False(timer.Tick(1f));
            Assert.False(timer.IsDisrupted);
        }

        [Test]
        public void Disruption_RejectsInvalidDurations()
        {
            var timer = new EmpDisruptionTimer();
            Assert.False(timer.Apply(0f));
            Assert.False(timer.Apply(-5f));
            Assert.False(timer.Apply(float.NaN));
            Assert.False(timer.Apply(float.PositiveInfinity));
            Assert.False(timer.IsDisrupted);
        }

        [Test]
        public void Disruption_BadTickDoesNotAdvance()
        {
            var timer = new EmpDisruptionTimer();
            timer.Apply(2f);
            timer.Tick(float.NaN);
            timer.Tick(-1f);
            Assert.That(timer.Remaining, Is.EqualTo(2f));
        }

        [Test]
        public void Disruption_ClearReleasesImmediately()
        {
            var timer = new EmpDisruptionTimer();
            timer.Apply(2f);
            timer.Clear();
            Assert.False(timer.IsDisrupted);
        }

        [Test]
        public void Cooldown_DefaultReady()
        {
            var timer = new EmpCooldownTimer();
            Assert.True(timer.Ready);
        }

        [Test]
        public void Cooldown_RequiresFullDuration()
        {
            var timer = new EmpCooldownTimer();
            timer.Commit(2f);
            Assert.False(timer.Ready);
            timer.Tick(1f);
            Assert.False(timer.Ready);
            timer.Tick(1f);
            Assert.True(timer.Ready);
        }

        [Test]
        public void Cooldown_HandlesZeroDuration()
        {
            var timer = new EmpCooldownTimer();
            timer.Commit(0f);
            Assert.True(timer.Ready);
        }

        [Test]
        public void Cooldown_NegativeTickDoesNotReset()
        {
            var timer = new EmpCooldownTimer();
            timer.Commit(2f);
            timer.Tick(-1f);
            Assert.That(timer.Remaining, Is.EqualTo(2f));
        }

        [Test]
        public void Cooldown_InvalidCommitFailsSafely()
        {
            var timer = new EmpCooldownTimer();
            timer.Commit(float.NaN);
            Assert.True(timer.Ready);
        }

        [Test]
        public void Radius_ChecksBoundary()
        {
            Assert.True(EmpPulseMath.IsWithinRadius(9f, 9f));
            Assert.False(EmpPulseMath.IsWithinRadius(9.01f, 9f));
        }

        [Test]
        public void Radius_RejectsInvalidValues()
        {
            Assert.False(EmpPulseMath.IsWithinRadius(float.NaN, 9f));
            Assert.False(EmpPulseMath.IsWithinRadius(1f, 0f));
        }
    }
}
