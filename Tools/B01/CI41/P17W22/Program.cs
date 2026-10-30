#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using B01.CanonicalWorldApi001;
using B01.CanonicalSlotAdapter002;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

internal static class Program
{
    const long Run=412712;
    static readonly Guid Slot=Guid.Parse("14c41041-c21a-4acc-8ed2-0ec407120022");
    static int checks,pairs,workers;
    static void Must(bool yes,string name)
    {
        checks++;
        if(!yes)throw new InvalidOperationException("W22_INDEPENDENT_CHECK_FAILED:"+name);
    }
    static byte[] Bytes(long generation,bool light,bool heavy)
    {
        var parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            var spec=SecureRigCatalog.At(i);
            bool detached=i==0?light:heavy;
            long owner=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(spec.PartId,owner,detached?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                detached?PartDockState.Detached:PartDockState.Attached,
                detached,!detached,
                new Vector3d(spec.RootX+spec.PartOffsetX,detached?2.2:1.2,0),
                0,0,0,1,detached?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),spec.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(Run,generation,parts));
    }
    static void AssertIds(byte[] bytes,long gen,bool light,bool heavy)
    {
        Must(ColdRestore.TryStage(bytes,out ColdRestore? cold)&&cold!=null
             &&cold.Snapshot.RunId==Run&&cold.Snapshot.Generation==gen,
             "exact immutable #556 binary integrity and run/generation");
        if(!ColdRestore.TryStage(bytes,out ColdRestore? state)||state==null)
            throw new InvalidOperationException("UNREACHABLE_BAD_FROZEN_BYTES");
        Must(state.ContainsConsumedId(0,FrozenPartCatalog.Id(0))==light&&
             state.ContainsConsumedId(1,FrozenPartCatalog.Id(1))==heavy,
             "exact original W06 consumed Light and Heavy IDs");
    }
    sealed class Gate:ICanonicalMutationGate
    {public bool IsQuiescent()=>true;}
    sealed class World:ICanonicalStagedWorld
    {
        readonly RunSnapshot snap;
        public bool Alive,Retired;
        public World(RunSnapshot snap){this.snap=snap;}
        public RunSnapshot Capture()=>snap;
        public void Activate(){Alive=true;}
        public void Deactivate(){Alive=false;}
        public void Retire(){Alive=false;Retired=true;}
    }
    sealed class Factory:ICanonicalRigFactory
    {
        public bool ReturnFalse,ThrowCleanup,PrepareThrows=true;
        public World? Partial;
        public int CleanupCalls;
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot snap,ColdRestore plan)
        {
            for(int i=0;i<2;i++)
                if(plan.ContainsConsumedId(i,FrozenPartCatalog.Id(i))!=snap.Parts[i].Consumed)
                    throw new InvalidOperationException("ORIGINAL_FROZEN_W06_ID_MISMATCH");
            return new Owned(this,snap);
        }
        sealed class Owned:ICanonicalStageOwnership
        {
            readonly Factory factory; readonly RunSnapshot snap;
            public Owned(Factory factory,RunSnapshot snap){this.factory=factory;this.snap=snap;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                factory.Partial=new World(snap){Alive=true};
                if(factory.PrepareThrows)
                    throw new IOException("STAGED_PRIVATE_RIG_ALLOCATED_THEN_PREPARE_THREW");
                return factory.Partial;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                factory.CleanupCalls++;
                if(factory.ThrowCleanup)throw new IOException("INJECTED_UNCERTAIN_RETIRE");
                if(factory.ReturnFalse)return false;
                if(factory.Partial!=null)factory.Partial.Retire();
                return factory.Partial==null||factory.Partial.Retired;
            }
        }
        public bool TrustedPhysicalRetire()
        {
            if(Partial==null)return false;
            Partial.Retire();
            return Partial.Retired&&!Partial.Alive;
        }
    }
    static Process Spawn(string root,string barrier,string ready,string result,string peer)
    {
        var psi=new ProcessStartInfo("dotnet"){UseShellExecute=false,CreateNoWindow=true};
        foreach(string arg in new[]{Assembly.GetExecutingAssembly().Location,
            "child",root,barrier,ready,result,peer})
            psi.ArgumentList.Add(arg);
        var p=Process.Start(psi);
        if(p==null)throw new IOException("ACTUAL_EXTERNAL_CLR_WORKER_UNAVAILABLE");
        workers++;return p;
    }
    static int Child(string[] args)
    {
        string directory=args[1],barrier=args[2],ready=args[3],result=args[4],peer=args[5];
        File.WriteAllText(ready,"READY");
        var sw=Stopwatch.StartNew();
        while(!File.Exists(barrier))
        {
            if(sw.Elapsed>TimeSpan.FromSeconds(25)){File.WriteAllText(result,"TIMEOUT");return 4;}
            Thread.Sleep(2);
        }
        var status=CanonicalSlotSessionR2.TryOpen(directory,Run,Slot,false,
            out CanonicalSlotSessionR2? sess);
        if(status==CanonicalSessionOpenStatus.Busy)
        {
            File.WriteAllText(result,"BUSY");return 0;
        }
        if(status!=CanonicalSessionOpenStatus.Accepted||sess==null)
        {
            File.WriteAllText(result,"UNEXPECTED_"+status);return 3;
        }
        try
        {
            var factory=new Factory{PrepareThrows=false};
            if(!sess.Attach(new Gate(),factory))
            {File.WriteAllText(result,"UNATTACHED");return 6;}
            var value=sess.TryRestore(Bytes(1,true,false),Run,1);
            if(value!=CanonicalGateResult.Accepted)
            {File.WriteAllText(result,"GATEWAY_"+value);return 7;}
            if(!sess.Read(Run,out long gen,out string digest)||gen!=1||
               digest!=CanonicalSlotSessionR2.SourceHash(Bytes(1,true,false)))
                return 8;
            return 0;
        }
        finally
        {
            // Real original success path, other process MUST observe lease busy,
            // then physical retire. Signal before release; peer writes result.
            File.WriteAllText(result,"ACCEPTED");
            var timer=Stopwatch.StartNew();
            while(!File.Exists(peer)&&timer.Elapsed<TimeSpan.FromSeconds(15))
                Thread.Sleep(2);
            // Explicit stage owner/physical test fake retirement, not native PhysX.
            sess.TryCloseAfterPhysicalRetirement(()=>factory.TrustedPhysicalRetire());
            sess.Dispose();
        }
    }
    static void Pair(string dir,string category,int id,bool expectedHolder)
    {
        string evidence=Path.Combine(Path.GetDirectoryName(dir)!,
             "pair-"+category+"-"+id.ToString("D2"));
        Directory.CreateDirectory(evidence);
        string go=Path.Combine(evidence,"go");
        string ar=Path.Combine(evidence,"a.ready"),br=Path.Combine(evidence,"b.ready");
        string ao=Path.Combine(evidence,"a.result"),bo=Path.Combine(evidence,"b.result");
        using var a=Spawn(dir,go,ar,ao,bo);
        using var b=Spawn(dir,go,br,bo,ao);
        var t=Stopwatch.StartNew();
        while(!File.Exists(ar)||!File.Exists(br))
        {
            if(t.Elapsed>TimeSpan.FromSeconds(25))
                throw new TimeoutException("BOTH_TRUE_CHILD_CLR_WORKERS_NOT_READY");
            Thread.Sleep(5);
        }
        File.WriteAllText(go,"START");
        Must(a.WaitForExit(25000)&&b.WaitForExit(25000),
             "two independent real CLR child processes complete");
        Must(a.ExitCode==0&&b.ExitCode==0,
             "both external original R2 session operations exit success");
        var first=File.ReadAllText(ao);var second=File.ReadAllText(bo);
        if(expectedHolder)
            Must(first=="BUSY"&&second=="BUSY",
                 "live partial owned rig must keep OS lock for BOTH external processes");
        else
            Must((first=="ACCEPTED"&&second=="BUSY")||
                 (first=="BUSY"&&second=="ACCEPTED"),
                 "exactly ONE external session authorized by real OS FileShare.None");
        pairs++;
    }
    static void QuarantinedScenario(string root,bool throwCleanup)
    {
        string variant=throwCleanup?"throws":"returnsfalse";
        string dir=Path.Combine(root,"owner-"+variant);
        var opened=CanonicalSlotSessionR2.TryOpen(dir,Run,Slot,true,out var owner);
        Must(opened==CanonicalSessionOpenStatus.Accepted&&owner!=null,
             "original #669 creates dedicated OS leased slot");
        if(owner==null)throw new InvalidOperationException("IMPOSSIBLE_OWNER_NULL");
        var f=new Factory{ReturnFalse=!throwCleanup,ThrowCleanup=throwCleanup};
        Must(owner.Attach(new Gate(),f),"original R2 attaches selected #691 gateway");
        AssertIds(Bytes(1,true,false),1,true,false);
        Must(owner.TryRestore(Bytes(1,true,false),Run,1)==CanonicalGateResult.RollbackQuarantine,
             "original #691 partial Prepare failure triggers same-call sticky quarantine");
        Must(owner.Quarantined&&!owner.HasWorld&&f.Partial!=null&&f.Partial.Alive&&
             f.CleanupCalls==1,
             "original #712 has unpublished but alive root and no active published world");
        owner.Dispose();owner.Dispose();
        Must(owner.OwnsSlot&&owner.Quarantined,
             "repeated original #712 Dispose must escrow FileShare.None OS lifetime owner");
        Must(owner.TryRestore(Bytes(1,false,true),Run,1)==CanonicalGateResult.RollbackQuarantine
             &&!owner.TryCapturePreview(out _),
             "no new Save or Restore in quarantined private orphan session");
        for(int i=0;i<2;i++)Pair(dir,variant+"-before",i,true);
        Must(!owner.TryCloseAfterPhysicalRetirement(()=>false),"false physical retirement proof denied");
        Must(!owner.TryCloseAfterPhysicalRetirement(
            ()=>throw new IOException("UNVERIFIABLE_RIG_HIERARCHY")),
            "throwing physical retirement proof denied");
        Must(owner.OwnsSlot,"OS lease still held after false/throw proof callback");
        Pair(dir,variant+"-still-locked",0,true);
        Must(f.TrustedPhysicalRetire()&&f.Partial!.Retired&&!f.Partial.Alive,
             "test-owned private rig specifically retired before releasing owner lease");
        Must(owner.TryCloseAfterPhysicalRetirement(
            ()=>f.Partial!.Retired&&!f.Partial.Alive),
            "explicit and verifiably retired private world can release original OS lease");
        Must(!owner.OwnsSlot,"OS lock now closed only after positive physical retirement");
        owner.Dispose();
        for(int i=0;i<2;i++)Pair(dir,variant+"-after",i,false);
        // External child must have authored genuine original full #556 envelope.
        var reopened=CanonicalSlotSessionR2.TryOpen(dir,Run,Slot,false,out var fresh);
        Must(reopened==CanonicalSessionOpenStatus.Accepted&&fresh!=null,
             "cold independent original session can safely acquire after root retired");
        if(fresh==null)throw new InvalidOperationException("NO_COLD_SESSION");
        try
        {
            Must(fresh.Read(Run,out long generation,out string hash)&&generation==1&&
                 hash==CanonicalSlotSessionR2.SourceHash(Bytes(1,true,false)),
                 "actual original #669 envelope retains exact W06 IDs and digest after race");
            var healthy=new Factory{PrepareThrows=false};
            Must(fresh.Attach(new Gate(),healthy),"cold canonical gateway attach");
            Must(fresh.TryRestore(Bytes(1,true,false),Run,1)==CanonicalGateResult.Accepted,
                 "exact original #556 cold replay restores Light/Heavy consumed ID");
            Must(healthy.Partial!=null&&healthy.Partial.Alive,
                 "healthy separate cold session owns actual staged managed private world");
            Must(healthy.TrustedPhysicalRetire(),"physically retire cold session before releasing");
            Must(fresh.TryCloseAfterPhysicalRetirement(
                 ()=>healthy.Partial!.Retired&&!healthy.Partial.Alive),
                 "cold restore cleanup verified before original R2 lease unlock");
        }
        finally {fresh.Dispose();}
    }
    static int Main(string[] args)
    {
        if(args.Length>0&&args[0]=="child")return Child(args);
        string dir=Path.Combine(Path.GetTempPath(),"ci41-w22-r2-original"+
           Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            QuarantinedScenario(dir,false);
            QuarantinedScenario(dir,true);
            Must(pairs==10&&workers==20,
                "exact ten separate two-CLR-process OS FileShare.None barrier races");
            Console.WriteLine("CI41_W22_ORIGINAL_712_707_SECURITY_REGRESSION_PASS asserts="+checks+
                " real_os_pairs="+pairs+" child_clr_processes="+workers);
            Console.WriteLine("CI41_W22_UNPUBLISHED_ORPHAN_QUARANTINE_HOLDS_REAL_669_OS_LEASE");
            Console.WriteLine("CI41_W22_FALSE_THROW_RETIRE_PROOFS_REFUSED_AND_COLD_556_IDS_RESTORED");
            Console.WriteLine("CI41_W22_SOURCE_ONLY_OWNER_PROOF_CALLBACK_NOT_NATIVE_RIG_ATTESTATION");
            Console.WriteLine("CI41_W22_LIVE_CANONICAL_510_G1_NOT_RUN_G2_HOLD");
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine("CI41_W22_SOURCE_SECURITY_TEST_FAILED "+e);return 2;}
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
}
