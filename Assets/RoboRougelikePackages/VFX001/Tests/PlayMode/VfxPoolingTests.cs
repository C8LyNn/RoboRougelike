using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.VFX001.Tests
{
    public class VfxPoolingTests
    {
        [UnityTest]
        public IEnumerator Pool_ReusesObjectAfterCompletion()
        {
            var go = new GameObject("pool");
            var pool = go.AddComponent<VfxPoolManager>();
            var first = pool.Emit(new VfxEvent(VfxEventKind.Hit, Vector3.zero, Vector3.up));
            Assert.IsNotNull(first);
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.AreEqual(0, pool.ActiveCount);
            int created = pool.CreatedCount;
            var again = pool.Emit(new VfxEvent(VfxEventKind.Hit, Vector3.zero, Vector3.up));
            Assert.IsNotNull(again);
            Assert.AreEqual(created, pool.CreatedCount);
            Object.Destroy(go);
            yield return null;
        }
        [UnityTest]
        public IEnumerator Pool_ClearAll_ReleasesTransientObjects()
        {
            var go = new GameObject("pool");
            var pool = go.AddComponent<VfxPoolManager>();
            pool.Emit(new VfxEvent(VfxEventKind.Explosion, Vector3.zero, Vector3.up));
            Assert.AreEqual(1, pool.ActiveCount);
            pool.ClearAll();
            Assert.AreEqual(0, pool.ActiveCount);
            Assert.GreaterOrEqual(pool.PooledCount, 1);
            Object.Destroy(go);
            yield return null;
        }
    }
}
