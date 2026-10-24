using NUnit.Framework;
using UnityEngine;
using RoboRougelike.Mechanics.StructuralScan;

namespace RoboRougelike.Mechanics.StructuralScan.Tests.EditMode
{
    public class StructuralScanMathTests
    {
        [Test] public void ForwardPointIsVisible() => Assert.IsTrue(StructuralScanMath.InCone(Vector3.zero, Vector3.forward, new Vector3(0, 0, 8), 10, 75));
        [Test] public void BackPointIsNotVisible() => Assert.IsFalse(StructuralScanMath.InCone(Vector3.zero, Vector3.forward, new Vector3(0, 0, -8), 10, 75));
        [Test] public void PointBeyondRangeIsNotVisible() => Assert.IsFalse(StructuralScanMath.InCone(Vector3.zero, Vector3.forward, new Vector3(0, 0, 11), 10, 75));
        [Test] public void PointExactlyAtRangeIsVisible() => Assert.IsTrue(StructuralScanMath.InCone(Vector3.zero, Vector3.forward, new Vector3(0, 0, 10), 10, 75));
        [Test] public void OriginIsNotTarget() => Assert.IsFalse(StructuralScanMath.InCone(Vector3.zero, Vector3.forward, Vector3.zero, 10, 75));
        [Test] public void ZeroForwardVectorCannotScan() => Assert.IsFalse(StructuralScanMath.InCone(Vector3.zero, Vector3.zero, Vector3.forward, 10, 75));
        [Test] public void ForwardMagnitudeDoesNotMatter() => Assert.IsTrue(StructuralScanMath.InCone(Vector3.zero, Vector3.forward * 500, new Vector3(0, 0, 2), 10, 75));
        [Test] public void OffAxisWithinConeIsVisible() => Assert.IsTrue(StructuralScanMath.InCone(Vector3.zero, Vector3.forward, new Vector3(1, 0, 5), 10, 75));
        [Test] public void OffAxisOutsideConeIsNotVisible() => Assert.IsFalse(StructuralScanMath.InCone(Vector3.zero, Vector3.forward, new Vector3(5, 0, 1), 10, 75));
        [Test] public void ConeUsesDirectionNotWorldZ() => Assert.IsTrue(StructuralScanMath.InCone(Vector3.zero, Vector3.right, new Vector3(4, 0, 0), 10, 75));
        [Test] public void ConeUsesOriginOffset() => Assert.IsTrue(StructuralScanMath.InCone(new Vector3(3, 2, 7), Vector3.forward, new Vector3(3, 2, 12), 6, 75));
        [Test] public void NarrowConeRejectsThirtyDegrees() => Assert.IsFalse(StructuralScanMath.InCone(Vector3.zero, Vector3.forward, new Vector3(5, 0, 8.66f), 12, 20));
        [Test] public void WideConeAcceptsThirtyDegrees() => Assert.IsTrue(StructuralScanMath.InCone(Vector3.zero, Vector3.forward, new Vector3(5, 0, 8.66f), 12, 90));
        [Test] public void VerticalAngleCounts() => Assert.IsFalse(StructuralScanMath.InCone(Vector3.zero, Vector3.forward, Vector3.up, 12, 75));

        [Test] public void CoreScoresAboveArmor() => Assert.Greater(StructuralScanMath.Priority(StructuralPartType.Core, 1, 0, true), StructuralScanMath.Priority(StructuralPartType.Armor, 1, 0, true));
        [Test] public void SupportScoresAboveUtility() => Assert.Greater(StructuralScanMath.Priority(StructuralPartType.StructuralSupport, 1, 0, true), StructuralScanMath.Priority(StructuralPartType.Utility, 1, 0, true));
        [Test] public void WeaponScoresAboveArmor() => Assert.Greater(StructuralScanMath.Priority(StructuralPartType.Weapon, 1, 0, true), StructuralScanMath.Priority(StructuralPartType.Armor, 1, 0, true));
        [Test] public void DamagedPartScoresHigher() => Assert.Greater(StructuralScanMath.Priority(StructuralPartType.Wheel, 0.2f, 0, true), StructuralScanMath.Priority(StructuralPartType.Wheel, 1, 0, true));
        [Test] public void ImportantPartScoresHigher() => Assert.Greater(StructuralScanMath.Priority(StructuralPartType.Armor, 1, 1, true), StructuralScanMath.Priority(StructuralPartType.Armor, 1, 0, true));
        [Test] public void NonFunctionalPartScoresLower() => Assert.Less(StructuralScanMath.Priority(StructuralPartType.Weapon, 1, 0, false), StructuralScanMath.Priority(StructuralPartType.Weapon, 1, 0, true));
        [Test] public void PriorityClampsImportance() => Assert.AreEqual(StructuralScanMath.Priority(StructuralPartType.Armor, 1, 1, true), StructuralScanMath.Priority(StructuralPartType.Armor, 1, 99, true));
        [Test] public void PriorityClampsHealth() => Assert.AreEqual(StructuralScanMath.Priority(StructuralPartType.Wheel, 0, 0, true), StructuralScanMath.Priority(StructuralPartType.Wheel, -5, 0, true));
        [Test] public void NanHealthIsSafe() => Assert.IsFalse(float.IsNaN(StructuralScanMath.Priority(StructuralPartType.Weapon, float.NaN, 0, true)));
        [Test] public void NanImportanceIsSafe() => Assert.IsFalse(float.IsNaN(StructuralScanMath.Priority(StructuralPartType.Weapon, 1, float.NaN, true)));
        [Test] public void InfiniteHealthIsSafe() => Assert.IsFalse(float.IsInfinity(StructuralScanMath.Priority(StructuralPartType.Weapon, float.PositiveInfinity, 0, true)));
        [Test] public void PriorityWithinBounds() => Assert.That(StructuralScanMath.Priority(StructuralPartType.Core, 0, 1, true), Is.InRange(0f, 150f));
        [TestCase(StructuralPartType.Wheel)]
        [TestCase(StructuralPartType.Track)]
        [TestCase(StructuralPartType.Propulsion)]
        public void MobilityDevicesHavePositivePriority(StructuralPartType type) => Assert.Greater(StructuralScanMath.Priority(type, 1, 0, true), 0);
    }
}
