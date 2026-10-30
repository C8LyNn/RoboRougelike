#nullable enable
using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using B01.CanonicalWorldApi001;
using B01.SingleFileSaveSlot001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

// Original #729 V3 gateway+lease, original #669 actual slot and frozen #556.
// Only future #510 physical rigs and external COMMIT PORT mapping are test doubles.
// This intentionally tests two INDEPENDENT real OS files: .owner vs slot.lease.
internal static class Program
{
    const long Run=61231;
    static readonly Guid Slot=Guid.Parse("61231000-6123-4123-8123-000000006123");
    static int passed,failed,children;
    static void Check(string name,Func<string,bool> check)
    {
        var root=Path.Combine(Path.GetTempPath(),"B0161W23-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            bool ok=check(Path.Combine(root,"private-slot"));
            Console.WriteLine((ok?"PASS ":"FAIL ")+name);
            if(ok)passed++;else failed++;
        }
        catch(Exception e){failed++;Console.WriteLine("FAIL "+name+" "+e);}
        finally{try{Directory.Delete(root,true);}catch(IOException){}
                catch(UnauthorizedAccessException){}}
    }
    static byte[] Data(long gen,bool light,bool heavy,long run=Run)
    {
        var a=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            var spec=SecureRigCatalog.At(i);
            bool det=i==0?light:heavy;
            long own=FrozenPartCatalog.Owner(i);
            a[i]=new PersistedPart(spec.PartId,own,det?0:own,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                det?PartDockState.Detached:PartDockState.Attached,
                det,!det,new Vector3d(spec.RootX+spec.PartOffsetX,det?2.2:1.2,0),
                0,0,0,1,det?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),spec.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,gen,a));
    }
    static bool Ids(byte[] bytes,long gen,bool light,bool heavy)
    {
        return ColdRestore.TryStage(bytes,out ColdRestore? c)&&c!=null&&
            c.Snapshot.RunId==Run&&c.Snapshot.Generation==gen&&
            c.Snapshot.Parts[0].Id==2310001&&c.Snapshot.Parts[1].Id==2310002&&
            c.ContainsConsumedId(0,2310001)==light&&
            c.ContainsConsumedId(1,2310002)==heavy;
    }
    static string Hash(byte[] a)=>Convert.ToHexString(SHA256.HashData(a)).ToLowerInvariant();
    sealed class Gate:ICanonicalMutationGate
    {
        public bool IsQuiescent()=>true;
    }
    sealed class World:ICanonicalStagedWorld
    {
        public readonly RunSnapshot Snapshot;
        public bool Active,Retired;
        public World(RunSnapshot x){Snapshot=x;}
        public RunSnapshot Capture()=>Snapshot;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Active=false;Retired=true;}
    }
    sealed class Factory:ICanonicalRigFactory
    {
        public World? Last,Old,Partial;
        public bool FailPartial,FailCleanup;
        public int Cleanups;
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot x,ColdRestore cold)
        {
            if(cold.ContainsConsumedId(0,2310001)!=x.Parts[0].Consumed)
                throw new IOException("W06 identity diverged");
            return new Owned(this,x);
        }
        sealed class Owned:ICanonicalStageOwnership
        {
            readonly Factory f; readonly RunSnapshot x;
            public Owned(Factory f,RunSnapshot x){this.f=f;this.x=x;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                if(f.FailPartial)
                {
                    f.Partial=new World(x){Active=true};
                    throw new IOException("partial private world allocation");
                }
                f.Old=f.Last;f.Last=new World(x);return f.Last;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                f.Cleanups++;
                if(f.FailCleanup)return false;
                f.Partial?.Retire();
                return true;
            }
        }
    }
    // TEST-OWNED bridge deliberately demonstrates a bad integration:
    // ORIGINAL #669 lease is acquired only for Read/Commit, while ORIGINAL
    // V3 #729 lifetime lease remains held on DIFFERENT .owner file.
    sealed class ShortStorePort:ICanonicalCommitPort
    {
        readonly string path;
        public bool LoseAck;
        public ShortStorePort(string p){path=p;}
        public bool Read(long run,out long epoch,out string digest)
        {
            epoch=0;digest="";
            if(run!=Run)return false;
            if(SingleFileSaveSlot.TryOpenExisting(path,Run,Slot,out var s)!=SlotStatus.Accepted||s==null)
                return false;
            using(s)
            {
                SlotStatus st=s.TryRead(out long g,out byte[] b);
                if(st==SlotStatus.Empty)return true;
                if(st!=SlotStatus.Accepted)return false;
                epoch=g;digest=Hash(b);return true;
            }
        }
        public bool Commit(long run,long prior,string before,long next,string newHash)
        {
            if(run!=Run||next!=prior+1)return false;
            if(SingleFileSaveSlot.TryOpenExisting(path,Run,Slot,out var s)!=SlotStatus.Accepted||s==null)
                return false;
            using(s)
            {
                SlotStatus current=s.TryRead(out long g,out byte[] b);
                if(current!=SlotStatus.Accepted&&current!=SlotStatus.Empty)return false;
                if(g!=prior||Hash(b)!=before&&current!=SlotStatus.Empty)return false;
                if(current==SlotStatus.Empty&&before!="")return false;
                var bytes=Data(next,next==1,next!=1);
                if(Hash(bytes)!=newHash)return false;
                if(s.TryCommit(prior,bytes)!=SlotStatus.Accepted)return false;
                if(LoseAck)throw new IOException("durable OS publish succeeded but ACK lost");
                return true;
            }
        }
    }
    static bool Seed(string p)
    {
        SlotStatus s=SingleFileSaveSlot.TryCreateNew(p,Run,Slot,out var created);
        if(s!=SlotStatus.Accepted||created==null)return false;
        created.Dispose();return true;
    }
    static bool OpenGateway(string p,Factory factory,Gate gate,
        out CanonicalWorldGateway? gw,out CanonicalWorldSlotLease? owner,
        out ShortStorePort? port)
    {
        gw=null;owner=null;port=null;
        if(!CanonicalWorldSlotLease.TryAcquire(p,Run,Slot,out owner)||owner==null)
            return false;
        port=new ShortStorePort(p);
        gw=new CanonicalWorldGateway(gate,factory,port,owner);
        return gw.AuthorityActive;
    }
    static int Child(string mode,string p)
    {
        if(mode=="--owner")
        {
            if(!CanonicalWorldSlotLease.TryAcquire(p,Run,Slot,out var l)&&l==null)
            {Console.WriteLine("CHILD_V3_OWNER_OS_DENIED");return 5;}
            l?.TryCancelUnbound();return 6;
        }
        if(mode=="--store")
        {
            var status=SingleFileSaveSlot.TryOpenExisting(p,Run,Slot,out var store);
            if(status!=SlotStatus.Accepted||store==null)return 7;
            using(store)
            {
                if(store.TryCommit(1,Data(2,false,true))!=SlotStatus.Accepted)return 8;
                if(store.TryRead(out long g,out byte[] b)!=SlotStatus.Accepted||
                   g!=2||!Ids(b,2,false,true))return 9;
                Console.WriteLine("CHILD_669_REAL_LEASE_PUBLISHED_HEAVY_WHILE_V3_OWNER_ACTIVE");
                return 0;
            }
        }
        return 10;
    }
    static bool ChildExit(string mode,string dir,int want)
    {
        var ps=new ProcessStartInfo("dotnet")
            {UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        ps.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        ps.ArgumentList.Add(mode);ps.ArgumentList.Add(dir);
        using var c=Process.Start(ps);
        if(c==null)return false;
        if(!c.WaitForExit(15000))
        {try{c.Kill(true);}catch{} return false;}
        string stdout=c.StandardOutput.ReadToEnd(),stderr=c.StandardError.ReadToEnd();
        children++;
        if(c.ExitCode!=want)
            Console.WriteLine("CHILD_WRONG_RETURN expected="+want+" actual="+c.ExitCode+
                " stdout="+stdout+" stderr="+stderr);
        return c.ExitCode==want&&
          (mode!="--owner"||stdout.Contains("CHILD_V3_OWNER_OS_DENIED"))&&
          (mode!="--store"||stdout.Contains("CHILD_669_REAL_LEASE_PUBLISHED_HEAVY_WHILE_V3_OWNER_ACTIVE"));
    }
    static bool SingleOwnerGuard(string p)
    {
        if(!Seed(p))return false;
        var f=new Factory();var gate=new Gate();
        if(!OpenGateway(p,f,gate,out var a,out _,out _)||a==null)return false;
        bool denied=!CanonicalWorldSlotLease.TryAcquire(p,Run,Slot,out var second)&&second==null;
        bool cross=ChildExit("--owner",p,5);
        if(!a.TryCloseAfterPhysicalRetirement(()=>true))return false;
        bool next=CanonicalWorldSlotLease.TryAcquire(p,Run,Slot,out var fresh)&&fresh!=null&&
                  fresh.TryCancelUnbound();
        return denied&&cross&&next;
    }
    static bool SameRunLeaseFileBypass(string p)
    {
        if(!Seed(p))return false;
        var f=new Factory();var gate=new Gate();
        if(!OpenGateway(p,f,gate,out var a,out _,out _)||a==null)return false;
        if(a.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
           f.Last==null||!f.Last.Active||!a.HasWorld||!a.AuthorityActive||
           !ChildExit("--owner",p,5))return false;
        // Real second CLR bypasses unchanged #729 .owner by using unchanged
        // #669 .lease (separate path) and physically publishes Heavy gen2.
        bool bypass=ChildExit("--store",p,0);
        bool oldStillActive=f.Last.Active&&a.HasWorld&&a.AuthorityActive;
        bool staleDenied=!a.TryCapturePreview(out _)&&a.Quarantined;
        Console.WriteLine("W23_729_669_TWO_DISTINCT_OS_LOCK_FILES_EXTERNAL_WRITER_RED="+
            (bypass&&oldStillActive?"REPRODUCED":"NOT_REPRODUCED"));
        Console.WriteLine("W23_729_STALE_LIGHT_PREVIEW_AFTER_EXTERNAL_HEAVY_COMMIT_GREEN="+
            (staleDenied?"DENIED_AND_QUARANTINED":"FAILED"));
        bool falseProofDenied=!a.TryCloseAfterPhysicalRetirement(()=>false);
        bool stillHeld=a.AuthorityActive&&ChildExit("--owner",p,5);
        f.Last.Retire();
        bool safe=a.TryCloseAfterPhysicalRetirement(()=>f.Last.Retired&&!f.Last.Active);
        return bypass&&oldStillActive&&staleDenied&&falseProofDenied&&stillHeld&&safe;
    }
    static bool ColdOriginalAndFork(string p)
    {
        if(!Seed(p))return false;
        var f=new Factory();
        if(!OpenGateway(p,f,new Gate(),out var a,out _,out _)||a==null)return false;
        if(a.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
           f.Last==null)return false;
        f.Last.Retire();
        if(!a.TryCloseAfterPhysicalRetirement(()=>true))return false;
        var f2=new Factory();
        if(!OpenGateway(p,f2,new Gate(),out var b,out _,out _)||b==null)return false;
        bool replay=b.TryRestore(Data(1,true,false),Run,1)==CanonicalGateResult.Accepted;
        bool fork=b.TryRestore(Data(1,false,true),Run,1)==CanonicalGateResult.StaleGeneration;
        bool run=b.TryRestore(Data(2,false,true,Run+1),Run+1,2)==CanonicalGateResult.WrongRun;
        bool stable=b.TryCapturePreview(out byte[] bytes)&&Ids(bytes,1,true,false);
        f2.Last?.Retire();
        return replay&&fork&&run&&stable&&b.TryCloseAfterPhysicalRetirement(()=>true);
    }
    static bool PartialWorldQuarantine(string p)
    {
        if(!Seed(p))return false;
        var f=new Factory(){FailPartial=true,FailCleanup=true};
        if(!OpenGateway(p,f,new Gate(),out var a,out _,out _)||a==null)return false;
        bool red=a.TryRestore(Data(1,true,false),Run,1)==CanonicalGateResult.RollbackQuarantine&&
                 a.Quarantined&&!a.HasWorld&&f.Partial!=null&&f.Partial.Active;
        bool locked=a.AuthorityActive&&ChildExit("--owner",p,5);
        bool cannotClose=!a.TryCloseAfterPhysicalRetirement(()=>false)&&
            !a.TryCloseAfterPhysicalRetirement(()=>
                throw new IOException("unproven cleanup"))&&
            a.AuthorityActive&&ChildExit("--owner",p,5);
        f.Partial?.Retire();
        bool canClose=a.TryCloseAfterPhysicalRetirement(()=>f.Partial!=null&&
                      f.Partial.Retired&&!f.Partial.Active);
        return red&&locked&&cannotClose&&canClose;
    }
    static bool LostAckSticky(string p)
    {
        if(!Seed(p))return false;
        var f=new Factory();
        if(!OpenGateway(p,f,new Gate(),out var a,out _,out var port)||a==null||port==null)
            return false;
        if(a.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.Accepted)
            return false;
        port.LoseAck=true;
        bool sealedOwner=a.TryRestore(Data(2,false,true),Run,2)
              ==CanonicalGateResult.RollbackQuarantine&&a.Quarantined&&a.AuthorityActive;
        var d=new ShortStorePort(p);
        bool disk=d.Read(Run,out long gen,out string hash)&&gen==2&&
                  hash==Hash(Data(2,false,true));
        bool neverExport=!a.TryCapturePreview(out _);
        f.Last?.Retire();
        bool retired=a.TryCloseAfterPhysicalRetirement(()=>
            f.Last==null||f.Last.Retired&&!f.Last.Active);
        return sealedOwner&&disk&&neverExport&&retired;
    }
    static bool StableFrozen()
    {
        for(int mask=0;mask<4;mask++)
            if(!Ids(Data(1,(mask&1)!=0,(mask&2)!=0),1,
                   (mask&1)!=0,(mask&2)!=0))return false;
        return true;
    }
    public static int Main(string[] args)
    {
        if(args.Length==2&&args[0].StartsWith("--",StringComparison.Ordinal))
            return Child(args[0],args[1]);
        if(args.Length!=0)return 11;
        Check("EXACT_ORIGINAL_556_W06_ALL_4_STABLE_ID_MASKS",_=>StableFrozen());
        Check("EXACT_729_V3_SAME_OS_OWNER_ONE_GATEWAY_CHILD_DENIAL",SingleOwnerGuard);
        Check("EXACT_729_V3_ACTUAL_669_CROSSPROCESS_SEPARATE_LEASE_BYPASS_WITNESS",SameRunLeaseFileBypass);
        Check("EXACT_729_V3_REAL_669_COLD_RESTART_AND_SAME_GEN_FORK",ColdOriginalAndFork);
        Check("EXACT_729_V3_PARTIAL_WORLD_FALSE_CLEANUP_RETAINS_OWNER",PartialWorldQuarantine);
        Check("EXACT_729_V3_REAL_669_COMMIT_AFTER_PUBLISH_ACK_LOSS_QUARANTINES",LostAckSticky);
        Console.WriteLine("B0161_W23_EXACT_SELECTED_729_669_SOURCE_TOTAL PASS="+passed+" FAIL="+failed);
        Console.WriteLine("B0161_W23_REAL_CHILD_CLR_PROCESSES="+children);
        Console.WriteLine("B0161_W23_RELEASE_GATE="+(failed==0?
            "HOLD_TWO_INDEPENDENT_OWNER_LOCK_FILES":"TEST_FAILURE_NOT_RELEASE"));
        Console.WriteLine("ACTUAL_UNITY_PHYSX_G1=NOT_RUN; CANONICAL_PRIVATE_510_RIG=UNIMPLEMENTED");
        return failed==0?0:1;
    }
}
