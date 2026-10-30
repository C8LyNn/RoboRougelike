// B01-61 P17 W10 independent executable test of unchanged producer SecureDockPolicy.cs.
// Executes the actual GuardedDockCommand, NOT a copied/mock implementation. Unity native NOT_RUN.
using System;
using System.Collections.Generic;
using B01.SecureReattachV2;

internal static class PublicDockApiQa
{
    private static int pass, fail, comparisons;
    private static void Yes(bool ok, string label)
    {
        comparisons++;
        if (!ok) throw new InvalidOperationException(label);
    }
    private static void Test(string label, Action body)
    {
        try { body(); pass++; Console.WriteLine("PASS " + label); }
        catch (Exception e) { fail++; Console.Error.WriteLine("FAIL " + label + ": " + e); }
    }
    private static GuardedDockCommand Create(Func<int,DockResult> inspect, Func<int,bool> commit)
        => new GuardedDockCommand(inspect,commit);
    private static void PublicAliasesShareInspector()
    {
        int inspections=0, commits=0;
        var gate=Create(i=>{inspections++;return DockResult.Allowed;},
            i=>{commits++;return true;});
        Yes(gate.Reattach(0),"Reattach allowed");
        Yes(gate.TrySafeReattach(1),"TrySafeReattach allowed");
        Yes(gate.ReattachFromGui(0),"ReattachFromGui allowed");
        Yes(inspections==3&&commits==3,"aliases bypass inspector or double commit");
        Yes(gate.LastResult==DockResult.Allowed,"allowed receipt");
    }
    private static void EveryRejectionIsFailClosed()
    {
        var denied=new[]{DockResult.NotDetached,DockResult.InvalidInput,DockResult.OutOfReach,
            DockResult.QuerySaturated,DockResult.Obstructed};
        foreach(var result in denied)
        {
            int inspected=0, committed=0;
            var gate=Create(i=>{inspected++;return result;},
                i=>{committed++;return true;});
            Yes(!gate.Reattach(0),"Reattach allowed "+result);
            Yes(!gate.TrySafeReattach(0),"Safe alias allowed "+result);
            Yes(!gate.ReattachFromGui(0),"GUI alias allowed "+result);
            Yes(inspected==3,"denied routes did not inspect exactly once "+result);
            Yes(committed==0,"denied routes committed "+result);
            Yes(gate.LastResult==result,"incorrect denial receipt "+result);
        }
    }
    private static void InvalidIndexNeverCallsUserCallbacks()
    {
        int looked=0, mutated=0;
        var gate=Create(i=>{looked++;return DockResult.Allowed;},
            i=>{mutated++;return true;});
        foreach(int n in new[]{-100,-1,2,3,int.MinValue,int.MaxValue})
        {
            Yes(!gate.Reattach(n),"invalid index through legacy-named API");
            Yes(!gate.TrySafeReattach(n),"invalid index through safe alias");
            Yes(!gate.ReattachFromGui(n),"invalid index through GUI");
            Yes(gate.LastResult==DockResult.InvalidInput,"invalid index receipt");
        }
        Yes(looked==0&&mutated==0,"invalid index reached callbacks");
    }
    private static void NoReentrantCommitFromInspector()
    {
        int committed=0, nested=0;
        GuardedDockCommand gate=null!;
        gate=Create(i=>{
            nested++;
            Yes(!gate.Reattach(0),"reentrant legacy API bypass");
            Yes(!gate.TrySafeReattach(0),"reentrant safe API bypass");
            Yes(!gate.ReattachFromGui(0),"reentrant GUI API bypass");
            return DockResult.Obstructed;
        },i=>{committed++;return true;});
        Yes(!gate.Reattach(0),"obstruction accepted after reentrant inspector");
        Yes(nested==1&&committed==0,"reentrant inspector committed mutation");
        Yes(gate.LastResult==DockResult.Obstructed,"reentrant refusal receipt");
    }
    private static void NoReentrantDoubleCommit()
    {
        int commits=0;
        GuardedDockCommand gate=null!;
        gate=Create(i=>DockResult.Allowed, i=>{
            commits++;
            Yes(!gate.Reattach(i),"reentrant commit allowed");
            Yes(!gate.TrySafeReattach(i),"reentrant safe commit allowed");
            Yes(!gate.ReattachFromGui(i),"reentrant GUI commit allowed");
            return true;
        });
        Yes(gate.Reattach(0),"outer valid commit failed");
        Yes(commits==1,"reentrant nested double commit");
    }
    private static void InspectExceptionsDoNotCommitAndRecover()
    {
        int inspected=0, commits=0;
        var gate=Create(i=>{
            inspected++;
            if(inspected==1)throw new InvalidOperationException("injected read failure");
            return DockResult.Obstructed;
        },i=>{commits++;return true;});
        bool thrown=false;
        try {gate.Reattach(0);}catch(InvalidOperationException){thrown=true;}
        Yes(thrown&&commits==0,"exception mutated state");
        Yes(!gate.Reattach(0),"exception left command permanently armed/unsafe");
        Yes(inspected==2&&commits==0,"exception or recovery bypassed inspection");
    }
    private static void MustReinspectOnEveryAttempt()
    {
        int inspection=0, commits=0;
        var gate=Create(i=>++inspection==1?DockResult.Obstructed:DockResult.Allowed,
            i=>{commits++;return true;});
        Yes(!gate.Reattach(0),"initial obstruction bypass");
        Yes(commits==0,"initial denied attempt mutated state");
        Yes(gate.ReattachFromGui(0),"authorized retry denied");
        Yes(inspection==2&&commits==1,"stale verdict reused across calls");
    }
    private static void SourcePolicyRejectsUntrustedEvidence()
    {
        var clear=Array.Empty<DockHit>();
        Yes(SecureDockPolicy.AllLayers==-1,"wrong all-layer constant");
        Yes(SecureDockPolicy.Capacity==128,"capacity drift");
        Yes(SecureDockPolicy.Check(true,true,1,3,0,0,128,.005,clear)==DockResult.InvalidInput,
            "layer mask bypass accepted");
        Yes(SecureDockPolicy.Check(true,true,1,3,-1,128,128,.005,
            NewHits(128,new DockHit(false,false,true,0)))==DockResult.QuerySaturated,
            "truncated broadphase accepted");
        Yes(SecureDockPolicy.Check(true,true,1,3,-1,1,128,.005,
            NewHits(1,new DockHit(false,false,false,0)))==DockResult.Obstructed,
            "unsupported collider accepted");
        Yes(SecureDockPolicy.Check(true,true,1,3,-1,1,128,.005,
            NewHits(1,new DockHit(false,false,true,.006)))==DockResult.Obstructed,
            "penetration accepted");
        Yes(SecureDockPolicy.Check(true,true,3.01,3,-1,0,128,.005,clear)==DockResult.OutOfReach,
            "remote teleport accepted");
        Yes(SecureDockPolicy.Check(true,true,1,3,-1,1,128,.005,
            NewHits(1,new DockHit(false,false,true,double.NaN)))==DockResult.Obstructed,
            "NaN overlap accepted");
        Yes(SecureDockPolicy.Check(true,true,1,3,-1,1,128,.005,
            NewHits(1,new DockHit(true,false,true,0)))==DockResult.Allowed,
            "own collider rejected");
        Yes(SecureDockPolicy.Check(true,true,1,3,-1,1,128,.005,
            NewHits(1,new DockHit(false,true,false,0)))==DockResult.Allowed,
            "trigger exclusion contract changed");
    }
    private static IReadOnlyList<DockHit> NewHits(int n,DockHit x)
    {
        var list=new List<DockHit>(n);
        for(int i=0;i<n;i++)list.Add(x);
        return list;
    }
    private static void ConstructorsRejectNullCallbacks()
    {
        bool inspectionRejected=false,commitRejected=false;
        try{new GuardedDockCommand(null!,i=>true);}
        catch(ArgumentNullException){inspectionRejected=true;}
        try{new GuardedDockCommand(i=>DockResult.Allowed,null!);}
        catch(ArgumentNullException){commitRejected=true;}
        Yes(inspectionRejected&&commitRejected,"null callback bypass");
    }
    public static int Main()
    {
        Test("all 3 public aliases invoke unchanged real inspection",PublicAliasesShareInspector);
        Test("5 rejection verdicts fail closed on ALL 3 APIs",EveryRejectionIsFailClosed);
        Test("invalid indices never inspect or mutate",InvalidIndexNeverCallsUserCallbacks);
        Test("nested public calls from inspector cannot commit",NoReentrantCommitFromInspector);
        Test("nested public calls from committer cannot double-commit",NoReentrantDoubleCommit);
        Test("exception cannot bypass inspector and handler recovers",InspectExceptionsDoNotCommitAndRecover);
        Test("every retry re-inspects instead of caching allow",MustReinspectOnEveryAttempt);
        Test("real all-layer physical observation policy rejects hostile inputs",SourcePolicyRejectsUntrustedEvidence);
        Test("constructor refuses null inspector and committer",ConstructorsRejectNullCallbacks);
        Console.WriteLine("SOURCE_ASSERTIONS "+comparisons);
        Console.WriteLine("SOURCE_RESULT "+pass+" PASS "+fail+" FAIL; UNCHANGED_PRODUCER_CSHARP9; NATIVE_UNITY_NOT_RUN");
        return fail==0?0:1;
    }
}
