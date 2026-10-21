using NUnit.Framework;
using UnityEngine;
using RoboRougelike.Mechanics.MOV005OmniDrive;

namespace RoboRougelike.Mechanics.MOV005OmniDrive.Tests
{
    public sealed class OmniDriveMathTests
    {
        [Test] public void ForwardAccelerationPointsForward()
        {
            Vector3 a = OmniDriveMath.LinearAcceleration(Vector3.zero, Quaternion.identity,
                new Vector2(0, 1), 12, 8, 0, 0.02f);
            Assert.AreEqual(0f, a.x, 0.0001f);
            Assert.AreEqual(8f, a.z, 0.0001f);
        }
        [Test] public void RightStrafeDoesNotYaw()
        {
            Vector3 a = OmniDriveMath.LinearAcceleration(Vector3.zero, Quaternion.identity,
                new Vector2(1, 0), 12, 8, 0, 0.02f);
            Assert.AreEqual(8f, a.x, 0.0001f);
            Assert.AreEqual(0f, a.z, 0.0001f);
        }
        [Test] public void DiagonalInputIsNormalized()
        {
            Vector2 input = OmniDriveMath.ClampInput(new Vector2(1, 1));
            Assert.AreEqual(1f, input.magnitude, 0.0001f);
        }
        [Test] public void LocalInputRotatesWithChassis()
        {
            Vector3 a = OmniDriveMath.LinearAcceleration(Vector3.zero, Quaternion.Euler(0, 90, 0),
                new Vector2(0, 1), 10, 6, 0, 0.02f);
            Assert.Greater(a.x, 5f);
            Assert.AreEqual(0f, a.z, 0.001f);
        }
        [Test] public void NoVerticalAccelerationOnSlopedBody()
        {
            Vector3 a = OmniDriveMath.LinearAcceleration(Vector3.up * 5f, Quaternion.Euler(15, 40, 0),
                new Vector2(1, 0.5f), 10, 6, 0, 0.02f);
            Assert.AreEqual(0f, a.y, 0.0001f);
        }
        [Test] public void ClampsSpeedCorrectionInsteadOfTeleportingVelocity()
        {
            Vector3 a = OmniDriveMath.LinearAcceleration(Vector3.forward * -500f, Quaternion.identity,
                Vector2.up, 9, 4, 0, 0.02f);
            Assert.LessOrEqual(a.magnitude, 4.001f);
        }
        [Test] public void CoastingLeavesVelocityAlone()
        {
            Assert.AreEqual(Vector3.zero, OmniDriveMath.LinearAcceleration(Vector3.forward * 10,
                Quaternion.identity, Vector2.zero, 12, 5, 0, .02f));
        }
        [Test] public void OptionalIdleBrakeActsAgainstVelocity()
        {
            Vector3 a = OmniDriveMath.LinearAcceleration(Vector3.forward * 10,
                Quaternion.identity, Vector2.zero, 12, 5, 3, .02f);
            Assert.AreEqual(-3f, a.z, 0.0001f);
        }
        [Test] public void ZeroDtReturnsNoAcceleration()
        {
            Assert.AreEqual(Vector3.zero, OmniDriveMath.LinearAcceleration(Vector3.zero,
                Quaternion.identity, Vector2.up, 10, 10, 0, 0));
        }
        [Test] public void NaNInputFailsSafe()
        {
            Assert.AreEqual(Vector2.zero, OmniDriveMath.ClampInput(new Vector2(float.NaN, 1)));
        }
        [Test] public void InfinityParametersFailSafe()
        {
            Assert.AreEqual(0f, OmniDriveMath.NonNegative(float.PositiveInfinity));
        }
        [Test] public void YawPositiveCommandAccelerates()
        {
            Assert.Greater(OmniDriveMath.YawAcceleration(0, 1, 90, 4, 0, .02f), 0);
        }
        [Test] public void YawAccelerationClamped()
        {
            Assert.AreEqual(3f, OmniDriveMath.YawAcceleration(-12, 1, 90, 3, 0, .02f), 0.0001f);
        }
        [Test] public void YawCoastsWithNoInput()
        {
            Assert.AreEqual(0f, OmniDriveMath.YawAcceleration(2, 0, 90, 4, 0, .02f));
        }
        [Test] public void YawBrakeOptional()
        {
            Assert.AreEqual(-2f, OmniDriveMath.YawAcceleration(3, 0, 90, 4, 2, .02f), .0001f);
        }
        [Test] public void InvalidYawTimeDoesNothing()
        {
            Assert.AreEqual(0f, OmniDriveMath.YawAcceleration(1, 1, 90, 4, 0, float.NaN));
        }
        [Test] public void InputClampKeepsNegativeDirection()
        {
            Assert.AreEqual(-1f, OmniDriveMath.ClampInput(new Vector2(-20, 0)).x, .0001f);
        }
    }
}
