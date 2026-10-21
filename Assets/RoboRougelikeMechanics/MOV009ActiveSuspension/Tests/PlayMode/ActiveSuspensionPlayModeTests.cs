using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.Mechanics.MOV009.Tests
{
    public sealed class ActiveSuspensionPlayModeTests
    {
        private readonly List<GameObject> created = new List<GameObject>();

        private sealed class Rig
        {
            public GameObject Root;
            public Rigidbody Body;
            public ActiveSuspensionController Controller;
            public ActiveSuspensionModule[] Modules;
        }

        private void MakeGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            created.Add(ground);
            ground.name = "Static test terrain";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(100f, 1f, 100f);
            Physics.SyncTransforms();
        }

        private Rig MakeRig(float x = 0f, float bodyHeight = 0.72f, int mountCount = 4)
        {
            var go = new GameObject("Vehicle test chassis");
            created.Add(go);
            go.transform.position = new Vector3(x, bodyHeight, 0f);
            var collider = go.AddComponent<BoxCollider>();
            collider.size = new Vector3(2f, 0.5f, 2f);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 100f;
            rb.useGravity = true;
            var controller = go.AddComponent<ActiveSuspensionController>();
            controller.SetSupportMode(ActiveSuspensionSupportMode.FullSupport);
            var modules = new ActiveSuspensionModule[mountCount];
            for (int i = 0; i < mountCount; i++)
            {
                var mount = new GameObject("Support " + i);
                created.Add(mount);
                mount.transform.SetParent(go.transform, false);
                mount.transform.localPosition = new Vector3(i % 2 == 0 ? -0.7f : 0.7f, -0.35f,
                    i < 2 ? -0.65f : 0.65f);
                modules[i] = mount.AddComponent<ActiveSuspensionModule>();
                modules[i].ConfigureSpring(0.55f, 8000f, 1050f, 9000f);
            }
            Physics.SyncTransforms();
            return new Rig { Root = go, Body = rb, Controller = controller, Modules = modules };
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (GameObject go in created)
                if (go != null) Object.Destroy(go);
            created.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FullSupportAppliesRealForce()
        {
            MakeGround();
            Rig rig = MakeRig();
            yield return new WaitForFixedUpdate();
            Assert.Greater(rig.Controller.LastTotalLiftNewtons, 0f);
            Assert.IsTrue(rig.Modules[0].LastGroundContact);
        }

        [UnityTest]
        public IEnumerator AssistModeIsWeakerThanFullSupport()
        {
            MakeGround();
            Rig full = MakeRig(-10f);
            Rig assist = MakeRig(10f);
            assist.Controller.SetSupportMode(ActiveSuspensionSupportMode.AssistOnly);
            yield return new WaitForFixedUpdate();
            Assert.Greater(full.Controller.LastTotalLiftNewtons, assist.Controller.LastTotalLiftNewtons);
            Assert.Greater(assist.Controller.LastTotalLiftNewtons, 0f);
        }

        [UnityTest]
        public IEnumerator InAirHasNoGroundSupport()
        {
            MakeGround();
            Rig rig = MakeRig(bodyHeight: 5f);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f, rig.Controller.LastTotalLiftNewtons, 0.001f);
        }

        [UnityTest]
        public IEnumerator DisabledControllerGeneratesNoLift()
        {
            MakeGround();
            Rig rig = MakeRig();
            rig.Controller.SetSuspensionEnabled(false);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f, rig.Controller.LastTotalLiftNewtons);
        }

        [UnityTest]
        public IEnumerator DestroyedModuleStopsGeneratingForce()
        {
            MakeGround();
            Rig rig = MakeRig();
            rig.Modules[0].SetOperational(false);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f, rig.Modules[0].LastAppliedForceNewtons);
            Assert.Greater(rig.Modules[1].LastAppliedForceNewtons, 0f);
        }

        [UnityTest]
        public IEnumerator AllDamagedMountsProvideNoSupport()
        {
            MakeGround();
            Rig rig = MakeRig();
            foreach (var module in rig.Modules) module.SetOperational(false);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f, rig.Controller.LastTotalLiftNewtons);
        }

        [UnityTest]
        public IEnumerator DetachedModuleUnregistersAndCannotSupportVehicle()
        {
            MakeGround();
            Rig rig = MakeRig();
            rig.Modules[0].transform.SetParent(null, true);
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(rig.Modules[0].IsAvailableFor(rig.Body));
            Assert.AreEqual(3, rig.Controller.RegisteredModuleCount);
        }

        [UnityTest]
        public IEnumerator TotalLiftRespectsRigidBodyMassAndGlobalCap()
        {
            MakeGround();
            Rig rig = MakeRig();
            yield return new WaitForFixedUpdate();
            Assert.LessOrEqual(rig.Controller.LastTotalLiftNewtons, rig.Body.mass * 18f + 0.01f);
        }

        [UnityTest]
        public IEnumerator TiltedOverUpsideDownCannotSelfLaunch()
        {
            MakeGround();
            Rig rig = MakeRig();
            rig.Root.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f, rig.Controller.LastTotalLiftNewtons);
        }

        [UnityTest]
        public IEnumerator RaisingRideHeightNeedsEnergyWhenConfigured()
        {
            MakeGround();
            Rig rig = MakeRig();
            var battery = rig.Root.AddComponent<ActiveSuspensionEnergyStore>();
            battery.Configure(100f, 0f, 0f);
            rig.Controller.SetEnergySource(battery);
            rig.Controller.SetEnergyConsumption(100f);
            rig.Controller.SetRideHeightOffset(0.2f);
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f, rig.Controller.CurrentRideHeightOffsetMeters);
            battery.Configure(100f, 0f, 100f);
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            Assert.Greater(rig.Controller.CurrentRideHeightOffsetMeters, 0f);
            Assert.Less(battery.Energy, 100f);
        }

        [UnityTest]
        public IEnumerator NoBatteryRequiredForPassiveSupport()
        {
            MakeGround();
            Rig rig = MakeRig();
            rig.Controller.SetEnergyConsumption(100f);
            yield return new WaitForFixedUpdate();
            Assert.Greater(rig.Controller.LastTotalLiftNewtons, 0f);
        }

        [UnityTest]
        public IEnumerator RaiseCommandChangesEffectiveHeightOverTime()
        {
            MakeGround();
            Rig rig = MakeRig();
            rig.Controller.SetRideHeightOffset(0.25f);
            yield return new WaitForFixedUpdate();
            float first = rig.Controller.CurrentRideHeightOffsetMeters;
            for (int i = 0; i < 16; i++) yield return new WaitForFixedUpdate();
            Assert.Greater(rig.Controller.CurrentRideHeightOffsetMeters, first);
            Assert.LessOrEqual(rig.Controller.CurrentRideHeightOffsetMeters, 0.25f + 0.00001f);
        }

        [UnityTest]
        public IEnumerator MountWithSeparateRigidBodyDoesNotDriveParent()
        {
            MakeGround();
            Rig rig = MakeRig(mountCount: 1);
            var rb = rig.Modules[0].gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(rig.Modules[0].IsAvailableFor(rig.Body));
            Assert.AreEqual(0f, rig.Controller.LastTotalLiftNewtons);
        }

        [UnityTest]
        public IEnumerator TooHighSupportHasNoContact()
        {
            MakeGround();
            Rig rig = MakeRig(bodyHeight: 8f);
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(rig.Modules[0].LastGroundContact);
        }
    }
}
