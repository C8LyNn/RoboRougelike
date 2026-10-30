using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B01.DebrisRepulse001;

namespace B01.DebrisPlayground001.Tests
{
    public class DebrisRigPlayModeTests
    {
        private GameObject host;
        private DebrisRigPlayground demo;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("Isolated PlayMode debris demo");
            demo = host.AddComponent<DebrisRigPlayground>();
            demo.BuildDemo();
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
        }

        [Test]
        public void TwoPhysicalBodiesAreDistinctAndMassesDiffer()
        {
            Rigidbody light = demo.PartBody(0);
            Rigidbody heavy = demo.PartBody(1);
            Assert.AreNotSame(light, heavy);
            Assert.IsTrue(light.GetComponent<Collider>() != null);
            Assert.IsTrue(heavy.GetComponent<Collider>() != null);
            Assert.That(light.mass, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(heavy.mass, Is.EqualTo(12f).Within(0.001f));
            Assert.IsTrue(light.isKinematic && heavy.isKinematic);
        }

        [Test]
        public void KinematicBodyCannotConsumeDetachLedger()
        {
            Rigidbody body = demo.PartBody(0);
            var emitter = body.transform.parent.GetComponent<DebrisRepulsionEmitter>();
            Assert.IsNotNull(emitter);
            Assert.IsFalse(emitter.TryApplyDetachImpulse(demo.PartId(0), body, Vector3.zero));
            Assert.That(demo.AppliedCount(0), Is.Zero);
            Assert.IsTrue(demo.Detach(0));
            Assert.That(demo.AppliedCount(0), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RealRigidbodyImpulseSetsTargetOutwardSpeedForBothMasses()
        {
            Assert.IsTrue(demo.Detach(0));
            Assert.IsTrue(demo.Detach(1));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(demo.PartBody(0).linearVelocity.x, Is.EqualTo(2.5f).Within(0.25f));
            Assert.That(demo.PartBody(1).linearVelocity.x, Is.EqualTo(2.5f).Within(0.25f));
        }

        [UnityTest]
        public IEnumerator DuplicateDetachDoesNotStackPhysicalForce()
        {
            Assert.IsTrue(demo.Detach(0));
            Assert.IsFalse(demo.Detach(0));
            Assert.That(demo.AppliedCount(0), Is.EqualTo(1));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(demo.PartBody(0).linearVelocity.x, Is.EqualTo(2.5f).Within(0.25f));
        }

        [UnityTest]
        public IEnumerator ReattachRearmsTheSameStablePartIdentity()
        {
            Assert.IsTrue(demo.Detach(0));
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(demo.Reattach(0));
            Assert.IsTrue(demo.PartBody(0).isKinematic);
            Assert.That(demo.AppliedCount(0), Is.Zero);
            Assert.IsTrue(demo.Detach(0));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(demo.PartBody(0).linearVelocity.x, Is.EqualTo(2.5f).Within(0.25f));
        }

        [Test]
        public void ResetProducesNewPhysicalBodiesWithoutOldLedger()
        {
            Rigidbody previous = demo.PartBody(0);
            Assert.IsTrue(demo.Detach(0));
            demo.ResetDemo();
            demo.BuildDemo();
            Assert.AreNotSame(previous, demo.PartBody(0));
            Assert.That(demo.AppliedCount(0), Is.Zero);
            Assert.IsTrue(demo.Detach(0));
        }

        [Test]
        public void AMountedPartCannotBeReattachedOrDetachedTwice()
        {
            Assert.IsFalse(demo.Reattach(1));
            Assert.IsTrue(demo.Detach(1));
            Assert.IsFalse(demo.Detach(1));
            Assert.IsTrue(demo.Reattach(1));
            Assert.IsFalse(demo.Reattach(1));
        }
    }
}
