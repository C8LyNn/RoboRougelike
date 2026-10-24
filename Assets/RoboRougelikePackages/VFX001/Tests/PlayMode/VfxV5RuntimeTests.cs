using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.VFX001.Tests
{
    public sealed class VfxV5RuntimeTests
    {
        [UnityTest]
        public IEnumerator HeatGlow_IsBoundedAndClears()
        {
            var root = new GameObject("HeatSurface");
            var heat = root.AddComponent<VfxHeatImpactController>();
            yield return null;
            if (VfxMaterialLibrary.HeatGlow == null)
            {
                Object.Destroy(root);
                Assert.Inconclusive("Generate VFX001 HeatGlow material using Tools > RoboRougelike > VFX001 > Create URP Materials.");
                yield break;
            }
            for (int i=0;i<14;i++)
                Assert.IsTrue(heat.PlayHeat(new Vector3(i*.1f,0f,0f), Vector3.up, 1f));
            Assert.LessOrEqual(heat.CreatedOverlays, heat.Capacity);
            Assert.LessOrEqual(heat.ActiveOverlays, heat.Capacity);
            Assert.IsNull(root.GetComponentInChildren<Rigidbody>());
            Assert.IsNull(root.GetComponentInChildren<Collider>());
            heat.Tick(2f);
            Assert.AreEqual(0, heat.ActiveOverlays);
            heat.ClearHeat();
            Object.Destroy(root);
            yield return null;
        }
        [UnityTest]
        public IEnumerator HeatGlow_RejectsInvalidSurfaceNormal()
        {
            var root = new GameObject("HeatSurface");
            var heat = root.AddComponent<VfxHeatImpactController>();
            yield return null;
            Assert.IsFalse(heat.PlayHeat(Vector3.zero,Vector3.zero,1f));
            Assert.AreEqual(0,heat.CreatedOverlays);
            Object.Destroy(root);
            yield return null;
        }
        [UnityTest]
        public IEnumerator MechanicalJolt_DisablingRestoresPose()
        {
            var root = new GameObject("Module");
            var visual = new GameObject("Decorative mesh");
            visual.transform.SetParent(root.transform, false);
            var component = root.AddComponent<VfxMechanicalJolt>();
            Assert.IsTrue(component.Bind(visual.transform));
            component.React(1f,Vector3.back);
            yield return null;
            component.enabled=false;
            Assert.AreEqual(Vector3.zero,visual.transform.localPosition);
            Assert.Less(Quaternion.Angle(Quaternion.identity, visual.transform.localRotation),.001f);
            Object.Destroy(root);
            yield return null;
        }
        [UnityTest]
        public IEnumerator CameraRigDoesNotMoveOnImpulse()
        {
            var rig=new GameObject("Gameplay camera rig");
            rig.transform.position=new Vector3(2f,3f,4f);
            var visual=new GameObject("Visual Camera");
            visual.transform.SetParent(rig.transform,false);
            var receiver=rig.AddComponent<VfxCameraImpulseReceiver>();
            receiver.Bind(visual.transform);
            receiver.Accept(new VfxEvent(VfxEventKind.CoreDestroyed,rig.transform.position,Vector3.up,ImpactMaterialType.Core,1f));
            yield return null;
            Assert.AreEqual(new Vector3(2f,3f,4f),rig.transform.position);
            Assert.IsNull(rig.GetComponent<Rigidbody>());
            Object.Destroy(rig);
            yield return null;
        }
    }
}
