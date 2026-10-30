// W25 B01-62 R3 NEW versioned correction of source #773 POSIX case alias.
// This harness is the same independently authored W25 18 original source tests
// with test19 inverted to demand PRODUCTION GREEN, not simply reproduce RED.
// NEW W24 QA derived from prior own W23 test fixture ONLY. Product compiled directly as ORIGINAL selected #749; no source changes.\n// New independent W23 original source tests: exact #729 V3 V3-store derived original #669 and original #556/W06.
#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.CanonicalSingleAuthorityStore005;
using OldSlot=B01.SingleFileSaveSlot001.SingleFileSaveSlot;
using B01.CanonicalSlotAdapter005;
using Session=B01.CanonicalSlotAdapter005.CanonicalSlotSessionV3R3;
 
// B01-62 W22 independent direct-original source security differential.
// V1 #697 is EXPECTED RED, corrected R2 #712 is EXPECTED GREEN.
// Original production #691/#669/#556/W06 code directly compiled, no copied implementation.
internal static class IndependentOriginalGate
{
    private const long Run=62562;
    private static readonly Guid Slot=Guid.Parse("01a62262-1000-4000-8000-000000000712");
    private static int passed,failed;
    private static void Check(string name,Func<bool> test)
    {
        try { if(test()){passed++;Console.WriteLine("PASS "+name);}
              else {failed++;Console.WriteLine("FAIL "+name);} }
        catch(Exception e){failed++;Console.WriteLine("FAIL "+name+" "+e);}
    }
    private static byte[] Snapshot(long gen,bool light,bool heavy,long run=Run)
    {
        var arr=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool d=i==0?light:heavy;
            var s=SecureRigCatalog.At(i);
            long owner=FrozenPartCatalog.Owner(i);
            arr[i]=new PersistedPart(s.PartId,owner,d?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                d?PartDockState.Detached:PartDockState.Attached,d,!d,
                new Vector3d(s.RootX+s.PartOffsetX,d?2.2:1.2,0),
                0,0,0,1,new Vector3d(d?0.25:0,0,0),new Vector3d(0,0,0),s.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,gen,arr));
    }
    private static bool Light(byte[] b,long gen,bool l,bool h)
    {
        return ColdRestore.TryStage(b,out var c)&&c!=null&&c.Snapshot.RunId==Run&&
            c.Snapshot.Generation==gen&&
            c.ContainsConsumedId(0,FrozenPartCatalog.Id(0))==l&&
            c.ContainsConsumedId(1,FrozenPartCatalog.Id(1))==h;
    }
    private enum Failure {None, CleanupFalse, CleanupThrow, CleanupSuccess}
    private sealed class Gate:ICanonicalMutationGate
    {public bool IsQuiescent()=>true;}
    private sealed class World:ICanonicalStagedWorld
    {
        public readonly RunSnapshot Snap;
        public bool Active,Retired;
        public World(RunSnapshot snap,bool active=false){Snap=snap;Active=active;}
        public RunSnapshot Capture()=>Snap;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Retired=true;Active=false;}
    }
    private sealed class Factory:ICanonicalRigFactory
    {
        public Failure Fault;
        public World? Partial,Published;
        public int Begins,Preparations,CleanupCalls;
        public Factory(Failure f){Fault=f;}
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot snap,ColdRestore cold)
        {
            Begins++;
            if(cold.Snapshot.RunId!=snap.RunId||
               cold.ContainsConsumedId(0,FrozenPartCatalog.Id(0))!=snap.Parts[0].Consumed)
                throw new InvalidOperationException("Original W06 membership mismatch");
            return new Stage(this,snap);
        }
        private sealed class Stage:ICanonicalStageOwnership
        {
            private readonly Factory f;
            private readonly RunSnapshot snap;
            public Stage(Factory f,RunSnapshot snap){this.f=f;this.snap=snap;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                f.Preparations++;
                if(f.Fault!=Failure.None)
                {
                    f.Partial=new World(snap,true);
                    throw new IOException("new private rig allocated; unreturned stage throws");
                }
                f.Published=new World(snap);
                return f.Published;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                f.CleanupCalls++;
                if(f.Fault==Failure.CleanupFalse)return false;
                if(f.Fault==Failure.CleanupThrow)
                    throw new IOException("cannot retire partially allocated private root");
                f.Partial?.Retire();
                f.Published?.Retire();
                return true;
            }
        }
        public bool RootsGone()=>(Partial==null||Partial.Retired)&&
            (Published==null||Published.Retired);
        public void RetireAll(){Partial?.Retire();Published?.Retire();}
    }
    private static bool New(string dir,bool create,Factory f,out Session? owner)
    {
        var result=Session.TryOpen(dir,Run,Slot,create,out owner);
        return result==CanonicalSessionOpenStatus.Accepted&&owner!=null&&
            owner.Attach(new Gate(),f);
    }
    private static bool CloseTrusted(Session? owner,Factory f)
    {
        if(owner==null)return false;
        if(!f.RootsGone())return false;
        bool ok=owner.TryCloseAfterPhysicalRetirement(f.RootsGone);
        owner.Dispose();return ok;
    }
    private static bool Scratch(Func<string,bool> test)
    {
        string root=Path.Combine(Path.GetTempPath(),
            "b0162-w22-independent-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string owned=Path.Combine(root,"owned");
        Directory.CreateDirectory(owned); // original #729 requires an existing authorized private root
        try{return test(owned);}
        finally {Directory.Delete(root,true);}
    }
    private static bool TrySecondClr(string dir,bool shouldGet)
    {
        var psi=new ProcessStartInfo("dotnet")
        {UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        psi.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        psi.ArgumentList.Add("--probe");psi.ArgumentList.Add(dir);
        using var process=Process.Start(psi);
        if(process==null)return false;
        string outText=process.StandardOutput.ReadToEnd();
        string errText=process.StandardError.ReadToEnd();
        if(!process.WaitForExit(15000))
        { try{process.Kill(true);}catch{} return false; }
        bool actual=outText.Contains("W23_CHILD_ACQUIRED");
        return process.ExitCode==(actual?0:3)&&actual==shouldGet&&
            !outText.Contains("W23_CHILD_ERROR");
    }
    private static bool HealthyLeaseAndColdSave()
        =>Scratch(dir=>{
            var f=new Factory(Failure.None);
            if(!New(dir,true,f,out var s)||s==null)return false;
            try
            {
                if(s.TryRestore(Snapshot(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
                   !s.HasWorld||!s.OwnsSlot||f.Published==null||!f.Published.Active)
                    return false;
                if(!TrySecondClr(dir,false))return false;
                if(!s.Read(Run,out long gen,out string sha)||gen!=1||
                   sha!=Session.SourceHash(Snapshot(1,true,false)))return false;
                f.Published.Retire();
                if(!CloseTrusted(s,f))return false;
                if(!TrySecondClr(dir,true))return false;
                if(!New(dir,false,new Factory(Failure.None),out var cold)||cold==null)
                    return false;
                try
                {
                    if(!cold.Read(Run,out gen,out sha)||gen!=1||
                       sha!=Session.SourceHash(Snapshot(1,true,false)))return false;
                    return cold.TryRestore(Snapshot(1,true,false),Run,1)==CanonicalGateResult.Accepted;
                }
                finally{cold.TryCloseAfterPhysicalRetirement(()=>true);cold.Dispose();}
            }
            finally{f.RetireAll();CloseTrusted(s,f);}
        });
    private static bool BadPartialLease(Failure behavior,bool useChild)
        =>Scratch(dir=>{
            var f=new Factory(behavior);
            if(!New(dir,true,f,out var s)||s==null)return false;
            try
            {
                var verdict=s.TryRestore(Snapshot(1,true,false),Run,1);
                if(verdict!=CanonicalGateResult.RollbackQuarantine||
                   !s.Quarantined||s.HasWorld||!s.OwnsSlot||
                   f.Partial==null||!f.Partial.Active||f.Partial.Retired||
                   f.CleanupCalls!=1)return false;
                s.Dispose();
                bool expectedAcquire=false;
                bool ok=useChild?TrySecondClr(dir,expectedAcquire):
                    CanOpen(dir,expectedAcquire);
                if(!ok)return false;
                Console.WriteLine("W23_CANONICAL_V3_SINGLE_OWNER_ORPHAN_LEASE_HELD_GREEN");
                // A false physical cleanup proof cannot release authority.
                if(s.TryCloseAfterPhysicalRetirement(()=>false))return false;
                if(!TrySecondClr(dir,false))return false;
                f.Partial.Retire();
                if(!CloseTrusted(s,f))return false;
                return TrySecondClr(dir,true);
            }
            finally
            {
                f.RetireAll();
                CloseTrusted(s,f);
            }
        });
    private static bool CanOpen(string dir,bool expected)
    {
        var result=Session.TryOpen(dir,Run,Slot,false,out var contender);
        contender?.TryCloseAfterPhysicalRetirement(()=>true);
        contender?.Dispose();
        return expected?result==CanonicalSessionOpenStatus.Accepted&&contender!=null:
                       result==CanonicalSessionOpenStatus.Busy&&contender==null;
    }
    private static bool SafeUnpublishedCleanup()
        =>Scratch(dir=>{
            var f=new Factory(Failure.CleanupSuccess);
            if(!New(dir,true,f,out var s)||s==null)return false;
            try
            {
                var v=s.TryRestore(Snapshot(1,true,false),Run,1);
                if(v!=CanonicalGateResult.HostException||s.Quarantined||
                   s.HasWorld||f.Partial==null||!f.Partial.Retired||
                   f.Partial.Active||f.CleanupCalls!=1)return false;
                // V3 intentionally requires explicit gateway physical-retirement
                // proof, even when a wholly-cleaned stage left no published world.
                if(!TrySecondClr(dir,false))return false;
                if(!CloseTrusted(s,f))return false;
                return TrySecondClr(dir,true);
            }
            finally{f.RetireAll();CloseTrusted(s,f);}
        });
    private static bool InvalidBeforeAlloc()
        =>Scratch(dir=>{
            var f=new Factory(Failure.None);
            if(!New(dir,true,f,out var s)||s==null)return false;
            try
            {
                byte[] bad=Snapshot(1,true,false);bad[90]^=4;
                if(s.TryRestore(bad,Run,1)!=CanonicalGateResult.InvalidSnapshot||
                   s.TryRestore(Snapshot(1,true,false,Run+1),Run,1)!=
                        CanonicalGateResult.InvalidSnapshot||
                   f.Begins!=0||s.Quarantined)return false;
                return !TrySecondClr(dir,true);
            }
            finally{f.RetireAll();CloseTrusted(s,f);}
        });
    private static bool WrongGuidOrRun()
        =>Scratch(dir=>{
            var f=new Factory(Failure.None);
            if(!New(dir,true,f,out var s)||s==null)return false;
            try
            {
                // Foreign identity is checked only after the legitimate
                // FileShare.None lifetime owner has physically retired.
                if(!CloseTrusted(s,f))return false;
                if(Session.TryOpen(dir,Run,Guid.NewGuid(),false,out var bad)!=
                    CanonicalSessionOpenStatus.NoOwner||bad!=null)return false;
                if(Session.TryOpen(dir,Run+1,Slot,false,out bad)!=
                    CanonicalSessionOpenStatus.NoOwner||bad!=null)return false;
                return true;
            }
            finally{f.RetireAll();CloseTrusted(s,f);}
        });
    private static bool StaleReplay()
        =>Scratch(dir=>{
            var f=new Factory(Failure.None);
            if(!New(dir,true,f,out var s)||s==null)return false;
            try
            {
                if(s.TryRestore(Snapshot(1,true,false),Run,1)!=CanonicalGateResult.Accepted)
                    return false;
                if(s.TryRestore(Snapshot(1,false,true),Run,1)!=
                   CanonicalGateResult.StaleGeneration)return false;
                if(s.TryRestore(Snapshot(2,false,true),Run,2)!=
                   CanonicalGateResult.Accepted)return false;
                if(!s.TryCapturePreview(out var preview)||
                   !Light(preview,2,false,true))return false;
                return !TrySecondClr(dir,true);
            }
            finally{f.RetireAll();CloseTrusted(s,f);}
        });
    private static int Child(string root)
    {
        var st=Session.TryOpen(root,Run,Slot,false,out var owned);
        if(st==CanonicalSessionOpenStatus.Accepted&&owned!=null)
        {owned.Dispose();Console.WriteLine("W23_CHILD_ACQUIRED");return 0;}
        if(st==CanonicalSessionOpenStatus.Busy)
        {Console.WriteLine("W23_CHILD_DENIED");return 3;}
        Console.WriteLine("W23_CHILD_ERROR "+st);return 4;
    }
    private static bool OnePhysicalOwnerNoLegacyBypass()
        =>Scratch(root=>{
            var f=new Factory(Failure.None);
            if(!New(root,true,f,out var session)||session==null)return false;
            try
            {
                if(session.TryRestore(Snapshot(1,true,false),Run,1)!=CanonicalGateResult.Accepted)
                    return false;
                string slotPath=Path.Combine(root,"b0162_canonical_single_authority_v3",
                    "run_"+Run+"_slot_"+Slot.ToString("N"));
                string authorities=Path.Combine(root,"b0123_canonical_world_authority");
                if(!Directory.Exists(authorities)||
                   Directory.GetFiles(authorities,"*.owner").Length!=1||
                   !Directory.Exists(slotPath)||
                   Directory.GetFiles(slotPath,"*.lease").Length!=0)return false;
                // Legacy unchanged original #669 must REJECT the new V3 slot,
                // even when it could acquire its unrelated .lease file.
                if(OldSlot.TryOpenExisting(slotPath,Run,Slot,out var legacy)!=
                   B01.SingleFileSaveSlot001.SlotStatus.WrongOwner||legacy!=null)
                    return false;
                if(OldSlot.TryCreateNew(slotPath,Run,Slot,out legacy)!=
                   B01.SingleFileSaveSlot001.SlotStatus.WrongOwner||legacy!=null)
                    return false;
                return !TrySecondClr(root,true)&&f.Published!=null&&
                       f.Published.Active;
            }
            finally{f.RetireAll();CloseTrusted(session,f);}
        });
    // New W24 negative runner: physically perform a REAL rename and lose ACK,
    // or inject pre-rename failure. Read/cold recovery uses exact ORIGINAL #556.
    private sealed class CrashAfterMove:IAtomicEnvelopePublisher
    {
        public bool FailBefore,FailAfter;
        public int Calls;
        public void Publish(string staged,string final)
        {
            Calls++;
            if(FailBefore)throw new IOException("W24 injected failed publication before File.Move");
            new OsAtomicEnvelopePublisher().Publish(staged,final);
            if(FailAfter)throw new IOException("W24 injected lost ACK after REAL File.Move");
        }
    }
    private static string OwnedSlot(string root)
        =>Path.Combine(root,"b0162_canonical_single_authority_v3",
            "run_"+Run+"_slot_"+Slot.ToString("N"));
    private static bool LostAckCrashRecovery(bool afterMove)
        =>Scratch(root=>{
            var publisher=new CrashAfterMove();
            var f=new Factory(Failure.None);
            Session? s=null;
            var initial=Session.TryOpen(root,Run,Slot,true,out s,publisher);
            if(initial!=CanonicalSessionOpenStatus.Accepted||s==null||
                !s.Attach(new Gate(),f))return false;
            try
            {
                if(s.TryRestore(Snapshot(1,true,false),Run,1)!=CanonicalGateResult.Accepted)
                    return false;
                if(afterMove)publisher.FailAfter=true;
                else publisher.FailBefore=true;
                if(s.TryRestore(Snapshot(2,false,true),Run,2)!=
                    CanonicalGateResult.RollbackQuarantine||!s.Quarantined||
                    publisher.Calls!=2)return false;
                if(!TrySecondClr(root,false))return false;
                if(s.TryRestore(Snapshot(3,true,true),Run,3)!=
                    CanonicalGateResult.RollbackQuarantine)return false;
                f.RetireAll();
                if(!CloseTrusted(s,f))return false;
                s=null;
                var fresh=new Factory(Failure.None);
                if(!New(root,false,fresh,out var reopen)||reopen==null)return false;
                try
                {
                    long expected=afterMove?2:1;
                    byte[] bytes=Snapshot(expected,!afterMove,afterMove);
                    if(!reopen.Read(Run,out long gen,out string digest)||
                       gen!=expected||digest!=Session.SourceHash(bytes))return false;
                    if(reopen.TryRestore(bytes,Run,expected)!=CanonicalGateResult.Accepted)
                        return false;
                    return Directory.GetFiles(OwnedSlot(root),"*.lease").Length==0;
                }
                finally{fresh.RetireAll();CloseTrusted(reopen,fresh);}
            }
            finally{f.RetireAll();if(s!=null)CloseTrusted(s,f);}
        });
    private static bool CorruptRecordMustQuarantine()
        =>Scratch(root=>{
            var f=new Factory(Failure.None);
            if(!New(root,true,f,out var s)||s==null)return false;
            try
            {
                if(s.TryRestore(Snapshot(1,true,false),Run,1)!=CanonicalGateResult.Accepted)
                    return false;
                string path=Path.Combine(OwnedSlot(root),"save.envelope");
                byte[] bad=File.ReadAllBytes(path);
                bad[bad.Length-1]^=0x40;
                File.WriteAllBytes(path,bad);
                if(s.Read(Run,out _,out _)||!s.Quarantined||
                    s.TryRestore(Snapshot(2,false,true),Run,2)!=
                        CanonicalGateResult.RollbackQuarantine||
                    !File.ReadAllBytes(path).SequenceEqual(bad))return false;
                if(!TrySecondClr(root,false))return false;
                f.RetireAll();
                if(!CloseTrusted(s,f))return false;
                if(!New(root,false,new Factory(Failure.None),out var fresh)||
                   fresh==null)return false;
                try
                {
                    bool blocked=!fresh.Read(Run,out _,out _)&&fresh.Quarantined;
                    return blocked&&File.ReadAllBytes(path).SequenceEqual(bad);
                }
                finally{fresh.TryCloseAfterPhysicalRetirement(()=>true);fresh.Dispose();}
            }
            finally{f.RetireAll();CloseTrusted(s,f);}
        });
    private static bool IncompleteOrphanStageIgnoredWithoutDeletion()
        =>Scratch(root=>{
            var f=new Factory(Failure.None);
            if(!New(root,true,f,out var s)||s==null)return false;
            try
            {
                if(s.TryRestore(Snapshot(1,true,false),Run,1)!=CanonicalGateResult.Accepted)
                    return false;
                string directory=OwnedSlot(root);
                string stage=Path.Combine(directory,"pending-foreign-w24.stage");
                byte[] foreign=new byte[]{0x63,0xF0,0x01,0x5A};
                File.WriteAllBytes(stage,foreign);
                f.RetireAll();
                if(!CloseTrusted(s,f))return false;
                var fresh=new Factory(Failure.None);
                if(!New(root,false,fresh,out var next)||next==null)return false;
                try
                {
                    if(!next.Read(Run,out long generation,out string hash)||
                       generation!=1||
                       hash!=Session.SourceHash(Snapshot(1,true,false))||
                       !File.ReadAllBytes(stage).SequenceEqual(foreign))
                        return false;
                    if(next.TryRestore(Snapshot(2,false,true),Run,2)!=
                        CanonicalGateResult.Accepted)return false;
                    return File.ReadAllBytes(stage).SequenceEqual(foreign)&&
                           next.Read(Run,out generation,out hash)&&generation==2&&
                           hash==Session.SourceHash(Snapshot(2,false,true));
                }
                finally{fresh.RetireAll();CloseTrusted(next,fresh);}
            }
            finally{f.RetireAll();CloseTrusted(s,f);}
        });
    private static bool NormalThreeGenerationsColdAndIDs()
        =>Scratch(root=>{
            var f=new Factory(Failure.None);
            if(!New(root,true,f,out var s)||s==null)return false;
            try
            {
                if(s.TryRestore(Snapshot(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
                   s.TryRestore(Snapshot(2,false,true),Run,2)!=CanonicalGateResult.Accepted)
                    return false;
                if(!s.Read(Run,out long g,out string hash)||g!=2||
                   hash!=Session.SourceHash(Snapshot(2,false,true)))
                    return false;
                f.RetireAll();
                if(!CloseTrusted(s,f))return false;
                var nextFactory=new Factory(Failure.None);
                if(!New(root,false,nextFactory,out var next)||next==null)return false;
                try
                {
                    if(next.TryRestore(Snapshot(2,false,true),Run,2)!=
                        CanonicalGateResult.Accepted)return false;
                    if(next.TryRestore(Snapshot(3,true,true),Run,3)!=
                        CanonicalGateResult.Accepted)return false;
                    if(!next.TryCapturePreview(out var preview)||
                       !Light(preview,3,true,true))return false;
                    return next.Read(Run,out g,out hash)&&g==3&&
                           hash==Session.SourceHash(Snapshot(3,true,true))&&
                           Directory.GetFiles(OwnedSlot(root),"*.lease").Length==0;
                }
                finally{nextFactory.RetireAll();CloseTrusted(next,nextFactory);}
            }
            finally{f.RetireAll();CloseTrusted(s,f);}
        });
    private sealed class NoopSavePort:ICanonicalCommitPort
    {
        public bool Read(long run,out long gen,out string fingerprint)
        {gen=0;fingerprint="";return false;}
        public bool Commit(long run,long expected,string prior,long next,string hash)=>false;
    }
    // W25 independent Linux case-sensitive path alias: owner identity and slot
    // match, but OrdinalIgnoreCase on POSIX may allow two physical envelopes
    // for two sequential same-slot owner lifetimes.
    private static bool CaseVariantRejectedAndOneCanonicalCold()
        =>Scratch(root=>{
            string canonical=OwnedSlot(root);
            string variant=canonical.Replace(
                "b0162_canonical_single_authority_v3",
                "b0162_CANONICAL_single_authority_v3");
            if(string.Equals(canonical,variant,StringComparison.Ordinal))return false;
            if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out var firstOwner)||
               firstOwner==null)return false;
            var originalGateway=new CanonicalWorldGateway(new Gate(),
                new Factory(Failure.None),new NoopSavePort(),firstOwner);
            try
            {
                // The original #773 case-insensitive check ALLOWED this to
                // create a physically different directory on POSIX.
                var rejected=CanonicalSingleAuthorityStoreV3R3.TryCreateNew(
                    variant,Run,Slot,firstOwner,out var invalid);
                if(rejected!=SlotStatus.Invalid||invalid!=null||
                   Directory.Exists(variant)||File.Exists(variant))return false;
                // The SINGLE exact-casing owner path still functions.
                var accepted=CanonicalSingleAuthorityStoreV3R3.TryCreateNew(
                    canonical,Run,Slot,firstOwner,out var owned);
                if(accepted!=SlotStatus.Accepted||owned==null)return false;
                try
                {
                    if(owned.TryCommit(0,Snapshot(1,true,false))!=
                       SlotStatus.Accepted)return false;
                    if(owned.TryRead(out var generation,out var payload)!=
                       SlotStatus.Accepted||generation!=1||
                       !Light(payload,1,true,false))return false;
                }
                finally{owned.Dispose();}
            }
            finally{originalGateway.TryCloseAfterPhysicalRetirement(()=>true);}
            // Old owner has truly retired, and a NEW physical same-slot owner
            // must see exactly g1 of original frozen W06 rather than a fork.
            var physical=new Factory(Failure.None);
            if(!New(root,false,physical,out var restart)||restart==null)return false;
            try
            {
                if(restart.TryRestore(Snapshot(1,true,false),Run,1)!=
                   CanonicalGateResult.Accepted||
                   !restart.Read(Run,out var g,out var hash)||
                   g!=1||hash!=Session.SourceHash(Snapshot(1,true,false))||
                   Directory.Exists(variant))return false;
                Console.WriteLine(
                  "W25_R3_WINDOWS_LINUX_EXACT_CASE_ONE_CANONICAL_SAVE=SOURCE_GREEN");
                return true;
            }
            finally{physical.RetireAll();CloseTrusted(restart,physical);}
        });
    private static bool RawSameOwnerSecondStoreDenied()
        =>Scratch(root=>{
            if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out var auth)||
               auth==null)return false;
            try
            {
                string canonical=OwnedSlot(root);
                var st=CanonicalSingleAuthorityStoreV3R3.TryCreateNew(
                    canonical,Run,Slot,auth,out var first);
                if(st!=SlotStatus.Accepted||first==null)return false;
                try
                {
                    st=CanonicalSingleAuthorityStoreV3R3.TryOpenExisting(
                        canonical,Run,Slot,auth,out var second);
                    if(st!=SlotStatus.Busy||second!=null)return false;
                    first.Dispose();
                    st=CanonicalSingleAuthorityStoreV3R3.TryOpenExisting(
                        canonical,Run,Slot,auth,out second);
                    return st==SlotStatus.Busy&&second==null;
                }
                finally{first.Dispose();}
            }
            finally{auth.TryCancelUnbound();}
        });
    private static bool RawForeignSiblingDeniedNoBytes()
        =>Scratch(root=>{
            if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out var auth)||
               auth==null)return false;
            try
            {
                string foreign=Path.Combine(Path.GetDirectoryName(root)!,
                    "foreign-"+Guid.NewGuid().ToString("N"));
                var st=CanonicalSingleAuthorityStoreV3R3.TryCreateNew(
                    foreign,Run,Slot,auth,out var store);
                return st==SlotStatus.Invalid&&store==null&&
                       !Directory.Exists(foreign);
            }
            finally{auth.TryCancelUnbound();}
        });
    public static int Main(string[] args)
    {
        if(args.Length==2&&args[0]=="--probe")return Child(args[1]);
        Check("01 direct untouched #556 Light stable consumed ID",()=>
            Light(Snapshot(1,true,false),1,true,false));
        Check("02 direct untouched #556 Heavy stable consumed ID",()=>
            Light(Snapshot(1,false,true),1,false,true));
        Check("03 original physical save/g1 cold restart and OS-process lease",HealthyLeaseAndColdSave);
        Check("04 failed partial ROOT cleanup FALSE may not lie about HasWorld",()=>
            BadPartialLease(Failure.CleanupFalse,false));
        Check("05 failed partial ROOT cleanup THROW cross-CLR boundary",()=>
            BadPartialLease(Failure.CleanupThrow,true));
        Check("06 failed partial cleanup FALSE real child process attempts owner",()=>
            BadPartialLease(Failure.CleanupFalse,true));
        Check("07 healthy partial owner cleanup permits fresh CLR acquisition",SafeUnpublishedCleanup);
        Check("08 malformed original #556 and wrong run reject before stage",InvalidBeforeAlloc);
        Check("09 foreign run/slot GUID protected",WrongGuidOrRun);
        Check("10 original same-generation different Light/Heavy refused",StaleReplay);
        Check("11 canonical original #729 ONLY ONE .owner, zero .lease, rejects raw original #669",OnePhysicalOwnerNoLegacyBypass);
        Check("12 real File.Move g2 then LOST ACK -> sticky owner quarantine; g2 cold recovery",
            ()=>LostAckCrashRecovery(true));
        Check("13 publisher throws BEFORE actual rename -> sticky quarantine; g1 cold survives",
            ()=>LostAckCrashRecovery(false));
        Check("14 physical corrupted final digest cannot overwrite or cold accept",
            CorruptRecordMustQuarantine);
        Check("15 real foreign orphan stage ignored, never deleted; g1->g2 survives",
            IncompleteOrphanStageIgnoredWithoutDeletion);
        Check("16 g1->g2 cold->g3 exact original frozen Light/Heavy W06 stable IDs",
            NormalThreeGenerationsColdAndIDs);
        Check("17 #773 raw sibling parent must reject and leave zero file bytes",
            RawForeignSiblingDeniedNoBytes);
        Check("18 #773 raw second Store on same original owner busy before and after Dispose",
            RawSameOwnerSecondStoreDenied);
        Check("19 R3 both OS exact canonical casing rejects alternate physical save root",
            CaseVariantRejectedAndOneCanonicalCold);
        Console.WriteLine("B0162_W25_R3_INDEPENDENT_GREEN_SOURCE_TOTAL "+
            (passed+failed)+" PASS "+passed+" FAIL "+failed);
        Console.WriteLine("B0162_W24_749_HONEST_CRASH_ORIGINAL_SOURCE_TOTAL "+
            (passed+failed)+" PASS "+passed+" FAIL "+failed);
        Console.WriteLine("B0162_W23_SINGLE_OWNER_EXACT_SOURCE_TOTAL "+
            (passed+failed)+" PASS "+passed+" FAIL "+failed);
        Console.WriteLine("B0162_W23_REAL_729_ONE_OWNER_669_ATOMIC_SAVE_GREEN_SOURCE_ONLY");
        Console.WriteLine("B0162_W23_NATIVE_UNITY_NOT_RUN_FROZEN_510_PRIVATE_WORLD_NOT_WIRED");
        return failed==0?0:1;
    }
}
