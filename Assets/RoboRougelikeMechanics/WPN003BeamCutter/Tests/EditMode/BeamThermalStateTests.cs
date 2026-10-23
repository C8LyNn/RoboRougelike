using NUnit.Framework;
using RoboRougelike.Mechanics.BeamCutter;

namespace RoboRougelike.Mechanics.BeamCutter.Tests
{
    public sealed class BeamThermalStateTests
    {
        private static BeamThermalState Thermal(float rate = 25f, float cool = 20f, float restart = 0.4f)
            => new BeamThermalState(new BeamCutterSettings
            {
                heatCapacity = 100f,
                heatPerSecond = rate,
                coolPerSecond = cool,
                restartHeatFraction = restart
            });

        [Test] public void StartsColdAndReady()
        {
            var t = Thermal();
            Assert.AreEqual(0f, t.Heat);
            Assert.IsFalse(t.IsOverheated);
        }
        [Test] public void AccumulatesHeatByElapsedSeconds()
        {
            var t = Thermal(); t.AdvanceFiring(0.5f, 0.5f);
            Assert.AreEqual(12.5f, t.Heat, 0.0001f);
        }
        [Test] public void GetsOverheatedAtCapacity()
        {
            var t = Thermal(100f); t.AdvanceFiring(1f, 1f);
            Assert.IsTrue(t.IsOverheated);
            Assert.AreEqual(100f, t.Heat);
        }
        [Test] public void FireDurationLimitedByRemainingHeatBudget()
        {
            var t = Thermal(100f); t.AdvanceFiring(0.8f, 0.8f);
            Assert.AreEqual(0.2f, t.AvailableFireSeconds(1f), 0.0001f);
        }
        [Test] public void CoolingReleasesLockoutAfterThreshold()
        {
            var t = Thermal(100f, 20f, 0.4f); t.AdvanceFiring(1f, 1f);
            t.AdvanceIdle(2f); Assert.IsTrue(t.IsOverheated);
            t.AdvanceIdle(1f); Assert.IsFalse(t.IsOverheated);
        }
        [Test] public void CoolingDoesNotGoNegative()
        {
            var t = Thermal(); t.AdvanceIdle(10f);
            Assert.AreEqual(0f, t.Heat);
        }
        [Test] public void NoHeatRate_AllowsFullTime()
        {
            var t = Thermal(0f);
            Assert.AreEqual(2f, t.AvailableFireSeconds(2f));
        }
        [Test] public void Overheated_CannotFireImmediately()
        {
            var t = Thermal(100f); t.AdvanceFiring(1f, 1f);
            Assert.AreEqual(0f, t.AvailableFireSeconds(0.5f));
        }
        [Test] public void PartialFireTick_CoolsUnusedRemainder()
        {
            var t = Thermal(100f, 10f); t.AdvanceFiring(0.5f, 1f);
            Assert.AreEqual(45f, t.Heat, 0.0001f);
        }
        [Test] public void ResetClearsOverheat()
        {
            var t = Thermal(100f); t.AdvanceFiring(1f, 1f); t.Reset();
            Assert.AreEqual(0f, t.Heat); Assert.IsFalse(t.IsOverheated);
        }
        [TestCase(-1f)]
        [TestCase(0f)]
        public void InvalidStep_NoHeatChange(float step)
        {
            var t = Thermal(); t.AdvanceIdle(step);
            Assert.AreEqual(0f, t.Heat);
        }
        [Test] public void InvalidFireTime_DoesNotHeat()
        {
            var t = Thermal(); t.AdvanceFiring(float.NaN, 1f);
            Assert.AreEqual(0f, t.Heat);
        }
        [Test] public void FireSecondsClampedToStep()
        {
            var t = Thermal(); t.AdvanceFiring(5f, 1f);
            Assert.AreEqual(25f, t.Heat);
        }
        [Test] public void HeatFraction_TracksCapacity()
        {
            var t = Thermal(); t.AdvanceFiring(1f, 1f);
            Assert.AreEqual(0.25f, t.HeatFraction, 0.0001f);
        }
    }
}
