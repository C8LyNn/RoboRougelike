using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.VFX001.Tests
{
    public class VfxModuleTests
    {
        [UnityTest]
        public IEnumerator Thruster_DoesNotScalePhysicsRoot()
        {
            var root = new GameObject("vehicleRoot");
            var child = new GameObject("flame");
            child.transform.SetParent(root.transform, false);
            var original = root.transform.localScale;
            var fx = root.AddComponent<ThrusterVfxController>();
            fx.Bind(child.transform);
            fx.SetThrust01(1f);
            yield return null;
            Assert.AreEqual(original, root.transform.localScale);
            Assert.Greater(fx.CurrentThrust01, 0f);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator Recoil_OnlyMovesVisualChild()
        {
            var root = new GameObject("gunRoot");
            var child = new GameObject("visualChild");
            child.transform.SetParent(root.transform, false);
            var original = root.transform.localPosition;
            var a = root.AddComponent<WeaponRecoilAnimator>();
            a.Bind(child.transform);
            a.PlayRecoil();
            yield return new WaitForSeconds(.05f);
            Assert.AreEqual(original, root.transform.localPosition);
            Assert.IsTrue(a.IsAnimating);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator LegacyTransient_DestroysAfterSetLifetime()
        {
            var go = new GameObject("transient");
            var transient = go.AddComponent<TransientAutoDestroy>();
            transient.SetLifetime(.05f);
            yield return new WaitForSeconds(.2f);
            Assert.IsTrue(go == null);
        }
    }
}
