// B01-60 PASS17: real .NET8 execution of three UNCHANGED original Codex C# files.
// Uses only UnityEngine API/JsonUtility shim; no actual Unity engine or gameplay.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R5;
using UnityEngine;

public static class Program
{
    private static readonly List<string> Checks=new();
    private static void Need(bool yes,string label)
    {
        if(!yes)throw new InvalidOperationException("ORIGINAL_R5_QA_FAIL: "+label);
        Checks.Add(label);
    }
    private static string Snapshot(R1EncounterDefinition[] offers)
       =>JsonSerializer.Serialize(offers,new JsonSerializerOptions{IncludeFields=true});
    private static R5Catalog Reload(R5Catalog src)
       =>JsonUtility.FromJson<R5Catalog>(JsonUtility.ToJson(src));
    private static void MustReject(string name,Action action)
    {
        try{action();}
        catch(ArgumentException){Checks.Add("REJECT_"+name);return;}
        throw new InvalidOperationException("ORIGINAL_R5_FAIL_OPEN:"+name);
    }
    public static int Main(string[] args)
    {
        if(args.Length!=2)
        {
            Console.Error.WriteLine("Usage: Harness <exact-original-owner-R5.json> <receipt.json>");
            return 2;
        }
        var raw=File.ReadAllText(args[0]);
        var src=JsonUtility.FromJson<R5Catalog>(raw);
        var itemCount=src.Stages.SelectMany(s=>s.Options).Count();
        try
        {
            Need(src.Stages.Length==6,"ORIGINAL_6_STAGES");
            Need(itemCount==10,"ORIGINAL_10_ENCOUNTER_OPTIONS");
            R5RunPlan.Validate(src.Stages);
            Checks.Add("ACTUAL_ORIGINAL_CSHARP_VALIDATE");
            var ids=src.Stages.SelectMany(s=>s.Options).Select(x=>x.Id).ToArray();
            Need(ids.Distinct().Count()==10,"ENCOUNTER_UNIQUE_IDS");
            Need(src.Stages.Skip(1).Take(4).All(s=>s.Options.Length==2),
                 "FOUR_DISTINCT_CHOICE_STAGES");
            Need(src.Stages[^1].Options[0].IsBoss,"BOSS_STAGES_6");
            int offersChecked=0,bosses=0,escorts=0;
            var seeds=Enumerable.Range(-32,128).ToArray();
            foreach(int seed in seeds)
            {
                var a=new R5RunPlan(src.Stages,seed);
                var b=new R5RunPlan(src.Stages,seed);
                Need(a.Count==6&&b.Count==6,"RUNPLAN_STAGE_COUNT_SEED_"+seed);
                for(int stage=0;stage<6;stage++)
                {
                    var original=a.Offers(stage);
                    var matching=b.Offers(stage);
                    Need(Snapshot(original)==Snapshot(matching),
                         "DETERMINISTIC_SEEDED_OFFERS_"+seed+"_"+stage);
                    Need(original.Length==src.Stages[stage].Options.Length,
                         "STAGE_OPTION_COUNT_"+seed+"_"+stage);
                    foreach(var item in original)
                    {
                        offersChecked++;
                        if(item.IsBoss)bosses++;else escorts++;
                        Need(item.SpawnCount==(item.IsBoss?1:item.EnemyCount+1),
                             "CORRECT_ACTUAL_ESCORT_"+seed+"_"+stage);
                        Need(R5RunPlan.Threat(item)<=item.ThreatBudget+.0001f,
                             "ACTUAL_SOURCE_THREAT_WITHIN_CAP_"+seed+"_"+stage);
                        Need(item.Risk==R5RunPlan.RiskName(item),
                             "ACTUAL_RISK_SOURCE_"+seed+"_"+stage);
                        var authored=src.Stages[stage].Options.Single(x=>x.Id==item.Id);
                        Need(item.ScrapReward>=authored.ScrapReward-authored.RewardJitter &&
                             item.ScrapReward<=authored.ScrapReward+authored.RewardJitter,
                             "JITTER_REWARD_RANGE_"+seed+"_"+stage);
                        Need(item.Roster.Length==item.EnemyCount &&
                             item.Variants.Length==item.EnemyCount,
                             "ROSTER_VARIANTS_EQUAL_"+seed+"_"+stage);
                    }
                }
            }
            // True compiler/code execution negatives, rather than the B01-10 JS model.
            {
                var x=Reload(src);x.Stages[2].Options[0].Id=x.Stages[1].Options[0].Id;
                MustReject("DUPLICATE_ID",()=>R5RunPlan.Validate(x.Stages));
            }
            {
                var x=Reload(src);x.Stages[4].Options[1].ThreatBudget=1;
                MustReject("REAL_CSHARP_BUDGET",()=>R5RunPlan.Validate(x.Stages));
            }
            {
                var x=Reload(src);x.Stages[3].Options[1].Roster[0]=R3EnemyArchetype.Bruiser;
                MustReject("GUNNER_ELITE_WRONG_ARCHETYPE",()=>R5RunPlan.Validate(x.Stages));
            }
            {
                var x=Reload(src);x.Stages[0].Options[0].RewardJitter=-1;
                MustReject("NEGATIVE_REWARD_JITTER",()=>R5RunPlan.Validate(x.Stages));
            }

            // Independent isolation QA absent in producer's 9 authored tests:
            // A caller's returned offers may alias the supposedly immutable itinerary.
            var plan=new R5RunPlan(src.Stages,202617);
            var originalOffer=plan.Offers(1);
            var originalMoney=originalOffer[0].ScrapReward;
            originalOffer[0].ScrapReward=originalMoney+10007;
            bool mutatedPlanMoney=plan.Offers(1)[0].ScrapReward==originalMoney+10007;
            originalOffer[0].ScrapReward=originalMoney; // undo in disposable memory
            var originalArchetype=originalOffer[0].Roster[0];
            var changedArchetype=originalArchetype==R3EnemyArchetype.Flanker?
                 R3EnemyArchetype.Scout:R3EnemyArchetype.Flanker;
            originalOffer[0].Roster[0]=changedArchetype;
            bool mutatedPlanRoster=plan.Offers(1)[0].Roster[0]==changedArchetype;
            originalOffer[0].Roster[0]=originalArchetype;
            var safeArray=plan.Offers(1);
            var originalId=safeArray[0].Id;
            safeArray[0]=new R1EncounterDefinition{Id="external-substitution"};
            bool arrayReplacementIsolated=plan.Offers(1)[0].Id==originalId;
            Need(arrayReplacementIsolated,"OFFERS_ARRAY_REPLACEMENT_ISOLATED");

            var receipt=new
            {
                package="R5-PLAN-ISOLATION-QA-001",
                original_csharp_execution="REAL_DOTNET8_WITH_EXPLICIT_UNITY_SHIM",
                independent_native_unity="NOT_RUN",
                original_catalog_stage_count=6,
                original_catalog_option_count=10,
                tested_seeds=seeds.Length,
                tested_source_offers=offersChecked,
                boss_offers=bosses,
                nonboss_escort_offers=escorts,
                real_original_csharp_rejections=4,
                checks=Checks.Count,
                producer_own_unity_tests="NOT_RUN",
                externally_returned_offer_mutates_internal_payout=mutatedPlanMoney,
                externally_returned_roster_mutates_internal_roster=mutatedPlanRoster,
                external_array_element_replacement_is_isolated=arrayReplacementIsolated,
                receiver_isolation_verdict=mutatedPlanMoney||mutatedPlanRoster?
                  "SOURCE_RED_RETURNED_OFFERS_ALIAS_INTERNAL_STATE":"ISOLATION_GREEN",
                game_integration="NO",
                codex_change="NONE"
            };
            var output=JsonSerializer.Serialize(receipt,new JsonSerializerOptions{WriteIndented=true});
            File.WriteAllText(args[1],output+"\n");
            Console.WriteLine("B0160_PASS17_R5_REAL_CSHARP_RECEIPT="+JsonSerializer.Serialize(receipt));
            Console.WriteLine("B0160_PASS17_R5_COMPILED_SOURCE_EXECUTED;NATIVE_UNITY_NOT_RUN");
            return 0;
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine("B0160_PASS17_R5_UNEXPECTED_FAIL="+ex);
            return 1;
        }
    }
}
