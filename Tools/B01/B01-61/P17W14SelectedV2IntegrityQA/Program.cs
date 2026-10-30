// B01-61 W14: INDEPENDENT C# execution of EXACT selected producer #554 V2 and
// ORIGINAL unchanged W06 ledger / frozen #510 atomic core. No UnityEngine mock.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

internal static class SelectedContractV2QA
{
    static int assertions, roundTrips, rejectedMutations, atomicDenials, fixedRed, newRed;
    const long Run=77;
    static void Check(bool ok,string name)
    {
        assertions++;
        if(!ok)throw new InvalidOperationException("QA_ASSERTION_FAILED: "+name);
    }
    static void Denied(bool ok,string name)
    {
        Check(ok,name);rejectedMutations++;
    }
    static PersistedPart Part(int slot,bool detached)
    {
        var zero=new Vector3d(0,0,0);
        return new PersistedPart(FrozenPartCatalog.Id(slot),FrozenPartCatalog.Owner(slot),
            detached?0:FrozenPartCatalog.Owner(slot),
            FrozenPartCatalog.Asset(slot),FrozenPartCatalog.Socket(slot),
            detached?PartDockState.Detached:PartDockState.Attached,
            detached,!detached,new Vector3d(slot+.6,.75,-.3),
            0,0,0,1,detached?new Vector3d(.2,1.4,-.15):zero,
            detached?new Vector3d(.05,0,-.02):zero,FrozenPartCatalog.Mass(slot));
    }
    static RunSnapshot State(long generation=5,bool a=true,bool b=false)
        => new RunSnapshot(Run,generation,new[]{Part(0,a),Part(1,b)});
    static byte[] Wire(long generation=5,bool a=true,bool b=false)
        => SnapshotCodec.Encode(State(generation,a,b));
    static byte[] Rewrite(byte[] original,Action<byte[]> action)
    {
        var buffer=(byte[])original.Clone();
        action(buffer);
        using(var sha=SHA256.Create())
        {
            byte[] digest=sha.ComputeHash(buffer,0,buffer.Length-32);
            Array.Copy(digest,0,buffer,buffer.Length-32,32);
        }
        return buffer;
    }
    static void VerifyRealW06AndOriginalR2()
    {
        for(int mask=0;mask<4;mask++)
        {
            bool a=(mask&1)!=0,b=(mask&2)!=0;
            byte[] bytes=Wire(5,a,b);
            Check(SnapshotCodec.TryDecode(bytes,out RunSnapshot? decoded)&&decoded!=null,
                "v2 actual source binary roundtrip");
            Check(decoded!.Parts.Length==2 && decoded.Parts[0].Id==SecureRigCatalog.Light.PartId &&
                decoded.Parts[1].Id==SecureRigCatalog.Heavy.PartId,
                "frozen original #510 stable IDs 2310001/2310002");
            Check(decoded.Parts[0].State==(a?PartDockState.Detached:PartDockState.Attached)&&
                decoded.Parts[1].State==(b?PartDockState.Detached:PartDockState.Attached),
                "all original two-rig detach state combinations");
            Check(ColdRestore.TryStage(bytes,out ColdRestore? cold)&&cold!=null,
                "real original W06 ledger cold rebuild");
            for(int slot=0;slot<2;slot++)
            {
                bool expected=(mask&(1<<slot))!=0;
                var rec=decoded.Parts[slot];
                Check(cold!.IsConsumed(slot,rec.Id)==expected &&
                    cold.ConsumedCount(slot)==(expected?1:0),
                    "exact original W06 source consumed ID state restored per slot");
                Check(rec.OwnerId==FrozenPartCatalog.Owner(slot)&&
                    rec.AssetGuid==FrozenPartCatalog.Asset(slot)&&
                    rec.SocketGuid==FrozenPartCatalog.Socket(slot)&&
                    rec.ParentId==(expected?0:rec.OwnerId)&&
                    rec.MassKg==FrozenPartCatalog.Mass(slot),
                    "original source durable part identity, ownership and attachment");
            }
            roundTrips++;
        }
        var ledger=new AtomicDockLedger();
        var zero=new Vector3d(0,0,0);
        Check(ledger.TryDetach(SecureRigCatalog.Light.PartId,zero,zero,zero,1,0,0,
                 out Vector3d impulse)&&impulse.MagnitudeSquared==0,
              "actual original R2+W06 consumed ID initialized offstage");
        int rollbackCalls=0;
        Check(!ledger.TryReattach(SecureRigCatalog.Light.PartId,
               ()=>throw new InvalidOperationException("simulated apply failure"),
               ()=>rollbackCalls++),"actual original R2 failed physical callback transaction");
        Check(rollbackCalls==1 && ledger.Contains(SecureRigCatalog.Light.PartId) &&
              ledger.Count==1&&!ledger.Busy,
              "original W06 ID preserved on atomic rollback");
    }
    static void VerifyTwoEarlier550RedsNowBlocked()
    {
        var originalInput=new[]{Part(0,true),Part(1,false)};
        var snapshot=new RunSnapshot(Run,5,originalInput);
        originalInput[0]=originalInput[1];
        Check(SnapshotRules.Valid(snapshot)&&snapshot.Parts[0].Id==2310001,
            "defensive source constructor copy prevents caller postvalidation mutation");
        var obtained=snapshot.Parts;
        obtained[0]=obtained[1];
        Check(SnapshotRules.Valid(snapshot)&&snapshot.Parts[0].Id==2310001,
            "v2 snapshot getter returns defensive array copy");
        var session=new ColdSessionHandle();
        Check(session.TrySwap(Wire(),Run,1,()=>true)==RestoreVerdict.Accepted,
              "genuine v2 cold logical session publication");
        var validated=session.Current!;
        var copy=validated.Snapshot.Parts;
        copy[0]=copy[1];
        Check(SnapshotRules.Valid(validated.Snapshot) &&
              validated.Snapshot.Parts[0].Id==2310001 &&
              validated.IsConsumed(0,2310001)&&validated.ConsumedCount(0)==1,
              "original #550 mutable published snapshot witness no longer changes committed ID");
        var publicMethods=typeof(ColdRestore).GetMethods(BindingFlags.Instance|
                          BindingFlags.DeclaredOnly|BindingFlags.Public);
        Check(!publicMethods.Any(m=>m.ReturnType==typeof(DebrisImpulseLedger) ||
              m.ReturnType==typeof(AtomicDockLedger) ||
              m.Name=="GetLedger"||m.Name=="RearmAfterReattach"||m.Name=="ClearForNewRun"),
              "original #550 mutable W06 ledger API removed from v2 public surface");
        Check(validated.IsConsumed(0,2310001) && validated.ConsumedCount(0)==1 &&
              !validated.IsConsumed(1,2310001),
              "opaque W06 ledger offers slot-guarded read-only access");
        fixedRed=2;
        Console.WriteLine("ORIGINAL_550_RED_FIXED_V2=POSTVALIDATION_ARRAY_COPY_AND_OPAQUE_W06_LEDGER_PASS");
    }
    static void VerifyBadBytesAndAtomicLogicalRejection()
    {
        byte[] good=Wire();
        byte[] changed=Rewrite(good,bytes=>bytes[0]=(byte)'X');
        Denied(!SnapshotCodec.TryDecode(changed,out _),"magic rejects recomputed checksum corruption");
        changed=Rewrite(good,bytes=>{
            byte[] old=Encoding.ASCII.GetBytes("B01SV13!");
            Array.Copy(old,bytes,old.Length);
            Array.Copy(BitConverter.GetBytes(1),0,bytes,8,4);
        });
        Denied(!SnapshotCodec.TryDecode(changed,out _),"full old W13 v1 magic+schema rejected after valid SHA");
        changed=Rewrite(good,bytes=>Array.Copy(Encoding.ASCII.GetBytes(
            "baf65f2664e38f3b9c38815c3f07285f8107c1b6"),0,bytes,12,40));
        Denied(!SnapshotCodec.TryDecode(changed,out _),"alternate #511 source SHA refused");
        changed=Rewrite(good,bytes=>Array.Copy(BitConverter.GetBytes((long)999),0,bytes,72,8));
        Denied(!SnapshotCodec.TryDecode(changed,out _),"unknown original part ID rejected valid hash");
        changed=Rewrite(good,bytes=>Array.Copy(BitConverter.GetBytes((long)2310001),0,bytes,275,8));
        Denied(!SnapshotCodec.TryDecode(changed,out _),"duplicate ID second slot rejected valid hash");
        var smaller=new byte[good.Length-1];
        Array.Copy(good,smaller,smaller.Length);
        Denied(!SnapshotCodec.TryDecode(smaller,out _),"truncated save rejected");
        Denied(!SnapshotCodec.TryDecode(new byte[SnapshotCodec.MaxBytes+1],out _),
            "oversized save rejected");
        Denied(!SnapshotCodec.TryDecode(null,out _),"null save rejected");
        var handle=new ColdSessionHandle();
        Check(handle.TrySwap(good,Run,1,()=>true)==RestoreVerdict.Accepted,
            "precondition current valid checkpoint");
        var prior=handle.Current;
        Check(prior!=null&&prior.IsConsumed(0,2310001),"precondition consumed ledger");
        atomicDenials=0;
        void Preserve(RestoreVerdict result,RestoreVerdict expected,string name)
        {
            Check(result==expected,name+" returns expected refusal");
            Check(object.ReferenceEquals(handle.Current,prior) &&
                  handle.Current!.IsConsumed(0,2310001)&&
                  handle.Current.Snapshot.Parts[0].Id==2310001,
                  name+" untouched old logical pointer and ID state");
            atomicDenials++;
        }
        Preserve(handle.TrySwap(changed,Run,1,()=>true),RestoreVerdict.InvalidSnapshot,
                 "last invalid changed bytes rollback");
        Preserve(handle.TrySwap(Wire(4),Run,1,()=>true),RestoreVerdict.StaleGeneration,
                 "stale original generation rollback");
        Preserve(handle.TrySwap(good,Run+1,1,()=>true),RestoreVerdict.WrongRun,
                 "foreign run rollback");
        Preserve(handle.TrySwap(good,Run,1,()=>false),RestoreVerdict.TransactionBusy,
                 "live host busy rejection");
        Preserve(handle.TrySwap(good,Run,1,()=>throw new IOException("host failed")),
                 RestoreVerdict.Quarantine,"throwing host callback quarantine");
        Console.WriteLine("EXACT_V2_NEGATIVE_INPUT_AND_ROLLBACK_PROTECTION_PASS="+rejectedMutations+
            " invalid_formats_and_"+atomicDenials+"_logical_state_preservation_cases");
    }
    static void VerifyReentrantHostIdle()
    {
        var h=new ColdSessionHandle();
        RestoreVerdict nested=RestoreVerdict.InvalidSnapshot;
        var outer=h.TrySwap(Wire(6,true,false),Run,1,()=>
        {
            nested=h.TrySwap(Wire(6,false,true),Run,1,()=>true);
            return true;
        });
        // This is a new independent reentrancy/linearization witness:
        // pre-publish hostIdle callback is evaluated while swapping=false,
        // permitting two successful loads in one synchronous outer transaction.
        Check(nested==RestoreVerdict.Accepted && outer==RestoreVerdict.Accepted,
              "actual original v2 both nested and outer load accepted at same generation");
        Check(h.Current!.Snapshot.Parts[0].State==PartDockState.Detached &&
              h.Current.IsConsumed(0,2310001)&&!h.Current.IsConsumed(1,2310002),
              "outer silently replaced valid different nested same-gen snapshot");
        newRed++;
        Console.WriteLine("V2_NEW_SOURCE_REENTRANT_HOST_IDLE_DOUBLE_ACCEPT_RED=REPRODUCED");
    }
    public static int Main()
    {
        try
        {
            VerifyRealW06AndOriginalR2();
            VerifyTwoEarlier550RedsNowBlocked();
            VerifyBadBytesAndAtomicLogicalRejection();
            VerifyReentrantHostIdle();
            Check(roundTrips==4 && fixedRed==2 && rejectedMutations==8 &&
                  atomicDenials==5 && newRed==1,
                  "all independent original producer source cases completed");
            Console.WriteLine("V2_EXACT_ORIGINAL_SOURCE_QA_EXECUTION_PASS assertions="+assertions+
                 " roundtrips="+roundTrips+" fixed_550="+fixedRed+
                 " rejected_forged="+rejectedMutations+" rollback_atomic="+atomicDenials+
                 " reentrancy_red="+newRed);
            Console.WriteLine("V2_SELECTED_SAVE_SOURCE_RELEASE_HOLD=REENTRANT_HOST_IDLE_DOUBLE_ACCEPT");
            Console.WriteLine("ORIGINAL_FROZEN510_R2_W06_CSHARP_EXECUTED=YES");
            Console.WriteLine("ACTUAL_UNITY_PHYSX=NOT_RUN; LIVE_SAVE_RESTORE=NOT_INTEGRATED; G2=HOLD");
            return 0; // QA success is a reproduced red witness, NOT contract safety acceptance.
        }
        catch(Exception e){Console.Error.WriteLine("QA_INFRA_FAILURE "+e);return 1;}
    }
}
