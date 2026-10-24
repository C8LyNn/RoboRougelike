using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Mechanics.SEN001.Tests
{
    public sealed class ScannerPulseEditModeTests
    {
        private GameObject scannerObject;
        private GameObject targetObject;
        private ScannerPulse scanner;
        private ScannerDetectable target;

        [SetUp]
        public void SetUp()
        {
            scannerObject = new GameObject("SEN001 scanner");
            scanner = scannerObject.AddComponent<ScannerPulse>();
            scanner.Configure(8f, 0f, 1f, ~0);
            targetObject = new GameObject("SEN001 target");
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

        [Test]
        public void FindsMarkedTargetAndLeavesColliderUntouched()
        {
            var collider = targetObject.GetComponent<Collider>();
            Assert.AreEqual(1, scanner.TryPulse(Vector3.zero));
            Assert.AreSame(target, scanner.Revealed[0]);
            Assert.IsTrue(scanner.IsRevealed(target));
            Assert.IsTrue(collider.enabled);
            Assert.IsTrue(targetObject.activeSelf);
        }

        [Test]
        public void MultipleCollidersOnSameTargetAreDeduplicated()
        {
            targetObject.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            Assert.AreEqual(1, scanner.TryPulse(Vector3.zero));
            Assert.AreEqual(1, scanner.LastHits.Count);
        }

        [Test]
        public void LayerMaskExcludesTargetsOutsideMask()
        {
            targetObject.layer = 9;
            scanner.Configure(8f, 0f, 1f, 1 << 8);
            Assert.AreEqual(0, scanner.TryPulse(Vector3.zero));
            scanner.ResetScanner();
            targetObject.layer = 8;
            Assert.AreEqual(1, scanner.TryPulse(Vector3.zero));
        }

        [Test]
        public void SelfHierarchyIsExcluded()
        {
            scannerObject.AddComponent<ScannerDetectable>();
            scannerObject.AddComponent<SphereCollider>();
            Physics.SyncTransforms();
            Assert.AreEqual(1, scanner.TryPulse(Vector3.zero));
        }

        [Test]
        public void CooldownAndResetAreDeterministic()
        {
            scanner.Configure(8f, 60f, 1f, ~0);
            Assert.AreEqual(1, scanner.TryPulse(Vector3.zero));
            Assert.AreEqual(-1, scanner.TryPulse(Vector3.zero));
            scanner.ResetScanner();
            Assert.IsEmpty(scanner.Revealed);
            Assert.AreEqual(1, scanner.TryPulse(Vector3.zero));
        }

        [Test]
        public void SignalMaskFiltersKindsWithoutChangingTargets()
        {
            target.SetSignalKind(ScannerSignalKind.Enemy);
            scanner.ConfigureSignals(ScannerSignalMask.Salvage | ScannerSignalMask.Hazard);
            Assert.AreEqual(0, scanner.TryPulse(Vector3.zero));
            Assert.AreEqual(ScannerSignalKind.Enemy, target.SignalKind);
            scanner.ConfigureSignals(ScannerSignalMask.Enemy);
            Assert.AreEqual(1, scanner.TryPulse(Vector3.zero));
            Assert.AreEqual(ScannerSignalKind.Enemy, target.SignalKind);
        }

        [Test]
        public void EmptySignalMaskFindsNothingAndUnknownCanBeReenabled()
        {
            scanner.ConfigureSignals(ScannerSignalMask.None);
            Assert.AreEqual(0, scanner.TryPulse(Vector3.zero));
            scanner.ConfigureSignals(ScannerSignalMask.Unknown);
            Assert.AreEqual(1, scanner.TryPulse(Vector3.zero));
        }

        [Test]
        public void DisabledRevealedMarkerIsHiddenAndPrunedOnNextPulse()
        {
            Assert.AreEqual(1, scanner.TryPulse(Vector3.zero));
            target.enabled = false;
            Assert.IsFalse(scanner.IsRevealed(target));
            Assert.AreEqual(0, scanner.TryPulse(Vector3.zero));
            Assert.IsEmpty(scanner.Revealed);
        }

        [Test]
        public void DisabledMarkerIsNotDetected()
        {
            target.enabled = false;
            Assert.AreEqual(0, scanner.TryPulse(Vector3.zero));
        }
    }
}
