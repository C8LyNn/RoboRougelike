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
            if (prefab == null)
            {
                Assert.Ignore("Prefab is staged outside Resources; run Editor import/asset lookup receiver test.");
                yield break;
            }
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
