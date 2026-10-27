using NUnit.Framework;
using UnityEngine;
using RoboRougelike.Mechanics.MOV005OmniDrive;

namespace RoboRougelike.Mechanics.MOV005OmniDrive.Tests
{
    public sealed class OmniModuleAndEnergyTests
    {
        private GameObject host;
        [SetUp] public void Setup() { host = new GameObject("MOV005_edit"); }
        [TearDown] public void TearDown() { Object.DestroyImmediate(host); }
        [Test] public void ModuleRejectsNegativeConfiguration()
        {
            OmniDriveModule m = host.AddComponent<OmniDriveModule>();
            m.Configure(-10, -5, -2, -6, -1);
            Assert.AreEqual(0f, m.MaximumSpeed);
            Assert.AreEqual(0f, m.LinearAcceleration);
            Assert.AreEqual(0f, m.YawAngularAcceleration);
            Assert.AreEqual(0f, m.EnergyCostPerSecond);
        }
        [Test] public void ModuleOperationalToggle()
        {
            OmniDriveModule m = host.AddComponent<OmniDriveModule>();
            m.SetOperational(false);
            Assert.IsFalse(m.IsOperational);
            m.SetOperational(true);
            Assert.IsTrue(m.IsOperational);
        }
        [Test] public void EnergyCannotGoNegative()
        {
            OmniPrototypeEnergyStore e = host.AddComponent<OmniPrototypeEnergyStore>();
            e.Configure(10, 3, 0);
            Assert.IsFalse(e.TrySpend(4));
            Assert.AreEqual(3f, e.CurrentEnergy);
        }
        [Test] public void SpendExactEnergy()
        {
            OmniPrototypeEnergyStore e = host.AddComponent<OmniPrototypeEnergyStore>();
            e.Configure(10, 4, 0);
            Assert.IsTrue(e.TrySpend(4));
            Assert.AreEqual(0f, e.CurrentEnergy, 0.0001f);
        }
        [Test] public void NegativeSpendRejected()
        {
            OmniPrototypeEnergyStore e = host.AddComponent<OmniPrototypeEnergyStore>();
            e.Configure(10, 5, 0);
            Assert.IsFalse(e.TrySpend(-1));
            Assert.AreEqual(5, e.CurrentEnergy, .0001f);
        }
        [Test] public void NanSpendRejected()
        {
            OmniPrototypeEnergyStore e = host.AddComponent<OmniPrototypeEnergyStore>();
            e.Configure(10, 5, 0);
            Assert.IsFalse(e.TrySpend(float.NaN));
        }
        [Test] public void GroundProbeCanBeDisabledForTestRig()
        {
            OmniGroundProbe probe = host.AddComponent<OmniGroundProbe>();
            probe.Configure(false);
            Assert.IsFalse(probe.RequireGroundContact);
        }
    }
}
