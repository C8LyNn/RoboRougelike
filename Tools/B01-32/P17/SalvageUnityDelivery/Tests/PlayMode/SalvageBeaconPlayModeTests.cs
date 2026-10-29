using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.B0132P17.SalvageUnity.Tests
{
    public sealed class SalvageBeaconPlayModeTests
    {
        private static GameObject InstantiateRealAsset()
        {
            var prefab = Resources.Load<GameObject>("B0132_SalvageBeaconLoop");
            Assert.IsNotNull(prefab, "Expected original generated prefab inside Resources");
            return Object.Instantiate(prefab);
        }

        [UnityTest]
        public IEnumerator Prefab_ContainsEightRealSpriteReferences()
        {
            var obj = InstantiateRealAsset();
            var visual = obj.GetComponent<SpriteRenderer>();
            var loop = obj.GetComponent<B0132SalvageBeaconLoop>();
            Assert.IsNotNull(visual);
            Assert.IsNotNull(loop);
            var field = typeof(B0132SalvageBeaconLoop).GetField(
                "frames", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Original frame-array serialization required");
            var frames = field.GetValue(loop) as Sprite[];
            Assert.IsNotNull(frames);
            Assert.AreEqual(8, frames.Length);
            for (int i = 0; i < frames.Length; i++)
                Assert.IsNotNull(frames[i], "Sprite " + i + " must resolve after native import");
            Assert.IsTrue(loop.IsVisible, "Real frame 0 should render when enabled");
            Object.Destroy(obj);
            yield return null;
        }

        [UnityTest]
        public IEnumerator VfxOff_ClearsActualPrefabAndDoesNotReactivateStaleFrame()
        {
            var obj = InstantiateRealAsset();
            var component = obj.GetComponent<B0132SalvageBeaconLoop>();
            var renderer = obj.GetComponent<SpriteRenderer>();
            Assert.IsTrue(component.IsVisible, "Real original imported prefab starts visible");
            component.SetVisualsEnabled(false);
            Assert.IsFalse(component.IsVisible);
            Assert.IsNull(renderer.sprite);
            Assert.IsFalse(renderer.enabled);
            obj.SetActive(false);
            obj.SetActive(true);
            Assert.IsFalse(component.IsVisible, "OFF state persists across enable/disable");
            Assert.IsNull(renderer.sprite);
            component.SetVisualsEnabled(true);
            Assert.IsTrue(component.IsVisible, "Re-enable restores frame zero");
            Object.Destroy(obj);
            yield return null;
        }
    }
}
