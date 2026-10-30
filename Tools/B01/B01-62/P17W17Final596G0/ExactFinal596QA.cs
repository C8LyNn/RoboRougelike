// Independent B01-62 W17 direct original frozen #596 runtime C#9 source + original #556, #510/W06.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.UnitySaveBridge001;
internal static class ExactFinal596QA
{
    static int pass,fail;
    static void Case(string name,Func<bool> action)
    {
        try{if(action()){pass++;Console.WriteLine("PASS "+name);}
            else{fail++;Console.WriteLine("FAIL "+name);}}
        catch(Exception e){fail++;Console.WriteLine("FAIL "+name+" "+e);}
    }
    static byte[] Make(bool light=true,bool heavy=false,long generation=7,long run=99)
    {
        var parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool det=i==0?light:heavy;var spec=SecureRigCatalog.At(i);
            parts[i]=new PersistedPart(spec.PartId,FrozenPartCatalog.Owner(i),
                det?0:FrozenPartCatalog.Owner(i),
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                det?PartDockState.Detached:PartDockState.Attached,
                det,!det,new Vector3d(spec.RootX+spec.PartOffsetX,1.2+(det?1:0),0),
                0,0,0,1,det?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),spec.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,generation,parts));
    }
    sealed class World : IPreparedWorld
    {
        public RunSnapshot S;
        public bool Active,Retired,CorruptOnActivate,FailRetire;
        public World(RunSnapshot s){S=s;}
        public RunSnapshot Capture()
        {
            if(!Active||!CorruptOnActivate)return S;
            var data=S.Parts;
            var t=data[0];data[0]=data[1];data[1]=t;
            return new RunSnapshot(S.RunId,S.Generation,data);
        }
        public void Activate()=>Active=true;
        public void Deactivate()=>Active=false;
        public void Retire(){Retired=true;Active=false;if(FailRetire)throw new IOException("rollback fault");}
    }
    sealed class Factory : IWorldFactory
    {
        public World? Last;
        public bool CorruptNew=false,FailRetire=false;
        public int Calls;
        public IPreparedWorld Prepare(RunSnapshot s)
        {Calls++;return Last=new World(s){CorruptOnActivate=CorruptNew,FailRetire=FailRetire};}
    }
    static bool Scratch(Func<string,bool> check)
    {
        var dir=Path.Combine(Path.GetTempPath(),"b0162-final596-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try{return check(dir);}finally{Directory.Delete(dir,true);}
    }
    static AtomicWorldSwap Host(string dir,Factory? f=null)=>
        new AtomicWorldSwap(f??new Factory(),new FileCheckpointStore(Path.Combine(dir,"slot.commit")));
    static bool RoundTrip(bool light,bool heavy)
    {
        return Scratch(dir=>{
            var b=Make(light,heavy);
            var host=Host(dir);
            if(host.TryLoad(b,99,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            if(!host.IsConsumed(0,2310001).Equals(light)||!host.IsConsumed(1,2310002).Equals(heavy))return false;
            var newProcessHost=Host(dir);
            return newProcessHost.TryLoad(b,99,1,()=>true)==WorldSwapVerdict.Accepted&&
                newProcessHost.IsConsumed(0,2310001)==light&&
                newProcessHost.IsConsumed(1,2310002)==heavy;
        });
    }
    static bool CrossProcess()
    {
        return Scratch(dir=>{
            if(Host(dir).TryLoad(Make(true,false,23),99,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            var spec=new ProcessStartInfo("dotnet") {UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
            spec.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
            spec.ArgumentList.Add("--verify-new-process");spec.ArgumentList.Add(dir);
            using var process=Process.Start(spec);if(process==null)return false;
            string stdout=process.StandardOutput.ReadToEnd();
            process.WaitForExit(30000);
            return process.HasExited&&process.ExitCode==0&&stdout.Contains("B0162_FINAL596_CHILD_CLR_RESTART_PASS");
        });
    }
    static bool SaveBumpsGeneration()
    {
        return Scratch(dir=>{
            var h=Host(dir);
            if(h.TryLoad(Make(true,false,7),99,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            if(!h.TrySave(99,()=>true,out var a)||!SnapshotCodec.TryDecode(a,out RunSnapshot? p)||p==null||p.Generation!=8)return false;
            if(!h.TrySave(99,()=>true,out var b)||!SnapshotCodec.TryDecode(b,out RunSnapshot? next)||next==null)return false;
            return next.Generation==9&&next.Parts[0].Id==2310001&&next.Parts[1].Id==2310002;
        });
    }
    static bool CompareAndSwap()
    {
        return Scratch(dir=>{
            var store=new FileCheckpointStore(Path.Combine(dir,"slot.commit"));
            if(!store.Read(99,out long g,out string h)||g!=0||h!="")return false;
            string one=new string('a',64),two=new string('b',64);
            if(!store.Commit(99,0,"",7,one))return false;
            if(store.Commit(99,0,"",8,two))return false;
            if(!store.Read(99,out g,out h)||g!=7||h!=one)return false;
            if(!store.Commit(99,7,one,8,two))return false;
            return store.Read(99,out g,out h)&&g==8&&h==two;
        });
    }
    static bool ForkDenied()
    {
        return Scratch(dir=>{
            var old=Host(dir);
            if(old.TryLoad(Make(true,false,7),99,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            var factory=new Factory();var newcomer=Host(dir,factory);
            return newcomer.TryLoad(Make(false,true,7),99,1,()=>true)==WorldSwapVerdict.StaleGeneration&&
                factory.Calls==0&&!newcomer.HasWorld;
        });
    }
    static bool PostActivationRollback()
    {
        return Scratch(dir=>{
            var f=new Factory();var h=Host(dir,f);
            if(h.TryLoad(Make(true,false,7),99,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            var old=f.Last;f.CorruptNew=true;
            var r=h.TryLoad(Make(false,true,8),99,1,()=>true);
            return r==WorldSwapVerdict.HostException&&old!.Active&&!old.Retired&&
                f.Last!.Retired&&h.Generation==7&&h.IsConsumed(0,2310001);
        });
    }
    static bool FailedCleanupQuarantine()
    {
        return Scratch(dir=>{
            var f=new Factory();var h=Host(dir,f);
            if(h.TryLoad(Make(true,false,7),99,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            f.CorruptNew=true;f.FailRetire=true;
            var verdict=h.TryLoad(Make(false,true,8),99,1,()=>true);
            return verdict==WorldSwapVerdict.RollbackQuarantine&&h.Quarantined&&
                !h.TrySave(99,()=>true,out _)&&
                h.TryLoad(Make(false,true,9),99,1,()=>true)==WorldSwapVerdict.RollbackQuarantine;
        });
    }
    static bool CorruptedSidecarFailClosed()
    {
        return Scratch(dir=>{
            if(Host(dir).TryLoad(Make(),99,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            var file=Path.Combine(dir,"slot.commit");File.WriteAllText(file,"FOREIGN-MODIFIED-COMMIT");
            var f=new Factory();var h=Host(dir,f);
            return h.TryLoad(Make(false,true,8),99,1,()=>true)==WorldSwapVerdict.RollbackQuarantine&&
                h.Quarantined&&f.Calls==0;
        });
    }
    static bool Mutation(int i)
    {
        return Scratch(dir=>{
            var b=Make();b[(i*13)%(b.Length-32)]^=(byte)(i+1);
            return Host(dir).TryLoad(b,99,1,()=>true)==WorldSwapVerdict.InvalidSnapshot;
        });
    }
    public static int Main(string[] args)
    {
        if(args.Length==2&&args[0]=="--verify-new-process")
        {
            var newHost=Host(args[1]);
            var result=newHost.TryLoad(Make(true,false,23),99,1,()=>true);
            var fork=Host(args[1]).TryLoad(Make(false,true,23),99,1,()=>true);
            if(result==WorldSwapVerdict.Accepted&&fork==WorldSwapVerdict.StaleGeneration&&
                newHost.IsConsumed(0,2310001))
            {Console.WriteLine("B0162_FINAL596_CHILD_CLR_RESTART_PASS");return 0;}
            return 2;
        }
        Case("01 exact frozen original #510 SHA",()=>FrozenPartCatalog.SelectedSourceSha==
            "d94400d95d13db7f27e1533df05c9292d5380431");
        Case("02 original stable Light ID",()=>FrozenPartCatalog.Id(0)==2310001);
        Case("03 original stable Heavy ID",()=>FrozenPartCatalog.Id(1)==2310002);
        Case("04 Light Heavy both attached actual source cold",()=>RoundTrip(false,false));
        Case("05 Light detached Heavy attached actual source cold",()=>RoundTrip(true,false));
        Case("06 Light attached Heavy detached actual source cold",()=>RoundTrip(false,true));
        Case("07 both detached original consumed ID source cold",()=>RoundTrip(true,true));
        Case("08 persistent source generation grows for new saves",SaveBumpsGeneration);
        Case("09 compare and swap file watermark denies stale writer",CompareAndSwap);
        Case("10 same-generation different-world cross-host fork denied",ForkDenied);
        Case("11 #572 post-activation original world rollback",PostActivationRollback);
        Case("12 #582 failed cleanup immediately quarantine",FailedCleanupQuarantine);
        Case("13 corrupted original on-disk watermark quarantine",CorruptedSidecarFailClosed);
        Case("14 real new CLR process replays identical world and rejects ID fork",CrossProcess);
        Case("15 original #556 rejects foreign run",()=>Scratch(dir=>Host(dir).TryLoad(Make(run:98),99,1,()=>true)==WorldSwapVerdict.WrongRun));
        Case("16 original #556 rejects invalid bytes",()=>Scratch(dir=>Host(dir).TryLoad(new byte[]{0,1,2},99,1,()=>true)==WorldSwapVerdict.InvalidSnapshot));
        for(int i=0;i<24;i++){int n=i;Case("corrupted original #556 payload "+n,()=>Mutation(n));}
        for(int i=0;i<16;i++){int n=i;Case("deterministic original Light Heavy Save/Load "+n,
            ()=>RoundTrip((n&1)!=0,(n&2)!=0));}
        Console.WriteLine("B0162_W17_FINAL596_ORIGINAL_SOURCE_TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
        Console.WriteLine("EXACT_ORIGINAL_596_CORE_W06_556_COMPILED; REAL_UNITY_G1_NOT_RUN");
        return fail==0?0:1;
    }
}
