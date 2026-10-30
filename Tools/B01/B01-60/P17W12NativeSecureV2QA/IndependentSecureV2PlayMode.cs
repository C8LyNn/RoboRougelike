// B01-60 independent Unity 6000.3.14f1 PlayMode/PhysX receiver-only tests.
// Never included in canonical Codex game. Imported only into disposable project.
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using UObject = UnityEngine.Object;

namespace B01.SecureReattachV2.NativeQA
{
    public sealed class SecureV2IndependentNativePlayMode
    {
        private GameObject host;
        private SecureDebrisPlaygroundV2 demo;
        private GameObject blocker;
        private GameObject standalone;
        private GameObject temporaryParent;

        [SetUp]
        public void Create()
        {
            host = new GameObject("B01-60 W12 independent PhysX receiver");
            demo = host.AddComponent<SecureDebrisPlaygroundV2>();
            demo.BuildDemo();
        }

        [TearDown]
        public void Clean()
        {
            if (blocker) UObject.DestroyImmediate(blocker);
            if (standalone) UObject.DestroyImmediate(standalone);
            if (temporaryParent) UObject.DestroyImmediate(temporaryParent);
            if (host) UObject.DestroyImmediate(host);
        }

        private static bool Dock(SecureDebrisPlaygroundV2 target, int route, int index)
        {
            switch (route)
            {
                case 0: return target.Reattach(index);
                case 1: return target.TrySafeReattach(index);
                case 2: return target.ReattachFromGui(index);
                default: throw new ArgumentOutOfRangeException(nameof(route));
            }
        }

        private void MakeBlocker(int index, int layer, bool isTrigger = false)
        {
            SecureRigSpec spec = SecureRigCatalog.At(index);
            blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "B01-60 W12 real 3D PhysX solid";
            blocker.layer = layer;
            blocker.transform.position = new Vector3(
                (float)(spec.RootX + spec.PartOffsetX), 1.2f, 0f);
            blocker.transform.localScale = Vector3.one * 0.85f;
            blocker.GetComponent<BoxCollider>().isTrigger = isTrigger;
            Physics.SyncTransforms();
        }

        private sealed class Snapshot
        {
            public Rigidbody Body;
            public Vector3 WorldPosition;
            public Quaternion WorldRotation;
            public Vector3 LocalPosition;
            public Transform Parent;
            public Vector3 Velocity;
            public Vector3 AngularVelocity;
            public bool Kinematic;
            public bool Detached;
            public int Consumed;
        }

        private Snapshot Take(int index)
        {
            Rigidbody body = demo.PartBody(index);
            return new Snapshot
            {
                Body = body, Parent = body.transform.parent,
                WorldPosition = body.transform.position,
                WorldRotation = body.transform.rotation,
                LocalPosition = body.transform.localPosition,
                Velocity = body.linearVelocity,
                AngularVelocity = body.angularVelocity,
                Kinematic = body.isKinematic, Detached = demo.IsDetached(index),
                Consumed = demo.AppliedCount(index)
            };
        }

        private void AssertUnchanged(int index, Snapshot before)
        {
            Rigidbody body = demo.PartBody(index);
            Assert.AreSame(before.Body, body, "Rigidbody reference mutated");
            Assert.AreSame(before.Parent, body.transform.parent, "parent changed on deny");
            Assert.AreEqual(before.WorldPosition, body.transform.position, "world pose changed");
            Assert.AreEqual(before.WorldRotation, body.transform.rotation, "world rotation changed");
            Assert.AreEqual(before.LocalPosition, body.transform.localPosition, "local pose changed");
            Assert.AreEqual(before.Velocity, body.linearVelocity, "linear velocity changed");
            Assert.AreEqual(before.AngularVelocity, body.angularVelocity, "angular velocity changed");
            Assert.AreEqual(before.Kinematic, body.isKinematic, "kinematic flag changed");
            Assert.AreEqual(before.Detached, demo.IsDetached(index), "Detached flag changed");
            Assert.AreEqual(before.Consumed, demo.AppliedCount(index), "consumed ID count changed");
        }

        // 2 genuine original masses * 3 publicly callable aliases. This covers
        // full pose/velocity/parent/ID rather than merely boolean denial.
        [TestCase(0,0)] [TestCase(0,1)] [TestCase(0,2)]
        [TestCase(1,0)] [TestCase(1,1)] [TestCase(1,2)]
        public void ColliderOnLayer31RefusesWithoutAnyBodyOrLedgerMutation(int index, int route)
        {
            Assert.IsTrue(demo.Detach(index), "original W06 real detach/force failed");
            MakeBlocker(index, 31);
            Snapshot before = Take(index);
            Assert.IsFalse(Dock(demo, route, index), "solid blocker bypassed all-layer PhysX");
            Assert.AreEqual(DockResult.Obstructed, demo.LastDockResult);
            AssertUnchanged(index, before);
        }

