using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.WPN001Shotgun.Tests.EditMode
{
    public sealed class ShotgunConfigAndGateTests
    {
        [Test] public void DefaultPelletCountIsEight() => Assert.AreEqual(8, new ShotgunConfig().Snapshot().PelletCount);
        [Test] public void PelletCountClampedAt64() => Assert.AreEqual(64, new ShotgunConfig { pelletCount = 999 }.Snapshot().PelletCount);
        [Test] public void PelletCountClampedAtOne() => Assert.AreEqual(1, new ShotgunConfig { pelletCount = -10 }.Snapshot().PelletCount);
        [Test] public void ConeAngleClampedTo90() => Assert.AreEqual(90f, new ShotgunConfig { fullConeAngleDegrees = 180f }.Snapshot().FullConeAngleDegrees);
        [Test] public void NonFiniteDamageFallsBack() => Assert.AreEqual(9f, new ShotgunConfig { damagePerPellet = float.NaN }.Snapshot().DamagePerPellet);
        [Test] public void NegativeDamageClampedToZero() => Assert.AreEqual(0f, new ShotgunConfig { damagePerPellet = -2f }.Snapshot().DamagePerPellet);
        [Test] public void CooldownNeverZero() => Assert.AreEqual(0.01f, new ShotgunConfig { cooldownSeconds = 0f }.Snapshot().CooldownSeconds);
        [Test] public void RecoilImpulseClampedToTen() => Assert.AreEqual(10f, new ShotgunConfig { recoilImpulse = 50f }.Snapshot().RecoilImpulse);
        [Test] public void VelocityInheritanceClampedToOne() => Assert.AreEqual(1f, new ShotgunConfig { inheritedVelocityFactor = 20f }.Snapshot().InheritedVelocityFactor);
        [Test] public void NegativeRangeClampedToMinimum() => Assert.AreEqual(0.1f, new ShotgunConfig { maxTravelDistance = -1f }.Snapshot().MaxTravelDistance);
        [Test] public void GateBeginsReady()
        {
            var gate = new ShotgunFireGate(); Assert.IsTrue(gate.CanFire(0f));
        }
        [Test] public void GateBlocksDuringCooldown()
        {
            var gate = new ShotgunFireGate();
            Assert.IsTrue(gate.RegisterSuccessfulVolley(1f, 0.5f));
            Assert.IsFalse(gate.CanFire(1.49f));
        }
        [Test] public void GateAllowsExactCooldownBoundary()
        {
            var gate = new ShotgunFireGate();
            gate.RegisterSuccessfulVolley(1f, 0.5f);
            Assert.IsTrue(gate.CanFire(1.5f));
        }
        [Test] public void FailedCooldownRegistrationDoesNotChangeCount()
        {
            var gate = new ShotgunFireGate();
            gate.RegisterSuccessfulVolley(1f, 2f);
            Assert.IsFalse(gate.RegisterSuccessfulVolley(2f, 2f));
            Assert.AreEqual(1u, gate.SuccessfulVolleys);
        }
        [Test] public void GateBecomesInactive()
        {
            var gate = new ShotgunFireGate(); gate.SetOperational(false);
            Assert.IsFalse(gate.CanFire(100f));
        }
        [Test] public void ReactivationPreservesCooldown()
        {
            var gate = new ShotgunFireGate(); gate.RegisterSuccessfulVolley(0f, 10f);
            gate.SetOperational(false); gate.SetOperational(true);
            Assert.IsFalse(gate.CanFire(9f));
        }
        [Test] public void GateRejectsNanAndInfinity()
        {
            var gate = new ShotgunFireGate();
            Assert.IsFalse(gate.CanFire(float.NaN));
            Assert.IsFalse(gate.CanFire(float.PositiveInfinity));
            Assert.IsFalse(gate.RegisterSuccessfulVolley(float.NegativeInfinity, 1f));
        }
        [Test] public void GateResetRestoresInitialState()
        {
            var gate = new ShotgunFireGate(); gate.RegisterSuccessfulVolley(0f, 2f);
            gate.SetOperational(false); gate.Reset();
            Assert.AreEqual(0u, gate.SuccessfulVolleys);
            Assert.IsTrue(gate.CanFire(0f));
        }
        [Test] public void GateClampsZeroCooldown()
        {
            var gate = new ShotgunFireGate(); gate.RegisterSuccessfulVolley(0f, 0f);
            Assert.IsFalse(gate.CanFire(0.001f));
            Assert.IsTrue(gate.CanFire(0.02f));
        }
    }
}
