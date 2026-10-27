using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Mechanics.MOV002JumpDrive.Tests
{
    public sealed class JumpDriveMathTests
    {
        private static bool Calculate(float gravity, float height, float supportedMass, float mass,
            float verticalSpeed, float speedCap, out float deltaV, out float impulse) =>
            JumpDriveMath.TryCalculate(gravity, height, supportedMass, mass, verticalSpeed, speedCap,
                out deltaV, out impulse);

        [Test]
        public void NominalJumpMatchesBallisticHeight()
        {
            Assert.IsTrue(Calculate(9.81f, 1.8f, 100f, 100f, 0f, 20f, out float dv, out float impulse));
            Assert.AreEqual(Mathf.Sqrt(2f * 9.81f * 1.8f), dv, 0.0001f);
            Assert.AreEqual(dv * 100f, impulse, 0.001f);
        }
        [Test]
        public void LightVehicleDoesNotGainExtraHeight()
        {
            Assert.IsTrue(Calculate(9.81f, 2f, 100f, 10f, 0f, 20f, out float dv, out _));
            Assert.AreEqual(Mathf.Sqrt(39.24f), dv, 0.0001f);
        }
        [Test]
        public void HeavyVehicleGetsSmallerVelocityIncrease()
        {
            Calculate(9.81f, 2f, 100f, 100f, 0f, 20f, out float light, out float lightImpulse);
            Calculate(9.81f, 2f, 100f, 200f, 0f, 20f, out float heavy, out float heavyImpulse);
            Assert.AreEqual(light * 0.5f, heavy, 0.001f);
            Assert.AreEqual(lightImpulse, heavyImpulse, 0.001f);
        }
        [Test]
        public void AtCapCannotJump()
        {
            Assert.IsFalse(Calculate(9.81f, 2f, 100f, 100f, 7f, 7f, out float dv, out float impulse));
            Assert.AreEqual(0f, dv);
            Assert.AreEqual(0f, impulse);
        }
        [Test]
        public void NearCapOnlyAddsRemainingSpeed()
        {
            Assert.IsTrue(Calculate(9.81f, 2f, 100f, 100f, 6f, 7f, out float dv, out _));
            Assert.AreEqual(1f, dv, 0.0001f);
        }
        [Test]
        public void DownwardVelocityGetsFullDeltaV()
        {
            Assert.IsTrue(Calculate(9.81f, 2f, 100f, 100f, -4f, 7f, out float dv, out _));
            Assert.AreEqual(Mathf.Sqrt(39.24f), dv, 0.0001f);
        }
        [TestCase(0f, 2f, 100f, 100f, 0f, 9f)]
        [TestCase(-9.81f, 2f, 100f, 100f, 0f, 9f)]
        [TestCase(9.81f, 0f, 100f, 100f, 0f, 9f)]
        [TestCase(9.81f, 2f, 0f, 100f, 0f, 9f)]
        [TestCase(9.81f, 2f, 100f, 0f, 0f, 9f)]
        [TestCase(9.81f, 2f, 100f, 100f, 0f, 0f)]
        public void InvalidPhysicalInputsRejected(float g, float h, float nominal, float mass, float v, float cap)
        {
            Assert.IsFalse(Calculate(g, h, nominal, mass, v, cap, out _, out _));
        }
        [Test]
        public void NonFiniteInputsRejected()
        {
            Assert.IsFalse(Calculate(9.81f, float.NaN, 100f, 100f, 0f, 9f, out _, out _));
            Assert.IsFalse(Calculate(9.81f, 2f, float.PositiveInfinity, 100f, 0f, 9f, out _, out _));
            Assert.IsFalse(Calculate(9.81f, 2f, 100f, 100f, float.NaN, 9f, out _, out _));
        }
        [Test]
        public void HighMassDoesNotOverflow()
        {
            Assert.IsTrue(Calculate(9.81f, 2f, 100f, 100000f, 0f, 9f, out float dv, out float impulse));
            Assert.Greater(dv, 0f);
            Assert.IsFalse(float.IsInfinity(impulse));
        }
    }
}
