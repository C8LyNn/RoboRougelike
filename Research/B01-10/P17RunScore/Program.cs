using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RoboRougelike.B0110.RunScore;

internal static class Program
{
    private static int passed, failed;
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions {
        WriteIndented = true, PropertyNamingPolicy = null,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
    private static StageScoreInput Stage(int number = 1, float threat = 6f)
    {
        return new StageScoreInput {
            StageNumber = number, EncounterId = "test_" + number,
            Victory = true, Threat = threat,
            ExpectedEnemies = 2, DefeatedEnemies = 2,
            HullRatioStart = 1f, HullRatioEnd = .80f,
            DurationSeconds = 60f, ScrapEarned = 80, RecoveredModules = 1
        };
    }
    private static List<StageScoreInput> Six()
    {
        return Enumerable.Range(1, 6).Select(i => Stage(i, 6 + i)).ToList();
    }
    private static void Check(bool ok, string description)
    {
        if (!ok) throw new InvalidOperationException(description);
    }
    private static void Equal<T>(T actual, T expected)
    {
        Check(EqualityComparer<T>.Default.Equals(actual, expected),
            "expected " + expected + ", got " + actual);
    }
    private static bool Reject(Action action)
    {
        try { action(); return false; }
        catch (ArgumentException) { return true; }
    }
    private static void Test(string name, Action action)
    {
        try {
            action(); passed++; Console.WriteLine("PASS " + passed.ToString("D2") + " " + name);
        } catch (Exception ex) {
            failed++; Console.Error.WriteLine("FAIL " + name + " :: " + ex.Message);
        }
    }
    private static int TestAll()
    {
        Test("01 first-stage victory earns real points", () => Check(RunScorer.ScoreStage(Stage()).Total > 0, "score must be positive"));
        Test("02 source bonus points sum exactly", () => {
            var x = RunScorer.ScoreStage(Stage());
            Equal(x.Total, x.BasePoints + x.TimeBonus + x.HullBonus + x.SalvageBonus);
        });
        Test("03 baseline stage=1 threat=6 base 340", () => Equal(RunScorer.ScoreStage(Stage()).BasePoints, 340));
        Test("04 ordinary 80pct hull awards 64", () => Equal(RunScorer.ScoreStage(Stage()).HullBonus, 64));
        Test("05 recovered one module awards 15", () => Equal(RunScorer.ScoreStage(Stage()).SalvageBonus, 15));
        Test("06 full run has exact 6 stages", () => Equal(RunScorer.ScoreRun(Six()).Stages.Length, 6));
        Test("07 complete six victories is won", () => Check(RunScorer.ScoreRun(Six()).RunWon, "run not won"));
        Test("08 5 stages remain in progress", () => {
            var s = Six(); s.RemoveAt(5);
            var r = RunScorer.ScoreRun(s);
            Check(!r.RunWon && !r.RunLost && r.Medal == RunMedal.None, "incomplete medal");
        });
        Test("09 early loss awards no extra score", () => {
            var s = Stage(); s.Victory = false; s.DefeatedEnemies = 1; s.HullRatioEnd = 0;
            var x = RunScorer.ScoreStage(s);
            Equal(x.Total, 0); Equal(x.Medal, RunMedal.None);
        });
        Test("10 run terminates on loss", () => {
            var s = Six(); s[3].Victory = false; s[3].DefeatedEnemies = 0; s.RemoveRange(4, 2);
            var r = RunScorer.ScoreRun(s);
            Check(r.RunLost && !r.RunWon && r.CompletedStages == 3, "loss semantics");
        });
        Test("11 no stages can follow loss", () => {
            var s = Six(); s[1].Victory = false; s[1].DefeatedEnemies = 0;
            Check(Reject(() => RunScorer.ScoreRun(s)), "post-defeat stage accepted");
        });
        Test("12 reward displays but cannot farm score", () => {
            var a = Stage(); var b = Stage(); b.ScrapEarned = 100000;
            Equal(RunScorer.ScoreStage(a).Total, RunScorer.ScoreStage(b).Total);
        });
        Test("13 speed bonus at par full 60", () => {
            var s = Stage(); s.DurationSeconds = RunScorer.ParSeconds(1, 6);
            Equal(RunScorer.ScoreStage(s).TimeBonus, 60);
        });
        Test("14 speed bonus at double par zero", () => {
            var s = Stage(); s.DurationSeconds = 2 * RunScorer.ParSeconds(1, 6);
            Equal(RunScorer.ScoreStage(s).TimeBonus, 0);
        });
        Test("15 faster cannot decrease score", () => {
            var a = Stage(); var b = Stage(); a.DurationSeconds = 200; b.DurationSeconds = 30;
            Check(RunScorer.ScoreStage(b).Total >= RunScorer.ScoreStage(a).Total, "speed non-monotonic");
        });
        Test("16 no time bonus above double par", () => {
            var s = Stage(); s.DurationSeconds = 2000;
            Equal(RunScorer.ScoreStage(s).TimeBonus, 0);
        });
        Test("17 survival improvement cannot reduce score", () => {
            var a = Stage(); var b = Stage(); a.HullRatioEnd = .20f; b.HullRatioEnd = .95f;
            Check(RunScorer.ScoreStage(b).Total >= RunScorer.ScoreStage(a).Total, "hull non-monotonic");
        });
        Test("18 no points for healing beyond 100pct", () => {
            var s = Stage(); s.HullRatioEnd = 1.01f;
            Check(Reject(() => RunScorer.ScoreStage(s)), "hull ratio overflow accepted");
        });
        Test("19 increasing threat raises base", () => {
            var a = Stage(); var b = Stage(1, 18);
            Check(RunScorer.ScoreStage(b).BasePoints > RunScorer.ScoreStage(a).BasePoints, "risk not rewarded");
        });
        Test("20 later stages have higher base", () => {
            Check(RunScorer.ScoreStage(Stage(6)).BasePoints > RunScorer.ScoreStage(Stage(1)).BasePoints, "bad");
        });
        Test("21 salvage points bounded 0..45", () => {
            var a = Stage(); a.RecoveredModules = 0;
            var b = Stage(); b.RecoveredModules = 3;
            Equal(RunScorer.ScoreStage(b).SalvageBonus - RunScorer.ScoreStage(a).SalvageBonus, 45);
        });
        Test("22 impossible salvaged 4 refused", () => {
            var s = Stage(); s.RecoveredModules = 4;
            Check(Reject(() => RunScorer.ScoreStage(s)), "accepted 4 parts");
        });
        Test("23 duplicate per-run encounter IDs rejected", () => {
            var s = Six(); s[1].EncounterId = s[0].EncounterId;
            Check(Reject(() => RunScorer.ScoreRun(s)), "dupe accepted");
        });
        Test("24 first stage must be one", () => {
            var s = Stage(2);
            Check(Reject(() => RunScorer.ScoreRun(new[]{s})), "bad start accepted");
        });
        Test("25 stages must be sequential", () => {
            var s = Six(); s[2].StageNumber = 5;
            Check(Reject(() => RunScorer.ScoreRun(s)), "gap accepted");
        });
        Test("26 victory requires all spawned enemies defeated", () => {
            var s = Stage(); s.DefeatedEnemies = 1;
            Check(Reject(() => RunScorer.ScoreStage(s)), "false victory accepted");
        });
        Test("27 overcount killed enemies refused", () => {
            var s = Stage(); s.DefeatedEnemies = 3;
            Check(Reject(() => RunScorer.ScoreStage(s)), "farm accepted");
        });
        Test("28 defeated with no kills accepted", () => {
            var s = Stage(); s.Victory = false; s.DefeatedEnemies = 0; s.HullRatioEnd = 0;
            Equal(RunScorer.ScoreStage(s).Total, 0);
        });
        Test("29 victory with dead hull refused", () => {
            var s = Stage(); s.HullRatioEnd = 0;
            Check(Reject(() => RunScorer.ScoreStage(s)), "invalid victory");
        });
        Test("30 NaN threat rejected", () => {
            var s = Stage(); s.Threat = float.NaN;
            Check(Reject(() => RunScorer.ScoreStage(s)), "NaN accepted");
        });
        Test("31 positive infinite threat rejected", () => {
            var s = Stage(); s.Threat = float.PositiveInfinity;
            Check(Reject(() => RunScorer.ScoreStage(s)), "infinite accepted");
        });
        Test("32 negative time rejected", () => {
            var s = Stage(); s.DurationSeconds = -1;
            Check(Reject(() => RunScorer.ScoreStage(s)), "negative time accepted");
        });
        Test("33 zero duration rejected", () => {
            var s = Stage(); s.DurationSeconds = 0;
            Check(Reject(() => RunScorer.ScoreStage(s)), "zero time accepted");
        });
        Test("34 null encounter ID rejected", () => {
            var s = Stage(); s.EncounterId = null;
            Check(Reject(() => RunScorer.ScoreStage(s)), "null id accepted");
        });
        Test("35 no stages rejected", () =>
            Check(Reject(() => RunScorer.ScoreRun(new StageScoreInput[0])), "empty accepted"));
        Test("36 seventh stage rejected", () => {
            var s = Six(); s.Add(Stage(7));
            Check(Reject(() => RunScorer.ScoreRun(s)), "7 stages accepted");
        });
        Test("37 null item rejected", () =>
            Check(Reject(() => RunScorer.ScoreRun(new StageScoreInput[]{null})), "null item accepted"));
        Test("38 100k reward bound allowed", () => {
            var s = Stage(); s.ScrapEarned = 100000;
            Check(RunScorer.ScoreStage(s).Total > 0, "max reward refused");
        });
        Test("39 negative earned scrap refused", () => {
            var s = Stage(); s.ScrapEarned = -1;
            Check(Reject(() => RunScorer.ScoreStage(s)), "negative accepted");
        });
        Test("40 par grows with difficulty", () =>
            Check(RunScorer.ParSeconds(6, 30) > RunScorer.ParSeconds(1, 6), "par scaling"));
        Test("41 all gold awards gold run medal", () =>
            Equal(RunScorer.ScoreRun(Six()).Medal, RunMedal.Gold));
        Test("42 slow no-hull run only bronze", () => {
            var s = Six();
            foreach(var t in s){t.DurationSeconds=1000; t.HullRatioEnd=.10f; t.RecoveredModules=0;}
            Equal(RunScorer.ScoreRun(s).Medal, RunMedal.Bronze);
        });
        Test("43 full par condition achievements", () => {
            var r = RunScorer.ScoreRun(Six());
            Check(r.Achievements.Contains("PAR_MASTER") && r.Achievements.Contains("HULL_KEEPER"), "missing achievements");
        });
        Test("44 2 modules per stage earns salvage achievement", () => {
            var s = Six();foreach(var t in s)t.RecoveredModules=2;
            Check(RunScorer.ScoreRun(s).Achievements.Contains("SALVAGE_EXPERT"), "missing salvage achievement");
        });
        Test("45 partial runs never receive achievements", () => {
            var s = Six();s.RemoveAt(5);
            Equal(RunScorer.ScoreRun(s).Achievements.Length, 0);
        });
        Test("46 defeated six-stage run never receives medals", () => {
            var s = Six();s[5].Victory=false;s[5].DefeatedEnemies=1;s[5].HullRatioEnd=0;
            var r = RunScorer.ScoreRun(s);
            Check(r.Medal==RunMedal.None && r.Achievements.Length==0, "false completion");
        });
        Test("47 scores consistent with sum", () => {
            var r=RunScorer.ScoreRun(Six());
            Equal(r.Total,r.Stages.Sum(x=>x.Total));
        });
        Test("48 stable exact output under repeated evaluation", () => {
            var input=Six();var a=RunScorer.ScoreRun(input);var b=RunScorer.ScoreRun(input);
            Equal(JsonSerializer.Serialize(a,JsonOptions),JsonSerializer.Serialize(b,JsonOptions));
        });
        Test("49 source does not mutate six stage input", () => {
            var input=Six();string before=JsonSerializer.Serialize(input,JsonOptions);
            RunScorer.ScoreRun(input);
            Equal(before,JsonSerializer.Serialize(input,JsonOptions));
        });
        Test("50 localized PL/EN run outputs", () => {
            var r=RunScorer.ScoreRun(Six());
            Equal(r.StatusPl,"Ukończona wyprawa");Equal(r.StatusEn,"Run completed");
        });
        Test("51 stage medal localization is correct", () => {
            var s=RunScorer.ScoreStage(Stage());
            Equal(s.LabelPl,"Złoto");Equal(s.LabelEn,"Gold");
        });
        Test("52 reject unsupported huge threat", () => {
            var s=Stage(1,101);
            Check(Reject(()=>RunScorer.ScoreStage(s)), "threat above 100 accepted");
        });
        Test("53 negative HP ratio rejected", () => {
            var s=Stage();s.HullRatioStart=-.5f;
            Check(Reject(()=>RunScorer.ScoreStage(s)), "negative HP accepted");
        });
        Test("54 maximum legal 4-spawn fight", () => {
            var s=Stage();s.ExpectedEnemies=4;s.DefeatedEnemies=4;
            Check(RunScorer.ScoreStage(s).Victory,"4-spawn accepted");
        });
        Test("55 no time manipulation with NaN", () => {
            var s=Stage();s.DurationSeconds=float.NaN;
            Check(Reject(()=>RunScorer.ScoreStage(s)), "NaN duration accepted");
        });
        Test("56 run survives deterministic 120-seed input variety", () => {
            for(int seed=1;seed<=120;seed++){
                var stages=Six();
                foreach(var x in stages){
                    x.Threat=1+((seed*3+x.StageNumber*11)%40);
                    x.DurationSeconds=30+((seed*13+x.StageNumber*19)%700);
                    x.HullRatioEnd=.05f+((seed+x.StageNumber)%20)*.045f;
                    x.RecoveredModules=(seed+x.StageNumber)%4;
                }
                var r=RunScorer.ScoreRun(stages);
                Check(r.Stages.Length==6 && r.RunWon && r.Total>=0 && r.Total<=100000,"seed stability "+seed);
            }
        });

        // WAVE04: source-timeline integration gap from real Codex controller:
        // victory wallet/kills are recorded before ConfirmRewards recovers parts.
        Test("57 two-phase victory has no result before rewards", () => {
            var c=new RunScoreCollector(1,"r5_s1",6,2,1,4,300,1);
            c.Advance(90,false);
            Check(c.FinishCombat(true,6,.8f,380)==null,"early scoreboard");
            Check(c.AwaitingRewards && !c.Finished,"wrong reward phase");
        });
        Test("58 final rewards observe actual recovered delta", () => {
            var c=new RunScoreCollector(1,"r5_s1",6,2,1,4,300,1);
            c.Advance(90,false);
            c.FinishCombat(true,6,.8f,380);
            var s=c.FinishRewards(3,395);
            Check(c.Finished&&s.Victory,"should close");
            Equal(s.DefeatedEnemies,2); Equal(s.RecoveredModules,2);
            Equal(s.ScrapEarned,95); Equal(RunScorer.ScoreStage(s).SalvageBonus,30);
        });
        Test("59 reward confirmation cannot precede combat", () => {
            var c=new RunScoreCollector(1,"r5_s1",6,2,1,0,0,0);
            Check(Reject(()=>c.FinishRewards(0,0)),"premature reward accepted");
        });
        Test("60 combat transition cannot be duplicated", () => {
            var c=new RunScoreCollector(1,"r5_s1",6,2,1,0,0,0);
            c.Advance(12,false);c.FinishCombat(true,2,.9f,80);
            Check(Reject(()=>c.FinishCombat(true,2,.9f,80)),"double combat");
        });
        Test("61 reward selection cannot generate double points", () => {
            var c=new RunScoreCollector(1,"r5_s1",6,2,1,0,0,0);
            c.Advance(12,false);c.FinishCombat(true,2,.9f,80);
            c.FinishRewards(1,80);
            Check(Reject(()=>c.FinishRewards(2,90)),"double recovery");
        });
        Test("62 true pause excluded from duration", () => {
            var c=new RunScoreCollector(1,"r5_s1",6,2,1,0,0,0);
            c.Advance(3,false);c.Advance(77,true);c.Advance(2,false);
            c.FinishCombat(true,2,1,80);
            Equal(c.FinishRewards(0,80).DurationSeconds,5f);
        });
        Test("63 no extra time permitted during salvage", () => {
            var c=new RunScoreCollector(1,"r5_s1",6,2,1,0,0,0);
            c.Advance(10,false);c.FinishCombat(true,2,.9f,80);
            Check(Reject(()=>c.Advance(99,false)),"postcombat fake elapsed");
        });
        Test("64 false victory with alive enemy rejected atomically", () => {
            var c=new RunScoreCollector(1,"r5_s1",6,2,1,0,0,0);
            c.Advance(10,false);
            Check(Reject(()=>c.FinishCombat(true,1,.9f,80)),"false victory");
            Check(!c.Finished&&!c.AwaitingRewards,"invalid finish mutated state");
            c.FinishCombat(true,2,.9f,80);
            Equal(c.FinishRewards(1,80).DefeatedEnemies,2);
        });
        Test("65 cumulative kill counter regression rejected", () => {
            var c=new RunScoreCollector(1,"r5_s1",6,2,1,6,300,2);
            c.Advance(10,false);
            Check(Reject(()=>c.FinishCombat(false,5,0,300)),"negative kills");
        });
        Test("66 negative scrap delta rejected with no irreversible close", () => {
            var c=new RunScoreCollector(1,"r5_s1",6,2,1,0,300,0);
            c.Advance(10,false);
            Check(Reject(()=>c.FinishCombat(true,2,.9f,200)),"scrap drop");
            c.FinishCombat(true,2,.9f,390);
            Equal(c.FinishRewards(0,390).ScrapEarned,90);
        });
        Test("67 bad recovered 4 rejects but later valid rewards pass", () => {
            var c=new RunScoreCollector(1,"r5_s1",6,2,1,0,0,0);
            c.Advance(10,false);c.FinishCombat(true,2,.9f,80);
            Check(Reject(()=>c.FinishRewards(4,80)),"four recovery accepted");
            Check(c.AwaitingRewards&&!c.Finished,"bad reward closed state");
            Equal(c.FinishRewards(3,80).RecoveredModules,3);
        });
        Test("68 negative recovered counter mutation refused", () => {
            var c=new RunScoreCollector(1,"r5_s1",6,2,1,0,0,5);
            c.Advance(10,false);c.FinishCombat(true,2,.9f,80);
            Check(Reject(()=>c.FinishRewards(4,80)),"counter rewind accepted");
            Equal(c.FinishRewards(5,80).RecoveredModules,0);
        });
        Test("69 duration over ten hours rejected atomically", () => {
            var c=new RunScoreCollector(1,"r5_s1",6,2,1,0,0,0);
            c.Advance(35999,false);
            Check(Reject(()=>c.Advance(5,false)),"time overrun");
            Equal(c.ActiveSeconds,35999d);
        });
        Test("70 NaN tick rejected", () => {
            var c=new RunScoreCollector(1,"r5_s1",6,2,1,0,0,0);
            Check(Reject(()=>c.Advance(float.NaN,false)),"NaN time");
        });
        Test("71 loss immediately closes without rewarding salvage", () => {
            var c=new RunScoreCollector(1,"r5_s1",6,2,1,2,300,3);
            c.Advance(30,false);
            var s=c.FinishCombat(false,3,0,300);
            Check(c.Finished&&!c.AwaitingRewards&&!s.Victory,"loss not closed");
            Equal(RunScorer.ScoreStage(s).Total,0);
            Check(Reject(()=>c.FinishRewards(4,300)),"reward after defeat");
        });
        Test("72 invalid id refused before any telemetry", () => {
            Check(Reject(()=>new RunScoreCollector(1," ",6,2,1,0,0,0)),"empty id");
        });
        Test("73 separate stage baselines avoid double-counting cumulative recovery", () => {
            var first=new RunScoreCollector(1,"r5_s1",6,2,1,0,0,0);
            first.Advance(25,false);first.FinishCombat(true,2,.8f,80);
            Equal(first.FinishRewards(2,95).RecoveredModules,2);
            var next=new RunScoreCollector(2,"r5_s2",7,3,.8f,2,95,2);
            next.Advance(70,false);next.FinishCombat(true,5,.7f,200);
            var final=next.FinishRewards(3,210);
            Equal(final.DefeatedEnemies,3);Equal(final.RecoveredModules,1);
            Equal(final.ScrapEarned,115);
        });
        Test("74 earned Scrap remains display-only even through collector", () => {
            var a=new RunScoreCollector(1,"r5_s1",6,2,1,0,0,0);
            var b=new RunScoreCollector(1,"r5_s1",6,2,1,0,0,0);
            a.Advance(10,false);b.Advance(10,false);
            a.FinishCombat(true,2,.9f,80);b.FinishCombat(true,2,.9f,500);
            var ra=RunScorer.ScoreStage(a.FinishRewards(1,80));
            var rb=RunScorer.ScoreStage(b.FinishRewards(1,600));
            Equal(ra.Total,rb.Total);
        });
        Console.WriteLine("B01-10 RUNSCORE REAL_CSHARP " + passed
            + " PASS / " + failed + " FAIL | NATIVE_UNITY=NOT_RUN");
        return failed==0?0:1;
    }
    public static int Main(string[] args)
    {
        if(args.Length==1 && args[0]=="--test")return TestAll();
        if(args.Length==2 && args[0]=="--input"){
            try{
                var path=Path.GetFullPath(args[1]);
                var observations=JsonSerializer.Deserialize<List<StageScoreInput>>(File.ReadAllText(path),JsonOptions);
                var scored=RunScorer.ScoreRun(observations);
                Console.WriteLine(JsonSerializer.Serialize(scored,JsonOptions));
                return 0;
            }catch(Exception ex){
                Console.Error.WriteLine("INVALID_INPUT: "+ex.Message);
                return 2;
            }
        }
        Console.Error.WriteLine("USAGE: dotnet run --project Research/B01-10/P17RunScore/RunScore.Runner.csproj -- (--test | --input FILE.json)");
        return 2;
    }
}
