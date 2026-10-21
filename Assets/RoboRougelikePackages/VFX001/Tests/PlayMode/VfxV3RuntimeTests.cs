using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.VFX001.Tests
{
    public sealed class VfxV3RuntimeTests
    {
        [UnityTest]
        public IEnumerator CoreBurst_ContainsAnimatedRing_AndRecyclesIt()
        {
            var root = new GameObject("Pool");
            var pool = root.AddComponent<VfxPoolManager>();
            var actor = pool.Emit(new VfxEvent(VfxEventKind.CoreDestroyed, Vector3.zero, Vector3.up, ImpactMaterialType.Core, 1f));
            Assert.IsNotNull(actor);
            var ring = actor.GetComponentInChildren<VfxShockwaveRing>(true);
            Assert.IsNotNull(ring);
            Assert.IsTrue(ring.IsRunning);
            yield return new WaitForSeconds(.72f);
            Assert.IsFalse(ring.IsRunning);
            pool.ClearAll();
            Assert.AreEqual(0, pool.ActiveCount);
            Object.Destroy(root);
            yield return null;
        }
        [UnityTest]
        public IEnumerator PersistentSmoke_RespondsToDamageAndDetachment()
        {
            var obj = new GameObject("Damage Module");
            var fx = obj.AddComponent<PersistentDamageFxController>();
            yield return null;
            Assert.IsTrue(fx.HasEmitter);
            fx.SetDamage01(.95f);
            Assert.Greater(fx.CurrentEmissionRate, 0f);
            fx.SetDetached(true);
            Assert.AreEqual(0f, fx.CurrentEmissionRate);
            Object.Destroy(obj);
            yield return null;
        }
        [UnityTest]
        public IEnumerator ExhaustTrail_HasNoGameplayBodySideEffects()
        {
            var obj = new GameObject("Nozzle");
            var rb = obj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            var pos = obj.transform.position;
            var fx = obj.AddComponent<EngineExhaustTrailController>();
            yield return null;
            fx.SetThrust01(.8f);
            Assert.IsTrue(fx.HasEmitter);
            Assert.Greater(fx.EmissionRate, 0f);
            Assert.AreEqual(pos, obj.transform.position);
            Assert.AreEqual(Vector3.zero, rb.linearVelocity);
            Object.Destroy(obj);
            yield return null;
        }
        [UnityTest]
        public IEnumerator GroundSkid_RequiresContactSlipAndSpeed()
        {
            var obj = new GameObject("Contact Fx");
            var fx = obj.AddComponent<GroundContactVfxController>();
            yield return null;
            fx.SetMotion(7f, 1f, false);
            Assert.IsFalse(fx.MarksEmitting);
            fx.SetMotion(7f, 1f, true);
            Assert.IsTrue(fx.MarksEmitting);
            Assert.Greater(fx.DustEmissionRate, 0f);
            fx.SetMotion(0f, 1f, true);
            Assert.IsFalse(fx.MarksEmitting);
            Object.Destroy(obj);
            yield return null;
        }
    }
}
