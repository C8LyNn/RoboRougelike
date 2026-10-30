using System;
using B01.UXFeed;
using System.Collections.Generic;

static class Program
{
    static int pass,fail;
    static TacticalInput E(long seq,long ms,TacticalKind kind=TacticalKind.CoreHit,string id="core",int amount=1,int run=7)
    {return new TacticalInput{Sequence=seq,TimeMs=ms,Kind=kind,SubjectId=id,Amount=amount,RunId=run};}
    static TacticalFeed F(int cap=32,int slots=3){return new TacticalFeed(7,cap,slots);}
    static void Check(string name,Func<bool> action)
    {
        try{if(!action())throw new Exception("assert false");Console.WriteLine("PASS "+name);pass++;}
        catch(Exception e){Console.WriteLine("FAIL "+name+": "+e.Message);fail++;}
    }
    static bool Throws(Action action)
    {try{action();return false;}catch(ArgumentException){return true;}}
    static void Main()
    {
        var feed=F();
        Check("Start history empty",()=>feed.Count==0&&feed.History().Length==0);
        Check("Start visible empty",()=>feed.Visible().Length==0);
        Check("Starting session id",()=>feed.RunId==7);
        Check("Starting clock",()=>feed.NowMs==0);
        Check("CoreHit normal category",()=>TacticalFeed.PriorityOf(TacticalKind.CoreHit)==TacticalPriority.Normal);
        Check("Detached critical category",()=>TacticalFeed.PriorityOf(TacticalKind.PartDetached)==TacticalPriority.Critical);
        Check("Weapon offline critical category",()=>TacticalFeed.PriorityOf(TacticalKind.WeaponOffline)==TacticalPriority.Critical);
        Check("Enemy destroyed normal",()=>TacticalFeed.PriorityOf(TacticalKind.EnemyDestroyed)==TacticalPriority.Normal);
        Check("Salvage normal",()=>TacticalFeed.PriorityOf(TacticalKind.SalvageRecovered)==TacticalPriority.Normal);
        Check("Objective normal",()=>TacticalFeed.PriorityOf(TacticalKind.ObjectiveChanged)==TacticalPriority.Normal);
        Check("Unknown category rejects",()=>Throws(()=>TacticalFeed.PriorityOf((TacticalKind)44)));
        Check("First accepted",()=>feed.Record(E(1,100,amount:8))==RecordOutcome.Accepted);
        Check("Correct first payload",()=>feed.History()[0].TotalAmount==8&&feed.History()[0].Occurrences==1);
        Check("First sequence/time kept",()=>feed.History()[0].FirstSequence==1&&feed.History()[0].FirstTimeMs==100);
        Check("Clock advances on accepted event",()=>feed.NowMs==100);
        Check("Same core damage coalesces <=750ms",()=>feed.Record(E(2,800,amount:5))==RecordOutcome.Coalesced);
        Check("Coalesced total",()=>feed.History()[0].TotalAmount==13);
        Check("Coalesced occurrences",()=>feed.History()[0].Occurrences==2);
        Check("Coalesced first timestamp pinned",()=>feed.History()[0].FirstTimeMs==100);
        Check("Coalesced last timestamp pinned",()=>feed.History()[0].LastTimeMs==800);
        Check("Coalesced first sequence pinned",()=>feed.History()[0].FirstSequence==1&&feed.History()[0].LastSequence==2);
        Check("Coalescing uses last timestamp",()=>feed.Record(E(3,1500,amount:1))==RecordOutcome.Coalesced);
        Check("Three chained hits total and count",()=>feed.Count==1&&feed.History()[0].TotalAmount==14&&feed.History()[0].Occurrences==3);
        Check("Combine outside window creates distinct event",()=>feed.Record(E(4,2251,amount:2))==RecordOutcome.Accepted);
        Check("New event holds exact own amount",()=>feed.Count==2&&feed.History()[1].TotalAmount==2);
        Check("Different enemy id never coalesces",()=>feed.Record(E(5,2300,id:"enemy-01"))==RecordOutcome.Accepted);
        Check("No coalescing across different event kind",()=>feed.Record(E(6,2400,TacticalKind.SalvageRecovered,id:"enemy-01"))==RecordOutcome.Accepted);
        Check("Salvage same source merges",()=>feed.Record(E(7,2900,TacticalKind.SalvageRecovered,id:"enemy-01",amount:3))==RecordOutcome.Coalesced);
        Check("Salvage aggregated amount",()=>feed.History()[3].TotalAmount==4);
        Check("Not coalescing critical module detach",()=>feed.Record(E(8,3000,TacticalKind.PartDetached,"wheel-01"))==RecordOutcome.Accepted);
        Check("Repeated critical stays separate",()=>feed.Record(E(9,3100,TacticalKind.PartDetached,"wheel-01"))==RecordOutcome.Accepted);
        Check("Critical priority overtakes recent normal",()=>{
            var x=feed.Visible();return x.Length==3&&x[0].Kind==TacticalKind.PartDetached&&x[1].Kind==TacticalKind.PartDetached;
        });
        Check("Critical-only view never includes normal",()=>{
            var x=feed.Visible(false);return x.Length==2&&x[0].Priority==TacticalPriority.Critical&&x[1].Priority==TacticalPriority.Critical;
        });
        Check("History maintains original append order",()=>{
            var h=feed.History();return h[0].FirstSequence==1&&h[h.Length-1].FirstSequence==9;
        });
        Check("Snapshot cannot alter internal list",()=>{
            var h=feed.History();var count=feed.Count;h[0]=null;return feed.Count==count&&feed.History()[0]!=null;
        });
        Check("Duplicate sequence rejected",()=>Throws(()=>feed.Record(E(9,3300,TacticalKind.ObjectiveChanged,"x"))));
        Check("Older sequence rejected",()=>Throws(()=>feed.Record(E(7,3300,TacticalKind.ObjectiveChanged,"x"))));
        Check("New event after rejected sequence succeeds",()=>feed.Record(E(10,3300,TacticalKind.ObjectiveChanged,"x"))==RecordOutcome.Accepted);
        Check("Old time rejected",()=>Throws(()=>feed.Record(E(11,3200))));
        Check("Rejected event leaves sequence untouched",()=>feed.Record(E(11,3300,TacticalKind.EnemyDestroyed,"foe-7"))==RecordOutcome.Accepted);
        Check("Clock time cannot decrease",()=>Throws(()=>feed.AdvanceTo(3299)));
        Check("Clock time can advance",()=>{feed.AdvanceTo(4200);return feed.NowMs==4200;});
        Check("Reject new event predating live clock",()=>Throws(()=>feed.Record(E(12,4199))));
        Check("Accept event at current clock",()=>feed.Record(E(12,4200,TacticalKind.EnemyDestroyed,"foe-8"))==RecordOutcome.Accepted);
        Check("Old normal TTL expires at exact 3000",()=>{
            var f=F();f.Record(E(1,10,TacticalKind.ObjectiveChanged,"objective"));f.AdvanceTo(3010);return f.Visible().Length==0&&f.Count==1;
        });
        Check("Normal TTL still visible at last millisecond",()=>{
            var f=F();f.Record(E(1,10,TacticalKind.ObjectiveChanged,"objective"));f.AdvanceTo(3009);return f.Visible().Length==1;
        });
        Check("Critical TTL persists beyond normal",()=>{
            var f=F();f.Record(E(1,10,TacticalKind.WeaponOffline,"gun-A"));f.AdvanceTo(5010);return f.Visible().Length==1;
        });
        Check("Critical expires exact 6500",()=>{
            var f=F();f.Record(E(1,10,TacticalKind.WeaponOffline,"gun-A"));f.AdvanceTo(6510);return f.Visible().Length==0&&f.Count==1;
        });
        Check("Expired history remains accessible",()=>{
            var f=F();f.Record(E(1,10));f.AdvanceTo(9000);return f.Visible().Length==0&&f.History().Length==1;
        });
        Check("History capacity bounded oldest dropped",()=>{
            var f=F(2,2);f.Record(E(1,1,TacticalKind.EnemyDestroyed,"e1"));f.Record(E(2,2,TacticalKind.EnemyDestroyed,"e2"));f.Record(E(3,3,TacticalKind.EnemyDestroyed,"e3"));
            return f.Count==2&&f.History()[0].SubjectId=="e2"&&f.History()[1].SubjectId=="e3";
        });
        Check("Critical feeds do not exceed visible slots",()=>{
            var f=F(8,1);f.Record(E(1,1,TacticalKind.PartDetached,"p1"));f.Record(E(2,2,TacticalKind.WeaponOffline,"p2"));return f.Visible().Length==1&&f.Visible()[0].SubjectId=="p2";
        });
        Check("Visible normal newest first",()=>{
            var f=F();f.Record(E(1,1,TacticalKind.EnemyDestroyed,"e1"));f.Record(E(2,2,TacticalKind.EnemyDestroyed,"e2"));return f.Visible()[0].SubjectId=="e2";
        });
        Check("Visible critical before newer normal",()=>{
            var f=F();f.Record(E(1,1,TacticalKind.PartDetached,"p1"));f.Record(E(2,2,TacticalKind.EnemyDestroyed,"e2"));return f.Visible()[0].SubjectId=="p1";
        });
        Check("UI-visible returns detached array",()=>{
            var f=F();f.Record(E(1,1,TacticalKind.EnemyDestroyed,"e1"));var x=f.Visible();x[0]=null;return f.Visible()[0]!=null;
        });
        Check("New run clears all history/time",()=>{
            var f=F();f.Record(E(1,200));f.StartNewRun(8);return f.Count==0&&f.RunId==8&&f.NowMs==0;
        });
        Check("New run sequence starts independently",()=>{
            var f=F();f.Record(E(5,500));f.StartNewRun(8);return f.Record(E(1,1,run:8))==RecordOutcome.Accepted;
        });
        Check("Old-run packet ignored, even with invalid stale actor",()=>{
            var f=F();f.StartNewRun(8);return f.Record(E(-4,-20,run:7,id:"<bad>"))==RecordOutcome.IgnoredPreviousRun&&f.Count==0;
        });
        Check("Future-run packet refused without silently switching",()=>{
            var f=F();return Throws(()=>f.Record(E(1,1,run:8)))&&f.RunId==7;
        });
        Check("Cannot restart same run",()=>Throws(()=>F().StartNewRun(7)));
        Check("Cannot restart older run",()=>Throws(()=>F().StartNewRun(6)));
        Check("Invalid negative initial run",()=>Throws(()=>new TacticalFeed(-1)));
        Check("Zero history capacity rejected",()=>Throws(()=>new TacticalFeed(0,0)));
        Check("Overlarge history capacity rejected",()=>Throws(()=>new TacticalFeed(0,257)));
        Check("Zero display slots rejected",()=>Throws(()=>new TacticalFeed(0,10,0)));
        Check("Display slots cannot exceed cap",()=>Throws(()=>new TacticalFeed(0,1,2)));
        Check("Display slots max 6",()=>Throws(()=>new TacticalFeed(0,20,7)));
        Check("Null input rejected",()=>Throws(()=>F().Record(null)));
        Check("Unknown event kind rejected",()=>Throws(()=>F().Record(E(1,1,(TacticalKind)999))));
        Check("Zero sequence rejected",()=>Throws(()=>F().Record(E(0,1))));
        Check("Negative sequence rejected",()=>Throws(()=>F().Record(E(-1,1))));
        Check("Negative tick rejected",()=>Throws(()=>F().Record(E(1,-1))));
        Check("Zero amount rejected",()=>Throws(()=>F().Record(E(1,1,amount:0))));
        Check("Negative amount rejected",()=>Throws(()=>F().Record(E(1,1,amount:-1))));
        Check("Overlarge amount rejected",()=>Throws(()=>F().Record(E(1,1,amount:1000001))));
        Check("Null subject rejected",()=>Throws(()=>F().Record(E(1,1,id:null))));
        Check("Empty subject rejected",()=>Throws(()=>F().Record(E(1,1,id:""))));
        Check("Whitespaced subject rejected",()=>Throws(()=>F().Record(E(1,1,id:"foo bar"))));
        Check("Markup-like subject rejected",()=>Throws(()=>F().Record(E(1,1,id:"<script>"))));
        Check("Long subject rejected",()=>Throws(()=>F().Record(E(1,1,id:new string('a',65)))));
        Check("64-char valid subject accepted",()=>F().Record(E(1,1,id:new string('a',64)))==RecordOutcome.Accepted);
        Check("Valid HUD id punctuation accepted",()=>F().Record(E(1,1,id:"player:wheel-1.left"))==RecordOutcome.Accepted);
        Check("Non-combinable enemy kills never deduplicate",()=>{
            var f=F();f.Record(E(1,10,TacticalKind.EnemyDestroyed,"e"));f.Record(E(2,11,TacticalKind.EnemyDestroyed,"e"));return f.Count==2;
        });
        Check("Different kind after coalescing prevents later merge",()=>{
            var f=F();f.Record(E(1,10));f.Record(E(2,20,TacticalKind.ObjectiveChanged,"o"));f.Record(E(3,30));return f.Count==3;
        });
        Check("No floating point clock dependencies",()=>TacticalFeed.CombineWindowMs==750&&TacticalFeed.NormalTtlMs==3000&&TacticalFeed.CriticalTtlMs==6500);
        Check("Very large timestamps do not overflow TTL",()=>{
            var f=F();f.AdvanceTo(long.MaxValue-5);f.Record(E(1,long.MaxValue-5,TacticalKind.WeaponOffline,"gun"));return f.Visible().Length==1;
        });
        Check("Very large sequence survives sorting",()=>{
            var f=F();f.Record(E(long.MaxValue-1,1,TacticalKind.WeaponOffline,"gun"));return f.Record(E(long.MaxValue,2,TacticalKind.WeaponOffline,"gun"))==RecordOutcome.Accepted;
        });
        Check("Record object is immutable view type",()=>{
            var item=F().History();return item.Length==0&&typeof(TacticalItem).GetField("TotalAmount").IsInitOnly;
        });
        Console.WriteLine("RESULT "+pass+" PASS, "+fail+" FAIL");
        if(fail!=0)Environment.Exit(1);
    }
}
