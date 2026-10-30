#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using B01.CanonicalSlotAdapter002;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

// INDEPENDENT W22 source QA: actual immutable R2 #712 + #691 + #669.
// Only owner's not-yet-written private Unity #510 Rig[] is a fault double.
internal static class QA
{
    const long Run=61222;
    static readonly Guid Slot=Guid.Parse("61222000-6122-4122-8122-006122006122");
    static int passed,failed,childProcesses;
    static byte[] Original(long epoch,bool light,bool heavy,long run=Run)
    {
        var a=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            var spec=SecureRigCatalog.At(i);
            bool d=i==0?light:heavy;
            long owner=FrozenPartCatalog.Owner(i);
            a[i]=new PersistedPart(spec.PartId,owner,d?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                d?PartDockState.Detached:PartDockState.Attached,d,!d,
                new Vector3d(spec.RootX+spec.PartOffsetX,d?2.2:1.2,0),
                0,0,0,1,d?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),spec.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,epoch,a));
    }
    static bool Ids(byte[] b,long epoch,bool light,bool heavy)
    {
        return ColdRestore.TryStage(b,out ColdRestore? c)&&c!=null&&
            c.Snapshot.RunId==Run&&c.Snapshot.Generation==epoch&&
            c.Snapshot.Parts[0].Id==2310001&&c.Snapshot.Parts[1].Id==2310002&&
            c.ContainsConsumedId(0,2310001)==light&&
            c.ContainsConsumedId(1,2310002)==heavy;
    }
    enum Failure{None,ReturnsFalse,Throws,SafeCleanup}
    sealed class Gate:ICanonicalMutationGate { public bool IsQuiescent()=>true; }
    sealed class World:ICanonicalStagedWorld
    {
        readonly RunSnapshot state;
        public bool Active,Retired;
        public World(RunSnapshot state){this.state=state;}
        public RunSnapshot Capture()=>state;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Active=false;Retired=true;}
    }
    sealed class Factory:ICanonicalRigFactory
    {
        public readonly Failure Mode;
        public World? Partial,Published;
        public int Cleanups;
        public Factory(Failure m){Mode=m;}
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot snap,ColdRestore plan)
        {
            if(plan.ContainsConsumedId(0,2310001)!=snap.Parts[0].Consumed)
                throw new IOException("W06 private ledger mismatch");
            return new Stage(this,snap);
        }
        sealed class Stage:ICanonicalStageOwnership
        {
            readonly Factory f;readonly RunSnapshot s;
            public Stage(Factory f,RunSnapshot s){this.f=f;this.s=s;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                if(f.Mode!=Failure.None)
                {
                    f.Partial=new World(s){Active=true};
                    throw new IOException("partial owned rig allocated before stage returns");
                }
                f.Published=new World(s);
                return f.Published;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                f.Cleanups++;
                if(f.Mode==Failure.ReturnsFalse)return false;
                if(f.Mode==Failure.Throws)throw new IOException("owned cleanup throws");
                f.Partial?.Retire();f.Published?.Retire();return true;
            }
        }
    }
    static bool Open(string path,bool create,Factory f,out CanonicalSlotSessionR2? s)
    {
        var result=CanonicalSlotSessionR2.TryOpen(path,Run,Slot,create,out s);
        return result==CanonicalSessionOpenStatus.Accepted&&s!=null&&s.Attach(new Gate(),f);
    }
    static bool Release(CanonicalSlotSessionR2? s,Factory f)
    {
        if(s==null)return false;
        f.Partial?.Retire();f.Published?.Retire();
        bool released=s.TryCloseAfterPhysicalRetirement(()=>
            (f.Partial==null||f.Partial.Retired&&!f.Partial.Active)&&
            (f.Published==null||f.Published.Retired&&!f.Published.Active));
        s.Dispose();return released;
    }
    static int Child(string path)
    {
        var state=CanonicalSlotSessionR2.TryOpen(path,Run,Slot,false,out var s);
        if(state==CanonicalSessionOpenStatus.Busy&&s==null)
        {
            Console.WriteLine("CHILD_REAL_OS_LEASE_BUSY");
            return 5;
        }
        if(state!=CanonicalSessionOpenStatus.Accepted||s==null)return 6;
        var factory=new Factory(Failure.None);
        if(!s.Attach(new Gate(),factory))return 7;
        try
        {
            if(s.TryRestore(Original(1,false,true),Run,1)!=CanonicalGateResult.Accepted||
                !s.TryCapturePreview(out byte[] b)||!Ids(b,1,false,true)||
                factory.Published==null||!factory.Published.Active)return 8;
            Console.WriteLine("CHILD_REAL_556_HEAVY_CONSUMED_2310002_COMMITTED");
            return 0;
        }
        finally{Release(s,factory);}
    }
    static bool ChildProbe(string path,int expected)
    {
        var start=new ProcessStartInfo("dotnet")
        { UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true };
        start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        start.ArgumentList.Add("--child");
        start.ArgumentList.Add(path);
        using var p=Process.Start(start);
        if(p==null)return false;
        if(!p.WaitForExit(12000))
        {try{p.Kill(true);}catch{} return false;}
        string log=p.StandardOutput.ReadToEnd(),error=p.StandardError.ReadToEnd();
        childProcesses++;
        if(p.ExitCode!=expected)
          Console.WriteLine("CHILD_EXPECTED="+expected+" ACTUAL="+p.ExitCode+" LOG="+log+" ERROR="+error);
        return p.ExitCode==expected&&
          (expected!=5||log.Contains("CHILD_REAL_OS_LEASE_BUSY"))&&
          (expected!=0||log.Contains("CHILD_REAL_556_HEAVY_CONSUMED_2310002_COMMITTED"));
    }
    static bool FailedPartial(string path,Failure fault)
    {
        var f=new Factory(fault);
        if(!Open(path,true,f,out var s)||s==null)return false;
        if(s.TryRestore(Original(1,true,false),Run,1)!=CanonicalGateResult.RollbackQuarantine||
           !s.Quarantined||s.HasWorld||!s.OwnsSlot||f.Partial==null||
           !f.Partial.Active||f.Cleanups!=1)return false;
        // The EXACT old #707 P0 — owner has an active orphan but HasWorld=false.
        s.Dispose();
        if(!s.OwnsSlot||!s.Quarantined||!ChildProbe(path,5))return false;
        var busy=CanonicalSlotSessionR2.TryOpen(path,Run,Slot,false,out var competitor);
        if(busy!=CanonicalSessionOpenStatus.Busy||competitor!=null)return false;
        if(s.TryCloseAfterPhysicalRetirement(()=>false)||
           s.TryCloseAfterPhysicalRetirement(()=>throw new IOException("not yet retired"))||
           !s.OwnsSlot||!ChildProbe(path,5))return false;
        Console.WriteLine("W22_712_ORPHAN_QUARANTINE_LOCK_RETENTION_GREEN="+fault);
        f.Partial.Retire();
        if(!s.TryCloseAfterPhysicalRetirement(()=>f.Partial.Retired&&!f.Partial.Active)||
           s.OwnsSlot)return false;
        if(!ChildProbe(path,0))return false;
        var coldFactory=new Factory(Failure.None);
        if(!Open(path,false,coldFactory,out var cold)||cold==null)return false;
        bool ok=cold.TryRestore(Original(1,false,true),Run,1)==CanonicalGateResult.Accepted&&
            cold.TryCapturePreview(out byte[] b)&&Ids(b,1,false,true);
        Console.WriteLine("W22_712_TRUE_COLD_RECOVERY_ONLY_AFTER_RETIRE_GREEN="+fault);
        return ok&&Release(cold,coldFactory);
    }
    static bool Normal(string path)
    {
        var f=new Factory(Failure.None);
        if(!Open(path,true,f,out var s)||s==null)return false;
        bool ok=s.TryRestore(Original(1,true,false),Run,1)==CanonicalGateResult.Accepted&&
            s.TryCapturePreview(out byte[] b)&&Ids(b,1,true,false)&&
            ChildProbe(path,5)&&
            s.TryRestore(Original(1,false,true),Run,1)==CanonicalGateResult.StaleGeneration&&
            s.TryRestore(Original(2,true,true,Run+1),Run,2)!=CanonicalGateResult.Accepted&&
            !s.Quarantined;
        return ok&&Release(s,f);
    }
    static bool SuccessfulPartial(string path)
    {
        var f=new Factory(Failure.SafeCleanup);
        if(!Open(path,true,f,out var s)||s==null)return false;
        if(s.TryRestore(Original(1,true,false),Run,1)!=CanonicalGateResult.HostException||
            s.Quarantined||s.HasWorld||f.Cleanups!=1||f.Partial==null||
            !f.Partial.Retired||f.Partial.Active)return false;
        s.Dispose();
        return !s.OwnsSlot&&ChildProbe(path,0);
    }
    static void Test(string name,Func<string,bool> action)
    {
        string root=Path.Combine(Path.GetTempPath(),"b0161-w22-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            bool ok=action(Path.Combine(root,"owned-slot"));
            if(ok){passed++;Console.WriteLine("PASS "+name);}
            else{failed++;Console.WriteLine("FAIL "+name);}
        }
        catch(Exception e){failed++;Console.WriteLine("FAIL "+name+" "+e);}
        finally{try{Directory.Delete(root,true);}catch(IOException){}
                catch(UnauthorizedAccessException){}}
    }
    public static int Main(string[] args)
    {
        if(args.Length==2&&args[0]=="--child")return Child(args[1]);
        if(args.Length>0)return 9;
        Test("FROZEN_556_W06_FOUR_ID_CONFIGURATIONS",_=>{
            for(int i=0;i<4;i++)
                if(!Ids(Original(1,(i&1)!=0,(i&2)!=0),
                    1,(i&1)!=0,(i&2)!=0))return false;
            return true;
        });
        Test("ORIGINAL_712_NORMAL_EXCLUSIVE_LEASE_STALE_FORK_WRONG_RUN",Normal);
        Test("ORIGINAL_712_FALSE_CLEANUP_ACTIVE_ORPHAN_DISPOSE_FAIL_CLOSED",p=>
            FailedPartial(p,Failure.ReturnsFalse));
        Test("ORIGINAL_712_THROW_CLEANUP_ACTIVE_ORPHAN_DISPOSE_FAIL_CLOSED",p=>
            FailedPartial(p,Failure.Throws));
        Test("ORIGINAL_712_HEALTHY_PARTIAL_CLEANUP_PERMITS_COLD_REOPEN",SuccessfulPartial);
        Console.WriteLine("B0161_W22_ORIGINAL_712_691_669_SOURCE_TOTAL PASS="+passed+" FAIL="+failed);
        Console.WriteLine("B0161_W22_INDEPENDENT_REAL_CHILD_CLR_PROCESSES="+childProcesses);
        Console.WriteLine("B0161_W22_SOURCE_FIX_707="+(failed==0?"GREEN_EXCLUSIVE_OWNER_UNTIL_VERIFIED_RETIRE":"NOT_VERIFIED"));
        Console.WriteLine("NATIVE_UNITY_G1=NOT_RUN PRIVATE_510_ADAPTER=UNIMPLEMENTED G2=HOLD NO_MERGE");
        return failed==0?0:1;
    }
}
