#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using B01.CanonicalWorldApi001;
using B01.CanonicalSlotAdapter003;
using B01.CanonicalSingleAuthorityStore003;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using LegacySlot=B01.SingleFileSaveSlot001.SingleFileSaveSlot;
using Session=B01.CanonicalSlotAdapter003.CanonicalSlotSessionV3;

internal static class Independent749
{
    const long Run=61924;
    static readonly Guid Slot=Guid.Parse("9b016124-0001-4000-8000-000000000749");
    static int passed,failed;
    static void Check(string name,Func<bool> fn)
    {
        try{if(fn()){passed++;Console.WriteLine("PASS "+name);}
            else{failed++;Console.WriteLine("FAIL "+name);}}
        catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+" "+ex);}
    }
    static byte[] Bytes(long generation,bool light,bool heavy,long run=Run)
    {
        var parts=new PersistedPart[2];
        for(int i=0;i<parts.Length;i++)
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
        return SnapshotCodec.Encode(new RunSnapshot(run,generation,parts));
    }
    static bool SameIds(byte[] data,long generation,bool light,bool heavy)
    {
        if(!ColdRestore.TryStage(data,out ColdRestore? cold)||cold==null||
           cold.Snapshot.Generation!=generation||cold.Snapshot.RunId!=Run)return false;
        return cold.ContainsConsumedId(0,FrozenPartCatalog.Id(0))==light&&
               cold.ContainsConsumedId(1,FrozenPartCatalog.Id(1))==heavy;
    }
    sealed class Gate:ICanonicalMutationGate
    {
        public Func<bool>? Hook;
        public bool IsQuiescent()=>Hook?.Invoke()??true;
    }
    sealed class World:ICanonicalStagedWorld
    {
        readonly RunSnapshot original;
        public bool Active,Retired;
        public World(RunSnapshot snapshot){original=snapshot;}
        public RunSnapshot Capture()=>original;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Active=false;Retired=true;}
    }
    sealed class Factory:ICanonicalRigFactory
    {
        public bool FailPrepare, FailCleanup,ThrowCleanup;
        public World? Partial,Published,Prior;
        public int Builds,Cleanups;
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot snap,ColdRestore ledger)
        {
            if(ledger.Snapshot.RunId!=snap.RunId||
               ledger.ContainsConsumedId(0,FrozenPartCatalog.Id(0))!=snap.Parts[0].Consumed)
                throw new InvalidOperationException("Original frozen W06 mismatch");
            return new Owned(this,snap);
        }
        sealed class Owned:ICanonicalStageOwnership
        {
            readonly Factory owner; readonly RunSnapshot snapshot;
            public Owned(Factory f,RunSnapshot s){owner=f;snapshot=s;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                owner.Builds++;
                if(owner.FailPrepare)
                {
                    owner.Partial=new World(snapshot){Active=true};
                    throw new IOException("injected orphan managed private root");
                }
                owner.Prior=owner.Published;
                owner.Published=new World(snapshot);
                return owner.Published;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                owner.Cleanups++;
                if(owner.ThrowCleanup)throw new IOException("cannot prove physical cleanup");
                if(owner.FailCleanup)return false;
                owner.Partial?.Retire();
                return true;
            }
        }
        public bool RetireAll()
        {
            Partial?.Retire();Published?.Retire();Prior?.Retire();
            return (Partial==null||Partial.Retired)&&
                   (Published==null||Published.Retired)&&
                   (Prior==null||Prior.Retired);
        }
    }
    sealed class AtomicFault:IAtomicEnvelopePublisher
    {
        public bool LoseAfterRename,LoseBeforeRename;
        public int Calls;
        public void Publish(string stage,string final)
        {
            Calls++;
            if(LoseBeforeRename)throw new IOException("fault before disk rename");
            new OsAtomicEnvelopePublisher().Publish(stage,final);
            if(LoseAfterRename)throw new IOException("lost ACK AFTER genuine rename");
        }
    }
    static bool Scratch(Func<string,bool> fn)
    {
        string path=Path.Combine(Path.GetTempPath(),"b0161-w24-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        string root=Path.Combine(path,"authorized-root");
        Directory.CreateDirectory(root);
        try{return fn(root);}
        finally{Directory.Delete(path,true);}
    }
    static bool Open(string root,bool create,Factory f,out Session? s,
        out Gate gate,IAtomicEnvelopePublisher? writer=null)
    {
        gate=new Gate();
        var st=Session.TryOpen(root,Run,Slot,create,out s,writer);
        return st==CanonicalSessionOpenStatus.Accepted&&s!=null&&s.Attach(gate,f);
    }
    static bool Close(Session? s,Factory f)
    {
        if(s==null)return false;
        bool destroyed=f.RetireAll();
        bool closed=s.TryCloseAfterPhysicalRetirement(()=>destroyed);
        s.Dispose();return destroyed&&closed;
    }
    static string SlotDir(string root)=>Path.Combine(root,
        "b0162_canonical_single_authority_v3",
        "run_"+Run+"_slot_"+Slot.ToString("N"));
    static bool ChildProbe(string root,bool expected)
    {
        var start=new ProcessStartInfo("dotnet")
            {UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        start.ArgumentList.Add("--probe");start.ArgumentList.Add(root);
        using var process=Process.Start(start);
        if(process==null)return false;
        string stdout=process.StandardOutput.ReadToEnd();
        string stderr=process.StandardError.ReadToEnd();
        if(!process.WaitForExit(12000)){try{process.Kill(true);}catch{}return false;}
        return process.ExitCode==(expected?0:3)&&
            stdout.Contains(expected?"W24_CHILD_OWNER_ACQUIRED":"W24_CHILD_OWNER_BUSY");
    }
    static int Probe(string root)
    {
        var s=Session.TryOpen(root,Run,Slot,false,out var session);
        if(s==CanonicalSessionOpenStatus.Busy)
        {Console.WriteLine("W24_CHILD_OWNER_BUSY");return 3;}
        if(s==CanonicalSessionOpenStatus.Accepted&&session!=null)
        {
            session.TryCloseAfterPhysicalRetirement(()=>true);
            session.Dispose();Console.WriteLine("W24_CHILD_OWNER_ACQUIRED");return 0;
        }
        Console.WriteLine("W24_CHILD_UNEXPECTED_"+s);return 5;
    }
    static bool FullColdAndOwnerLease(string root)
    {
        var f=new Factory();
        if(!Open(root,true,f,out var s,out _ )||s==null)return false;
        try
        {
            if(s.TryRestore(Bytes(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
               s.TryRestore(Bytes(2,false,true),Run,2)!=CanonicalGateResult.Accepted||
               s.TryRestore(Bytes(3,true,true),Run,3)!=CanonicalGateResult.Accepted||
               !s.TryCapturePreview(out byte[] p)||!SameIds(p,3,true,true)||
               !s.Read(Run,out long gen,out string hash)||gen!=3||
               hash!=Session.SourceHash(Bytes(3,true,true))||
               !ChildProbe(root,false))return false;
            string authority=Path.Combine(root,"b0123_canonical_world_authority");
            if(Directory.GetFiles(authority,"*.owner").Length!=1||
               Directory.GetFiles(SlotDir(root),"*.lease").Length!=0)return false;
            if(!Close(s,f)||!ChildProbe(root,true))return false;
            var restored=new Factory();
            if(!Open(root,false,restored,out var cold,out _)||cold==null)return false;
            try
            {
                if(cold.TryRestore(Bytes(3,false,true),Run,3)!=CanonicalGateResult.StaleGeneration||
                   restored.Builds!=0||
                   cold.TryRestore(Bytes(3,true,true),Run,3)!=CanonicalGateResult.Accepted||
                   !cold.TryCapturePreview(out p)||!SameIds(p,3,true,true))
                   return false;
                return Close(cold,restored);
            }
            finally{Close(cold,restored);}
        }
        finally{Close(s,f);}
    }
    static bool PartialUnretired(string root,bool throws)
    {
        var f=new Factory{FailPrepare=true,FailCleanup=!throws,ThrowCleanup=throws};
        if(!Open(root,true,f,out var session,out _)||session==null)return false;
        try
        {
            if(session.TryRestore(Bytes(1,true,false),Run,1)!=CanonicalGateResult.RollbackQuarantine||
               !session.Quarantined||session.HasWorld||
               f.Partial==null||!f.Partial.Active||f.Cleanups!=1)return false;
            session.Dispose();
            if(!session.OwnsSlot||!ChildProbe(root,false)||
               session.TryCloseAfterPhysicalRetirement(()=>false)||
               session.TryCloseAfterPhysicalRetirement(()=>throw new IOException("lying proof"))||
               !ChildProbe(root,false)||!session.OwnsSlot)return false;
            f.Partial.Retire();
            bool ok=session.TryCloseAfterPhysicalRetirement(()=>
                f.Partial.Retired&&!f.Partial.Active);
            if(ok)Console.WriteLine("W24_SELECTED_749_ORPHAN_RETIRE_PROOF_GREEN");
            return ok&&ChildProbe(root,true);
        }
        finally{f.RetireAll();Close(session,f);}
    }
    static bool LostAckCold(string root,bool after)
    {
        var pub=new AtomicFault();
        var f=new Factory();
        if(!Open(root,true,f,out var session,out _,pub)||session==null)return false;
        try
        {
            if(session.TryRestore(Bytes(1,true,false),Run,1)!=CanonicalGateResult.Accepted)return false;
            if(after)pub.LoseAfterRename=true;else pub.LoseBeforeRename=true;
            if(session.TryRestore(Bytes(2,false,true),Run,2)!=CanonicalGateResult.RollbackQuarantine||
               !session.Quarantined||
               session.TryRestore(Bytes(3,true,true),Run,3)!=CanonicalGateResult.RollbackQuarantine||
               !ChildProbe(root,false))return false;
            if(!Close(session,f))return false;
            var next=new Factory();
            if(!Open(root,false,next,out var cold,out _ )||cold==null)return false;
            try
            {
                long expected=after?2:1;
                bool light=!after,heavy=after;
                if(!cold.Read(Run,out long gen,out string hash)||gen!=expected||
                   hash!=Session.SourceHash(Bytes(expected,light,heavy))||
                   cold.TryRestore(Bytes(expected,light,heavy),Run,expected)!=CanonicalGateResult.Accepted||
                   !cold.TryCapturePreview(out byte[] p)||!SameIds(p,expected,light,heavy))
                   return false;
                Console.WriteLine(after?"W24_SELECTED_749_LOST_ACK_REAL_COMMIT_G2_COLD_GREEN":"W24_SELECTED_749_PRE_RENAME_FAULT_G1_COLD_GREEN");
                return Close(cold,next);
            }
            finally{Close(cold,next);}
        }
        finally{Close(session,f);}
    }
    static bool Reentry(string root)
    {
        var f=new Factory();
        if(!Open(root,true,f,out var s,out var gate)||s==null)return false;
        try
        {
            int calls=0;
            gate.Hook=()=>{
                calls++;
                return s.TryRestore(Bytes(1,true,false),Run,1)==CanonicalGateResult.HostBusy;
            };
            bool works=s.TryRestore(Bytes(1,true,false),Run,1)==CanonicalGateResult.Accepted&&calls>=2;
            gate.Hook=null;
            return works&&Close(s,f);
        }
        finally{gate.Hook=null;Close(s,f);}
    }
    static bool LegacyAndForeign(string root)
    {
        var f=new Factory();
        if(!Open(root,true,f,out var s,out _ )||s==null)return false;
        try
        {
            byte[] corrupt=Bytes(1,true,false);corrupt[90]^=1;
            if(s.TryRestore(corrupt,Run,1)!=CanonicalGateResult.InvalidSnapshot||
               s.TryRestore(Bytes(1,true,false,Run+1),Run,1)!=CanonicalGateResult.InvalidSnapshot||
               f.Builds!=0)return false;
            if(s.TryRestore(Bytes(1,true,false),Run,1)!=CanonicalGateResult.Accepted)return false;
            string dir=SlotDir(root);
            if(LegacySlot.TryOpenExisting(dir,Run,Slot,out var other)!=
               B01.SingleFileSaveSlot001.SlotStatus.WrongOwner||other!=null||
               LegacySlot.TryCreateNew(dir,Run,Slot,out other)!=
               B01.SingleFileSaveSlot001.SlotStatus.WrongOwner||other!=null)return false;
            if(!Close(s,f))return false;
            // Honest crash resilience does NOT guarantee authenticity against externally
            // replaced, correctly checksummed full Save. No anti-tamper claim.
            return !File.Exists(Path.Combine(dir,"slot.lease"));
        }
        finally{Close(s,f);}
    }
    static bool CorruptPhysical(string root)
    {
        var f=new Factory();
        if(!Open(root,true,f,out var s,out _ )||s==null)return false;
        try
        {
            if(s.TryRestore(Bytes(1,true,false),Run,1)!=CanonicalGateResult.Accepted)return false;
            string file=Path.Combine(SlotDir(root),"save.envelope");
            byte[] original=File.ReadAllBytes(file);
            original[original.Length-1]^=1;
            File.WriteAllBytes(file,original);
            return !s.Read(Run,out _,out _)&&s.Quarantined&&
                   s.TryRestore(Bytes(2,false,true),Run,2)==CanonicalGateResult.RollbackQuarantine&&
                   !s.TryCapturePreview(out _)&&ChildProbe(root,false);
        }
        finally{Close(s,f);}
    }
    public static int Main(string[] args)
    {
        if(args.Length==2&&args[0]=="--probe")return Probe(args[1]);
        if(args.Length!=0)return 4;
        for(int i=0;i<4;i++)
        {
            int mask=i;
            Check("ORIGINAL_556_W06_CONSUMED_BITS_"+mask,()=>
                SameIds(Bytes(1,(mask&1)!=0,(mask&2)!=0),1,(mask&1)!=0,(mask&2)!=0));
        }
        Check("GREEN_749_ONE_OWNER_REAL_CHILD_AND_THREE_GENERATION_COLD",()=>Scratch(FullColdAndOwnerLease));
        Check("GREEN_749_PARTIAL_CLEANUP_FALSE_NO_EARLY_RELEASE",()=>Scratch(x=>PartialUnretired(x,false)));
        Check("GREEN_749_PARTIAL_CLEANUP_THROW_NO_EARLY_RELEASE",()=>Scratch(x=>PartialUnretired(x,true)));
        Check("GREEN_749_LOST_ACK_POST_RENAME_REAL_COLD",()=>Scratch(x=>LostAckCold(x,true)));
        Check("GREEN_749_FAULT_BEFORE_RENAME_REAL_COLD",()=>Scratch(x=>LostAckCold(x,false)));
        Check("GREEN_749_QUIESCENCE_REENTRY_BUSY_FENCE",()=>Scratch(Reentry));
        Check("GREEN_749_LEGACY_RAW_669_MARKER_AND_FOREIGN_RUN_DENIED",()=>Scratch(LegacyAndForeign));
        Check("GREEN_749_CORRUPT_PHYSICAL_ENVELOPE_STICKY_QUARANTINE",()=>Scratch(CorruptPhysical));
        Console.WriteLine("B0161_W24_SELECTED_749_DIRECT_UNMODIFIED_729_GATEWAY_669_LEGACY_556_W06");
        Console.WriteLine("TOTAL "+(passed+failed)+" PASS "+passed+" FAIL "+failed);
        Console.WriteLine("SOURCE_RELEASE_GATE="+(failed==0?"GREEN_SCOPE_ONLY_FROZEN_PRIVATE_510_HOLD":"HOLD_TEST_FAILURE"));
        Console.WriteLine("SOURCE_G0_REQUIRES_W24_INDEPENDENT_REVIEW NATIVE_G1_NOT_RUN PRIVATE_RIG_510_NOT_WIRED G2_HOLD");
        return failed==0?0:1;
    }
}
