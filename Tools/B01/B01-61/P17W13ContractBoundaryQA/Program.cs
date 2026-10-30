// Independent B01-61 W13: compile ORIGINAL new producer schema and unchanged original W06 C#.
// Test PASS means reproducing SOURCE SECURITY RED, NOT Save/Load accepted or native Unity.
using System;
using System.IO;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;

internal static class Program
{
    static int assertions,positive,negative,red;
    static void Check(bool b,string msg)
    {
        assertions++;
        if(!b)throw new Exception("QA_INFRA_ERROR "+msg);
    }
    static PersistedPart Part(int i,bool detached)
    {
        var zero=new Vector3d(0,0,0);
        return new PersistedPart(FrozenPartCatalog.Id(i),FrozenPartCatalog.Owner(i),
            detached?0:FrozenPartCatalog.Owner(i),
            FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
            detached?PartDockState.Detached:PartDockState.Attached,
            detached,!detached,new Vector3d(i+.75,0,-.5),
            0,0,0,1,detached?new Vector3d(.3,0,0):zero,zero,
            FrozenPartCatalog.Mass(i));
    }
    static RunSnapshot Snap(long generation=3)=>
        new RunSnapshot(77,generation,new []{Part(0,true),Part(1,false)});
    static byte[] Wire(long generation=3)=>SnapshotCodec.Encode(Snap(generation));
    static void ValidOriginal()
    {
        byte[] bytes=Wire();
        Check(SnapshotCodec.TryDecode(bytes,out RunSnapshot? read)&&read!=null,
              "real producer binary codec roundtrip");
        Check(read!.Parts[0].Id==2310001&&read.Parts[1].Id==2310002,
              "exact stable frozen #510 IDs");
        Check(ColdRestore.TryStage(bytes,out ColdRestore? staged)&&staged!=null,
              "original producer actual cold-staging");
        Check(staged!.GetLedger(0).Contains(2310001)&&staged.GetLedger(0).Count==1&&
              staged.GetLedger(1).Count==0,"actual unchanged W06 consumed ID reconstruction");
        var h=new ColdSessionHandle();
        Check(h.TrySwap(bytes,77,1,()=>true)==RestoreVerdict.Accepted,
              "actual original logical current pointer swap");
        Check(h.Current!.Snapshot.Generation==3,"committed generation correct");
        Check(h.TrySwap(Wire(2),77,1,()=>true)==RestoreVerdict.StaleGeneration,
              "reject stale generation");
        Check(h.TrySwap(bytes,78,1,()=>true)==RestoreVerdict.WrongRun,
              "reject foreign run");
        Check(h.TrySwap(bytes,77,1,()=>false)==RestoreVerdict.TransactionBusy,
              "reject busy before staging");
        Check(h.TrySwap(bytes,77,1,()=>throw new IOException("fault"))==RestoreVerdict.Quarantine,
              "quarantine host callback exception");
        positive+=9;
    }
    static void NegativeOriginal()
    {
        byte[] bytes=Wire();
        var corrupted=(byte[])bytes.Clone();
        corrupted[corrupted.Length/2]^=0x40;
        Check(!SnapshotCodec.TryDecode(corrupted,out _),"reject corrupted original checksum");
        Check(!SnapshotCodec.TryDecode(null,out _),"reject null bytes");
        Check(!SnapshotCodec.TryDecode(new byte[3],out _),"reject short bytes");
        Check(!SnapshotCodec.TryDecode(new byte[SnapshotCodec.MaxBytes+1],out _),
              "reject oversized bytes");
        var truncated=new byte[bytes.Length-4];
        Array.Copy(bytes,truncated,truncated.Length);
        Check(!SnapshotCodec.TryDecode(truncated,out _),"reject truncated checksum");
        Check(!SnapshotRules.Valid(new RunSnapshot(77,3,new[]{Part(1,false),Part(0,true)})),
              "reject wrong slot ordering");
        Check(!SnapshotRules.Valid(new RunSnapshot(77,3,new[]{Part(0,true),Part(0,true)})),
              "reject duplicate stable ID");
        Check(!SnapshotRules.Valid(new RunSnapshot(77,3,new[]{Part(0,true)})),
              "reject missing rig");
        var noId=new PersistedPart(FrozenPartCatalog.Id(0),FrozenPartCatalog.Owner(0),0,
            FrozenPartCatalog.Asset(0),FrozenPartCatalog.Socket(0),
            PartDockState.Detached,false,false,new Vector3d(0,0,0),
            0,0,0,1,new Vector3d(.1,0,0),new Vector3d(0,0,0),FrozenPartCatalog.Mass(0));
        Check(!SnapshotRules.Valid(new RunSnapshot(77,3,new[]{noId,Part(1,false)})),
              "reject detached missing consumed ID");
        var nan=new PersistedPart(FrozenPartCatalog.Id(0),FrozenPartCatalog.Owner(0),0,
            FrozenPartCatalog.Asset(0),FrozenPartCatalog.Socket(0),
            PartDockState.Detached,true,false,new Vector3d(double.NaN,0,0),
            0,0,0,1,new Vector3d(.1,0,0),new Vector3d(0,0,0),FrozenPartCatalog.Mass(0));
        Check(!SnapshotRules.Valid(new RunSnapshot(77,3,new[]{nan,Part(1,false)})),
              "reject nonfinite position");
        negative+=10;
    }
    static void ActualSecurityWitness()
    {
        byte[] bytes=Wire();
        var h=new ColdSessionHandle();
        Check(h.TrySwap(bytes,77,1,()=>true)==RestoreVerdict.Accepted,
              "valid producer snapshot published");
        var plan=h.Current!;
        Check(SnapshotRules.Valid(plan.Snapshot)&&plan.GetLedger(0).Contains(2310001),
              "valid source snapshot & original W06 ledger before mutation");
        // Actual producer RunSnapshot.Parts readonly ARRAY REFERENCE still permits mutation
        // of elements through exposed public array. No reflection nor source modification.
        plan.Snapshot.Parts[0]=Part(1,false);
        Check(!SnapshotRules.Valid(h.Current!.Snapshot),
              "RED: actual PUBLIC accepted snapshot can become invalid after publish");
        Check(h.Current!.GetLedger(0).Contains(2310001),
              "RED: actual ledger still consumed while published snapshot invalid");
        red++;
        Console.WriteLine("REAL_ORIGINAL_SCHEMA_RED_MUTABLE_ACCEPTED_PART_ARRAY=REPRODUCED");

        var independent=new ColdSessionHandle();
        Check(independent.TrySwap(bytes,77,1,()=>true)==RestoreVerdict.Accepted,
              "second valid source publish");
        var again=independent.Current!;
        Check(again.Snapshot.Parts[0].Consumed && again.GetLedger(0).Contains(2310001),
              "precondition consumed detached ID");
        Check(again.GetLedger(0).RearmAfterReattach(2310001),
              "RED: PUBLIC GetLedger exposes real W06 rearm mutator without docking");
        Check(again.Snapshot.Parts[0].Consumed&&!again.GetLedger(0).Contains(2310001),
              "RED: public source snapshot and original W06 ledger diverge after external rearm");
        red++;
        Console.WriteLine("REAL_ORIGINAL_SCHEMA_RED_MUTABLE_EXPOSED_W06_LEDGER=REPRODUCED");
    }
    public static int Main()
    {
        try
        {
            ValidOriginal();NegativeOriginal();ActualSecurityWitness();
            Check(positive==9&&negative==10&&red==2,"all independent groups");
            Console.WriteLine("ORIGINAL_NEW_SAVE_SCHEMA_QA_EXECUTION_PASS assertions="+assertions+
                " positive="+positive+" negative="+negative+" source_red="+red);
            Console.WriteLine("SAVE_CONTRACT_RELEASE_GATE_RED=MUTABLE_VALIDATED_SNAPSHOT_AND_EXPOSED_LEDGER");
            Console.WriteLine("ORIGINAL_NEW_PRODUCER_AND_W06_DIRECT_CSHARP_EXECUTED=YES");
            Console.WriteLine("UNITY_NATIVE_NOT_RUN; PHYSICAL_510_REHYDRATION_NOT_RUN; G2_HOLD");
            return 0;
        }
        catch(Exception ex){Console.Error.WriteLine("QA_TEST_INFRA_FAIL "+ex);return 1;}
    }
}