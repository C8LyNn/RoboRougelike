using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.VFX001.Tests
{
    public sealed class VfxV5OrchestrationTests
    {
        [UnityTest]
        public IEnumerator RoutedImpact_DoesNotMutatePhysics()
        {
            var poolGo = new GameObject("Pool");
            var pool = poolGo.AddComponent<VfxPoolManager>();
            var rig = new GameObject("Camera Rig");
            var camera = new GameObject("Camera Visual");
            camera.transform.SetParent(rig.transform,false);
            var receiver = rig.AddComponent<VfxCameraImpulseReceiver>();
            receiver.Bind(camera.transform);
            var controllerGo = new GameObject("Effects only");
            var orchestrator = controllerGo.AddComponent<VfxFeedbackOrchestrator>();
            orchestrator.Bind(pool,receiver);
            var evt = new VfxEvent(VfxEventKind.Hit,Vector3.zero,Vector3.up,ImpactMaterialType.Metal,.9f);
            Assert.IsTrue(orchestrator.Emit(evt));
            Assert.AreEqual(1,receiver.ActiveCount);
            Assert.Greater(pool.ActiveCount,0);
            Assert.IsNull(controllerGo.GetComponent<Rigidbody>());
            Assert.IsNull(controllerGo.GetComponent<Collider>());
            pool.ClearAll();
            receiver.ResetImpulses();
            Object.Destroy(controllerGo);
            Object.Destroy(rig);
            Object.Destroy(poolGo);
            yield return null;
        }
    }
}
