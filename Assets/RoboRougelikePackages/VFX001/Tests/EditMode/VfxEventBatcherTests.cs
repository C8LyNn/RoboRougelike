using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.VFX001.Tests
{
    public class VfxEventBatcherTests
    {
        [Test]
        public void SameCauseNearbyDetachment_IsMerged()
        {
            var go = new GameObject("batcher");
            var b = go.AddComponent<VfxEventBatcher>();
            Assert.IsTrue(b.Report(new VfxEvent(VfxEventKind.Detached, Vector3.zero, Vector3.up,
                ImpactMaterialType.Metal, .3f, 10, 44)));
            Assert.IsTrue(b.Report(new VfxEvent(VfxEventKind.Detached, Vector3.right, Vector3.up,
                ImpactMaterialType.Metal, .3f, 10, 44)));
            Assert.AreEqual(1, b.PendingCount);
            Object.DestroyImmediate(go);
        }
        [Test]
        public void DifferentSequence_DoesNotMerge()
        {
            var go = new GameObject("batcher");
            var b = go.AddComponent<VfxEventBatcher>();
            b.Report(new VfxEvent(VfxEventKind.Detached, Vector3.zero, Vector3.up,
                ImpactMaterialType.Metal, .3f, 10, 44));
            b.Report(new VfxEvent(VfxEventKind.Detached, Vector3.zero, Vector3.up,
                ImpactMaterialType.Metal, .3f, 10, 45));
            Assert.AreEqual(2, b.PendingCount);
            Object.DestroyImmediate(go);
        }
        [Test]
        public void UnknownCause_DoesNotMerge()
        {
            var go = new GameObject("batcher");
            var b = go.AddComponent<VfxEventBatcher>();
            b.Report(new VfxEvent(VfxEventKind.Detached, Vector3.zero, Vector3.up));
            b.Report(new VfxEvent(VfxEventKind.Detached, Vector3.zero, Vector3.up));
            Assert.AreEqual(2, b.PendingCount);
            Object.DestroyImmediate(go);
        }
    }
}
