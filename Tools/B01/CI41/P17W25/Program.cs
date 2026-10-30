#nullable enable
using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Linq;
using B01.CanonicalWorldApi001;
using B01.CanonicalSlotAdapter004;
using B01.CanonicalSingleAuthorityStore004;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using Legacy=B01.SingleFileSaveSlot001.SingleFileSaveSlot;
internal static class Program
{
    const long Run=412924;
    static readonly Guid GuidSlot=Guid.Parse("b01b4141-0240-4124-8240-000000000749");
    static int checks,pairs,workers;
    static void Verify(bool value,string message)
    {
        checks++;
        if(!value)throw new InvalidOperationException("CI41_W25_ORIGINAL_ASSERT_FAIL:"+message);
    }
    static byte[] Snapshot(long gen,bool light,bool heavy)
    {
        var items=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool detached=i==0?light:heavy;
            var spec=SecureRigCatalog.At(i);
            long owner=FrozenPartCatalog.Owner(i);
            items[i]=new PersistedPart(spec.PartId,owner,detached?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                detached?PartDockState.Detached:PartDockState.Attached,
                detached,!detached,
                new Vector3d(spec.RootX+spec.PartOffsetX,detached?2.2:1.2,0),
                0,0,0,1,new Vector3d(detached?0.25:0,0,0),
                new Vector3d(0,0,0),spec.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(Run,gen,items));
    }
    static void AssertFrozen(byte[] b,long gen,bool light,bool heavy)
    {
        Verify(ColdRestore.TryStage(b,out ColdRestore? cold)&&cold!=null&&
            cold.Snapshot.RunId==Run&&cold.Snapshot.Generation==gen,
            "original frozen #556 binary cold stage exact run/epoch");
        if(!ColdRestore.TryStage(b,out ColdRestore? c)||c==null)
            throw new IOException("NO_FROZEN_COLD_LEDGER");
        Verify(c.ContainsConsumedId(0,FrozenPartCatalog.Id(0))==light&&
               c.ContainsConsumedId(1,FrozenPartCatalog.Id(1))==heavy,
               "true original W06 Light and Heavy immutable consumed IDs");
    }
    sealed class Gate:ICanonicalMutationGate
    {public bool IsQuiescent()=>true;}
    sealed class TestWorld:ICanonicalStagedWorld
    {
        readonly RunSnapshot snapshot;
        public bool Alive,Retired;
        public TestWorld(RunSnapshot snap){snapshot=snap;}
        public RunSnapshot Capture()=>snapshot;
        public void Activate(){Alive=true;}
        public void Deactivate(){Alive=false;}
        public void Retire(){Alive=false;Retired=true;}
    }
    sealed class Factory:ICanonicalRigFactory
    {
        public bool PrepareFailure,CleanupThrows,CleanupFalse;
        public TestWorld? Last;
        public int CleanupCalls;
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot snap,ColdRestore cold)
        {
            for(int i=0;i<FrozenPartCatalog.Slots;i++)
                if(cold.ContainsConsumedId(i,FrozenPartCatalog.Id(i))!=snap.Parts[i].Consumed)
                    throw new IOException("ACTUAL_FROZEN_CONSUMED_LEDGER_MISMATCH");
            return new Owned(this,snap);
        }
        sealed class Owned:ICanonicalStageOwnership
        {
            readonly Factory f;readonly RunSnapshot snap;
            public Owned(Factory f,RunSnapshot snap){this.f=f;this.snap=snap;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                f.Last=new TestWorld(snap);
                if(f.PrepareFailure)
                {f.Last.Alive=true;throw new IOException("PARTIAL_OWNED_WORLD_ALLOCATED_BEFORE_THROW");}
                return f.Last;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                f.CleanupCalls++;
                if(f.CleanupThrows)throw new IOException("SYNTHETIC_PRIVATE_ROOT_CLEANUP_EXCEPTION");
                if(f.CleanupFalse)return false;
                f.Last?.Retire();return f.Last==null||f.Last.Retired;
            }
        }
        public bool RootsGone()=>Last==null||Last.Retired&&!Last.Alive;
        public bool RetireAll(){Last?.Retire();return RootsGone();}
    }
    static Process ChildProcess(string root,string start,string ready,string output,string other)
    {
        var psi=new ProcessStartInfo("dotnet"){UseShellExecute=false,CreateNoWindow=true};
        foreach(var a in new[]{Assembly.GetExecutingAssembly().Location,
                 "child",root,start,ready,output,other})psi.ArgumentList.Add(a);
        Process? p=Process.Start(psi);
        if(p==null)throw new IOException("CHILD_OS_PROCESS_MISSING");
        workers++;return p;
    }
    static int Child(string[] a)
    {
        string root=a[1],start=a[2],ready=a[3],output=a[4],other=a[5];
        File.WriteAllText(ready,"READY");
        Stopwatch watch=Stopwatch.StartNew();
        while(!File.Exists(start))
        {
            if(watch.Elapsed>TimeSpan.FromSeconds(25))
            {File.WriteAllText(output,"TIMEOUT");return 5;}
            Thread.Sleep(3);
        }
        var res=CanonicalSlotSessionV3R2.TryOpen(root,Run,GuidSlot,false,out var slot);
        if(res==CanonicalSessionOpenStatus.Busy)
        {File.WriteAllText(output,"BUSY");return 0;}
        if(res!=CanonicalSessionOpenStatus.Accepted||slot==null)
        {File.WriteAllText(output,"INVALID_"+res);return 3;}
        try
        {
            // Original V3 requires BindOnce via Attach before storage Read;
            // Unbound acquired owner is NOT an active publication authority.
            if(!slot.Attach(new Gate(),new Factory()))
            {File.WriteAllText(output,"ATTACH_FAILED");return 9;}
            // Genuine exact original physical Save envelope or EMPTY after failed Prepare.
            if(!slot.Read(Run,out long gen,out string hash)||gen<0||
               (gen>0&&hash!=CanonicalSlotSessionV3R2.SourceHash(Snapshot(1,true,false))))
            {File.WriteAllText(output,"READ_INVALID");return 7;}
            File.WriteAllText(output,"ACCEPTED");
            var hold=Stopwatch.StartNew();
            while(!File.Exists(other)&&hold.Elapsed<TimeSpan.FromSeconds(20))
                Thread.Sleep(3);
            return 0;
        }
        finally
        {
            slot.TryCloseAfterPhysicalRetirement(()=>true);
            slot.Dispose();
        }
    }
    static void Compete(string root,string evidence,bool mustDeny)
    {
        Directory.CreateDirectory(evidence);
        string start=Path.Combine(evidence,"start");
        string ra=Path.Combine(evidence,"a.ready"),rb=Path.Combine(evidence,"b.ready");
        string oa=Path.Combine(evidence,"a.out"),ob=Path.Combine(evidence,"b.out");
        using var a=ChildProcess(root,start,ra,oa,ob);
        using var b=ChildProcess(root,start,rb,ob,oa);
        var clock=Stopwatch.StartNew();
        while(!File.Exists(ra)||!File.Exists(rb))
        {
            if(clock.Elapsed>TimeSpan.FromSeconds(25))
                throw new TimeoutException("EXTERNAL_CLR_WORKERS_DID_NOT_START");
            Thread.Sleep(4);
        }
        File.WriteAllText(start,"GO");
        Verify(a.WaitForExit(25000)&&b.WaitForExit(25000),
               "both independent CLR processes exited");
        string sa=File.Exists(oa)?File.ReadAllText(oa):"MISSING_A";
        string sb=File.Exists(ob)?File.ReadAllText(ob):"MISSING_B";
        Verify(a.ExitCode==0&&b.ExitCode==0,
               "both original real sessions exit expected status: a="+a.ExitCode+
               "/"+sa+" b="+b.ExitCode+"/"+sb);
        if(mustDeny)
            Verify(sa=="BUSY"&&sb=="BUSY",
                   "two external CLR processes denied by SAME original .owner FileShare.None");
        else
            Verify((sa=="ACCEPTED"&&sb=="BUSY")||
                   (sa=="BUSY"&&sb=="ACCEPTED"),
                   "exactly one external original #749 session owns same slot after retirement");
        pairs++;
    }
    static string V3SaveDir(string root)=>
        Path.Combine(root,"b0162_canonical_single_authority_v3",
            "run_"+Run+"_slot_"+GuidSlot.ToString("N"));
    static void Scenario(string scratch,int index)
    {
        string root=Path.Combine(scratch,"scenario-"+index.ToString("D2"),"authorized");
        Directory.CreateDirectory(root);
        var f=new Factory{PrepareFailure=index!=0,
               CleanupFalse=index==1,CleanupThrows=index==2};
        Verify(CanonicalSlotSessionV3R2.TryOpen(root,Run,GuidSlot,true,out var session)
                ==CanonicalSessionOpenStatus.Accepted&&session!=null,
               "original candidate #773 owns fresh trusted root and ONE genuine V3 FileStream");
        if(session==null)throw new IOException("SESSION_NULL");
        Verify(session.Attach(new Gate(),f),"original V3 gateway bound exactly once");
        Verify(!session.Attach(new Gate(),new Factory()),"cannot bind second V3 owner to same session");
        string authorityDir=Path.Combine(root,"b0123_canonical_world_authority");
        string saveDir=V3SaveDir(root);
        Verify(Directory.Exists(authorityDir)&&
               Directory.GetFiles(authorityDir,"*.owner").Length==1,
               "exactly ONE owner authority physical .owner FileStream");
        Verify(Directory.GetFiles(root,"*.lease",SearchOption.AllDirectories).Length==0,
               "no legacy independent .lease exists anywhere in owned source root");
        Verify(File.Exists(Path.Combine(saveDir,"slot.canonical-v3"))&&
               !File.Exists(Path.Combine(saveDir,"slot.owner")),
               "new V3 marker cannot be opened with old #669 identity");
        // Hostile legacy original #669 is compiled DIRECT from immutable original Git:
        Verify(Legacy.TryOpenExisting(saveDir,Run,GuidSlot,out var legacy)!=
               B01.SingleFileSaveSlot001.SlotStatus.Accepted&&legacy==null,
               "old original #669 cannot bypass selected new V3 marker");
        byte[] light=Snapshot(1,true,false);
        AssertFrozen(light,1,true,false);
        var result=session.TryRestore(light,Run,1);
        if(index==0)
        {
            Verify(result==CanonicalGateResult.Accepted&&session.HasWorld&&
                   !session.Quarantined&&f.Last!=null&&f.Last.Alive,
                   "genuine full original #556 Save accepted via sole V3 session");
            Verify(session.Read(Run,out long gen,out string hash)&&gen==1&&
                   hash==CanonicalSlotSessionV3R2.SourceHash(light),
                   "single original #556 physical envelope correct generation and SHA");
            Verify(session.TryCapturePreview(out byte[] preview)&&preview.SequenceEqual(light),
                   "real original V3 gateway preview equals canonical stored snapshot");
            Verify(session.TryRestore(Snapshot(1,false,true),Run,1)==
                   CanonicalGateResult.StaleGeneration,
                   "same-generation different original W06 Light/Heavy ledger rejected");
        }
        else
        {
            Verify(result==CanonicalGateResult.RollbackQuarantine&&session.Quarantined&&
                   !session.HasWorld&&f.Last!=null&&f.Last.Alive&&f.CleanupCalls==1,
                   "false/throwing owned partial rig cleanup causes sticky quarantine");
            Verify(!session.TryCapturePreview(out _)&&
                   session.TryRestore(Snapshot(2,false,true),Run,1)==
                       CanonicalGateResult.RollbackQuarantine,
                   "quarantined owner rejects further original snapshot preview and future restore");
        }
        Compete(root,Path.Combine(Path.GetDirectoryName(root)!,"before"),true);
        session.Dispose();session.Dispose();
        Verify(session.OwnsSlot,"raw Dispose twice keeps original candidate R2 owner alive");
        Compete(root,Path.Combine(Path.GetDirectoryName(root)!,"after-dispose"),true);
        Verify(!session.TryCloseAfterPhysicalRetirement(()=>false),
               "false physical retirement callback cannot release original owner");
        Verify(!session.TryCloseAfterPhysicalRetirement(
               ()=>throw new IOException("UNVERIFIED_RIGIDBODY_ROOTS")),
               "throwing physical retirement callback cannot release original owner");
        Verify(session.OwnsSlot,"one OS owner escrowed after false/throw retirement");
        Compete(root,Path.Combine(Path.GetDirectoryName(root)!,"after-failed-proof"),true);
        Verify(f.RetireAll(),"surrogate physical root explicitly retired before lease release");
        Verify(session.TryCloseAfterPhysicalRetirement(f.RootsGone),
               "only verified partial and published root retirement releases owner");
        Verify(!session.OwnsSlot,"single physical owner actually released");
        Compete(root,Path.Combine(Path.GetDirectoryName(root)!,"after-retired"),false);
        Verify(CanonicalSlotSessionV3R2.TryOpen(root,Run,GuidSlot,false,out var cold)
                  ==CanonicalSessionOpenStatus.Accepted&&cold!=null,
               "fresh independent source owner can acquire after trusted retirement");
        if(cold==null)throw new IOException("COLD_REOPEN_FAILED");
        try
        {
            var newFactory=new Factory();
            Verify(cold.Attach(new Gate(),newFactory),
                   "fresh cold owner binds one original gateway before physical Save Read");
            Verify(cold.Read(Run,out long gen,out string hash)&&
                   gen==(index==0?1:0)&&
                   (gen==0||hash==CanonicalSlotSessionV3R2.SourceHash(light)),
                   "cold genuine full single-file envelope persists correct epoch after multiple races");
            if(index==0)
            {
                Verify(cold.TryRestore(light,Run,1)==CanonicalGateResult.Accepted,
                       "cold original #556 run and W06 Light/Heavy ledger restored");
                Verify(newFactory.RetireAll(),"cold surrogate physically retired");
                Verify(cold.TryCloseAfterPhysicalRetirement(newFactory.RootsGone),
                       "cold replay authority released only after all roots retired");
            }
            else Verify(cold.TryCloseAfterPhysicalRetirement(()=>true),
                        "empty store original owner safe explicit release");
        }
        finally{cold.Dispose();}
    }

    // Independently exercise ACTUAL PUBLIC #773 Store API, not session-only happy path.
    // A case-insensitive "canonical" comparison admits a DIFFERENT Linux directory.
    static void RawCaseFoldWitness(string scratch)
    {
        string root=Path.Combine(scratch,"casefold","trusted");
        Directory.CreateDirectory(root);
        Verify(CanonicalWorldSlotLease.TryAcquire(root,Run,GuidSlot,out var lease)
                &&lease!=null,"real original #729 OS .owner acquired for raw #773 public Store");
        if(lease==null)throw new IOException("REAL_V3_OWNER_NULL");
        var gw=new CanonicalWorldGateway(new Gate(),new Factory(),
                                        new NoopPort(),lease);
        string physicalCanonical=Path.Combine(root,
          "b0162_canonical_single_authority_v3",
          "run_"+Run+"_slot_"+GuidSlot.ToString("N"));
        string caseDifferent=Path.Combine(root,
          "B0162_canonical_single_authority_v3",
          "run_"+Run+"_slot_"+GuidSlot.ToString("N"));
        var admission=CanonicalSingleAuthorityStoreV3R2.TryCreateNew(
           caseDifferent,Run,GuidSlot,lease,out var wrong);
        // On Linux, original #773 currently compares two distinct FULL PATHS
        // with OrdinalIgnoreCase, even though the OS filesystem is case-sensitive.
        Verify(admission==SlotStatus.Accepted&&wrong!=null,
               "actual #773 public raw store accepted case-folded path");
        if(wrong==null)throw new IOException("CANONICAL_CASE_WITNESS_UNEXPECTED_DENIAL");
        byte[] light=Snapshot(1,true,false);
        Verify(wrong.TryCommit(0,light)==SlotStatus.Accepted,
               "actual original #773 wrong-cased public path physically committed original #556 g1");
        Verify(wrong.TryRead(out long generation,out byte[] readback)==SlotStatus.Accepted
               &&generation==1&&readback.SequenceEqual(light),
               "physical casefold sibling contains verified original #556 Light consumed envelope");
        Verify(File.Exists(Path.Combine(caseDifferent,"save.envelope")),
               "different-case sibling physical file exists after flush and atomic rename");
        Compete(root,Path.Combine(scratch,"casefold","external-while-owned"),true);
        wrong.Dispose();
        Verify(gw.TryCloseAfterPhysicalRetirement(()=>true),
               "original V3 owner released only with synthetic no-live-world proof");
        Verify(CanonicalWorldSlotLease.TryAcquire(root,Run,GuidSlot,out var next)
               &&next!=null,"new physical owner after verified release");
        if(next==null)throw new IOException("COLD_OWNER_MISSING");
        var freshGateway=new CanonicalWorldGateway(new Gate(),new Factory(),
                                                   new NoopPort(),next);
        try
        {
            var coldResult=CanonicalSingleAuthorityStoreV3R2.TryOpenExisting(
                physicalCanonical,Run,GuidSlot,next,out var cold);
            if(OperatingSystem.IsLinux())
            {
                Verify(!Directory.Exists(physicalCanonical) &&
                       Directory.Exists(caseDifferent),
                       "Linux actual OS has two distinct physical dirs by casing");
                Verify(coldResult==SlotStatus.WrongOwner&&cold==null,
                       "Linux exact canonical path CANNOT cold reopen committed sibling Save");
                Console.WriteLine("CI41_W25_773_LINUX_RAW_PUBLIC_CASE_ALIAS_P0_REPRODUCED_PRODUCT_RED");
            }
            else
            {
                Verify(coldResult==SlotStatus.Accepted&&cold!=null,
                       "Windows case-insensitive OS reopens identical physical canonical slot");
                if(cold!=null)
                {
                    Verify(cold.TryRead(out long gen,out byte[] bytes)==SlotStatus.Accepted
                           &&gen==1&&bytes.SequenceEqual(light),
                           "Windows raw case alias resolves same original envelope");
                    cold.Dispose();
                }
                Console.WriteLine("CI41_W25_773_WINDOWS_CASE_INSENSITIVE_COLD_REOPEN_OK");
            }
        }
        finally{Verify(freshGateway.TryCloseAfterPhysicalRetirement(()=>true),
                       "cold lease physically retired after real disk casefold probe");}
    }
    sealed class NoopPort:ICanonicalCommitPort
    {
        public bool Read(long run,out long generation,out string hash)
        {generation=0;hash="";return false;}
        public bool Commit(long run,long previous,string previousHash,long next,string hash)
        =>false;
    }

    static int Main(string[] args)
    {
        if(args.Length>0&&args[0]=="child")return Child(args);
        string root=Path.Combine(Path.GetTempPath(),
            "ci41-w25-candidate773-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            for(int i=0;i<3;i++)Scenario(root,i);
            RawCaseFoldWitness(root);
            Verify(pairs==13&&workers==26,
                "three independent original V3 sessions plus raw public store: 26 external CLR");
            Console.WriteLine("CI41_W25_CANDIDATE_773_SELECTED_LINEAGE_ORIGINAL_OS_QA_PASS assertions="+checks+
                " barrier_pairs="+pairs+" child_processes="+workers);
            Console.WriteLine("CI41_W25_707_PARTIAL_FALSE_THROW_QUARANTINE_ESCROW_PASS");
            Console.WriteLine("CI41_W25_669_LEGACY_WRITER_REJECTED_AND_FROZEN_556_IDS_COLD_PASS");
            Console.WriteLine("CI41_W25_REAL_OS_SOURCE_ONLY_PRODUCT_CASEFOLD_RED_UNITY_G1_NOT_RUN_G2_HOLD");
            return 0;
        }
        catch(Exception e)
        {
            Console.Error.WriteLine("CI41_W25_ORIGINAL_SELECTED_SOURCE_QA_FAIL "+e);
            return 2;
        }
        finally{Directory.Delete(root,true);}
    }
}
