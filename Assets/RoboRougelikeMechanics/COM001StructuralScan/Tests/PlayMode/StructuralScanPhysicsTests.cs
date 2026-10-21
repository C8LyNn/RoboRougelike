using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.Mechanics.StructuralScan;

namespace RoboRougelike.Mechanics.StructuralScan.Tests.PlayMode
{
    public class StructuralScanPhysicsTests
    {
        private readonly List<GameObject> created = new List<GameObject>();
        private StructuralScanRegistry registry;
        private StructuralScanController scanner;
        private Transform sensor;
        private Transform ownerRoot;
        private StructuralScanSettings settings;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            created.Clear();
            registry = Make("registry").AddComponent<StructuralScanRegistry>();
            ownerRoot = Make("own-vehicle").transform;
            sensor = Make("sensor").transform;
            sensor.SetParent(ownerRoot);
            sensor.position = new Vector3(0, 1, 0);
            sensor.rotation = Quaternion.identity;
            scanner = Make("scanner").AddComponent<StructuralScanController>();
            settings = new StructuralScanSettings { rangeMeters = 24, fullConeDegrees = 75, cooldownSeconds = 8, displaySeconds = 5, maxRevealedParts = 16 };
            scanner.Bind(sensor, ownerRoot, 1, 1, registry);
            scanner.Configure(settings);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = created.Count - 1; i >= 0; --i) if (created[i] != null) Object.Destroy(created[i]);
            created.Clear();
            yield return null;
        }

        private GameObject Make(string name) { var go = new GameObject(name); created.Add(go); return go; }
        private StructuralScanPartProxy Target(string name, Vector3 point, int owner = 2, int team = 2, int id = 1,
            StructuralPartType type = StructuralPartType.Armor)
        {
            var go = Make(name); go.transform.position = point;
            go.AddComponent<BoxCollider>();
            var proxy = go.AddComponent<StructuralScanPartProxy>();
            proxy.Configure(registry, owner, team, id, type);
            Physics.SyncTransforms();
            return proxy;
        }
        private void Barrier(Vector3 point, Vector3 scale)
        {
            var go = Make("barrier"); go.transform.position = point; go.transform.localScale = scale; go.AddComponent<BoxCollider>(); Physics.SyncTransforms();
        }
        private void SetEnergy(float required, float available)
        {
            settings.activationEnergy = required;
            var source = Make("energy").AddComponent<StructuralScanEnergyStore>();
            source.Configure(100, available);
            scanner.Bind(sensor, ownerRoot, 1, 1, registry, source);
            scanner.Configure(settings);
        }

        [UnityTest] public IEnumerator VisibleEnemyScanSucceeds()
        {
            Target("wheel", new Vector3(0, 1, 8), type: StructuralPartType.Wheel);
            Assert.IsTrue(scanner.TryScan()); Assert.AreEqual(1, scanner.Contacts.Count); Assert.AreEqual(2, scanner.SelectedTargetOwner);
            yield return null;
        }
        [UnityTest] public IEnumerator BlockingWallHidesTarget()
        {
            Target("weapon", new Vector3(0, 1, 8)); Barrier(new Vector3(0, 1, 4), new Vector3(3, 4, .5f));
            Assert.IsFalse(scanner.TryScan()); yield return null;
        }
        [UnityTest] public IEnumerator TargetBehindScannerIsNotDetected()
        {
            Target("rear", new Vector3(0, 1, -8)); Assert.IsFalse(scanner.TryScan()); yield return null;
        }
        [UnityTest] public IEnumerator TargetOutsideRangeIsNotDetected()
        {
            settings.rangeMeters = 6; scanner.Configure(settings); Target("far", new Vector3(0, 1, 9));
            Assert.IsFalse(scanner.TryScan()); yield return null;
        }
        [UnityTest] public IEnumerator FriendlyTargetIsNotDetected()
        {
            Target("friend", new Vector3(0, 1, 8), 2, 1); Assert.IsFalse(scanner.TryScan()); yield return null;
        }
        [UnityTest] public IEnumerator OwnTargetIsNotDetected()
        {
            Target("my-part", new Vector3(0, 1, 8), 1, 2); Assert.IsFalse(scanner.TryScan()); yield return null;
        }
        [UnityTest] public IEnumerator DetachedPartIsNotDetected()
        {
            var part = Target("detached", new Vector3(0, 1, 8)); part.SetAttached(false);
            Assert.IsFalse(scanner.TryScan()); yield return null;
        }
        [UnityTest] public IEnumerator ResultsExpireWithoutRemovingCooldown()
        {
            Target("wheel", new Vector3(0, 1, 8)); Assert.IsTrue(scanner.TryScan()); scanner.Advance(6);
            Assert.IsFalse(scanner.HasVisibleContacts); Assert.AreEqual(0, scanner.Contacts.Count); Assert.IsFalse(scanner.IsReady);
            yield return null;
        }
        [UnityTest] public IEnumerator CooldownBlocksSecondScan()
        {
            Target("wheel", new Vector3(0, 1, 8)); Assert.IsTrue(scanner.TryScan());
            Assert.IsFalse(scanner.TryScan()); scanner.Advance(9); Assert.IsTrue(scanner.TryScan());
            yield return null;
        }
        [UnityTest] public IEnumerator NoEnergyDoesNotSpendOrCooldown()
        {
            Target("wheel", new Vector3(0, 1, 8)); SetEnergy(7, 6);
            Assert.IsFalse(scanner.TryScan()); Assert.IsTrue(scanner.IsReady); yield return null;
        }
        [UnityTest] public IEnumerator ScanSpendsEnergyExactlyOnce()
        {
            Target("wheel", new Vector3(0, 1, 8)); SetEnergy(7, 16);
            var source = Object.FindFirstObjectByType<StructuralScanEnergyStore>();
            Assert.IsTrue(scanner.TryScan()); Assert.AreEqual(9, source.Charge, .001f);
            Assert.IsFalse(scanner.TryScan()); Assert.AreEqual(9, source.Charge, .001f); yield return null;
        }
        [UnityTest] public IEnumerator FailedScanDoesNotSpendEnergy()
        {
            Target("wheel", new Vector3(0, 1, -8)); SetEnergy(7, 16);
            var source = Object.FindFirstObjectByType<StructuralScanEnergyStore>();
            Assert.IsFalse(scanner.TryScan()); Assert.AreEqual(16, source.Charge, .001f); yield return null;
        }
        [UnityTest] public IEnumerator MostRelevantVisibleContactFirst()
        {
            Target("armor", new Vector3(-1, 1, 8), id: 1, type: StructuralPartType.Armor);
            Target("core", new Vector3(1, 1, 8), id: 2, type: StructuralPartType.Core);
            Assert.IsTrue(scanner.TryScan()); Assert.AreEqual(2, scanner.Contacts.Count);
            Assert.AreEqual(2, scanner.Contacts[0].PartKey); yield return null;
        }
        [UnityTest] public IEnumerator ScanDoesNotRevealOccludedSibling()
        {
            Target("visible-wheel", new Vector3(-2, 1, 7), id: 1, type: StructuralPartType.Wheel);
            Target("hidden-core", new Vector3(2, 1, 9), id: 2, type: StructuralPartType.Core);
            Barrier(new Vector3(1, 1, 4), new Vector3(1, 4, 1));
            Assert.IsTrue(scanner.TryScan()); Assert.AreEqual(1, scanner.Contacts.Count); Assert.AreEqual(1, scanner.Contacts[0].PartKey);
            yield return null;
        }
        [UnityTest] public IEnumerator OneTargetChosenWhenTwoEnemyVehiclesExist()
        {
            Target("close", new Vector3(-1, 1, 6), 2, 2, 1);
            Target("far", new Vector3(1, 1, 10), 3, 3, 1);
            Assert.IsTrue(scanner.TryScan()); Assert.AreEqual(2, scanner.SelectedTargetOwner);
            Assert.AreEqual(1, scanner.Contacts.Count); yield return null;
        }
        [UnityTest] public IEnumerator LostSensorOrPartInvalidatesActivation()
        {
            var part = Target("part", new Vector3(0, 1, 8)); part.SetAttached(false);
            Assert.IsFalse(scanner.TryScan()); scanner.Bind(null, ownerRoot, 1, 1, registry);
            Assert.IsFalse(scanner.TryScan()); yield return null;
        }
        [UnityTest] public IEnumerator OwnColliderBetweenSensorAndTargetIgnored()
        {
            Target("enemy", new Vector3(0, 1, 8));
            var own = Make("own-armor"); own.transform.SetParent(ownerRoot);
            own.transform.position = new Vector3(0, 1, 3); own.AddComponent<BoxCollider>();
            Physics.SyncTransforms(); Assert.IsTrue(scanner.TryScan()); yield return null;
        }
        [UnityTest] public IEnumerator SolidColliderInsideSensorBlocksScanning()
        {
            Target("enemy", new Vector3(0, 1, 8)); Barrier(new Vector3(0, 1, 0), new Vector3(1, 1, 1));
            Assert.IsFalse(scanner.TryScan()); yield return null;
        }
        [UnityTest] public IEnumerator MultipleCollidersOnOnePartReturnOneContact()
        {
            var proxy = Target("enemy", new Vector3(0, 1, 8));
            var go = proxy.gameObject;
            var child = new GameObject("second-collider"); child.transform.SetParent(go.transform);
            child.transform.localPosition = new Vector3(0, 0, .2f); child.AddComponent<BoxCollider>();
            proxy.Configure(registry, 2, 2, 1, StructuralPartType.Weapon);
            Physics.SyncTransforms(); Assert.IsTrue(scanner.TryScan()); Assert.AreEqual(1, scanner.Contacts.Count);
            Object.Destroy(child); yield return null;
        }
        [UnityTest] public IEnumerator ResultsLimitApplies()
        {
            settings.maxRevealedParts = 1; scanner.Configure(settings);
            Target("a", new Vector3(-1, 1, 8), id: 1);
            Target("b", new Vector3(1, 1, 8), id: 2);
            Assert.IsTrue(scanner.TryScan()); Assert.AreEqual(1, scanner.Contacts.Count); yield return null;
        }
        [UnityTest] public IEnumerator OperationalOffClearsResults()
        {
            Target("a", new Vector3(0, 1, 8)); Assert.IsTrue(scanner.TryScan());
            scanner.SetOperational(false); Assert.IsFalse(scanner.HasVisibleContacts); Assert.IsFalse(scanner.TryScan());
            yield return null;
        }
        [UnityTest] public IEnumerator PhysicsStepDoesNotDestroyScanState()
        {
            Target("a", new Vector3(0, 1, 8)); Assert.IsTrue(scanner.TryScan());
            yield return new WaitForFixedUpdate(); Assert.IsTrue(scanner.HasVisibleContacts);
        }
    }
}
