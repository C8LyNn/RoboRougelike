using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.VFX001.Tests
{
    public sealed class VfxTimelineTests
    {
        [Test]
        public void ExplosionHasSmokeAndFragments()
        {
            var p = VfxPresetResolver.Resolve(new VfxEvent(VfxEventKind.Explosion, Vector3.zero, Vector3.up, ImpactMaterialType.Engine, .9f));
            Assert.Greater(p.Smoke.Count, 0);
            Assert.Greater(p.Fragments.Count, 0);
            Assert.Greater(p.Duration, p.Flash.Lifetime);
        }
        [Test]
        public void DustHasNoMetalFragments()
        {
            var p = VfxPresetResolver.Resolve(new VfxEvent(VfxEventKind.LandingDust, Vector3.zero, Vector3.up));
            Assert.AreEqual(0, p.Fragments.Count);
            Assert.AreEqual(0, p.Sparks.Count);
        }
    }
}
