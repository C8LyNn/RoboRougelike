using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.VFX001.Tests
{
    public sealed class VfxV4ScarTests
    {
        [TestCase(-4f, .12f)]
        [TestCase(0f, .12f)]
        [TestCase(1f, .48f)]
        [TestCase(8f, .48f)]
        public void ScarDiameterClamps(float input, float diameter)
        {
            Assert.AreEqual(diameter, VfxScarResolver.Resolve(ImpactMaterialType.Metal, input).Diameter, .0001f);
        }
        [Test]
        public void RubberCracksAreLesserThanMetal()
        {
            Assert.Less(VfxScarResolver.Resolve(ImpactMaterialType.Rubber, .7f).CrackStrength,
                        VfxScarResolver.Resolve(ImpactMaterialType.Metal, .7f).CrackStrength);
        }
        [Test]
        public void HighIntensityHasHigherOpacity()
        {
            Assert.Greater(VfxScarResolver.Resolve(ImpactMaterialType.Core, 1f).Opacity,
                           VfxScarResolver.Resolve(ImpactMaterialType.Core, .1f).Opacity);
        }
        [Test]
        public void DistanceLodIsBoundedAndFades()
        {
            Assert.AreEqual(1f, VfxDistanceLod.RateFactor(3f), .0001f);
            Assert.AreEqual(0f, VfxDistanceLod.RateFactor(100f), .0001f);
            Assert.Greater(VfxDistanceLod.RateFactor(20f), VfxDistanceLod.RateFactor(30f));
        }
        [Test]
        public void DistanceLodRejectsNonfiniteDistance()
        {
            Assert.AreEqual(0f, VfxDistanceLod.RateFactor(float.NaN));
            Assert.AreEqual(0f, VfxDistanceLod.RateFactor(float.PositiveInfinity));
        }
        [Test]
        public void ScarControllerRejectsMissingNormalsWithoutCreatingMarks()
        {
            var go = new GameObject("Armor Part");
            var scars = go.AddComponent<ArmorScarController>();
            Assert.IsFalse(scars.AddScar(Vector3.zero, Vector3.zero, ImpactMaterialType.Metal, 1f));
            Assert.AreEqual(0, scars.MarkCount);
            Object.DestroyImmediate(go);
        }
    }
}
