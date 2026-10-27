using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.VFX001.Tests
{
    /// <summary>
    /// B01-32 W66 QA-only original-Unity acceptance for the existing VFX001 pool.
    /// Never changes damage, gameplay or the canonical VFX implementation.
    /// Must be run on an authorized original Unity PlayMode host; source review is NOT execution.
    /// </summary>
    public sealed class B0132VfxPoolDisableLifecycleTests
    {
        [UnityTest]
        public IEnumerator HostDeactivatedDuringPendingCoreBurst_ClearsParticlesAndReusesActor()
        {
            var root = new GameObject("B01-32 isolated VFX pool");
            try
            {
                var pool = root.AddComponent<VfxPoolManager>();
                var original = pool.Emit(new VfxEvent(
                    VfxEventKind.CoreDestroyed, Vector3.zero, Vector3.up,
                    ImpactMaterialType.Core, 1f));

                Assert.IsNotNull(original, "Fixture must admit the initial core burst.");
                Assert.AreEqual(1, pool.ActiveCount);
                var systems = original.GetComponentsInChildren<ParticleSystem>(true);
                Assert.AreEqual(4, systems.Length, "Check the original four-layer actor.");

                // Precondition: deferred smoke and fragments are still pending.
                // Host deactivation must reset them before their delayed emission.
                root.SetActive(false);
                Assert.AreEqual(0, pool.ActiveCount);
                Assert.IsFalse(original.IsPlaying);
                foreach (var system in systems)
                    Assert.AreEqual(0, system.particleCount, "No stale particles after host deactivation.");

                root.SetActive(true);
                Assert.AreEqual(0, pool.ActiveCount);
                var reused = pool.Emit(new VfxEvent(
                    VfxEventKind.Hit, Vector3.one, Vector3.up,
                    ImpactMaterialType.Metal, .25f));
                Assert.AreSame(original, reused, "Disabled actor must be recycled, not duplicated.");
                Assert.AreEqual(1, pool.ActiveCount);

                // New low-intensity Hit has NO smoke/fragment preset.
                // Previous core burst must never leak its delayed layers on reuse.
                yield return new WaitForSeconds(.2f);
                var smoke = reused.transform.Find("smoke")?.GetComponent<ParticleSystem>();
                var fragments = reused.transform.Find("fragments")?.GetComponent<ParticleSystem>();
                Assert.IsNotNull(smoke);
                Assert.IsNotNull(fragments);
                Assert.AreEqual(0, smoke.particleCount, "Old deferred smoke must not reappear.");
                Assert.AreEqual(0, fragments.particleCount, "Old deferred fragments must not reappear.");
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        [Explicit("KNOWN SOURCE-NEGATIVE until VFX pool disabled-state policy/owner fix; do not run as green release gate.")]
        public IEnumerator ComponentDisabled_EmitMustFailClosed_WithoutRespawningVisual()
        {
            var root = new GameObject("B01-32 isolated VFX negative");
            try
            {
                var pool = root.AddComponent<VfxPoolManager>();
                var first = pool.Emit(new VfxEvent(
                    VfxEventKind.Hit, Vector3.zero, Vector3.up));
                Assert.IsNotNull(first);
                pool.enabled = false; // Pool.OnDisable() invokes ClearAll().
                Assert.AreEqual(0, pool.ActiveCount);

                // Intentional RED criterion: current original Emit() checks budgets,
                // but not isActiveAndEnabled. Disabled pool may respawn a visible burst.
                var unexpected = pool.Emit(new VfxEvent(
                    VfxEventKind.Explosion, Vector3.one, Vector3.up));
                Assert.IsNull(unexpected,
                    "FAIL-CLOSED CONTRACT: disabled pooling component must reject late cosmetic Emit.");
                Assert.AreEqual(0, pool.ActiveCount);
            }
            finally
            {
                Object.Destroy(root);
            }
            yield return null;
        }
    }
}
