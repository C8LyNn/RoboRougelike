using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.B0132P16.Tests
{
    public sealed class B0132EffectPlayModeTests
    {
        [UnityTest]
        public IEnumerator ThreeModes_RenderWithoutPhysicalCollisionAndClear()
        {
            foreach (var kind in new[] {
                B0132Effect.EffectKind.ProjectileHit,
                B0132Effect.EffectKind.PartDetached,
                B0132Effect.EffectKind.CriticalWarning })
            {
                var root = new GameObject("B0132 test " + kind);
                var effect = root.AddComponent<B0132Effect>();
                effect.Play(kind, Vector3.zero, Vector3.forward);
                Assert.IsTrue(effect.IsPlaying);
                Assert.Greater(effect.ActiveVisualCount, 0);
                Assert.AreEqual(0, root.GetComponentsInChildren<Rigidbody>(true).Length);
                foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                    Assert.IsFalse(collider.enabled, "Cosmetic collider must not affect vehicle.");
                effect.StopVisuals();
                Assert.IsFalse(effect.IsPlaying);
                Assert.AreEqual(0, effect.ActiveVisualCount);
                Object.Destroy(root);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Disable_ClearsActiveWarningImmediately()
        {
            var root = new GameObject("B0132 warning teardown");
            var effect = root.AddComponent<B0132Effect>();
            effect.Play(B0132Effect.EffectKind.CriticalWarning, Vector3.zero, Vector3.up);
            Assert.Greater(effect.ActiveVisualCount, 0);
            root.SetActive(false);
            Assert.IsFalse(effect.IsPlaying);
            Assert.AreEqual(0, effect.ActiveVisualCount);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                Assert.IsFalse(renderer.gameObject.activeInHierarchy);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Hit_ExpiresAutonomously()
        {
            var root = new GameObject("B0132 lifetime");
            var effect = root.AddComponent<B0132Effect>();
            effect.Play(B0132Effect.EffectKind.ProjectileHit, Vector3.zero, Vector3.up);
            Assert.IsTrue(effect.IsPlaying);
            yield return new WaitForSeconds(.8f);
            Assert.IsFalse(effect.IsPlaying);
            Assert.AreEqual(0, effect.ActiveVisualCount);
            Object.Destroy(root);
            yield return null;
        }
    }
}
