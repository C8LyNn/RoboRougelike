using NUnit.Framework;
using RoboRougelike.WPN002Railgun;

namespace RoboRougelike.WPN002Railgun.Tests.EditMode
{
    public sealed class RailgunRulesTests
    {
        [TestCase(100f, 10f, 1f, 90f)]
        [TestCase(100f, 0f, 5f, 100f)]
        [TestCase(100f, 50f, 5f, 0f)]
        [TestCase(50f, -5f, 2f, 50f)]
        [TestCase(0f, 2f, 2f, 0f)]
        public void TravelLossIsBounded(float input, float meters, float loss, float expected)
            => Assert.That(RailgunRules.AfterTravel(input, meters, loss), Is.EqualTo(expected).Within(0.0001f));

        [Test] public void TravelRejectsNaN()
            => Assert.That(RailgunRules.AfterTravel(float.NaN, 1f, 1f), Is.Zero);
        [Test] public void TravelRejectsInfinity()
            => Assert.That(RailgunRules.AfterTravel(float.PositiveInfinity, 1f, 1f), Is.Zero);
        [Test] public void TravelRejectsInvalidDistance()
            => Assert.That(RailgunRules.AfterTravel(100f, float.NaN, 1f), Is.Zero);
        [Test] public void ClampRejectsNaN()
            => Assert.That(RailgunRules.ClampFinite(float.NaN, 1f, 10f, 5f), Is.EqualTo(5f));
        [Test] public void ClampRejectsInfinity()
            => Assert.That(RailgunRules.ClampFinite(float.PositiveInfinity, 1f, 10f, 5f), Is.EqualTo(5f));
        [Test] public void ClampCapsUpperBound()
            => Assert.That(RailgunRules.ClampFinite(50f, 1f, 10f, 5f), Is.EqualTo(10f));
        [Test] public void ClampCapsLowerBound()
            => Assert.That(RailgunRules.ClampFinite(-50f, 1f, 10f, 5f), Is.EqualTo(1f));
        [Test] public void DamageScalesWithIncomingEnergy()
            => Assert.That(RailgunRules.Evaluate(100, 20, .5f, 200, 0, 3, 2, true).Damage, Is.EqualTo(50f));
        [Test] public void DamageCannotExceedCap()
            => Assert.That(RailgunRules.Evaluate(100, 20, 3, 24, 0, 3, 2, true).Damage, Is.EqualTo(24f));
        [Test] public void DamageIsZeroWithoutEnergy()
            => Assert.That(RailgunRules.Evaluate(0, 20, 1, 100, 0, 3, 2, true).Damage, Is.Zero);
        [Test] public void ResistanceSubtractsEnergy()
            => Assert.That(RailgunRules.Evaluate(100, 35, 1, 200, 0, 3, 2, true).RemainingEnergy, Is.EqualTo(65f));
        [Test] public void ResistanceCannotProduceNegativeEnergy()
            => Assert.That(RailgunRules.Evaluate(100, 350, 1, 200, 0, 3, 2, true).RemainingEnergy, Is.Zero);
        [Test] public void ZeroResistanceStillConsumesEnergy()
            => Assert.That(RailgunRules.Evaluate(100, 0, 1, 200, 0, 3, 2, true).RemainingEnergy, Is.LessThan(100f));
        [Test] public void NonPenetrablePartStopsProjectile()
            => Assert.That(RailgunRules.Evaluate(100, 10, 1, 200, 0, 3, 2, false).Continues, Is.False);
        [Test] public void FirstPenetrablePartAllowsContinuation()
            => Assert.That(RailgunRules.Evaluate(100, 10, 1, 200, 0, 3, 2, true).Continues, Is.True);
        [Test] public void FinalAllowedPartStopsProjectile()
            => Assert.That(RailgunRules.Evaluate(100, 10, 1, 200, 2, 3, 2, true).Continues, Is.False);
        [Test] public void MinimumEnergyThresholdStopsProjectile()
            => Assert.That(RailgunRules.Evaluate(100, 98, 1, 200, 0, 3, 3, true).Continues, Is.False);
        [Test] public void NonFiniteResistanceFailsClosed()
            => Assert.That(RailgunRules.Evaluate(100, float.NaN, 1, 200, 0, 3, 0, true).Continues, Is.False);
        [Test] public void OnePartMaximumStops()
            => Assert.That(RailgunRules.Evaluate(100, 10, 1, 200, 0, 1, 2, true).Continues, Is.False);
    }
}
