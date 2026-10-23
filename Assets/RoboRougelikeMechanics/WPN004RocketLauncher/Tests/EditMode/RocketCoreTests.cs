using NUnit.Framework;
using UnityEngine;
using RoboRougelike.Mechanics.Rockets;

namespace RoboRougelike.WPN004.Tests.EditMode
{
    public sealed class RocketCoreTests
    {
        [TestCase(12f, 1f, 12f)]
        [TestCase(0f, 2f, 2f)]
        [TestCase(-5f, 7f, 7f)]
        [TestCase(float.NaN, 8f, 8f)]
        [TestCase(float.PositiveInfinity, 4f, 4f)]
        public void PositiveRejectsInvalid(float value, float fallback, float expected)
        {
            Assert.AreEqual(expected, RocketMath.Positive(value, fallback), .0001f);
        }

        [TestCase(-1f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(2f, 2f)]
        [TestCase(float.NaN, 0f)]
        [TestCase(float.PositiveInfinity, 0f)]
        public void NonNegativeRejectsInvalid(float value, float expected)
        {
            Assert.AreEqual(expected, RocketMath.NonNegative(value), .0001f);
        }

        [TestCase(10f, 20f, 40f, .5f, 20f)]
        [TestCase(30f, 20f, 40f, 1f, 40f)]
        [TestCase(10f, -5f, 40f, 1f, 10f)]
        [TestCase(10f, 15f, 40f, -1f, 10f)]
        public void SpeedIsClamped(float speed, float accel, float max, float dt, float expected)
        {
            Assert.AreEqual(expected, RocketMath.SpeedAfter(speed, accel, max, dt), .0001f);
        }

        [TestCase(0f, 3f, 100f)]
        [TestCase(1.5f, 3f, 60f)]
        [TestCase(3f, 3f, 20f)]
        [TestCase(3.1f, 3f, 0f)]
        [TestCase(-1f, 3f, 0f)]
        public void SplashLinearFalloff(float distance, float radius, float expected)
        {
            Assert.AreEqual(expected, RocketMath.BlastDamage(distance, radius, 100f, .2f), .0001f);
        }

        [Test]
        public void BlastZeroRadiusNoDamage() =>
            Assert.AreEqual(0f, RocketMath.BlastDamage(0f, 0f, 100f, .2f));

        [Test]
        public void BlastNegativeDamageNoDamage() =>
            Assert.AreEqual(0f, RocketMath.BlastDamage(0f, 3f, -100f, .2f));

        [Test]
        public void SteerZeroTurnRateDoesNotChangeForward() =>
            Assert.Less(Vector3.Distance(Vector3.forward, RocketMath.Steer(Vector3.forward, Vector3.right, 0f, 1f)), .0001f);

        [Test]
        public void SteerLimitedTurnAngle()
        {
            Vector3 result = RocketMath.Steer(Vector3.forward, Vector3.right, 45f, 1f);
            Assert.AreEqual(45f, Vector3.Angle(Vector3.forward, result), .02f);
        }

        [Test]
        public void SteerZeroTargetKeepsForward() =>
            Assert.AreEqual(Vector3.forward, RocketMath.Steer(Vector3.forward, Vector3.zero, 45f, 1f));

        [Test]
        public void FireGateStartsReady()
        {
            Assert.IsTrue(new RocketFireGate().Ready(0f));
        }

        [Test]
        public void FireGateBlocksUntilCooldownExpires()
        {
            var gate = new RocketFireGate();
            gate.Commit(10f, 2f);
            Assert.IsFalse(gate.Ready(11.99f));
            Assert.IsTrue(gate.Ready(12f));
        }

        [Test]
        public void FireGateIgnoresNegativeCooldown()
        {
            var gate = new RocketFireGate();
            gate.Commit(10f, -3f);
            Assert.IsTrue(gate.Ready(10f));
        }

        [Test]
        public void FireGateResetRestoresReadiness()
        {
            var gate = new RocketFireGate();
            gate.Commit(10f, 15f);
            gate.Reset();
            Assert.IsTrue(gate.Ready(0f));
        }

        [Test]
        public void SnapshotDefaultsProduceValidParameters()
        {
            var s = new RocketConfig().Snapshot();
            Assert.Greater(s.InitialSpeed, 0f);
            Assert.GreaterOrEqual(s.MaxSpeed, s.InitialSpeed);
            Assert.Greater(s.Radius, 0f);
            Assert.Greater(s.MaxCastHits, 0);
        }

        [Test]
        public void SnapshotSanitizesDangerousParameters()
        {
            var c = new RocketConfig { initialSpeed = -10f, maxSpeed = -100f,
                damageAtCenter = -40f, edgeDamageFraction = 5f,
                ammoCost = -10, maxBlastColliders = 2, maxCastHits = 2 };
            var s = c.Snapshot();
            Assert.Greater(s.InitialSpeed, 0f);
            Assert.GreaterOrEqual(s.MaxSpeed, s.InitialSpeed);
            Assert.AreEqual(0f, s.CenterDamage);
            Assert.AreEqual(1f, s.EdgeDamageFraction);
            Assert.AreEqual(0, s.AmmoCost);
            Assert.GreaterOrEqual(s.MaxBlastColliders, 8);
            Assert.GreaterOrEqual(s.MaxCastHits, 8);
        }

        [Test]
        public void SnapshotIsImmutableWhenConfigChanges()
        {
            var c = new RocketConfig { damageAtCenter = 30f };
            var s = c.Snapshot();
            c.damageAtCenter = 500f;
            Assert.AreEqual(30f, s.CenterDamage);
        }

        [Test]
        public void SnapshotPreservesGuidanceAndMasks()
        {
            var c = new RocketConfig { guidanceDegreesPerSecond = 75f, impactMask = 1 << 7 };
            var s = c.Snapshot();
            Assert.AreEqual(75f, s.GuidanceDegreesPerSecond);
            Assert.AreEqual(1 << 7, s.ImpactMask);
        }

        [Test]
        public void AmmoMagazineSpendAndRefund()
        {
            var go = new GameObject("ammo");
            try
            {
                var magazine = go.AddComponent<RocketAmmoMagazine>();
                magazine.SetRounds(3);
                Assert.IsTrue(magazine.TrySpendAmmo(2));
                Assert.AreEqual(1, magazine.Rounds);
                magazine.RefundAmmo(1);
                Assert.AreEqual(2, magazine.Rounds);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void AmmoMagazineRejectsOverdraft()
        {
            var go = new GameObject("ammo");
            try
            {
                var magazine = go.AddComponent<RocketAmmoMagazine>();
                magazine.SetRounds(1);
                Assert.IsFalse(magazine.TrySpendAmmo(2));
                Assert.AreEqual(1, magazine.Rounds);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void EnergyStoreSpendAndRefund()
        {
            var go = new GameObject("energy");
            try
            {
                var source = go.AddComponent<RocketEnergyStore>();
                source.SetStored(4);
                Assert.IsTrue(source.TrySpendEnergy(3f));
                Assert.AreEqual(1f, source.Stored);
                source.RefundEnergy(2f);
                Assert.AreEqual(3f, source.Stored);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void EnergyStoreRejectsInsufficientCharge()
        {
            var go = new GameObject("energy");
            try
            {
                var source = go.AddComponent<RocketEnergyStore>();
                source.SetStored(1f);
                Assert.IsFalse(source.TrySpendEnergy(2f));
                Assert.AreEqual(1f, source.Stored);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void EnergyStoreRejectsNaNAndNegativeCharge()
        {
            var go = new GameObject("energy");
            try
            {
                var source = go.AddComponent<RocketEnergyStore>();
                source.SetStored(float.NaN);
                Assert.AreEqual(0f, source.Stored);
                Assert.IsFalse(source.TrySpendEnergy(float.NaN));
                Assert.IsFalse(source.TrySpendEnergy(-1f));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
