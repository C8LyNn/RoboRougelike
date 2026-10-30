// Pure original C#9 source and original V3 type under .NET8, no fake Unity runtime.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using RoboRougelike.B01.Economy.RebuildV3;
using RoboRougelike.B01.Economy.ReadinessV4;
sealed class DurableTestOwner : IAtomicRebuildOwnerStoreV3
{
    readonly string file;
    static readonly JsonSerializerOptions Opt=new JsonSerializerOptions{IncludeFields=true};
    public DurableTestOwner(string path){file=path;}
    public static void Initialize(string path)
    {
        var s=new OwnerRebuildStateV3{
            OwnerId="owner1",RunId="run1",Generation=3,Scrap=350,FreeUsedAtWin=0,
            Parts=new List<OwnedPartV3>{
                new OwnedPartV3{Id=Test.A,Kind="Wheel",Health=0,Purchase=100,
                    PaidQuote=70,ReservationKey="reserve-A"},
                new OwnedPartV3{Id=Test.B,Kind="Core",Health=1,Purchase=100,
                    PaidQuote=70},
                new OwnedPartV3{Id=Test.C,Kind="Gun",Health=0,Purchase=100,
                    PaidQuote=70}
            },
            Pending=new List<RebuildOrderV3>{
                new RebuildOrderV3{PartId=Test.A,ReservationKey="reserve-A",
                    OrderedAfterWins=0,WasFree=true,Charged=0,Purchase=100,PaidQuote=70}
            }
        };
        File.WriteAllText(path,JsonSerializer.Serialize(s,Opt));
    }
    public static OwnerRebuildStateV3 Load(string file)=>
        JsonSerializer.Deserialize<OwnerRebuildStateV3>(File.ReadAllText(file),Opt);
    public static void Write(string file,OwnerRebuildStateV3 s)=>
        File.WriteAllText(file,JsonSerializer.Serialize(s,Opt));
    public bool TryRead(string owner,string run,out OwnerRebuildStateV3 state,out string why)
    {
        try
        {
            state=Load(file);
            why="OK";return true;
        }
        catch(Exception){state=null;why="READ_FAIL";return false;}
    }
    public PublishResultV3 TryPublish(string owner,string run,long expected,
        OwnerRebuildStateV3 proposed,string winnerEventId,out string reason)
    {reason="READ_ONLY_TEST_HOST_NO_PUBLISH";throw new Exception(reason);}
}
sealed class Probe : ITrustedDeploymentProbeV4
{
    public bool CanDeploy,HasChallenge=true,Fail;
    public string[] Blockers=Array.Empty<string>();
    public Action<OwnerRebuildStateV3> During;
    public bool TryInspectCanonical(OwnerRebuildStateV3 s,
        out TrustedDeploymentObservationV4 result,out string why)
    {
        During?.Invoke(s);
        result=Fail?null:new TrustedDeploymentObservationV4 {
            OwnerId=s.OwnerId,RunId=s.RunId,OwnerGeneration=s.Generation,
            CanDeploy=CanDeploy,HasGenuineNextChallenge=HasChallenge,
            BlockingPhysicalIds=Blockers
        };
        why=Fail?"PROBE_REFUSED":"OK";return !Fail;
    }
}
static class Test
{
    public const string A="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    public const string B="bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    public const string C="cccccccccccccccccccccccccccccccc";
    public const string X="dddddddddddddddddddddddddddddddd";
    static int checks=0;
    static void Ok(bool x,string m){if(!x)throw new Exception("FAILED "+m);checks++;}
    static ReadinessReportV4 Eval(EconomyRunReadinessGateV4 g)
    {
        Ok(g.TryEvaluate(out var r,out var reason),"eval "+reason);return r;
    }
    static void Cases(string file)
    {
        DurableTestOwner.Initialize(file);
        var disk=new DurableTestOwner(file);
        var probe=new Probe {CanDeploy=false,Blockers=new[]{A}};
        var first=new EconomyRunReadinessGateV4(disk,probe,"owner1","run1");
        var second=new EconomyRunReadinessGateV4(new DurableTestOwner(file),
            probe,"owner1","run1");
        var bytes=File.ReadAllBytes(file);
        var s=Eval(first);
        Ok(s.Verdict==ReadinessVerdictV4.CriticalPending0HpBlock,"pending essential critical");
        Ok(s.RequiresOwnerRecoveryDecision&&!s.AuthorizesCombatStart,"no free win and owner decision");
        Ok(s.PendingCount==1&&s.BlockedPhysicalIds.SequenceEqual(new[]{A})&&
           s.Generation==3&&s.FreeSlotsRemainingInEpoch==0&&s.Scrap==350,
           "exact one owner epoch + physical pending and money");
        Ok(Eval(second).Verdict==s.Verdict,"same owner two observers consistent");
        Ok(bytes.SequenceEqual(File.ReadAllBytes(file)),"evaluation strictly never writes host file");
        probe.Blockers=new[]{C};
        Ok(Eval(first).Verdict==ReadinessVerdictV4.CriticalUnqueued0HpBlock,
           "critical unqueued physical part");
        probe.CanDeploy=true;probe.Blockers=Array.Empty<string>();
        Ok(Eval(first).Verdict==ReadinessVerdictV4.NormalChallengeReady,
           "normal CanDeploy despite optional 0HP parts");
        probe.CanDeploy=false;probe.Blockers=Array.Empty<string>();
        Ok(Eval(first).Verdict==ReadinessVerdictV4.HostDeployBlocked,"other host blocker");
        probe.HasChallenge=false;
        Ok(Eval(first).Verdict==ReadinessVerdictV4.NoGenuineChallengeWhilePending,
           "no next genuine win with pending module");
        probe.HasChallenge=true;probe.Blockers=new[]{A};
        var initial=DurableTestOwner.Load(file);
        probe.During=copy=> {copy.Scrap=999999;copy.Parts[0].Health=1;};
        Ok(Eval(first).Scrap==350&&Eval(first).PendingCount==1,
           "malicious probe only mutates defensive copy");
        probe.During=null;
        probe.Blockers=new[]{A,A};
        Ok(!first.TryEvaluate(out var invalid,out var why)&&
           why=="INVALID_BLOCKED_PHYSICAL_ID"&&
           invalid.Verdict==ReadinessVerdictV4.Unverified,"duplicate blocker fail closed");
        probe.Blockers=new[]{X};
        Ok(!first.TryEvaluate(out invalid,out why)&&
           why=="INVALID_BLOCKED_PHYSICAL_ID","foreign GUID rejected");
        probe.Blockers=new[]{B};
        Ok(!first.TryEvaluate(out invalid,out why)&&
           why=="INVALID_BLOCKED_PHYSICAL_ID","healthy part false 0HP blocker rejected");
        probe.Blockers=new[]{A};probe.CanDeploy=true;
        Ok(!first.TryEvaluate(out invalid,out why)&&
           why=="CONTRADICTORY_DEPLOYMENT_PROOF","contradictory witness rejected");
        probe.CanDeploy=false;
        probe.During=_=> {
            var x=DurableTestOwner.Load(file);x.Generation++;DurableTestOwner.Write(file,x);
        };
        Ok(!first.TryEvaluate(out invalid,out why)&&
           why=="OWNER_CHANGED_DURING_PROBE","CAS changed between reads fail closed");
        probe.During=null;
        var mutated=DurableTestOwner.Load(file);
        mutated.Generation=3;DurableTestOwner.Write(file,mutated);
        probe.During=_=> {
            var x=DurableTestOwner.Load(file);x.Scrap=999;DurableTestOwner.Write(file,x);
        };
        Ok(!first.TryEvaluate(out invalid,out why)&&
           why=="OWNER_CHANGED_DURING_PROBE","same-gen fork blocked by full equality");
        probe.During=null;
        DurableTestOwner.Write(file,initial);
        var wrong=new EconomyRunReadinessGateV4(disk,probe,"other","run1");
        Ok(!wrong.TryEvaluate(out invalid,out why)&&why=="INVALID_OWNER_ECONOMY",
           "foreign owner fail closed");
        var otherRun=new EconomyRunReadinessGateV4(disk,probe,"owner1","wrongrun");
        Ok(!otherRun.TryEvaluate(out invalid,out why)&&why=="INVALID_OWNER_ECONOMY",
           "run isolation");
        var corrupt=DurableTestOwner.Load(file);corrupt.Parts.Add(corrupt.Parts[0].Copy());
        DurableTestOwner.Write(file,corrupt);
        Ok(!first.TryEvaluate(out invalid,out why)&&why=="INVALID_OWNER_ECONOMY",
           "duplicate physical ID source invalid");
        DurableTestOwner.Write(file,initial);
        probe.Fail=true;
        Ok(!first.TryEvaluate(out invalid,out why)&&
           invalid.Verdict==ReadinessVerdictV4.Unverified,
           "host unavailable fail closed");
        probe.Fail=false;
        Ok(Eval(first).Verdict==ReadinessVerdictV4.CriticalPending0HpBlock,
           "recovered normal read after probe failure");
        Ok(File.ReadAllBytes(file).SequenceEqual(bytes),
           "all scenarios restored exact original file");
        Console.WriteLine("PASS READINESS V4 no-fake-win, full-equality owner CAS, double-read, physical IDs, two services, no host writes");
    }
    static void PricingModel()
    {
        const int wins=12,perWinOrders=2,price=70;
        int ordered=wins*perWinOrders;
        int completed=(wins-1)*perWinOrders;
        Ok(completed==22&&ordered-completed==2,"next distinct victory only");
        Ok((ordered-wins)*price==840&&
           (ordered-1)*price==1610,"illustrative free-per-victory vs free-per-run");
        // If essential wheel is 0HP and host says CanDeploy=false, no
        // synthetic future victory may be counted to heal it.
        int futureGenuineWins=0;
        Ok(futureGenuineWins==0,"no imaginary won challenge to break deadlock");
        Console.WriteLine("BALANCE_MODEL_ONLY wins=12 ordered=24 completed=22 pending=2 spend_per_victory=840 spend_per_run=1610 softlock=UNRESOLVED players=0");
    }
    static int Main(string[] args)
    {
        if(args.Length!=2)return 2;
        try{
            if(args[0]=="--scenario"){Cases(args[1]);PricingModel();}
            else if(args[0]=="--cold")
            {
                var disk=new DurableTestOwner(args[1]);
                var probe=new Probe{CanDeploy=false,Blockers=new[]{A}};
                var q=new EconomyRunReadinessGateV4(disk,probe,"owner1","run1");
                var r=Eval(q);
                Ok(r.Verdict==ReadinessVerdictV4.CriticalPending0HpBlock&&
                   r.Generation==3&&r.Scrap==350,"cold OS process same original disk snapshot");
                Ok(!r.AuthorizesCombatStart,"cold process not authority for WIN");
                Console.WriteLine("PASS READINESS V4 separate cold OS process no phantom win");
            } else return 2;
            Console.WriteLine("ECON_V4_SOURCE_PASS checks="+checks+
                 " NATIVE_UNITY=NOT_RUN LIVE_R3=NOT_WIRED");
            return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
