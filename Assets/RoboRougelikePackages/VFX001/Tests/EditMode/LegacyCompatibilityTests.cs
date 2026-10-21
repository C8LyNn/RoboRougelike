using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.VFX001.Tests
{
    public class LegacyCompatibilityTests
    {
        [Test]
        public void V1Resolver_Preserved()
        {
            var metal = ImpactFeedbackResolver.Resolve(ImpactMaterialType.Metal, 1f);
            var rubber = ImpactFeedbackResolver.Resolve(ImpactMaterialType.Rubber, 1f);
            Assert.Greater(metal.particleCount, rubber.particleCount);
        }
        [Test]
        public void V1Spec_Valid()
        {
            var spec = ImpactFeedbackSpec.Default;
            Assert.Greater(spec.particleCount, 0);
            Assert.Greater(spec.lifeTime, 0f);
        }
    }
}
