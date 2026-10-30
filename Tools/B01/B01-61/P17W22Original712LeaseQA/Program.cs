using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using B01.CanonicalSlotAdapter002;
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
    private static bool Open(string path,bool create,Factory f,out CanonicalSlotSessionR2? s)
    {
        var status=CanonicalSlotSessionR2.TryOpen(path,Run,Id,create,out s);
        return status==CanonicalSessionOpenStatus.Accepted&&s!=null&&s.Attach(new Gate(),f);
    }
    private static bool Close(CanonicalSlotSessionR2? s,Factory f)
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
            bool held=CanonicalSlotSessionR2.TryOpen(path,Run,Id,false,out var contender)==
                    CanonicalSessionOpenStatus.Busy&&contender==null;
            f.Published.Retire();
            return held&&Close(a,f)&&
                CanonicalSlotSessionR2.TryOpen(path,Run,Id,false,out contender)==
                CanonicalSessionOpenStatus.Accepted&&contender!=null&&Cleanup(contender);
        }
        finally{if(f.Published!=null)f.Published.Retire();Close(a,f);}
    }
    private static bool Cleanup(CanonicalSlotSessionR2 c)
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
            var opened=CanonicalSlotSessionR2.TryOpen(path,Run,Id,false,out var second);
            return opened==CanonicalSessionOpenStatus.Accepted&&second!=null&&Cleanup(second);
        }
        finally{f.Partial?.Retire();Close(session,f);}
    }
    private static bool VerifyBadCleanupRetainsLeaseUntilVerifiedRetirement(string path,Mode mode,bool doChild)
    {
        var f=new Factory(mode);
        if(!Open(path,true,f,out var session)||session==null)return false;
        try
        {
            if(session.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.RollbackQuarantine||
                !session.Quarantined||session.HasWorld||f.Cleanups!=1||
                f.Partial==null||!f.Partial.Active||f.Partial.Retired)return false;
            if(!Probe(path,3))return false;
            // NEW selected R2 source (never alter #697 V1) MUST retain the original
            // OS FileShare.None lease after failed unpublished-world cleanup.
            session.Dispose();
            if(!session.OwnsSlot||
               CanonicalSlotSessionR2.TryOpen(path,Run,Id,false,out var contender)!=
                   CanonicalSessionOpenStatus.Busy||contender!=null||!Probe(path,3))
                return false;
            if(session.TryRestore(Data(2,false,true),Run,2)!=CanonicalGateResult.RollbackQuarantine||
               session.TryCapturePreview(out _))return false;
            // Negative or exception physical-retirement proof MUST NOT authorize release.
            if(session.TryCloseAfterPhysicalRetirement(()=>false) ||
               session.TryCloseAfterPhysicalRetirement(()=>throw new IOException("destroyed roots not proven"))||
               !session.OwnsSlot||!Probe(path,3))return false;
            // Real published save must stay ABSENT while unsafe orphan exists.
            if(File.Exists(Path.Combine(path,"save.envelope")))return false;
            if(!f.Partial.Active||f.Partial.Retired)return false;
            Console.WriteLine("W22_712_ORPHAN_PARTIAL_OWNER_DISPOSE_AND_CHILD_DENIED_GREEN=PASS");
            f.Partial.Retire();
            if(!f.Partial.Retired||f.Partial.Active||
               !session.TryCloseAfterPhysicalRetirement(()=>
                   f.Partial.Retired&&!f.Partial.Active)||session.OwnsSlot)return false;
            session.Dispose();
            if(doChild)
            {
                bool good=ChildPublishesAfterRealRetirement(path,f.Partial);
                if(good)Console.WriteLine("W22_712_CHILD_CAN_PUBLISH_ONLY_AFTER_VERIFIED_RETIREMENT_GREEN=PASS");
                return good;
            }
            return Probe(path,0);
        }
        finally {f.Partial?.Retire();Close(session,f);}
    }
    private static bool ChildPublishesAfterRealRetirement(string path,World retired)
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
            if(!File.Exists(ready)||proc.HasExited||retired.Active||!retired.Retired||
                File.ReadAllText(ready)!="CHILD_HEAVY_GEN1_ORIGINAL_556_2310002_ACTIVE")
                return false;
            bool physical=File.Exists(Path.Combine(path,"save.envelope"));
            if(physical)
                Console.WriteLine("W22_712_PHYSICAL_G1_ONLY_AFTER_OLD_PARTIAL_OWNER_RETIRED_GREEN=PASS");
            return physical;
        }
        finally
        {
            File.WriteAllText(release,"release");
            if(!proc.WaitForExit(15000)){try{proc.Kill(true);}catch{}}
            Console.WriteLine("W22_712_CHILD_CLR_EXIT="+proc.ExitCode);
        }
    }
    private static bool Probe(string path,int expected)
    {
        var psi=new ProcessStartInfo("dotnet")
        {UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        psi.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        psi.ArgumentList.Add("--probe");psi.ArgumentList.Add(path);
        using var child=Process.Start(psi);
        if(child==null)return false;
        string stdout=child.StandardOutput.ReadToEnd();
        string stderr=child.StandardError.ReadToEnd();
        if(!child.WaitForExit(10000)){try{child.Kill(true);}catch{}return false;}
        if(child.ExitCode!=expected)return false;
        return stdout.Contains(expected==3?"W22_CHILD_SLOT_HELD_DENY":"W22_CHILD_SLOT_RELEASED_ACCEPT");
    }
    private static int ChildProbe(string path)
    {
        var result=CanonicalSlotSessionR2.TryOpen(path,Run,Id,false,out var s);
        if(result==CanonicalSessionOpenStatus.Busy)
        {Console.WriteLine("W22_CHILD_SLOT_HELD_DENY");return 3;}
        if(result==CanonicalSessionOpenStatus.Accepted&&s!=null)
        {
            bool closed=s.TryCloseAfterPhysicalRetirement(()=>true);
            s.Dispose();
            Console.WriteLine("W22_CHILD_SLOT_RELEASED_ACCEPT");
            return closed?0:5;
        }
        Console.WriteLine("W22_CHILD_UNEXPECTED_"+result);
        return 4;
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
    private static bool TwoGenerationColdRestart(string path)
    {
        var f=new Factory(Mode.Good);
        if(!Open(path,true,f,out var a)||a==null)return false;
        try
        {
            if(a.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
               f.Published==null||!f.Published.Active||
               a.TryRestore(Data(2,false,true),Run,2)!=CanonicalGateResult.Accepted||
               !a.TryCapturePreview(out byte[] first)||!Valid(first,2,false,true))
                return false;
            if(!a.Read(Run,out long epoch,out string digest)||epoch!=2||
               digest!=CanonicalSlotSessionR2.SourceHash(Data(2,false,true)))return false;
            // Old after genuine stage world g1 must retire after g2 published.
            if(f.Published==null||!f.Published.Active)return false;
            f.Published.Retire();
            if(!Close(a,f))return false;
            var next=new Factory(Mode.Good);
            if(!Open(path,false,next,out var b)||b==null)return false;
            try
            {
                if(b.TryRestore(Data(2,true,false),Run,2)!=CanonicalGateResult.StaleGeneration||
                   next.Begins!=0||
                   b.TryRestore(Data(2,false,true),Run,2)!=CanonicalGateResult.Accepted||
                   !b.TryCapturePreview(out byte[] same)||!Valid(same,2,false,true))
                    return false;
                next.Published?.Retire();
                return Close(b,next);
            }
            finally{next.Published?.Retire();Close(b,next);}
        }
        finally{f.Published?.Retire();Close(a,f);}
    }
    public static int Main(string[] args)
    {
        if(args.Length==4&&args[0]=="--child")
            return Child(args[1],args[2],args[3]);
        if(args.Length==2&&args[0]=="--probe")
            return ChildProbe(args[1]);
        if(args.Length>1||(args.Length==1&&args[0]!="--release"))return 4;
        for(int mask=0;mask<4;mask++)
        {
            int actual=mask;
            Check("ORIGINAL_712_556_W06_ID_MASK_"+actual,()=>Valid(
                Data(1,(actual&1)!=0,(actual&2)!=0),1,(actual&1)!=0,(actual&2)!=0));
        }
        Check("ORIGINAL_712_GOOD_WORLD_HOLDS_LEASE_UNTIL_RETIRE",()=>Scratch(SafeNormal));
        Check("ORIGINAL_712_SUCCESSFUL_PARTIAL_CLEANUP_PERMITS_CLOSE",()=>Scratch(SafeCleanupBeforeWorld));
        Check("ORIGINAL_712_CORRUPT_AND_WRONG_RUN_PRESTAGE_DENIAL",()=>Scratch(InvalidBeforeStage));
        Check("ORIGINAL_712_GEN1_TO_GEN2_EXACT_IDS_COLD_FORK_DENIAL",()=>Scratch(TwoGenerationColdRestart));
        Check("GREEN_712_FALSE_CLEANUP_RETINS_REAL_OS_LEASE",()=>Scratch(path=>
            VerifyBadCleanupRetainsLeaseUntilVerifiedRetirement(path,Mode.FailedCleanupReturnsFalse,false)));
        Check("GREEN_712_THROW_CLEANUP_RETINS_REAL_OS_LEASE",()=>Scratch(path=>
            VerifyBadCleanupRetainsLeaseUntilVerifiedRetirement(path,Mode.FailedCleanupThrows,false)));
        Check("GREEN_712_REAL_CHILD_DENIED_THEN_RESTORE_ONLY_AFTER_ROOT_RETIRE",()=>Scratch(path=>
            VerifyBadCleanupRetainsLeaseUntilVerifiedRetirement(path,Mode.FailedCleanupReturnsFalse,true)));
        Console.WriteLine("B0161_W22_ORIGINAL_712_691_669_DIRECT_SOURCE_CSHARP9");
        Console.WriteLine("TOTAL "+(passed+failed)+" PASS "+passed+" FAIL "+failed);
        Console.WriteLine("SOURCE_RELEASE_GATE="+(failed==0?"GREEN_TESTED_707_SCOPE_ONLY":"HOLD_TEST_FAILURE"));
        Console.WriteLine("SOURCE_G0_REQUIRES_FORMAL_G0 NATIVE_UNITY_G1_NOT_RUN GAME_510_PRIVATE_RIG_NOT_IMPLEMENTED");
        return failed!=0?1:(args.Length==1?3:0);
    }
}
