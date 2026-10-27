using NUnit.Framework;
using UnityEngine;
using RoboRougelike.Mechanics.MOV007;

namespace RoboRougelike.Mechanics.MOV007.Tests
{
    public sealed class HoverPhysicsMathTests
    {
        private const float Gravity = 9.81f;
        private static float Lift(float height, float speed = 0f, int pods = 4,
                                  float mass = 320f, float maxLoad = 160f)
        {
            return HoverPhysicsMath.LiftForceNewtons(mass, pods, Gravity,
                height, 1.3f, speed, 1.6f, 0.95f, maxLoad, 2.6f);
        }

        [Test] public void AtTargetHeightBalancesGravityPerPod()
        {
            Assert.That(Lift(1.3f), Is.EqualTo(320f * Gravity / 4f).Within(0.001f));
        }
        [Test] public void LowerPositionProducesMoreLift() => Assert.Greater(Lift(1.1f), Lift(1.3f));
        [Test] public void HigherPositionProducesLessLift() => Assert.Less(Lift(1.5f), Lift(1.3f));
        [Test] public void FallingIncreasesLift() => Assert.Greater(Lift(1.3f, -1f), Lift(1.3f, +1f));
        [Test] public void RisingDecreasesLift() => Assert.Less(Lift(1.3f, +1f), Lift(1.3f, 0f));
        [Test] public void LiftNeverBecomesNegative() => Assert.AreEqual(0f, Lift(2.5f, +20f));
        [Test] public void SinglePodIsLimitedByItsRatedLoad()
        {
            Assert.That(Lift(0.3f, 0f, 1, 10000f), Is.LessThanOrEqualTo(160f * Gravity * 2.6f + 0.01f));
        }
        [Test] public void InvalidMassGivesZero() => Assert.AreEqual(0f, Lift(1.3f, mass:0f));
        [Test] public void InvalidPodCountGivesZero() => Assert.AreEqual(0f, Lift(1.3f, pods:0));
        [Test] public void NoNominalLoadGivesZero()
        {
            Assert.AreEqual(0f, HoverPhysicsMath.LiftForceNewtons(100f, 1, Gravity, 1f, 1.3f, 0f, 1.6f, 1f, 0f, 2f));
        }
        [Test] public void DesiredPlanarVelocityUsesForwardAndSideways()
        {
            var go = new GameObject("frame");
            try
            {
                Vector3 result = HoverPhysicsMath.DesiredPlanarVelocity(go.transform, new Vector2(1f, 0f), 12f, 8f);
                Assert.That(result.x, Is.EqualTo(8f).Within(0.001f));
                Assert.That(result.y, Is.EqualTo(0f).Within(0.001f));
                Assert.That(result.z, Is.EqualTo(0f).Within(0.001f));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void PlanarMotorNeverExceedsForceCap()
        {
            var f = HoverPhysicsMath.PlanarMotorForce(Vector3.zero, Vector3.forward * 20f, 300f, .2f, 120f, false);
            Assert.That(f.magnitude, Is.LessThanOrEqualTo(120.001f));
        }
        [Test] public void PlanarMotorDoesNotOverwriteBoostSpeedByDefault()
        {
            var f = HoverPhysicsMath.PlanarMotorForce(Vector3.forward * 25f, Vector3.forward * 14f, 300f, .5f, 1000f, false);
            Assert.AreEqual(Vector3.zero, f);
        }
        [Test] public void BrakeMayCounteractExternalBoost()
        {
            var f = HoverPhysicsMath.PlanarMotorForce(Vector3.forward * 25f, Vector3.forward * 14f, 300f, .5f, 1000f, true);
            Assert.Less(f.z, 0f);
        }
        [Test] public void YawTorqueIsClamped()
        {
            Assert.That(Mathf.Abs(HoverPhysicsMath.YawMotorTorque(0f, 15f, 300f, .1f, 200f)), Is.EqualTo(200f));
        }
        [Test] public void YawTorqueSupportsReverse()
        {
            Assert.Less(HoverPhysicsMath.YawMotorTorque(1f, -1f, 10f, .5f, 100f), 0f);
        }
    }
}
