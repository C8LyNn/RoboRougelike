using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.Mechanics.COV001.Tests
{
    public sealed class DestructibleArenaCoverPlayModeTests
    {
        [UnityTest]
        public IEnumerator BreakDisablesPhysicsBlockerAndResetRestoresIt()
        {
            var obstacle = new GameObject("COV001 play obstacle");
            try
            {
                var collider = obstacle.AddComponent<BoxCollider>();
                var cover = obstacle.AddComponent<DestructibleArenaCover>();
                yield return null;
                Assert.IsTrue(collider.enabled);
                Assert.IsTrue(cover.ApplyDamage(50f));
                yield return null;
                Assert.IsFalse(collider.enabled);
                cover.ResetCover();
                yield return null;
                Assert.IsTrue(collider.enabled);
            }
            finally { Object.Destroy(obstacle); }
        }

        [UnityTest]
        public IEnumerator InvalidAndRepeatedHitsNeverDoubleBreak()
        {
            var obstacle = new GameObject("COV001 play duplicate hits");
            try
            {
                obstacle.AddComponent<BoxCollider>();
                var cover = obstacle.AddComponent<DestructibleArenaCover>();
                int broken = 0;
                cover.BrokenOnce += () => broken++;
                yield return null;
                Assert.IsFalse(cover.ApplyDamage(float.NaN));
                Assert.IsTrue(cover.ApplyDamage(500f));
                Assert.IsFalse(cover.ApplyDamage(500f));
                yield return null;
                Assert.AreEqual(1, broken);
            }
            finally { Object.Destroy(obstacle); }
        }
    }
}
