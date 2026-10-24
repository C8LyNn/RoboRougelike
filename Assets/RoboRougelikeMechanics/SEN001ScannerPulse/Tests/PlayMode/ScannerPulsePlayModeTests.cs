using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.Mechanics.SEN001.Tests
{
    public sealed class ScannerPulsePlayModeTests
    {
        private GameObject scannerObject;
        private GameObject targetObject;
        private ScannerPulse scanner;
        private ScannerDetectable target;

        [SetUp]
        public void SetUp()
        {
            scannerObject = new GameObject("SEN001 play scanner");
            scanner = scannerObject.AddComponent<ScannerPulse>();
            scanner.Configure(8f, 0.05f, 0.05f, ~0);
            targetObject = new GameObject("SEN001 play target");
            targetObject.transform.position = new Vector3(2f, 0f, 0f);
            target = targetObject.AddComponent<ScannerDetectable>();
            targetObject.AddComponent<SphereCollider>();
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(scannerObject);
        }

        [UnityTest]
        public IEnumerator RevealExpiresWithoutChangingTarget()
        {
            Assert.AreEqual(1, scanner.TryPulse(Vector3.zero));
            Assert.IsTrue(scanner.IsRevealed(target));
            yield return new WaitForSeconds(0.12f);
            Assert.IsFalse(scanner.IsRevealed(target));
            Assert.IsEmpty(scanner.Revealed);
            Assert.IsTrue(targetObject.activeInHierarchy);
        }

        [UnityTest]
        public IEnumerator CooldownExpiresAndNextPulseIsAllowed()
        {
            Assert.AreEqual(1, scanner.TryPulse(Vector3.zero));
            Assert.AreEqual(-1, scanner.TryPulse(Vector3.zero));
            yield return new WaitForSeconds(0.12f);
            Assert.AreEqual(1, scanner.TryPulse(Vector3.zero));
        }

        [UnityTest]
        public IEnumerator DisablingScannerClearsState()
        {
            Assert.AreEqual(1, scanner.TryPulse(Vector3.zero));
            scanner.enabled = false;
            yield return null;
            Assert.IsFalse(scanner.IsRevealed(target));
            Assert.AreEqual(-1, scanner.TryPulse(Vector3.zero));
        }
    }
}
