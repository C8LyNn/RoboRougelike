// BAL17-R5-ENCOUNTER-DECK v1.0 -- optional Unity 6000.3.14f1 EditMode acceptance.
// AUTHORED ONLY. NOT_RUN in Unity by B01-10; C# not compiled here.
// Copy to a disposable Codex-owned Editor-only test assembly AFTER replacing
// original R5/Encounters.json in a disposable project/worktree, or run this
// fixture read-only against the RESEARCH path before any runtime integration.
// Do NOT add this fixture or the research deck to canonical Assets directly.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R5;
using UnityEngine;

public sealed class B0110Bal17R5EncounterDeckNativeTests
{
    private static R5Catalog Deck()
    {
        var root=Directory.GetCurrentDirectory();
        var file=Path.Combine(root,"Research","B01-10","P17","R5_EncounterDeck_BAL17.json");
        Assert.That(File.Exists(file),Is.True,"Copy the original immutable P17 JSON into test checkout.");
        var text=File.ReadAllText(file);
        Assert.That(text,Does.Contain("\"Stages\""));
        var deck=JsonUtility.FromJson<R5Catalog>(text);
        Assert.That(deck,Is.Not.Null);
        return deck;
    }
    [Test]
    public void OriginalR5RunPlan_ImportsSixStagesTenSupportedOptionsWithoutSchemaChanges()
    {
        var d=Deck();
        Assert.That(d.Stages,Has.Length.EqualTo(6));
        Assert.That(d.Stages.Sum(s=>s.Options.Length),Is.EqualTo(10));
        Assert.DoesNotThrow(()=>R5RunPlan.Validate(d.Stages));
        Assert.That(d.Stages[0].Options,Has.Length.EqualTo(1));
        Assert.That(d.Stages[5].Options,Has.Length.EqualTo(1));
        Assert.That(d.Stages[5].Options[0].IsBoss,Is.True);
        for(int i=1;i<=4;i++)Assert.That(d.Stages[i].Options,Has.Length.EqualTo(2));
    }
    [TestCase(0)]
    [TestCase(7)]
    [TestCase(2026)]
    [TestCase(867)]
    public void OriginalR5RunPlan_SeededOfferReplayAndRiskCaps(int seed)
    {
        var catalog=Deck();
        var a=new R5RunPlan(catalog.Stages,seed);
        var b=new R5RunPlan(catalog.Stages,seed);
        for(int stage=0;stage<6;stage++)
        {
            var first=a.Offers(stage);
            var second=b.Offers(stage);
            Assert.That(first.Select(JsonUtility.ToJson),
                Is.EqualTo(second.Select(JsonUtility.ToJson)));
            Assert.That(first.Select(JsonUtility.ToJson),
                Is.EqualTo(a.Offers(stage).Select(JsonUtility.ToJson)));
            foreach(var choice in first)
            {
                Assert.That(choice.SpawnCount,Is.InRange(1,4));
                Assert.That(R5RunPlan.Threat(choice),
                    Is.LessThanOrEqualTo(choice.ThreatBudget+0.0001f),
                    "Source threat budget exceeded: "+choice.Id);
                Assert.That(choice.Risk,Is.EqualTo(R5RunPlan.RiskName(choice)));
                Assert.That(choice.ScrapReward,Is.GreaterThanOrEqualTo(0));
            }
        }
    }
    [Test]
    public void OriginalR5RunPlan_BranchRewardsSeparatedDespiteJitter()
    {
        var catalog=Deck();
        for(int i=1;i<=4;i++)
        {
            var routes=catalog.Stages[i].Options;
            var lower=routes[0];
            var higher=routes[1];
            Assert.That(lower.ScrapReward+lower.RewardJitter,
                Is.LessThan(higher.ScrapReward-higher.RewardJitter),
                "Expected a perceptible guaranteed higher reward for harder branch.");
        }
    }
    [Test]
    public void OriginalR5RunPlan_RejectsOverBudgetRosterWithoutMutationOfSource()
    {
        var catalog=Deck();
        catalog.Stages[4].Options[1].ThreatBudget=1;
        Assert.Throws<ArgumentException>(()=>R5RunPlan.Validate(catalog.Stages));
    }
    [Test]
    public void OriginalR5RunPlan_RejectsDuplicateIdsWithoutMutationOfSource()
    {
        var catalog=Deck();
        catalog.Stages[2].Options[0].Id=catalog.Stages[1].Options[0].Id;
        Assert.Throws<ArgumentException>(()=>R5RunPlan.Validate(catalog.Stages));
    }
    [Test]
    public void OriginalR5RunPlan_RejectsIncompatibleEliteArchetype()
    {
        var catalog=Deck();
        // Stage4, variant EliteGunner is only allowed on original Gunner.
        catalog.Stages[3].Options[1].Roster[0]=R3EnemyArchetype.Bruiser;
        Assert.Throws<ArgumentException>(()=>R5RunPlan.Validate(catalog.Stages));
    }
}
#endif
