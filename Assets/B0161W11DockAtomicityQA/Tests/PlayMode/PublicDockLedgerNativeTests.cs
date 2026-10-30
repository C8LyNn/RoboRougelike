using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B01.DebrisPlayground001;
using B01.DebrisRepulse001;

namespace B01.W11LedgerAtomicityQA.Tests
{
    // Executable on licensed Unity 6000.3.14f1 only: actually uses Rigidbody + original W06 ledger.
    public sealed class PublicDockLedgerNativeTests
    {
        private IEnumerator Exercise(int index)
        {
            var host = new GameObject("B01-61 W11 ledger atomicity receiver");
            var demo = host.AddComponent<DebrisRigPlayground>();
            demo.BuildDemo();
            Rigidbody body = null;
            try
            {
                Assert.IsTrue(demo.Detach(index));
                body = demo.PartBody(index);
                var emitter = host.transform.GetChild(index).GetComponent<DebrisRepulsionEmitter>();
                Assert.IsNotNull(emitter);
                Assert.AreEqual(1, emitter.AppliedCount);
                var spec = DebrisRigDemoCatalog.ByIndex(index);
                body.position = new Vector3((float)(spec.RootX + spec.PartOffsetX), 1.2f, 0f);
                body.linearVelocity = new Vector3(1.25f, 0f, -0.5f);
                body.angularVelocity = new Vector3(0f, 0.25f, 0f);
                Physics.SyncTransforms();
                yield return null;

                // Deliberately induce original W06 ledger mismatch AFTER valid detach.
                emitter.ClearForNewRun();
                Assert.AreEqual(0, emitter.AppliedCount);
                Transform parentBefore = body.transform.parent;
                Vector3 positionBefore = body.transform.position;
                Vector3 velocityBefore = body.linearVelocity;
                Vector3 angularBefore = body.angularVelocity;
                bool kinematicBefore = body.isKinematic;

                bool result = false;
                Exception exception = null;
                try { result = demo.Reattach(index); }
                catch(Exception e) { exception = e; }

                Assert.IsTrue(demo.IsDetached(index), "Rearm failure must not change Detached");
                Assert.AreSame(parentBefore, body.transform.parent, "Rearm failure cannot reparent");
                Assert.AreEqual(kinematicBefore, body.isKinematic, "Rearm failure cannot freeze body");
                Assert.Less(Vector3.Distance(positionBefore, body.transform.position), .001f);
                Assert.Less(Vector3.Distance(velocityBefore, body.linearVelocity), .001f);
                Assert.Less(Vector3.Distance(angularBefore, body.angularVelocity), .001f);
                Assert.AreEqual(0, emitter.AppliedCount, "Ledger must not fabricate ID");
                Assert.IsNull(exception, "Fail-closed API should return false, not throw after commit");
                Assert.IsFalse(result, "No successful dock on missing ID");
            }
            finally
            {
                if (body != null && body.gameObject != null) UnityEngine.Object.DestroyImmediate(body.gameObject);
                if (host != null) UnityEngine.Object.DestroyImmediate(host);
            }
        }
        [UnityTest] public IEnumerator LightMissingIdCannotPartialCommit()
        { yield return Exercise(0); }
        [UnityTest] public IEnumerator HeavyMissingIdCannotPartialCommit()
        { yield return Exercise(1); }
    }
}
