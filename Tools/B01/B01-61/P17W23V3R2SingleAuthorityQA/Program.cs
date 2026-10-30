#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using B01.CanonicalWorldApi001;
using B01.CanonicalSlotAdapter002;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.SingleFileSaveSlot001;

internal static class Independent
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
            if(cold.ContainsConsumedId(i,FrozenPartCatalog.Id(i)) !=
                (i==0?light:heavy))return false;
        return true;
    }
    private sealed class Gate:ICanonicalMutationGate
    {
        public Func<bool>? Callback;
        public bool IsQuiescent()=>Callback?.Invoke()??true;
    }
    private sealed class World:ICanonicalStagedWorld
    {
        private readonly RunSnapshot snapshot;
        public bool Active,Retired;
        public World(RunSnapshot s){snapshot=s;}
        public RunSnapshot Capture()=>snapshot;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Retired=true;Active=false;}
    }
    private sealed class Factory:ICanonicalRigFactory
    {
        public World? Last,Previous,Partial;
        public bool FailBeforeStage,FailCleanup;
        public int Allocations,Cleanups;
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot snapshot,ColdRestore cold)
        {
            if(cold.Snapshot.RunId!=snapshot.RunId||
               cold.ContainsConsumedId(0,FrozenPartCatalog.Id(0))!=snapshot.Parts[0].Consumed)
               throw new InvalidOperationException("W06 original ledger mismatch");
            return new Owned(this,snapshot);
        }
        private sealed class Owned:ICanonicalStageOwnership
        {
            private readonly Factory f;
            private readonly RunSnapshot s;
            public Owned(Factory f,RunSnapshot s){this.f=f;this.s=s;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                f.Allocations++;
                if(f.FailBeforeStage)
                {
                    f.Partial=new World(s){Active=true};
                    throw new IOException("injected partial original owner factory failure");
                }
                f.Previous=f.Last;
                f.Last=new World(s);
                return f.Last;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                f.Cleanups++;
                if(f.FailCleanup)return false;
                f.Partial?.Retire();f.Last?.Retire();
                return true;
            }
        }
        public bool RetireAll()
        {
            Last?.Retire();Previous?.Retire();Partial?.Retire();
            return (Last==null||Last.Retired)&&(Previous==null||Previous.Retired)&&
                   (Partial==null||Partial.Retired);
        }
    }
    private static bool Scratch(Func<string,bool> body)
    {
        string root=Path.Combine(Path.GetTempPath(),
            "b0161-p17w23-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{return body(root);}
        finally{Directory.Delete(root,true);}
    }
    private static string Slot(string root)=>Path.Combine(root,"slot");
    private static bool OpenR2(string root,bool create,Factory factory,out CanonicalSlotSessionR2? s)
    {
        var state=CanonicalSlotSessionR2.TryOpen(Slot(root),Run,Id,create,out s);
        return state==CanonicalSessionOpenStatus.Accepted&&s!=null&&s.Attach(new Gate(),factory);
    }
    private static bool CloseR2(CanonicalSlotSessionR2? s,Factory factory)
    {
        if(s==null)return false;
        bool retired=factory.RetireAll();
        bool closed=s.TryCloseAfterPhysicalRetirement(()=>retired);
        s.Dispose();
        return retired&&closed;
    }
    private sealed class OriginalShortPhysicalPort:ICanonicalCommitPort
    {
        private readonly string dir;
        public OriginalShortPhysicalPort(string root){dir=Slot(root);}
        public bool Read(long run,out long generation,out string hash)
        {
            generation=0;hash="";
            var st=SingleFileSaveSlot.TryOpenExisting(dir,run,Id,out var slot);
            if(st!=SlotStatus.Accepted||slot==null)return false;
            try
            {
                st=slot.TryRead(out generation,out byte[] bytes);
                if(st==SlotStatus.Empty){generation=0;return true;}
                if(st!=SlotStatus.Accepted)return false;
                hash=CanonicalSlotSessionR2.SourceHash(bytes);
                return true;
            }
            finally{slot.Dispose();}
        }
        // V3 only cold replays real original already committed g1 here:
        // never accepts an unauthorized second durable publisher.
        public bool Commit(long run,long prior,string oldHash,long next,string hash)=>false;
    }
    private static bool OpenV3(string root,Factory f,out CanonicalWorldGateway? gateway,
        out CanonicalWorldSlotLease? lease,ICanonicalCommitPort? port=null)
    {
        gateway=null;
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out lease)||lease==null)return false;
        gateway=new CanonicalWorldGateway(new Gate(),f,
            port??new OriginalShortPhysicalPort(root),lease);
        return true;
    }
    private static bool CloseV3(CanonicalWorldGateway? g,Factory f)
    {
        if(g==null)return false;
        return g.TryCloseAfterPhysicalRetirement(()=>f.RetireAll());
    }
    private static bool R2ColdGenerationAndIds(string root)
    {
        var f=new Factory();
        if(!OpenR2(root,true,f,out var r)||r==null)return false;
        try
        {
            if(r.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
               r.TryRestore(Data(2,false,true),Run,2)!=CanonicalGateResult.Accepted||
               !r.TryCapturePreview(out byte[] heavy)||!Valid(heavy,2,false,true)||
               !CloseR2(r,f))return false;
            var cold=new Factory();
            if(!OpenR2(root,false,cold,out var reopened)||reopened==null)return false;
            try
            {
                if(reopened.TryRestore(Data(2,true,false),Run,2)!=
                       CanonicalGateResult.StaleGeneration||cold.Allocations!=0||
                   reopened.TryRestore(Data(2,false,true),Run,2)!=CanonicalGateResult.Accepted||
                   !reopened.TryCapturePreview(out heavy)||!Valid(heavy,2,false,true))
                    return false;
                return CloseR2(reopened,cold);
            }
            finally{CloseR2(reopened,cold);}
        }
        finally{CloseR2(r,f);}
    }
    private static bool V3InternalOneOwner(string root)
    {
        var f=new Factory();
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out var first)||first==null)
            return false;
        if(CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out _)||
           !first.TryCancelUnbound())return false;
        if(!OpenV3(root,f,out var gateway,out var lease)||gateway==null||lease==null)
            return false;
        try
        {
            if(CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out _))return false;
            try
            {
                _=new CanonicalWorldGateway(new Gate(),new Factory(),
                   new OriginalShortPhysicalPort(root),lease);
                return false; // bind-once MUST throw.
            }
            catch(InvalidOperationException){}
            return CloseV3(gateway,f)&&
               CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out var token)&&
               token!=null&&token.TryCancelUnbound();
        }
        finally{CloseV3(gateway,f);}
    }
    private static bool V3WrongRunAndPrestage(string root)
    {
        var f=new Factory();
        if(!OpenV3(root,f,out var g,out _)||g==null)return false;
        try
        {
            bool denied=g.TryRestore(Data(1,true,false,Run+1),Run+1,1)==
                    CanonicalGateResult.WrongRun&&f.Allocations==0;
            return denied&&CloseV3(g,f);
        }
        finally{CloseV3(g,f);}
    }
    private static bool TwoRealSeparateLocksWithTwoActiveWorlds(string root)
    {
        var a=new Factory();
        if(!OpenR2(root,true,a,out var r2)||r2==null)return false;
        CanonicalWorldGateway? g=null;
        var b=new Factory();
        try
        {
            if(r2.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
               a.Last==null||!a.Last.Active)return false;
            if(!OpenV3(root,b,out g,out _,r2)||g==null)return false;
            if(g.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
               b.Last==null||!b.Last.Active||!g.AuthorityActive||
               !r2.OwnsSlot||!r2.HasWorld)return false;
            if(!g.TryCapturePreview(out byte[] v3)||!Valid(v3,1,true,false)||
               !r2.TryCapturePreview(out byte[] r2bytes)||!Valid(r2bytes,1,true,false))
                return false;
            Console.WriteLine("W23_SELECTED_729_V3_AND_712_R2_TWO_ACTIVE_WORLDS_IN_ONE_SLOT_RED=REPRODUCED");
            Console.WriteLine("W23_REAL_ORIGINAL_OWNER_DOT_OWNER_AND_STORE_DOT_LEASE_BOTH_HELD_RED=REPRODUCED");
            return true;
        }
        finally {CloseV3(g,b);CloseR2(r2,a);}
    }
    private static bool ParentV3WithOriginalG1(string root,Factory f,out CanonicalWorldGateway? v3)
    {
        v3=null;
        var init=new Factory();
        if(!OpenR2(root,true,init,out var s)||s==null)return false;
        try
        {
            if(s.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
                !CloseR2(s,init))return false;
        }
        finally {CloseR2(s,init);}
        return OpenV3(root,f,out v3,out _)&&v3!=null&&
            v3.TryRestore(Data(1,true,false),Run,1)==CanonicalGateResult.Accepted&&
            f.Last!=null&&f.Last.Active&&v3.AuthorityActive;
    }
    private static bool ChildR2PublishesWhileV3WorldLive(string root)
    {
        var f=new Factory();
        if(!ParentV3WithOriginalG1(root,f,out var v3)||v3==null)return false;
        string ready=Path.Combine(root,"child.ready");
        string done=Path.Combine(root,"child.done");
        var psi=new ProcessStartInfo("dotnet")
        {UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        psi.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        psi.ArgumentList.Add("--child-r2");psi.ArgumentList.Add(root);
        psi.ArgumentList.Add(ready);psi.ArgumentList.Add(done);
        using var proc=Process.Start(psi);
        if(proc==null){CloseV3(v3,f);return false;}
        try
        {
            if(!SpinWait.SpinUntil(()=>File.Exists(ready)||proc.HasExited,15000)||
               !File.Exists(ready)||proc.HasExited||
               File.ReadAllText(ready)!="CHILD_ORIGINAL_712_PHYSICAL_GEN2_HEAVY_WHILE_729_OWNER_LIVE"||
               f.Last==null||!f.Last.Active||!v3.AuthorityActive)return false;
            Console.WriteLine("W23_729_OWNER_LIVE_WHILE_CHILD_712_PHYSICALLY_COMMITS_GEN2_RED=REPRODUCED");
            bool stale=!v3.TryCapturePreview(out _)&&v3.Quarantined;
            if(stale)Console.WriteLine("W23_V3_STALE_PREVIEW_CHECKPOINT_FENCE_GREEN=PASS");
            return stale;
        }
        finally
        {
            File.WriteAllText(done,"done");
            if(!proc.WaitForExit(15000)){try{proc.Kill(true);}catch{}}
            Console.WriteLine("W23_CHILD_R2_CLR_EXIT="+proc.ExitCode);
            CloseV3(v3,f);
        }
    }
    private static int Child(string root,string ready,string done)
    {
        var f=new Factory();
        if(!OpenR2(root,false,f,out var r)||r==null)return 2;
        try
        {
            if(r.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
               r.TryRestore(Data(2,false,true),Run,2)!=CanonicalGateResult.Accepted||
               !r.TryCapturePreview(out byte[] bytes)||!Valid(bytes,2,false,true)||
               !r.Read(Run,out long gen,out _ )||gen!=2)return 3;
            File.WriteAllText(ready,"CHILD_ORIGINAL_712_PHYSICAL_GEN2_HEAVY_WHILE_729_OWNER_LIVE");
            return SpinWait.SpinUntil(()=>File.Exists(done),15000)?0:4;
        }
        finally{CloseR2(r,f);}
    }
    private static bool V3UnverifiedRetirementFailClosed(string root)
    {
        var f=new Factory();
        if(!OpenV3(root,f,out var g,out _ )||g==null)return false;
        if(g.TryCloseAfterPhysicalRetirement(()=>false)||
           !g.AuthorityActive||
           CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out _))return false;
        if(g.TryCloseAfterPhysicalRetirement(()=>throw new IOException("no proof"))||
           !g.AuthorityActive||
           CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out _))return false;
        return CloseV3(g,f);
    }
    private static bool V3FailedPartialOwnerHoldsLease(string root)
    {
        var init=new Factory();
        if(!OpenR2(root,true,init,out var s)||s==null)return false;
        try
        {
            if(s.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
               !CloseR2(s,init))return false;
        }
        finally{CloseR2(s,init);}
        var f=new Factory {FailBeforeStage=true,FailCleanup=true};
        if(!OpenV3(root,f,out var g,out _ )||g==null)return false;
        try
        {
            if(g.TryRestore(Data(1,true,false),Run,1)!=
                   CanonicalGateResult.RollbackQuarantine||
               !g.Quarantined||g.HasWorld||f.Partial==null||!f.Partial.Active||
               CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out _))return false;
            if(g.TryCloseAfterPhysicalRetirement(()=>false)||
               !g.AuthorityActive||
               CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out _))return false;
            f.Partial.Retire();
            return CloseV3(g,f);
        }
        finally{f.Partial?.Retire();CloseV3(g,f);}
    }
    public static int Main(string[] args)
    {
        if(args.Length==4&&args[0]=="--child-r2")
            return Child(args[1],args[2],args[3]);
        if(args.Length>1||(args.Length==1&&args[0]!="--release"))return 4;
        for(int mask=0;mask<4;mask++)
        {
            int m=mask;
            Check("ORIGINAL_556_W06_LIGHT_HEAVY_ID_MASK_"+m,()=>
                Valid(Data(1,(m&1)!=0,(m&2)!=0),1,(m&1)!=0,(m&2)!=0));
        }
        Check("GREEN_V3_ONE_OWNER_OS_LEASE_BIND_ONCE",()=>Scratch(V3InternalOneOwner));
        Check("GREEN_V3_WRONG_RUN_DENIED_BEFORE_STAGING",()=>Scratch(V3WrongRunAndPrestage));
        Check("GREEN_V3_FAILED_PARTIAL_STAGE_LEASE_RETAINED",()=>Scratch(V3FailedPartialOwnerHoldsLease));
        Check("GREEN_V3_FALSE_AND_THROW_RETIREMENT_PROOF_LOCKS_OS",()=>Scratch(V3UnverifiedRetirementFailClosed));
        Check("GREEN_R2_ORIGINAL_669_COLD_STABLE_IDS_SAME_GEN_FORK_DENIED",()=>Scratch(R2ColdGenerationAndIds));
        Check("RED_ORIGINAL_729_AND_712_SIMULTANEOUS_ACTIVE_SAME_SLOT",()=>Scratch(TwoRealSeparateLocksWithTwoActiveWorlds));
        Check("RED_REAL_CHILD_712_GEN2_COMMIT_WHILE_V3_GEN1_WORLD_LIVE",()=>Scratch(ChildR2PublishesWhileV3WorldLive));
        Console.WriteLine("B0161_W23_DIRECT_ORIGINAL_729_712_669_556_W06_TESTS");
        Console.WriteLine("TOTAL "+(passed+failed)+" PASS "+passed+" FAIL "+failed);
        Console.WriteLine("SOURCE_RELEASE_GATE="+(failed==0?
            "HOLD_TWO_INDEPENDENT_DOT_OWNER_DOT_LEASE_AUTHORITIES":
            "UNVERIFIED_TEST_HARNESS_FAILURE"));
        Console.WriteLine("NATIVE_UNITY_G1_NOT_RUN FROZEN_PRIVATE_510_RIG_LEDGER_NOT_WIRED G0_COMBINED_HOLD NO_MERGE");
        return failed!=0?1:(args.Length==1?3:0);
    }
}
