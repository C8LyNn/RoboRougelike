// B01-24 ENEMY-RELAY-001 — runs ORIGINAL C# R5RunPlan.Validate,
// R5RunPlan.Threat, R5RunPlan.Offers, and R1EncounterDefinition.Spawn*.
// Only UnityEngine.JsonUtility + Mathf/attributes are headlessly substituted.
// .NET8 output is a source-level contract test; Unity native load is NOT_RUN.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R5;
using UnityEngine;

internal static class Program
{
    private static int failures;
    private static int passes;
    private static void Check(bool ok, string detail)
    {
        if(!ok) throw new InvalidOperationException(detail);
    }
    private static void Run(string name, Action action)
    {
        try { action(); passes++; Console.WriteLine("PASS " + name); }
        catch(Exception e) { failures++; Console.Error.WriteLine("FAIL " + name + " :: " + e.Message); }
    }
    private static R5Catalog Load(string filename) =>
        JsonUtility.FromJson<R5Catalog>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, filename)));
    private static R5Catalog Fresh() => Load("Encounters-Relay.json");
    private static R5Catalog Copy(R5Catalog original) =>
        JsonUtility.FromJson<R5Catalog>(JsonUtility.ToJson(original));
    private static IEnumerable<R1EncounterDefinition> All(R5Catalog input) =>
        input.Stages.SelectMany(s => s.Options);
    private static void Rejected(Action action, string why)
    {
        try { action(); }
        catch(ArgumentException) { return; }
        throw new InvalidOperationException("Source validator accepted illegal case: " + why);
    }
    private static string Fingerprint(R5RunPlan plan)
    {
        return string.Join("|",Enumerable.Range(0,plan.Count).SelectMany(stage =>
            plan.Offers(stage).Select(o => o.Id + ":" + o.ScrapReward + ":" + o.Risk + ":" +
               string.Join(",",o.Roster.Select(x=>((int)x).ToString())))));
    }
    private static void ValidatePhysicalSpawnCount(R1EncounterDefinition item)
    {
        Check(item.SpawnCount == (item.IsBoss ? 1 : item.EnemyCount + 1),
            "wrong effective source SpawnCount: " + item.Id);
        if(!item.IsBoss)
        {
            Check(item.SpawnArchetype(item.SpawnCount-1)==R3EnemyArchetype.Scout,
                "missing original mandatory escort");
            Check(item.SpawnVariant(item.SpawnCount-1)==R5EnemyVariant.Normal,
                "extra escort cannot be elite/boss");
        }
    }
    public static int Main()
    {
        Run("actual-original-6-stage-catalog-validation", () =>
        {
            var c=Fresh(); R5RunPlan.Validate(c.Stages);
            Check(c.Stages.Length==6, "requires exact 6 stages");
            Check(c.Stages.Sum(s=>s.Options.Length)==10,"requires ten new encounters");
            Check(c.Stages.All(s=>s.Options.Length>=1&&s.Options.Length<=2),"one/two routes per stage");
        });
        Run("source-owned-existing-catalog-positive-control",() =>
        {
            R5RunPlan.Validate(Load("Existing-Encounters.json").Stages);
        });
        Run("unique-new-identifiers-and-zero-overlap-with-canonical",() =>
        {
            var c=Fresh();var ids=All(c).Select(o=>o.Id).ToArray();
            var existing=new HashSet<string>(All(Load("Existing-Encounters.json")).Select(o=>o.Id));
            Check(ids.Length==ids.Distinct().Count(),"repeated new ID");
            Check(ids.All(id=>id.StartsWith("b0124-relay-",StringComparison.Ordinal)),"owner prefix");
            Check(ids.All(id=>!existing.Contains(id)),"duplicate canonical encounter ID");
        });
        Run("original-threat-budgets-base-and-alternatives",() =>
        {
            foreach(var o in All(Fresh()))
            {
                Check(R5RunPlan.Threat(o)<=o.ThreatBudget,"base exceeded budget "+o.Id);
                if(o.AlternativeRoster!=null&&o.AlternativeRoster.Length>0)
                {
                    var alt=JsonUtility.FromJson<R1EncounterDefinition>(JsonUtility.ToJson(o));
                    alt.Roster=o.AlternativeRoster.ToArray();
                    Check(R5RunPlan.Threat(alt)<=o.ThreatBudget,"alternate exceeded budget "+o.Id);
                }
            }
        });
        Run("actual-source-extra-escort-and-boss-spawn-semantics",() =>
        {
            foreach(var o in All(Fresh())) ValidatePhysicalSpawnCount(o);
            Check(All(Fresh()).Count(o=>o.IsBoss)==1,"exactly one boss finale");
        });
        Run("all-6-original-R5RunPlan-seed-replays",() =>
        {
            var c=Fresh();foreach(var seed in new[]{7,31,83,109,202,407})
            {
                var a=new R5RunPlan(c.Stages,seed), b=new R5RunPlan(c.Stages,seed);
                Check(Fingerprint(a)==Fingerprint(b),"seed nondeterminism "+seed);
                Check(a.Count==6,"stage count");
                for(int stage=0;stage<6;stage++)
                    foreach(var o in a.Offers(stage))
                    {
                        Check(o.EnemyCount==o.Roster.Length,"resolved roster mismatch");
                        ValidatePhysicalSpawnCount(o);
                    }
            }
        });
        Run("bounded-real-rewards-and-risk-labels-from-source",() =>
        {
            var c=Fresh();
            var originals=All(c).ToDictionary(o=>o.Id);
            foreach(var seed in new[]{7,31,83,109,202,407})
            foreach(var stage in Enumerable.Range(0,6))
            foreach(var o in new R5RunPlan(c.Stages,seed).Offers(stage))
            {
                var x=originals[o.Id];
                Check(Math.Abs(o.ScrapReward-x.ScrapReward)<=x.RewardJitter,"jitter escaped source bounds");
                Check(o.Risk==R5RunPlan.RiskName(o),"risk label mismatch "+o.Id);
            }
        });
        Run("generation-does-not-mutate-authored-input-data",() =>
        {
            var c=Fresh(),before=JsonUtility.ToJson(c);
            _=new R5RunPlan(c.Stages,83);
            Check(JsonUtility.ToJson(c)==before,"original recipe mutated");
        });
        Run("reject-invalid-elite-archetype-pair",() =>
        {
            var c=Fresh();c.Stages[3].Options[0].Roster[0]=R3EnemyArchetype.Scout;
            Rejected(()=>R5RunPlan.Validate(c.Stages),"elite gunner must remain gunner");
        });
        Run("reject-invalid-alternate-elite-roster",() =>
        {
            var c=Fresh();c.Stages[3].Options[0].AlternativeRoster[0]=R3EnemyArchetype.Flanker;
            Rejected(()=>R5RunPlan.Validate(c.Stages),"alternate must keep elite gunner aligned");
        });
        Run("reject-duplicate-id",() =>
        {
            var c=Fresh();c.Stages[1].Options[0].Id=c.Stages[0].Options[0].Id;
            Rejected(()=>R5RunPlan.Validate(c.Stages),"duplicate encounter identifier");
        });
        Run("reject-budget-exceeded-even-for-alternative",() =>
        {
            var c=Fresh();c.Stages[4].Options[1].ThreatBudget=.1f;
            Rejected(()=>R5RunPlan.Validate(c.Stages),"impossible source threat budget");
        });
        Run("reject-missing-stage",() =>
        {
            var c=Fresh();c.Stages=c.Stages.Take(5).ToArray();
            Rejected(()=>R5RunPlan.Validate(c.Stages),"five stages forbidden");
        });
        Run("reject-third-route-on-stage",() =>
        {
            var c=Fresh();c.Stages[1].Options=c.Stages[1].Options.Concat(
                new[]{Copy(c).Stages[0].Options[0]}).ToArray();
            Rejected(()=>R5RunPlan.Validate(c.Stages),"more than two options");
        });
        Run("reject-negative-or-unbounded-reward-jitter",() =>
        {
            var c=Fresh();c.Stages[0].Options[0].RewardJitter=c.Stages[0].Options[0].ScrapReward+1;
            Rejected(()=>R5RunPlan.Validate(c.Stages),"reward jitter exceeds reward");
        });
        Run("reject-invalid-arena-enum",() =>
        {
            var c=Fresh();c.Stages[0].Options[0].Arena=(R5ArenaKind)12345;
            Rejected(()=>R5RunPlan.Validate(c.Stages),"unknown arena");
        });
        Run("reject-invalid-nonfinite-mobility",() =>
        {
            var c=Fresh();c.Stages[0].Options[0].MoveSpeed=float.NaN;
            Rejected(()=>R5RunPlan.Validate(c.Stages),"NaN move speed");
        });
        Console.WriteLine("B01-24 ENEMY-RELAY-001 original-C# " + passes +
            " PASS / " + failures + " FAIL; native Unity/PhysX/Windows Player NOT_RUN.");
        return failures==0 ? 0 : 1;
    }
}
