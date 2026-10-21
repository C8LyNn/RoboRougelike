using NUnit.Framework;
using UnityEngine;
using RoboRougelike.Mechanics.StructuralScan;

namespace RoboRougelike.Mechanics.StructuralScan.Tests.EditMode
{
    public class StructuralScanStateTests
    {
        [Test] public void ClockStartsReady() => Assert.IsTrue(new StructuralScanClock().Ready);
        [Test] public void ClockStartsInvisible() => Assert.IsFalse(new StructuralScanClock().Visible);
        [Test] public void ClockStartLocksCooldown() { var c = new StructuralScanClock(); c.Start(4, 2); Assert.IsFalse(c.Ready); }
        [Test] public void ClockStartShowsResults() { var c = new StructuralScanClock(); c.Start(4, 2); Assert.IsTrue(c.Visible); }
        [Test] public void CooldownDecreases() { var c = new StructuralScanClock(); c.Start(4, 2); c.Advance(1); Assert.AreEqual(3, c.CooldownLeft, .001f); }
        [Test] public void VisibilityDecreases() { var c = new StructuralScanClock(); c.Start(4, 2); c.Advance(1); Assert.AreEqual(1, c.VisibleLeft, .001f); }
        [Test] public void VisibilityEndsBeforeCooldown() { var c = new StructuralScanClock(); c.Start(8, 2); c.Advance(3); Assert.IsFalse(c.Visible); Assert.IsFalse(c.Ready); }
        [Test] public void ClockReadyAfterCooldown() { var c = new StructuralScanClock(); c.Start(8, 2); c.Advance(9); Assert.IsTrue(c.Ready); }
        [Test] public void CooldownDoesNotGoNegative() { var c = new StructuralScanClock(); c.Start(1, 1); c.Advance(100); Assert.AreEqual(0, c.CooldownLeft); }
        [Test] public void NegativeDeltaIgnored() { var c = new StructuralScanClock(); c.Start(5, 3); c.Advance(-4); Assert.AreEqual(5, c.CooldownLeft); }
        [Test] public void NaNDeltaIgnored() { var c = new StructuralScanClock(); c.Start(5, 3); c.Advance(float.NaN); Assert.AreEqual(5, c.CooldownLeft); }
        [Test] public void InfinityDeltaIgnored() { var c = new StructuralScanClock(); c.Start(5, 3); c.Advance(float.PositiveInfinity); Assert.AreEqual(5, c.CooldownLeft); }
        [Test] public void ClearVisibilityPreservesCooldown() { var c = new StructuralScanClock(); c.Start(5, 3); c.ClearVisibility(); Assert.AreEqual(5, c.CooldownLeft); Assert.IsFalse(c.Visible); }
        [Test] public void ResetClearsBoth() { var c = new StructuralScanClock(); c.Start(5, 3); c.Reset(); Assert.IsTrue(c.Ready); Assert.IsFalse(c.Visible); }
        [Test] public void SettingsClampsNegativeCooldown() { var s = new StructuralScanSettings { cooldownSeconds = -3 }; s.Validate(); Assert.AreEqual(0, s.cooldownSeconds); }
        [Test] public void SettingsClampsNegativeEnergy() { var s = new StructuralScanSettings { activationEnergy = -100 }; s.Validate(); Assert.AreEqual(0, s.activationEnergy); }
        [Test] public void SettingsRepairsNaNRange() { var s = new StructuralScanSettings { rangeMeters = float.NaN }; s.Validate(); Assert.AreEqual(24, s.rangeMeters); }
        [Test] public void SettingsRepairsInfinityTime() { var s = new StructuralScanSettings { displaySeconds = float.PositiveInfinity }; s.Validate(); Assert.AreEqual(5, s.displaySeconds); }
        [Test] public void SettingsClampsResultsCount() { var s = new StructuralScanSettings { maxRevealedParts = 10000 }; s.Validate(); Assert.AreEqual(128, s.maxRevealedParts); }
        [Test] public void SettingsClampsRegisteredCount() { var s = new StructuralScanSettings { maxRegisteredParts = -4 }; s.Validate(); Assert.AreEqual(1, s.maxRegisteredParts); }

        [Test] public void RegistryRejectsDuplicateIdentity()
        {
            var go = new GameObject("registry");
            var reg = go.AddComponent<StructuralScanRegistry>();
            var a = new GameObject("a"); a.AddComponent<BoxCollider>();
            var b = new GameObject("b"); b.AddComponent<BoxCollider>();
            a.SetActive(false); b.SetActive(false);
            var pa = a.AddComponent<StructuralScanPartProxy>();
            var pb = b.AddComponent<StructuralScanPartProxy>();
            pa.Configure(reg, 2, 2, 3, StructuralPartType.Wheel);
            pb.Configure(reg, 2, 2, 3, StructuralPartType.Weapon);
            Assert.IsTrue(reg.Register(pa));
            Assert.IsFalse(reg.Register(pb));
            Object.DestroyImmediate(a); Object.DestroyImmediate(b); Object.DestroyImmediate(go);
        }
        [Test] public void RegistryUnregisters()
        {
            var go = new GameObject("registry"); var reg = go.AddComponent<StructuralScanRegistry>();
            var p = new GameObject("part"); p.SetActive(false); p.AddComponent<BoxCollider>();
            var proxy = p.AddComponent<StructuralScanPartProxy>(); proxy.Configure(reg, 2, 2, 3, StructuralPartType.Wheel);
            Assert.IsTrue(reg.Register(proxy)); Assert.IsTrue(reg.Unregister(proxy)); Assert.AreEqual(0, reg.Count);
            Object.DestroyImmediate(p); Object.DestroyImmediate(go);
        }
        [Test] public void EnergyIsNotOverdrawn()
        {
            var go = new GameObject("energy"); var e = go.AddComponent<StructuralScanEnergyStore>(); e.Configure(10, 5);
            Assert.IsFalse(e.TrySpend(6)); Assert.AreEqual(5, e.Charge);
            Object.DestroyImmediate(go);
        }
        [Test] public void EnergyDeductsExactlyOnce()
        {
            var go = new GameObject("energy"); var e = go.AddComponent<StructuralScanEnergyStore>(); e.Configure(10, 5);
            Assert.IsTrue(e.TrySpend(3)); Assert.AreEqual(2, e.Charge);
            Object.DestroyImmediate(go);
        }
        [Test] public void NegativeEnergyChargeRejected()
        {
            var go = new GameObject("energy"); var e = go.AddComponent<StructuralScanEnergyStore>(); e.Configure(10, 5);
            Assert.IsFalse(e.TrySpend(-1)); Assert.AreEqual(5, e.Charge);
            Object.DestroyImmediate(go);
        }
        [Test] public void EnergyCapacityClamped()
        {
            var go = new GameObject("energy"); var e = go.AddComponent<StructuralScanEnergyStore>(); e.Configure(10, 25);
            Assert.AreEqual(10, e.Charge); Object.DestroyImmediate(go);
        }
    }
}
