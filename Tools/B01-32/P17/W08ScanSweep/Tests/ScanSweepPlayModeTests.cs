using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.B0132P17.ScanSweep.Tests
{
    public sealed class ScanSweepPlayModeTests
    {
        private GameObject Spawn()
        {
            var prefab = Resources.Load<GameObject>("B0132_ScanSweep");
            Assert.IsNotNull(prefab, "Original packaged scan prefab must resolve");
            var obj = Object.Instantiate(prefab);
            Assert.IsNotNull(obj.GetComponent<B0132ScanSweepVisual>());
            return obj;
        }

        [UnityTest]
        public IEnumerator RealPrefabHasTransparentMeshAndNoPhysicsComponents()
        {
            var obj = Spawn();
            yield return null;
            var mesh = obj.GetComponent<MeshFilter>();
            var renderer = obj.GetComponent<MeshRenderer>();
            Assert.IsNotNull(mesh);
            Assert.IsNotNull(renderer);
            Assert.IsNotNull(mesh.sharedMesh);
            Assert.AreEqual(ScanSweepCore.VertexCount, mesh.sharedMesh.vertexCount);
            Assert.AreEqual(ScanSweepCore.IndexCount, mesh.sharedMesh.triangles.Length);
            Assert.IsNotNull(renderer.sharedMaterial);
            Assert.AreEqual("B0132/ScanSweepTransparentURP", renderer.sharedMaterial.shader.name);
            Assert.AreEqual(0, obj.GetComponentsInChildren<Collider>().Length);
            Assert.AreEqual(0, obj.GetComponentsInChildren<Rigidbody>().Length);
            Object.Destroy(obj);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ScanOffHidesMeshAndDoesNotReappearWithoutNewTrigger()
        {
            var obj = Spawn();
            var fx = obj.GetComponent<B0132ScanSweepVisual>();
            Assert.IsTrue(fx.StartVisualScan(0f));
            yield return null;
            Assert.AreEqual(ScanState.Sweeping, fx.State);
            fx.SetVisualsEnabled(false);
            Assert.AreEqual(ScanState.Off, fx.State);
            Assert.IsFalse(obj.GetComponent<MeshRenderer>().enabled);
            Assert.IsFalse(fx.StartVisualScan(0f));
            fx.SetVisualsEnabled(true);
            Assert.IsFalse(obj.GetComponent<MeshRenderer>().enabled);
            Assert.IsTrue(fx.StartVisualScan(1f));
            Assert.IsTrue(obj.GetComponent<MeshRenderer>().enabled);
            fx.CancelVisualScan();
            Assert.IsFalse(obj.GetComponent<MeshRenderer>().enabled);
            Object.Destroy(obj);
            yield return null;
        }
    }
}
