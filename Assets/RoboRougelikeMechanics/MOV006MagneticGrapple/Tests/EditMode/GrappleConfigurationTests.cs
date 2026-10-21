using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Mechanics.MOV006MagneticGrapple.Tests
{
    public sealed class GrappleConfigurationTests
    {
        private GameObject go;
        [TearDown] public void Cleanup() { if (go != null) Object.DestroyImmediate(go); }

        [Test] public void ClampNegativeTuning()
        {
            var t = new GrappleTuning
            {
                maxRange = -1f, maxStretchDistance = -3f, minRopeLength = -5f,
                reelSpeed = -1f, cooldownSeconds = -2f, maximumVehicleAcceleration = -2f,
                maximumAttachmentSeconds = -2f, activationEnergy = -10f
            };
            t.Sanitize();
            Assert.GreaterOrEqual(t.maxRange, 1f);
            Assert.GreaterOrEqual(t.maxStretchDistance, t.maxRange);
            Assert.GreaterOrEqual(t.minRopeLength, 0.1f);
            Assert.AreEqual(0f, t.reelSpeed);
            Assert.AreEqual(0f, t.cooldownSeconds);
            Assert.AreEqual(0f, t.maximumVehicleAcceleration);
            Assert.AreEqual(0f, t.maximumAttachmentSeconds);
            Assert.AreEqual(0f, t.activationEnergy);
        }
        [Test] public void EnergyIsDeductedExactlyOnceOnSuccessfulSpend()
        {
            go = new GameObject("energy");
            var store = go.AddComponent<PrototypeGrappleEnergyTank>();
            store.SetEnergy(45f);
            Assert.IsTrue(store.TrySpend(20f));
            Assert.AreEqual(25f, store.CurrentEnergy, 0.001f);
        }
        [Test] public void InsufficientEnergyDoesNotSpendPartially()
        {
            go = new GameObject("energy");
            var store = go.AddComponent<PrototypeGrappleEnergyTank>();
            store.SetEnergy(10f);
            Assert.IsFalse(store.TrySpend(20f));
            Assert.AreEqual(10f, store.CurrentEnergy, 0.001f);
        }
        [Test] public void NegativeEnergyCostRejected()
        {
            go = new GameObject("energy");
            var store = go.AddComponent<PrototypeGrappleEnergyTank>();
            store.SetEnergy(12f);
            Assert.IsFalse(store.TrySpend(-1f));
            Assert.AreEqual(12f, store.CurrentEnergy);
        }
        [Test] public void EmitterOperationalStateIsExplicit()
        {
            go = new GameObject("emitter");
            var emitter = go.AddComponent<MagneticGrappleEmitter>();
            Assert.IsTrue(emitter.IsOperational);
            emitter.SetOperational(false);
            Assert.IsFalse(emitter.IsOperational);
            emitter.SetOperational(true);
            Assert.IsTrue(emitter.IsOperational);
        }
        [Test] public void SurfaceAcceptFlagCanBeChanged()
        {
            go = new GameObject("surface");
            var surface = go.AddComponent<MagneticGrappleSurface>();
            Assert.IsTrue(surface.AcceptsGrapple);
            surface.SetAcceptsGrapple(false);
            Assert.IsFalse(surface.AcceptsGrapple);
        }
    }
}
