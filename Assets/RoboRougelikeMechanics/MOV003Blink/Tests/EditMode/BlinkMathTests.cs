using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Mechanics.MOV003Blink.Tests
{
    public sealed class BlinkMathTests
    {
        [Test]
        public void PlansHorizontalDisplacement()
        {
            Assert.IsTrue(BlinkMath.TryPlan(new Vector3(1f, 3f, 2f), Vector3.forward, 8f, out Vector3 target));
            Assert.AreEqual(new Vector3(1f, 3f, 10f), target);
        }
        [Test]
        public void IgnoresVerticalComponentOfDirection()
        {
            Assert.IsTrue(BlinkMath.TryPlan(Vector3.one, new Vector3(0f, 10f, 1f), 5f, out Vector3 target));
            Assert.That(target.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(target.y, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(target.z, Is.EqualTo(6f).Within(0.0001f));
        }
        [Test]
        public void SupportsArbitraryGroundPlaneDirection()
        {
            Assert.IsTrue(BlinkMath.TryPlan(Vector3.zero, new Vector3(1f, 4f, 1f), 10f, out Vector3 target));
            Assert.That(target.x, Is.EqualTo(7.0710678f).Within(0.0001f));
            Assert.That(target.z, Is.EqualTo(7.0710678f).Within(0.0001f));
        }
        [TestCase(0f)]
        [TestCase(-1f)]
        public void NonPositiveDistanceRejected(float distance)
        {
            Assert.IsFalse(BlinkMath.TryPlan(Vector3.zero, Vector3.forward, distance, out _));
        }
        [Test]
        public void PurelyVerticalDirectionRejected() =>
            Assert.IsFalse(BlinkMath.TryPlan(Vector3.zero, Vector3.up, 4f, out _));
        [Test]
        public void NonFiniteInputsRejected()
        {
            Assert.IsFalse(BlinkMath.TryPlan(new Vector3(float.NaN, 0f, 0f), Vector3.forward, 2f, out _));
            Assert.IsFalse(BlinkMath.TryPlan(Vector3.zero, new Vector3(float.PositiveInfinity, 0f, 0f), 2f, out _));
            Assert.IsFalse(BlinkMath.TryPlan(Vector3.zero, Vector3.forward, float.NaN, out _));
            Assert.IsFalse(BlinkMath.TryPlan(Vector3.zero, Vector3.forward, float.PositiveInfinity, out _));
        }
    }
}
