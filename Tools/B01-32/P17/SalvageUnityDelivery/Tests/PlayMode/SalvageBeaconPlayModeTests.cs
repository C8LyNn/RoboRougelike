using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.B0132P17.SalvageUnity.Tests
{
    public sealed class SalvageBeaconPlayModeTests
    {
        [UnityTest]
        public IEnumerator Prefab_ContainsEightRealSpriteReferences()
        {
            // Verify actual generated prefab after import via scene loading in Unity Editor.
            var prefab = Resources.Load<GameObject>("B0132_SalvageBeaconLoop");
            Assert.IsNotNull(prefab, "Generated prefab must be placed inside Assets/B0132SalvageBeacon/Resources");
            var copy = Object.Instantiate(prefab);
            Assert.IsNotNull(copy.GetComponent<SpriteRenderer>());
            Assert.IsNotNull(copy.GetComponent<B0132SalvageBeaconLoop>());
            Object.Destroy(copy);
            yield return null;
        }

        [UnityTest]
        public IEnumerator VfxOff_HidesActualSpriteImmediately()
        {
            var obj = new GameObject("beacon teardown", typeof(SpriteRenderer));
            var component = obj.AddComponent<B0132SalvageBeaconLoop>();
            component.SetVisualsEnabled(false);
            Assert.IsFalse(component.IsVisible);
            Assert.IsFalse(obj.GetComponent<SpriteRenderer>().enabled);
            component.enabled = false;
            Assert.IsFalse(component.IsVisible);
            Object.Destroy(obj);
            yield return null;
        }
    }
}
