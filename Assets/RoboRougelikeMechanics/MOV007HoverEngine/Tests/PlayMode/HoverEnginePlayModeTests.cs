using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.Mechanics.MOV007;

namespace RoboRougelike.Mechanics.MOV007.Tests
{
    public sealed class HoverEnginePlayModeTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();
        private float _previousFixedDelta;
        private Vector3 _previousGravity;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousGravity = Physics.gravity;
            _previousFixedDelta = Time.fixedDeltaTime;
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
            Time.fixedDeltaTime = 0.02f;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var go in _created) if (go != null) Object.Destroy(go);
            _created.Clear();
            Physics.gravity = _previousGravity;
            Time.fixedDeltaTime = _previousFixedDelta;
            yield return null;
        }

        private void Floor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Static floor";
            floor.transform.localScale = new Vector3(200f, 1f, 200f);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            _created.Add(floor);
        }

        private HoverVehicleController Vehicle(float y = 1.4f, int pods = 1)
        {
            var car = GameObject.CreatePrimitive(PrimitiveType.Cube);
            car.name = "Test vehicle";
            car.transform.position = new Vector3(0f, y, 0f);
            _created.Add(car);
            var rb = car.AddComponent<Rigidbody>();
            rb.mass = 120f;
            rb.useGravity = true;
            rb.linearDamping = 0.02f;
            rb.angularDamping = 0.3f;
            for (int i = 0; i < pods; i++)
            {
                var child = new GameObject("Hover pod " + i);
                child.transform.SetParent(car.transform, false);
                child.transform.localPosition = pods > 1
                    ? new Vector3((i % 2 == 0 ? -0.55f : 0.55f), 0f, (i / 2 == 0 ? 0.55f : -0.55f))
                    : Vector3.zero;
                var pod = child.AddComponent<HoverLiftPod>();
                pod.targetHeightMeters = 1.4f;
                pod.nominalLoadKg = 180f;
            }
            var controller = car.AddComponent<HoverVehicleController>();
            controller.RefreshModules();
            Physics.SyncTransforms();
            return controller;
        }

        private static IEnumerator Steps(int count)
        {
            for (int i = 0; i < count; i++) yield return new WaitForFixedUpdate();
        }

        [UnityTest] public IEnumerator HoverEngineProvidesLiftAgainstGravity()
        {
            Floor();
            var ctrl = Vehicle(1.4f);
            yield return Steps(100);
            Assert.Greater(ctrl.Body.position.y, 0.85f);
            Assert.Greater(ctrl.LastSupportedPodCount, 0);
        }

        [UnityTest] public IEnumerator WithoutHoverModuleGravityDropsVehicle()
        {
            Floor();
            var ctrl = Vehicle(1.4f, 0);
            yield return Steps(100);
            Assert.Less(ctrl.Body.position.y, 0.75f);
            Assert.AreEqual(0, ctrl.LastSupportedPodCount);
        }

        [UnityTest] public IEnumerator HoverModeOffDoesNotApplyLift()
        {
            Floor();
            var ctrl = Vehicle(1.4f);
            ctrl.SetHoverMode(false);
            yield return Steps(90);
            Assert.Less(ctrl.Body.position.y, 0.8f);
        }

        [UnityTest] public IEnumerator StrafeInputMovesBodyRight()
        {
            Floor();
            var ctrl = Vehicle();
            ctrl.SetMoveInput(Vector2.right);
            yield return Steps(90);
            Assert.Greater(ctrl.Body.position.x, 0.2f);
        }

        [UnityTest] public IEnumerator YawInputTurnsBody()
        {
            Floor();
            var ctrl = Vehicle();
            ctrl.SetTurnInput(1f);
            yield return Steps(50);
            Assert.Greater(Mathf.Abs(Vector3.Dot(ctrl.Body.angularVelocity, Vector3.up)), 0.02f);
        }

        [UnityTest] public IEnumerator NoGroundMeansNoLiftOrHorizontalDrive()
        {
            var ctrl = Vehicle(15f);
            ctrl.SetMoveInput(Vector2.right);
            yield return Steps(15);
            Assert.AreEqual(0, ctrl.LastSupportedPodCount);
            Assert.Less(Mathf.Abs(ctrl.Body.position.x), 0.1f);
        }

        [UnityTest] public IEnumerator DisablingPodRemovesItsLift()
        {
            Floor();
            var ctrl = Vehicle();
            yield return Steps(65);
            Assert.Greater(ctrl.LastSupportedPodCount, 0);
            var pod = ctrl.GetComponentInChildren<HoverLiftPod>();
            pod.SetOperational(false);
            yield return Steps(65);
            Assert.AreEqual(0, ctrl.LastSupportedPodCount);
            Assert.Less(ctrl.Body.position.y, 0.85f);
        }

        [UnityTest] public IEnumerator MultiPodSupportIsRegistered()
        {
            Floor();
            var ctrl = Vehicle(1.4f, 4);
            yield return Steps(40);
            Assert.AreEqual(4, ctrl.RegisteredPodCount);
            Assert.GreaterOrEqual(ctrl.LastSupportedPodCount, 2);
        }

        [UnityTest] public IEnumerator OwnBodyColliderDoesNotBlockGroundRay()
        {
            Floor();
            var ctrl = Vehicle();
            var pod = ctrl.GetComponentInChildren<HoverLiftPod>();
            yield return Steps(12);
            Assert.True(pod.HasGroundContact);
            Assert.That(pod.LastMeasuredGroundDistance, Is.GreaterThan(0.4f));
        }

        [UnityTest] public IEnumerator DynamicVehicleBelowIsNotSupportSurfaceByDefault()
        {
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _created.Add(platform);
            platform.transform.position = new Vector3(0f, 0f, 0f);
            platform.transform.localScale = new Vector3(40f, 1f, 40f);
            var kinBody = platform.AddComponent<Rigidbody>();
            kinBody.isKinematic = true;
            var ctrl = Vehicle(2f);
            Physics.SyncTransforms();
            yield return Steps(4);
            Assert.AreEqual(0, ctrl.LastSupportedPodCount);
        }

        [UnityTest] public IEnumerator EnergyStarvationDisablesHoverLift()
        {
            Floor();
            var ctrl = Vehicle();
            var pod = ctrl.GetComponentInChildren<HoverLiftPod>();
            var energy = ctrl.gameObject.AddComponent<HoverPrototypeEnergyTank>();
            energy.capacity = 5f;
            energy.regenPerSecond = 0f;
            energy.SetEnergyForTest(0f);
            pod.requireEnergy = true;
            pod.energySourceBehaviour = energy;
            yield return Steps(60);
            Assert.AreEqual(0, ctrl.LastSupportedPodCount);
            Assert.Less(ctrl.Body.position.y, 0.85f);
        }

        [UnityTest] public IEnumerator PoweredPodConsumesEnergy()
        {
            Floor();
            var ctrl = Vehicle();
            var pod = ctrl.GetComponentInChildren<HoverLiftPod>();
            var energy = ctrl.gameObject.AddComponent<HoverPrototypeEnergyTank>();
            energy.capacity = 50f;
            energy.regenPerSecond = 0f;
            energy.SetEnergyForTest(40f);
            pod.requireEnergy = true;
            pod.energyPerSecond = 10f;
            pod.energySourceBehaviour = energy;
            yield return Steps(35);
            Assert.Less(energy.CurrentEnergy, 39f);
            Assert.Greater(energy.CurrentEnergy, 30f);
        }

        [UnityTest] public IEnumerator DetachedPodOnNestedRigidbodyCannotPowerParent()
        {
            Floor();
            var ctrl = Vehicle();
            var pod = ctrl.GetComponentInChildren<HoverLiftPod>();
            pod.gameObject.AddComponent<Rigidbody>();
            yield return Steps(5);
            Assert.False(pod.IsAvailableFor(ctrl.Body));
            Assert.AreEqual(0, ctrl.LastSupportedPodCount);
        }

        [UnityTest] public IEnumerator ChangingPodCountCanBeRefreshed()
        {
            Floor();
            var ctrl = Vehicle();
            Assert.AreEqual(1, ctrl.RegisteredPodCount);
            var child = new GameObject("Another pod");
            child.transform.SetParent(ctrl.transform, false);
            child.AddComponent<HoverLiftPod>();
            ctrl.RefreshModules();
            yield return Steps(2);
            Assert.AreEqual(2, ctrl.RegisteredPodCount);
        }
    }
}
