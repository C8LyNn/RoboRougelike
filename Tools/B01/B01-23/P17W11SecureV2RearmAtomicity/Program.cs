// W11 RED→GREEN: compiles ORIGINAL real V2 MonoBehaviour, W06 emitter and ledger.
// Only UnityEngine here is the clearly tagged test boundary, not PhysX.
using System;
using System.Reflection;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using UnityEngine;
internal static class Program
{
    static int checks;
    static void Check(bool v,string name)
    {if(!v)throw new Exception("W11_SOURCE_ASSERT_RED "+name);checks++;Console.WriteLine("PASS "+name);}
    static (SecureDebrisPlaygroundV2,DebrisRepulsionEmitter) NewRig(int index)
    {
        Physics.Reset();
        var host=new GameObject("Actual V2 class test");
        var demo=host.AddComponent<SecureDebrisPlaygroundV2>();
        demo.BuildDemo();
        var array=(Array)typeof(SecureDebrisPlaygroundV2).GetField("rigs",
            BindingFlags.Instance|BindingFlags.NonPublic).GetValue(demo);
        var rig=array.GetValue(index);
        var emitter=(DebrisRepulsionEmitter)rig.GetType().GetField("Emitter").GetValue(rig);
        return (demo,emitter);
    }
    static bool Route(SecureDebrisPlaygroundV2 d,int index,int route)
    {
        if(route==0)return d.Reattach(index);
        if(route==1)return d.TrySafeReattach(index);
        return d.ReattachFromGui(index);
    }
    static void MissingId(int index,int route,bool inQuery)
    {
        var (demo,emitter)=NewRig(index);
        Check(demo.Detach(index),"original W06 real detach consumes ID");
        var body=demo.PartBody(index);
        Check(demo.AppliedCount(index)==1,"original W06 genuine consumed set count");
        Vector3 beforeLinear=body.linearVelocity;
        Vector3 beforeAngular=body.angularVelocity;
        bool beforeKinematic=body.isKinematic;
        Transform beforeParent=body.transform.parent;
        Vector3 beforePosition=body.position;
        if(inQuery)Physics.BeforeOverlap=()=>{emitter.ClearForNewRun();Physics.BeforeOverlap=null;};
        else emitter.ClearForNewRun();
        bool ok=false,threw=false;
        try{ok=Route(demo,index,route);}catch(InvalidOperationException){threw=true;}
        // Old original V2 throws after changing physics while ID missing; required old RED.
        if(threw && (body.isKinematic!=beforeKinematic || body.transform.parent!=beforeParent ||
                    !demo.IsDetached(index)))
        {
            Console.WriteLine("W11_OLD_SOURCE_EXPECTED_RED_PARTIAL_COMMIT index="+index+" route="+route);
            Environment.Exit(3);
        }
        Check(!ok&&!threw,"missing original ledger ID returns false without throw");
        Check(demo.IsDetached(index),"detached state preserved");
        Check(body.transform.parent==beforeParent,"unchanged parent after failure");
        Check(body.position==beforePosition,"unchanged world position");
        Check(body.isKinematic==beforeKinematic,"kinematic unchanged");
        Check(body.linearVelocity==beforeLinear,"linear velocity unchanged");
        Check(body.angularVelocity==beforeAngular,"angular velocity unchanged");
        Check(demo.AppliedCount(index)==0,"no manufactured consumed ID");
        Check(Physics.Mask==-1 && Physics.Samples>0,"all layer physics inspector executed");
        Check(!Route(demo,index,route),"repeated missing-ID requests fail closed");
        Check(demo.IsDetached(index),"no second attempt changes state");
    }
    static void CoveredRoutes()
    {
        for(int rig=0;rig<2;rig++)
        for(int route=0;route<3;route++)
        {
            MissingId(rig,route,false);
            MissingId(rig,route,true);
        }
    }
    static void PositiveAndBlocked()
    {
        for(int rig=0;rig<2;rig++)
        for(int route=0;route<3;route++)
        {
            var (demo,emitter)=NewRig(rig);
            Check(demo.Detach(rig),"positive W06 detach with actual ledger");
            var block=new GameObject("layer 31 obstacle").AddComponent<BoxCollider>();
            block.gameObject.layer=31;
            Physics.Hits.Add(block);
            Check(!Route(demo,rig,route),"real class gate rejects simulated thick physical blocker");
            Check(demo.IsDetached(rig)&&demo.AppliedCount(rig)==1,"blocked path has intact original ledger");
            Physics.Reset();
            Check(Route(demo,rig,route),"real class successful reattach after obstacle cleared");
            Check(!demo.IsDetached(rig),"successful physical dock state");
            Check(demo.AppliedCount(rig)==0,"successful rearm consumed original real W06 ledger");
            Check(!Route(demo,rig,route),"duplicate reattach fails");
            Check(demo.Detach(rig),"same stable ID may detach again after legit rearm");
        }
    }
    static void Main()
    {
        CoveredRoutes();PositiveAndBlocked();
        Console.WriteLine("W11_ACTUAL_UNITY_CLASS_W06_LEDGER_GUARDED_ASSERTIONS_PASS="+checks+
            " REAL_W06_CLR_LEDGER=YES UNITY_API_TEST_STUB=YES NATIVE_PHYSX=NOT_RUN");
    }
}
