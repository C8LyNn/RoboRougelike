using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.VFX001.Tests
{
    public class VfxBatcherIntegrationTests
    {
        [UnityTest]
        public IEnumerator MultipleConnectedFragments_YieldOneTransient()
        {
            var go = new GameObject("visualRoot");
            var pool = go.AddComponent<VfxPoolManager>();
            var batcher = go.AddComponent<VfxEventBatcher>();
            for (int i = 0; i < 5; i++)
                batcher.Report(new VfxEvent(VfxEventKind.Detached,
                    new Vector3(.05f * i, 0f, 0f), Vector3.up,
                    ImpactMaterialType.Metal, .4f, 1, 123));
            Assert.AreEqual(1, batcher.PendingCount);
            batcher.Flush();
            Assert.AreEqual(1, pool.ActiveCount);
            Object.Destroy(go);
            yield return null;
        }
    }
}
