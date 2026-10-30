#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.SingleFileSaveSlot001;

internal static class IndependentV3
{
    private const long Run=61923;
    private static readonly Guid Id=Guid.Parse("00c11161-2023-4111-8111-000000000729");
    private static int passed,failed;
    private static void Check(string label,Func<bool> fn)
    {
        try{if(fn()){passed++;Console.WriteLine("PASS "+label);}
            else{failed++;Console.WriteLine("FAIL "+label);}}
        catch(Exception e){failed++;Console.WriteLine("FAIL "+label+" "+e);}
    }
    private static byte[] Data(long gen,bool light,bool heavy,long run=Run)
    {
        var parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            var s=SecureRigCatalog.At(i);
            bool det=i==0?light:heavy;
            long owner=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(s.PartId,owner,det?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                det?PartDockState.Detached:PartDockState.Attached,
                det,!det,new Vector3d(s.RootX+s.PartOffsetX,det?2.2:1.2,0),
                0,0,0,1,det?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),s.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,gen,parts));
    }
    private static bool Valid(byte[] bytes,long gen,bool light,bool heavy)
    {
        if(!ColdRestore.TryStage(bytes,out ColdRestore? cold)||cold==null||
           cold.Snapshot.RunId!=Run||cold.Snapshot.Generation!=gen)return false;
        for(int i=0;i<2;i++)
            if(cold.ContainsConsumedId(i,FrozenPartCatalog.Id(i))!=(i==0?light:heavy))return false;
        return true;
    }
    private sealed class Gate:ICanonicalMutationGate
    {
        public bool IsQuiescent()=>true;
    }
    private sealed class World:ICanonicalStagedWorld
    {
        readonly RunSnapshot snapshot;
        public bool Active,Retired;
        public World(RunSnapshot s){snapshot=s;}
        public RunSnapshot Capture()=>snapshot;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Retired=true;Active=false;}
    }
    private sealed class Factory:ICanonicalRigFactory
    {
        public World? Last,Partial;
        public bool FailPrepare,FailCleanup;
        public int Builds;
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore ledger)
        {
            if(ledger.Snapshot.RunId!=s.RunId||
                ledger.ContainsConsumedId(0,FrozenPartCatalog.Id(0))!=s.Parts[0].Consumed)
                throw new InvalidOperationException("frozen W06 original consumed mismatch");
            return new Owned(this,s);
        }
        private sealed class Owned:ICanonicalStageOwnership
        {
            readonly Factory f; readonly RunSnapshot s;
            public Owned(Factory f,RunSnapshot s){this.f=f;this.s=s;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                f.Builds++;
                if(f.FailPrepare)
                {
                    f.Partial=new World(s){Active=true};
                    throw new IOException("inject partially active original interface private world");
                }
                f.Last=new World(s);return f.Last;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                if(f.FailCleanup)return false;
                f.Partial?.Retire();
                return true;
            }
        }
        public bool RetireAll()
        {
            Last?.Retire();Partial?.Retire();
            return (Last==null||Last.Retired)&&(Partial==null||Partial.Retired);
        }
    }
    private static bool Scratch(Func<string,bool> body)
    {
        string root=Path.Combine(Path.GetTempPath(),
            "b0161-p17w23-v3r2-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{return body(root);}
        finally{Directory.Delete(root,true);}
    }
    private static string Slot(string root)=>Path.Combine(root,"owned-slot");
    private static bool CreatePhysicalOriginal669G1(string root)
    {
        if(SingleFileSaveSlot.TryCreateNew(Slot(root),Run,Id,out var s)!=SlotStatus.Accepted||
           s==null)return false;
        try{return s.TryCommit(0,Data(1,true,false))==SlotStatus.Accepted;}
        finally{s.Dispose();}
    }
    private sealed class Original669PhysicalReadPort:ICanonicalCommitPort
    {
        readonly string path;
        public Original669PhysicalReadPort(string root){path=Slot(root);}
        public bool Read(long run,out long gen,out string hash)
        {
            gen=0;hash="";
            if(SingleFileSaveSlot.TryOpenExisting(path,run,Id,out var s)!=SlotStatus.Accepted||
               s==null)return false;
            try
            {
                var result=s.TryRead(out gen,out byte[] raw);
                if(result==SlotStatus.Empty){gen=0;return true;}
                if(result!=SlotStatus.Accepted)return false;
                using var sha=System.Security.Cryptography.SHA256.Create();
                hash=BitConverter.ToString(sha.ComputeHash(raw)).Replace("-","").ToLowerInvariant();
                return true;
            }
            finally{s.Dispose();}
        }
        // Test only authorizes EXISTING original #669 g1 cold replay; no second writer.
        public bool Commit(long run,long prior,string priorHash,long next,string hash)=>false;
    }
    private static bool OpenV3(string root,Factory f,out CanonicalWorldGateway? v3)
    {
        v3=null;
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out var lease)||lease==null)
            return false;
        v3=new CanonicalWorldGateway(new Gate(),f,new Original669PhysicalReadPort(root),lease);
        return true;
    }
    private static bool CloseV3(CanonicalWorldGateway? v3,Factory f)=>
        v3!=null&&v3.TryCloseAfterPhysicalRetirement(()=>f.RetireAll());
    private static bool RealV3DuplicateLeaseDenied(string root)
    {
        var f=new Factory();
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out var unbound)||
            unbound==null||CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out _)||
            !unbound.TryCancelUnbound())return false;
        if(!OpenV3(root,f,out var g)||g==null)return false;
        try
        {
            if(!g.AuthorityActive||CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out _))
                return false;
            return CloseV3(g,f)&&
                CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out var last)&&
                last!=null&&last.TryCancelUnbound();
        }
        finally{CloseV3(g,f);}
    }
    private static bool WrongRunRejected(string root)
    {
        var f=new Factory();
        if(!OpenV3(root,f,out var g)||g==null)return false;
        try{return g.TryRestore(Data(1,true,false,Run+1),Run+1,1)==
                   CanonicalGateResult.WrongRun&&f.Builds==0&&CloseV3(g,f);}
        finally{CloseV3(g,f);}
    }
    private static bool FalseProofAndThrowHold(string root)
    {
        var f=new Factory();
        if(!OpenV3(root,f,out var g)||g==null)return false;
        try
        {
            if(g.TryCloseAfterPhysicalRetirement(()=>false)||!g.AuthorityActive||
               CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out _))return false;
            if(g.TryCloseAfterPhysicalRetirement(()=>throw new IOException("unproven"))||
               !g.AuthorityActive||CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out _))
               return false;
            return CloseV3(g,f);
        }
        finally{CloseV3(g,f);}
    }
    private static bool FailedPartialWorldKeepsOwner(string root)
    {
        if(!CreatePhysicalOriginal669G1(root))return false;
        var f=new Factory{FailPrepare=true,FailCleanup=true};
        if(!OpenV3(root,f,out var g)||g==null)return false;
        try
        {
            if(g.TryRestore(Data(1,true,false),Run,1)!=
                    CanonicalGateResult.RollbackQuarantine||
                !g.Quarantined||g.HasWorld||f.Partial==null||!f.Partial.Active||
                CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out _))return false;
            if(g.TryCloseAfterPhysicalRetirement(()=>false)||!g.AuthorityActive||
               CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out _))return false;
            f.Partial.Retire();
            return CloseV3(g,f);
        }
        finally{f.Partial?.Retire();CloseV3(g,f);}
    }
    private static bool Original669ColdAndFork(string root)
    {
        if(!CreatePhysicalOriginal669G1(root))return false;
        var state=SingleFileSaveSlot.TryOpenExisting(Slot(root),Run,Id,out var s);
        if(state!=SlotStatus.Accepted||s==null)return false;
        try
        {
            if(s.TryRead(out long g,out byte[] light)!=SlotStatus.Accepted||
               g!=1||!Valid(light,1,true,false))return false;
            if(s.TryCommit(1,Data(2,false,true))!=SlotStatus.Accepted||
               s.TryRead(out g,out byte[] heavy)!=SlotStatus.Accepted||
               g!=2||!Valid(heavy,2,false,true))return false;
            return s.TryCommit(1,Data(2,true,false))==SlotStatus.Stale;
        }
        finally{s.Dispose();}
    }
    private static bool V3AndRealR2ChildOverlap(string root)
    {
        if(!CreatePhysicalOriginal669G1(root))return false;
        var f=new Factory();
        if(!OpenV3(root,f,out var g)||g==null)return false;
        string ready=Path.Combine(root,"r2.ready");
        string command=Path.Combine(root,"r2.commit");
        string committed=Path.Combine(root,"r2.committed");
        string exit=Path.Combine(root,"r2.exit");
        string baseDir=Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string childDll=Path.GetFullPath(Path.Combine(baseDir,"..","..","..",
            "bin-child","Release","net8.0","ChildR2OriginalQA.dll"));
        var start=new ProcessStartInfo("dotnet")
            {UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        start.ArgumentList.Add(childDll);
        start.ArgumentList.Add(root);
        start.ArgumentList.Add(ready);
        start.ArgumentList.Add(command);
        start.ArgumentList.Add(committed);
        start.ArgumentList.Add(exit);
        Process? child=null;
        try
        {
            if(g.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
               f.Last==null||!f.Last.Active||!g.AuthorityActive||
               !g.TryCapturePreview(out byte[] original)||!Valid(original,1,true,false))
                return false;
            child=Process.Start(start);
            if(child==null)return false;
            if(!SpinWait.SpinUntil(()=>File.Exists(ready)||child.HasExited,16000)||
               !File.Exists(ready)||child.HasExited||
               File.ReadAllText(ready)!="R2_ORIGINAL_691_712_LIVE_G1_LIGHT"||
               !f.Last.Active||!g.AuthorityActive)return false;
            Console.WriteLine("W23_REAL_729_V3_AND_712_R2_TWO_LIVE_SAME_SLOT_WORLD_RED=REPRODUCED");
            Console.WriteLine("W23_REAL_OWNER_DOT_OWNER_AND_STORE_DOT_LEASE_SEPARATE_LOCKS_RED=REPRODUCED");
            File.WriteAllText(command,"commit-now");
            if(!SpinWait.SpinUntil(()=>File.Exists(committed)||child.HasExited,16000)||
               !File.Exists(committed)||child.HasExited||
               File.ReadAllText(committed)!="R2_ORIGINAL_669_PHYSICAL_GEN2_HEAVY"||
               !f.Last.Active||!g.AuthorityActive)return false;
            Console.WriteLine("W23_CHILD_R2_WRITES_669_GEN2_WHILE_V3_G1_LIGHT_WORLD_ACTIVE_RED=REPRODUCED");
            bool safe=!g.TryCapturePreview(out _)&&g.Quarantined;
            if(safe)Console.WriteLine("W23_V3_SOURCE_STALE_PREVIEW_FAIL_CLOSED_AFTER_COMPETING_WRITE_GREEN=PASS");
            return safe;
        }
        finally
        {
            File.WriteAllText(exit,"exit");
            if(child!=null)
            {
                if(!child.WaitForExit(16000)){try{child.Kill(true);}catch{}}
                Console.WriteLine("W23_CHILD_ACTUAL_CLR_EXIT="+child.ExitCode);
                child.Dispose();
            }
            CloseV3(g,f);
        }
    }
    public static int Main(string[] args)
    {
        if(args.Length>1||(args.Length==1&&args[0]!="--release"))return 4;
        for(int mask=0;mask<4;mask++)
        {
            int m=mask;
            Check("ORIGINAL_556_W06_STABLE_IDS_MASK_"+m,()=>
                Valid(Data(1,(m&1)!=0,(m&2)!=0),1,(m&1)!=0,(m&2)!=0));
        }
        Check("GREEN_SELECTED_729_V3_SAME_OWNER_OS_LEASE_DENIED",()=>Scratch(RealV3DuplicateLeaseDenied));
        Check("GREEN_SELECTED_729_V3_WRONG_RUN_PRESTAGE",()=>Scratch(WrongRunRejected));
        Check("GREEN_SELECTED_729_V3_BAD_PARTIAL_OWNER_ESCROW",()=>Scratch(FailedPartialWorldKeepsOwner));
        Check("GREEN_SELECTED_729_V3_FALSE_THROW_PROOF_HOLDS_OS_LEASE",()=>Scratch(FalseProofAndThrowHold));
        Check("GREEN_ORIGINAL_669_TRUE_DISK_G1_G2_ID_FORK_DENIED",()=>Scratch(Original669ColdAndFork));
        Check("RED_ACTUAL_ORIGINAL_729_AND_712_TWO_CLR_PROCESSES_TWO_LIVE_WORLDS",()=>Scratch(V3AndRealR2ChildOverlap));
        Console.WriteLine("B0161_W23_SEPARATE_DIRECT_ORIGINAL_729_V3_AND_712_R2_BINARIES");
        Console.WriteLine("TOTAL "+(passed+failed)+" PASS "+passed+" FAIL "+failed);
        Console.WriteLine("SOURCE_RELEASE_GATE="+(failed==0?
            "HOLD_729_OWNER_VS_712_LEASE_TWO_INDEPENDENT_AUTHORITIES":
            "UNVERIFIED_QA_HARNESS_FAILURE"));
        Console.WriteLine("REAL_NATIVE_UNITY_G1_NOT_RUN FROZEN_PRIVATE_510_WORLD_NOT_IMPLEMENTED COMBINED_G0_HOLD NO_MERGE");
        return failed!=0?1:(args.Length==1?3:0);
    }
}
