using NUnit.Framework;
using RoboRougelike.Mechanics.BeamCutter;

namespace RoboRougelike.Mechanics.BeamCutter.Tests
{
    public sealed class BeamSettingsTests
    {
        [Test] public void DefaultRange_IsPositive() => Assert.Greater(new BeamCutterSettings().rangeMeters, 0f);
        [Test] public void DefaultDamage_IsPositive() => Assert.Greater(new BeamCutterSettings().damagePerSecond, 0f);
        [Test] public void Sanitized_IsCopy()
        {
            var original = new BeamCutterSettings();
            Assert.AreNotSame(original, original.Sanitized());
        }
        [Test] public void Sanitized_NullOrNaNValues_FallBack()
        {
            var cfg = new BeamCutterSettings { rangeMeters = float.NaN, heatCapacity = float.PositiveInfinity, damagePerSecond = float.NegativeInfinity };
            var safe = cfg.Sanitized();
            Assert.AreEqual(16f, safe.rangeMeters);
            Assert.AreEqual(100f, safe.heatCapacity);
            Assert.AreEqual(0f, safe.damagePerSecond);
        }
        [TestCase(-1f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(7f, 7f)]
        public void Sanitized_DamageClamped(float input, float expected)
        {
            Assert.AreEqual(expected, new BeamCutterSettings { damagePerSecond = input }.Sanitized().damagePerSecond);
        }
        [TestCase(-2f, 0f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(5f, 1f)]
        public void Sanitized_ThresholdInRange(float input, float expected)
        {
            Assert.AreEqual(expected, new BeamCutterSettings { restartHeatFraction = input }.Sanitized().restartHeatFraction);
        }
        [TestCase(-1f, 0f)]
        [TestCase(3.5f, 3.5f)]
        public void Sanitized_EnergyNonnegative(float energy, float expected)
        {
            Assert.AreEqual(expected, new BeamCutterSettings { energyPerSecond = energy }.Sanitized().energyPerSecond);
        }
        [Test] public void Sanitized_LayersPreserved()
        {
            var c = new BeamCutterSettings { hitLayers = 1 << 5 };
            Assert.AreEqual(1 << 5, c.Sanitized().hitLayers.value);
        }
    }
}
