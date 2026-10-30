// Executes real W07 original (RED) or W10 revised (GREEN) C# class, no copied logic.
// Simulated boundary: NOT native Unity/PhysX. Actual call routing/state measured.
using System;
using System.Reflection;
using B01.DebrisPlayground001;
using UnityEngine;

internal static class Program
{
    private static int count;
    static void Check(bool ok,string name)
    {
        if(!ok)throw new Exception("SOURCE_SECURITY_RED "+name);
        ++count;Console.WriteLine("SOURCE_PASS "+name);
    }
    static DebrisRigPlayground Demo()
    {
        Physics.Reset();
        var host=new GameObject("real original class host");
        var p=host.AddComponent<DebrisRigPlayground>();
        p.BuildDemo();
        return p;
    }
    static BoxCollider Blocker()
    {
        var block=new GameObject("physically blocked target");
        var box=block.AddComponent<BoxCollider>();
        Physics.Hits.Add(box);return box;
    }
    static void Negative(DebrisRigPlayground demo,int index,string reason)
    {
        Rigidbody body=demo.PartBody(index);
        Vector3 oldLinear=body.linearVelocity,oldAngular=body.angularVelocity;
        Transform parent=body.transform.parent;
        int applied=demo.AppliedCount(index);
        bool oldKinematic=body.isKinematic;
        bool result=demo.Reattach(index);
        Check(!result,"public direct Reattach rejects "+reason);
        Check(demo.IsDetached(index),"no Detach state mutation "+reason);
        Check(body.isKinematic==oldKinematic,"no kinematic mutation "+reason);
        Check(body.transform.parent==parent,"no parent mutation "+reason);
        Check(body.linearVelocity.x==oldLinear.x && body.angularVelocity.x==oldAngular.x,
            "no velocity mutation "+reason);
        Check(demo.AppliedCount(index)==applied,"original emitter applied-ID count preserved "+reason);
    }
    static void BothRigs(int index)
    {
        var demo=Demo();
        Check(demo.Detach(index),"original actual source detaches rig"+index);
        var obstacle=Blocker();
        Physics.Depth=.3f;
        Negative(demo,index,"real penetrated obstacle "+index);
        Check(Physics.LastMask==-1,"ALL layers incl ignored obstacle"+index);
        Check(Physics.LastQuery==QueryTriggerInteraction.Collide,"triggers included for saturation"+index);
        Check(Physics.SyncCalls>0,"physical Physics.SyncTransforms"+index);
        Physics.Depth=.0051f;Negative(demo,index,"just over tolerance");
        Physics.Depth=float.NaN;Negative(demo,index,"nonfinite penetration");
        Physics.Depth=-1f;Negative(demo,index,"negative penetration");
        Physics.CorruptDirection=true;Negative(demo,index,"nonfinite separation direction");
        Physics.CorruptDirection=false;
        Physics.Depth=.004f;
        Check(demo.Reattach(index),"true narrow-phase penetration within 5mm accepted");
        Check(!demo.IsDetached(index),"successful public reattach state");
        Check(demo.PartBody(index).isKinematic,"successful reattach kinematic");
        Check(!demo.Reattach(index),"repeat reattach never duplicates rearm");
        Check(demo.Detach(index),"original detached ID can rearm after real approved docking");
        Physics.Reset();
        var trigger=Blocker();trigger.isTrigger=true;
        Check(demo.Reattach(index),"explicit real trigger-only target permitted");
        Check(demo.Detach(index),"same ID can detach after approved rearm");
        Physics.Reset();var unsupported=new GameObject().AddComponent<MeshCollider>();
        unsupported.convex=false;Physics.Hits.Add(unsupported);
        Negative(demo,index,"unsupported concave collider fail closed");
        Physics.Reset();Blocker();Physics.Penetrating=false;Physics.Depth=0;
        Check(demo.Reattach(index),"no actual penetration permits clean dock");
        Check(!demo.IsDetached(index),"clean dock after denied attempts");
    }
    static void Extended()
    {
        var demo=Demo();Check(demo.Detach(0),"extended detached");
        var body=demo.PartBody(0);
        Physics.Reset();
        for(int i=0;i<64;i++)Blocker();
        Negative(demo,0,"saturated physical hit buffer");
        Physics.Reset();Physics.Hits.Add(null);
        Negative(demo,0,"invalid null collider");
        Physics.Reset();var bad=Blocker();bad.enabled=false;
        Negative(demo,0,"disabled collider inconsistent broadphase");
        Physics.Reset();
        var box=body.GetComponent<BoxCollider>();box.enabled=false;
        Negative(demo,0,"disabled own physical geometry");
        box.enabled=true;box.isTrigger=true;Negative(demo,0,"trigger own part invalid");
        box.isTrigger=false;
        var scale=body.transform.localScale;
        body.transform.localScale=new Vector3(float.NaN,1,1);
        Negative(demo,0,"NaN actual part scale");
        body.transform.localScale=scale;
        var pos=body.transform.position;
        body.transform.position=pos+new Vector3(50,0,0);
        Negative(demo,0,"out-of-reach actual part");
        body.transform.position=pos;
        body.isKinematic=true;Negative(demo,0,"kinematic detached body invalid");
        body.isKinematic=false;
        Blocker();Physics.Depth=.2f;
        typeof(DebrisRigPlayground).GetMethod("OnGUI",BindingFlags.NonPublic|BindingFlags.Instance);
        GUILayout.NextButton="Reattach";
        typeof(DebrisRigPlayground).GetMethod("OnGUI",BindingFlags.NonPublic|BindingFlags.Instance)
            .Invoke(demo,null);
        Check(demo.IsDetached(0),"legacy GUI route cannot bypass public mandatory guard");
        var invalid1=false;
        try{demo.Reattach(-1);}catch(InvalidOperationException){invalid1=true;}
        Check(invalid1,"invalid rig ID cannot perform docking");
        Check(demo.IsDetached(0),"invalid rig leaves true state unchanged");
    }
    static void Main()
    {
        BothRigs(0);BothRigs(1);Extended();
        Console.WriteLine("ORIGINAL_PUBLIC_API_SECURITY_ASSERTIONS_PASS="+count+
           " PHYSICS_API_STUB_NOT_NATIVE G1=NOT_RUN");
    }
}
