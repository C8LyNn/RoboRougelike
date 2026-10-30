using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;
using B01.CanonicalWorldApi001;

internal static class Tests
{
    static int passed,failed;
    static void Check(string label,Func<bool> test)
    {
        try
        {
            if(test()){passed++;Console.WriteLine("PASS "+label);}
            else{failed++;Console.WriteLine("FAIL "+label);}
        }
        catch(Exception ex){failed++;Console.WriteLine("FAIL "+label+" "+ex);}
    }
    static byte[] Save(bool light,bool heavy,long gen=1,long run=7)
    {
        var parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool detached=i==0?light:heavy;
            long id=FrozenPartCatalog.Id(i),owner=FrozenPartCatalog.Owner(i);
            double mass=FrozenPartCatalog.Mass(i);
            parts[i]=new PersistedPart(id,owner,detached?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                detached?PartDockState.Detached:PartDockState.Attached,
                detached,!detached,new Vector3d(i*3,1,0),0,0,0,1,
                detached?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),mass);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,gen,parts));
    }
    sealed class Gate : ICanonicalMutationGate
    {
        public bool Allowed=true,Throw;
        public Action? OnIdle;
        public bool IsQuiescent()
        {
            OnIdle?.Invoke();
            if(Throw)throw new IOException("host busy");
            return Allowed;
        }
    }
    sealed class World : ICanonicalStagedWorld
    {
        public RunSnapshot Data;
        public bool Active,Retired,FailActivate,FailRetire,FailReactivate,WrongAfterActivate;
        public int Activations;
        public World(RunSnapshot data){Data=data;}
        public RunSnapshot Capture()
        {
            if(WrongAfterActivate&&Active)
                return SnapshotCodec.TryDecode(Save(false,true,Data.Generation,Data.RunId),
                    out RunSnapshot? altered)&&altered!=null?altered:Data;
            return Data;
        }
        public void Activate()
        {
            Activations++;
            if(FailReactivate&&Activations>1)throw new IOException("rollback failure");
            Active=true;
            if(FailActivate)throw new IOException("activation");
        }
        public void Deactivate(){Active=false;}
        public void Retire()
        {
            if(FailRetire)throw new IOException("retire");
            Retired=true;Active=false;
        }
    }
    sealed class Factory : ICanonicalRigFactory
    {
        public World? Last;
        public bool FailStage,FailActivate,WrongAfterActivate;
        public int Stages;
        public ICanonicalStagedWorld BuildInactive(RunSnapshot snap,ColdRestore privateLedgerPlan)
        {
            Stages++;
            if(FailStage)throw new IOException("stage");
            for(int i=0;i<FrozenPartCatalog.Slots;i++)
            {
                PersistedPart part=snap.Parts[i];
                if(privateLedgerPlan.ContainsConsumedId(i,part.Id)!=part.Consumed)
                    throw new InvalidDataException("original frozen W06 ledger mismatch");
            }
            Last=new World(snap){FailActivate=FailActivate,WrongAfterActivate=WrongAfterActivate};
            return Last;
        }
    }
    sealed class Disk : ICanonicalCommitPort
    {
        readonly string file;
        public bool ThrowBeforeCommit,ThrowAfterCommit,RejectCommit,FailRead;
        public Action? OnSuccess;
        public Disk(string file){this.file=file;}
        public bool Read(long run,out long gen,out string hash)
        {
            gen=0;hash="";
            if(FailRead)return false;
            if(!File.Exists(file))return true;
            string[] parts=File.ReadAllText(file).Split('|');
            if(parts.Length!=3||parts[0]!=run.ToString())return false;
            if(!long.TryParse(parts[1],out gen)||hash.Length>64)return false;
            hash=parts[2];return hash.Length==64;
        }
        public bool Commit(long run,long old,string oldHash,long next,string newHash)
        {
            if(ThrowBeforeCommit)throw new IOException("before commit");
            if(RejectCommit)return false;
            if(!Read(run,out long g,out string h)||g!=old||h!=oldHash)return false;
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            byte[] bytes=System.Text.Encoding.ASCII.GetBytes(run+"|"+next+"|"+newHash);
            using(var writer=new FileStream(file,FileMode.Create,FileAccess.Write,FileShare.None))
            {writer.Write(bytes,0,bytes.Length);writer.Flush(true);}
            OnSuccess?.Invoke();
            if(ThrowAfterCommit)throw new IOException("after disk durability, before ACK");
            return true;
        }
    }
    static void WithDisk(Func<Gate,Factory,Disk,CanonicalWorldGateway,bool> test)
    {
        string dir=Path.Combine(Path.GetTempPath(),"b01-canonical-"+Guid.NewGuid().ToString("N"));
        try
        {
            var gate=new Gate();var fac=new Factory();
            var disk=new Disk(Path.Combine(dir,"revision"));
            var api=new CanonicalWorldGateway(gate,fac,disk);
            if(!test(gate,fac,disk,api))throw new InvalidOperationException("Assertion failed");
        }
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
    static bool Every(Func<Gate,Factory,Disk,CanonicalWorldGateway,bool> f)
    {WithDisk(f);return true;}
    static string Hash(byte[] data)
    {
        using(var sha=SHA256.Create())
            return Convert.ToHexString(sha.ComputeHash(data)).ToLowerInvariant();
    }
    static void MainSuite()
    {
        Check("exact original frozen schema and canonical source pin",()=>
            FrozenPartCatalog.SelectedSourceSha=="d94400d95d13db7f27e1533df05c9292d5380431"&&
            SnapshotCodec.TryDecode(Save(true,false),out RunSnapshot? s)&&s!=null&&
            s.Parts[0].Id==2310001&&s.Parts[1].Id==2310002);
        for(int flags=0;flags<4;flags++)
        {
            int f=flags;
            Check("real W06 ledger stage + #556 roundtrip combination "+f,()=>Every((g,fac,d,api)=>{
                byte[] b=Save((f&1)!=0,(f&2)!=0);
                return api.TryRestore(b,7,1)==CanonicalGateResult.Accepted&&
                    fac.Last!=null&&fac.Last.Active&&
                    api.TryCapturePreview(out byte[] outBytes)&&
                    Convert.ToBase64String(outBytes)==Convert.ToBase64String(b)&&
                    d.Read(7,out long gen,out string hash)&&gen==1&&hash==Hash(b);
            }));
        }
        Check("wrong run never stages",()=>Every((g,f,d,a)=>
            a.TryRestore(Save(true,false,1,51),52,1)==CanonicalGateResult.WrongRun&&f.Stages==0));
        Check("corrupt original #556 checksum never stages",()=>Every((g,f,d,a)=>{
            byte[] b=Save(true,false);b[105]^=0x4;
            return a.TryRestore(b,7,1)==CanonicalGateResult.InvalidSnapshot&&f.Stages==0;
        }));
        Check("old active g10 preserved after invalid incoming data",()=>Every((g,f,d,a)=>{
            if(a.TryRestore(Save(true,false,10),7,1)!=CanonicalGateResult.Accepted)return false;
            World? old=f.Last;byte[] bad=Save(false,true,11);bad[130]^=0x2;
            return a.TryRestore(bad,7,1)==CanonicalGateResult.InvalidSnapshot&&
                ReferenceEquals(old,f.Last)&&old!=null&&old.Active&&a.Generation==10;
        }));
        Check("stale and same-generation fork denied BEFORE stage",()=>Every((g,f,d,a)=>{
            if(a.TryRestore(Save(true,false,10),7,1)!=CanonicalGateResult.Accepted)return false;
            int before=f.Stages;
            return a.TryRestore(Save(false,true,10),7,1)==CanonicalGateResult.StaleGeneration&&
                a.TryRestore(Save(false,true,9),7,1)==CanonicalGateResult.StaleGeneration&&
                f.Stages==before;
        }));
        Check("post-activation ID drift rolls back original world",()=>Every((g,f,d,a)=>{
            if(a.TryRestore(Save(true,false,10),7,1)!=CanonicalGateResult.Accepted)return false;
            World? old=f.Last;f.WrongAfterActivate=true;
            return a.TryRestore(Save(false,true,11),7,1)==CanonicalGateResult.HostException&&
                old!=null&&old.Active&&!a.Quarantined&&a.Generation==10&&
                d.Read(7,out long disk,out _)&&disk==10;
        }));
        Check("real disk Commit first load succeeded",()=>Every((g,f,d,a)=>
            a.TryRestore(Save(false,true,1),7,1)==CanonicalGateResult.Accepted&&
            d.Read(7,out long gen,out _)&&gen==1));
        Check("cold restart on same exact frozen bytes is allowed",()=>Every((g,f,d,a)=>{
            byte[] bytes=Save(false,true,10);
            if(a.TryRestore(bytes,7,1)!=CanonicalGateResult.Accepted)return false;
            var next=new CanonicalWorldGateway(g,new Factory(),d);
            return next.TryRestore(bytes,7,1)==CanonicalGateResult.Accepted;
        }));
        Check("cold restart same-gen different consumed ID is refused",()=>Every((g,f,d,a)=>{
            if(a.TryRestore(Save(true,false,10),7,1)!=CanonicalGateResult.Accepted)return false;
            var freshFactory=new Factory();
            var fresh=new CanonicalWorldGateway(g,freshFactory,d);
            return fresh.TryRestore(Save(false,true,10),7,1)==CanonicalGateResult.StaleGeneration&&
                freshFactory.Stages==0;
        }));
        Check("external Commit AFTER disk write then throws -> sticky quarantine",()=>Every((g,f,d,a)=>{
            if(a.TryRestore(Save(true,false,10),7,1)!=CanonicalGateResult.Accepted)return false;
            World? old=f.Last;d.ThrowAfterCommit=true;
            var verdict=a.TryRestore(Save(false,true,11),7,1);
            return verdict==CanonicalGateResult.RollbackQuarantine&&a.Quarantined&&
                old!=null&&old.Active&&a.Generation==10&&
                d.Read(7,out long gen,out _)&&gen==11&&
                !a.TryCapturePreview(out _)&&
                a.TryRestore(Save(false,true,12),7,1)==CanonicalGateResult.RollbackQuarantine;
        }));
        Check("external Commit BEFORE disk write throws -> conservative quarantine",()=>Every((g,f,d,a)=>{
            if(a.TryRestore(Save(true,false,10),7,1)!=CanonicalGateResult.Accepted)return false;
            d.ThrowBeforeCommit=true;
            return a.TryRestore(Save(false,true,11),7,1)==CanonicalGateResult.RollbackQuarantine&&
                a.Quarantined&&d.Read(7,out long gen,out _)&&gen==10;
        }));
        Check("external Commit(false) -> quarantine",()=>Every((g,f,d,a)=>{
            if(a.TryRestore(Save(true,false,10),7,1)!=CanonicalGateResult.Accepted)return false;
            d.RejectCommit=true;
            return a.TryRestore(Save(false,true,11),7,1)==CanonicalGateResult.RollbackQuarantine&&
                a.Quarantined;
        }));
        Check("post Commit real private ledger changes -> quarantine",()=>Every((g,f,d,a)=>{
            if(a.TryRestore(Save(true,false,10),7,1)!=CanonicalGateResult.Accepted)return false;
            d.OnSuccess=()=>{if(f.Last!=null)f.Last.WrongAfterActivate=true;};
            return a.TryRestore(Save(false,true,11),7,1)==CanonicalGateResult.RollbackQuarantine&&
                a.Quarantined&&d.Read(7,out long gen,out _)&&gen==11;
        }));
        Check("pre Commit activation throws -> old restored, no quarantine",()=>Every((g,f,d,a)=>{
            if(a.TryRestore(Save(true,false,10),7,1)!=CanonicalGateResult.Accepted)return false;
            World? old=f.Last;f.FailActivate=true;
            return a.TryRestore(Save(false,true,11),7,1)==CanonicalGateResult.HostException&&
                !a.Quarantined&&old!=null&&old.Active&&a.Generation==10&&
                d.Read(7,out long gen,out _)&&gen==10;
        }));
        Check("failed old Retire after publish => quarantine immediately",()=>Every((g,f,d,a)=>{
            if(a.TryRestore(Save(true,false,10),7,1)!=CanonicalGateResult.Accepted)return false;
            if(f.Last==null)return false;
            f.Last.FailRetire=true;
            return a.TryRestore(Save(false,true,11),7,1)==CanonicalGateResult.RollbackQuarantine&&
                a.Quarantined&&a.Generation==11;
        }));
        Check("no stage when quiescence busy",()=>Every((g,f,d,a)=>{
            g.Allowed=false;
            return a.TryRestore(Save(true,false),7,1)==CanonicalGateResult.HostBusy&&f.Stages==0;
        }));
        Check("idle callback reentrant Load blocked by transaction",()=>Every((g,f,d,a)=>{
            CanonicalGateResult nested=CanonicalGateResult.Accepted;bool once=false;
            g.OnIdle=()=>{
                if(once)return;
                once=true;nested=a.TryRestore(Save(false,true,2),7,1);
            };
            return a.TryRestore(Save(true,false,1),7,1)==CanonicalGateResult.Accepted&&
                nested==CanonicalGateResult.HostBusy;
        }));
        Check("wrong thread cannot mutate or capture",()=>Every((g,f,d,a)=>{
            if(a.TryRestore(Save(true,false,10),7,1)!=CanonicalGateResult.Accepted)return false;
            bool[] off=Task.Run(()=>new[]{
                a.TryRestore(Save(false,true,11),7,1)==CanonicalGateResult.HostBusy,
                !a.TryCapturePreview(out _)
            }).GetAwaiter().GetResult();
            return off[0]&&off[1]&&a.Generation==10;
        }));
        Check("missing file/snapshot caused no global game file mutation",()=>Every((g,f,d,a)=>{
            byte[] b=Save(true,false,7);
            return a.TryRestore(b,7,1)==CanonicalGateResult.Accepted&&
                d.Read(7,out long gen,out _)&&gen==7&&f.Stages==1;
        }));
        for(int n=0;n<16;n++)
        {
            int j=n;
            Check("original #556 corruption independent byte "+j,()=>Every((g,f,d,a)=>{
                byte[] b=Save(true,false,1);b[115+j]^=0x1;
                return a.TryRestore(b,7,1)==CanonicalGateResult.InvalidSnapshot&&f.Stages==0;
            }));
        }
        Console.WriteLine("UNITY_ENGINE_NOT_COMPILED; PRIVATE_510_RIG_LEDGER_PORT_NOT_IMPLEMENTED; FULL_CRASH_ATOMICITY_NOT_PROVEN");
    }
    public static int Main()
    {
        MainSuite();
        Console.WriteLine("TOTAL "+(passed+failed)+" PASS "+passed+" FAIL "+failed);
        return failed==0?0:1;
    }
}
