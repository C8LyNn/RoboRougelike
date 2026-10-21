using NUnit.Framework;
using UnityEngine;
using RoboRougelike.MapSystemR4;

namespace RoboRougelike.MapSystemR4.Tests
{
    public class MapR4SocketTests
    {
        [Test]
        public void TryAlign_PutsSocketOnTargetAndFacesOppositeDirection()
        {
            var root = new GameObject("module");
            var sourceGo = new GameObject("source");
            sourceGo.transform.SetParent(root.transform, false);
            sourceGo.transform.localPosition = new Vector3(0f, 0f, 5f);
            var source = sourceGo.AddComponent<MapR4ModuleSocket>();

            var targetGo = new GameObject("target");
            targetGo.transform.position = new Vector3(10f, 0f, 10f);
            var target = targetGo.AddComponent<MapR4ModuleSocket>();
            Assert.IsTrue(MapR4SocketPlacement.TryAlign(root.transform, source, target));
            Assert.Less(Vector3.Distance(source.transform.position, target.transform.position), 0.001f);
            Assert.Less(Vector3.Distance(source.transform.forward, -target.transform.forward), 0.001f);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void TryAlign_RejectsWidthMismatch()
        {
            var root = new GameObject("module");
            var source = new GameObject("source");
            source.transform.SetParent(root.transform, false);
            var a = source.AddComponent<MapR4ModuleSocket>();
            var targetGo = new GameObject("target");
            var b = targetGo.AddComponent<MapR4ModuleSocket>();
            b.clearWidth = 12f;
            Assert.IsFalse(MapR4SocketPlacement.TryAlign(root.transform, a, b));
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(targetGo);
        }
    }
}
