using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using B01.CanonicalSlotAdapter001;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

internal static class Independent
{
    private const long Run=61971;
    private static readonly Guid Id=Guid.Parse("a3333197-0079-4101-b332-431112342131");
    private static int passed,failed;
    private static void Check(string label,Func<bool> fn)
    {
        try {
            if(fn()){passed++;Console.WriteLine("PASS "+label);}
            else{failed++;Console.WriteLine("FAIL "+label);}
        } catch(Exception e){failed++;Console.WriteLine("FAIL "+label+" "+e);}
    }
    private static byte[] Data(long gen,bool light,bool heavy,long run=Run)
    {
        var parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool detached=i==0?light:heavy;
            var s=SecureRigCatalog.At(i);
            long owner=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(s.PartId,owner,detached?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                detached?PartDockState.Detached:PartDockState.Attached,
                detached,!detached,new Vector3d(s.RootX+s.PartOffsetX,detached?2.2:1.2,0),
                0,0,0,1,detached?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),s.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,gen,parts));
    }
    private static bool Valid(byte[] bytes,long gen,bool light,bool heavy)
    {
        if(!SnapshotCodec.TryDecode(bytes,out RunSnapshot? p)||p==null||
            p.RunId!=Run||p.Generation!=gen||p.Parts.Length!=2)return false;
        return p.Parts[0].Id==2310001&&p.Parts[1].Id==2310002&&
            p.Parts[0].Consumed==light&&p.Parts[1].Consumed==heavy;
    }
    private enum Mode { Good, FailedCleanupReturnsFalse, FailedCleanupThrows, CleanupFullySucceeded }
    private sealed class Gate:ICanonicalMutationGate
    {
        public bool IsQuiescent()=>true;
    }
    private sealed class World:ICanonicalStagedWorld
    {
        readonly RunSnapshot state;
        public bool Active,Retired;
        public World(RunSnapshot s){state=s;}
        public RunSnapshot Capture()=>state;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Retired=true;Active=false;}
    }
    private sealed class Factory:ICanonicalRigFactory
    {
        public readonly Mode Behavior;
        public World? Partial,Published;
        public int Begins,Prepared,Cleanups;
        public Factory(Mode mode){Behavior=mode;}
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot snap,ColdRestore cold)
        {
            Begins++;
            if(cold.Snapshot.RunId!=snap.RunId||
               cold.ContainsConsumedId(0,FrozenPartCatalog.Id(0))!=snap.Parts[0].Consumed)
                throw new InvalidOperationException("original W06 ledger mismatch");
            return new Owned(this,snap);
        }
        private sealed class Owned:ICanonicalStageOwnership
        {
            readonly Factory factory;
            readonly RunSnapshot snap;
            public Owned(Factory f,RunSnapshot s){factory=f;snap=s;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                factory.Prepared++;
                if(factory.Behavior!=Mode.Good)
                {
                    factory.Partial=new World(snap){Active=true};
                    throw new IOException("source-injected partial owned private rig prior to factory return");
                }
                factory.Published=new World(snap);
                return factory.Published;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                factory.Cleanups++;
                if(factory.Behavior==Mode.FailedCleanupReturnsFalse)return false;
                if(factory.Behavior==Mode.FailedCleanupThrows)
                    throw new IOException("owner cannot verify partly allocated rig retirement");
                factory.Partial?.Retire();
                factory.Published?.Retire();
                return true;
            }
        }
    }
    private static bool Scratch(Func<string,bool> body)
    {
        string root=Path.Combine(Path.GetTempPath(),"b0161-w21-adapter697-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{return body(Path.Combine(root,"private-owned-slot"));}
        finally{Directory.Delete(root,true);}
    }
    private static bool Open(string path,bool create,Factory f,out CanonicalSlotSession? s)
    {
        var status=CanonicalSlotSession.TryOpen(path,Run,Id,create,out s);
        return status==CanonicalSessionOpenStatus.Accepted&&s!=null&&s.Attach(new Gate(),f);
    }
    private static bool Close(CanonicalSlotSession? s,Factory f)
    {
        if(s==null)return false;
        bool oldGone=(f.Published==null||!f.Published.Active)&&
            (f.Partial==null||!f.Partial.Active);
        if(!oldGone)return false;
        bool closed=s.TryCloseAfterPhysicalRetirement(()=>true);
        s.Dispose();return closed;
    }
    private static bool SafeNormal(string path)
    {
        var f=new Factory(Mode.Good);
        if(!Open(path,true,f,out var a)||a==null)return false;
        try
        {
            if(a.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
                !a.HasWorld||!a.TryCapturePreview(out byte[] saved)||
                !Valid(saved,1,true,false)||f.Published==null||!f.Published.Active)
                return false;
            bool held=CanonicalSlotSession.TryOpen(path,Run,Id,false,out var contender)==
                    CanonicalSessionOpenStatus.Busy&&contender==null;
            f.Published.Retire();
            return held&&Close(a,f)&&
                CanonicalSlotSession.TryOpen(path,Run,Id,false,out contender)==
                CanonicalSessionOpenStatus.Accepted&&contender!=null&&Cleanup(contender);
        }
        finally{if(f.Published!=null)f.Published.Retire();Close(a,f);}
    }
    private static bool Cleanup(CanonicalSlotSession c)
    {
        bool result=c.TryCloseAfterPhysicalRetirement(()=>true);
        c.Dispose();return result;
    }
    private static bool SafeCleanupBeforeWorld(string path)
    {
        var f=new Factory(Mode.CleanupFullySucceeded);
        if(!Open(path,true,f,out var session)||session==null)return false;
        try
        {
            var result=session.TryRestore(Data(1,true,false),Run,1);
            if(result!=CanonicalGateResult.HostException||session.Quarantined||
               session.HasWorld||f.Partial==null||f.Partial.Active||
               !f.Partial.Retired||f.Cleanups!=1)return false;
            session.Dispose();
            var opened=CanonicalSlotSession.TryOpen(path,Run,Id,false,out var second);
            return opened==CanonicalSessionOpenStatus.Accepted&&second!=null&&Cleanup(second);
        }
        finally{f.Partial?.Retire();Close(session,f);}
    }
    private static bool VerifyBadCleanupTriggersPrematureUnlock(string path,Mode mode,bool doChild)
    {
        var f=new Factory(mode);
        if(!Open(path,true,f,out var session)||session==null)return false;
        try
        {
            if(session.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.RollbackQuarantine||
                !session.Quarantined||session.HasWorld||f.Cleanups!=1||
                f.Partial==null||!f.Partial.Active||f.Partial.Retired)return false;
            bool leaseBusy=CanonicalSlotSession.TryOpen(path,Run,Id,false,out var contender)==
                CanonicalSessionOpenStatus.Busy&&contender==null;
            if(!leaseBusy)return false;
            // Actual ORIGINAL #697 publicly advertised Dispose(). Because
            // gateway.HasWorld=false on bad Prepare, this RELEASES original
            // #669 FileShare.None owner lock despite still-active private rig.
            session.Dispose();
            bool unexpectedlyOpen=CanonicalSlotSession.TryOpen(path,Run,Id,false,out contender)==
                CanonicalSessionOpenStatus.Accepted&&contender!=null;
            if(!unexpectedlyOpen)return false;
            if(!Cleanup(contender!))return false;
            if(doChild)
            {
                bool overlap=ChildPublishesOtherWorld(path,f.Partial);
                if(overlap)
                    Console.WriteLine("W21_697_EXTERNAL_CHILD_PUBLISHES_WITH_ABANDONED_ACTIVE_RIG_RED=REPRODUCED");
                return overlap;
            }
            Console.WriteLine("W21_697_DISPOSE_UNLOCKS_SLOT_DURING_FAILED_PARTIAL_WORLD_CLEANUP_RED=REPRODUCED");
            return f.Partial.Active;
        }
        finally {f.Partial?.Retire();Close(session,f);}
    }
    private static bool ChildPublishesOtherWorld(string path,World orphan)
    {
        string root=Path.GetDirectoryName(path)!;
        string ready=Path.Combine(root,"ready");
        string release=Path.Combine(root,"release");
        var psi=new ProcessStartInfo("dotnet")
        {UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        psi.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        psi.ArgumentList.Add("--child");psi.ArgumentList.Add(path);
        psi.ArgumentList.Add(ready);psi.ArgumentList.Add(release);
        using var proc=Process.Start(psi);
        if(proc==null)return false;
        try
        {
            if(!SpinWait.SpinUntil(()=>File.Exists(ready)||proc.HasExited,15000))
                return false;
            if(!File.Exists(ready)||proc.HasExited||!orphan.Active||
                File.ReadAllText(ready)!="CHILD_HEAVY_GEN1_ORIGINAL_556_2310002_ACTIVE")
                return false;
            bool physical=File.Exists(Path.Combine(path,"save.envelope"));
            if(physical)
                Console.WriteLine("W21_697_PHYSICAL_DISK_ENVELOPE_G1_WITH_OLD_ACTIVE_ORPHAN_RED=REPRODUCED");
            return physical;
        }
        finally
        {
            File.WriteAllText(release,"release");
            if(!proc.WaitForExit(15000)){try{proc.Kill(true);}catch{}}
            Console.WriteLine("W21_697_CHILD_CLR_EXIT="+proc.ExitCode);
        }
    }
    private static int Child(string path,string ready,string done)
    {
        var f=new Factory(Mode.Good);
        if(!Open(path,false,f,out var s)||s==null)return 2;
        try
        {
            if(s.TryRestore(Data(1,false,true),Run,1)!=CanonicalGateResult.Accepted||
                f.Published==null||!f.Published.Active||
                !s.TryCapturePreview(out byte[] raw)||!Valid(raw,1,false,true))
                return 3;
            File.WriteAllText(ready,"CHILD_HEAVY_GEN1_ORIGINAL_556_2310002_ACTIVE");
            return SpinWait.SpinUntil(()=>File.Exists(done),15000)?0:4;
        }
        finally {f.Published?.Retire();Close(s,f);}
    }
    private static bool InvalidBeforeStage(string path)
    {
        var f=new Factory(Mode.Good);
        if(!Open(path,true,f,out var s)||s==null)return false;
        try
        {
            byte[] corrupt=Data(1,true,false);corrupt[90]^=0x1;
            return s.TryRestore(corrupt,Run,1)==CanonicalGateResult.InvalidSnapshot&&
                s.TryRestore(Data(1,true,false,Run+1),Run,1)==CanonicalGateResult.InvalidSnapshot&&
                f.Begins==0&&f.Published==null&&!s.Quarantined&&Close(s,f);
        }finally{Close(s,f);}
    }
    public static int Main(string[] args)
    {
        if(args.Length==4&&args[0]=="--child")
            return Child(args[1],args[2],args[3]);
        if(args.Length>1||(args.Length==1&&args[0]!="--release"))return 4;
        Check("ORIGINAL_697_556_W06_LIGHT_HEAVY_STABLE_IDS",()=>
            Valid(Data(1,true,false),1,true,false)&&Valid(Data(1,false,true),1,false,true));
        Check("ORIGINAL_697_HEALTHY_ACCEPTED_WORLD_KEEPS_EXCLUSIVE_OS_LEASE",()=>Scratch(SafeNormal));
        Check("ORIGINAL_691_V2_SUCCESSFUL_PARTIAL_CLEANUP_ALLOWS_SAFE_DISPOSE",()=>Scratch(SafeCleanupBeforeWorld));
        Check("ORIGINAL_697_CORRUPT_OR_FOREIGN_556_REJECTED_BEFORE_STAGE",()=>Scratch(InvalidBeforeStage));
        Check("RED_697_FAILED_CLEANUP_FALSE_ALLOWS_UNSAFE_DISPOSE_UNLOCK",()=>Scratch(path=>
            VerifyBadCleanupTriggersPrematureUnlock(path,Mode.FailedCleanupReturnsFalse,false)));
        Check("RED_697_FAILED_CLEANUP_THROW_ALLOWS_UNSAFE_DISPOSE_UNLOCK",()=>Scratch(path=>
            VerifyBadCleanupTriggersPrematureUnlock(path,Mode.FailedCleanupThrows,false)));
        Check("RED_697_REAL_CHILD_RESTORE_WHILE_PARTIAL_OLD_RIG_IS_ACTIVE",()=>Scratch(path=>
            VerifyBadCleanupTriggersPrematureUnlock(path,Mode.FailedCleanupReturnsFalse,true)));
        Console.WriteLine("B0161_W21_EXACT_697_691_669_ORIGINAL_CSHARP_SOURCES_DIRECT_EXECUTED");
        Console.WriteLine("TOTAL "+(passed+failed)+" PASS "+passed+" FAIL "+failed);
        Console.WriteLine("SOURCE_RELEASE_GATE="+(failed==0?"HOLD_ORPHAN_LEASE_LOSS_ON_DISPOSE":"UNVERIFIED"));
        Console.WriteLine("ORIGINAL_510_PRIVATE_RIG_NOT_IMPLEMENTED NATIVE_G1_NOT_RUN G2_HOLD NO_MERGE");
        return failed!=0?1:(args.Length==1?3:0);
    }
}
