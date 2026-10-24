using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.VFX001.Tests
{
    public class VfxResolverTests
    {
        [TestCase(VfxEventKind.Hit)]
        [TestCase(VfxEventKind.Detached)]
        [TestCase(VfxEventKind.Explosion)]
        [TestCase(VfxEventKind.CoreDestroyed)]
        [TestCase(VfxEventKind.LandingDust)]
        public void EveryKind_ProducesFiniteNonNegativeValues(VfxEventKind kind)
        {
            var s = VfxPresetResolver.Resolve(new VfxEvent(kind, Vector3.zero, Vector3.up, ImpactMaterialType.Metal, .5f));
            Assert.Greater(s.Duration, 0f);
            Assert.GreaterOrEqual(s.Flash.Count, 0);
            Assert.GreaterOrEqual(s.Sparks.Count, 0);
            Assert.GreaterOrEqual(s.Smoke.Count, 0);
            Assert.GreaterOrEqual(s.Fragments.Count, 0);
            Assert.IsFalse(float.IsNaN(s.Sparks.Speed));
        }

        [Test]
        public void CoreDestroyed_MoreVisibleParticles_ThanHit()
        {
            var core = VfxPresetResolver.Resolve(new VfxEvent(VfxEventKind.CoreDestroyed, Vector3.zero, Vector3.up, ImpactMaterialType.Core, 1f));
            var hit = VfxPresetResolver.Resolve(new VfxEvent(VfxEventKind.Hit, Vector3.zero, Vector3.up, ImpactMaterialType.Core, 1f));
            Assert.Greater(core.Sparks.Count + core.Smoke.Count, hit.Sparks.Count + hit.Smoke.Count);
        }

        [Test]
        public void Rubber_DoesNotProduceFlash()
        {
            var p = VfxPresetResolver.Resolve(new VfxEvent(VfxEventKind.Hit, Vector3.zero, Vector3.up, ImpactMaterialType.Rubber, .8f));
            Assert.AreEqual(0, p.Flash.Count);
        }

        [Test]
        public void Energy_IsBlueDominant()
        {
            var p = VfxPresetResolver.Resolve(new VfxEvent(VfxEventKind.Hit, Vector3.zero, Vector3.up, ImpactMaterialType.Energy, .8f));
            Assert.Greater(p.Sparks.Color.b, p.Sparks.Color.r);
        }

        [Test]
        public void Event_NormalIsNormalized_AndIntensityClamped()
        {
            var e = new VfxEvent(VfxEventKind.Hit, Vector3.zero, Vector3.up * 8, ImpactMaterialType.Metal, 4f);
            Assert.AreEqual(1f, e.Intensity, .0001f);
            Assert.AreEqual(1f, e.Normal.magnitude, .0001f);
        }

        [Test]
        public void Event_ZeroNormalUsesUp()
        {
            var e = new VfxEvent(VfxEventKind.Explosion, Vector3.zero, Vector3.zero);
            Assert.AreEqual(Vector3.up, e.Normal);
        }
    }
}
