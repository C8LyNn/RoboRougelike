using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Mechanics.MOV009.Tests
{
    public sealed class SuspensionMathTests
    {
        [TestCase(0.5f, 0.7f, 0f)]
        [TestCase(0.5f, 0.5f, 0f)]
        [TestCase(0.5f, -1f, 0f)]
        public void NoSupportOutsideCompression(float target, float measured, float expected)
        {
            Assert.AreEqual(expected, SuspensionMath.LiftForceNewtons(target, measured, 0f, 1000f, 100f, 5000f), 0.0001f);
        }

        [Test]
        public void CompressionProducesKnownForce() =>
            Assert.AreEqual(200f, SuspensionMath.LiftForceNewtons(0.6f, 0.4f, 0f, 1000f, 0f, 5000f), 0.001f);

        [Test]
        public void DownwardVelocityIncreasesSupport() =>
            Assert.AreEqual(400f, SuspensionMath.LiftForceNewtons(0.6f, 0.4f, -2f, 1000f, 100f, 5000f), 0.001f);

        [Test]
        public void UpwardVelocityDampsSupport() =>
            Assert.AreEqual(100f, SuspensionMath.LiftForceNewtons(0.6f, 0.4f, 1f, 1000f, 100f, 5000f), 0.001f);

        [Test]
        public void NeverPullsVehicleDown() =>
            Assert.AreEqual(0f, SuspensionMath.LiftForceNewtons(0.6f, 0.4f, 10f, 1000f, 1000f, 5000f));

        [Test]
        public void ModuleForceIsLimited() =>
            Assert.AreEqual(500f, SuspensionMath.LiftForceNewtons(1f, 0f, -8f, 40000f, 4000f, 500f));

        [Test]
        public void ZeroForceLimitIsZero() =>
            Assert.AreEqual(0f, SuspensionMath.LiftForceNewtons(1f, 0.1f, 0f, 1000f, 0f, 0f));

        [TestCase(-10f, 0.2f)]
        [TestCase(0.1f, 0.6f)]
        [TestCase(10f, 1.2f)]
        public void HeightClamped(float offset, float expected) =>
            Assert.AreEqual(expected, SuspensionMath.DesiredHeight(0.5f, offset, 0.2f, 1.2f), 0.00001f);

        [TestCase(1000f, 500f, 0.5f)]
        [TestCase(1000f, 1200f, 1f)]
        [TestCase(1000f, 0f, 0f)]
        [TestCase(0f, 1000f, 0f)]
        public void SharedCapScaling(float requested, float cap, float expected) =>
            Assert.AreEqual(expected, SuspensionMath.SafeForceScale(requested, cap), 0.00001f);

        [Test]
        public void InvalidNumbersDoNotReachPhysics()
        {
            Assert.AreEqual(0f, SuspensionMath.LiftForceNewtons(float.NaN, float.NaN,
                float.PositiveInfinity, float.NaN, float.NaN, float.NaN));
            Assert.AreEqual(0f, SuspensionMath.SafeForceScale(float.NaN, 1000f));
            Assert.AreEqual(0.5f, SuspensionMath.DesiredHeight(0.5f, float.NaN, 0.2f, 1f));
        }

        [Test]
        public void MoreCompressionMeansMoreForce() =>
            Assert.Greater(SuspensionMath.LiftForceNewtons(0.6f, 0.2f, 0f, 1000f, 0f, 5000f),
                SuspensionMath.LiftForceNewtons(0.6f, 0.5f, 0f, 1000f, 0f, 5000f));
    }
}
