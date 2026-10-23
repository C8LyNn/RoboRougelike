using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Defenses.DirectionalShield.Tests.EditMode
{
    public class ShieldGeometryTests
    {
        private static bool Covered(Vector3 hit, Vector3 direction, float radius=9f, float angle=60f,
            bool flat=true, bool front=true, Vector3? orientation=null)
        {
            return ShieldMath.Covers(Vector3.zero, orientation ?? Vector3.forward,
                hit, direction, radius, angle, flat, front);
        }

        [Test] public void FromFrontBlocks() => Assert.IsTrue(Covered(Vector3.forward, Vector3.back));
        [Test] public void FromRearDoesNotBlock() => Assert.IsFalse(Covered(Vector3.forward, Vector3.forward));
        [Test] public void BehindMountDoesNotBlock() => Assert.IsFalse(Covered(Vector3.back, Vector3.back));
        [Test] public void CanIgnoreHitPointFrontLimit() => Assert.IsTrue(Covered(Vector3.back, Vector3.back, front:false));
        [Test] public void DistantImpactDoesNotBlock() => Assert.IsFalse(Covered(Vector3.forward * 12f, Vector3.back));
        [Test] public void EdgeAtRadiusAccepted() => Assert.IsTrue(Covered(Vector3.forward * 9f, Vector3.back));
        [Test] public void RightEdgeAtAngleAccepted() => Assert.IsTrue(Covered(Vector3.forward, new Vector3(-0.8660254f, 0f, -0.5f)));
        [Test] public void AngleBeyondArcRejected() => Assert.IsFalse(Covered(Vector3.forward, Vector3.right));
        [Test] public void ZeroTravelRejected() => Assert.IsFalse(Covered(Vector3.forward, Vector3.zero));
        [Test] public void ZeroFacingRejected() => Assert.IsFalse(Covered(Vector3.forward, Vector3.back, orientation:Vector3.zero));
        [Test] public void VerticalTravelInPlanarRejected() => Assert.IsFalse(Covered(Vector3.forward, Vector3.up));
        [Test] public void VerticalTravelIn3DAccepted() => Assert.IsTrue(Covered(Vector3.up, Vector3.down, flat:false, orientation:Vector3.up));
        [Test] public void PlanarArcIgnoresVerticalComponent() => Assert.IsTrue(Covered(Vector3.forward, new Vector3(0f, 10f, -1f)));
        [Test] public void NaNTravelRejected() => Assert.IsFalse(Covered(Vector3.forward, new Vector3(float.NaN,0f,-1f)));
        [Test] public void NaNPositionRejected() => Assert.IsFalse(Covered(new Vector3(float.NaN,0f,1f), Vector3.back));
        [Test] public void InfinityPositionRejected() => Assert.IsFalse(Covered(new Vector3(float.PositiveInfinity,0f,1f), Vector3.back));
        [Test] public void ZeroRadiusRejected() => Assert.IsFalse(Covered(Vector3.forward, Vector3.back, radius:0f));
        [Test] public void NegativeRadiusRejected() => Assert.IsFalse(Covered(Vector3.forward, Vector3.back, radius:-1f));
        [Test] public void OutOfRangeAngleRejected() => Assert.IsFalse(Covered(Vector3.forward, Vector3.back, angle:181f));
        [Test] public void ZeroAngleAllowsHeadOn() => Assert.IsTrue(Covered(Vector3.forward, Vector3.back, angle:0f));
        [Test] public void FullHalfAngleAllowsRearDirectionIfHitPointInFront() => Assert.IsTrue(Covered(Vector3.forward, Vector3.forward, angle:180f));
        [Test] public void HitAtMountPassesDirectionalCheck() => Assert.IsTrue(Covered(Vector3.zero, Vector3.back));
        [Test] public void RotatedModuleFacesRight() => Assert.IsTrue(Covered(Vector3.right, Vector3.left, orientation:Vector3.right));
        [Test] public void VerticalOffsetAllowedWithinSphere() => Assert.IsTrue(Covered(new Vector3(0f,5f,1f), Vector3.back));
    }
}
