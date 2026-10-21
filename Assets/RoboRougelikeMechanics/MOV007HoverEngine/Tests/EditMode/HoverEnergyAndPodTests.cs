using NUnit.Framework;
using UnityEngine;
using RoboRougelike.Mechanics.MOV007;

namespace RoboRougelike.Mechanics.MOV007.Tests
{
    public sealed class HoverEnergyAndPodTests
    {
        [Test] public void InsufficientEnergyIsRejectedWithoutNegativeBalance()
        {
            var go = new GameObject("energy");
            try
            {
                var tank = go.AddComponent<HoverPrototypeEnergyTank>();
                tank.capacity = 50f;
                tank.SetEnergyForTest(5f);
                Assert.False(tank.TrySpend(10f));
                Assert.That(tank.CurrentEnergy, Is.EqualTo(5f));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void SuccessfulEnergyDebitChangesBalance()
        {
            var go = new GameObject("energy");
            try
            {
                var tank = go.AddComponent<HoverPrototypeEnergyTank>();
                tank.capacity = 50f;
                tank.SetEnergyForTest(12f);
                Assert.True(tank.TrySpend(2f));
                Assert.That(tank.CurrentEnergy, Is.EqualTo(10f));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void EngineMustBelongToSelectedRigidbody()
        {
            var vehicle = new GameObject("vehicle");
            var foreign = new GameObject("foreign");
            try
            {
                var body = vehicle.AddComponent<Rigidbody>();
                var other = foreign.AddComponent<Rigidbody>();
                var podGO = new GameObject("pod");
                podGO.transform.SetParent(vehicle.transform);
                var pod = podGO.AddComponent<HoverLiftPod>();
                Assert.True(pod.IsAvailableFor(body));
                Assert.False(pod.IsAvailableFor(other));
                pod.SetOperational(false);
                Assert.False(pod.IsAvailableFor(body));
            }
            finally { Object.DestroyImmediate(vehicle); Object.DestroyImmediate(foreign); }
        }
        [Test] public void EnergyRequiredWithoutSourceDisablesEngine()
        {
            var vehicle = new GameObject("vehicle");
            try
            {
                var body = vehicle.AddComponent<Rigidbody>();
                var pod = vehicle.AddComponent<HoverLiftPod>();
                pod.requireEnergy = true;
                Assert.False(pod.IsAvailableFor(body));
            }
            finally { Object.DestroyImmediate(vehicle); }
        }
        [Test] public void MissingEnergySourceIsAllowedIfEnergyRequirementDisabled()
        {
            var vehicle = new GameObject("vehicle");
            try
            {
                var body = vehicle.AddComponent<Rigidbody>();
                var pod = vehicle.AddComponent<HoverLiftPod>();
                Assert.True(pod.IsAvailableFor(body));
            }
            finally { Object.DestroyImmediate(vehicle); }
        }
    }
}
