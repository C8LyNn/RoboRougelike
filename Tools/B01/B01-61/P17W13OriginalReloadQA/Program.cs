// B01-61 W13: EXECUTE exact #510 unchanged original Unity-host C# + original W06 ledger
// to demonstrate that a serialized detached/consumed-ID snapshot is NOT restored
// by the original game's public BuildDemo/ResetForNewRun API.
// UnityEngine is a QA-only synthetic stub: this is SOURCE-ONLY, NOT native Unity/PhysX.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using B01.SecureReattachV2;
using UnityEngine;

public sealed class RecordedPart
{
    public long StableId {get;set;}
    public bool Detached {get;set;}
    public bool Consumed {get;set;}
    public float[] Position {get;set;} = Array.Empty<float>();
    public float[] LinearVelocity {get;set;} = Array.Empty<float>();
    public float[] AngularVelocity {get;set;} = Array.Empty<float>();
}
public sealed class RecordedRun
{
    public int SchemaVersion {get;set;}
    public string ProducerSha {get;set;}="";
    public List<RecordedPart> Parts {get;set;} = new List<RecordedPart>();
}
internal static class OriginalReloadWitness
{
    const string PRODUCER="d94400d95d13db7f27e1533df05c9292d5380431";
    static int assertions, lossWitnesses, gateDenials, resetWitnesses, positiveControls;
    static void Check(bool ok,string reason)
    {
        assertions++;
        if(!ok)throw new Exception("INDEPENDENT_ORIGINAL_SOURCE_QA_FAIL: "+reason);
    }
    static SecureDebrisPlaygroundV2 Demo()
    {
        Physics.Reset();
        var host=new GameObject("original frozen #510 source fixture");
        var demo=host.AddComponent<SecureDebrisPlaygroundV2>();
        demo.BuildDemo();
        return demo;
    }
    static AtomicDockLedger Ledger(SecureDebrisPlaygroundV2 d,int index)
    {
        var f=typeof(SecureDebrisPlaygroundV2).GetField("rigs",
            BindingFlags.NonPublic|BindingFlags.Instance);
        Check(f!=null,"original frozen controller private rig array");
        var rig=((Array)f.GetValue(d)).GetValue(index);
        var lf=rig.GetType().GetField("Ledger");
        Check(lf!=null,"original source real per-rig atomic W06 ledger");
        return (AtomicDockLedger)lf.GetValue(rig);
    }
    static float[] V(Vector3 x)=>new float[]{x.x,x.y,x.z};
    static bool VecEqual(float[] old,Vector3 actual)=>
        old!=null && old.Length==3 && old[0]==actual.x &&
        old[1]==actual.y && old[2]==actual.z;
    static bool Alias(SecureDebrisPlaygroundV2 d,int index,int route)
    {
        if(route==0)return d.Reattach(index);
        if(route==1)return d.TrySafeReattach(index);
        if(route==2)return d.ReattachFromGui(index);
        throw new ArgumentOutOfRangeException(nameof(route));
    }
    static RecordedRun Snapshot(SecureDebrisPlaygroundV2 d)
    {
        var record=new RecordedRun{SchemaVersion=1,ProducerSha=PRODUCER};
        for(int i=0;i<2;i++)
        {
            var body=d.PartBody(i);
            record.Parts.Add(new RecordedPart{
                StableId=d.PartId(i), Detached=d.IsDetached(i),
                Consumed=Ledger(d,i).Contains(d.PartId(i)),
                Position=V(body.transform.position),
                LinearVelocity=V(body.linearVelocity),
                AngularVelocity=V(body.angularVelocity)
            });
        }
        return record;
    }
    static void PrepareBothDetached(SecureDebrisPlaygroundV2 d)
    {
        for(int i=0;i<2;i++)
        {
            Check(d.Detach(i),"actual original V2 detach rig "+i);
            var b=d.PartBody(i);
            b.transform.position=b.transform.position+new Vector3(.17f*(i+1),.1f,-.03f);
            b.linearVelocity=new Vector3(1.4f+i,.5f,-.25f);
            b.angularVelocity=new Vector3(-.1f,.2f+i,.3f);
            Check(d.IsDetached(i) && Ledger(d,i).Contains(d.PartId(i)),
                  "original W06 consumed ID and true detached rig "+i);
        }
    }
    static void PostReloadLoss()
    {
        var original=Demo();
        PrepareBothDetached(original);
        // JSON is a QA observation only. It is NOT a production save adapter.
        var json=JsonSerializer.Serialize(Snapshot(original));
        var recorded=JsonSerializer.Deserialize<RecordedRun>(json);
        Check(recorded!=null && recorded.SchemaVersion==1 &&
              recorded.ProducerSha==PRODUCER && recorded.Parts.Count==2,
              "QA fixture JSON bytes survive an actual CLR serialization roundtrip");
        Check(recorded.Parts[0].StableId!=recorded.Parts[1].StableId,
              "two unique actual stable part IDs");
        Check(recorded.Parts.All(p=>p.Detached&&p.Consumed),
              "saved observational snapshot has two detached consumed IDs");
        var reloaded=Demo(); // new original source instance, as after rebuilding a run
        for(int i=0;i<2;i++)
        {
            var prior=recorded.Parts[i];
            var newbody=reloaded.PartBody(i);
            Check(prior.StableId==reloaded.PartId(i),"stable ID numerically unchanged after new BuildDemo");
            Check(!reloaded.IsDetached(i) && !Ledger(reloaded,i).Contains(prior.StableId),
                  "new source instance loses detached and consumed ledger state");
            Check(newbody.isKinematic && newbody.transform.parent!=null,
                  "new source instance attaches same ID instead of rehydrating it");
            Check(!VecEqual(prior.Position,newbody.transform.position) ||
                  !VecEqual(prior.LinearVelocity,newbody.linearVelocity),
                  "new instance does not restore saved source pose or velocity");
            lossWitnesses++;
            for(int alias=0;alias<3;alias++)
            {
                var parent=newbody.transform.parent;
                var velocity=newbody.linearVelocity;
                Check(!Alias(reloaded,i,alias),
                      "all public R2 routes reject a reloaded rig not marked detached");
                Check(reloaded.LastDockResult==DockResult.NotDetached,
                      "gate says NotDetached instead of restoring saved consumed ID");
                Check(newbody.transform.parent==parent && newbody.linearVelocity==velocity,
                      "no unintended mutation from denied post-reload route");
                gateDenials++;
            }
        }
        Console.WriteLine("REAL_ORIGINAL_SOURCE_RELOAD_LOSS_REPRODUCED: 2 stable IDs intact but detached/ledger/pose unavailable");
        Console.WriteLine("JSON_CAPTURE_QA_FIXTURE_ONLY: production serializer/restore NOT IMPLEMENTED");
    }
    static void ResetDestroysDetachedHistory()
    {
        var d=Demo();
        PrepareBothDetached(d);
        var original=Snapshot(d);
        var old0=d.PartBody(0).gameObject;
        var old1=d.PartBody(1).gameObject;
        d.ResetForNewRun();
        Check(old0.Destroyed && old1.Destroyed,
              "original Unity-host reset destroys both real source detached objects");
        d.BuildDemo();
        for(int i=0;i<2;i++)
        {
            Check(d.PartId(i)==original.Parts[i].StableId,"stable numeric ID survives same host reset");
            Check(!d.IsDetached(i) && d.AppliedCount(i)==0,
                  "original new run silently resets detached/consumed state");
            resetWitnesses++;
        }
    }
    static void ResetDuringAtomicCommitRejected()
    {
        var d=Demo();
        Check(d.Detach(0),"detach for reset-during-commit probe");
        var ledger=Ledger(d,0);
        var body=d.PartBody(0);
        long id=d.PartId(0);
        Check(ledger.Contains(id),"precondition: actual W06 ledger ID consumed");
        bool resetBlocked=false;
        bool result=ledger.TryReattach(id,()=>
        {
            try{d.ResetForNewRun();}
            catch(InvalidOperationException){resetBlocked=true;throw;}
        },()=>{});
        Check(!result&&resetBlocked,"real ResetForNewRun blocked during source atomic transaction");
        Check(d.IsDetached(0)&&ledger.Contains(id)&&!ledger.Busy,
              "reset rejected with original detached state and consumed ID restored");
        Check(!body.gameObject.Destroyed,"reset did not destroy original body mid transaction");
        positiveControls++;
    }
    static void DetachedThenLegitimateDock()
    {
        var d=Demo();
        for(int i=0;i<2;i++)
        {
            Check(d.Detach(i),"new run actual detach");
            var ledger=Ledger(d,i);
            Check(ledger.Contains(d.PartId(i)),"real W06 ledger has ID");
            Check(d.Reattach(i),"original actual safe reattach on same run");
            Check(!d.IsDetached(i)&&ledger.Count==0,"atomic rearm commits and clears consumed ledger");
            positiveControls++;
        }
    }
    public static int Main()
    {
        try
        {
            PostReloadLoss();
            ResetDestroysDetachedHistory();
            ResetDuringAtomicCommitRejected();
            DetachedThenLegitimateDock();
            Check(lossWitnesses==2&&gateDenials==6&&resetWitnesses==2&&positiveControls==3,
                  "all original source persistence-loss and healthy transaction paths executed");
            Console.WriteLine("ORIGINAL_SOURCE_RELOAD_QA_PASS assertions="+assertions+
                " lost_id_states="+lossWitnesses+" public_denials="+gateDenials+
                " reset_loss="+resetWitnesses+" transactional_controls="+positiveControls);
            Console.WriteLine("SAVE_REHYDRATION_RELEASE_GATE_RED=NO_PRODUCTION_PERSISTENCE_OR_RESTORE_API");
            Console.WriteLine("EXACT_FROZEN_510_HOST_AND_W06_LEDGER_EXECUTED=YES");
            Console.WriteLine("NATIVE_UNITY=NOT_RUN; SYNTHETIC_PHYSICS_BOUNDARY=YES; CANONICAL_SAVE_LOAD=NOT_RUN");
            return 0;
        }
        catch(Exception e)
        {
            Console.Error.WriteLine("SOURCE_RELOAD_QA_FAILURE "+e);
            return 1;
        }
    }
}