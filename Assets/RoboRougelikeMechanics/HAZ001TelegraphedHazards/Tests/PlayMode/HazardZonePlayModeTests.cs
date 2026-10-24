using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.Mechanics.HAZ001.Tests
{
    public sealed class HazardZonePlayModeTests
    {
        private GameObject zoneObject;
        private GameObject targetObject;
        private TelegraphedHazardZone zone;

        [SetUp]
        public void SetUp()
        {
            zoneObject = new GameObject("HAZ001 Zone");
            zone = zoneObject.AddComponent<TelegraphedHazardZone>();
            zone.Configure(.05f, 30f, 1f, 3f, .5f, 7f, 1 << 9);
            targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.layer = 9;
            targetObject.transform.position = Vector3.right;
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(zoneObject);
            Object.DestroyImmediate(targetObject);
        }

        [UnityTest]
        public IEnumerator DamageRequestsAreRateLimitedAndNonMutating()
        {
            int hits = 0;
            float lastAmount = -1f;
            zone.DamageRequested += (_, amount) => { hits++; lastAmount = amount; };
            zone.Begin();
            zone.ManualTick(.05f);
            Assert.AreEqual(1, zone.ScanAtTime(0f));
            Assert.AreEqual(0, zone.ScanAtTime(.2f));
            Assert.AreEqual(1, zone.ScanAtTime(.5f));
            Assert.AreEqual(2, hits);
            Assert.AreEqual(7f, lastAmount);
            Assert.IsTrue(targetObject.activeSelf);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LayerMaskRejectsOtherObjects()
        {
            targetObject.layer = 10;
            Physics.SyncTransforms();
            zone.Begin();
            zone.ManualTick(.05f);
            Assert.AreEqual(0, zone.ScanAtTime(0f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TwoCollidersOnSameRigidbodyEmitOnce()
        {
            Object.DestroyImmediate(targetObject.GetComponent<BoxCollider>());
            var body = targetObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            targetObject.AddComponent<BoxCollider>();
            var child = new GameObject("Second collider");
            child.transform.SetParent(targetObject.transform, false);
            child.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            zone.Begin();
            zone.ManualTick(.05f);
            Assert.AreEqual(1, zone.ScanAtTime(0f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisablingCancelsActiveHazard()
        {
            zone.Begin();
            zone.ManualTick(.05f);
            zoneObject.SetActive(false);
            Assert.AreEqual(HazardPhase.Idle, zone.Phase);
            Assert.AreEqual(0, zone.ScanAtTime(0f));
            yield return null;
        }
    }
}
