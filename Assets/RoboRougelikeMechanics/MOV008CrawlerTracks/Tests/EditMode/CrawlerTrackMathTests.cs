using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Mechanics.MOV008.Tests
{
    public sealed class CrawlerTrackMathTests
    {
        [TestCase(-4f, -1f)]
        [TestCase(4f, 1f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(0f, 0f)]
        public void InputsAreClamped(float input, float expected) =>
            Assert.AreEqual(expected, CrawlerTrackMath.ClampInput(input), 0.0001f);

        [Test]
        public void InvalidInputsFailSafe()
        {
            Assert.AreEqual(0f, CrawlerTrackMath.ClampInput(float.NaN));
            Assert.AreEqual(0f, CrawlerTrackMath.ClampInput(float.PositiveInfinity));
            Assert.AreEqual(0f, CrawlerTrackMath.ClampInput(float.NegativeInfinity));
        }

        [TestCase(1f, 0f, 1f, 1f)]
        [TestCase(0f, 1f, 1f, -1f)]
        [TestCase(0f, -1f, -1f, 1f)]
        [TestCase(0.5f, 0.5f, 1f, 0f)]
        [TestCase(-1f, 0f, -1f, -1f)]
        [TestCase(0f, 0f, 0f, 0f)]
        public void DifferentialSteering(float forward, float steer, float left, float right)
        {
            Assert.AreEqual(left, CrawlerTrackMath.SideDemand(forward, steer, CrawlerTrackSide.Left), 0.0001f);
            Assert.AreEqual(right, CrawlerTrackMath.SideDemand(forward, steer, CrawlerTrackSide.Right), 0.0001f);
        }

        [Test]
        public void ReverseSpeedUsesReverseCap()
        {
            Assert.AreEqual(-6f, CrawlerTrackMath.TargetSpeed(-1f, 20f, 6f), 0.001f);
            Assert.AreEqual(20f, CrawlerTrackMath.TargetSpeed(1f, 20f, 6f), 0.001f);
        }

        [Test]
        public void SupportGripLimitedByRatedLoad()
        {
            float light = CrawlerTrackMath.SupportForceLimit(100f, 500f, 10f, 1.5f);
            float heavy = CrawlerTrackMath.SupportForceLimit(2000f, 500f, 10f, 1.5f);
            Assert.AreEqual(1500f, light, 0.001f);
            Assert.AreEqual(7500f, heavy, 0.001f);
        }

        [Test]
        public void SupportGripIsNeverNegative()
        {
            Assert.AreEqual(0f, CrawlerTrackMath.SupportForceLimit(-1f, 100f, 9.81f, 1f));
            Assert.AreEqual(0f, CrawlerTrackMath.SupportForceLimit(100f, -1f, 9.81f, 1f));
            Assert.AreEqual(0f, CrawlerTrackMath.SupportForceLimit(100f, 100f, -9.81f, 1f));
        }

        [Test]
        public void LongitudinalForceAcceleratesForward()
        {
            float force = CrawlerTrackMath.LongitudinalForce(0f, 1f, 200f, 12f, 5f,
                0.4f, 5f, 0.2f, 20000f, 20000f, 10000f, false);
            Assert.Greater(force, 0f);
            Assert.LessOrEqual(force, 10000f);
        }

        [Test]
        public void LongitudinalForceAcceleratesBackwards()
        {
            float force = CrawlerTrackMath.LongitudinalForce(0f, -1f, 200f, 12f, 5f,
                0.4f, 5f, 0.2f, 20000f, 20000f, 10000f, false);
            Assert.Less(force, 0f);
        }

        [Test]
        public void LongitudinalForceStopsAcceleratingAtTargetSpeed()
        {
            float force = CrawlerTrackMath.LongitudinalForce(12f, 1f, 200f, 12f, 5f,
                0.4f, 5f, 0.2f, 20000f, 20000f, 10000f, false);
            Assert.AreEqual(0f, force, 0.0001f);
        }

        [Test]
        public void HoldingThrottleDoesNotFightAnExternalBooster()
        {
            float force = CrawlerTrackMath.LongitudinalForce(24f, 1f, 200f, 12f, 5f,
                0.4f, 5f, 0.2f, 20000f, 20000f, 10000f, false);
            Assert.AreEqual(0f, force, 0.001f);
        }

        [Test]
        public void BrakeStrongerThanCoasting()
        {
            float coast = CrawlerTrackMath.LongitudinalForce(5f, 0f, 200f, 12f, 5f,
                0.4f, 5f, 0.2f, 20000f, 20000f, 10000f, false);
            float brake = CrawlerTrackMath.LongitudinalForce(5f, 0f, 200f, 12f, 5f,
                0.4f, 5f, 0.2f, 20000f, 20000f, 10000f, true);
            Assert.Less(brake, coast);
        }

        [Test]
        public void TractionCapAlwaysApplied()
        {
            float force = CrawlerTrackMath.LongitudinalForce(0f, 1f, 500f, 100f, 100f,
                0.02f, 5f, 0.2f, 1000000f, 1000000f, 350f, false);
            Assert.AreEqual(350f, force, 0.001f);
        }

        [Test]
        public void MotorForceCapAlwaysApplied()
        {
            float force = CrawlerTrackMath.LongitudinalForce(0f, 1f, 500f, 100f, 100f,
                0.02f, 5f, 0.2f, 400f, 1000000f, 1000000f, false);
            Assert.AreEqual(400f, force, 0.001f);
        }

        [Test]
        public void ZeroRatedMotorProducesNoPoweredForce()
        {
            Assert.AreEqual(0f, CrawlerTrackMath.LongitudinalForce(0f, 1f, 100f, 10f, 6f,
                0.4f, 5f, 0.2f, 0f, 5000f, 1000f, false), 0.001f);
        }

        [Test]
        public void LateralGripOpposesSidewaysSlip()
        {
            Assert.Less(CrawlerTrackMath.LateralForce(3f, 200f, 0.5f, 3000f), 0f);
            Assert.Greater(CrawlerTrackMath.LateralForce(-3f, 200f, 0.5f, 3000f), 0f);
        }

        [Test]
        public void LateralGripIsClamped()
        {
            Assert.AreEqual(-50f, CrawlerTrackMath.LateralForce(20f, 100f, 0.02f, 50f), 0.001f);
        }

        [Test]
        public void ForwardIsProjectedOnGroundPlane()
        {
            Vector3 projected = CrawlerTrackMath.ForwardOnGround(Vector3.forward + Vector3.up, Vector3.up);
            Assert.Less(Vector3.Distance(Vector3.forward, projected), 0.0001f);
        }

        [Test]
        public void DegenerateNormalGivesNoDirection() =>
            Assert.AreEqual(Vector3.zero, CrawlerTrackMath.ForwardOnGround(Vector3.forward, Vector3.zero));
    }
}
