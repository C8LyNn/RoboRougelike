using System.Collections;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.VFX001;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.B01Performance.Tests
{
    // REAL UNITY PlayMode tests: written source, NOT_RUN until licensed Editor.
    public sealed class B0150W28DebrisVfxBudgetPlayModeTests
    {
        private static GameObject Spawn(string label, float speed)
        {
            var go = new GameObject(label);
            go.SetActive(false);
            var body = go.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.linearVelocity = Vector3.right * speed;
            go.AddComponent<R3Debris>(); // fires actual static R3Debris.Separated on activation
            go.SetActive(true);
            return go;
        }

        [UnityTest]
        public IEnumerator ActualR3SeparationCreatesBoundedVisualEmittersWithoutVelocityWrites()
        {
            var root = new GameObject("B0150_W28_budget_test");
            var budget = root.AddComponent<B0150DetachedDebrisSparkBudget>();
            budget.Configure(1, 2f, .25f);
            GameObject a = null, b = null;
            try
            {
                a = Spawn("real_R3_debris_fast", 7f);
                b = Spawn("real_R3_debris_slow", 4f);
                yield return null;
                var ra = a.GetComponent<Rigidbody>();
                var rb = b.GetComponent<Rigidbody>();
                float beforeFast = ra.linearVelocity.x, beforeSlow = rb.linearVelocity.x;
                budget.RefreshNow();
                Assert.AreEqual(2, budget.TrackedCount);
                Assert.AreEqual(1, budget.ActiveEmitters);
                Assert.NotNull(a.GetComponent<DetachedModuleVfxController>());
                Assert.IsNull(b.GetComponent<DetachedModuleVfxController>(), "R2 defers unselected emitter allocation");
                Assert.Greater(a.GetComponent<DetachedModuleVfxController>().EmissionRate, 0f);
                // Under lazy allocation the unselected debris intentionally has NO VFX component.
                Assert.AreEqual(beforeFast, ra.linearVelocity.x, 0.001f);
                Assert.AreEqual(beforeSlow, rb.linearVelocity.x, 0.001f);
            }
            finally
            {
                if (a != null) Object.Destroy(a);
                if (b != null) Object.Destroy(b);
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator DisabledBudgetStopsVisualsAndDoesNotSleepDebris()
        {
            var root = new GameObject("B0150_W28_disable_test");
            var budget = root.AddComponent<B0150DetachedDebrisSparkBudget>();
            budget.Configure(1, 2f, .25f);
            GameObject part = null;
            try
            {
                part = Spawn("real_R3_debris", 5f);
                yield return null;
                budget.RefreshNow();
                var body = part.GetComponent<Rigidbody>();
                var fx = part.GetComponent<DetachedModuleVfxController>();
                Assert.NotNull(fx);
                budget.enabled = false;
                Assert.AreEqual(0f, fx.EmissionRate);
                Assert.IsTrue(part.activeInHierarchy);
                Assert.IsFalse(body.isKinematic);
                Assert.AreEqual(0, budget.ActiveEmitters);
            }
            finally
            {
                if (part != null) Object.Destroy(part);
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator GoneDebrisIsPurgedWithoutTouchingOtherBodies()
        {
            var root = new GameObject("B0150_W28_purge_test");
            var budget = root.AddComponent<B0150DetachedDebrisSparkBudget>();
            budget.Configure(1, 2f, .25f);
            GameObject part = null;
            try
            {
                part = Spawn("real_R3_debris_to_remove", 6f);
                yield return null;
                Assert.AreEqual(1, budget.TrackedCount);
                part.SetActive(false);
                budget.RefreshNow();
                Assert.AreEqual(0, budget.TrackedCount);
                Assert.AreEqual(0, budget.ActiveEmitters);
            }
            finally
            {
                if (part != null) Object.Destroy(part);
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator SeventyActualR3DebrisCreateAtMostEightEmittersUntilSelected()
        {
            var root = new GameObject("B0150_W29_large_battle_budget");
            var budget = root.AddComponent<B0150DetachedDebrisSparkBudget>();
            budget.Configure(8, 2f, .25f);
            var parts = new System.Collections.Generic.List<GameObject>();
            try
            {
                for (int i = 0; i < 70; ++i)
                    parts.Add(Spawn("genuine_R3_detachment_" + i, 3f + i * .05f));
                yield return null;
                budget.RefreshNow();
                Assert.AreEqual(70, budget.TrackedCount);
                Assert.AreEqual(8, budget.ActiveEmitters);
                int created = 0, emitting = 0;
                for (int i = 0; i < parts.Count; ++i)
                {
                    var body = parts[i].GetComponent<Rigidbody>();
                    var fx = parts[i].GetComponent<DetachedModuleVfxController>();
                    Assert.NotNull(body);
                    Assert.IsFalse(body.isKinematic);
                    if (fx == null) continue;
                    ++created;
                    if (fx.EmissionRate > 0f) ++emitting;
                }
                Assert.AreEqual(8, created, "Do not allocate 70 VFX MonoBehaviours for 8 visual leases");
                Assert.AreEqual(8, emitting);
            }
            finally
            {
                foreach (var p in parts) if (p != null) Object.Destroy(p);
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator NextFastestRealDebrisGainsSlotAfterRetirement()
        {
            var root = new GameObject("B0150_W29_reassign_visual_lease");
            var budget = root.AddComponent<B0150DetachedDebrisSparkBudget>();
            budget.Configure(1, 2f, .25f);
            GameObject fast = null, slow = null;
            try
            {
                fast = Spawn("fast_R3_debris", 8f);
                slow = Spawn("slow_R3_debris", 5f);
                yield return null;
                budget.RefreshNow();
                Assert.NotNull(fast.GetComponent<DetachedModuleVfxController>());
                Assert.IsNull(slow.GetComponent<DetachedModuleVfxController>());
                fast.SetActive(false);
                budget.RefreshNow();
                Assert.AreEqual(1, budget.TrackedCount);
                Assert.AreEqual(1, budget.ActiveEmitters);
                var resumed = slow.GetComponent<DetachedModuleVfxController>();
                Assert.NotNull(resumed);
                Assert.Greater(resumed.EmissionRate, 0f);
                Assert.AreEqual(0f, fast.GetComponent<DetachedModuleVfxController>().EmissionRate);
            }
            finally
            {
                if (fast != null) Object.Destroy(fast);
                if (slow != null) Object.Destroy(slow);
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator NoEmitterOrBodyWritesAtVisualBudgetZero()
        {
            var root = new GameObject("B0150_W29_zero_budget");
            var budget = root.AddComponent<B0150DetachedDebrisSparkBudget>();
            budget.Configure(0, 1f, .25f);
            GameObject part = null;
            try
            {
                part = Spawn("R3_debris_without_emitter", 7f);
                yield return null;
                var body = part.GetComponent<Rigidbody>();
                float velocityBefore = body.linearVelocity.x;
                budget.RefreshNow();
                Assert.AreEqual(1, budget.TrackedCount);
                Assert.AreEqual(0, budget.ActiveEmitters);
                Assert.IsNull(part.GetComponent<DetachedModuleVfxController>());
                Assert.AreEqual(velocityBefore, body.linearVelocity.x, .001f);
                Assert.IsTrue(part.activeInHierarchy);
            }
            finally
            {
                if (part != null) Object.Destroy(part);
                Object.Destroy(root);
            }
        }
        [UnityTest]
        public IEnumerator BelowVfxActualParticleThresholdDoesNotReportActiveEmitter()
        {
            var root = new GameObject("B0150_W30_no_phantom_sparks");
            var budget = root.AddComponent<B0150DetachedDebrisSparkBudget>();
            GameObject part = null;
            try
            {
                // Budget accepts a weaker velocity threshold than VFX001's own
                // serialized default 2.2 m/s. Source R2 incorrectly said
                // ActiveEmitters=1 even though actual EmissionRate was zero.
                budget.Configure(1, .2f, .25f);
                part = Spawn("actual_R3_slow_unlit_debris", 1f);
                yield return null;
                budget.RefreshNow();
                var fx = part.GetComponent<DetachedModuleVfxController>();
                Assert.IsNotNull(fx, "Selected source creates the VFX component");
                Assert.AreEqual(0f, fx.EmissionRate, "Physics must not light slow debris");
                Assert.IsFalse(fx.IsDetached, "No phantom detached VFX after denial");
                Assert.AreEqual(0, budget.ActiveEmitters,
                    "Budget must count REAL emitting visuals, not selection slots");
                Assert.IsNotNull(part.GetComponent<Rigidbody>());
                Assert.IsTrue(part.activeInHierarchy);
            }
            finally
            {
                if (part != null) Object.Destroy(part);
                Object.Destroy(root);
            }
        }

    }
}
