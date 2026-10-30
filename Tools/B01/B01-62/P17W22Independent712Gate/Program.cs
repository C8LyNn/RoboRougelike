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
#if OLD
using B01.CanonicalSlotAdapter001;
using Session=B01.CanonicalSlotAdapter001.CanonicalSlotSession;
#else
using B01.CanonicalSlotAdapter002;
using Session=B01.CanonicalSlotAdapter002.CanonicalSlotSessionR2;
#endif

// B01-62 W22 independent direct-original source security differential.
// V1 #697 is EXPECTED RED, corrected R2 #712 is EXPECTED GREEN.
// Original production #691/#669/#556/W06 code directly compiled, no copied implementation.
internal static class IndependentOriginalGate
{
    private const long Run=62262;
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
        try{return test(Path.Combine(root,"owned"));}
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
        bool actual=outText.Contains("W22_CHILD_ACQUIRED");
        return process.ExitCode==(actual?0:3)&&actual==shouldGet&&
            !outText.Contains("W22_CHILD_ERROR");
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
#if OLD
                // Original #697 source P0: Dispose relies only on HasWorld=false.
                // A separate process can acquire the slot with ACTIVE orphan.
                bool expectedAcquire=true;
#else
                // Fixed #712 source must keep FileShare.None for unresolved stages.
                bool expectedAcquire=false;
#endif
                bool ok=useChild?TrySecondClr(dir,expectedAcquire):
                    CanOpen(dir,expectedAcquire);
                if(!ok)return false;
#if OLD
                Console.WriteLine("W22_ORIGINAL_V1_697_ORPHAN_LEASE_P0_RELEASE_RED_REPRODUCED");
                return f.Partial.Active;
#else
                Console.WriteLine("W22_FIXED_R2_712_ORPHAN_LEASE_DENIED_GREEN");
                // Failed explicit cleanup proof cannot close/release the OS slot.
                if(s.TryCloseAfterPhysicalRetirement(()=>false))return false;
                if(!TrySecondClr(dir,false))return false;
                f.Partial.Retire();
                if(!CloseTrusted(s,f))return false;
                return TrySecondClr(dir,true);
#endif
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
                s.Dispose();
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
    private static int Child(string dir)
    {
        var st=SingleFileSaveSlot.TryOpenExisting(dir,Run,Slot,out var store);
        store?.Dispose();
        if(st==SlotStatus.Accepted){Console.WriteLine("W22_CHILD_ACQUIRED");return 0;}
        if(st==SlotStatus.Busy){Console.WriteLine("W22_CHILD_DENIED");return 3;}
        Console.WriteLine("W22_CHILD_ERROR "+st);return 4;
    }
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
        Console.WriteLine("B0162_W22_712_INDEPENDENT_ORIGINAL_SOURCE_TOTAL "+
            (passed+failed)+" PASS "+passed+" FAIL "+failed);
#if OLD
        Console.WriteLine("B0162_W22_V1_697_EXPECTED_PRODUCT_SECURITY_RED=REPRODUCED");
#else
        Console.WriteLine("B0162_W22_R2_712_EXPECTED_PRODUCT_SECURITY_GREEN=VERIFIED_SOURCE");
#endif
        Console.WriteLine("B0162_W22_NATIVE_UNITY_NOT_RUN_FROZEN_510_PRIVATE_WORLD_NOT_WIRED");
        return failed==0?0:1;
    }
}
