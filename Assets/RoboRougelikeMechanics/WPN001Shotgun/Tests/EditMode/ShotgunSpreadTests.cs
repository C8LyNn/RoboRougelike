using System;
using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.WPN001Shotgun.Tests.EditMode
{
    public sealed class ShotgunSpreadTests
    {
        private static Vector3[] Make(Vector3 axis, int count, float angle, uint seed)
        {
            var result = new Vector3[count];
            ShotgunSpread.BuildDirections(axis, count, angle, seed, result);
            return result;
        }
        [Test] public void ExactNumberOfDirections() =>
            Assert.AreEqual(8, ShotgunSpread.BuildDirections(Vector3.forward, 8, 14f, 1u, new Vector3[8]));
        [Test] public void EveryDirectionHasUnitLength()
        {
            foreach (Vector3 d in Make(Vector3.forward, 32, 60f, 17u))
                Assert.That(d.magnitude, Is.EqualTo(1f).Within(0.00001f));
        }
        [Test] public void DirectionsRemainInsideHalfAngle()
        {
            foreach (Vector3 d in Make(Vector3.forward, 64, 30f, 17u))
                Assert.LessOrEqual(Vector3.Angle(Vector3.forward, d), 15.001f);
        }
        [Test] public void DirectionsFollowRotatedMuzzle()
        {
            Vector3 axis = new Vector3(1, 1, 0).normalized;
            foreach (Vector3 d in Make(axis, 16, 18f, 1u))
                Assert.LessOrEqual(Vector3.Angle(axis, d), 9.001f);
        }
        [Test] public void SameSeedProducesIdenticalVectors()
        {
            Vector3[] a = Make(Vector3.up, 12, 30f, 512u);
            Vector3[] b = Make(Vector3.up, 12, 30f, 512u);
            for (int i = 0; i < a.Length; i++) Assert.AreEqual(a[i], b[i]);
        }
        [Test] public void DifferentSeedChangesPattern()
        {
            Vector3[] a = Make(Vector3.forward, 12, 30f, 512u);
            Vector3[] b = Make(Vector3.forward, 12, 30f, 513u);
            Assert.Greater(Vector3.Distance(a[0], b[0]), 0.0001f);
        }
        [Test] public void ConsecutiveVolleysHaveDistinctSeeds() =>
            Assert.AreNotEqual(ShotgunSpread.VolleySeed(14u, 0u), ShotgunSpread.VolleySeed(14u, 1u));
        [Test] public void NoSpreadPointsExactlyForward()
        {
            foreach (Vector3 d in Make(Vector3.forward, 16, 0f, 18u))
                Assert.AreEqual(Vector3.forward, d);
        }
        [Test] public void OnePelletPointsExactlyForward()
        {
            Assert.AreEqual(Vector3.right, Make(Vector3.right, 1, 60f, 18u)[0]);
        }
        [Test] public void ZeroSeedWorks()
        {
            foreach (Vector3 d in Make(Vector3.forward, 8, 20f, 0u))
                Assert.IsFalse(float.IsNaN(d.x));
        }
        [Test] public void RejectNullOutput() =>
            Assert.Throws<ArgumentNullException>(() => ShotgunSpread.BuildDirections(Vector3.forward, 1, 1f, 0u, null));
        [Test] public void RejectUndersizedBuffer() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => ShotgunSpread.BuildDirections(Vector3.forward, 8, 10f, 0u, new Vector3[7]));
        [Test] public void RejectMoreThan64Pellets() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => ShotgunSpread.BuildDirections(Vector3.forward, 65, 10f, 0u, new Vector3[65]));
        [Test] public void RejectZeroForward() =>
            Assert.Throws<ArgumentException>(() => Make(Vector3.zero, 3, 10f, 0u));
        [Test] public void RejectInvalidCone() =>
            Assert.Throws<ArgumentException>(() => Make(Vector3.forward, 3, float.NaN, 0u));
        [Test] public void DoesNotModifyGlobalUnityRandomState()
        {
            UnityEngine.Random.State before = UnityEngine.Random.state;
            Make(Vector3.forward, 15, 23f, 398u);
            Assert.AreEqual(before, UnityEngine.Random.state);
        }
    }
}