        private AtomicDockLedger InternalLedger(int index)
        {
            FieldInfo rigsField = typeof(SecureDebrisPlaygroundV2).GetField(
                "rigs", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(rigsField, "selected #510 private rig layout changed");
            Array rigs = (Array)rigsField.GetValue(demo);
            object rig = rigs.GetValue(index);
            Assert.IsNotNull(rig);
            FieldInfo field = rig.GetType().GetField("Ledger",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "selected #510 private original AtomicDockLedger missing");
            return (AtomicDockLedger)field.GetValue(rig);
        }

        // Reproduces #486 against Unity REAL Rigidbody/Transform, both original
        // rig masses and each public route. No GUI-only false safety claim.
        [TestCase(0,0)] [TestCase(0,1)] [TestCase(0,2)]
        [TestCase(1,0)] [TestCase(1,1)] [TestCase(1,2)]
        public void MissingConsumedIdRefusesWithoutRealRigidbodyPartialCommit(int index, int route)
        {
            Assert.IsTrue(demo.Detach(index));
            AtomicDockLedger ledger = InternalLedger(index);
            Assert.AreEqual(1, ledger.Count, "original W06 stable ID not consumed");
            ledger.ClearForNewRun(); // test-only fault injection, never production public reset
            Assert.AreEqual(0, ledger.Count);
            Physics.SyncTransforms();
            Snapshot before = Take(index);
            Assert.IsFalse(Dock(demo, route, index), "lost ID bypassed transaction");
            Assert.AreEqual(DockResult.TransactionRejected, demo.LastDockResult);
            AssertUnchanged(index, before);
        }

        // Tests actual native Rigidbody/Transform mutation + rollback callback
        // under ORIGINAL #510 AtomicDockLedger; independent of owner's C# stubs.
        [Test]
        public void ForcedMidCommitExceptionRestoresRealRigidbodyAndConsumedId()
        {
            standalone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temporaryParent = new GameObject("B01-60 alternate parent");
            Rigidbody body = standalone.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = false;
            body.linearVelocity = new Vector3(1f, 2f, 3f);
            body.angularVelocity = new Vector3(0f, 4f, 0f);
            standalone.transform.position = new Vector3(20f, 10f, -5f);
            Physics.SyncTransforms();
            AtomicDockLedger ledger = new AtomicDockLedger();
            long id = 919191;
            Assert.IsTrue(ledger.TryDetach(id,
                new Vector3d(0,0,0), new Vector3d(1,0,0),
                new Vector3d(0,0,0), 2.0, 0, 0, out Vector3d impulse));
            Assert.AreEqual(1, ledger.Count);
            Vector3 originalVelocity = body.linearVelocity;
            Vector3 originalAngular = body.angularVelocity;
            Vector3 originalPosition = standalone.transform.position;
            Quaternion originalRotation = standalone.transform.rotation;
            Transform originalParent = standalone.transform.parent;
            bool originalKinematic = body.isKinematic;

            bool accepted = ledger.TryReattach(id,
                () =>
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                    body.isKinematic = true;
                    standalone.transform.SetParent(temporaryParent.transform, false);
                    standalone.transform.localPosition = Vector3.one;
                    throw new InvalidOperationException("B01-60 deliberately injected physical commit failure");
                },
                () =>
                {
                    standalone.transform.SetParent(originalParent, true);
                    standalone.transform.position = originalPosition;
                    standalone.transform.rotation = originalRotation;
                    body.isKinematic = originalKinematic;
                    body.linearVelocity = originalVelocity;
                    body.angularVelocity = originalAngular;
                });
            Assert.IsFalse(accepted, "transaction cannot report success after callback throws");
            Assert.IsTrue(ledger.Contains(id), "original W06 consumed ID was lost on rollback");
            Assert.AreEqual(1, ledger.Count);
            Assert.AreEqual(originalPosition, standalone.transform.position);
            Assert.AreEqual(originalRotation, standalone.transform.rotation);
            Assert.AreSame(originalParent, standalone.transform.parent);
            Assert.AreEqual(originalKinematic, body.isKinematic);
            Assert.AreEqual(originalVelocity, body.linearVelocity);
            Assert.AreEqual(originalAngular, body.angularVelocity);
        }

        [Test]
        public void RollbackFailureIsQuarantinedRatherThanSilentlyAccepted()
        {
            AtomicDockLedger ledger = new AtomicDockLedger();
            const long id = 828282;
            Assert.IsTrue(ledger.TryDetach(id, new Vector3d(0,0,0),
                new Vector3d(1,0,0), new Vector3d(0,0,0),
                1.0, 0, 0, out Vector3d unused));
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                ledger.TryReattach(id,
                    () => { throw new InvalidOperationException("injected apply failure"); },
                    () => { throw new InvalidOperationException("injected rollback failure"); }));
            StringAssert.Contains("quarantine", ex.Message.ToLowerInvariant());
            Assert.IsTrue(ledger.Contains(id), "ledger must retain consumed ID for quarantine");
            Assert.IsFalse(ledger.Busy, "reset the transaction busy flag on fault exit");
        }
    }
}
