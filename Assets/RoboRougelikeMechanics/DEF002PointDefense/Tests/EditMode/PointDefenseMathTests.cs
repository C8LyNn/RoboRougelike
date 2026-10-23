using NUnit.Framework;
using UnityEngine;
using RoboRougelike.Mechanics.PointDefense;

namespace RoboRougelike.Mechanics.PointDefense.Tests
{
    public sealed class PointDefenseMathTests
    {
        [Test]
        public void IncomingDirectThreat_IsRecognized()
        {
            float time;
            Assert.IsTrue(PointDefenseMath.IsThreatApproaching(new Vector3(0, 0, 10),
                new Vector3(0, 0, -5), Vector3.zero, Vector3.zero, 5, 2, out time));
            Assert.That(time, Is.EqualTo(2f).Within(0.0001f));
        }
        [Test]
        public void PassingOutsideProtectedRadius_IsRejected()
        {
            float time;
            Assert.IsFalse(PointDefenseMath.IsThreatApproaching(new Vector3(12, 0, 10),
                new Vector3(0, 0, -5), Vector3.zero, Vector3.zero, 5, 2, out time));
        }
        [Test]
        public void MovingAway_IsRejected()
        {
            float time;
            Assert.IsFalse(PointDefenseMath.IsThreatApproaching(new Vector3(0, 0, 10),
                new Vector3(0, 0, 5), Vector3.zero, Vector3.zero, 5, 2, out time));
        }
        [Test]
        public void FutureThreatAfterHorizon_IsRejected()
        {
            float time;
            Assert.IsFalse(PointDefenseMath.IsThreatApproaching(new Vector3(0, 0, 30),
                new Vector3(0, 0, -5), Vector3.zero, Vector3.zero, 3, 2, out time));
        }
        [Test]
        public void ThreatAlreadyInsideProtection_IsRecognized()
        {
            float time;
            Assert.IsTrue(PointDefenseMath.IsThreatApproaching(new Vector3(0, 0, 0.5f),
                Vector3.zero, Vector3.zero, Vector3.zero, 3, 2, out time));
        }
        [Test]
        public void RelativeMotion_IsConsidered()
        {
            float time;
            Assert.IsTrue(PointDefenseMath.IsThreatApproaching(new Vector3(0, 0, 10),
                new Vector3(0, 0, 1), Vector3.zero, new Vector3(0, 0, 6), 4, 1, out time));
        }
        [Test]
        public void StaticTarget_LeadMatchesCurrentPosition()
        {
            Vector3 point; float seconds;
            Assert.IsTrue(PointDefenseMath.TryLead(Vector3.zero, Vector3.zero,
                new Vector3(0, 0, 10), Vector3.zero, 20, 2, out point, out seconds));
            Assert.That(point.z, Is.EqualTo(10).Within(.0001f));
            Assert.That(seconds, Is.EqualTo(.5f).Within(.0001f));
        }
        [Test]
        public void MovingTarget_LeadIsAhead()
        {
            Vector3 point; float seconds;
            Assert.IsTrue(PointDefenseMath.TryLead(Vector3.zero, Vector3.zero,
                new Vector3(0, 0, 10), new Vector3(4, 0, 0), 25, 3, out point, out seconds));
            Assert.Greater(point.x, 0f);
            Assert.Greater(seconds, 0f);
        }
        [Test]
        public void FasterRetreatingTarget_IsImpossible()
        {
            Vector3 point; float seconds;
            Assert.IsFalse(PointDefenseMath.TryLead(Vector3.zero, Vector3.zero,
                new Vector3(0, 0, 10), new Vector3(0, 0, 25), 20, 3, out point, out seconds));
        }
        [Test]
        public void ZeroProjectileSpeed_IsRejected()
        {
            Vector3 point; float seconds;
            Assert.IsFalse(PointDefenseMath.TryLead(Vector3.zero, Vector3.zero,
                Vector3.forward, Vector3.zero, 0, 2, out point, out seconds));
        }
        [Test]
        public void ShortLifetime_RejectsLateInterception()
        {
            Vector3 point; float seconds;
            Assert.IsFalse(PointDefenseMath.TryLead(Vector3.zero, Vector3.zero,
                new Vector3(0, 0, 100), Vector3.zero, 10, 1, out point, out seconds));
        }
        [Test]
        public void InheritedShooterVelocity_IsUsedForLead()
        {
            Vector3 point; float seconds;
            Assert.IsTrue(PointDefenseMath.TryLead(Vector3.zero, new Vector3(5, 0, 0),
                new Vector3(0, 0, 12), new Vector3(5, 0, 0), 20, 2, out point, out seconds));
            Assert.That(seconds, Is.EqualTo(.6f).Within(.0001f));
        }
        [TestCase(0f, true)]
        [TestCase(2.9f, true)]
        [TestCase(5.1f, false)]
        [TestCase(45f, false)]
        public void Alignment_UsesConfiguredTolerance(float angle, bool expected)
        {
            var rotated = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Assert.AreEqual(expected, PointDefenseMath.IsAligned(Vector3.forward, rotated, 5f));
        }
        [Test]
        public void ZeroAimDirection_CannotPassAlignment()
        { Assert.IsFalse(PointDefenseMath.IsAligned(Vector3.forward, Vector3.zero, 5f)); }
        [Test]
        public void ConfigSanitization_RestoresPositiveDefaults()
        {
            var c = new PointDefenseConfiguration {
                detectionRadius = -1, scanInterval = 0, threatLookAhead = -2,
                defendedRadius = 0, interceptorSpeed = -10, shotInterval = 0,
                interceptorLifetime = -1, interceptorRadius = -1, interceptorDamage = -1,
                traverseDegreesPerSecond = -1, energyPerShot = -1, recoilImpulse = -1,
                startingAmmunition = -5, firingToleranceDegrees = -99
            };
            c.Sanitize();
            Assert.Greater(c.detectionRadius, 0); Assert.Greater(c.scanInterval, 0);
            Assert.Greater(c.threatLookAhead, 0); Assert.Greater(c.defendedRadius, 0);
            Assert.Greater(c.interceptorSpeed, 0); Assert.Greater(c.shotInterval, 0);
            Assert.Greater(c.interceptorLifetime, 0); Assert.Greater(c.interceptorRadius, 0);
            Assert.Greater(c.interceptorDamage, 0); Assert.Greater(c.traverseDegreesPerSecond, 0);
            Assert.AreEqual(0, c.energyPerShot); Assert.AreEqual(0, c.recoilImpulse);
            Assert.AreEqual(0, c.startingAmmunition); Assert.AreEqual(0, c.firingToleranceDegrees);
        }
        [Test]
        public void Sanitize_NaNIsReplaced()
        {
            var c = new PointDefenseConfiguration { shotInterval = float.NaN,
                interceptorSpeed = float.NaN, energyPerShot = float.NaN };
            c.Sanitize();
            Assert.Greater(c.shotInterval, 0); Assert.Greater(c.interceptorSpeed, 0);
            Assert.AreEqual(0f, c.energyPerShot);
        }
        [Test]
        public void ConfigurationCopy_DoesNotShareMutableReference()
        {
            var a = new PointDefenseConfiguration(); var b = a.Copy();
            b.detectionRadius = 55;
            Assert.AreNotEqual(a.detectionRadius, b.detectionRadius);
        }
    }
}
