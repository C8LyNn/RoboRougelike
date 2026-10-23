using NUnit.Framework;
using RoboRougelike.WPN002Railgun;

namespace RoboRougelike.WPN002Railgun.Tests.EditMode
{
    public sealed class RailgunConfigAndGateTests
    {
        [Test] public void DefaultHasPositiveFlightSpeed() => Assert.That(new RailgunConfig().Snapshot().Speed, Is.GreaterThan(0));
        [Test] public void DefaultHasPositiveRange() => Assert.That(new RailgunConfig().Snapshot().Range, Is.GreaterThan(0));
        [Test] public void DefaultPenetratesMultipleParts() => Assert.That(new RailgunConfig().Snapshot().MaxDistinctParts, Is.GreaterThan(1));
        [Test] public void DefaultHasRecoil() => Assert.That(new RailgunConfig().Snapshot().RecoilImpulse, Is.GreaterThan(0));
        [Test] public void HugeMaximumPartCountIsCapped() => Assert.That(new RailgunConfig { maximumDistinctParts = 999 }.Snapshot().MaxDistinctParts, Is.EqualTo(24));
        [Test] public void TooSmallPartCountIsRaised() => Assert.That(new RailgunConfig { maximumDistinctParts = -1 }.Snapshot().MaxDistinctParts, Is.EqualTo(1));
        [Test] public void NanSpeedUsesFallback() => Assert.That(new RailgunConfig { projectileSpeed = float.NaN }.Snapshot().Speed, Is.EqualTo(85f));
        [Test] public void NegativeEnergyIsClamped() => Assert.That(new RailgunConfig { initialPenetrationEnergy = -4f }.Snapshot().InitialEnergy, Is.GreaterThan(0));
        [Test] public void NegativeRecoilIsClampedToZero() => Assert.That(new RailgunConfig { recoilImpulse = -5f }.Snapshot().RecoilImpulse, Is.Zero);
        [Test] public void ZeroCooldownIsClamped() => Assert.That(new RailgunConfig { cooldownSeconds = 0f }.Snapshot().Cooldown, Is.GreaterThan(0));
        [Test] public void InitialShotIsReady() => Assert.That(new RailgunFireGate().CanFire(0f), Is.True);
        [Test] public void CommitStartsCooldown()
        { var gate = new RailgunFireGate(); Assert.That(gate.Commit(5f, 2f), Is.True); Assert.That(gate.CanFire(6f), Is.False); }
        [Test] public void CooldownExpiresExactlyAtBoundary()
        { var gate = new RailgunFireGate(); gate.Commit(5f, 2f); Assert.That(gate.CanFire(7f), Is.True); }
        [Test] public void FailedCommitDoesNotAdvanceSequence()
        { var gate = new RailgunFireGate(); gate.Commit(0f, 2f); Assert.That(gate.Commit(1f, 2f), Is.False); Assert.That(gate.SuccessfulShots, Is.EqualTo(1u)); }
        [Test] public void InactiveGateRejects()
        { var gate = new RailgunFireGate(); gate.SetOperational(false); Assert.That(gate.Commit(0f, 2f), Is.False); }
        [Test] public void ReenabledGatePreservesCooldown()
        { var gate = new RailgunFireGate(); gate.Commit(0f, 2f); gate.SetOperational(false); gate.SetOperational(true); Assert.That(gate.CanFire(1f), Is.False); }
        [Test] public void InvalidClockRejectsNaN() => Assert.That(new RailgunFireGate().CanFire(float.NaN), Is.False);
        [Test] public void InvalidClockRejectsInfinity() => Assert.That(new RailgunFireGate().CanFire(float.PositiveInfinity), Is.False);
        [Test] public void ResetClearsSequenceAndCooldown()
        { var gate = new RailgunFireGate(); gate.Commit(1f, 10f); gate.Reset(); Assert.That(gate.SuccessfulShots, Is.Zero); Assert.That(gate.CanFire(1f), Is.True); }
    }
}
