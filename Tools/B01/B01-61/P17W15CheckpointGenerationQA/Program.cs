// B01-61 PASS17 WAVE15. Execute exact ORIGINAL frozen #556 R2 Save codec, original
// W06 ledger, and #510 rig/atomic ledger directly. This is SOURCE QA; no Unity.
using System;
using System.IO;
using System.Security.Cryptography;
using System.Reflection;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

internal static class Program
{
    static int checks, negatives, positive, collision;
    const long Run=77;
    const long Generation=9;
    static void Assert(bool valid,string label)
    {
        checks++;
        if(!valid)throw new InvalidOperationException("B01_61_W15_QA_FAILURE "+label);
    }
    static PersistedPart Part(int slot,bool detached)
    {
        var zero=new Vector3d(0,0,0);
        return new PersistedPart(
            FrozenPartCatalog.Id(slot),FrozenPartCatalog.Owner(slot),
            detached?0:FrozenPartCatalog.Owner(slot),
            FrozenPartCatalog.Asset(slot),FrozenPartCatalog.Socket(slot),
            detached?PartDockState.Detached:PartDockState.Attached,
            detached,!detached,new Vector3d(slot+0.25,.5,-.5),
            0,0,0,1,detached?new Vector3d(.5,-.3,.2):zero,
            detached?new Vector3d(.1,.0,-.05):zero,
            FrozenPartCatalog.Mass(slot));
    }
    static byte[] Bytes(long generation,bool light,bool heavy)=>
        SnapshotCodec.Encode(new RunSnapshot(Run,generation,
            new[]{Part(0,light),Part(1,heavy)}));
    static string Hash(byte[] buffer)=>
        Convert.ToHexString(SHA256.HashData(buffer)).ToLowerInvariant();
    static void State(ColdSessionHandle h,bool light,bool heavy,long generation,string label)
    {
        var current=h.Current;
        Assert(current!=null && current.Snapshot.RunId==Run &&
            current.Snapshot.Generation==generation,label+" run/generation");
        var records=current!.Snapshot.Parts;
        Assert(records.Length==2 && records[0].Id==SecureRigCatalog.Light.PartId &&
            records[1].Id==SecureRigCatalog.Heavy.PartId,label+" exact original #510 IDs");
        Assert(records[0].OwnerId==FrozenPartCatalog.Owner(0) &&
            records[1].OwnerId==FrozenPartCatalog.Owner(1) &&
            records[0].AssetGuid==FrozenPartCatalog.Asset(0) &&
            records[1].SocketGuid==FrozenPartCatalog.Socket(1),
            label+" owner/asset/socket");
        Assert(records[0].ParentId==(light?0:records[0].OwnerId) &&
            records[1].ParentId==(heavy?0:records[1].OwnerId),
            label+" canonical detached parent");
        Assert(records[0].State==(light?PartDockState.Detached:PartDockState.Attached) &&
            records[1].State==(heavy?PartDockState.Detached:PartDockState.Attached),
            label+" decoded detached flags");
        Assert(current.ContainsConsumedId(0,records[0].Id)==light &&
            current.ContainsConsumedId(1,records[1].Id)==heavy &&
            current.ConsumedCount(0)==(light?1:0) &&
            current.ConsumedCount(1)==(heavy?1:0),
            label+" REAL unchanged W06 ID/ledger coupling");
    }
    static void Write(string path)
    {
        var bytes=Bytes(Generation,true,false);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllBytes(path,bytes); // QA fixture: NOT durable production atomic writer
        var actual=File.ReadAllBytes(path);
        Assert(Hash(bytes)==Hash(actual)&&bytes.Length==actual.Length,"actual disk write/read bytes");
        Assert(SnapshotCodec.TryDecode(actual,out var restored)&&restored!=null,
            "exact original #556 producer decodes physically saved file");
        Assert(restored!.Parts[0].Id==SecureRigCatalog.Light.PartId &&
            restored.Parts[1].Id==SecureRigCatalog.Heavy.PartId,
            "disk fixture retains exact selected #510 IDs");
        Console.WriteLine("SOURCE_CHECKPOINT_DISK_WRITTEN bytes="+bytes.Length+
            " sha256="+Hash(bytes)+" source556=yes");
    }
    static void ReadFreshProcess(string path)
    {
        var disk=File.ReadAllBytes(path);
        var session=new ColdSessionHandle(); // new CLR process; W06 ledgers rebuilt from file
        Assert(session.Current==null,"fresh process begins without shared managed ledger");
        Assert(session.TrySwap(disk,Run,Generation,()=>true)==RestoreVerdict.Accepted,
            "exact original #556 logical cold-load accepted on fresh process");
        State(session,true,false,Generation,"fresh-process cold restore");
        var copy=session.Current!.Snapshot.Parts;
        copy[0]=copy[1]; // adversarial mutation cannot rewrite original private array
        State(session,true,false,Generation,"published snapshot immutable");
        Assert(typeof(ColdRestore).GetMethod("GetLedger",
            BindingFlags.Public|BindingFlags.Instance)==null,
            "frozen selected #556 protects original W06 mutable ledger from callers");
        positive++;
        Console.WriteLine("SOURCE_CROSS_PROCESS_COLD_RESTORE_PASS bytes="+disk.Length+
            " sha256="+Hash(disk)+" W06_consumed_light=true W06_consumed_heavy=false");
    }
    static void Collision(string path)
    {
        byte[] saved=File.ReadAllBytes(path);
        var session=new ColdSessionHandle();
        Assert(session.TrySwap(saved,Run,Generation,()=>true)==RestoreVerdict.Accepted,
            "initial actual disk checkpoint published");
        State(session,true,false,Generation,"initial checkpoint");
        var initial=session.Current;
        var conflicting=Bytes(Generation,false,true);
        Assert(Hash(saved)!=Hash(conflicting),
            "two physically distinct snapshots at same run+generation");
        Assert(SnapshotCodec.TryDecode(conflicting,out var parsed)&&parsed!=null,
            "conflicting checkpoint is individually valid, not corrupt");
        // Exact production #556 source uses Current.Generation > candidate.Generation
        // rather than >=, and has no same-generation content hash consistency rule.
        // This admits silent, state-changing replay with the SAME version counter.
        var verdict=session.TrySwap(conflicting,Run,Generation,()=>true);
        Assert(verdict==RestoreVerdict.Accepted,
            "SOURCE RED reproduced: conflicting same-gen state accepted by original #556");
        Assert(!Object.ReferenceEquals(initial,session.Current),
            "SOURCE RED published pointer overwritten at same generation");
        State(session,false,true,Generation,"source same-generation changed checkpoint");
        collision++;
        Console.WriteLine("SOURCE_SAVE_GENERATION_CONFLICT_RED=DIFFERENT_STATES_SAME_RUN_SAME_GENERATION_BOTH_ACCEPTED");
        Console.WriteLine("SAVED_HASH="+Hash(saved)+" CONFLICTING_HASH="+Hash(conflicting));
    }
    static void NegativeAndRollback(string path)
    {
        var bytes=File.ReadAllBytes(path);
        var handle=new ColdSessionHandle();
        Assert(handle.TrySwap(bytes,Run,Generation,()=>true)==RestoreVerdict.Accepted,
            "reference snapshot accepted before negative rollback probes");
        var reference=handle.Current;
        void Deny(byte[] candidate,long run,long min,Func<bool> idle,
                  RestoreVerdict expected,string label)
        {
            Assert(handle.TrySwap(candidate,run,min,idle)==expected,label+" return");
            Assert(Object.ReferenceEquals(reference,handle.Current),
                label+" no intermediate logical world/pointer commit");
            State(handle,true,false,Generation,label+" original W06 consumed ID");
            negatives++;
        }
        var corrupted=(byte[])bytes.Clone();
        corrupted[corrupted.Length/2]^=0x20;
        Deny(corrupted,Run,Generation,()=>true,RestoreVerdict.InvalidSnapshot,"checksum bit flip");
        var truncated=new byte[bytes.Length-1];
        Array.Copy(bytes,truncated,truncated.Length);
        Deny(truncated,Run,Generation,()=>true,RestoreVerdict.InvalidSnapshot,"truncated disk");
        var extended=new byte[bytes.Length+1];
        Array.Copy(bytes,extended,bytes.Length);
        Deny(extended,Run,Generation,()=>true,RestoreVerdict.InvalidSnapshot,"extra trailing byte");
        Deny(Bytes(Generation-1,false,true),Run,Generation,()=>true,
             RestoreVerdict.StaleGeneration,"stale generation");
        Deny(bytes,Run+1,Generation,()=>true,RestoreVerdict.WrongRun,"foreign run");
        Deny(Bytes(Generation+1,false,true),Run,Generation,()=>false,
             RestoreVerdict.TransactionBusy,"busy host");
        Deny(Bytes(Generation+1,false,true),Run,Generation,
             ()=>throw new IOException("simulated host callback fault"),
             RestoreVerdict.Quarantine,"callback exception");
        int idleCalls=0;
        Deny(Bytes(Generation+1,false,true),Run,Generation,()=>
             {idleCalls++;return idleCalls==1;},RestoreVerdict.TransactionBusy,
             "host becomes busy between stage and commit");
        Assert(idleCalls==2,"real original #556 hostIdle double-check executed");
        positive++;
        Console.WriteLine("SOURCE_FAILURE_ATOMIC_LOGICAL_RESTORE_PASS cases="+negatives+
            " pointer_unchanged=true real_W06_ID_unchanged=true");
    }
    static void BusyNested()
    {
        var handle=new ColdSessionHandle();
        RestoreVerdict nested=RestoreVerdict.Quarantine;
        var outer=handle.TrySwap(Bytes(Generation,true,false),Run,Generation,()=>
        {
            nested=handle.TrySwap(Bytes(Generation,false,true),Run,Generation,()=>true);
            return true;
        });
        Assert(outer==RestoreVerdict.Accepted&&nested==RestoreVerdict.TransactionBusy,
            "exact original #556 prevents reentrant nested publication");
        State(handle,true,false,Generation,"no accidental nested accepted world");
        positive++;
        Console.WriteLine("SOURCE_REENTRANT_GUARD_556_PASS nested=TransactionBusy");
    }
    static void OriginalAtomicLedger()
    {
        var ledger=new AtomicDockLedger();
        var zero=new Vector3d(0,0,0);
        Assert(ledger.TryDetach(SecureRigCatalog.Light.PartId,zero,zero,zero,1,0,0,
               out var impulse) && impulse.MagnitudeSquared==0,
               "original frozen #510 R2 actually rehydrates consumed ID without impulse");
        int rolledBack=0;
        Assert(!ledger.TryReattach(SecureRigCatalog.Light.PartId,
            ()=>throw new IOException("physical commit fault"),
            ()=>rolledBack++),"original frozen #510 R2 rejects faulty physical dock");
        Assert(rolledBack==1 && ledger.Contains(SecureRigCatalog.Light.PartId) &&
            ledger.Count==1&&!ledger.Busy,
            "original W06 consumed ID remains after atomic failure");
        positive++;
    }
    public static int Main(string[] args)
    {
        try
        {
            if(args.Length!=2)throw new ArgumentException("mode [write|read|collision|negative] checkpoint_path");
            switch(args[0])
            {
                case "write": Write(args[1]);break;
                case "read": ReadFreshProcess(args[1]);break;
                case "collision": Collision(args[1]);break;
                case "negative": NegativeAndRollback(args[1]);BusyNested();OriginalAtomicLedger();break;
                default:throw new ArgumentException("unknown QA mode");
            }
            Console.WriteLine("ORIGINAL_556_SOURCE_QA_EXECUTED mode="+args[0]+
                 " assertions="+checks+" positives="+positive+" negatives="+negatives+
                 " same_generation_source_red="+collision);
            Console.WriteLine("ACTUAL_UNITY_G1=NOT_RUN; ACTUAL_HOST_SAVELOAD=NOT_INTEGRATED; NO_MERGE");
            return 0; // expected release RED witness is CI-success (test of known defect)
        }
        catch(Exception ex){Console.Error.WriteLine("INDEPENDENT_QA_FAILURE "+ex);return 1;}
    }
}
