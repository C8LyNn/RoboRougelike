// ORIGINAL #494 production DebrisRigPlayground + ORIGINAL W06 DebrisImpulseLedger.
// UnityEngine simulation is an explicit test boundary, NEVER native Unity/PhysX.
using System;
using System.Reflection;
using B01.DebrisPlayground001;
using B01.DebrisRepulse001;
using UnityEngine;

internal static class Program
{
    static int pass;
    static void Check(bool condition,string claim)
    {
        if(!condition)throw new Exception("FAIL "+claim);
        pass++;Console.WriteLine("PASS "+claim);
    }
    static DebrisRigPlayground NewRig()
    {
        Physics.Reset();
        var host=new GameObject("B01-62 independent original source host");
        var demo=host.AddComponent<DebrisRigPlayground>();
        demo.BuildDemo();
        return demo;
    }
    static DebrisRepulsionEmitter Emitter(DebrisRigPlayground demo,int index)
    {
        var field=typeof(DebrisRigPlayground).GetField("rigs", BindingFlags.Instance|BindingFlags.NonPublic);
        var rigs=(Array)field.GetValue(demo);
        var r=rigs.GetValue(index);
        return (DebrisRepulsionEmitter)r.GetType().GetField("Emitter",
                BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(r);
    }
    static void HappyPath(int index)
    {
        var demo=NewRig();
        Check(demo.Detach(index),"genuine W06 ledger consumption index="+index);
        var emitter=Emitter(demo,index);
        Check(emitter.Contains(demo.PartId(index)),"original consumed stable ID index="+index);
        Check(demo.Reattach(index),"original #494 successful guarded mount index="+index);
        Check(!demo.IsDetached(index),"success clears detached flag index="+index);
        Check(!emitter.Contains(demo.PartId(index)),"success rearms original W06 ID index="+index);
        Check(!demo.Reattach(index),"duplicate mount rejected index="+index);
        Check(demo.Detach(index),"rearmed ID detachable again index="+index);
    }
    static void CollisionDenial(int index)
    {
        var demo=NewRig();
        Check(demo.Detach(index),"blocked rig detached index="+index);
        var emitter=Emitter(demo,index);
        var body=demo.PartBody(index);
        var parent=body.transform.parent;
        var velocity=body.linearVelocity;
        var before=body.isKinematic;
        var blocker=new GameObject().AddComponent<BoxCollider>();
        Physics.Hits.Add(blocker);
        Physics.Depth=.25f;
        Check(!demo.Reattach(index),"actual original #494 class rejects shim blocker index="+index);
        Check(demo.IsDetached(index),"blocked state still detached index="+index);
        Check(body.transform.parent==parent && body.isKinematic==before,
            "blocked physical state unchanged index="+index);
        Check(body.linearVelocity.x==velocity.x && emitter.Contains(demo.PartId(index)),
            "blocked ID and velocity unchanged index="+index);
    }
    static void LostIdPartialCommit(int index)
    {
        var demo=NewRig();
        Check(demo.Detach(index),"stale-ledger fixture detached index="+index);
        var emitter=Emitter(demo,index);
        long id=demo.PartId(index);
        Check(emitter.Contains(id),"pre-reset original W06 consumed ID index="+index);
        var body=demo.PartBody(index);
        var parent=body.transform.parent;
        var velocity=body.linearVelocity;
        Check(parent==null && !body.isKinematic && demo.IsDetached(index),
            "pre-reset genuine original class in detached state index="+index);
        emitter.ClearForNewRun(); // public original W06 API, during detached state
        Check(!emitter.Contains(id),"genuine frozen W06 lost ID index="+index);
        bool threw=false;
        try {demo.Reattach(index);}
        catch(InvalidOperationException) {threw=true;}
        Check(threw,"original #494 throws after original W06 rearm rejects ID index="+index);
        Check(body.isKinematic && body.transform.parent!=parent && !demo.IsDetached(index),
            "ORIGINAL R2 STATE PARTIALLY COMMITTED BEFORE FAILURE index="+index);
        Check(body.linearVelocity.x!=velocity.x && !emitter.Contains(id),
            "ORIGINAL R2 VELOCITY MUTATED WHILE LEDGER UNREARMED index="+index);
        Check(!demo.Reattach(index),"partially docked part cannot retry mount index="+index);
    }
    public static int Main()
    {
        try
        {
            HappyPath(0);HappyPath(1);
            CollisionDenial(0);CollisionDenial(1);
            LostIdPartialCommit(0);LostIdPartialCommit(1);
            Console.WriteLine("ORIGINAL_R2_LEDGER_ATOMICITY_WITNESS_PASS="+pass);
            Console.WriteLine("RELEASE_VERDICT=HOLD_PARTIAL_STATE_MUTATION");
            Console.WriteLine("SOURCE_DOTNET8_WITH_UNITY_SHIM_NOT_NATIVE_UNITY_G1_NOT_RUN");
            return 0;
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            return 1;
        }
    }
}
