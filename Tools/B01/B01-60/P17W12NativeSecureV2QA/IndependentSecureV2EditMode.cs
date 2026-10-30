// Independent native Unity Editor assembly/link + original W06 ledger smoke tests.
using NUnit.Framework;
using UnityEngine;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
namespace B01.SecureReattachV2.NativeQA
{
    public sealed class SecureV2IndependentNativeEditMode
    {
        [Test]
        public void ExactOwnerRuntimeAndOriginalW06AreLinkableByUnity()
        {
            Assert.IsNotNull(typeof(SecureDebrisPlaygroundV2).GetMethod("Reattach"));
            Assert.IsNotNull(typeof(SecureDebrisPlaygroundV2).GetMethod("TrySafeReattach"));
            Assert.IsNotNull(typeof(SecureDebrisPlaygroundV2).GetMethod("ReattachFromGui"));
            Assert.IsNotNull(typeof(AtomicDockLedger).GetMethod("TryReattach"));
            Assert.IsNotNull(typeof(DebrisImpulseLedger).GetMethod("RearmAfterReattach"));
            Assert.IsTrue(typeof(SecureDebrisPlaygroundV2).IsSubclassOf(typeof(MonoBehaviour)));
        }

        [Test]
        public void RealOriginalW06LedgerClearFailsClosedOnMissingIdBeforeCallbacks()
        {
            const long partId = 9911;
            var ledger = new AtomicDockLedger();
            Assert.IsTrue(ledger.TryDetach(partId,
                new Vector3d(0,0,0), new Vector3d(1,0,0),
                new Vector3d(0,0,0), 1.0, 0, 0, out Vector3d impulse));
            Assert.IsTrue(ledger.Contains(partId));
            ledger.ClearForNewRun();
            bool mutatorCalled = false;
            bool rolledBack = false;
            bool allowed = ledger.TryReattach(partId,
                () => { mutatorCalled = true; },
                () => { rolledBack = true; });
            Assert.IsFalse(allowed);
            Assert.IsFalse(mutatorCalled, "original ID precondition bypassed");
            Assert.IsFalse(rolledBack, "rollback should not start on preflight denial");
            Assert.IsFalse(ledger.Busy);
        }
    }
}
