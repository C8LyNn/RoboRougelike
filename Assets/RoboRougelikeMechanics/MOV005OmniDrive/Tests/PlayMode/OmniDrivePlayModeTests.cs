using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.Mechanics.MOV005OmniDrive;

namespace RoboRougelike.Mechanics.MOV005OmniDrive.Tests
{
    public sealed class OmniDrivePlayModeTests
    {
        private GameObject root;
        private GameObject moduleObject;
        private GameObject ground;
        private Rigidbody body;
        private OmniDriveController controller;
        private OmniDriveModule module;

        [SetUp]
        public void Setup()
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "MOV005_ground";
            ground.transform.position = new Vector3(0, -0.55f, 0);
            ground.transform.localScale = new Vector3(300, 1, 300);
            root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "MOV005_vehicle";
            root.transform.position = new Vector3(0, 0.65f, 0);
            body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.mass = 150f;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ |
                RigidbodyConstraints.FreezePositionY;
            moduleObject = new GameObject("MOV005_drive");
            moduleObject.transform.SetParent(root.transform, false);
            moduleObject.transform.localPosition = new Vector3(0, -0.53f, 0);
            module = moduleObject.AddComponent<OmniDriveModule>();
            module.Configure(10f, 15f, 105f, 10f);
            module.GroundProbe.Configure(true, 0.55f, 1.0f, 0.12f);
            controller = root.AddComponent<OmniDriveController>();
            controller.SetCommandTimeout(0f);
            controller.RebuildModules();
        }
        [TearDown]
        public void Cleanup()
        {
            if (moduleObject != null) Object.DestroyImmediate(moduleObject);
            if (root != null) Object.DestroyImmediate(root);
            if (ground != null) Object.DestroyImmediate(ground);
        }
        private static IEnumerator PhysicsSteps(int n)
        {
            for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate();
        }
        [UnityTest] public IEnumerator RightStrafeChangesVelocityWithoutRotating()
        {
            controller.SetInput(new Vector2(1, 0), 0);
            yield return PhysicsSteps(14);
            Assert.Greater(body.linearVelocity.x, 0.1f);
            Assert.Less(Mathf.Abs(body.linearVelocity.z), 0.05f);
            Assert.Less(Mathf.Abs(body.rotation.eulerAngles.y), 0.05f);
        }
        [UnityTest] public IEnumerator ForwardDriveAccelerates()
        {
            controller.SetInput(new Vector2(0, 1), 0);
            yield return PhysicsSteps(14);
            Assert.Greater(body.linearVelocity.z, 0.1f);
        }
        [UnityTest] public IEnumerator DiagonalInputDoesNotExceedLinearCap()
        {
            controller.SetInput(new Vector2(1, 1), 0);
            yield return PhysicsSteps(2);
            Assert.LessOrEqual(controller.LastLinearAcceleration.magnitude, 15.01f);
        }
        [UnityTest] public IEnumerator HeavierVehicleReceivesLessLinearAcceleration()
        {
            body.mass = 300f;
            controller.SetInput(Vector2.right, 0);
            yield return PhysicsSteps(2);
            Assert.Greater(controller.LastLinearAcceleration.x, 5f);
            Assert.Less(controller.LastLinearAcceleration.x, 8f);
        }
        [UnityTest] public IEnumerator YawWithoutTravelTurnsChassis()
        {
            controller.SetInput(Vector2.zero, 1);
            yield return PhysicsSteps(20);
            Assert.Greater(Vector3.Dot(body.angularVelocity, Vector3.up), 0.01f);
        }
        [UnityTest] public IEnumerator DisabledPartCannotMoveVehicle()
        {
            module.SetOperational(false);
            controller.SetInput(Vector2.right, 0);
            yield return PhysicsSteps(5);
            Assert.AreEqual(Vector3.zero, controller.LastLinearAcceleration);
        }
        [UnityTest] public IEnumerator DetachedPartDoesNotProvidePropulsion()
        {
            moduleObject.transform.SetParent(null, true);
            controller.SetInput(Vector2.right, 0);
            yield return PhysicsSteps(5);
            Assert.AreEqual(Vector3.zero, controller.LastLinearAcceleration);
        }
        [UnityTest] public IEnumerator UngroundedModuleHasNoThrust()
        {
            module.GroundProbe.Configure(true, 0.1f, 0.1f, 0.05f);
            controller.SetInput(Vector2.right, 0);
            yield return PhysicsSteps(5);
            Assert.AreEqual(Vector3.zero, controller.LastLinearAcceleration);
        }
        [UnityTest] public IEnumerator KinematicBodyReceivesNoDrive()
        {
            body.isKinematic = true;
            controller.SetInput(Vector2.right, 0);
            yield return PhysicsSteps(5);
            Assert.AreEqual(Vector3.zero, controller.LastLinearAcceleration);
        }
        [UnityTest] public IEnumerator PositiveEnergyCostWithoutSourceFailsClosed()
        {
            module.Configure(10, 15, 105, 10, 5);
            controller.SetInput(Vector2.right, 0);
            yield return PhysicsSteps(5);
            Assert.AreEqual(Vector3.zero, controller.LastLinearAcceleration);
        }
        [UnityTest] public IEnumerator WithSourceConsumesEnergyOncePerStep()
        {
            module.Configure(10, 15, 105, 10, 5);
            OmniPrototypeEnergyStore store = root.AddComponent<OmniPrototypeEnergyStore>();
            store.Configure(10, 10, 0);
            controller.SetEnergySource(store);
            controller.SetInput(Vector2.right, 0);
            float before = store.CurrentEnergy;
            yield return PhysicsSteps(5);
            Assert.Less(store.CurrentEnergy, before);
            Assert.Greater(body.linearVelocity.x, 0);
        }
        [UnityTest] public IEnumerator MultipleModulesDoNotMultiplyTopSpeed()
        {
            GameObject m2 = new GameObject("MOV005_second_module");
            m2.transform.SetParent(root.transform, false);
            m2.transform.localPosition = new Vector3(0, -0.53f, 0);
            OmniDriveModule second = m2.AddComponent<OmniDriveModule>();
            second.Configure(10f, 15f, 105f, 10f);
            second.GroundProbe.Configure(false);
            controller.RebuildModules();
            Assert.AreEqual(2, controller.ModuleCount);
            controller.SetInput(Vector2.right, 0);
            yield return PhysicsSteps(90);
            Assert.Less(body.linearVelocity.x, 11f);
            Object.DestroyImmediate(m2);
        }
        [UnityTest] public IEnumerator NoNewInputEventuallyTimesOut()
        {
            controller.SetCommandTimeout(0.1f);
            controller.SetInput(Vector2.right, 0);
            yield return PhysicsSteps(20);
            Assert.AreEqual(Vector3.zero, controller.LastLinearAcceleration);
        }
        [UnityTest] public IEnumerator CoastingDoesNotStopAnExternalImpulse()
        {
            controller.SetInput(Vector2.zero, 0);
            body.linearVelocity = Vector3.forward * 8f;
            yield return PhysicsSteps(3);
            Assert.AreEqual(Vector3.zero, controller.LastLinearAcceleration);
        }
    }
}
