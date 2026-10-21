using NUnit.Framework;
using UnityEngine;
using RoboRougelike.Mechanics.MOV004GyroscopicStabilizer;

namespace RoboRougelike.Mechanics.MOV004GyroscopicStabilizer.Tests
{
    public sealed class GyroStabilizationMathTests
    {
        [Test]
        public void UprightAtRest_HasNoCorrection()
        {
            Vector3 acc = GyroStabilizationMath.Calculate(Quaternion.identity, Vector3.zero, Vector3.up, 15f, 6f, 12f);
            Assert.Less(acc.sqrMagnitude, 0.000001f);
        }

        [Test]
        public void PureYawSpin_IsNotDamped()
        {
            Vector3 acc = GyroStabilizationMath.Calculate(Quaternion.Euler(0f, 20f, 0f), Vector3.up * 4f, Vector3.up, 15f, 6f, 12f);
            Assert.Less(acc.sqrMagnitude, 0.000001f);
        }

        [Test]
        public void RightTilt_ReceivesNegativeRollCorrection()
        {
            Vector3 acc = GyroStabilizationMath.Calculate(Quaternion.Euler(0f, 0f, 25f), Vector3.zero, Vector3.up, 15f, 6f, 12f);
            Assert.Less(acc.z, -0.1f);
        }

        [Test]
        public void ForwardTilt_ReceivesNegativePitchCorrection()
        {
            Vector3 acc = GyroStabilizationMath.Calculate(Quaternion.Euler(25f, 0f, 0f), Vector3.zero, Vector3.up, 15f, 6f, 12f);
            Assert.Less(acc.x, -0.1f);
        }

        [Test]
        public void TiltSpin_IsDampedEvenAtZeroStiffness()
        {
            Vector3 acc = GyroStabilizationMath.Calculate(Quaternion.identity, Vector3.right * 1.5f, Vector3.up, 0f, 3f, 12f);
            Assert.Less(acc.x, 0f);
        }

        [Test]
        public void YawComponent_IsZeroEvenWhenTilted()
        {
            Vector3 acc = GyroStabilizationMath.Calculate(Quaternion.Euler(15f, 45f, 25f),
                new Vector3(2f, 5f, -3f), Vector3.up, 15f, 6f, 20f);
            Assert.AreEqual(0f, acc.y, 0.0001f);
        }

        [Test]
        public void Acceleration_IsClamped()
        {
            Vector3 acc = GyroStabilizationMath.Calculate(Quaternion.Euler(70f, 0f, 40f), Vector3.zero, Vector3.up, 100f, 0f, 2f);
            Assert.LessOrEqual(acc.magnitude, 2.0001f);
        }

        [Test]
        public void UpsideDown_GetsFiniteRecoveryAxis()
        {
            Vector3 acc = GyroStabilizationMath.Calculate(Quaternion.Euler(180f, 0f, 0f), Vector3.zero, Vector3.up, 15f, 6f, 12f);
            Assert.Greater(acc.sqrMagnitude, 0.001f);
            Assert.IsFalse(float.IsNaN(acc.x) || float.IsNaN(acc.y) || float.IsNaN(acc.z));
        }

        [Test]
        public void ZeroTargetUp_UsesWorldUp()
        {
            Quaternion q = Quaternion.Euler(0f, 0f, 20f);
            Vector3 a = GyroStabilizationMath.Calculate(q, Vector3.zero, Vector3.zero, 12f, 4f, 10f);
            Vector3 b = GyroStabilizationMath.Calculate(q, Vector3.zero, Vector3.up, 12f, 4f, 10f);
            Assert.Less((a - b).sqrMagnitude, 0.000001f);
        }

        [Test]
        public void CustomTargetUp_CorrectsToGivenNormal()
        {
            Vector3 desired = Quaternion.Euler(0f, 0f, 20f) * Vector3.up;
            Vector3 acc = GyroStabilizationMath.Calculate(Quaternion.identity, Vector3.zero, desired, 12f, 4f, 10f);
            Assert.Greater(acc.z, 0f);
        }

        [Test]
        public void InvalidParameters_ReturnZeroNotNaN()
        {
            Vector3 acc = GyroStabilizationMath.Calculate(Quaternion.identity, Vector3.zero, Vector3.up, float.NaN, 6f, 12f);
            Assert.AreEqual(Vector3.zero, acc);
        }

        [Test]
        public void NoMaximumAcceleration_ReturnsZero()
        {
            Vector3 acc = GyroStabilizationMath.Calculate(Quaternion.Euler(20f, 0f, 0f), Vector3.zero, Vector3.up, 10f, 5f, 0f);
            Assert.AreEqual(Vector3.zero, acc);
        }

        [Test]
        public void DesiredUpParallelToYawAxis_NotAccidentallyDamped()
        {
            Vector3 slopeNormal = new Vector3(0f, 1f, 1f).normalized;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, slopeNormal);
            Vector3 acc = GyroStabilizationMath.Calculate(rotation, slopeNormal * 3f, slopeNormal, 15f, 5f, 12f);
            Assert.Less(acc.magnitude, 0.0002f);
        }
    }
}
