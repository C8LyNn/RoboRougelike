using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.Mechanics.MOV008.Tests
{
    public sealed class CrawlerTrackPlayModeTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();
        private PhysicsMaterial floorMaterial;

        private struct Rig
        {
            public GameObject Root;
            public Rigidbody Body;
            public CrawlerTrackController Drive;
            public CrawlerTrackModule Left;
            public CrawlerTrackModule Right;
        }

        private GameObject Make(string name)
        {
            var gameObject = new GameObject(name);
            spawned.Add(gameObject);
            return gameObject;
        }

        private void Floor()
        {
            GameObject floor = Make("Static floor");
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            BoxCollider col = floor.AddComponent<BoxCollider>();
            col.size = new Vector3(200f, 1f, 200f);
            // The controller models longitudinal and lateral track grip itself.
            // A default sliding hull adds competing PhysX friction at its box contacts.
            floorMaterial = new PhysicsMaterial("Crawler test floor")
            {
                staticFriction = 0f,
                dynamicFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };
            col.sharedMaterial = floorMaterial;
            Physics.SyncTransforms();
        }

        private CrawlerTrackModule AddTrack(GameObject parent, CrawlerTrackSide side, float x)
        {
            GameObject trackObj = Make(side.ToString() + " track");
            trackObj.transform.SetParent(parent.transform, false);
            trackObj.transform.localPosition = new Vector3(x, -0.5f, 0f);
            var track = trackObj.AddComponent<CrawlerTrackModule>();
            track.SetSide(side);
            return track;
        }

        private Rig CreateRig(bool ground = true, float mass = 300f, float y = 1f)
        {
            if (ground) Floor();
            GameObject root = Make("Vehicle");
            root.transform.position = new Vector3(0f, y, 0f);
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(2f, 1f, 3f);
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.None;
            var drive = root.AddComponent<CrawlerTrackController>();
            CrawlerTrackModule left = AddTrack(root, CrawlerTrackSide.Left, -0.92f);
            CrawlerTrackModule right = AddTrack(root, CrawlerTrackSide.Right, 0.92f);
            drive.RefreshModules();
            Physics.SyncTransforms();
            return new Rig { Root = root, Body = rb, Drive = drive, Left = left, Right = right };
        }

        private static IEnumerator Steps(int count)
        {
            for (int i = 0; i < count; i++) yield return new WaitForFixedUpdate();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (GameObject obj in spawned)
                if (obj != null) Object.Destroy(obj);
            spawned.Clear();
            if (floorMaterial != null) Object.Destroy(floorMaterial);
            floorMaterial = null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator BothTracksPropelChassisForward()
        {
            Rig rig = CreateRig();
            rig.Drive.SetInput(1f, 0f);
            yield return Steps(65);
            Assert.Greater(rig.Root.transform.position.z, 1f);
            Assert.Greater(rig.Drive.GroundedModuleCount, 0);
        }

        [UnityTest]
        public IEnumerator BothTracksPropelChassisBackwards()
        {
            Rig rig = CreateRig();
            rig.Drive.SetInput(-1f, 0f);
            yield return Steps(60);
            Assert.Less(rig.Root.transform.position.z, -0.5f);
        }

        [UnityTest]
        public IEnumerator OppositeTrackDemandTurnsOnTheSpot()
        {
            Rig rig = CreateRig();
            rig.Drive.SetInput(0f, 1f);
            yield return Steps(12);
            Assert.Greater(rig.Body.angularVelocity.y, 0.05f);
        }

        [UnityTest]
        public IEnumerator SteeringTheOtherDirectionReversesYaw()
        {
            Rig rig = CreateRig();
            rig.Drive.SetInput(0f, -1f);
            yield return Steps(12);
            Assert.Less(rig.Body.angularVelocity.y, -0.05f);
        }

        [UnityTest]
        public IEnumerator NoGroundMeansNoPropulsion()
        {
            Rig rig = CreateRig(false, 300f, 6f);
            rig.Drive.SetInput(1f, 0f);
            yield return Steps(10);
            Assert.AreEqual(0, rig.Drive.GroundedModuleCount);
            Assert.Less(Mathf.Abs(rig.Root.transform.position.z), 0.05f);
        }

        [UnityTest]
        public IEnumerator NoFloorSelfCollidersNeverProvideTraction()
        {
            Rig rig = CreateRig(false, 300f, 1f);
            rig.Drive.SetInput(1f, 0f);
            yield return Steps(10);
            Assert.AreEqual(0, rig.Drive.GroundedModuleCount);
            Assert.Less(Mathf.Abs(rig.Root.transform.position.z), 0.05f);
        }

        [UnityTest]
        public IEnumerator UnpoweredTracksCannotPropelChassis()
        {
            Rig rig = CreateRig();
            rig.Left.SetOperational(false);
            rig.Right.SetOperational(false);
            rig.Drive.SetInput(1f, 0f);
            yield return Steps(35);
            Assert.AreEqual(0, rig.Drive.GroundedModuleCount);
            Assert.Less(Mathf.Abs(rig.Root.transform.position.z), 0.05f);
        }

        [UnityTest]
        public IEnumerator DetachedTrackIsRemovedFromPhysicalDrive()
        {
            Rig rig = CreateRig();
            yield return Steps(5);
            Assert.AreEqual(2, rig.Drive.GroundedModuleCount);
            GameObject detachedParent = Make("Debris root");
            rig.Right.transform.SetParent(detachedParent.transform);
            Physics.SyncTransforms();
            yield return Steps(5);
            Assert.AreEqual(1, rig.Drive.GroundedModuleCount);
            Assert.IsFalse(rig.Right.IsAvailableFor(rig.Body));
        }

        [UnityTest]
        public IEnumerator DisablingControllerDisablesAllPropulsion()
        {
            Rig rig = CreateRig();
            rig.Drive.SetDriveEnabled(false);
            rig.Drive.SetInput(1f, 1f);
            yield return Steps(35);
            Assert.Less(Mathf.Abs(rig.Root.transform.position.z), 0.05f);
            Assert.AreEqual(0, rig.Drive.GroundedModuleCount);
        }

        [UnityTest]
        public IEnumerator EmptyEnergyDisablesMotorWithoutDisablingGroundChecks()
        {
            Rig rig = CreateRig();
            var energy = rig.Root.AddComponent<CrawlerPrototypeEnergyStore>();
            energy.Configure(100f, 0f, 0f);
            rig.Drive.SetEnergySource(energy);
            rig.Drive.SetEnergyCost(15f);
            rig.Drive.SetInput(1f, 0f);
            yield return Steps(35);
            Assert.AreEqual(2, rig.Drive.GroundedModuleCount);
            Assert.Less(Mathf.Abs(rig.Root.transform.position.z), 0.1f);
        }

        [UnityTest]
        public IEnumerator AvailableEnergyIsConsumedDuringPropulsion()
        {
            Rig rig = CreateRig();
            var energy = rig.Root.AddComponent<CrawlerPrototypeEnergyStore>();
            energy.Configure(100f, 0f, 40f);
            rig.Drive.SetEnergySource(energy);
            rig.Drive.SetEnergyCost(10f);
            rig.Drive.SetInput(1f, 0f);
            yield return Steps(30);
            Assert.Less(energy.CurrentEnergy, 40f);
            Assert.Greater(rig.Root.transform.position.z, 0.1f);
        }

        [UnityTest]
        public IEnumerator BrakingReducesSpeed()
        {
            Rig rig = CreateRig();
            rig.Drive.SetInput(1f, 0f);
            yield return Steps(22);
            rig.Drive.SetInput(0f, 0f);
            float initial = rig.Body.linearVelocity.magnitude;
            rig.Drive.SetBrake(true);
            yield return Steps(15);
            Assert.Less(rig.Body.linearVelocity.magnitude, initial * 0.7f);
        }

        [UnityTest]
        public IEnumerator HeavyChassisAcceleratesSlowerWithSameTwoMotors()
        {
            Rig light = CreateRig(true, 250f);
            // Do not create a second floor; both vehicles share first one.
            GameObject heavyRoot = Make("HeavyVehicle");
            heavyRoot.transform.position = new Vector3(6f, 1f, 0f);
            heavyRoot.AddComponent<BoxCollider>().size = new Vector3(2f, 1f, 3f);
            var heavyBody = heavyRoot.AddComponent<Rigidbody>();
            heavyBody.mass = 6000f;
            var heavyDrive = heavyRoot.AddComponent<CrawlerTrackController>();
            AddTrack(heavyRoot, CrawlerTrackSide.Left, -0.92f);
            AddTrack(heavyRoot, CrawlerTrackSide.Right, 0.92f);
            heavyDrive.RefreshModules();
            Physics.SyncTransforms();
            light.Drive.SetInput(1f, 0f);
            heavyDrive.SetInput(1f, 0f);
            yield return Steps(24);
            Assert.Greater(light.Root.transform.position.z, heavyRoot.transform.position.z + 0.05f);
        }

        [UnityTest]
        public IEnumerator DisabledTrackCreatesAsymmetricMotion()
        {
            Rig rig = CreateRig();
            rig.Right.SetOperational(false);
            rig.Drive.SetInput(1f, 0f);
            yield return Steps(12);
            Assert.Greater(Mathf.Abs(rig.Body.angularVelocity.y), 0.05f);
        }

        [UnityTest]
        public IEnumerator CanRecoverAfterDisabledModuleIsReenabled()
        {
            Rig rig = CreateRig();
            rig.Left.SetOperational(false);
            rig.Right.SetOperational(false);
            rig.Drive.SetInput(1f, 0f);
            yield return Steps(10);
            Assert.AreEqual(0, rig.Drive.GroundedModuleCount);
            rig.Left.SetOperational(true);
            rig.Right.SetOperational(true);
            yield return Steps(10);
            Assert.AreEqual(2, rig.Drive.GroundedModuleCount);
            Assert.Greater(rig.Body.linearVelocity.z, 0.1f);
        }
    }
}
