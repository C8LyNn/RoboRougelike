using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Linq;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.UnitySaveBridge001;

// B01-41 independent TEST code. Direct original #691 CanonicalWorldGateway
// and exact original #647 FileCheckpointStore; no substitute Save or fake disk.
// UnityEngine/PhysX is NOT compiled/executed.
internal static class Program
{
    const long RunId=451;
    static int assertions,races,workers;
    static void Must(bool ok,string reason)
    {
        assertions++;
        if(!ok)throw new InvalidOperationException("CI41_W21_ASSERT_FAILED:"+reason);
    }
    static PersistedPart Part(int slot,bool detached)
    {
        long id=FrozenPartCatalog.Id(slot),owner=FrozenPartCatalog.Owner(slot);
        return new PersistedPart(id,owner,detached?0:owner,
            FrozenPartCatalog.Asset(slot),FrozenPartCatalog.Socket(slot),
            detached?PartDockState.Detached:PartDockState.Attached,
            detached,!detached,
            new Vector3d(slot*3,1,0),0,0,0,1,
            detached?new Vector3d(.25,0,0):new Vector3d(0,0,0),
            new Vector3d(0,0,0),FrozenPartCatalog.Mass(slot));
    }
    static byte[] Snapshot(long generation,bool light,bool heavy)
    {
        return SnapshotCodec.Encode(new RunSnapshot(RunId,generation,new[]{
            Part(0,light),Part(1,heavy)}));
    }
    static string Fingerprint(byte[] bytes)
    {
        using var hash=SHA256.Create();
        return Convert.ToHexString(hash.ComputeHash(bytes)).ToLowerInvariant();
    }
    sealed class Gate:ICanonicalMutationGate
    {
        public bool IsQuiescent()=>true;
    }
    sealed class World:ICanonicalStagedWorld
    {
        readonly RunSnapshot snapshot;
        public bool Active,Retired;
        public World(RunSnapshot snapshot){this.snapshot=snapshot;}
        public RunSnapshot Capture()=>snapshot;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Retired=true;Active=false;}
    }
    sealed class Factory:ICanonicalRigFactory
    {
        public World? Last;
        public bool PrepareThrowsAfterAlloc,CleanupReturnsFalse,CleanupThrows;
        public int Begun,Cleanups;
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot snap,ColdRestore ledger)
        {
            Begun++;
            return new Ownership(this,snap,ledger);
        }
        sealed class Ownership:ICanonicalStageOwnership
        {
            readonly Factory f;
            readonly RunSnapshot snap;
            readonly ColdRestore ledger;
            World? owned;
            public Ownership(Factory f,RunSnapshot s,ColdRestore l)
            {this.f=f;snap=s;ledger=l;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                for(int i=0;i<FrozenPartCatalog.Slots;i++)
                {
                    var p=snap.Parts[i];
                    if(ledger.ContainsConsumedId(i,p.Id)!=p.Consumed)
                        throw new InvalidDataException("Original W06 immutable consumed ledger mismatch");
                }
                owned=new World(snap);
                f.Last=owned;
                if(f.PrepareThrowsAfterAlloc)
                {
                    owned.Active=true;
                    throw new IOException("PREPARE_PARTLY_BUILT_PRIVATE_WORLD_THEN_THREW");
                }
                return owned;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                f.Cleanups++;
                if(f.CleanupThrows)throw new IOException("CLEANUP_STAGED_WORLD_LOST");
                if(f.CleanupReturnsFalse)return false;
                if(owned!=null)
                {
                    owned.Deactivate();owned.Retire();
                    return owned.Retired&&!owned.Active;
                }
                return true;
            }
        }
    }
    sealed class OriginalPhysicalPort:ICanonicalCommitPort
    {
        readonly FileCheckpointStore actual;
        public bool ThrowAfterDurable;
        public OriginalPhysicalPort(string file){actual=new FileCheckpointStore(file);}
        public bool Read(long run,out long generation,out string hash)=>
            actual.Read(run,out generation,out hash);
        public bool Commit(long run,long prior,string priorHash,long next,string hash)
        {
            bool wrote=actual.Commit(run,prior,priorHash,next,hash);
            if(wrote&&ThrowAfterDurable)
                throw new IOException("ORIGINAL_PHYSICAL_COMMIT_SUCCEEDED_ACK_LOST");
            return wrote;
        }
    }
    static (CanonicalWorldGateway api,Factory factory,OriginalPhysicalPort port,World old,
        string path) Seed(string root,string label)
    {
        string path=Path.Combine(root,label,"checkpoint");
        var port=new OriginalPhysicalPort(path);
        var factory=new Factory();
        var api=new CanonicalWorldGateway(new Gate(),factory,port);
        byte[] old=Snapshot(10,true,false);
        Must(port.Commit(RunId,0,"",10,Fingerprint(old)),
            "seed original physically durable g10");
        Must(api.TryRestore(old,RunId,1)==CanonicalGateResult.Accepted,
            "cold replay original g10 accepted");
        Must(factory.Last!=null&&factory.Last.Active&&!factory.Last.Retired,
            "original old Light consumed world active");
        Must(ColdRestore.TryStage(old,out ColdRestore? cold)&&cold!=null&&
            cold.ContainsConsumedId(0,FrozenPartCatalog.Id(0))&&
            !cold.ContainsConsumedId(1,FrozenPartCatalog.Id(1)),
            "original frozen #556 and W06 exact Light/Heavy stable IDs");
        return (api,factory,port,factory.Last!,path);
    }
    static void CleanupScenarios(string root)
    {
        // Distinct from producer: direct real ORIGINAL physical R4 checkpoint,
        // not the owner's test Disk that truncates the file.
        {
            var (api,f,p,old,path)=Seed(root,"cleanup-ok");
            f.PrepareThrowsAfterAlloc=true;
            var result=api.TryRestore(Snapshot(11,false,true),RunId,1);
            Must(result==CanonicalGateResult.HostException&&!api.Quarantined,
                "V2 cleanup successful precommit exception not false quarantine");
            Must(f.Last!=null&&!ReferenceEquals(f.Last,old)&&f.Last.Retired&&
                !f.Last.Active&&f.Cleanups==1,
                "V2 owned stage cleans orphan not returned by Prepare");
            Must(old.Active&&api.Generation==10&&p.Read(RunId,out long g,out _)
                &&g==10,"original durable g10 and old Light unchanged");
            Must(api.TryCapturePreview(out byte[] preview)&&
                Fingerprint(preview)==Fingerprint(Snapshot(10,true,false)),
                "original owner preview remains coherent after successful cleanup");
            Must(File.Exists(path)&&!Directory.GetFiles(Path.GetDirectoryName(path)!,"*.tmp").Any(),
                "real physical original sidecar, no orphan tmp files");
        }
        {
            var (api,f,p,old,_)=Seed(root,"cleanup-false");
            f.PrepareThrowsAfterAlloc=true;f.CleanupReturnsFalse=true;
            Must(api.TryRestore(Snapshot(11,false,true),RunId,1)
                 ==CanonicalGateResult.RollbackQuarantine&&api.Quarantined,
                 "false private-world cleanup permanently quarantines SAME CALL");
            Must(f.Cleanups==1&&f.Last!=null&&f.Last.Active&&old.Active,
                "unsafely unretired staged world remains detectable, not accepted");
            Must(p.Read(RunId,out long g,out _)&&g==10&&api.Generation==10,
                "no fake advanced original durable checkpoint");
            Must(!api.TryCapturePreview(out _)&&
                 api.TryRestore(Snapshot(12,false,true),RunId,1)
                    ==CanonicalGateResult.RollbackQuarantine,
                "no Save/Load retries after failed original owner cleanup");
        }
        {
            var (api,f,p,old,_)=Seed(root,"cleanup-exception");
            f.PrepareThrowsAfterAlloc=true;f.CleanupThrows=true;
            Must(api.TryRestore(Snapshot(11,false,true),RunId,1)
                    ==CanonicalGateResult.RollbackQuarantine&&api.Quarantined,
                 "throwing ICanonicalStageOwnership cleanup permanently quarantines");
            Must(f.Cleanups==1&&old.Active&&
                 p.Read(RunId,out long g,out _)&&g==10,
                 "actual disk and old world preserved for containment");
        }
        {
            var (api,f,p,old,_)=Seed(root,"ack-lost");
            p.ThrowAfterDurable=true;
            Must(api.TryRestore(Snapshot(11,false,true),RunId,1)
                    ==CanonicalGateResult.RollbackQuarantine&&api.Quarantined,
                 "real original R4 checkpoint durable write then ACK exception quarantine");
            Must(p.Read(RunId,out long g,out string hash)&&g==11&&
                 hash==Fingerprint(Snapshot(11,false,true)),
                 "original physical disk carries new Heavy stable IDs");
            Must(old.Active&&api.Generation==10&&!api.TryCapturePreview(out _),
                 "old Light held only as quarantined physical containment");
            Must(f.Last!=null&&f.Last.Retired&&!f.Last.Active,
                 "new Heavy stage retired after postcommit exception");
            var fresh=new CanonicalWorldGateway(new Gate(),new Factory(),
                new OriginalPhysicalPort(Path.Combine(root,"ack-lost","checkpoint")));
            Must(fresh.TryRestore(Snapshot(10,true,false),RunId,1)
                    ==CanonicalGateResult.StaleGeneration,
                 "fresh original physical disk rejects older consumed-Light g10");
            Must(fresh.TryRestore(Snapshot(11,false,true),RunId,1)
                    ==CanonicalGateResult.Accepted,
                 "fresh canonical cold restore accepts matching g11 Heavy IDs");
        }
    }

    static Process Spawn(string path,string start,string ready,string result,bool heavy)
    {
        var info=new ProcessStartInfo("dotnet")
        {UseShellExecute=false,CreateNoWindow=true};
        foreach(string arg in new[]{typeof(Program).Assembly.Location,"worker",
            path,start,ready,result,heavy?"1":"0"})
            info.ArgumentList.Add(arg);
        Process? p=Process.Start(info);
        if(p==null)throw new IOException("CHILD_CLR_WORKER_NOT_CREATED");
        workers++;return p;
    }
    static int Worker(string[] args)
    {
        var port=new OriginalPhysicalPort(args[1]);
        CanonicalWorldGateway? api=null;
        // OS FileShare.None source Read may reject a simultaneously held lock:
        // only begin the race after both cold baseline actual-source stages succeed.
        for(int i=0;i<100;i++)
        {
            var tryApi=new CanonicalWorldGateway(new Gate(),new Factory(),port);
            if(tryApi.TryRestore(Snapshot(10,true,false),RunId,1)
                ==CanonicalGateResult.Accepted){api=tryApi;break;}
            Thread.Sleep(10);
        }
        if(api==null){File.WriteAllText(args[4],"COLD_REPLAY_NOT_READY");return 4;}
        File.WriteAllText(args[3],"READY");
        var timer=Stopwatch.StartNew();
        while(!File.Exists(args[2]))
        {
            if(timer.Elapsed>TimeSpan.FromSeconds(25))
            {File.WriteAllText(args[4],"START_TIMEOUT");return 5;}
            Thread.Sleep(3);
        }
        bool heavy=args[5]=="1";
        var verdict=api.TryRestore(Snapshot(11,!heavy,heavy),RunId,1);
        File.WriteAllText(args[4],verdict.ToString());
        return 0;
    }
    static void Race(string root,int index)
    {
        string dir=Path.Combine(root,"race-"+index.ToString("D2"));
        Directory.CreateDirectory(dir);
        string checkpoint=Path.Combine(dir,"checkpoint");
        var disk=new OriginalPhysicalPort(checkpoint);
        Must(disk.Commit(RunId,0,"",10,Fingerprint(Snapshot(10,true,false))),
            "real original physical disk race baseline g10");
        string start=Path.Combine(dir,"start");
        string ar=Path.Combine(dir,"a.ready"),br=Path.Combine(dir,"b.ready");
        string ao=Path.Combine(dir,"a.result"),bo=Path.Combine(dir,"b.result");
        using var a=Spawn(checkpoint,start,ar,ao,true);
        using var b=Spawn(checkpoint,start,br,bo,false);
        var sw=Stopwatch.StartNew();
        while(!File.Exists(ar)||!File.Exists(br))
        {
            if(sw.Elapsed>TimeSpan.FromSeconds(25))
                throw new TimeoutException("TWO_REAL_PROCESSES_BASELINE_NOT_READY");
            Thread.Sleep(5);
        }
        File.WriteAllText(start,"GO");
        Must(a.WaitForExit(25000)&&b.WaitForExit(25000),
            "both genuinely independent CLR workers exited");
        Must(a.ExitCode==0&&b.ExitCode==0,"both real crossprocess original gateway trials executed");
        string ra=File.ReadAllText(ao),rb=File.ReadAllText(bo);
        Must((ra=="Accepted")!=(rb=="Accepted"),
            "EXACT_ONE_WINNER_V2_GATEWAY_USING_ORIGINAL_R4_PHYSICAL_CAS");
        string loser=ra=="Accepted"?rb:ra;
        Must(loser=="StaleGeneration"||loser=="RollbackQuarantine",
            "loser cannot publish or erase winning real checkpoint");
        bool winnerHeavy=ra=="Accepted";
        byte[] winner=Snapshot(11,!winnerHeavy,winnerHeavy);
        Must(disk.Read(RunId,out long gen,out string hash)&&gen==11&&
            hash==Fingerprint(winner),"original g11 sidecar matches winning private #556 stable IDs");
        Must(!disk.Commit(RunId,10,Fingerprint(Snapshot(10,true,false)),
                  11,Fingerprint(Snapshot(11,winnerHeavy,!winnerHeavy))),
            "loser stale version cannot overwrite real winner");
        Must(Directory.GetFiles(dir,"*.tmp").Length==0,
            "no abandoned original atomic checkpoint temporary files");
        races++;
    }
    static int Main(string[] args)
    {
        if(args.Length>0&&args[0]=="worker")return Worker(args);
        string root=Path.Combine(Path.GetTempPath(),
            "ci41-w21-canonical-gateway-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            CleanupScenarios(root);
            for(int i=0;i<8;i++)Race(root,i);
            Must(races==8&&workers==16,"8 real simultaneous competing process pairs / 16 child CLR");
            Console.WriteLine("CI41_W21_ORIGINAL_691_REAL_R4_DISK_CAS_PASS races="+races+
                " child_processes="+workers+" asserts="+assertions);
            Console.WriteLine("CI41_W21_685_PARTIAL_PREPARE_CLEANUP_GREEN_AND_FAILED_CLEANUP_STICKY_QUARANTINE_GREEN");
            Console.WriteLine("CI41_W21_REAL_COMMIT_ACK_LOST_QUARANTINE_AND_FROZEN_556_IDS_GREEN");
            Console.WriteLine("CI41_W21_MULTI_GATEWAY_OWNER_LEASE_EXTERNAL_NOT_CERTIFIED");
            Console.WriteLine("CI41_W21_NATIVE_UNITY_PHYSX_NOT_RUN_CANONICAL510_PRIVATE_ADAPTER_HOLD_G2_HOLD");
            return 0;
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine("CI41_W21_SOURCE_QA_UNEXPECTED_FAIL "+ex);
            return 2;
        }
        finally
        {
            if(Directory.Exists(root))Directory.Delete(root,true);
        }
    }
}
