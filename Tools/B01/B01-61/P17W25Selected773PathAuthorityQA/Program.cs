#nullable enable
// B01-61 P17 W25 independently executes exact ORIGINAL R2 #773
// PUBLIC STORE, real #729 OS .owner and frozen #556/W06 binary.
// Negative Linux mixed-case alias: case-insensitive C# string equality
// must NOT authorize two physical directories on case-sensitive filesystem.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using B01.CanonicalWorldApi001;
using B01.CanonicalSingleAuthorityStore004;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
internal static class QA61
{
    const long Run=61225;
    static readonly Guid Slot=Guid.Parse("61225000-6125-4125-8125-612561256125");
    static int pass,fail,child;
    static void Test(string tag,Func<string,bool> action)
    {
        string root=Path.Combine(Path.GetTempPath(),"b0161-w25-773-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{
            string path=Path.Combine(root,"trusted");
            Directory.CreateDirectory(path);
            if(action(path)){pass++;Console.WriteLine("PASS "+tag);}
            else{fail++;Console.WriteLine("FAIL "+tag);}
        }
        catch(Exception ex){fail++;Console.WriteLine("FAIL "+tag+" "+ex);}
        finally{try{Directory.Delete(root,true);}catch(IOException){}
                catch(UnauthorizedAccessException){}}
    }
    static string Canon(string root)=>Path.Combine(root,
        "b0162_canonical_single_authority_v3",
        "run_"+Run+"_slot_"+Slot.ToString("N"));
    static string CaseAlias(string root)=>Path.Combine(root,
        "b0162_CANONICAL_single_authority_v3",
        "run_"+Run+"_slot_"+Slot.ToString("N"));
    static byte[] Bytes(long gen,bool light,bool heavy)
    {
        var parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool d=i==0?light:heavy;
            var s=SecureRigCatalog.At(i);
            long o=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(s.PartId,o,d?0:o,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                d?PartDockState.Detached:PartDockState.Attached,d,!d,
                new Vector3d(s.RootX+s.PartOffsetX,d?2.2:1.2,0),
                0,0,0,1,new Vector3d(d?0.25:0,0,0),
                new Vector3d(0,0,0),s.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(Run,gen,parts));
    }
    static bool Verify(byte[] b,long gen,bool light,bool heavy)
        =>ColdRestore.TryStage(b,out var cold)&&cold!=null&&
           cold.Snapshot.RunId==Run&&cold.Snapshot.Generation==gen&&
           cold.ContainsConsumedId(0,2310001)==light&&
           cold.ContainsConsumedId(1,2310002)==heavy;
    sealed class Idle:ICanonicalMutationGate {public bool IsQuiescent()=>true;}
    sealed class Factory:ICanonicalRigFactory
    {
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore c)
            =>throw new InvalidOperationException("QA raw-store never stages Unity world");
    }
    sealed class Port:ICanonicalCommitPort
    {
        public bool Read(long run,out long generation,out string hash)
        {generation=0;hash="";return false;}
        public bool Commit(long run,long before,string old,long after,string next)=>false;
    }
    sealed class LeaseOwner
    {
        public readonly CanonicalWorldSlotLease Lease;
        public readonly CanonicalWorldGateway Gateway;
        public LeaseOwner(CanonicalWorldSlotLease lease)
        {
            Lease=lease;
            Gateway=new CanonicalWorldGateway(new Idle(),new Factory(),new Port(),lease);
        }
        public bool Release()
            =>Gateway.TryCloseAfterPhysicalRetirement(()=>true);
    }
    static LeaseOwner? Owner(string root)
    {
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out var lease)||
            lease==null)return null;
        return new LeaseOwner(lease);
    }
    static int Child(string root)
    {
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out var lease)||
           lease==null){Console.WriteLine("CHILD_OS_OWNER_REFUSED");return 5;}
        bool ok=lease.TryCancelUnbound();
        Console.WriteLine(ok?"CHILD_OS_OWNER_RELEASED":"CHILD_OS_OWNER_BAD_RELEASE");
        return ok?0:9;
    }
    static bool Probe(string root,int expected)
    {
        var psi=new ProcessStartInfo("dotnet"){
            UseShellExecute=false,RedirectStandardOutput=true,
            RedirectStandardError=true};
        psi.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        psi.ArgumentList.Add("--child");psi.ArgumentList.Add(root);
        using var proc=Process.Start(psi);
        if(proc==null)return false;
        if(!proc.WaitForExit(15000)){
            try{proc.Kill(true);}catch{}
            return false;
        }
        child++;
        string stdout=proc.StandardOutput.ReadToEnd();
        string stderr=proc.StandardError.ReadToEnd();
        if(proc.ExitCode!=expected)
            Console.WriteLine("BAD_CHILD exit="+proc.ExitCode+
                " expected="+expected+" stdout="+stdout+" stderr="+stderr);
        return proc.ExitCode==expected &&
            (expected!=5||stdout.Contains("CHILD_OS_OWNER_REFUSED"))&&
            (expected!=0||stdout.Contains("CHILD_OS_OWNER_RELEASED"));
    }
    static bool CanonicalHealthyAndActualCrossProcess(string root)
    {
        var owner=Owner(root);if(owner==null)return false;
        CanonicalSingleAuthorityStoreV3R2? store=null;
        bool released=false;
        try
        {
            string canonical=Canon(root);
            var status=CanonicalSingleAuthorityStoreV3R2.TryCreateNew(
                canonical,Run,Slot,owner.Lease,out store);
            if(status!=SlotStatus.Accepted||store==null)return false;
            if(!Probe(root,5))return false;
            if(store.TryCommit(0,Bytes(1,true,false))!=SlotStatus.Accepted ||
               store.TryRead(out long gen,out byte[] snap)!=SlotStatus.Accepted ||
               gen!=1||!Verify(snap,1,true,false))return false;
            if(store.TryCommit(0,Bytes(1,false,true))!=SlotStatus.Stale)
                return false;
            if(store.TryCommit(1,Bytes(2,false,true))!=SlotStatus.Accepted ||
               store.TryRead(out gen,out snap)!=SlotStatus.Accepted ||
               gen!=2||!Verify(snap,2,false,true))return false;
            store.Dispose();
            if(CanonicalSingleAuthorityStoreV3R2.TryOpenExisting(
                canonical,Run,Slot,owner.Lease,out var duplicate)!=SlotStatus.Busy ||
               duplicate!=null)return false;
            if(!owner.Release())return false;
            released=true;
            if(!Probe(root,0))return false;
            var coldOwner=Owner(root);if(coldOwner==null)return false;
            try{
                if(CanonicalSingleAuthorityStoreV3R2.TryOpenExisting(
                    canonical,Run,Slot,coldOwner.Lease,out var cold)!=SlotStatus.Accepted||
                    cold==null)return false;
                try{
                    return cold.TryRead(out gen,out snap)==SlotStatus.Accepted&&
                        gen==2&&Verify(snap,2,false,true);
                }
                finally{cold.Dispose();}
            }
            finally{coldOwner.Release();}
        }
        finally{
            store?.Dispose();
            if(!released)owner.Release();
        }
    }
    static bool AbsoluteForeignPathRejectedWithoutIO(string root)
    {
        var owner=Owner(root);if(owner==null)return false;
        try
        {
            string sibling=Path.Combine(Path.GetDirectoryName(root)!,"FOREIGN");
            var status=CanonicalSingleAuthorityStoreV3R2.TryCreateNew(
                sibling,Run,Slot,owner.Lease,out var bad);
            return status==SlotStatus.Invalid&&bad==null&&
                   !Directory.Exists(sibling)&&!File.Exists(sibling);
        }
        finally{owner.Release();}
    }
    static bool DifferentCaseDifferentPhysicalSlotAfterTrueOwnerRetirement(string root)
    {
        var owner=Owner(root);if(owner==null)return false;
        string alias=CaseAlias(root), canonical=Canon(root);
        CanonicalSingleAuthorityStoreV3R2? alternate=null;
        try
        {
            var status=CanonicalSingleAuthorityStoreV3R2.TryCreateNew(
                alias,Run,Slot,owner.Lease,out alternate);
            if(status!=SlotStatus.Accepted||alternate==null)return false;
            if(alternate.TryCommit(0,Bytes(1,false,true))!=SlotStatus.Accepted)
                return false;
            if(!owner.Release())return false;
            alternate.Dispose();
            var next=Owner(root);if(next==null)return false;
            try
            {
                if(OperatingSystem.IsWindows())
                {
                    // Windows' ordinary NTFS path folding: this is SAME
                    // physical file so cannot create a second revision.
                    var b=CanonicalSingleAuthorityStoreV3R2.TryCreateNew(
                        canonical,Run,Slot,next.Lease,out var dup);
                    if(b!=SlotStatus.WrongOwner||dup!=null)return false;
                    if(CanonicalSingleAuthorityStoreV3R2.TryOpenExisting(
                        canonical,Run,Slot,next.Lease,out var existing)!=SlotStatus.Accepted||
                        existing==null)return false;
                    try{
                        return existing.TryRead(out long g,out byte[] data)==SlotStatus.Accepted &&
                               g==1&&Verify(data,1,false,true);
                    }
                    finally{existing.Dispose();}
                }
                // Linux ext4-like case-sensitive directory: original R2
                // OrdinalIgnoreCase owner path check INCORRECTLY accepts alias
                // and authorizes a completely different physical envelope.
                if(!Directory.Exists(alias)||Directory.Exists(canonical))
                    return false;
                var actual=CanonicalSingleAuthorityStoreV3R2.TryCreateNew(
                    canonical,Run,Slot,next.Lease,out var canonicalStore);
                if(actual!=SlotStatus.Accepted||canonicalStore==null)return false;
                try
                {
                    if(canonicalStore.TryCommit(0,Bytes(1,true,false))!=SlotStatus.Accepted)
                        return false;
                    string a=Path.Combine(alias,"save.envelope");
                    string b=Path.Combine(canonical,"save.envelope");
                    if(!File.Exists(a)||!File.Exists(b)||
                       Path.GetFullPath(a)==Path.GetFullPath(b)||
                       File.ReadAllBytes(a).SequenceEqual(File.ReadAllBytes(b)))
                        return false;
                    bool witness=alternate.TryRead(out _,out _)!=SlotStatus.Accepted;
                    // disposed store is closed; the old file still exists.
                    if(witness)Console.WriteLine(
                       "W25_EXACT_773_LINUX_CASE_ALIAS_P0_RED=TWO_DISTINCT_DURABLE_G1_LIGHT_HEAVY_ENVELOPES");
                    return witness;
                }
                finally{canonicalStore.Dispose();}
            }
            finally{next.Release();}
        }
        finally{alternate?.Dispose();owner.Release();}
    }
    public static int Main(string[] args)
    {
        if(args.Length==2&&args[0]=="--child")return Child(args[1]);
        if(args.Length!=0)return 9;
        Test("Genuine original OS single lease and valid frozen W06 durable g1-g2",
            CanonicalHealthyAndActualCrossProcess);
        Test("Raw foreign sibling path rejected before any disk create",
            AbsoluteForeignPathRejectedWithoutIO);
        Test("OS-specific case-folded public path R2 negative",
            DifferentCaseDifferentPhysicalSlotAfterTrueOwnerRetirement);
        Console.WriteLine("W25_773_B0161_ORIGINAL_REAL_SOURCE_CASES PASS="+pass+" FAIL="+fail);
        Console.WriteLine("W25_773_B0161_ACTUAL_CROSS_CLR_PROBES="+child);
        Console.WriteLine("W25_773_RAW_STORE_SECURITY_GATE="+
            (fail==0&&OperatingSystem.IsLinux()?
              "RELEASE_RED_CASE_SENSITIVE_ALIAS":"PENDING_OR_PLATFORM_UNAFFECTED"));
        Console.WriteLine("NATIVE_UNITY_PHYSX=NOT_RUN CANONICAL_PRIVATE_510_RIG=HOLD");
        return fail==0?0:1;
    }
}
