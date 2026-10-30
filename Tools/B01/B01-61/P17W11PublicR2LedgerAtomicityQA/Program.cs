using System;
using System.Reflection;
using B01.DebrisPlayground001;
using B01.DebrisRepulse001;
using UnityEngine;

internal static class Program
{
    private static int pass;
    private static int witnesses;
    private static void Check(bool ok, string why)
    {
        if (!ok) throw new Exception("QA_ASSERTION_FAILED " + why);
        pass++;
        Console.WriteLine("PASS " + why);
    }
    private static DebrisRigPlayground Fresh()
    {
        Physics.Reset();
        var host = new GameObject("B01-61 original W10 + original W06");
        var d = host.AddComponent<DebrisRigPlayground>();
        d.BuildDemo();
        return d;
    }
    private static DebrisRepulsionEmitter OriginalEmitter(DebrisRigPlayground demo, int index)
    {
        FieldInfo f = typeof(DebrisRigPlayground).GetField("rigs", BindingFlags.Instance | BindingFlags.NonPublic);
        Check(f != null, "original private rig list exists");
        var rigs = (Array)f.GetValue(demo);
        var rig = rigs.GetValue(index);
        FieldInfo field = rig.GetType().GetField("Emitter", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Check(field != null, "original rig stores W06 emitter");
        var emitter = (DebrisRepulsionEmitter)field.GetValue(rig);
        Check(emitter != null, "real original W06 emitter instance");
        return emitter;
    }
    private static bool Same(Vector3 a,Vector3 b) =>
        a.x == b.x && a.y == b.y && a.z == b.z;
    private static void VerifyNormalSuccessfulRearm(int index)
    {
        var demo=Fresh();
        Check(demo.Detach(index), "original W10 physical demo detach rig "+index);
        var emitter=OriginalEmitter(demo,index);
        Check(emitter.AppliedCount==1, "original W06 ledger consumed ID "+index);
        Check(demo.Reattach(index), "baseline W10 public reattach succeeds with valid ledger "+index);
        Check(!demo.IsDetached(index), "baseline closes detach "+index);
        Check(demo.PartBody(index).isKinematic, "baseline physical kinematic "+index);
        Check(emitter.AppliedCount==0, "baseline original W06 ledger rearmed "+index);
        Check(demo.Detach(index), "baseline next detach is possible "+index);
    }
    private static void VerifyAtomicityUnderLostId(int index)
    {
        var demo=Fresh();
        Check(demo.Detach(index), "original source detach before ledger fault "+index);
        var body=demo.PartBody(index);
        var emitter=OriginalEmitter(demo,index);
        Check(emitter.AppliedCount==1, "consumed ledger starts correct "+index);
        var originalParent=body.transform.parent;
        var originalPosition=body.transform.position;
        var originalLocal=body.transform.localPosition;
        var originalLinear=body.linearVelocity;
        var originalAngular=body.angularVelocity;
        var originalKinematic=body.isKinematic;
        bool detached=demo.IsDetached(index);
        emitter.ClearForNewRun(); // Actual original W06 public API: authorized fault injection.
        Check(emitter.AppliedCount==0, "original W06 ledger cleared while body detached "+index);
        bool approved=false; Exception failure=null;
        try { approved=demo.Reattach(index); }
        catch(Exception exception) { failure=exception; }
        // A successful atomic failure must return false/no throw, preserve parent,
        // rigidbody velocities and kinematic, detached state, and original ID ledger.
        bool broken = failure is InvalidOperationException &&
             !approved && detached && !demo.IsDetached(index) &&
             !originalKinematic && body.isKinematic &&
             originalParent!=body.transform.parent &&
             Same(body.linearVelocity,Vector3.zero) &&
             Same(body.angularVelocity,Vector3.zero) &&
             emitter.AppliedCount==0;
        Console.WriteLine("LEDGER_CLEAR_FAULT index="+index+
            " threw="+(failure?.GetType().Name??"NONE")+
            " approved="+approved+" detached_before="+detached+
            " detached_after="+demo.IsDetached(index)+
            " kinematic_before="+originalKinematic+
            " kinematic_after="+body.isKinematic+
            " parent_changed="+(originalParent!=body.transform.parent)+
            " position_changed="+!Same(originalPosition,body.transform.position)+
            " local_changed="+!Same(originalLocal,body.transform.localPosition)+
            " velocity_changed="+!Same(originalLinear,body.linearVelocity)+
            " angular_changed="+!Same(originalAngular,body.angularVelocity)+
            " ledger_after="+emitter.AppliedCount);
        Check(broken, "exact original W10 public method partially commits BEFORE original W06 ledger denial "+index);
        witnesses++;
    }
    private static int Main(string[] args)
    {
        try
        {
            for(int i=0;i<2;i++) VerifyNormalSuccessfulRearm(i);
            for(int i=0;i<2;i++) VerifyAtomicityUnderLostId(i);
            Console.WriteLine("ORIGINAL_SOURCE_WITNESS_PASS asserts="+pass+" rigs="+witnesses+
                " original_W10_game_class=YES original_W06_LEDGER=YES original_W06_EMITTER=YES PHYSICS=SIMULATED UNITY=NOT_RUN");
            if(args.Length>0 && args[0]=="--release")
            {
                Console.WriteLine("RELEASE_HOLD: Actual original protected public API can commit partial physics state on ledger-rearm rejection.");
                return 3;
            }
            return 0;
        }
        catch(Exception e)
        {
            Console.Error.WriteLine("QA_HARNESS_FAILED "+e);
            return 2;
        }
    }
}
