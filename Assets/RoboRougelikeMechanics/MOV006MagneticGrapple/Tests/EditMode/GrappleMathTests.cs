using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Mechanics.MOV006MagneticGrapple.Tests
{
    public sealed class GrappleMathTests
    {
        private static float Tension(float distance = 10f, float rest = 5f,
            float speed = 0f, float stiffness = 100f, float damping = 10f,
            float maxForce = 10000f, float mass = 100f, float acc = 100f,
            float targetMass = 100f, float targetAcc = 100f, bool dynamic = false)
        {
            return GrappleMath.Tension(distance, rest, speed, stiffness,
                damping, maxForce, mass, acc, targetMass, targetAcc, dynamic);
        }

        [Test] public void SlackRopeHasNoForce() => Assert.AreEqual(0f, Tension(distance: 4f));
        [Test] public void ExactlyRestLengthHasNoForce() => Assert.AreEqual(0f, Tension(distance: 5f));
        [Test] public void StretchCreatesForce() => Assert.AreEqual(500f, Tension(), 0.001f);
        [Test] public void SeparatingVelocityIncreasesTension() => Assert.AreEqual(520f, Tension(speed: 2f), 0.001f);
        [Test] public void ApproachingVelocityDecreasesTension() => Assert.AreEqual(480f, Tension(speed: -2f), 0.001f);
        [Test] public void ForceNeverPushes() => Assert.AreEqual(0f, Tension(speed: -100f));
        [Test] public void RespectsAbsoluteForceCap() => Assert.AreEqual(300f, Tension(maxForce: 300f), 0.001f);
        [Test] public void RespectsVehicleAccelerationCap() => Assert.AreEqual(150f, Tension(mass: 30f, acc: 5f), 0.001f);
        [Test] public void RespectsTargetAccelerationCap() => Assert.AreEqual(90f, Tension(dynamic: true, targetMass: 15f, targetAcc: 6f), 0.001f);
        [Test] public void StaticAnchorDoesNotUseTargetAccelerationCap() => Assert.AreEqual(500f, Tension(dynamic: false, targetMass: 1f, targetAcc: 1f));
        [Test] public void InvalidDynamicTargetMassDoesNotApplyForce() => Assert.AreEqual(0f, Tension(dynamic: true, targetMass: 0f));
        [Test] public void NoBodyMassMeansNoForce() => Assert.AreEqual(0f, Tension(mass: 0f));
        [Test] public void ZeroStiffnessCanUseDamping() => Assert.AreEqual(20f, Tension(stiffness: 0f, speed: 2f));
        [Test] public void ReelShortensAtCorrectRate() => Assert.AreEqual(8f, GrappleMath.ReelLength(10f, 2f, 4f, 0.5f), 0.0001f);
        [Test] public void ReelStopsAtMinLength() => Assert.AreEqual(2f, GrappleMath.ReelLength(3f, 2f, 100f, 1f));
        [Test] public void ReelCannotIncreaseShortExistingLine() => Assert.AreEqual(1f, GrappleMath.ReelLength(1f, 3f, 1f, 0.1f));
        [Test] public void ZeroReelSpeedKeepsLength() => Assert.AreEqual(10f, GrappleMath.ReelLength(10f, 2f, 0f, 1f));
        [Test] public void RejectsZeroDirection() => Assert.IsFalse(GrappleMath.HasDirection(Vector3.zero));
        [Test] public void RejectsNaNDirection() => Assert.IsFalse(GrappleMath.HasDirection(new Vector3(float.NaN, 0f, 1f)));
        [Test] public void AcceptsNonzeroDirection() => Assert.IsTrue(GrappleMath.HasDirection(Vector3.forward));
    }
}
