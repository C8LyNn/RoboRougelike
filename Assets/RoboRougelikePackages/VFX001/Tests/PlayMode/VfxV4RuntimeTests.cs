using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.VFX001.Tests
{
    public sealed class VfxV4RuntimeTests
    {
        [UnityTest]
        public IEnumerator CosmeticBurning_DoesNotAddGameplayPhysics()
        {
            var go=new GameObject("Burning Visual");
            var fx=go.AddComponent<BurningModuleFxController>();
            yield return null;
            fx.SetVisualState(.9f,true,false);
            Assert.IsTrue(fx.HasEmitters);
            Assert.Greater(fx.FlameRate,0f);
            Assert.IsNull(go.GetComponent<Rigidbody>());
            fx.SetVisualState(0f,false,false);
            Assert.AreEqual(0f,fx.FlameRate);
            Object.Destroy(go);
            yield return null;
        }
        [UnityTest]
        public IEnumerator DetachedTrailRequiresDetachedAndSpeed()
        {
            var go=new GameObject("Fallen Part");
            var fx=go.AddComponent<DetachedModuleVfxController>();
            fx.SetDetachedMotion(false, 15f);
            Assert.AreEqual(0f,fx.EmissionRate);
            fx.SetDetachedMotion(true, 10f);
            Assert.IsTrue(fx.HasEmitter);
            Assert.Greater(fx.EmissionRate,0f);
            fx.SetDetachedMotion(true, 0f);
            Assert.AreEqual(0f,fx.EmissionRate);
            Object.Destroy(go);
            yield return null;
        }
        [UnityTest]
        public IEnumerator CoreDirectorFinishesAndCanCancel()
        {
            var root=new GameObject("Pool Root");
            var pool=root.AddComponent<VfxPoolManager>();
            var director=root.AddComponent<VfxCoreDestructionDirector>();
            director.Bind(pool);
            Assert.IsTrue(director.Begin(Vector3.zero,Vector3.up,1f, 123, 17));
            Assert.IsTrue(director.IsRunning);
            director.Advance(.55f);
            Assert.IsFalse(director.IsRunning);
            Assert.AreEqual(VfxCoreDestructionTimeline.BeatCount,director.CompletedBeats);
            director.Cancel();
            Assert.IsFalse(director.IsRunning);
            pool.ClearAll();
            Object.Destroy(root);
            yield return null;
        }
        [UnityTest]
        public IEnumerator ArmorScarsReuseBoundedOverlayWithoutCollider()
        {
            var go=new GameObject("Armor Surface");
            var scar=go.AddComponent<ArmorScarController>();
            yield return null;
            bool shaderReady = VfxMaterialLibrary.ArmorScar != null;
            if (!shaderReady)
            {
                Assert.Inconclusive("Armor scar material unavailable; run Tools/RoboRougelike/VFX001/Create URP Materials.");
                Object.Destroy(go);
                yield break;
            }
            for(int i=0;i<12;i++)
                Assert.IsTrue(scar.AddScar(new Vector3(0f,.1f,0f),Vector3.forward,ImpactMaterialType.Metal,.6f,i));
            Assert.LessOrEqual(scar.MarkCount,scar.Capacity);
            Assert.IsNull(go.GetComponentInChildren<Collider>());
            scar.ClearScars();
            Assert.AreEqual(0,scar.MarkCount);
            Object.Destroy(go);
            yield return null;
        }
        [UnityTest]
        public IEnumerator DebrisMeshIsGeometryNotRigidbody()
        {
            var metal=VfxDebrisMeshLibrary.ForMaterial(ImpactMaterialType.Metal);
            var rubber=VfxDebrisMeshLibrary.ForMaterial(ImpactMaterialType.Rubber);
            Assert.Greater(metal.vertexCount,0);
            Assert.Greater(rubber.vertexCount,0);
            Assert.AreNotSame(metal,rubber);
            yield return null;
        }
    }
}
