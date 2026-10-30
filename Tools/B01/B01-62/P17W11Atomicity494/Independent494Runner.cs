// B01-62 W11: ACTUAL immutable #494 DebrisRigPlayground and unchanged W06
// DebrisRepulsionEmitter/DebrisImpulseLedger, executed with a TEST-ONLY physics shim.
// This is NOT native Unity or PhysX. No production code is rewritten.
using System;
using System.Reflection;
using B01.DebrisPlayground001;
using B01.DebrisRepulse001;
using UnityEngine;

internal static class Program
{
    private static int pass,fail;
    static void Check(string label,Func<bool> action)
    {
        try { if(action()){pass++;Console.WriteLine("PASS "+label);}
              else {fail++;Console.WriteLine("FAIL "+label);} }
        catch(Exception ex){fail++;Console.WriteLine("FAIL "+label+" "+ex);}
    }
    static DebrisRigPlayground Demo()
    {
        Physics.Reset();
        var host=new GameObject("B01-62 exact source QA");
        var d=host.AddComponent<DebrisRigPlayground>();
        d.BuildDemo();
        return d;
    }
    static DebrisRepulsionEmitter Emitter(DebrisRigPlayground demo,int index)
    {
        const BindingFlags f=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
        var array=(Array)typeof(DebrisRigPlayground).GetField("rigs",f).GetValue(demo);
        object rig=array.GetValue(index);
        return (DebrisRepulsionEmitter)rig.GetType().GetField("Emitter",f).GetValue(rig);
    }
    static bool Equal(Vector3 a,Vector3 b)=>a.x==b.x&&a.y==b.y&&a.z==b.z;
    static bool CleanDock(int index)
    {
        var d=Demo();
        if(!d.Detach(index) || d.AppliedCount(index)!=1)return false;
        Physics.Reset();
        if(!d.Reattach(index))return false;
        return !d.IsDetached(index)&&d.PartBody(index).isKinematic&&
               d.AppliedCount(index)==0;
    }
    static bool BlockedDockPreservesState(int index)
    {
        var d=Demo();
        if(!d.Detach(index))return false;
        var body=d.PartBody(index);
        var parent=body.transform.parent;
        bool kin=body.isKinematic;
        Vector3 lin=body.linearVelocity,ang=body.angularVelocity;
        int ids=d.AppliedCount(index);
        Physics.Reset();
        var blocker=new GameObject("blocked dock").AddComponent<BoxCollider>();
        Physics.Hits.Add(blocker);
        Physics.Depth=.25f;
        bool result=d.Reattach(index);
        return !result && d.IsDetached(index) && body.isKinematic==kin &&
               body.transform.parent==parent && Equal(body.linearVelocity,lin) &&
               Equal(body.angularVelocity,ang) && d.AppliedCount(index)==ids &&
               Physics.LastMask==-1;
    }
    static bool MissingIdCommitIsNonAtomic(int index)
    {
        var d=Demo();
        if(!d.Detach(index))return false;
        var body=d.PartBody(index);
        if(body.transform.parent!=null || body.isKinematic ||
           d.AppliedCount(index)!=1)return false;
        // Invoke the ORIGINAL W06 emitter's public method, not a fake ledger reset.
        Emitter(d,index).ClearForNewRun();
        if(d.AppliedCount(index)!=0 || !d.IsDetached(index))return false;
        var oldParent=body.transform.parent;
        Vector3 oldVelocity=body.linearVelocity;
        Physics.Reset();
        bool threw=false;
        try{d.Reattach(index);}catch(InvalidOperationException){threw=true;}
        // This should be a RED safety state despite all harness assertions PASS.
        return threw && oldParent==null && body.transform.parent!=null &&
               body.isKinematic && !d.IsDetached(index) &&
               Equal(body.linearVelocity,Vector3.zero) &&
               !Equal(oldVelocity,body.linearVelocity) &&
               d.AppliedCount(index)==0;
    }
    static bool OriginalLedgerDoubleDetachRefused(int index)
    {
        var d=Demo();
        if(!d.Detach(index))return false;
        var e=Emitter(d,index);
        var b=d.PartBody(index);
        int old=d.AppliedCount(index);
        bool repeat=e.TryApplyDetachImpulse(d.PartId(index),b,Vector3.zero);
        return !repeat && old==1 && d.AppliedCount(index)==1;
    }
    static bool LegacyGuiStillUsesGuard()
    {
        var d=Demo();
        if(!d.Detach(0))return false;
        Physics.Reset();
        Physics.Hits.Add(new GameObject("wall").AddComponent<BoxCollider>());
        Physics.Depth=.3f;
        GUILayout.NextButton="Reattach";
        typeof(DebrisRigPlayground).GetMethod("OnGUI",
            BindingFlags.NonPublic|BindingFlags.Instance).Invoke(d,null);
        return d.IsDetached(0) && !d.PartBody(0).isKinematic &&
               d.AppliedCount(0)==1;
    }
    static int Main()
    {
        Check("ORIGINAL_494_SAFE_DOCK_RIG0",()=>CleanDock(0));
        Check("ORIGINAL_494_SAFE_DOCK_RIG1",()=>CleanDock(1));
        Check("ORIGINAL_494_BLOCKER_NO_MUTATION_RIG0",()=>BlockedDockPreservesState(0));
        Check("ORIGINAL_494_BLOCKER_NO_MUTATION_RIG1",()=>BlockedDockPreservesState(1));
        Check("ORIGINAL_W06_DUPLICATE_ID_REFUSED_RIG0",()=>OriginalLedgerDoubleDetachRefused(0));
        Check("ORIGINAL_W06_DUPLICATE_ID_REFUSED_RIG1",()=>OriginalLedgerDoubleDetachRefused(1));
        Check("ORIGINAL_494_LEGACY_GUI_COLLISION_REFUSED",LegacyGuiStillUsesGuard);
        Check("REPRO_PARTIAL_STATE_AFTER_REAL_W06_LEDGER_CLEAR_RIG0",()=>MissingIdCommitIsNonAtomic(0));
        Check("REPRO_PARTIAL_STATE_AFTER_REAL_W06_LEDGER_CLEAR_RIG1",()=>MissingIdCommitIsNonAtomic(1));
        Console.WriteLine("TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail+
            " V2_494_RELEASE_HOLD_PARTIAL_STATE NATIVE_UNITY_NOT_RUN");
        return fail==0?0:1;
    }
}
