using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.VFX001.Tests
{
    /// <summary>
    /// B01-32 PASS15: a real Unity PlayMode witness for *two independent visual paths*.
    /// The test assembly references VFX001.Runtime, not the canonical Assembly-CSharp.
    /// Reflection invokes ORIGINAL R6 feedback/bridge and P12 damageable without
    /// changing any game asmdef or adding compile-time dependencies on Codex code.
    /// Authored only; requires an actual activated Unity 6000.3.14f1 run.
    /// </summary>
    public sealed class B0132P15MuzzlePoolSeparationTests
    {
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        private static Type RequiredType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, false);
                if (type != null) return type;
            }
            throw new AssertionException("Original game type missing: " + fullName +
                " (this test must run against the full Codex integration, not VFX001 alone)");
        }

        private static MethodInfo RequiredMethod(Type owner, string name, BindingFlags flags)
        {
            var method = owner.GetMethod(name, flags);
            if (method == null) throw new AssertionException(owner.FullName + "." + name + " missing");
            return method;
        }

        private sealed class Rig
        {
            private readonly Type feedbackType;
            private readonly MonoBehaviour bridge;
            private readonly object feedback;
            private readonly bool feedbackWasPresent;
            private readonly GameObject bridgeObject;
            private readonly GameObject muzzleObject;
            private readonly MethodInfo clearTransient;
            private readonly MethodInfo muzzle;
            private readonly MethodInfo switchVfx;
            private readonly FieldInfo legacyQueueField;

            public readonly VfxPoolManager Pool;

            public Rig()
            {
                var bridgeType = RequiredType("RoboRougelike.VFX001Integration.Vfx001R6LiveBridge");
                feedbackType = RequiredType("RoboRougelike.R6.R6CombatFeedback");
                var damageType = RequiredType("RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable");
                if (!typeof(MonoBehaviour).IsAssignableFrom(bridgeType) ||
                    !typeof(MonoBehaviour).IsAssignableFrom(damageType))
                    throw new AssertionException("Expected original bridge/damageable MonoBehaviour APIs");

                clearTransient = RequiredMethod(feedbackType, "ClearTransientVisuals", PublicStatic);
                muzzle = RequiredMethod(feedbackType, "Muzzle", PublicStatic);
                switchVfx = RequiredMethod(bridgeType, "SetVisualEffectsEnabled",
                    BindingFlags.Instance | BindingFlags.Public);
                legacyQueueField = feedbackType.GetField("effects", PrivateInstance);
                if (legacyQueueField == null)
                    throw new AssertionException("Original legacy R6 effects queue missing");

                // Do not destroy an existing game-owned R6 singleton after the test.
                feedbackWasPresent = GameObject.Find("Combat feedback R6") != null;
                feedback = RequiredMethod(feedbackType, "Ensure", PublicStatic).Invoke(null, null);
                clearTransient.Invoke(null, null);
                bridgeObject = new GameObject("B01-32 P15 temporary bridge");
                bridge = (MonoBehaviour)bridgeObject.AddComponent(bridgeType);
                Pool = bridgeObject.GetComponentInChildren<VfxPoolManager>(true);
                if (Pool == null)
                    throw new AssertionException("Original bridge Awake did not create VFX001 pool");

                muzzleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                muzzleObject.name = "B01-32 P15 isolated damageable muzzle";
                if (muzzleObject.GetComponent<Renderer>()?.sharedMaterial == null)
                    throw new AssertionException("Muzzle fixture requires an original Renderer material");
                muzzleObject.AddComponent(damageType);
            }

            public int LegacyCount
            {
                get
                {
                    var queue = legacyQueueField.GetValue(feedback);
                    var countProperty = queue?.GetType().GetProperty("Count");
                    if (countProperty == null)
                        throw new AssertionException("Original R6 queue Count unavailable");
                    return (int)countProperty.GetValue(queue);
                }
            }

            public void LegacyMuzzle()
            {
                var parameters = muzzle.GetParameters();
                if (parameters.Length != 2 ||
                    parameters[0].ParameterType != typeof(Transform) ||
                    !parameters[1].ParameterType.IsEnum)
                    throw new AssertionException("Original R6 Muzzle(Transform,R4WeaponKind) changed");
                var automatic = Enum.Parse(parameters[1].ParameterType, "Automatic");
                muzzle.Invoke(null, new object[] { muzzleObject.transform, automatic });
            }

            public void PooledHit()
            {
                var actor = Pool.Emit(new VfxEvent(
                    VfxEventKind.Hit, Vector3.zero, Vector3.up,
                    ImpactMaterialType.Metal, .25f));
                Assert.IsNotNull(actor, "Original cosmetic pool must admit normal hit first");
            }

            public void VisualEffectsOff() => switchVfx.Invoke(bridge, new object[] { false });
            public void DisableBridge() => bridge.enabled = false;

            public void Cleanup()
            {
                clearTransient.Invoke(null, null);
                if (bridgeObject != null) UnityEngine.Object.Destroy(bridgeObject);
                if (muzzleObject != null) UnityEngine.Object.Destroy(muzzleObject);
                if (!feedbackWasPresent && feedback is Component component && component != null)
                    UnityEngine.Object.Destroy(component.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator PooledOff_CurrentSourceStillAllowsLegacyMuzzle_ThenBridgeDisableClearsBoth()
        {
            Rig rig = null;
            try
            {
                rig = new Rig();
                rig.PooledHit();
                rig.LegacyMuzzle();

                Assert.AreEqual(1, rig.Pool.ActiveCount, "VFX001 effect precondition");
                Assert.Greater(rig.LegacyCount, 0, "Original independent R6 muzzle precondition");

                // SOURCE CONTRACT: current SetVisualEffectsEnabled(false) gates ONLY pool.
                rig.VisualEffectsOff();
                Assert.AreEqual(0, rig.Pool.ActiveCount,
                    "OFF must immediately clear original pooled effects");
                Assert.Greater(rig.LegacyCount, 0,
                    "DIAGNOSTIC: original R6 muzzle survives pool-only OFF today");

                // OnDisable DOES call both cleanup paths even without an encounter.
                rig.PooledHit();
                rig.LegacyMuzzle();
                rig.DisableBridge();
                Assert.AreEqual(0, rig.Pool.ActiveCount,
                    "Bridge teardown must clear already active VFX001 actors");
                Assert.AreEqual(0, rig.LegacyCount,
                    "Bridge teardown must clear separate R6 transient muzzle queue");
            }
            finally { rig?.Cleanup(); }
            yield return null; // let Unity finish Destroy of transient renderer objects
        }

        [UnityTest]
        [Explicit("PENDING HQ VFX_OFF global-scope decision; expected RED on current source")]
        public IEnumerator GlobalVfxOff_MustAlsoClearLegacyMuzzle_OnlyIfApprovedByHQ()
        {
            Rig rig = null;
            try
            {
                rig = new Rig();
                rig.PooledHit();
                rig.LegacyMuzzle();
                Assert.AreEqual(1, rig.Pool.ActiveCount);
                Assert.Greater(rig.LegacyCount, 0);
                rig.VisualEffectsOff();
                Assert.AreEqual(0, rig.Pool.ActiveCount);
                Assert.AreEqual(0, rig.LegacyCount,
                    "If HQ approves GLOBAL cosmetics OFF, muzzle spheres also must clear");
            }
            finally { rig?.Cleanup(); }
            yield return null;
        }
    }
}
