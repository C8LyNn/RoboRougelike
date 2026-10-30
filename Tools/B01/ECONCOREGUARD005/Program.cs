using System;
using System.IO;
using System.Linq;
using RoboRougelike.B01.Economy.RebuildV3;
using RoboRougelike.B01.Economy.ReadinessV4;
using RoboRougelike.B01.Economy.CoreGuardV5;

// Reuse the original frozen W22 disk owner and physical part fixture classes.
sealed class W23Probe : ICanonicalCoreAndEncounterProbeV5
{
    public string CoreId=Test.B,Owner,Run;
    public long GenerationOffset;
    public bool Functional=true,CanDeploy=true,Next=true,Fail;
    public string[] Blockers=Array.Empty<string>();
    public Action<OwnerRebuildStateV3> During;
    public bool TryObserve(OwnerRebuildStateV3 s,out CanonicalCoreAndEncounterV5 e,out string why)
    {
        During?.Invoke(s);
        e=Fail?null:new CanonicalCoreAndEncounterV5{
            OwnerId=Owner??s.OwnerId,RunId=Run??s.RunId,
            OwnerGeneration=s.Generation+GenerationOffset,
            CorePhysicalPartId=CoreId,CoreFunctional=Functional,
            CanDeploy=CanDeploy,HasGenuineNextChallenge=Next,
            BlockingPhysicalIds=Blockers
        };
        why=Fail?"CANONICAL_NOT_AVAILABLE":"OK";return !Fail;
    }
}
static class W23Checks
{
    static int count;
    static void Check(bool x,string label)
    {if(!x)throw new Exception("ASSERT_FAIL "+label);count++;}
    static EconomyRunReadinessGateV4 Gate(string p,W23Probe probe)=>
        new EconomyRunReadinessGateV4(new DurableTestOwner(p),
          new CoreGuardedDeploymentProbeV5(probe),"owner1","run1");
    static ReadinessReportV4 Read(EconomyRunReadinessGateV4 gate)
    {
        var good=gate.TryEvaluate(out var r,out var why);
        Check(good,"read "+why);return r;
    }
    static void Deny(EconomyRunReadinessGateV4 gate,string reason)
    {
        bool accepted=gate.TryEvaluate(out var report,out _);
        Check(!accepted&&report.Verdict==ReadinessVerdictV4.Unverified&&
             !report.AuthorizesCombatStart,"deny "+reason);
    }
    static void Scenario(string file)
    {
        DurableTestOwner.Initialize(file);
        var bytes=File.ReadAllBytes(file);
        var original=DurableTestOwner.Load(file);
        var facts=new W23Probe();
        var gate=Gate(file,facts);
        var second=Gate(file,facts);
        Check(Read(gate).Verdict==ReadinessVerdictV4.NormalChallengeReady,
              "genuine living Core and genuine challenge");
        Check(Read(second).Generation==3,"same owner, two observers, no new authority");
        facts.Next=false;
        Check(Read(gate).Verdict==ReadinessVerdictV4.NoGenuineChallengeWhilePending,
              "pending wheel without any actual next encounter");
        facts.Next=true;facts.CanDeploy=false;facts.Blockers=new[]{Test.A};
        Check(Read(gate).Verdict==ReadinessVerdictV4.CriticalPending0HpBlock,
              "essential pending wheel cannot enter next fight");
        var dead=original.Copy();
        dead.Parts.Single(x=>x.Id==Test.B).Health=0;
        DurableTestOwner.Write(file,dead);
        facts.Functional=false;facts.Blockers=new[]{Test.B};
        Check(Read(gate).Verdict==ReadinessVerdictV4.CriticalUnqueued0HpBlock,
              "dead physical Core blocks real deploy, no instant repair");
        facts.Blockers=new[]{Test.A,Test.B};
        Check(Read(gate).Verdict==ReadinessVerdictV4.CriticalPending0HpBlock,
              "dead Core plus queued wheel is still unavailable");
        facts.Next=false;
        Check(Read(gate).Verdict==ReadinessVerdictV4.NoGenuineChallengeWhilePending,
              "no real encounter while Core dead; cannot forge an extra win");
        facts.Next=true;facts.Functional=true;
        Deny(gate,"0HP Core claimed functional");
        facts.Functional=false;facts.CanDeploy=true;
        Deny(gate,"disabled Core claimed deployable");
        facts.CanDeploy=false;facts.Blockers=Array.Empty<string>();
        Deny(gate,"dead Core missing from blockers");
        facts.Blockers=new[]{Test.B,Test.B};
        Deny(gate,"duplicate blocker");
        facts.Blockers=new[]{Test.X};
        Deny(gate,"unowned blocker");
        facts.Blockers=new[]{Test.B};
        facts.CoreId=Test.X;
        Deny(gate,"foreign alleged Core GUID");
        facts.CoreId=Test.B;facts.GenerationOffset=1;
        Deny(gate,"wrong owner revision");
        facts.GenerationOffset=0;facts.Owner="other";
        Deny(gate,"wrong owner");
        facts.Owner=null;facts.Run="different";
        Deny(gate,"wrong run");
        facts.Run=null;facts.Fail=true;
        Deny(gate,"missing canonical proof");
        facts.Fail=false;
        facts.During=s=>{s.Scrap=9999;s.Parts[0].Health=1;};
        Check(Read(gate).Scrap==350,"probe receives only a defensive copy");
        facts.During=null;
        facts.During=_=> {
            var fork=DurableTestOwner.Load(file);
            fork.Scrap=400;DurableTestOwner.Write(file,fork);
        };
        Deny(gate,"same-generation fork during observation");
        facts.During=null;
        DurableTestOwner.Write(file,original);
        facts.Functional=true;facts.CanDeploy=false;facts.Blockers=new[]{Test.B};
        Deny(gate,"healthy Core fraudulently marked zero HP");
        facts.CanDeploy=true;facts.Blockers=Array.Empty<string>();
        var ready=Read(gate);
        Check(ready.Verdict==ReadinessVerdictV4.NormalChallengeReady&&
              !ready.AuthorizesCombatStart,"even ready result never grants entry");
        Check(File.ReadAllBytes(file).SequenceEqual(bytes),
              "no real disk mutation by guard");
        Console.WriteLine("PASS W23 original V3/V4/V5 Core proof two observers, dead Core, next encounter, equal generation fork");
    }
    static void Model()
    {
        int wins=12,ordered=2*wins,restored=2*(wins-1),quote=70;
        Check(ordered==24&&restored==22&&ordered-restored==2,
              "NEXT distinct win only");
        Check((ordered-wins)*quote==840&&(ordered-1)*quote==1610,
              "hypothetical wallet pricing, NOT adopted");
        Console.WriteLine("MODEL_ONLY wins=12 ordered=24 completed=22 pending=2 assumed_cost=840_vs_1610 human_participants=0");
    }
    static int Main(string[] args)
    {
        if(args.Length!=2)return 2;
        try
        {
            if(args[0]=="--scenario"){Scenario(args[1]);Model();}
            else if(args[0]=="--cold")
            {
                var probe=new W23Probe();
                var gate=Gate(args[1],probe);
                Check(Read(gate).Verdict==ReadinessVerdictV4.NormalChallengeReady,
                      "independent cold process recovers same owner without new win");
                probe.Next=false;
                Check(Read(gate).Verdict==ReadinessVerdictV4.NoGenuineChallengeWhilePending,
                      "no challenge remains no challenge after restart");
                Console.WriteLine("PASS W23 cold OS process Core and next event checks");
            }
            else return 2;
            Console.WriteLine("ECON_W23_CSHARP_SOURCE_PASS checks="+count+
                " G1_NATIVE_NOT_RUN CORE_HOST_NOT_WIRED");
            return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
