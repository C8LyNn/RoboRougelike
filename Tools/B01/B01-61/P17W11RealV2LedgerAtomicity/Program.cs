// Runs unchanged ORIGINAL V2 Unity host class with independently adapted synthetic Unity boundary,
// and ORIGINAL W06 DebrisImpulseLedger compiled in-process. NOT native Unity or PhysX.
// Receipt is intentionally HOLD while a missing consumed ID can partially mutate Rigidbody/parent.
using System;
using System.Reflection;
using B01.SecureReattachV2;
using B01.DebrisRepulse001;
using UnityEngine;

internal static class Program
{
    private static int assertions, blockedCases, controlCases, failures;
    private static void Check(bool ok, string tag)
    {
        assertions++;
        if (!ok) throw new Exception("ASSERT_FAIL " + tag);
    }

    private static SecureDebrisPlaygroundV2 NewDemo()
    {
        Physics.Reset();
        var host=new GameObject("original V2 source host - synthetic boundary only");
        var demo=host.AddComponent<SecureDebrisPlaygroundV2>();
        demo.BuildDemo();
        return demo;
    }

    private static DebrisRepulsionEmitter Emitter(SecureDebrisPlaygroundV2 demo,int index)
    {
        var field=typeof(SecureDebrisPlaygroundV2).GetField("rigs",BindingFlags.NonPublic|BindingFlags.Instance);
        Check(field!=null,"original rig array");
        var rigs=(Array)field!.GetValue(demo)!;
        var rig=rigs.GetValue(index)!;
        var emitterField=rig.GetType().GetField("Emitter",BindingFlags.Public|BindingFlags.Instance);
        Check(emitterField!=null,"original emitter member");
        return (DebrisRepulsionEmitter)emitterField!.GetValue(rig)!;
    }

    private static bool Alias(SecureDebrisPlaygroundV2 demo,int index,int route)
    {
        switch(route)
        {
            case 0:return demo.Reattach(index);
            case 1:return demo.TrySafeReattach(index);
            case 2:return demo.ReattachFromGui(index);
            default:throw new ArgumentOutOfRangeException(nameof(route));
        }
    }

    private static void LegitimateCommit(int rigIndex,int route)
    {
        var demo=NewDemo();
        Check(demo.Detach(rigIndex),"real original V2 detach");
        var emitter=Emitter(demo,rigIndex);
        Check(emitter.HasConsumed(demo.PartId(rigIndex)),"original W06 ledger consumed");
        Check(emitter.AppliedCount==1,"original ledger count after detach");
        Check(Alias(demo,rigIndex,route),"unobstructed reattach");
        Check(!demo.IsDetached(rigIndex),"valid dock commits original state");
        Check(emitter.AppliedCount==0,"original ledger rearmed on valid reattach");
        Check(!Alias(demo,rigIndex,route),"duplicate cannot rearm ledger");
        Check(demo.Detach(rigIndex),"next original detach allowed after true rearm");
        controlCases++;
    }

    private static void ObstacleIsFailClosed(int index)
    {
        var demo=NewDemo();
        Check(demo.Detach(index),"detach before blocker");
        var body=demo.PartBody(index);
        var velocity=body.linearVelocity;
        var parent=body.transform.parent;
        var emitter=Emitter(demo,index);
        var blocker=new GameObject("QA synthetic obstruction");
        Physics.Hits.Add(blocker.AddComponent<BoxCollider>());
        Physics.Depth=0.1f;
        Check(!demo.Reattach(index),"obstructed target rejected");
        Check(demo.IsDetached(index),"blocked dock keeps part detached");
        Check(body.transform.parent==parent && !body.isKinematic,
              "blocked dock keeps parent and kinematic flags");
        Check(body.linearVelocity==velocity,"blocked dock keeps velocity");
        Check(emitter.AppliedCount==1,"blocked dock does not consume/rearm original ledger");
        controlCases++;
    }

    private static void LostConsumedIdMustNotPartiallyCommit(int rigIndex,int route)
    {
        var demo=NewDemo();
        Check(demo.Detach(rigIndex),"original V2 real detach");
        var emitter=Emitter(demo,rigIndex);
        var body=demo.PartBody(rigIndex);
        var originalParent=body.transform.parent;
        Check(originalParent==null && !body.isKinematic,"detached physical state");
        // External W06 API is actually present in original immutable emitter implementation.
        // The QA-only adapter wires ClearForNewRun to original immutable W06 DebrisImpulseLedger.
        emitter.ClearForNewRun();
        Check(!emitter.HasConsumed(demo.PartId(rigIndex)) &&
            emitter.AppliedCount==0,"genuine W06 consumed ID removed out-of-sequence");
        body.linearVelocity=new Vector3(1,2,3);
        body.angularVelocity=new Vector3(4,5,6);
        var priorLinear=body.linearVelocity;
        var priorAngular=body.angularVelocity;

        bool threw=false;
        bool result=false;
        try {result=Alias(demo,rigIndex,route);}
        catch (InvalidOperationException) {threw=true;}

        // Expected original-source safety RED: commit executes physical mutations first,
        // then original W06 RearmAfterReattach rejects missing consumed ID and throws.
        Check(threw && !result,"real V2 throws on missing ID after committing");
        Check(!demo.IsDetached(rigIndex),"real V2 original Detached=false despite error");
        Check(body.isKinematic,"original body was set kinematic despite error");
        Check(body.transform.parent!=originalParent,"original body parent modified despite error");
        Check(body.linearVelocity==Vector3.zero && body.angularVelocity==Vector3.zero,
              "original body velocity overwritten despite error");
        Check(priorLinear!=body.linearVelocity && priorAngular!=body.angularVelocity,
              "prior motion not preserved");
        Check(emitter.AppliedCount==0,"original ledger remains unconsumed");
        Check(demo.LastDockResult==DockResult.Allowed,
              "router incorrectly reports allowance despite transaction failure");
        blockedCases++;
        Console.WriteLine("REAL_V2_SOURCE_PARTIAL_COMMIT_REPRODUCED rig="+rigIndex+
                          " public_route="+route+" | Unity/PhysX SYNTHETIC");
    }

    public static int Main(string[] args)
    {
        try
        {
            for(int i=0;i<2;i++)
            {
                ObstacleIsFailClosed(i);
                for(int route=0;route<3;route++)
                {
                    LegitimateCommit(i,route);
                    LostConsumedIdMustNotPartiallyCommit(i,route);
                }
            }
            Check(blockedCases==6 && controlCases==8,
                  "six unsafe witnesses + eight safe control cases");
            Console.WriteLine("QA_EXECUTION_PASS assertions="+assertions+
              " safe_controls="+controlCases+" unsafe_direct_original_host_witnesses="+blockedCases);
            Console.WriteLine("SOURCE_RELEASE_GATE_RED=PARTIAL_COMMIT_BEFORE_LEDGER_REARM");
            Console.WriteLine("ORIGINAL_W06_LEDGER_EXECUTED=YES; ORIGINAL_V2_HOST_EXECUTED=YES");
            Console.WriteLine("UNITY_NATIVE_G1=NOT_RUN; SYNTHETIC_UNITY_BOUNDARY=YES");
            return 0; // QA DETECTED blocker successfully; not game release success.
        }
        catch(Exception ex)
        {
            failures++;
            Console.Error.WriteLine("QA_RUNNER_FAIL "+ex);
            return 1;
        }
    }
}
