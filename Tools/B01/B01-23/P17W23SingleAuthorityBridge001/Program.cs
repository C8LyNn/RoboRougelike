#nullable enable
using System;
using System.IO;
using System.Diagnostics;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.CanonicalWorldApi001;
using B01.CanonicalSingleAuthorityW23;
using B01.SingleFileSaveSlot001;
internal static class T
{
    static int passed,failed;
    static void Check(string label,Func<bool> test)
    {try{if(test()){passed++;Console.WriteLine("PASS "+label);}
         else{failed++;Console.WriteLine("FAIL "+label);}}
     catch(Exception e){failed++;Console.WriteLine("FAIL "+label+" "+e);}}
    static byte[] Snapshot(long run,long generation,bool light)
    {
        var parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool d=i==0?light:!light;
            long owner=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(FrozenPartCatalog.Id(i),owner,d?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                d?PartDockState.Detached:PartDockState.Attached,d,!d,
                new Vector3d(i*3,1,0),0,0,0,1,
                d?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),FrozenPartCatalog.Mass(i));
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,generation,parts));
    }
    sealed class LostAck:IAtomicEnvelopePublisher
    {
        public void Publish(string stage,string final)
        {File.Move(stage,final,true);throw new IOException("ACK lost after atomic rename");}
    }
    sealed class Gate:ICanonicalMutationGate{public bool IsQuiescent()=>true;}
    sealed class World:ICanonicalStagedWorld
    {
        readonly RunSnapshot snapshot;
        public bool Active,Retired;
        public World(RunSnapshot s){snapshot=s;}
        public RunSnapshot Capture()=>snapshot;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Active=false;Retired=true;}
    }
    sealed class Factory:ICanonicalRigFactory
    {
        public World? Last;
        public bool Partial,FailCleanup;
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore plan)
            =>new Stage(this,s,plan);
        sealed class Stage:ICanonicalStageOwnership
        {
            readonly Factory factory;readonly RunSnapshot snapshot;readonly ColdRestore ledger;
            World? w;
            public Stage(Factory f,RunSnapshot s,ColdRestore l){factory=f;snapshot=s;ledger=l;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                for(int i=0;i<2;i++)
                    if(ledger.ContainsConsumedId(i,snapshot.Parts[i].Id)!=snapshot.Parts[i].Consumed)
                        throw new IOException("W06 original ledger mismatch");
                w=new World(snapshot);factory.Last=w;
                if(factory.Partial){w.Active=true;throw new IOException("orphan before return");}
                return w;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                if(factory.FailCleanup)return false;
                w?.Deactivate();w?.Retire();return w==null||w.Retired;
            }
        }
    }
    static bool InRoot(Func<string,bool> test)
    {
        string p=Path.Combine(Path.GetTempPath(),"b01w23_"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(p);
        try{return test(p);}finally{try{Directory.Delete(p,true);}catch(IOException){}}
    }
    static CanonicalSingleAuthoritySession Open(string dir,Guid id,bool create,Factory? fac=null)
    {
        var status=CanonicalSingleAuthoritySession.TryOpen(dir,71,id,create,out var session);
        if(status!=SessionOpenResult.Accepted||session==null||
           !session.Attach(new Gate(),fac??new Factory()))
            throw new IOException("single authority session could not open: "+status);
        return session;
    }
    static int Child(string dir,Guid id)
    {
        var psi=new ProcessStartInfo("dotnet"){UseShellExecute=false};
        psi.ArgumentList.Add(typeof(T).Assembly.Location);psi.ArgumentList.Add("--child");
        psi.ArgumentList.Add(dir);psi.ArgumentList.Add(id.ToString("D"));
        using var p=Process.Start(psi);
        if(p==null)return -1;
        if(!p.WaitForExit(30000)){p.Kill(true);return -2;}
        return p.ExitCode;
    }
    static int Main(string[] args)
    {
        if(args.Length==3&&args[0]=="--child")
        {
            var status=CanonicalSingleAuthoritySession.TryOpen(args[1],71,Guid.Parse(args[2]),
                false,out var candidate);
            if(status==SessionOpenResult.Busy)return 7;
            if(status!=SessionOpenResult.Accepted||candidate==null)return 8;
            return candidate.TryCloseAfterPhysicalRetirement(()=>true)?0:9;
        }
        Check("real original #556 stable Light 2310001 and Heavy 2310002",()=>{
            return SnapshotCodec.TryDecode(Snapshot(71,1,true),out RunSnapshot? x)&&
                x!=null&&x.Parts[0].Id==2310001&&x.Parts[1].Id==2310002;
        });
        Check("ONE original #669 physical OS slot.lease and durable #556 envelope",()=>InRoot(root=>{
            string dir=Path.Combine(root,"new");Guid slot=Guid.NewGuid();var owner=Open(dir,slot,true);
            bool pass=owner.TryRestore(Snapshot(71,1,true),71,1)==CanonicalGateResult.Accepted&&
                File.Exists(Path.Combine(dir,"slot.lease"))&&File.Exists(Path.Combine(dir,"save.envelope"))&&
                !Directory.Exists(Path.Combine(dir,"b0123_canonical_world_authority"))&&
                owner.TryCapturePreview(out byte[] b)&&
                SnapshotCodec.TryDecode(b,out RunSnapshot? s)&&s!=null&&s.Parts[0].Consumed;
            return owner.TryCloseAfterPhysicalRetirement(()=>true)&&pass;
        }));
        Check("different actual CLR process denied until verified physical retirement",()=>InRoot(root=>{
            string dir=Path.Combine(root,"new");Guid slot=Guid.NewGuid();var owner=Open(dir,slot,true);
            bool denied=owner.TryRestore(Snapshot(71,1,true),71,1)==CanonicalGateResult.Accepted&&
                Child(dir,slot)==7;
            bool released=owner.TryCloseAfterPhysicalRetirement(()=>true);
            return denied&&released&&Child(dir,slot)==0;
        }));
        Check("P0 #707 partial rig with HasWorld false holds sole physical lease even after Dispose",()=>InRoot(root=>{
            string dir=Path.Combine(root,"new");Guid slot=Guid.NewGuid();
            var f=new Factory{Partial=true,FailCleanup=true};var owner=Open(dir,slot,true,f);
            bool unsafeStage=owner.TryRestore(Snapshot(71,1,true),71,1)==
                CanonicalGateResult.RollbackQuarantine&&owner.Quarantined&&!owner.HasWorld;
            owner.Dispose();
            bool refused=unsafeStage&&Child(dir,slot)==7&&
                !owner.TryCloseAfterPhysicalRetirement(()=>false);
            f.Last?.Deactivate();f.Last?.Retire();
            return refused&&owner.TryCloseAfterPhysicalRetirement(()=>f.Last!=null&&f.Last.Retired)&&
                Child(dir,slot)==0;
        }));
        Check("cold restart exact gen1 allowed but same-gen different ledger denied",()=>InRoot(root=>{
            string dir=Path.Combine(root,"new");Guid slot=Guid.NewGuid();var first=Open(dir,slot,true);
            if(first.TryRestore(Snapshot(71,1,true),71,1)!=CanonicalGateResult.Accepted||
               !first.TryCloseAfterPhysicalRetirement(()=>true))return false;
            var second=Open(dir,slot,false);
            bool stale=second.TryRestore(Snapshot(71,1,false),71,1)==
                CanonicalGateResult.StaleGeneration;
            bool same=second.TryRestore(Snapshot(71,1,true),71,1)==CanonicalGateResult.Accepted;
            return second.TryCloseAfterPhysicalRetirement(()=>true)&&stale&&same;
        }));
        Check("same process second original slot.lease owner denied",()=>InRoot(root=>{
            string dir=Path.Combine(root,"new");Guid slot=Guid.NewGuid();
            var first=Open(dir,slot,true);
            var status=CanonicalSingleAuthoritySession.TryOpen(dir,71,slot,false,out var rival);
            return status==SessionOpenResult.Busy&&rival==null&&
                first.TryCloseAfterPhysicalRetirement(()=>true);
        }));
        Check("wrong run and corrupt original #556 refuse before physical staging",()=>InRoot(root=>{
            string dir=Path.Combine(root,"new");Guid slot=Guid.NewGuid();var f=new Factory();
            var owner=Open(dir,slot,true,f);
            var invalid=Snapshot(71,1,true);invalid[110]^=1;
            bool denied=owner.TryRestore(Snapshot(72,1,true),72,1)==
                CanonicalGateResult.WrongRun&&
                owner.TryRestore(invalid,71,1)==CanonicalGateResult.InvalidSnapshot&&
                f.Last==null&&!File.Exists(Path.Combine(dir,"save.envelope"));
            return denied&&owner.TryCloseAfterPhysicalRetirement(()=>true);
        }));
        Check("actual original #669 published envelope then LOST ACK retains owner quarantine",()=>InRoot(root=>{
            string dir=Path.Combine(root,"new");Guid id=Guid.NewGuid();
            var result=CanonicalSingleAuthoritySession.TryOpen(dir,71,id,true,
                out var owner,new LostAck());
            if(result!=SessionOpenResult.Accepted||owner==null||
                !owner.Attach(new Gate(),new Factory()))return false;
            bool failClosed=owner.TryRestore(Snapshot(71,1,true),71,1)==
                CanonicalGateResult.RollbackQuarantine&&owner.Quarantined&&
                File.Exists(Path.Combine(dir,"save.envelope"))&&
                !owner.TryCapturePreview(out _)&&Child(dir,id)==7;
            return failClosed&&owner.TryCloseAfterPhysicalRetirement(()=>true)&&
                Child(dir,id)==0;
        }));
        Check("external valid previous-generation envelope rollback blocks live preview",()=>InRoot(root=>{
            string dir=Path.Combine(root,"new");Guid id=Guid.NewGuid();var owner=Open(dir,id,true);
            if(owner.TryRestore(Snapshot(71,1,true),71,1)!=CanonicalGateResult.Accepted)
                return false;
            byte[] previous=File.ReadAllBytes(Path.Combine(dir,"save.envelope"));
            if(owner.TryRestore(Snapshot(71,2,false),71,1)!=CanonicalGateResult.Accepted)
                return false;
            File.WriteAllBytes(Path.Combine(dir,"save.envelope"),previous);
            bool fenced=!owner.TryCapturePreview(out _)&&owner.Quarantined&&
                owner.OwnsSlot&&Child(dir,id)==7;
            return fenced&&owner.TryCloseAfterPhysicalRetirement(()=>true);
        }));
        Console.WriteLine("TOTAL "+(passed+failed)+" PASS "+passed+" FAIL "+failed);
        Console.WriteLine("UNITY_G1=NOT_RUN PRIVATE_510=NOT_IMPLEMENTED");
        return failed==0?0:1;
    }
}
