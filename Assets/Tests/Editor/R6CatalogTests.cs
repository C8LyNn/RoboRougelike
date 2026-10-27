using System;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R5;
using RoboRougelike.R3;
public class R6CatalogTests
{
    R5Catalog Catalog()=>JsonUtility.FromJson<R5Catalog>(Resources.Load<TextAsset>("R5/Encounters").text);
    [Test] public void InvalidAndUnselectedConfigurationsFailBeforeRunStarts()
    {
        var broken=Catalog();broken.Stages[0]=null;Assert.Throws<ArgumentException>(()=>new R5RunPlan(broken.Stages,2026));
        broken=Catalog();broken.Stages[1].Options[1].DamageMultiplier=float.NaN;Assert.Throws<ArgumentException>(()=>new R5RunPlan(broken.Stages,2026));
        broken=Catalog();broken.Stages[1].Options[1].AlternativeRoster=new[]{(R3EnemyArchetype)99};Assert.Throws<ArgumentException>(()=>new R5RunPlan(broken.Stages,2026));
        broken=Catalog();broken.Stages[2].Options[0].ScrapReward=-5;Assert.Throws<ArgumentException>(()=>new R5RunPlan(broken.Stages,2026));
        broken=Catalog();broken.Stages[2].Options[0].Id=broken.Stages[0].Options[0].Id;Assert.Throws<ArgumentException>(()=>new R5RunPlan(broken.Stages,2026));
    }
    [Test] public void MoreRewardAlwaysHasMoreThreatForBothRosterOutcomes()
    {
        for(int seed=0;seed<50;seed++)
        {
            var plan=new R5RunPlan(Catalog().Stages,seed);
            for(int stage=0;stage<6;stage++)
            {
                var offers=plan.Offers(stage);if(offers.Length<2)continue;
                Assert.That(offers[1].ScrapReward,Is.GreaterThan(offers[0].ScrapReward));
                Assert.That(R5RunPlan.Threat(offers[1]),Is.GreaterThan(R5RunPlan.Threat(offers[0])),"Stage "+stage+", seed "+seed);
            }
        }
    }
}
