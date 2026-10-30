using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using B01.CanonicalUnifiedAuthorityV4;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.SingleFileSaveSlot001;

internal static class Program
{
    private static int passed,failed;
    static void Test(string name,Func<bool> f)
    {
        try
        {
            bool ok=f();
            if(ok){passed++;Console.WriteLine("PASS "+name);}
            else{failed++;Console.WriteLine("FAIL "+name);}
        }
        catch(Exception e){failed++;Console.WriteLine("FAIL "+name+" "+e);}
    }
    static byte[] Save(bool light,bool heavy,long gen=1,long run=7)
    {
        PersistedPart[] parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool detached=i==0?light:heavy;
            long id=FrozenPartCatalog.Id(i),owner=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(id,owner,detached?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                detached?PartDockState.Detached:PartDockState.Attached,
                detached,!detached,new Vector3d(i*3,1,0),0,0,0,1,
                detached?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),FrozenPartCatalog.Mass(i));
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,gen,parts));
    }
    sealed class Gate : ICanonicalMutationGate
    {
        public bool Allowed=true;
        public bool IsQuiescent()=>Allowed;
    }
    sealed class World : ICanonicalStagedWorld
    {
        readonly RunSnapshot snapshot;
        public bool Active,Retired;
        public World(RunSnapshot snap){snapshot=snap;}
        public RunSnapshot Capture()=>snapshot;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Active=false;Retired=true;}
    }
    sealed class Factory : ICanonicalRigFactory
    {
        public bool FailAfterAllocate,FailCleanup;
        public World? Last;
        public int Count;
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot snap,
            ColdRestore cold)
        {
            Count++;
            for(int i=0;i<2;i++)
                if(cold.ContainsConsumedId(i,snap.Parts[i].Id)!=snap.Parts[i].Consumed)
                    throw new IOException("genuine W06 ledger mismatch");
            return new Owner(this,snap);
        }
        sealed class Owner : ICanonicalStageOwnership
        {
            readonly Factory f;readonly RunSnapshot snap;
            World? w;
            public Owner(Factory f,RunSnapshot snap){this.f=f;this.snap=snap;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                w=new World(snap);f.Last=w;
                if(f.FailAfterAllocate)
                {w.Active=true;throw new IOException("partial private rig");}
                return w;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                if(w==null)return true;
                if(f.FailCleanup)return false;
                w.Deactivate();w.Retire();return w.Retired;
            }
        }
    }
    sealed class LostAckPublisher : IAtomicEnvelopePublisher
    {
        public void Publish(string stage,string final)
        {
            File.Move(stage,final,true);
            throw new IOException("actual disk rename succeeded but ACK lost");
        }
    }
    static string Root()
    {
        string dir=Path.Combine(Path.GetTempPath(),"b0123-w23-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);return dir;
    }
    static bool Open(string root,Guid guid,out CanonicalUnifiedSlotSession? s,
        bool create=true,IAtomicEnvelopePublisher? writer=null)
        =>CanonicalUnifiedSlotSession.TryOpen(root,7,guid,create,out s,writer)==
            UnifiedOpenStatus.Accepted&&s!=null;
    static bool Close(CanonicalUnifiedSlotSession? s,Factory f)
    {
        if(s==null)return false;
        // The test-double's Retire happens in Gateway, verified by this exact
        // *test-only* callback; NOT evidence real Rigidbody destroyed in Unity.
        return s.TryCloseAfterPhysicalRetirement(()=>
            f.Last==null||(!f.Last.Active&&f.Last.Retired));
    }
    static bool With(Func<string,Guid,CanonicalUnifiedSlotSession,Factory,bool> f,
        IAtomicEnvelopePublisher? writer=null)
    {
        string root=Root();Guid id=Guid.NewGuid();
        if(!Open(root,id,out CanonicalUnifiedSlotSession? s,true,writer)||s==null)
            return false;
        Factory factory=new Factory();
        if(!s.Attach(new Gate(),factory))return false;
        bool result=f(root,id,s,factory);
        // If caller intentionally refused closure, DO NOT force Dispose or
        // remove a live owner. Temporary runner owns and cleans its sandbox.
        if(s.OwnsAuthority)Close(s,factory);
        return result;
    }
    static bool ChildBlocked(string root,Guid id)
    {
        var p=new ProcessStartInfo("dotnet");
        p.ArgumentList.Add(typeof(Program).Assembly.Location);
        p.ArgumentList.Add("--probe");
        p.ArgumentList.Add(root);
        p.ArgumentList.Add(id.ToString("D"));
        p.UseShellExecute=false;p.RedirectStandardOutput=true;
        using var child=Process.Start(p);
        if(child==null)return false;
        if(!child.WaitForExit(30000)){child.Kill();return false;}
        return child.ExitCode==12;
    }
    static int Main(string[] args)
    {
        if(args.Length==3&&args[0]=="--probe")
        {
            Guid id=Guid.Parse(args[2]);
            UnifiedOpenStatus st=CanonicalUnifiedSlotSession.TryOpen(
                args[1],7,id,false,out CanonicalUnifiedSlotSession? other);
            if(st==UnifiedOpenStatus.Accepted){other?.Dispose();return 0;}
            return st==UnifiedOpenStatus.Busy?12:20;
        }
        Test("original frozen #556 Light Heavy W06 stable IDs",()=>
            SnapshotCodec.TryDecode(Save(true,false),out RunSnapshot? d)&&d!=null&&
            d.Parts[0].Id==2310001&&d.Parts[1].Id==2310002&&
            d.Parts[0].Consumed&&!d.Parts[1].Consumed);
        for(int flag=0;flag<4;flag++)
        {
            int k=flag;
            Test("four real #556/#669 single-envelope original W06 states "+k,()=>
                With((root,id,s,f)=>{
                    byte[] bytes=Save((k&1)!=0,(k&2)!=0);
                    if(s.TryRestore(bytes,7,1)!=CanonicalGateResult.Accepted||
                       !s.TryCapturePreview(out byte[] preview)||
                       !preview.SequenceEqual(bytes))return false;
                    string directory=CanonicalUnifiedSlotSession.CanonicalDirectory(root,7,id);
                    bool onePhysical=File.Exists(Path.Combine(directory,"slot.lease"))&&
                        File.Exists(Path.Combine(directory,"save.envelope"))&&
                        !Directory.Exists(Path.Combine(root,"b0123_canonical_world_authority"));
                    SlotStatus status=s.TryReadSaved(out byte[] disk,out long gen);
                    return onePhysical&&status==SlotStatus.Accepted&&gen==1&&
                        disk.SequenceEqual(bytes)&&f.Last!=null&&f.Last.Active;
                }));
        }
        Test("same CLR second physical slot lease denied",()=>With((root,id,s,f)=>
            CanonicalUnifiedSlotSession.TryOpen(root,7,id,false,out _)==
                UnifiedOpenStatus.Busy));
        Test("actual child CLR process denied same physical slot lease",()=>
            With((root,id,s,f)=>ChildBlocked(root,id)));
        Test("no separate world .owner lock or duplicate canonical authority",()=>
            With((root,id,s,f)=>{
                string dir=CanonicalUnifiedSlotSession.CanonicalDirectory(root,7,id);
                return File.Exists(Path.Combine(dir,"slot.lease"))&&
                    File.Exists(Path.Combine(dir,"slot.owner"))&&
                    !Directory.Exists(Path.Combine(root,"b0123_canonical_world_authority"));
            }));
        Test("one session exactly one gateway, no reattach",()=>
            With((root,id,s,f)=>!s.Attach(new Gate(),new Factory())));
        Test("legitimate cold restart from original exact saved envelope",()=>{
            string root=Root();Guid id=Guid.NewGuid();
            if(!Open(root,id,out var a)||a==null)return false;
            var first=new Factory();if(!a.Attach(new Gate(),first))return false;
            byte[] value=Save(true,false,1);
            if(a.TryRestore(value,7,1)!=CanonicalGateResult.Accepted||
               !Close(a,first))return false;
            if(!Open(root,id,out var b,false)||b==null)return false;
            var second=new Factory();if(!b.Attach(new Gate(),second))return false;
            SlotStatus st=b.TryReadSaved(out byte[] cold,out long gen);
            bool ok=st==SlotStatus.Accepted&&gen==1&&cold.SequenceEqual(value)&&
                b.TryRestore(cold,7,1)==CanonicalGateResult.Accepted&&
                second.Last!=null&&second.Last.Active;
            return Close(b,second)&&ok;
        });
        Test("same-gen fork cannot overwrite legitimate consumed W06 IDs",()=>With((root,id,s,f)=>{
            if(s.TryRestore(Save(true,false),7,1)!=CanonicalGateResult.Accepted)return false;
            return s.TryRestore(Save(false,true),7,1)==
                CanonicalGateResult.StaleGeneration&&s.TryReadSaved(out byte[] disk,out _)
                ==SlotStatus.Accepted&&SnapshotCodec.TryDecode(disk,out RunSnapshot? snap)
                &&snap!=null&&snap.Parts[0].Consumed&&!snap.Parts[1].Consumed;
        }));
        Test("invalid foreign run cannot access owned epoch",()=>With((root,id,s,f)=>
            s.TryRestore(Save(true,false,1,77),77,1)!=CanonicalGateResult.Accepted&&
            f.Count==0));
        Test("frozen checksum corruption refused before physical stage",()=>With((root,id,s,f)=>{
            byte[] raw=Save(true,false);raw[106]^=0x20;
            return s.TryRestore(raw,7,1)==CanonicalGateResult.InvalidSnapshot&&f.Count==0;
        }));
        Test("failed partial stage false prevents Dispose releasing OS slot",()=>With((root,id,s,f)=>{
            f.FailAfterAllocate=true;f.FailCleanup=true;
            if(s.TryRestore(Save(true,false),7,1)!=CanonicalGateResult.RollbackQuarantine||
                !s.Quarantined||s.HasWorld)return false;
            s.Dispose();
            bool blocked=CanonicalUnifiedSlotSession.TryOpen(root,7,id,false,out _)==
                    UnifiedOpenStatus.Busy&&ChildBlocked(root,id);
            if(f.Last==null)return false;
            f.Last.Deactivate();f.Last.Retire();
            return blocked&&Close(s,f)&&!s.OwnsAuthority;
        }));
        Test("physical close false keeps one OS owner until retirement proof",()=>With((root,id,s,f)=>{
            if(s.TryRestore(Save(true,false),7,1)!=CanonicalGateResult.Accepted)return false;
            bool refused=!s.TryCloseAfterPhysicalRetirement(()=>false);
            bool other=CanonicalUnifiedSlotSession.TryOpen(root,7,id,false,out _)==
                UnifiedOpenStatus.Busy;
            return refused&&other&&Close(s,f);
        }));
        Test("lost original #669 file-publish ACK quarantines owner but leaves envelope",()=>{
            string root=Root();Guid id=Guid.NewGuid();
            if(!Open(root,id,out var s,true,new LostAckPublisher())||s==null)return false;
            var factory=new Factory();if(!s.Attach(new Gate(),factory))return false;
            bool rejected=s.TryRestore(Save(true,false),7,1)==
                CanonicalGateResult.RollbackQuarantine&&s.Quarantined&&
                CanonicalUnifiedSlotSession.TryOpen(root,7,id,false,out _)==
                    UnifiedOpenStatus.Busy;
            s.Dispose();
            if(!Close(s,factory)||!Open(root,id,out var restart,false)||restart==null)
                return false;
            var next=new Factory();if(!restart.Attach(new Gate(),next))return false;
            bool correct=restart.TryReadSaved(out byte[] original,out long gen)==
                SlotStatus.Accepted&&gen==1&&
                restart.TryRestore(original,7,1)==CanonicalGateResult.Accepted;
            return rejected&&correct&&Close(restart,next);
        });
        Test("foreign EMPTY owned slot directory creation rejected, no delete",()=>{
            string root=Root(),dir=CanonicalUnifiedSlotSession.CanonicalDirectory(
                root,7,Guid.NewGuid());
            Directory.CreateDirectory(dir);
            return CanonicalUnifiedSlotSession.TryOpen(root,7,
                Guid.Parse(Path.GetFileName(dir).Split("_guid_")[1]),true,out _)
                ==UnifiedOpenStatus.WrongOwner&&Directory.Exists(dir);
        });
        Console.WriteLine("B0123_W23_UNIFIED_ORIGINAL_669_729_TOTAL "+passed+
            " PASS "+passed+" FAIL "+failed);
        return failed==0?0:1;
    }
}
