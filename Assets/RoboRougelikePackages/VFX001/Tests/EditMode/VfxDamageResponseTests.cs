using NUnit.Framework;

namespace RoboRougelike.VFX001.Tests
{
    public sealed class VfxDamageResponseTests
    {
        [TestCase(1f, VfxDamageStage.Healthy)]
        [TestCase(.7f, VfxDamageStage.Scratched)]
        [TestCase(.5f, VfxDamageStage.Smoking)]
        [TestCase(.1f, VfxDamageStage.Critical)]
        public void Health_MapsToExpectedStage(float health, VfxDamageStage expected)
        {
            Assert.AreEqual(expected, VfxDamageResponse.Classify(health, false));
        }
        [Test]
        public void Detached_HasIndependentHighestPriority()
        {
            Assert.AreEqual(VfxDamageStage.Detached, VfxDamageResponse.Classify(1f, true));
            Assert.AreEqual(0f, VfxDamageResponse.SmokeIntensity(.1f, true));
        }
        [Test]
        public void Smoke_OnlyBelowThreshold_AndClamped()
        {
            Assert.AreEqual(0f, VfxDamageResponse.SmokeIntensity(.9f, false));
            Assert.AreEqual(1f, VfxDamageResponse.SmokeIntensity(0f, false));
            Assert.That(VfxDamageResponse.SmokeIntensity(.4f, false), Is.InRange(0f, 1f));
        }
        [Test]
        public void FlashOnlyOnMeaningfulEscalation()
        {
            Assert.IsTrue(VfxDamageResponse.ShouldFlashOnDamage(VfxDamageStage.Healthy, VfxDamageStage.Smoking));
            Assert.IsFalse(VfxDamageResponse.ShouldFlashOnDamage(VfxDamageStage.Critical, VfxDamageStage.Critical));
            Assert.IsFalse(VfxDamageResponse.ShouldFlashOnDamage(VfxDamageStage.Healthy, VfxDamageStage.Detached));
        }
    }
}
