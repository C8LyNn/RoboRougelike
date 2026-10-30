#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using B01.CanonicalWorldApi001;
using B01.CanonicalSlotAdapter004;
using B01.CanonicalSingleAuthorityStore004;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

internal static class Independent773
{
    private const long Run=61925;
    private static readonly Guid Id=Guid.Parse("019a6192-5000-4000-8000-000000000773");
    private static int passed,failed;
    private static void Check(string name,Func<bool> run)
    {
        try{if(run()){++passed;Console.WriteLine("PASS "+name);}
            else{++failed;Console.WriteLine("FAIL "+name);}}
        catch(Exception ex){++failed;Console.WriteLine("FAIL "+name+" "+ex);}
    }
    private static bool Scratch(Func<string,bool> fn)
    {
        string parent=Path.Combine(Path.GetTempPath(),"b0161-w25-773-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(parent);
        string root=Path.Combine(parent,"trusted-root");
        Directory.CreateDirectory(root);
        try{return fn(root);}
        finally{Directory.Delete(parent,true);}
    }
    private static string Slot(string root,string prefix="run_") =>
        Path.Combine(root,"b0162_canonical_single_authority_v3",
             prefix+Run+"_slot_"+Id.ToString("N"));
    private static byte[] Data(long gen,bool light,bool heavy,long run=Run)
    {
        var p=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool det=i==0?light:heavy;
            var s=SecureRigCatalog.At(i);
            long owner=FrozenPartCatalog.Owner(i);
            p[i]=new PersistedPart(s.PartId,owner,det?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                det?PartDockState.Detached:PartDockState.Attached,det,!det,
                new Vector3d(s.RootX+s.PartOffsetX,det?2.2:1.2,0),
                0,0,0,1,new Vector3d(det?0.25:0,0,0),
                new Vector3d(0,0,0),s.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,gen,p));
    }
    private static bool Matches(byte[] b,long gen,bool light,bool heavy)
    {
        if(!ColdRestore.TryStage(b,out ColdRestore? c)||c==null||
           c.Snapshot.Generation!=gen||c.Snapshot.RunId!=Run)return false;
        return c.ContainsConsumedId(0,FrozenPartCatalog.Id(0))==light&&
               c.ContainsConsumedId(1,FrozenPartCatalog.Id(1))==heavy;
    }
    private sealed class Idle:ICanonicalMutationGate
    {public bool IsQuiescent()=>true;}
    private sealed class FakePort:ICanonicalCommitPort
    {
        public bool Read(long run,out long gen,out string hash)
        {gen=0;hash="";return false;}
        public bool Commit(long run,long prev,string old,long next,string hash)=>false;
    }
    private sealed class World:ICanonicalStagedWorld
    {
        private readonly RunSnapshot snap;
        public bool Active,Retired;
        public World(RunSnapshot value){snap=value;}
        public RunSnapshot Capture()=>snap;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Active=false;Retired=true;}
    }
    private sealed class Factory:ICanonicalRigFactory
    {
        public World? Live,Prior,Orphan;
        public bool FailPartial,CleanupFalse,CleanupThrow;
        public int Builds;
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore c)
        {
            if(c.Snapshot.RunId!=s.RunId||
               c.ContainsConsumedId(0,FrozenPartCatalog.Id(0))!=s.Parts[0].Consumed)
                throw new InvalidOperationException("Frozen W06 ID proof failed");
            return new Stage(this,s);
        }
        private sealed class Stage:ICanonicalStageOwnership
        {
            private readonly Factory f;private readonly RunSnapshot s;
            public Stage(Factory f,RunSnapshot s){this.f=f;this.s=s;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                ++f.Builds;
                if(f.FailPartial)
                {
                    f.Orphan=new World(s){Active=true};
                    throw new IOException("test-owned partial root created before throwing");
                }
                f.Prior=f.Live;return f.Live=new World(s);
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                if(f.CleanupThrow)throw new IOException("cleanup proof missing");
                if(f.CleanupFalse)return false;
                f.Orphan?.Retire();
                return true;
            }
        }
        public bool RetireAll()
        {
            Live?.Retire();Prior?.Retire();Orphan?.Retire();
            return (Live==null||Live.Retired)&&(Prior==null||Prior.Retired)&&
                   (Orphan==null||Orphan.Retired);
        }
    }
    private sealed class AckLoss:IAtomicEnvelopePublisher
    {
        public int Calls;
        public void Publish(string source,string target)
        {
            ++Calls;
            File.Move(source,target,true);
            throw new IOException("lost ACK after real physical File.Move");
        }
    }
    private static bool Open(string root,bool create,Factory f,out CanonicalSlotSessionV3R2? session,
         IAtomicEnvelopePublisher? writer=null)
    {
        var status=CanonicalSlotSessionV3R2.TryOpen(root,Run,Id,create,out session,writer);
        return status==CanonicalSessionOpenStatus.Accepted&&session!=null&&
               session.Attach(new Idle(),f);
    }
    private static bool Close(CanonicalSlotSessionV3R2? s,Factory f)
    {
        if(s==null)return false;
        bool gone=f.RetireAll();
        bool ended=s.TryCloseAfterPhysicalRetirement(()=>gone);
        s.Dispose();return gone&&ended;
    }
    private static bool Probe(string root,bool expectOpen)
    {
        var psi=new ProcessStartInfo("dotnet")
        {UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        psi.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        psi.ArgumentList.Add("--child");psi.ArgumentList.Add(root);
        using var p=Process.Start(psi);
        if(p==null)return false;
        string stdout=p.StandardOutput.ReadToEnd();
        _=p.StandardError.ReadToEnd();
        if(!p.WaitForExit(16000)){try{p.Kill(true);}catch{}return false;}
        return p.ExitCode==(expectOpen?0:3)&&
               stdout.Contains(expectOpen?"W25_CHILD_OWNER_ACQUIRED":"W25_CHILD_OWNER_BUSY");
    }
    private static int Child(string root)
    {
        var st=CanonicalSlotSessionV3R2.TryOpen(root,Run,Id,false,out var s);
        if(st==CanonicalSessionOpenStatus.Busy)
        {Console.WriteLine("W25_CHILD_OWNER_BUSY");return 3;}
        if(st==CanonicalSessionOpenStatus.Accepted&&s!=null)
        {s.TryCloseAfterPhysicalRetirement(()=>true);s.Dispose();
         Console.WriteLine("W25_CHILD_OWNER_ACQUIRED");return 0;}
        Console.WriteLine("W25_CHILD_UNEXPECTED_"+st);return 5;
    }
    private static bool GenuineCanonicalAndSameOwnerDuplicate(string root)
    {
        var f=new Factory();
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out var authority)||
           authority==null)return false;
        var gateway=new CanonicalWorldGateway(new Idle(),f,new FakePort(),authority);
        try
        {
            if(!Probe(root,false))return false;
            string foreign=Path.Combine(Path.GetDirectoryName(root)!,"foreign-sibling-"+Guid.NewGuid().ToString("N"));
            if(CanonicalSingleAuthorityStoreV3R2.TryCreateNew(foreign,Run,Id,authority,
                    out var wrong)!=SlotStatus.Invalid||wrong!=null||Directory.Exists(foreign))
                return false;
            string path=Slot(root);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if(CanonicalSingleAuthorityStoreV3R2.TryCreateNew(path,Run,Id,authority,
                out var stored)!=SlotStatus.Accepted||stored==null)return false;
            try
            {
                if(CanonicalSingleAuthorityStoreV3R2.TryOpenExisting(path,Run,Id,authority,
                   out var doubleOpen)!=SlotStatus.Busy||doubleOpen!=null)return false;
                if(stored.TryCommit(0,Data(1,true,false))!=SlotStatus.Accepted||
                   stored.TryRead(out long gen,out byte[] b)!=SlotStatus.Accepted||
                   gen!=1||!Matches(b,1,true,false))return false;
                stored.Dispose();
                if(CanonicalSingleAuthorityStoreV3R2.TryOpenExisting(path,Run,Id,authority,
                    out doubleOpen)!=SlotStatus.Busy||doubleOpen!=null||!Probe(root,false))
                    return false;
            }
            finally{stored.Dispose();}
            return gateway.TryCloseAfterPhysicalRetirement(f.RetireAll)&&Probe(root,true);
        }
        finally{gateway.TryCloseAfterPhysicalRetirement(f.RetireAll);}
    }
    // Explicit original P0 #767 rerun: #773 compares physical paths with
    // OrdinalIgnoreCase despite Linux case-sensitive disk name semantics.
    // A second legitimate lifetime OS owner can publish forked g1 in RUN_...
    // and leave canonical run_... original g1 untouched.
    private static bool IndependentCaseFoldFork(string root)
    {
        string exact=Slot(root);
        string alias=Slot(root,"RUN_");
        Directory.CreateDirectory(Path.GetDirectoryName(exact)!);
        bool caseSensitive;
        using(var ownerA=Acquire(root))
        {
            var gate=new CanonicalWorldGateway(new Idle(),new Factory(),new FakePort(),ownerA.Lease);
            try
            {
                if(CanonicalSingleAuthorityStoreV3R2.TryCreateNew(exact,Run,Id,ownerA.Lease,
                    out var s)!=SlotStatus.Accepted||s==null)return false;
                try
                {
                    if(s.TryCommit(0,Data(1,true,false))!=SlotStatus.Accepted)return false;
                }
                finally{s.Dispose();}
            }
            finally
            {if(!gate.TryCloseAfterPhysicalRetirement(()=>true))return false;}
        }
        caseSensitive=!Directory.Exists(alias);
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out var ownerB)||
           ownerB==null)return false;
        var second=new CanonicalWorldGateway(new Idle(),new Factory(),new FakePort(),ownerB);
        try
        {
            var outcome=CanonicalSingleAuthorityStoreV3R2.TryCreateNew(
                alias,Run,Id,ownerB,out var storeB);
            if(!caseSensitive)
            {
                if(outcome!=SlotStatus.WrongOwner||storeB!=null)return false;
                Console.WriteLine("W25_773_WINDOWS_CASE_ALIAS_SAME_PHYSICAL_DIR_DENIED=PASS");
                return true;
            }
            if(outcome!=SlotStatus.Accepted||storeB==null)return false;
            try
            {
                if(storeB.TryCommit(0,Data(1,false,true))!=SlotStatus.Accepted||
                   storeB.TryRead(out long variantGen,out byte[] variant)!=SlotStatus.Accepted||
                   variantGen!=1||!Matches(variant,1,false,true))return false;
            }
            finally{storeB.Dispose();}
            if(!File.Exists(Path.Combine(exact,"save.envelope"))||
               !File.Exists(Path.Combine(alias,"save.envelope"))||
               Path.GetFullPath(exact)==Path.GetFullPath(alias))return false;
            Console.WriteLine("W25_773_LINUX_NONCANONICAL_CASE_PATH_TWO_REAL_SAVE_FILES=REPRODUCED");
            Console.WriteLine("W25_773_LINUX_SAME_RUN_GUID_GEN1_CONFLICTING_LIGHT_HEAVY_REPRODUCED");
            return true;
        }
        finally{second.TryCloseAfterPhysicalRetirement(()=>true);}
    }
    private sealed class LeaseOwner:IDisposable
    {
        public readonly CanonicalWorldSlotLease Lease;
        public LeaseOwner(CanonicalWorldSlotLease x){Lease=x;}
        public void Dispose(){} // binding, verified source gateway owns physical release.
    }
    private static LeaseOwner Acquire(string root)
    {
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out var l)||l==null)
            throw new InvalidOperationException("No original physical .owner acquired");
        return new LeaseOwner(l);
    }
    private static bool ColdGenerationAndFork(string root)
    {
        var f=new Factory();
        if(!Open(root,true,f,out var s)||s==null)return false;
        try
        {
            if(s.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
               s.TryRestore(Data(2,false,true),Run,2)!=CanonicalGateResult.Accepted||
               !s.TryCapturePreview(out byte[] preview)||!Matches(preview,2,false,true)||
               !Probe(root,false)||!Close(s,f)||!Probe(root,true))return false;
            var cold=new Factory();
            if(!Open(root,false,cold,out var reopened)||reopened==null)return false;
            try
            {
                if(reopened.TryRestore(Data(2,true,false),Run,2)!=
                    CanonicalGateResult.StaleGeneration||cold.Builds!=0||
                   reopened.TryRestore(Data(2,false,true),Run,2)!=
                    CanonicalGateResult.Accepted||
                   !reopened.TryCapturePreview(out preview)||!Matches(preview,2,false,true))
                    return false;
                return Close(reopened,cold);
            }
            finally{Close(reopened,cold);}
        }
        finally{Close(s,f);}
    }
    private static bool UnretiredPartialWorld(string root)
    {
        var f=new Factory{FailPartial=true,CleanupFalse=true};
        if(!Open(root,true,f,out var s)||s==null)return false;
        try
        {
            if(s.TryRestore(Data(1,true,false),Run,1)!=
                   CanonicalGateResult.RollbackQuarantine||
               !s.Quarantined||s.HasWorld||f.Orphan==null||
               !f.Orphan.Active||!Probe(root,false))return false;
            s.Dispose();
            if(!s.OwnsSlot||
                s.TryCloseAfterPhysicalRetirement(()=>false)||!Probe(root,false))
                return false;
            f.Orphan.Retire();
            return s.TryCloseAfterPhysicalRetirement(f.RetireAll)&&Probe(root,true);
        }
        finally{f.RetireAll();Close(s,f);}
    }
    private static bool LostAckCommitAndCold(string root)
    {
        var f=new Factory();
        var writer=new AckLoss();
        if(!Open(root,true,f,out var s,writer)||s==null)return false;
        try
        {
            if(s.TryRestore(Data(1,true,false),Run,1)!=
               CanonicalGateResult.RollbackQuarantine||
               !s.Quarantined||!Probe(root,false)||writer.Calls!=1||
               !File.Exists(Path.Combine(Slot(root),"save.envelope")))return false;
            if(!Close(s,f))return false;
            var cold=new Factory();
            if(!Open(root,false,cold,out var r)||r==null)return false;
            try
            {
                if(r.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
                   !r.TryCapturePreview(out byte[] snapshot)||!Matches(snapshot,1,true,false))
                    return false;
                return Close(r,cold);
            }
            finally{Close(r,cold);}
        }
        finally{Close(s,f);}
    }
    public static int Main(string[] args)
    {
        if(args.Length==2&&args[0]=="--child")return Child(args[1]);
        if(args.Length>0)return 4;
        for(int mask=0;mask<4;mask++)
        {
            int m=mask;
            Check("FROZEN_556_W06_ID_MASK_"+mask,()=>
                Matches(Data(1,(m&1)!=0,(m&2)!=0),1,(m&1)!=0,(m&2)!=0));
        }
        Check("GREEN_773_ONE_CANONICAL_EXACT_STORE_AND_SECOND_CLR_EXCLUSION",
              ()=>Scratch(GenuineCanonicalAndSameOwnerDuplicate));
        Check("GREEN_773_COLD_GEN2_EXACT_LEDGER_AND_SAME_GEN_FORK_REJECTED",
              ()=>Scratch(ColdGenerationAndFork));
        Check("GREEN_773_PARTIAL_ROOT_DISPOSE_OS_LOCK_RETAINED",
              ()=>Scratch(UnretiredPartialWorld));
        Check("GREEN_773_LOST_ACK_AFTER_REAL_DISK_RENAME_COLD_RESTART",
              ()=>Scratch(LostAckCommitAndCold));
        Check("W25_773_LINUX_CASEFOLD_PHYSICAL_SAVE_FORK_NEGATIVE_WITNESS",
              ()=>Scratch(IndependentCaseFoldFork));
        Console.WriteLine("TOTAL "+(passed+failed)+" PASS "+passed+" FAIL "+failed);
        Console.WriteLine("QA_WITNESS_773_SOURCE_GATE=HOLD_IF_CASEFOLD_REPRODUCED_ON_POSIX");
        Console.WriteLine("SOURCE_G0_FIX773_HOLD_NATIVE_G1_NOT_RUN_PRIVATE_510_GAME_WIRING_HOLD_G2_HOLD");
        return failed==0?0:1;
    }
}
