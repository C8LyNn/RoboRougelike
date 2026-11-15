using System;
using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.WPN001Shotgun.Tests.EditMode
{
    public sealed class ShotgunSpreadTests
    {
        [TestCase(1,0f,0u)][TestCase(8,14f,17u)][TestCase(64,60f,512u)]
        public void IndexedPatternMatchesExistingDirectionsRegardlessOfAccessOrder(int count,float cone,uint seed)
        {
            Vector3 axis=new Vector3(1,1,2).normalized;
            var expected=Make(axis,count,cone,seed);
            var pattern=new ShotgunSpread.IndexedPattern(axis,count,cone,seed);
            for(int i=count-1;i>=0;i--)
                Assert.That(Vector3.Distance(pattern.DirectionAt(i),expected[i]),Is.LessThan(.000001f));
        }
        [Test] public void IndexedPatternCanAddressVeryLargeVolleyWithoutAllocatingAnArray()
        {
            const int count=int.MaxValue;var pattern=new ShotgunSpread.IndexedPattern(Vector3.forward,count,60f,42u);
            var first=pattern.DirectionAt(0);var middle=pattern.DirectionAt(count/2);var last=pattern.DirectionAt(count-1);
            foreach(var direction in new[]{first,middle,last})
            {
                Assert.That(direction.magnitude,Is.EqualTo(1f).Within(.00001f));
                Assert.LessOrEqual(Vector3.Angle(Vector3.forward,direction),30.001f);
            }
            Assert.Greater(Vector3.Distance(first,middle),.01f);Assert.Greater(Vector3.Distance(middle,last),.01f);
            Assert.Throws<ArgumentOutOfRangeException>(()=>pattern.DirectionAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>pattern.DirectionAt(count));
        }
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
        [Test] public void LargeVolleyFillsOnlyRequestedDirectionsDeterministically()
        {
            const int count=129;var axis=new Vector3(1,2,3).normalized;
            var output=new Vector3[count+1];output[count]=Vector3.one*7;
            Assert.AreEqual(count,ShotgunSpread.BuildDirections(axis,count,40f,512u,output));
            var again=Make(axis,count,40f,512u);var different=Make(axis,count,40f,513u);
            for(int i=0;i<count;i++)
            {
                Assert.That(output[i].magnitude,Is.EqualTo(1f).Within(.00001f));
                Assert.LessOrEqual(Vector3.Angle(axis,output[i]),20.001f);
                Assert.AreEqual(output[i],again[i]);
            }
            Assert.Greater(Vector3.Distance(output[count-1],different[count-1]),.0001f);
            Assert.AreEqual(Vector3.one*7,output[count]);
        }
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
