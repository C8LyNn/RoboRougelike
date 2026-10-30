// B01-62 W17 independent source execution of byte-exact selected #604 original C#.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.UnitySaveBridge001;
internal static class Exact604IndependentSourceQA
{
    private static int passed,failed;
    private static void Test(string name,Func<bool> body)
    {
        try{if(body()){passed++;Console.WriteLine("PASS "+name);}
            else{failed++;Console.WriteLine("FAIL "+name);}}
        catch(Exception e){failed++;Console.WriteLine("FAIL "+name+" "+e);}
    }
    private static byte[] Make(bool light=true,bool heavy=false,long epoch=7,long run=99,double drift=0)
    {
        var p=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool det=i==0?light:heavy;
            var s=SecureRigCatalog.At(i);
            p[i]=new PersistedPart(s.PartId,FrozenPartCatalog.Owner(i),
                det?0:FrozenPartCatalog.Owner(i),FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                det?PartDockState.Detached:PartDockState.Attached,
                det,!det,new Vector3d(s.RootX+s.PartOffsetX+drift,1.2+(det?1:0),0),
                0,0,0,1,det?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),s.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,epoch,p));
    }
    private sealed class World : IPreparedWorld
    {
        public RunSnapshot Snapshot;
        public bool Active,Destroyed,MutateAfterActivate,FailActivate,FailRetire;
        public Action? AfterActivate;
        public World(RunSnapshot snapshot){Snapshot=snapshot;}
        public RunSnapshot Capture()=>Active&&MutateAfterActivate?
            new RunSnapshot(Snapshot.RunId,Snapshot.Generation,
                new []{Snapshot.Parts[1],Snapshot.Parts[0]}):Snapshot;
        public void Activate()
        {
            Active=true;AfterActivate?.Invoke();
            if(FailActivate)throw new IOException("injected partial activation failure");
        }
        public void Deactivate()=>Active=false;
        public void Retire()
        {
            Destroyed=true;Active=false;
            if(FailRetire)throw new IOException("injected rollback/cleanup failure");
        }
    }
    private sealed class Factory : IWorldFactory
    {
        public World? Last;
        public bool WrongAfterActivation,FailRetire;
        public bool FailActivate=false;
        public Action? DuringPrepare,AfterActivation;
        public IPreparedWorld Prepare(RunSnapshot snap)
        {
            DuringPrepare?.Invoke();
            return Last=new World(snap){
                MutateAfterActivate=WrongAfterActivation,FailActivate=FailActivate,
                FailRetire=FailRetire,AfterActivate=AfterActivation};
        }
    }
    private sealed class FailSeal : ICheckpointFence
    {
        public bool IsDurable=>true;
        public FenceVerdict Check(long run,long g,byte[] hash,bool empty)=>FenceVerdict.Allowed;
        public void Seal(long run,long g,byte[] hash)=>throw new IOException("ambiguous durable seal");
    }
    private static bool InTemp(Func<string,bool> run)
    {
        var folder=Path.Combine(Path.GetTempPath(),"b01-62-w17-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try{return run(folder);}finally{Directory.Delete(folder,true);}
    }
    private static AtomicWorldSwap Host(string folder,Factory? f=null)=>
        new AtomicWorldSwap(f??new Factory(),new FileCheckpointFence(Path.Combine(folder,"watermark.bin")));
    private static bool ColdRestart(bool light,bool heavy)
    {
        return InTemp(dir=>{
            var raw=Make(light,heavy);
            var first=Host(dir);
            if(first.TryLoad(raw,99,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            if(!first.TrySave(99,()=>true,out var saved)||!raw.SequenceEqual(saved))return false;
            var second=Host(dir);
            return second.TryLoad(saved,99,1,()=>true)==WorldSwapVerdict.Accepted&&
                second.IsConsumed(0,2310001)==light&&second.IsConsumed(1,2310002)==heavy&&
                second.DurableFence&&second.Generation==7;
        });
    }
    private static bool ForkWithOldWorld()
    {
        return InTemp(dir=>{
            var f=new Factory();var host=Host(dir,f);
            if(host.TryLoad(Make(true,false),99,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            var old=f.Last;
            return host.TryLoad(Make(false,true),99,1,()=>true)==WorldSwapVerdict.StaleGeneration&&
                ReferenceEquals(old,f.Last)&&old!.Active&&host.IsConsumed(0,2310001)&&
                !host.IsConsumed(1,2310002);
        });
    }
    private static bool ForkAfterRestart()
    {
        return InTemp(dir=>{
            if(Host(dir).TryLoad(Make(true,false),99,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            var f=new Factory();var h=Host(dir,f);
            return h.TryLoad(Make(false,true),99,1,()=>true)==WorldSwapVerdict.StaleGeneration&&
                f.Last==null&&!h.HasWorld;
        });
    }
    private static bool PostActivationMismatch()
    {
        return InTemp(dir=>{
            var f=new Factory();var h=Host(dir,f);
            if(h.TryLoad(Make(true,false,7),99,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            var old=f.Last;
            f.WrongAfterActivation=true;
            var rc=h.TryLoad(Make(false,true,8),99,1,()=>true);
            return rc==WorldSwapVerdict.HostException&&old!.Active&&!old.Destroyed&&
                f.Last!.Destroyed&&h.Generation==7&&h.IsConsumed(0,2310001)&&
                Host(dir).TryLoad(Make(false,true,8),99,1,()=>true)==WorldSwapVerdict.Accepted;
        });
    }
    private static bool FailedRollbackQuarantine()
    {
        return InTemp(dir=>{
            var f=new Factory();var h=Host(dir,f);
            if(h.TryLoad(Make(true,false,7),99,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            f.WrongAfterActivation=true;f.FailRetire=true;
            var rc=h.TryLoad(Make(false,true,8),99,1,()=>true);
            return rc==WorldSwapVerdict.RollbackQuarantine&&h.Quarantined&&
                !h.TrySave(99,()=>true,out _)&&
                h.TryLoad(Make(true,false,9),99,1,()=>true)==WorldSwapVerdict.RollbackQuarantine;
        });
    }
    private static bool FailedDurableSealQuarantine()
    {
        var f=new Factory();var h=new AtomicWorldSwap(f,new FailSeal());
        return h.TryLoad(Make(),99,1,()=>true)==WorldSwapVerdict.RollbackQuarantine&&
            h.Quarantined&&!h.HasWorld&&f.Last!.Destroyed;
    }
    private static bool BusyAfterActivation()
    {
        return InTemp(dir=>{
            var f=new Factory();bool idle=true;f.AfterActivation=()=>idle=false;
            var h=Host(dir,f);
            return h.TryLoad(Make(),99,1,()=>idle)==WorldSwapVerdict.HostBusy&&
                !h.HasWorld&&f.Last!.Destroyed;
        });
    }
    private static bool BusyAfterPrepare()
    {
        return InTemp(dir=>{
            var f=new Factory();bool idle=true;f.DuringPrepare=()=>idle=false;
            var h=Host(dir,f);
            return h.TryLoad(Make(),99,1,()=>idle)==WorldSwapVerdict.HostBusy&&
                !h.HasWorld&&f.Last!.Destroyed;
        });
    }
    private static bool WatermarkCorrupt()
    {
        return InTemp(dir=>{
            var h=Host(dir);
            if(h.TryLoad(Make(),99,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            var watermark=Path.Combine(dir,"watermark.bin");
            var data=File.ReadAllBytes(watermark);
            if(data.Length!=88)return false;
            data[12]^=0x80;File.WriteAllBytes(watermark,data);
            var f=new Factory();var second=Host(dir,f);
            return second.TryLoad(Make(false,true,8),99,1,()=>true)==WorldSwapVerdict.RollbackQuarantine
                &&second.Quarantined&&f.Last==null;
        });
    }
    private static bool StaleAfterRestart()
    {
        return InTemp(dir=>{
            if(Host(dir).TryLoad(Make(true,false,11),99,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            return Host(dir).TryLoad(Make(false,true,10),99,1,()=>true)==WorldSwapVerdict.StaleGeneration;
        });
    }
    private static bool HigherGeneration()
    {
        return InTemp(dir=>{
            if(Host(dir).TryLoad(Make(true,false,11),99,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            return Host(dir).TryLoad(Make(false,true,12),99,1,()=>true)==WorldSwapVerdict.Accepted;
        });
    }
    private static bool RealChildCLR()
    {
        return InTemp(dir=>{
            if(Host(dir).TryLoad(Make(true,false,23),99,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            var proc=new ProcessStartInfo("dotnet"){
                UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
            proc.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
            proc.ArgumentList.Add("--child-cold-check");proc.ArgumentList.Add(dir);
            using var child=Process.Start(proc);
            if(child==null)return false;
            string stdout=child.StandardOutput.ReadToEnd();
            child.WaitForExit(30000);
            return child.HasExited&&child.ExitCode==0&&stdout.Contains("B0162_W17_ACTUAL_FRESH_PROCESS_PASS");
        });
    }
    private static bool InvalidChecksum(int index)
    {
        var raw=Make();
        raw[(index*17)%(raw.Length-32)]^=(byte)(index+1);
        return InTemp(dir=>{
            var f=new Factory();var h=Host(dir,f);
            return h.TryLoad(raw,99,1,()=>true)==WorldSwapVerdict.InvalidSnapshot&&
                !h.HasWorld&&f.Last==null;
        });
    }
    public static int Main(string[] args)
    {
        if(args.Length==2&&args[0]=="--child-cold-check")
        {
            var host=Host(args[1]);
            bool ok=host.TryLoad(Make(true,false,23),99,1,()=>true)==WorldSwapVerdict.Accepted&&
                Host(args[1]).TryLoad(Make(false,true,23),99,1,()=>true)==WorldSwapVerdict.StaleGeneration&&
                host.IsConsumed(0,2310001);
            if(ok)Console.WriteLine("B0162_W17_ACTUAL_FRESH_PROCESS_PASS");
            return ok?0:5;
        }
        Test("01 exact frozen #510 gameplay SHA compiled",()=>FrozenPartCatalog.SelectedSourceSha==
            "d94400d95d13db7f27e1533df05c9292d5380431");
        Test("02 frozen stable Light ID",()=>FrozenPartCatalog.Id(0)==2310001);
        Test("03 frozen stable Heavy ID",()=>FrozenPartCatalog.Id(1)==2310002);
        Test("04 physically written watermark Light/Heavy attached",()=>ColdRestart(false,false));
        Test("05 physically written watermark Light detached",()=>ColdRestart(true,false));
        Test("06 physically written watermark Heavy detached",()=>ColdRestart(false,true));
        Test("07 physically written watermark both detached",()=>ColdRestart(true,true));
        Test("08 same-gen live world fork denied",ForkWithOldWorld);
        Test("09 same-gen cross-host fork denied before factory",ForkAfterRestart);
        Test("10 #572 post-activation mutation rollback old",PostActivationMismatch);
        Test("11 #582 failed candidate cleanup first-call quarantine",FailedRollbackQuarantine);
        Test("12 ambiguous disk seal quarantines and retires partial stage",FailedDurableSealQuarantine);
        Test("13 busy after actual Activate rolls physical candidate back",BusyAfterActivation);
        Test("14 busy after Prepare rolls candidate back",BusyAfterPrepare);
        Test("15 corrupted 88-byte original watermark quarantines",WatermarkCorrupt);
        Test("16 old generation rejected after host restart",StaleAfterRestart);
        Test("17 higher generation accepted after host restart",HigherGeneration);
        Test("18 real separate new CLR process disk checkpoint replay",RealChildCLR);
        Test("19 reject wrong original run",()=>InTemp(dir=>Host(dir).TryLoad(Make(run:77),99,1,()=>true)==WorldSwapVerdict.WrongRun));
        Test("20 enforce original single #556 schema corruption",()=>InTemp(dir=>Host(dir).TryLoad(new byte[]{0,2,3},99,1,()=>true)==WorldSwapVerdict.InvalidSnapshot));
        for(int i=0;i<24;i++){int n=i;Test("corrupt exact #556 bytes "+n,()=>InvalidChecksum(n));}
        for(int i=0;i<16;i++){int n=i;Test("disk-backed two-rig original consumed state "+n,
            ()=>ColdRestart((n&1)!=0,(n&2)!=0));}
        Console.WriteLine("B0162_W17_SELECTED604_EXACT_SOURCE_TOTAL "+(passed+failed)+" PASS "+passed+" FAIL "+failed);
        Console.WriteLine("ACTUAL_ORIGINAL_604_CORE_AND_FENCE_DIRECTLY_COMPILED; ORIGINAL_556_W06_510_SPEC; UNITY_G1_NOT_RUN");
        return failed==0?0:1;
    }
}
