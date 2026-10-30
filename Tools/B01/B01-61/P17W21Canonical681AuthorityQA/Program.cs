using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;

internal static class Independent
{
    private const long Run = 61021;
    private static int pass, fail;
    private static void Check(string label,Func<bool> action)
    {
        try {
            if(action()){pass++;Console.WriteLine("PASS "+label);}
            else{fail++;Console.WriteLine("FAIL "+label);}
        } catch(Exception ex){fail++;Console.WriteLine("FAIL "+label+" "+ex);}
    }
    private static byte[] Data(long gen,bool light,bool heavy,long run=Run)
    {
        var parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool detached=i==0?light:heavy;
            long owner=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(FrozenPartCatalog.Id(i),owner,detached?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                detached?PartDockState.Detached:PartDockState.Attached,
                detached,!detached,new Vector3d(i*3,detached?2.2:1.2,0),
                0,0,0,1,detached?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),FrozenPartCatalog.Mass(i));
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,gen,parts));
    }
    private static bool HasIds(byte[] bytes,long gen,bool light,bool heavy)
    {
        if(!SnapshotCodec.TryDecode(bytes,out RunSnapshot? x)||x==null)return false;
        return x.RunId==Run&&x.Generation==gen&&x.Parts.Length==2&&
            x.Parts[0].Id==2310001&&x.Parts[1].Id==2310002&&
            x.Parts[0].Consumed==light&&x.Parts[1].Consumed==heavy;
    }
    private static string Hash(byte[] bytes)
    {
        using(var sha=SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
    }
    private sealed class Gate:ICanonicalMutationGate
    {
        public bool Allowed=true;
        public Action? Callback;
        public bool IsQuiescent(){Callback?.Invoke();return Allowed;}
    }
    private sealed class World:ICanonicalStagedWorld
    {
        readonly RunSnapshot initial;
        public bool Active,Retired,FailDeactivate,FailRetire,AlterAfterActivate;
        public World(RunSnapshot state){initial=state;}
        public RunSnapshot Capture()
        {
            if(AlterAfterActivate&&Active)
            {
                byte[] changed=Data(initial.Generation,!initial.Parts[0].Consumed,
                    !initial.Parts[1].Consumed,initial.RunId);
                if(SnapshotCodec.TryDecode(changed,out RunSnapshot? s)&&s!=null)return s;
            }
            return initial;
        }
        public void Activate(){Active=true;}
        public void Deactivate()
        {
            if(FailDeactivate)throw new IOException("old world refuses deactivation");
            Active=false;
        }
        public void Retire()
        {
            if(FailRetire)throw new IOException("new world refuses retirement");
            Retired=true;Active=false;
        }
    }
    private sealed class Factory:ICanonicalRigFactory
    {
        public World? Last,First;
        public int Stages;
        public bool AlterAfterActivate,FailRetire;
        public ICanonicalStagedWorld BuildInactive(RunSnapshot s,ColdRestore ledger)
        {
            for(int i=0;i<FrozenPartCatalog.Slots;i++)
                if(ledger.ContainsConsumedId(i,s.Parts[i].Id)!=s.Parts[i].Consumed)
                    throw new InvalidDataException("original W06 ledger split");
            Stages++;
            Last=new World(s){AlterAfterActivate=AlterAfterActivate,FailRetire=FailRetire};
            if(First==null)First=Last;
            return Last;
        }
    }
    // A REAL on-disk, per-path owner-agnostic CAS implementation for the exact public
    // ICanonicalCommitPort surface. This is a test port, NOT a claimed producer
    // B01-62 durable snapshot protocol or Unity native filesystem.
    private sealed class OsCas:ICanonicalCommitPort
    {
        readonly string path;
        public bool ThrowAfterWrite;
        public OsCas(string path){this.path=Path.GetFullPath(path);}
        private bool ReadLocked(long run,out long gen,out string hash)
        {
            gen=0;hash="";
            if(!File.Exists(path))return true;
            string[] a=File.ReadAllText(path).Split('|');
            if(a.Length!=3||a[0]!=run.ToString(System.Globalization.CultureInfo.InvariantCulture)||
               !long.TryParse(a[1],out gen)||gen<=0||a[2].Length!=64)return false;
            hash=a[2];return true;
        }
        public bool Read(long run,out long generation,out string hash)
        {
            generation=0;hash="";
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using(var fd=new FileStream(path+".lock",FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,FileShare.None))
                    return ReadLocked(run,out generation,out hash);
            } catch {return false;}
        }
        public bool Commit(long run,long prev,string prevHash,long next,string nextHash)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try {
                using(var fd=new FileStream(path+".lock",FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,FileShare.None))
                {
                    if(!ReadLocked(run,out long g,out string h)||
                       g!=prev||h!=prevHash||next<=prev||nextHash.Length!=64)return false;
                    byte[] payload=Encoding.ASCII.GetBytes(run+"|"+next+"|"+nextHash);
                    using(var f=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,
                        FileShare.None)){f.Write(payload,0,payload.Length);f.Flush(true);}
                    File.Move(temp,path,true);
                    if(ThrowAfterWrite)throw new IOException("physically committed, acknowledgement lost");
                    return true;
                }
            } finally {if(File.Exists(temp))File.Delete(temp);}
        }
    }
    private static bool Scratch(Func<string,bool> action)
    {
        string root=Path.Combine(Path.GetTempPath(),"b0161_w21_canonical681_"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{return action(root);}
        finally {Directory.Delete(root,true);}
    }
    private static (CanonicalWorldGateway api,Factory factory,Gate gate,OsCas disk) Make(string root)
    {
        var gate=new Gate();var factory=new Factory();
        var disk=new OsCas(Path.Combine(root,"same-slot.revision"));
        return (new CanonicalWorldGateway(gate,factory,disk),factory,gate,disk);
    }
    private static bool FourOriginalIds(string root)
    {
        for(int i=0;i<4;i++)
        {
            var x=Make(Path.Combine(root,"mask"+i));
            bool light=(i&1)!=0,heavy=(i&2)!=0;
            if(x.api.TryRestore(Data(10,light,heavy),Run,10)!=CanonicalGateResult.Accepted||
               x.factory.Last==null||!x.factory.Last.Active||
               !x.api.TryCapturePreview(out byte[] bytes)||
               !HasIds(bytes,10,light,heavy)||
               !x.disk.Read(Run,out long gen,out string h)||gen!=10||
               h!=Hash(Data(10,light,heavy)))return false;
        }
        return true;
    }
    private static bool AdvanceRollback(string root)
    {
        var x=Make(root);
        if(x.api.TryRestore(Data(10,true,false),Run,10)!=CanonicalGateResult.Accepted)return false;
        World old=x.factory.Last!;
        if(x.api.TryRestore(Data(11,false,true),Run,11)!=CanonicalGateResult.Accepted||
           !old.Retired||old.Active||x.factory.Last==null||!x.factory.Last.Active)return false;
        return x.api.TryCapturePreview(out byte[] preview)&&
           HasIds(preview,11,false,true)&&x.api.Generation==11;
    }
    private static bool ForkDeniedBeforePrepare(string root)
    {
        var x=Make(root);
        if(x.api.TryRestore(Data(10,true,false),Run,10)!=CanonicalGateResult.Accepted)return false;
        var y=Make(root);
        return y.api.TryRestore(Data(10,false,true),Run,1)==CanonicalGateResult.StaleGeneration&&
            y.factory.Stages==0&&x.factory.Last!.Active;
    }
    private static bool InvalidRunAndBinary(string root)
    {
        var x=Make(root);
        byte[] broken=Data(10,true,false);broken[70]^=0x20;
        return x.api.TryRestore(Data(10,true,false,Run+1),Run,10)==CanonicalGateResult.WrongRun&&
           x.api.TryRestore(broken,Run,10)==CanonicalGateResult.InvalidSnapshot&&x.factory.Stages==0;
    }
    private static bool MainThreadAndReentry(string root)
    {
        var x=Make(root);
        bool foreign=Task.Run(()=>x.api.TryRestore(Data(10,true,false),Run,10)==CanonicalGateResult.HostBusy).GetAwaiter().GetResult();
        CanonicalGateResult reenter=CanonicalGateResult.Accepted;
        x.gate.Callback=()=>{reenter=x.api.TryRestore(Data(10,true,false),Run,10);};
        bool ok=x.api.TryRestore(Data(10,true,false),Run,10)==CanonicalGateResult.Accepted;
        return foreign&&ok&&reenter==CanonicalGateResult.HostBusy&&x.factory.Stages==1;
    }
    private static bool MissingAckSticky(string root)
    {
        var x=Make(root);
        if(x.api.TryRestore(Data(10,true,false),Run,10)!=CanonicalGateResult.Accepted)return false;
        x.disk.ThrowAfterWrite=true;
        var verdict=x.api.TryRestore(Data(11,false,true),Run,11);
        return verdict==CanonicalGateResult.RollbackQuarantine&&x.api.Quarantined&&
            x.disk.Read(Run,out long g,out _ )&&g==11&&
            x.api.Generation==10&&!x.api.TryCapturePreview(out _ )&&
            x.api.TryRestore(Data(12,true,true),Run,12)==CanonicalGateResult.RollbackQuarantine;
    }
    private static bool BadActivateRollback(string root)
    {
        var x=Make(root);
        if(x.api.TryRestore(Data(10,true,false),Run,10)!=CanonicalGateResult.Accepted)return false;
        World old=x.factory.Last!;
        x.factory.AlterAfterActivate=true;
        var verdict=x.api.TryRestore(Data(11,false,true),Run,11);
        return verdict==CanonicalGateResult.HostException&&!x.api.Quarantined&&
            old.Active&&x.api.Generation==10&&
            x.disk.Read(Run,out long g,out _)&&g==10;
    }
    private static bool AbortFailureSticky(string root)
    {
        var x=Make(root);
        if(x.api.TryRestore(Data(10,true,false),Run,10)!=CanonicalGateResult.Accepted)return false;
        x.factory.AlterAfterActivate=true;x.factory.FailRetire=true;
        return x.api.TryRestore(Data(11,false,true),Run,11)==CanonicalGateResult.RollbackQuarantine&&
           x.api.Quarantined&&!x.api.TryCapturePreview(out _);
    }
    private static bool InProcessTwoActive(string root)
    {
        var a=Make(root);
        if(a.api.TryRestore(Data(10,true,false),Run,10)!=CanonicalGateResult.Accepted)return false;
        var b=Make(root);
        bool both=b.api.TryRestore(Data(10,true,false),Run,10)==CanonicalGateResult.Accepted&&
            a.api.HasWorld&&b.api.HasWorld&&a.factory.Last!.Active&&b.factory.Last!.Active&&
            a.api.TryCapturePreview(out byte[] pa)&&HasIds(pa,10,true,false)&&
            b.api.TryCapturePreview(out byte[] pb)&&HasIds(pb,10,true,false);
        if(both)Console.WriteLine("W21_681_SAME_SLOT_TWO_GATEWAYS_REPLAY_TWO_ACTIVE_WORLDS_RED=REPRODUCED");
        return both;
    }
    private static bool CrossProcessTwoEpochs(string root)
    {
        var old=Make(root);
        if(old.api.TryRestore(Data(10,true,false),Run,10)!=CanonicalGateResult.Accepted)return false;
        string ready=Path.Combine(root,"child-ready");
        string done=Path.Combine(root,"child-done");
        var p=new ProcessStartInfo("dotnet"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        p.ArgumentList.Add(typeof(Independent).Assembly.Location);
        p.ArgumentList.Add("--child-owner");
        p.ArgumentList.Add(root);p.ArgumentList.Add(ready);p.ArgumentList.Add(done);
        using(var child=Process.Start(p))
        {
            if(child==null)return false;
            try {
                if(!SpinWait.SpinUntil(()=>File.Exists(ready)||child.HasExited,12000))
                    return false;
                if(!File.Exists(ready)||child.HasExited)return false;
                bool current=old.factory.Last!=null&&old.factory.Last.Active&&
                    old.api.TryCapturePreview(out byte[] preview)&&HasIds(preview,10,true,false)&&
                    old.disk.Read(Run,out long rev,out string hash)&&rev==11&&
                    hash==Hash(Data(11,false,true));
                if(current)Console.WriteLine("W21_681_CROSS_PROCESS_NEW_HEAVY_WHILE_OLD_LIGHT_ACTIVE_RED=REPRODUCED");
                return current;
            } finally {
                File.WriteAllText(done,"release");
                if(!child.WaitForExit(12000))child.Kill();
                Console.WriteLine("ORIGINAL_681_CHILD_PROCESS_EXIT="+child.ExitCode);
            }
        }
    }
    private static int Child(string root,string ready,string done)
    {
        var x=Make(root);
        if(x.api.TryRestore(Data(11,false,true),Run,11)!=CanonicalGateResult.Accepted||
            !x.api.HasWorld||x.factory.Last==null||!x.factory.Last.Active||
            !x.api.TryCapturePreview(out byte[] current)||!HasIds(current,11,false,true))
            return 2;
        File.WriteAllText(ready,"child-owner-still-active");
        return SpinWait.SpinUntil(()=>File.Exists(done),12000)?0:3;
    }
    public static int Main(string[] args)
    {
        if(args.Length==4&&args[0]=="--child-owner")
            return Child(args[1],args[2],args[3]);
        if(args.Length>1||(args.Length==1&&args[0]!="--release"))return 4;
        Check("SOURCE_ORIGINAL_556_W06_ALL_FOUR_LIGHT_HEAVY_IDS",()=>Scratch(FourOriginalIds));
        Check("SOURCE_MONOTONIC_GEN10_TO_11_CLEAN_OLD_RETIRE",()=>Scratch(AdvanceRollback));
        Check("SOURCE_FOREIGN_SAME_GEN_CONSUMED_FORK_REJECTED_PRESTAGE",()=>Scratch(ForkDeniedBeforePrepare));
        Check("SOURCE_WRONG_RUN_AND_INVALID_BINARY_PRESTAGE_DENIAL",()=>Scratch(InvalidRunAndBinary));
        Check("SOURCE_MAIN_THREAD_AND_REENTRY_GUARDS",()=>Scratch(MainThreadAndReentry));
        Check("SOURCE_REAL_FILE_DURABLE_COMMIT_LOST_ACK_STICKY_QUARANTINE",()=>Scratch(MissingAckSticky));
        Check("SOURCE_PRECOMMIT_INVALID_ACTIVATE_ROLLBACK_PRESERVES_OLD",()=>Scratch(BadActivateRollback));
        Check("SOURCE_INCOMPLETE_ABORT_STICKY_QUARANTINE",()=>Scratch(AbortFailureSticky));
        Check("RED_681_TWO_LIVE_GATEWAYS_SAME_COLD_REPLAY_NO_LEASE",()=>Scratch(InProcessTwoActive));
        Check("RED_681_CHILD_PROCESS_CAN_PUBLISH_NEWER_EPOCH_WHILE_OLD_WORLD_ACTIVE",()=>Scratch(CrossProcessTwoEpochs));
        Console.WriteLine("B0161_W21_ORIGINAL_681_SOURCE_GIT_DIRECT_CSHARP_EXECUTED");
        Console.WriteLine("TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
        Console.WriteLine("SOURCE_RELEASE_GATE="+(fail==0?"HOLD_UNFENCED_MULTI_OWNER_681":"HARNESS_FAILURE"));
        Console.WriteLine("UNITY_G1_NOT_RUN CANONICAL_PRIVATE_510_UNIMPLEMENTED G2_HOLD NO_MERGE");
        return fail!=0?1:(args.Length==1?3:0);
    }
}
