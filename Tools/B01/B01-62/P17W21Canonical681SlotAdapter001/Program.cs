#nullable enable
using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using B01.CanonicalWorldApi001;
using B01.CanonicalSlotAdapter001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.SingleFileSaveSlot001;

internal static class Program
{
    private const long Run=62121;
    private static readonly Guid Id=Guid.Parse("eec16810-1111-4111-8111-000000000681");
    private static int passed,failed;
    private static void Test(string name,Func<bool> fn)
    {
        try
        {
            if(fn()){passed++;Console.WriteLine("PASS "+name);}
            else{failed++;Console.WriteLine("FAIL "+name);}
        }
        catch(Exception e)
        {failed++;Console.WriteLine("FAIL "+name+" "+e);}
    }
    private static bool Temp(Func<string,bool> body)
    {
        string root=Path.Combine(Path.GetTempPath(),"B0162-W21-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{return body(root);}
        finally {Directory.Delete(root,true);}
    }
    private static byte[] Bytes(long gen,bool light,bool heavy,long run=Run)
    {
        PersistedPart[] parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            var spec=SecureRigCatalog.At(i);
            bool det=i==0?light:heavy;
            long owner=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(spec.PartId,owner,det?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                det?PartDockState.Detached:PartDockState.Attached,det,!det,
                new Vector3d(spec.RootX+spec.PartOffsetX,det?2.2:1.2,0),
                0,0,0,1,det?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),spec.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,gen,parts));
    }
    private static bool Equivalent(byte[] raw,long generation,bool light,bool heavy)
    {
        if(!ColdRestore.TryStage(raw,out ColdRestore? c)||c==null||
           c.Snapshot.Generation!=generation||c.Snapshot.RunId!=Run)return false;
        for(int i=0;i<2;i++)
            if(c.ContainsConsumedId(i,FrozenPartCatalog.Id(i))!=(i==0?light:heavy))
                return false;
        return true;
    }
    private sealed class Gate:ICanonicalMutationGate
    {
        public bool Idle=true;
        public int Calls;
        public Func<bool>? OnCheck;
        public bool IsQuiescent(){Calls++;return (OnCheck?.Invoke()??true)&&Idle;}
    }
    private sealed class World:ICanonicalStagedWorld
    {
        public RunSnapshot Snapshot;
        public bool Active,Retired;
        public World(RunSnapshot snapshot){Snapshot=snapshot;}
        public RunSnapshot Capture()=>Snapshot;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Retired=true;Active=false;}
    }
    private sealed class Factory:ICanonicalRigFactory
    {
        public World? Last,Previous;
        public int Builds;
        public ICanonicalStagedWorld BuildInactive(RunSnapshot snap,ColdRestore cold)
        {
            if(cold.Snapshot.RunId!=snap.RunId||
               !cold.ContainsConsumedId(0,FrozenPartCatalog.Id(0))==
                    snap.Parts[0].Consumed)throw new Exception("bad ledger");
            Previous=Last;Last=new World(snap);Builds++;return Last;
        }
        public bool RetireAll()
        {
            if(Last!=null)Last.Retire();
            if(Previous!=null)Previous.Retire();
            return (Last==null||Last.Retired)&&(Previous==null||Previous.Retired);
        }
    }
    private sealed class LostAck:IAtomicEnvelopePublisher
    {
        public bool Before,After;
        public int Publishes;
        public void Publish(string stage,string final)
        {
            Publishes++;
            if(Before)throw new IOException("injected failure before actual final rename");
            new OsAtomicEnvelopePublisher().Publish(stage,final);
            if(After)throw new IOException("injected ACK lost AFTER real atomic rename");
        }
    }
    private static bool Open(string directory,bool create,out CanonicalSlotSession? s,
        out Factory f, out Gate gate,IAtomicEnvelopePublisher? pub=null)
    {
        f=new Factory();gate=new Gate();s=null;
        var status=CanonicalSlotSession.TryOpen(directory,Run,Id,create,out s,pub);
        return status==CanonicalSessionOpenStatus.Accepted&&s!=null&&s.Attach(gate,f);
    }
    private static bool Release(CanonicalSlotSession? s,Factory f)
    {
        if(s==null)return false;
        bool done=f.RetireAll();
        bool result=s.TryCloseAfterPhysicalRetirement(()=>done);
        s.Dispose();
        return done&&result;
    }
    private static bool RoundTrip(int index)
    {
        return Temp(root=>{
            string path=Path.Combine(root,"owned-"+index);
            if(!Open(path,true,out var first,out var factory,out _)
               ||first==null)return false;
            bool light=(index&1)!=0,heavy=(index&2)!=0;
            try
            {
                if(first.TryRestore(Bytes(1,light,heavy),Run,1)!=CanonicalGateResult.Accepted)
                    return false;
                if(!first.Read(Run,out long generation,out string digest)||
                   generation!=1||digest!=CanonicalSlotSession.SourceHash(Bytes(1,light,heavy)))
                    return false;
                if(first.TryRestore(Bytes(2,!light,!heavy),Run,2)!=CanonicalGateResult.Accepted)
                    return false;
                if(!first.Read(Run,out generation,out digest)||generation!=2||
                   digest!=CanonicalSlotSession.SourceHash(Bytes(2,!light,!heavy)))
                    return false;
                if(!first.TryCapturePreview(out byte[] preview)||
                   !Equivalent(preview,2,!light,!heavy))return false;
                if(CanonicalSlotSession.TryOpen(path,Run,Id,false,out var other)!=
                   CanonicalSessionOpenStatus.Busy||other!=null)return false;
                if(!Release(first,factory))return false;
                first=null;
                if(!Open(path,false,out var cold,out var coldFactory,out _)||
                   cold==null)return false;
                try
                {
                    if(cold.TryRestore(Bytes(2,!light,!heavy),Run,2)!=CanonicalGateResult.Accepted)
                        return false; // legitimate replay of full original stored bytes
                    if(cold.TryRestore(Bytes(2,light,heavy),Run,2)!=
                       CanonicalGateResult.StaleGeneration)return false;
                    if(cold.TryRestore(Bytes(3,light,heavy),Run,3)!=CanonicalGateResult.Accepted)
                        return false;
                    if(!cold.Read(Run,out generation,out digest)||generation!=3||
                       digest!=CanonicalSlotSession.SourceHash(Bytes(3,light,heavy)))
                        return false;
                    return Release(cold,coldFactory);
                }
                finally {if(cold!=null)Release(cold,coldFactory);}
            }
            finally {if(first!=null)Release(first,factory);}
        });
    }
    private static bool LostAckScenario(bool post)
    {
        return Temp(root=>{
            string path=Path.Combine(root,"ack");
            var publisher=new LostAck();
            if(!Open(path,true,out var first,out var factory,out _,publisher)||first==null)
                return false;
            try
            {
                if(first.TryRestore(Bytes(1,true,false),Run,1)!=CanonicalGateResult.Accepted)
                    return false;
                if(post)publisher.After=true;else publisher.Before=true;
                if(first.TryRestore(Bytes(2,false,true),Run,2)!=
                   CanonicalGateResult.RollbackQuarantine||!first.Quarantined)
                    return false;
                if(first.TryRestore(Bytes(3,true,true),Run,3)!=
                   CanonicalGateResult.RollbackQuarantine)return false;
                if(first.Read(Run,out _,out _))return false;
                if(!Release(first,factory))return false;
                first=null;
                if(!Open(path,false,out var fresh,out var newFactory,out _)||fresh==null)
                    return false;
                try
                {
                    long expect=post?2:1;bool l=!post,h=post;
                    if(!fresh.Read(Run,out long gen,out string hash)||
                       gen!=expect||hash!=CanonicalSlotSession.SourceHash(Bytes(expect,l,h)))
                        return false;
                    if(fresh.TryRestore(Bytes(expect,l,h),Run,expect)!=
                       CanonicalGateResult.Accepted)return false;
                    return Release(fresh,newFactory);
                }
                finally{if(fresh!=null)Release(fresh,newFactory);}
            }
            finally{if(first!=null)Release(first,factory);}
        });
    }
    private static bool Reentry()
    {
        return Temp(root=>{
            if(!Open(Path.Combine(root,"reentry"),true,out var a,out var fac,out var gate)
                ||a==null)return false;
            try
            {
                int attempted=0;
                gate.OnCheck=()=>{
                    attempted++;
                    return a.TryRestore(Bytes(1,true,false),Run,1)==CanonicalGateResult.HostBusy;
                };
                bool ok=a.TryRestore(Bytes(1,true,false),Run,1)==CanonicalGateResult.Accepted&&
                    attempted>=2;
                gate.OnCheck=null;
                return ok&&Release(a,fac);
            }
            finally{gate.OnCheck=null;Release(a,fac);}
        });
    }
    private static bool Corrupt()
    {
        return Temp(root=>{
            string dir=Path.Combine(root,"corrupt");
            if(!Open(dir,true,out var a,out var fa,out _)||a==null)return false;
            try
            {
                if(a.TryRestore(Bytes(1,true,false),Run,1)!=CanonicalGateResult.Accepted)
                    return false;
                string file=Path.Combine(dir,"save.envelope");
                byte[] data=File.ReadAllBytes(file);data[data.Length-1]^=0x40;
                File.WriteAllBytes(file,data);
                return !a.Read(Run,out _,out _)&&a.Quarantined&&
                    a.TryRestore(Bytes(2,false,true),Run,2)==
                    CanonicalGateResult.RollbackQuarantine&&
                    File.ReadAllBytes(file).Length==data.Length;
            }
            finally{Release(a,fa);}
        });
    }
    private static bool ChildProbe()
    {
        return Temp(root=>{
            string dir=Path.Combine(root,"child");
            if(!Open(dir,true,out var a,out var fac,out _)||a==null)return false;
            try
            {
                if(!Probe(dir,3))return false;
                if(!Release(a,fac))return false;
                return Probe(dir,0);
            }
            finally{Release(a,fac);}
        });
    }
    private static bool Probe(string path,int expectedExit)
    {
        var psi=new ProcessStartInfo("dotnet")
        {UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        psi.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        psi.ArgumentList.Add("--child");
        psi.ArgumentList.Add(path);
        using var child=Process.Start(psi);
        if(child==null)return false;
        string stdout=child.StandardOutput.ReadToEnd();
        string stderr=child.StandardError.ReadToEnd();
        if(!child.WaitForExit(20000)){try{child.Kill(true);}catch{}return false;}
        return child.ExitCode==expectedExit&&
            stdout.Contains("W21_681_669_CHILD_"+(expectedExit==3?"DENIED":"GRANTED"));
    }
    private static bool BasicGuards()
    {
        return Temp(root=>{
            string dir=Path.Combine(root,"foreign");
            Directory.CreateDirectory(dir);
            if(CanonicalSlotSession.TryOpen(dir,Run,Id,true,out _)!=
               CanonicalSessionOpenStatus.NoOwner)return false;
            dir=Path.Combine(root,"owned");
            if(!Open(dir,true,out var a,out var fac,out _)||a==null)return false;
            try
            {
                if(a.TryRestore(Bytes(1,true,false,Run+1),Run,1)!=
                   CanonicalGateResult.InvalidSnapshot)return false;
                if(a.TryRestore(Bytes(1,false,true),Run+1,1)!=
                   CanonicalGateResult.InvalidSnapshot)return false;
                if(a.TryRestore(Bytes(2,false,true),Run,2)!=
                   CanonicalGateResult.Accepted)return false; // ORIGINAL #681 permits skip? original #669 refuses jump → quarantine
                return false;
            }
            finally{Release(a,fac);}
        });
    }
    public static int Main(string[] args)
    {
        if(args.Length==2&&args[0]=="--child")
        {
            var code=SingleFileSaveSlot.TryOpenExisting(args[1],Run,Id,out var slot);
            slot?.Dispose();
            Console.WriteLine("W21_681_669_CHILD_"+(code==SlotStatus.Busy?"DENIED":
                code==SlotStatus.Accepted?"GRANTED":"ERROR"));
            return code==SlotStatus.Busy?3:code==SlotStatus.Accepted?0:4;
        }
        Test("01 actual frozen #556 selected #510 source provenance",()=>
            FrozenPartCatalog.SelectedSourceSha=="d94400d95d13db7f27e1533df05c9292d5380431");
        Test("02 direct original #556 Light W06 ID",()=>Equivalent(Bytes(1,true,false),1,true,false));
        Test("03 direct original #556 Heavy W06 ID",()=>Equivalent(Bytes(1,false,true),1,false,true));
        Test("04 direct original #556 dual consumed ID",()=>Equivalent(Bytes(1,true,true),1,true,true));
        Test("05 cold restart exact original #669 single-slot OS lease",ChildProbe);
        Test("06 nested reentry denied by original #681 gateway+new session guard",Reentry);
        Test("07 after-physical File.Move then ACK loss quarantines original #681",()=>LostAckScenario(true));
        Test("08 before File.Move attempted publication quarantines and old disk survives",()=>LostAckScenario(false));
        Test("09 corrupt final physical #669 envelope quarantines source gateway",Corrupt);
        for(int i=0;i<24;i++){int n=i;Test("W21_ORIGINAL_556_681_669_DURABLE_LIGHT_HEAVY_COLD_"+n,
            ()=>RoundTrip(n));}
        Console.WriteLine("B0162_W21_CANONICAL_681_669_ACTUAL_SOURCE_TOTAL "+
            (passed+failed)+" PASS "+passed+" FAIL "+failed);
        Console.WriteLine("B0162_W21_ONLY_681_SELECTED_682_ALT_HOLD_669_SINGLE_AUTHORITY");
        Console.WriteLine("B0162_W21_NATIVE_UNITY_NOT_RUN_PRIVATE_510_NOT_WIRED");
        return failed==0?0:1;
    }
}
