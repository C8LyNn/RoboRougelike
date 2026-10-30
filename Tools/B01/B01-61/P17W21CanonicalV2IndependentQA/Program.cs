// B01-61 W21 independent exact ORIGINAL #691 source, #685 partial-stage
// cleanup AND #688 simultaneous owner authority. This is NOT native Unity.
using System;
using System.IO;
using System.Security.Cryptography;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;

internal static class W21V2
{
    const long Run=71021;
    static int pass,fail;
    static void Test(string name,Func<string,bool> check)
    {
        string p=Path.Combine(Path.GetTempPath(),"B0161-W21-V2-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(p);
        try {if(check(p)){pass++;Console.WriteLine("PASS "+name);}
             else{fail++;Console.WriteLine("FAIL "+name);}}
        catch(Exception e){fail++;Console.WriteLine("FAIL "+name+" "+e);}
        finally{try{Directory.Delete(p,true);}catch(IOException){}}
    }
    static byte[] Save(long generation,bool light,bool heavy,long run=Run)
    {
        var parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool detach=i==0?light:heavy;
            long owner=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(FrozenPartCatalog.Id(i),owner,
                detach?0:owner,FrozenPartCatalog.Asset(i),
                FrozenPartCatalog.Socket(i),detach?PartDockState.Detached:
                PartDockState.Attached,detach,!detach,
                new Vector3d(i*3,detach?2.2:1.2,0),0,0,0,1,
                detach?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),FrozenPartCatalog.Mass(i));
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,generation,parts));
    }
    static bool SameIds(byte[] b,long gen,bool l,bool h)
    {
        return SnapshotCodec.TryDecode(b,out RunSnapshot? s)&&s!=null&&
            s.RunId==Run&&s.Generation==gen&&
            s.Parts[0].Id==2310001&&s.Parts[1].Id==2310002&&
            s.Parts[0].Consumed==l&&s.Parts[1].Consumed==h;
    }
    sealed class Gate:ICanonicalMutationGate
    {
        public bool Allowed=true;
        public bool IsQuiescent()=>Allowed;
    }
    sealed class World:ICanonicalStagedWorld
    {
        readonly RunSnapshot data;
        public bool Active,Retired,ThrowCleanup,WrongAfterActivation;
        public World(RunSnapshot data){this.data=data;}
        public RunSnapshot Capture()
        {
            if(WrongAfterActivation&&Active &&
               SnapshotCodec.TryDecode(Save(data.Generation,!data.Parts[0].Consumed,
                          !data.Parts[1].Consumed,data.RunId),
                          out RunSnapshot? other)&&other!=null)return other;
            return data;
        }
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire()
        {
            if(ThrowCleanup)throw new IOException("physical retirement fault");
            Active=false;Retired=true;
        }
    }
    sealed class Factory:ICanonicalRigFactory
    {
        public bool PartialThrow,CleanupThrows,CleanupReturnsFalse,
                    WrongAfterActivation,FailOnBegin;
        public int Stages,Cleanups;
        public World? Last,PartialWorld;
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore plan)
        {
            if(FailOnBegin)throw new IOException("failed before any tracked allocation");
            Stages++;
            return new Owned(this,s,plan);
        }
        sealed class Owned:ICanonicalStageOwnership
        {
            readonly Factory f;
            readonly RunSnapshot s;
            readonly ColdRestore cold;
            World? world;
            public Owned(Factory factory,RunSnapshot snap,ColdRestore plan)
            { f=factory;s=snap;cold=plan; }
            public ICanonicalStagedWorld PrepareInactive()
            {
                for(int i=0;i<FrozenPartCatalog.Slots;i++)
                    if(cold.ContainsConsumedId(i,s.Parts[i].Id)!=s.Parts[i].Consumed)
                        throw new InvalidDataException("frozen W06 consumed divergence");
                world=new World(s){WrongAfterActivation=f.WrongAfterActivation};
                f.Last=world;
                if(f.PartialThrow)
                {
                    f.PartialWorld=world;
                    world.Active=true; // model malicious orphan physics allocation
                    throw new IOException("partial rig allocation + PrepareInactive fault");
                }
                return world;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                f.Cleanups++;
                if(f.CleanupThrows)throw new IOException("failed owned-stage cleanup");
                if(f.CleanupReturnsFalse)return false;
                if(world!=null&&!world.Retired)
                {
                    world.Deactivate();world.Retire();
                }
                return world==null||(world.Retired&&!world.Active);
            }
        }
    }
    // Only external ICanonicalCommitPort test adapter is synthesized.
    // It performs ORIGINAL OS file reads, FileShare.None lock, fsync, same-dir
    // rename, but is NOT the separate B01-62 #669 production storage.
    sealed class Disk:ICanonicalCommitPort
    {
        readonly string path;
        public bool LoseAck;
        public Disk(string root){path=Path.Combine(root,"slot.checkpoint");}
        public bool Read(long run,out long gen,out string hash)
        {
            gen=0;hash="";
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var l=new FileStream(path+".lock",FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,FileShare.None);
                if(!File.Exists(path))return true;
                var fields=File.ReadAllText(path).Split('|');
                if(fields.Length!=3||fields[0]!=run.ToString()||
                   !long.TryParse(fields[1],out gen)||gen<=0||
                   fields[2].Length!=64)return false;
                hash=fields[2];return true;
            }
            catch{return false;}
        }
        public bool Commit(long run,long prior,string previousHash,
                           long next,string nextHash)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string staging=path+"."+Guid.NewGuid().ToString("N")+".staged";
            using var l=new FileStream(path+".lock",FileMode.OpenOrCreate,
                FileAccess.ReadWrite,FileShare.None);
            if(!File.Exists(path))
            {
                if(prior!=0||previousHash!="")return false;
            }
            else
            {
                var fields=File.ReadAllText(path).Split('|');
                if(fields.Length!=3||fields[0]!=run.ToString()||
                   fields[1]!=prior.ToString()||fields[2]!=previousHash)
                    return false;
            }
            var bytes=System.Text.Encoding.ASCII.GetBytes(
                run+"|"+next+"|"+nextHash);
            try{
                using(var f=new FileStream(staging,FileMode.CreateNew,
                    FileAccess.Write,FileShare.None))
                {f.Write(bytes,0,bytes.Length);f.Flush(true);}
                File.Move(staging,path,true);
                if(LoseAck)throw new IOException("durably committed, ACK lost");
                return true;
            }
            finally{if(File.Exists(staging))File.Delete(staging);}
        }
    }
    static (CanonicalWorldGateway api,Factory factory,Gate gate,Disk disk)
        New(string root)
    {
        var g=new Gate();var f=new Factory();var d=new Disk(root);
        return(new CanonicalWorldGateway(g,f,d),f,g,d);
    }
    static bool BeforeFaultSeed(string root,out CanonicalWorldGateway api,
                                out Factory f,out Disk disk,out World old)
    {
        var x=New(root);api=x.api;f=x.factory;disk=x.disk;
        if(api.TryRestore(Save(10,true,false),Run,10)!=CanonicalGateResult.Accepted||
           f.Last==null){old=null!;return false;}
        old=f.Last;
        return old.Active&&api.Generation==10;
    }
    static bool CleanupSuccessAfterPartialThrow(string root)
    {
        if(!BeforeFaultSeed(root,out var a,out var f,out var d,out var old))return false;
        f.PartialThrow=true;
        var result=a.TryRestore(Save(11,false,true),Run,11);
        bool safe=result==CanonicalGateResult.HostException&&
            !a.Quarantined&&a.Generation==10&&old.Active&&
            f.PartialWorld!=null&&f.PartialWorld.Retired&&
            !f.PartialWorld.Active&&f.Cleanups>=1&&
            d.Read(Run,out long g,out _)&&g==10;
        f.PartialThrow=false;
        return safe&&a.TryRestore(Save(11,false,true),Run,11)
                 ==CanonicalGateResult.Accepted&&
            f.Last!=null&&f.Last.Active&&old.Retired&&
            a.TryCapturePreview(out byte[] outBytes)&&
            SameIds(outBytes,11,false,true);
    }
    static bool CleanupThrowsMustQuarantine(string root)
    {
        if(!BeforeFaultSeed(root,out var a,out var f,out var d,out var old))return false;
        f.PartialThrow=true;f.CleanupThrows=true;
        var result=a.TryRestore(Save(11,false,true),Run,11);
        return result==CanonicalGateResult.RollbackQuarantine&&
            a.Quarantined&&a.Generation==10&&old.Active&&
            f.PartialWorld!=null&&f.PartialWorld.Active&&
            !f.PartialWorld.Retired&&f.Cleanups>=1&&
            d.Read(Run,out long g,out _)&&g==10&&
            !a.TryCapturePreview(out _ )&&
            a.TryRestore(Save(12,false,false),Run,12)
                ==CanonicalGateResult.RollbackQuarantine;
    }
    static bool CleanupFalseMustQuarantine(string root)
    {
        if(!BeforeFaultSeed(root,out var a,out var f,out var d,out _))return false;
        f.PartialThrow=true;f.CleanupReturnsFalse=true;
        return a.TryRestore(Save(11,false,true),Run,11)
                ==CanonicalGateResult.RollbackQuarantine&&
            a.Quarantined&&f.PartialWorld!=null&&
            f.PartialWorld.Active&&f.Cleanups>0&&
            !a.TryCapturePreview(out _);
    }
    static bool BeforeAllocationNoQuarantine(string root)
    {
        if(!BeforeFaultSeed(root,out var a,out var f,out var d,out var old))return false;
        f.FailOnBegin=true;
        return a.TryRestore(Save(11,false,true),Run,11)
                    ==CanonicalGateResult.HostException&&
            !a.Quarantined&&old.Active&&a.Generation==10&&
            d.Read(Run,out long g,out _ )&&g==10&&
            f.Cleanups==0; // failed BeginOwnedStage allocated nothing to clean
    }
    static bool DistinctOriginalIds(string root)
    {
        for(int mask=0;mask<4;mask++)
        {
            var x=New(Path.Combine(root,"mask"+mask));
            bool l=(mask&1)!=0,h=(mask&2)!=0;
            if(x.api.TryRestore(Save(10,l,h),Run,10)!=CanonicalGateResult.Accepted||
                !x.api.TryCapturePreview(out byte[] bytes)||
                !SameIds(bytes,10,l,h))return false;
        }
        return true;
    }
    static bool EqualForkAndForeignRunDenied(string root)
    {
        var x=New(root);
        if(x.api.TryRestore(Save(10,true,false),Run,10)
                   !=CanonicalGateResult.Accepted)return false;
        int before=x.factory.Stages;
        return x.api.TryRestore(Save(10,false,true),Run,10)
                   ==CanonicalGateResult.StaleGeneration&&
            x.api.TryRestore(Save(11,false,true,Run+1),Run,11)
                   ==CanonicalGateResult.WrongRun&&
            x.factory.Stages==before;
    }
    static bool DurableCommitLostAckQuarantine(string root)
    {
        var x=New(root);
        if(x.api.TryRestore(Save(10,true,false),Run,10)
                   !=CanonicalGateResult.Accepted)return false;
        x.disk.LoseAck=true;
        return x.api.TryRestore(Save(11,false,true),Run,11)
                   ==CanonicalGateResult.RollbackQuarantine&&
            x.api.Quarantined&&
            x.disk.Read(Run,out long g,out _ )&&g==11&&
            !x.api.TryCapturePreview(out _);
    }
    static bool TwoSimultaneousGatewayOwnersStillAccepted(string root)
    {
        var a=New(root);
        if(a.api.TryRestore(Save(10,true,false),Run,10)
                    !=CanonicalGateResult.Accepted)return false;
        var b=New(root);
        bool witnesses=b.api.TryRestore(Save(10,true,false),Run,10)
                    ==CanonicalGateResult.Accepted&&
            a.factory.Last!=null&&b.factory.Last!=null&&
            a.factory.Last.Active&&b.factory.Last.Active&&
            a.api.TryCapturePreview(out byte[] ab)&&SameIds(ab,10,true,false)&&
            b.api.TryCapturePreview(out byte[] bb)&&SameIds(bb,10,true,false);
        if(witnesses)Console.WriteLine(
            "W21_CANONICAL_V2_691_TWO_ACTIVE_GATEWAYS_SAME_SLOT_RELEASE_RED=REPRODUCED");
        return witnesses;
    }
    static bool OldWorldPreviewAfterAnotherGatewayCommit(string root)
    {
        var a=New(root);
        if(a.api.TryRestore(Save(10,true,false),Run,10)
                    !=CanonicalGateResult.Accepted)return false;
        var b=New(root);
        if(b.api.TryRestore(Save(11,false,true),Run,11)
                    !=CanonicalGateResult.Accepted)return false;
        bool witness=a.factory.Last!=null&&b.factory.Last!=null&&
            a.factory.Last.Active&&b.factory.Last.Active&&
            a.api.TryCapturePreview(out byte[] old)&&SameIds(old,10,true,false)&&
            b.api.TryCapturePreview(out byte[] current)&&SameIds(current,11,false,true)&&
            a.disk.Read(Run,out long disk,out _ )&&disk==11;
        if(witness)Console.WriteLine(
            "W21_CANONICAL_V2_691_STALE_OWNER_PREVIEW_WITH_NEW_DURABLE_EPOCH_RED=REPRODUCED");
        return witness;
    }
    public static int Main()
    {
        Test("V2_#685_ORPHAN_PARTIAL_PREPARE_CLEANED_AND_RETRY",CleanupSuccessAfterPartialThrow);
        Test("V2_#685_ORPHAN_PARTIAL_PREPARE_CLEANUP_THROWS_STICKY_QUARANTINE",CleanupThrowsMustQuarantine);
        Test("V2_#685_ORPHAN_PARTIAL_PREPARE_CLEANUP_RETURNS_FALSE_STICKY_QUARANTINE",CleanupFalseMustQuarantine);
        Test("V2_#685_FAILURE_BEFORE_ALLOCATION_NORMAL_HOST_EXCEPTION",BeforeAllocationNoQuarantine);
        Test("V2_FROZEN_556_FOUR_W06_LIGHT_HEAVY_IDS",DistinctOriginalIds);
        Test("V2_SAME_GEN_FORK_WRONG_RUN_REJECTED_PRESTAGE",EqualForkAndForeignRunDenied);
        Test("V2_REAL_OS_DURABLE_ACK_LOSS_QUARANTINES",DurableCommitLostAckQuarantine);
        Test("V2_AUTHORITY_RED_TWO_SIMULTANEOUS_GATEWAYS_SAME_SLOT",TwoSimultaneousGatewayOwnersStillAccepted);
        Test("V2_AUTHORITY_RED_OLD_PREVIEW_AFTER_DIFFERENT_OWNER_NEW_GEN",OldWorldPreviewAfterAnotherGatewayCommit);
        Console.WriteLine("B0161_W21_SELECTED_V2_691_REAL_SOURCE_EXECUTED=YES");
        Console.WriteLine("B0161_W21_SELECTED_V2_691_QA_TOTAL PASS="+pass+" FAIL="+fail);
        Console.WriteLine("SOURCE_685_PARTIAL_OWNER_CLEANUP="+(fail==0?"GREEN_REPRO_FIX":"UNVERIFIED"));
        Console.WriteLine("SOURCE_GATE="+(fail==0?"HOLD_GLOBAL_MULTI_OWNER_AUTHORITY_691":"HARNESS_FAILURE"));
        Console.WriteLine("NATIVE_G1_NOT_RUN; PRIVATE_510_ADAPTER_HOLD; G2_HOLD; NO_MERGE");
        return fail==0?0:1;
    }
}
