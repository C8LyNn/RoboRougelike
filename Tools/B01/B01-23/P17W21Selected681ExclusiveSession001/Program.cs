using System;
using System.IO;
using System.Security.Cryptography;
using B01.CanonicalWorldApi001;
using B01.Selected681LeaseSession001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;

static class R2Tests
{
    static int ok,bad;
    static void Check(string name,Func<bool> f)
    {
        try{if(f()){ok++;Console.WriteLine("PASS "+name);}
        else{bad++;Console.WriteLine("FAIL "+name);}}
        catch(Exception ex){bad++;Console.WriteLine("FAIL "+name+" "+ex);}
    }
    static byte[] Bytes(long run,long gen,bool light,bool heavy)
    {
        var parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool off=i==0?light:heavy;
            long id=FrozenPartCatalog.Id(i),owner=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(id,owner,off?0:owner,
              FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
              off?PartDockState.Detached:PartDockState.Attached,off,!off,
              new Vector3d(i*3,1,0),0,0,0,1,
              off?new Vector3d(.25,0,0):new Vector3d(0,0,0),
              new Vector3d(0,0,0),FrozenPartCatalog.Mass(i));
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,gen,parts));
    }
    sealed class Gate:ICanonicalMutationGate
    {public bool IsQuiescent()=>true;}
    sealed class World:ICanonicalStagedWorld
    {
        public bool Active,Retired;
        readonly RunSnapshot snap;
        public World(RunSnapshot x){snap=x;}
        public RunSnapshot Capture()=>snap;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Active=false;Retired=true;}
    }
    sealed class Factory:ICanonicalRigFactory
    {
        public World? Last;
        public ICanonicalStagedWorld BuildInactive(RunSnapshot s,ColdRestore plan)
        {
            for(int i=0;i<2;i++)
                if(plan.ContainsConsumedId(i,s.Parts[i].Id)!=s.Parts[i].Consumed)
                    throw new InvalidOperationException("original #556 W06 ledger mismatch");
            Last=new World(s);return Last;
        }
    }
    sealed class Disk:ICanonicalCommitPort
    {
        readonly string file;
        public Disk(string path){file=path;}
        public bool Read(long run,out long generation,out string hash)
        {
            generation=0;hash="";
            if(!File.Exists(file))return true;
            string[] data=File.ReadAllText(file).Split('|');
            if(data.Length!=3||data[0]!=run.ToString())return false;
            if(!long.TryParse(data[1],out generation))return false;
            hash=data[2];return hash.Length==64;
        }
        public bool Commit(long run,long before,string beforeHash,long after,string hash)
        {
            if(!Read(run,out long rev,out string old)||rev!=before||old!=beforeHash)
                return false;
            File.WriteAllText(file,run+"|"+after+"|"+hash);
            return true;
        }
        public void Tamper(long run,long next,byte[] data)
        {
            using(var sha=SHA256.Create())
                File.WriteAllText(file,run+"|"+next+"|"+
                    BitConverter.ToString(sha.ComputeHash(data)).Replace("-","").ToLowerInvariant());
        }
    }
    sealed class Retire:IConfirmedPhysicalRetirement
    {
        public bool Allowed=true,Throws;
        public Factory? Factory;
        public bool RetireAllWorldsAndVerifyDestroyed()
        {
            if(Throws)throw new IOException("retirement failed");
            if(!Allowed)return false;
            // This is a declared managed test-double world. Do not fake
            // a positive retirement receipt while the model is still active.
            World? world=Factory?.Last;
            if(world==null)return true;
            world.Deactivate();
            world.Retire();
            return world.Retired&&!world.Active;
        }
    }
    static bool InDir(Func<string,bool> f)
    {
        string path=Path.Combine(Path.GetTempPath(),"b01-23-w21-681-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        try{return f(path);}
        finally
        {
            // Any rejected retirement intentionally holds OS locks until
            // process exit; never delete another owner's trusted data.
            try{Directory.Delete(path,true);}catch(IOException){}
        }
    }
    static ExclusiveCanonicalSession Open(string path,long run,Guid slot,Disk disk,Retire proof)
    {
        var factory=new Factory();
        proof.Factory=factory;
        return new ExclusiveCanonicalSession(path,run,slot,new Gate(),factory,disk,proof);
    }
    static int Main()
    {
        Check("W21 same exact run/slot second OS owner denied while live",()=>InDir(path=>{
            Guid slot=Guid.NewGuid();var disk=new Disk(Path.Combine(path,"revision"));
            var retirement=new Retire();
            using var first=Open(path,51,slot,disk,retirement);
            bool refused=false;
            try{using var second=Open(path,51,slot,disk,new Retire());}
            catch(IOException){refused=true;}
            return refused;
        }));
        Check("W21 original #556 real Light ID restored in exclusive session",()=>InDir(path=>{
            using var session=Open(path,51,Guid.NewGuid(),new Disk(Path.Combine(path,"revision")),new Retire());
            return session.TryRestore(Bytes(51,10,true,false),51,1)==CanonicalGateResult.Accepted&&
                session.Generation==10&&session.TryCaptureVerifiedPreview(out byte[] b)&&
                SnapshotCodec.TryDecode(b,out RunSnapshot? x)&&x!=null&&
                x.Parts[0].Id==2310001&&x.Parts[0].Consumed;
        }));
        Check("W21 unrelated run denied before any physics stage",()=>InDir(path=>{
            using var a=Open(path,51,Guid.NewGuid(),new Disk(Path.Combine(path,"revision")),new Retire());
            return a.TryRestore(Bytes(52,10,true,false),52,1)==CanonicalGateResult.WrongRun&&!a.HasWorld;
        }));
        Check("W21 external durable epoch advances => stale preview quarantine",()=>InDir(path=>{
            var disk=new Disk(Path.Combine(path,"revision"));
            using var a=Open(path,51,Guid.NewGuid(),disk,new Retire());
            if(a.TryRestore(Bytes(51,10,true,false),51,1)!=CanonicalGateResult.Accepted)return false;
            disk.Tamper(51,11,Bytes(51,11,false,true));
            return !a.TryCaptureVerifiedPreview(out _)&&a.Quarantined&&
                a.TryRestore(Bytes(51,12,false,true),51,1)==CanonicalGateResult.RollbackQuarantine;
        }));
        Check("W21 same slot cannot transfer while physical world retirement unverified",()=>InDir(path=>{
            Guid slot=Guid.NewGuid();var disk=new Disk(Path.Combine(path,"revision"));
            var proof=new Retire{Allowed=false};
            using var first=Open(path,51,slot,disk,proof);
            if(first.TryRestore(Bytes(51,10,true,false),51,1)!=CanonicalGateResult.Accepted||
               first.TryCloseOnlyAfterAllOwnedWorldsGone())return false;
            bool blocked=false;
            try{using var other=Open(path,51,slot,disk,new Retire());}
            catch(IOException){blocked=true;}
            proof.Allowed=true;
            return blocked&&first.TryCloseOnlyAfterAllOwnedWorldsGone();
        }));
        Check("W21 throwing private physical retirement proof retains held OS authority",()=>InDir(path=>{
            Guid slot=Guid.NewGuid();var disk=new Disk(Path.Combine(path,"revision"));
            var proof=new Retire{Throws=true};
            using var first=Open(path,51,slot,disk,proof);
            if(first.TryRestore(Bytes(51,10,true,false),51,1)!=CanonicalGateResult.Accepted||
               first.TryCloseOnlyAfterAllOwnedWorldsGone())return false;
            bool denied=false;
            try{using var second=Open(path,51,slot,disk,new Retire());}
            catch(IOException){denied=true;}
            proof.Throws=false;
            return denied&&first.TryCloseOnlyAfterAllOwnedWorldsGone();
        }));
        Check("W21 verified physical retirement releases slot and permits same-run cold restart",()=>InDir(path=>{
            Guid slot=Guid.NewGuid();var disk=new Disk(Path.Combine(path,"revision"));
            var first=Open(path,51,slot,disk,new Retire());
            if(first.TryRestore(Bytes(51,10,false,true),51,1)!=CanonicalGateResult.Accepted||
               !first.TryCloseOnlyAfterAllOwnedWorldsGone())return false;
            using var second=Open(path,51,slot,disk,new Retire());
            return second.TryRestore(Bytes(51,10,false,true),51,1)==CanonicalGateResult.Accepted&&
                second.TryCaptureVerifiedPreview(out byte[] b)&&
                SnapshotCodec.TryDecode(b,out RunSnapshot? x)&&x!=null&&
                x.Parts[1].Id==2310002&&x.Parts[1].Consumed;
        }));
        Check("W21 different run+slot can hold distinct owner leases simultaneously",()=>InDir(path=>{
            using var a=Open(path,51,Guid.NewGuid(),new Disk(Path.Combine(path,"a")),new Retire());
            using var b=Open(path,52,Guid.NewGuid(),new Disk(Path.Combine(path,"b")),new Retire());
            return a.TryRestore(Bytes(51,1,true,false),51,1)==CanonicalGateResult.Accepted&&
                b.TryRestore(Bytes(52,1,false,true),52,1)==CanonicalGateResult.Accepted;
        }));
        Check("W21 malicious invalid binary cannot be previewed as committed",()=>InDir(path=>{
            using var a=Open(path,51,Guid.NewGuid(),new Disk(Path.Combine(path,"revision")),new Retire());
            byte[] s=Bytes(51,1,true,false);s[110]^=1;
            return a.TryRestore(s,51,1)==CanonicalGateResult.InvalidSnapshot;
        }));
        Console.WriteLine("TOTAL "+(ok+bad)+" PASS "+ok+" FAIL "+bad);
        Console.WriteLine("SOURCE_R2_HELD_LEASE=TESTED; NATIVE_UNITY_NOT_RUN; PRIVATE_510_RIG_HOLD");
        return bad==0?0:1;
    }
}
