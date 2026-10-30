// Independent B01-61 W11 R2 acceptance: compiles original unchanged R2 Unity host,
// unchanged R2 AtomicDockLedger and unchanged W06 DebrisImpulseLedger.
// Synthetic Unity/Physics API boundary; NEVER claim native Unity G1.
using System;
using System.Reflection;
using B01.SecureReattachV2;
using B01.DebrisRepulse001;
using UnityEngine;

internal static class AtomicR2QA
{
    private static int assertions, negativeRoutes, validControls, rollbackTests;
    static void Assert(bool pass,string message)
    {
        assertions++;
        if(!pass)throw new InvalidOperationException("INDEPENDENT_QA_FAIL "+message);
    }
    static SecureDebrisPlaygroundV2 Demo()
    {
        Physics.Reset();
        var go=new GameObject("R2 exact original source synthetic fixture");
        var demo=go.AddComponent<SecureDebrisPlaygroundV2>();
        demo.BuildDemo();
        return demo;
    }
    static AtomicDockLedger Ledger(SecureDebrisPlaygroundV2 demo,int n)
    {
        var f=typeof(SecureDebrisPlaygroundV2).GetField("rigs",
            BindingFlags.Instance|BindingFlags.NonPublic);
        Assert(f!=null,"original rig array");
        var rigs=(Array)f!.GetValue(demo)!;
        var rig=rigs.GetValue(n)!;
        var ledger=rig.GetType().GetField("Ledger",BindingFlags.Instance|BindingFlags.Public);
        Assert(ledger!=null,"original atomic ledger field");
        return (AtomicDockLedger)ledger!.GetValue(rig)!;
    }
    static bool Dock(SecureDebrisPlaygroundV2 d,int n,int route)
    {
        if(route==0)return d.Reattach(n);
        if(route==1)return d.TrySafeReattach(n);
        if(route==2)return d.ReattachFromGui(n);
        throw new ArgumentOutOfRangeException(nameof(route));
    }
    static void MissingIdMustBeAtomic(int n,int route)
    {
        var d=Demo();
        Assert(d.Detach(n),"real R2 detach consumes original W06 ID");
        var ledger=Ledger(d,n);
        Assert(ledger.Contains(d.PartId(n)) && ledger.Count==1,"original consumed stable ID");
        var body=d.PartBody(n);
        var priorParent=body.transform.parent;
        Assert(priorParent==null && !body.isKinematic,"real R2 detached state");
        // Exact public reset on original AtomicDockLedger, intentional misplaced reset.
        ledger.ClearForNewRun();
        Assert(ledger.Count==0 && !ledger.Contains(d.PartId(n)),"original consumed ID cleared");
        body.linearVelocity=new Vector3(1,2,3);
        body.angularVelocity=new Vector3(4,5,6);
        var linear=body.linearVelocity;
        var angular=body.angularVelocity;
        var position=body.transform.position;
        var rotation=body.transform.rotation;
        Assert(!Dock(d,n,route),"missing consumed ID must refuse all public APIs");
        Assert(d.LastDockResult==DockResult.TransactionRejected,"transaction rejection receipt");
        Assert(d.IsDetached(n) && !body.isKinematic,"failed transaction retains detached/dynamic");
        Assert(body.transform.parent==priorParent,"failed transaction must retain original parent");
        Assert(body.transform.position==position,"failed transaction must retain position");
        Assert(body.transform.rotation==rotation,"failed transaction must retain rotation");
        Assert(body.linearVelocity==linear && body.angularVelocity==angular,
            "failed transaction must preserve original momenta");
        Assert(ledger.Count==0 && !ledger.Busy,"failed transaction cannot consume ID or retain busy");
        negativeRoutes++;
        Console.WriteLine("PASS R2_MISSING_ID_ATOMIC_REJECTION rig="+n+" route="+route);
    }
    static void ValidDock(int n,int route)
    {
        var d=Demo();
        Assert(d.Detach(n),"valid detach");
        var l=Ledger(d,n);
        Assert(l.Count==1,"valid initial consumed ledger ID");
        Assert(Dock(d,n,route),"valid reattach must succeed");
        Assert(!d.IsDetached(n) && d.PartBody(n).isKinematic,"valid attached physical state");
        Assert(l.Count==0,"valid reattach removed consumed ID");
        Assert(!Dock(d,n,route),"same part cannot dock twice");
        Assert(d.Detach(n),"new detach after authorized rearm");
        validControls++;
    }
    static void CollisionBlock(int n)
    {
        var d=Demo();
        Assert(d.Detach(n),"blocked fixture detaches");
        var b=d.PartBody(n);
        var before=b.linearVelocity;
        var obstacle=new GameObject("synthetic obstruction");
        Physics.Hits.Add(obstacle.AddComponent<BoxCollider>());
        Physics.Depth=.2f;
        Assert(!d.Reattach(n),"obstructed API must fail closed");
        Assert(d.IsDetached(n)&&!b.isKinematic&&b.transform.parent==null,"blocked body must not change");
        Assert(b.linearVelocity==before&&Ledger(d,n).Count==1,
            "blocked attempt must retain velocity and consumed ID");
        validControls++;
    }
    static AtomicDockLedger Consumed(long id)
    {
        var l=new AtomicDockLedger();
        Vector3d p=new Vector3d(0,0,0);
        Assert(l.TryDetach(id,p,p,p,1.0,0,0,out Vector3d impulse),"original W06 consume ID");
        Assert(impulse.MagnitudeSquared==0 && l.Contains(id),"zero-force original W06 ledger");
        return l;
    }
    static void FaultedCommitMustRollbackAndReconsume()
    {
        long id=8191;
        var l=Consumed(id);
        int state=1, applied=0, restored=0;
        bool result=l.TryReattach(id,
            ()=>{applied++;state=9;throw new InvalidOperationException("injected original physical mutation failure");},
            ()=>{restored++;state=1;});
        Assert(!result && state==1 && applied==1 && restored==1,
            "failed physical callback must invoke complete restore");
        Assert(l.Contains(id)&&l.Count==1&&!l.Busy,"original consumed ID restored by W06 on rollback");
        Assert(l.TryReattach(id,()=>state=2,()=>state=1),
            "fresh authorized retry after rollback");
        Assert(state==2&&l.Count==0&&!l.Busy,"correct rollback then successful commit");
        rollbackTests++;
    }
    static void NestedDockAndResetDeniedDuringCommit()
    {
        long id=8192;
        var l=Consumed(id);
        bool nested=false, resetThrown=false;
        Assert(l.TryReattach(id,()=>{
            nested=l.TryReattach(id,()=>{},()=>{});
            try{l.ClearForNewRun();}catch(InvalidOperationException){resetThrown=true;}
        },()=>{}),"outer commit succeeds");
        Assert(!nested && resetThrown && !l.Busy && l.Count==0,
            "nested dock/reset cannot invalidate in-flight ID transaction");
        rollbackTests++;
    }
    static void UnsafeRollbackIsNeverSilentlyApproved()
    {
        long id=8193;
        var l=Consumed(id);
        bool threw=false;
        try
        {
            l.TryReattach(id,
                ()=>throw new InvalidOperationException("commit failed"),
                ()=>throw new InvalidOperationException("rollback failed"));
        }
        catch(InvalidOperationException ex)
        {threw=ex.Message.Contains("quarantine");}
        Assert(threw,"rollback fault must quarantine instead of returning success");
        Assert(l.Contains(id)&&l.Count==1&&!l.Busy,
            "W06 ledger still consumed after rollback callback error");
        rollbackTests++;
    }
    static void MissingIdCannotCallPhysicalCallbacks()
    {
        var l=new AtomicDockLedger();
        bool applied=false, restored=false;
        Assert(!l.TryReattach(7191,()=>applied=true,()=>restored=true),
            "missing ledger ID must be rejected");
        Assert(!applied&&!restored&&l.Count==0&&!l.Busy,
            "missing ID must not invoke either physical callback");
        rollbackTests++;
    }
    public static int Main()
    {
        try
        {
            for(int n=0;n<2;n++)
            {
                CollisionBlock(n);
                for(int route=0;route<3;route++)
                {
                    ValidDock(n,route);
                    MissingIdMustBeAtomic(n,route);
                }
            }
            FaultedCommitMustRollbackAndReconsume();
            NestedDockAndResetDeniedDuringCommit();
            UnsafeRollbackIsNeverSilentlyApproved();
            MissingIdCannotCallPhysicalCallbacks();
            Assert(negativeRoutes==6&&validControls==8&&rollbackTests==4,
                "expected coverage matrix");
            Console.WriteLine("QA_R2_ATOMIC_SOURCE_GREEN assertions="+assertions+
                " missing_id_denials="+negativeRoutes+" positive_controls="+validControls+
                " rollback_injection_groups="+rollbackTests);
            Console.WriteLine("ORIGINAL_R2_HOST=EXECUTED; ORIGINAL_R2_ATOMIC_LEDGER=EXECUTED; ORIGINAL_W06_LEDGER=EXECUTED");
            Console.WriteLine("UNITY_NATIVE_G1_NOT_RUN; UNITY_SYNTHETIC_API_BOUNDARY=YES; NO_MERGE");
            return 0;
        }
        catch(Exception e)
        {
            Console.Error.WriteLine("INDEPENDENT_QA_RED "+e);
            return 1;
        }
    }
}
