#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RoboRougelike.B0131.W27.NativeTests
{
    /// <summary>
    /// Real Unity 6000.3.14f1 PlayMode acceptance of REAL P12/W24/W25 source.
    /// Reflection is intentional: frozen P12 and W24 are in predefined
    /// Assembly-CSharp, which a separate PlayMode asmdef cannot reference.
    /// There is NO fake P12, fake UnityEngine or managed-only test source here.
    /// Native G1 remains NOT_RUN until Unity Editor actually executes this.
    /// </summary>
    public sealed class B0131W27ArmorRealP12PlayModeTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        private static Type RuntimeType(string qualifiedName)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type result = a.GetType(qualifiedName, false);
                if (result != null) return result;
            }
            Assert.Fail("Required ORIGINAL runtime type absent: " + qualifiedName);
            throw new InvalidOperationException(qualifiedName);
        }

        private static object Call(Component target, string method, params object[] args)
        {
            var m = target.GetType().GetMethod(method,
                BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(m, "Missing real runtime ABI: " + method);
            try { return m.Invoke(target, args); }
            catch (TargetInvocationException e)
            {
                throw new InvalidOperationException(
                    "Real Unity runtime " + method + " failed", e.InnerException ?? e);
            }
        }

        private static T Property<T>(Component source, string prop)
        {
            var p = source.GetType().GetProperty(prop,
                BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(p, "Required original P12 public property absent: " + prop);
            return (T)p.GetValue(source);
        }

        private static void Alive(bool isAlive, GameObject ok, GameObject damaged,
                                  GameObject destroyed, string context)
        {
            int count = (ok.activeSelf ? 1 : 0) +
                        (damaged.activeSelf ? 1 : 0) +
                        (destroyed.activeSelf ? 1 : 0);
            Assert.AreEqual(1, count, "Exactly ONE visual variant: " + context);
            Assert.AreEqual(isAlive, !destroyed.activeSelf,
                "Destroyed silhouette: " + context);
        }

        private (Component source, Component visual, Component binder,
                 GameObject ok, GameObject damaged, GameObject destroyed) Setup()
        {
            var sourceObject = new GameObject("W27_REAL_P12_TEMPORARY_SOURCE");
            spawned.Add(sourceObject);
            // Separate source and visuals: original P12 may Destroy its
            // GameObject during Die, and must not destroy the visual root.
            var visualObject = new GameObject("W27_REAL_W24_W25_VISUAL");
            spawned.Add(visualObject);

            var p12 = sourceObject.AddComponent(RuntimeType(
                "RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable"));
            Call(p12, "Configure", 100f, false);
            var w24 = visualObject.AddComponent(RuntimeType(
                "RoboRougelike.B0131.Art.B0131W24ArmorDamageVisual"));
            var ok = new GameObject("OK");
            var damaged = new GameObject("DAMAGED");
            var destroyed = new GameObject("DESTROYED");
            ok.transform.SetParent(visualObject.transform, false);
            damaged.transform.SetParent(visualObject.transform, false);
            destroyed.transform.SetParent(visualObject.transform, false);

            Assert.IsTrue((bool)Call(w24, "ConfigureVisuals", ok, damaged, destroyed),
                "Genuine W24 visual controller must have 3 distinct live children");

            var binder = visualObject.AddComponent(RuntimeType(
                "RoboRougelike.B0131.Art.B0131W25P12DamageVisualBinding"));
            Assert.IsTrue((bool)Call(binder, "Bind", p12, w24),
                "Genuine W25 adapter binds to original P12 and W24");
            Assert.IsTrue(ok.activeSelf && !damaged.activeSelf && !destroyed.activeSelf,
                "Real source begins intact");
            return (p12, w24, binder, ok, damaged, destroyed);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var item in spawned)
                if (item != null) Object.Destroy(item);
            spawned.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator RealP12DamageCrossesInclusiveFiftyPercentAndKeepsOneMesh()
        {
            var t = Setup();
            Call(t.source, "ApplyDamage", 49f, Vector3.zero, Vector3.forward);
            yield return null; // Actual Unity MonoBehaviour.Update rather than stub.
            Assert.AreEqual(51f, Property<float>(t.source, "CurrentHp"), 0.001f);
            Assert.IsTrue(t.ok.activeSelf && !t.damaged.activeSelf);
            Call(t.source, "ApplyDamage", 1f, Vector3.zero, Vector3.forward);
            yield return null;
            Assert.AreEqual(50f, Property<float>(t.source, "CurrentHp"), 0.001f);
            Assert.IsFalse(t.ok.activeSelf);
            Assert.IsTrue(t.damaged.activeSelf);
            Alive(true, t.ok, t.damaged, t.destroyed, "P12 at 50%");
        }

        [UnityTest]
        public IEnumerator RealP12RepairRestoresVisualWithoutChangingIdOrSave()
        {
            var t = Setup();
            Call(t.source, "ApplyDamage", 70f, Vector3.zero, Vector3.forward);
            yield return null;
            Assert.IsTrue(t.damaged.activeSelf);
            Call(t.source, "RestoreHealthRatio", .91f);
            yield return null;
            Assert.AreEqual(91f, Property<float>(t.source, "CurrentHp"), 0.001f);
            Assert.IsTrue(t.ok.activeSelf);
            Alive(true, t.ok, t.damaged, t.destroyed, "real original P12 repair");
        }

        [UnityTest]
        public IEnumerator RealP12DieRaisesEventBeforeSourceDestroyAndVisualPersists()
        {
            var t = Setup();
            Call(t.source, "ApplyDamage", 1000f, Vector3.zero, Vector3.forward);
            Assert.IsTrue(t.destroyed.activeSelf,
                "Original P12 Died event must drive W25 immediately");
            yield return null;
            Assert.IsTrue(t.destroyed.activeSelf,
                "Separate visual GameObject survives original destroyed source");
            Alive(false, t.ok, t.damaged, t.destroyed, "original P12 death");
        }

        [UnityTest]
        public IEnumerator InvalidThresholdDoesNotClaimVisualStateAndRealHPUnchanged()
        {
            var t = Setup();
            float oldHP = Property<float>(t.source, "CurrentHp");
            Assert.IsFalse((bool)Call(t.binder, "SetDamageThreshold", float.NaN));
            Assert.IsFalse((bool)Call(t.binder, "SetDamageThreshold", 1.1f));
            yield return null;
            Assert.AreEqual(oldHP, Property<float>(t.source, "CurrentHp"), 0.001f);
            Assert.IsTrue(t.ok.activeSelf);
            Alive(true, t.ok, t.damaged, t.destroyed, "invalid view-only threshold");
        }

        [UnityTest]
        public IEnumerator DisableReenableRehydratesActualP12HPWithoutExtraDamage()
        {
            var t = Setup();
            ((Behaviour)t.binder).enabled = false;
            Call(t.source, "ApplyDamage", 75f, Vector3.zero, Vector3.forward);
            yield return null;
            ((Behaviour)t.binder).enabled = true;
            yield return null;
            Assert.AreEqual(25f, Property<float>(t.source, "CurrentHp"), 0.001f);
            Assert.IsTrue(t.damaged.activeSelf);
            Alive(true, t.ok, t.damaged, t.destroyed, "real Unity OnEnable refresh");
        }
    }
}
#endif
