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
                Assert.NotNull(b.GetComponent<DetachedModuleVfxController>());
                Assert.Greater(a.GetComponent<DetachedModuleVfxController>().EmissionRate, 0f);
                Assert.AreEqual(0f, b.GetComponent<DetachedModuleVfxController>().EmissionRate);
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
    }
}
