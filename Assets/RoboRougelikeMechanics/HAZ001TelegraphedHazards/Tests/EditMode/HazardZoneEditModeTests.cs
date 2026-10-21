using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Mechanics.HAZ001.Tests
{
    public sealed class HazardZoneEditModeTests
    {
        private GameObject go;
        private TelegraphedHazardZone zone;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("HAZ001 EditMode");
            zone = go.AddComponent<TelegraphedHazardZone>();
            zone.Configure(1f, 2f, 3f, 2f, .5f, 10f, ~0);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(go);

        [Test]
        public void BeginOnlyFromIdle()
        {
            Assert.IsTrue(zone.Begin());
            Assert.IsFalse(zone.Begin());
            Assert.AreEqual(HazardPhase.Telegraph, zone.Phase);
            Assert.IsFalse(zone.IsDangerous);
        }

        [Test]
        public void TimelineCarriesOvershootAcrossPhases()
        {
            zone.Begin();
            zone.ManualTick(1.5f);
            Assert.AreEqual(HazardPhase.Active, zone.Phase);
            Assert.AreEqual(.5f, zone.PhaseElapsed, .001f);
            zone.ManualTick(1.5f);
            Assert.AreEqual(HazardPhase.Cooldown, zone.Phase);
            zone.ManualTick(3f);
            Assert.AreEqual(HazardPhase.Idle, zone.Phase);
        }

        [Test]
        public void HugeStepCompletesExactlyOneCycle()
        {
            int transitions = 0;
            zone.PhaseChanged += _ => transitions++;
            zone.Begin();
            zone.ManualTick(999f);
            Assert.AreEqual(HazardPhase.Idle, zone.Phase);
            Assert.AreEqual(4, transitions);
        }

        [Test]
        public void InvalidTimeDoesNotAdvance()
        {
            zone.Begin();
            zone.ManualTick(float.NaN);
            zone.ManualTick(float.PositiveInfinity);
            zone.ManualTick(-100f);
            Assert.AreEqual(0f, zone.PhaseElapsed);
        }

        [Test]
        public void ResetCancelsAndPermitsRestart()
        {
            zone.Begin();
            zone.ManualTick(1f);
            zone.ResetCycle();
            Assert.AreEqual(HazardPhase.Idle, zone.Phase);
            Assert.IsTrue(zone.Begin());
        }

        [Test]
        public void DisabledZoneCannotBegin()
        {
            go.SetActive(false);
            Assert.IsFalse(zone.Begin());
            Assert.AreEqual(HazardPhase.Idle, zone.Phase);
        }

        [Test]
        public void ConfigureSanitizesInvalidValues()
        {
            zone.Configure(float.NaN, -1f, float.PositiveInfinity, -2f,
                0f, float.NaN, ~0);
            Assert.GreaterOrEqual(zone.Radius, .1f);
            zone.Begin();
            zone.ManualTick(.05f);
            Assert.AreEqual(HazardPhase.Active, zone.Phase);
        }

        [Test]
        public void NeverRequestsDamageOutsideActive()
        {
            zone.Begin();
            Assert.AreEqual(0, zone.ScanAtTime(0f));
            zone.ManualTick(1f);
            zone.ManualTick(2f);
            Assert.AreEqual(HazardPhase.Cooldown, zone.Phase);
            Assert.AreEqual(0, zone.ScanAtTime(1f));
        }
    }
}
