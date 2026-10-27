using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R1;

public class R1RunModelTests
{
    private R1UpgradeDefinition[] catalog;
    [SetUp] public void SetUp()
    {
        catalog = Enum.GetValues(typeof(R1UpgradeEffect)).Cast<R1UpgradeEffect>().Select(effect =>
        {
            var definition = ScriptableObject.CreateInstance<R1UpgradeDefinition>();
            definition.Id = effect.ToString(); definition.Title = definition.Id;
            definition.Effect = effect; definition.Amount = 0.2f;
            return definition;
        }).ToArray();
    }
    [TearDown] public void TearDown() { foreach (var item in catalog) UnityEngine.Object.DestroyImmediate(item); }

    [Test] public void RunStartsWithCleanProgressAndThreeEncounters()
    {
        var run = new R1RunProgress();
        Assert.That(run.Start(), Is.True);
        Assert.That(run.State, Is.EqualTo(R1RunState.Combat));
        Assert.That(run.TotalEncounters, Is.EqualTo(3));
        Assert.That(run.EncounterIndex, Is.Zero);
        Assert.That(run.Stats.Damage, Is.EqualTo(1));
    }
    [Test] public void VictoryOffersThreeUniqueChoicesAndOnlyOneCanBeApplied()
    {
        var run = new R1RunProgress(); run.Start();
        Assert.That(run.WinEncounter(catalog, new System.Random(12)), Is.True);
        Assert.That(run.Offers.Count, Is.EqualTo(3));
        Assert.That(run.Offers.Select(item => item.Id).Distinct().Count(), Is.EqualTo(3));
        Assert.That(run.ChooseUpgrade(-1), Is.Null);
        Assert.That(run.ChooseUpgrade(3), Is.Null);
        Assert.That(run.ChooseUpgrade(0), Is.Not.Null);
        Assert.That(run.ChooseUpgrade(1), Is.Null);
        Assert.That(run.Acquired.Count, Is.EqualTo(1));
        Assert.That(run.EncounterIndex, Is.EqualTo(1));
    }
    [Test] public void ThirdVictoryEndsRunWithoutAnotherUpgrade()
    {
        var run = new R1RunProgress(); run.Start();
        for (int i = 0; i < 3; i++)
        {
            run.WinEncounter(catalog, new System.Random(i));
            if (i < 2) { run.ChooseUpgrade(0); Assert.That(run.LeaveGarage(),Is.True); }
        }
        Assert.That(run.State, Is.EqualTo(R1RunState.Won));
        Assert.That(run.CompletedEncounters, Is.EqualTo(3));
        Assert.That(run.Offers, Is.Empty);
        run.Lose("late death");
        Assert.That(run.State, Is.EqualTo(R1RunState.Won));
    }
    [Test] public void LossClearsOffersAndNewRunDoesNotInheritBonuses()
    {
        var run = new R1RunProgress(); run.Start();
        run.WinEncounter(catalog, new System.Random(7)); run.ChooseUpgrade(0);
        run.LeaveGarage();
        run.Lose("Rdzeń zniszczony");
        Assert.That(run.State, Is.EqualTo(R1RunState.Lost));
        Assert.That(run.Start(), Is.True);
        Assert.That(run.CompletedEncounters, Is.Zero);
        Assert.That(run.Acquired, Is.Empty);
        Assert.That(run.Stats.Hull, Is.EqualTo(1));
        Assert.That(run.Stats.Mobility, Is.EqualTo(1));
    }
    [Test] public void ModifiersActuallyChangeStatsAndStayWithinBounds()
    {
        var stats = new R1RunStats();
        stats.Apply(catalog.Single(item => item.Effect == R1UpgradeEffect.Damage));
        Assert.That(stats.Damage, Is.EqualTo(1.2f).Within(0.001));
        for (int i = 0; i < 50; i++) foreach (var item in catalog) stats.Apply(item);
        Assert.That(stats.Hull, Is.EqualTo(3));
        Assert.That(stats.Damage, Is.EqualTo(3));
        Assert.That(stats.Mobility, Is.EqualTo(2));
        Assert.That(stats.Core, Is.EqualTo(4));
        Assert.That(stats.FireInterval, Is.EqualTo(0.35f).Within(0.001));
        Assert.That(catalog[0].Amount, Is.EqualTo(0.2f));
    }
    [Test] public void RepeatedWinCannotAdvancePastUnchosenReward()
    {
        var run = new R1RunProgress(); run.Start();
        run.WinEncounter(catalog, new System.Random(1));
        Assert.That(run.WinEncounter(catalog, new System.Random(2)), Is.False);
        Assert.That(run.CompletedEncounters, Is.EqualTo(1));
        Assert.That(run.Start(), Is.False);
    }
}
