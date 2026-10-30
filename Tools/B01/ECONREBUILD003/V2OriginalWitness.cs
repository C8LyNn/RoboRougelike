// W21 negative evidence: compile frozen REAL V2 producer C# without altering it.
// A permissive V2 store is ABI-conformant yet does NOT authenticate any battle.
// Detector PASS => original V2 source contract release RED; not live Unity exploit.
using System;
using System.Collections.Generic;
using RoboRougelike.B01.Economy.RebuildV2;
sealed class PermissiveOriginalStore : IAtomicRebuildOwnerStoreV2
{
    OwnerRebuildStateV2 actual=new OwnerRebuildStateV2{
        OwnerId="owner-A",RunId="run-A",Scrap=0,
        Parts=new List<OwnedPartV2>{
            new OwnedPartV2{Id="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",Kind="Wheel",
                Health=0,Purchase=100,PaidQuote=70},
            new OwnedPartV2{Id="bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",Kind="Wheel",
                Health=0,Purchase=100,PaidQuote=70}
        }
    };
    public bool TryRead(string owner,string run,out OwnerRebuildStateV2 state,out string reason)
    {state=actual.Copy();reason="OK";return true;}
    public PublishResultV2 TryPublish(string owner,string run,long expected,
        OwnerRebuildStateV2 proposed,out string reason)
    {
        if(actual.Generation!=expected){reason="CONFLICT";return PublishResultV2.Conflict;}
        actual=proposed.Copy();reason="OK";return PublishResultV2.Committed;
    }
}
static class Witness
{
    static void Check(bool x,string why){if(!x)throw new Exception("FAILED:"+why);}
    static int Main()
    {
        var owner=new PermissiveOriginalStore();
        var economy=new RebuildEconomyAuthorityV2(owner,"owner-A","run-A");
        Check(economy.TryReserveDestroyed("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",out var reason),
            "queue free 0hp");
        // Never completed any combat; caller simply supplied an affirmative boolean!
        bool acceptedFake=economy.TryFinishChallenge("never-battled-001",true,out reason);
        Check(acceptedFake,"untrusted bool forged win not reproduced");
        Check(economy.TryReadAuthoritative(out var v1,out reason),"read");
        Check(v1.Wins==1 && v1.AppliedVictoryIds.Contains("never-battled-001") &&
            v1.Parts.Find(x=>x.Id=="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa").Health==1 &&
            v1.Pending.Count==0,"fake win updated real producer state");
        Check(economy.TryReserveDestroyed("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",out reason),
            "new free claim after forged win");
        bool acceptedSecondFake=economy.TryFinishChallenge("never-battled-002",true,out reason);
        Check(acceptedSecondFake,"second fake win not reproduced");
        Check(economy.TryReadAuthoritative(out var v2,out reason),"read second");
        Check(v2.Wins==2&&v2.Pending.Count==0&&v2.Scrap==0 &&
            v2.Parts.Find(x=>x.Id=="bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb").Health==1,
            "two free 0hp restored without any trusted battle");
        Console.WriteLine("QA_DETECTOR_PASS ORIGINAL_FROZEN_V2_CONTRACT_RED 2 fake public bool wins accepted; V3 trusted host receipt required; LIVE_UNITY_NOT_TESTED");
        return 0;
    }
}
