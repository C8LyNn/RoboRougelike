using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;
using B01.SecureReattachV2;
using B01.W16Selected587Repair;

internal static class Program
{
    private static int good,bad;
    private static void Check(string s,Func<bool> run)
    {
        try {if(run()){good++;Console.WriteLine("PASS "+s);}else{bad++;Console.WriteLine("FAIL "+s);}}
        catch(Exception e){bad++;Console.WriteLine("FAIL "+s+" "+e);}
    }
    private static RunSnapshot Snap(bool light=true,bool heavy=false,long gen=1,long run=7,double offset=0)
    {
        var p=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool det=i==0?light:heavy;
            var spec=SecureRigCatalog.At(i);
            p[i]=new PersistedPart(spec.PartId,FrozenPartCatalog.Owner(i),
                det?0:FrozenPartCatalog.Owner(i),FrozenPartCatalog.Asset(i),
                FrozenPartCatalog.Socket(i),det?PartDockState.Detached:PartDockState.Attached,
                det,!det,new Vector3d(spec.RootX+spec.PartOffsetX+offset,1.2+(det?1:0),0),
                0,0,0,1,det?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),spec.PartMassKg);
        }
        return new RunSnapshot(run,gen,p);
    }
    private static byte[] Wire(bool light=true,bool heavy=false,long gen=1,long run=7,double offset=0)
        =>SnapshotCodec.Encode(Snap(light,heavy,gen,run,offset));
    private sealed class W : IPreparedWorld
    {
        public RunSnapshot Data;
        public bool Active,Destroyed, FailActivate,FailDeactivate,FailRetire;
        public bool MutateOnActivate,ThrowOnCaptureAfterActivate;
        public int ActivationCount;
        public W(RunSnapshot p){Data=p;}
        public RunSnapshot Capture()
        {
            if(Active&&ThrowOnCaptureAfterActivate)throw new IOException("capture after activation");
            return Data;
        }
        public void Activate()
        {
            ActivationCount++;Active=true;
            if(MutateOnActivate)Data=Snap(!Data.Parts[0].Consumed,!Data.Parts[1].Consumed,Data.Generation,Data.RunId,13);
            if(FailActivate)throw new IOException("bad activate");
        }
        public void Deactivate()
        {
            Active=false;
            if(FailDeactivate)throw new IOException("bad deactivate");
        }
        public void Retire()
        {
            Destroyed=true;Active=false;
            if(FailRetire)throw new IOException("bad retire");
        }
    }
    private sealed class F : IWorldFactory
    {
        public W? Last;
        public bool FailFactory, FailActivate,FailRetire,MutateAfterActivate,ThrowAfterActivate;
        public Action? DuringPrepare;
        public IPreparedWorld Prepare(RunSnapshot s)
        {
            DuringPrepare?.Invoke();
            if(FailFactory)throw new IOException("bad prepare");
            Last=new W(s){FailActivate=FailActivate,FailRetire=FailRetire,
                MutateOnActivate=MutateAfterActivate,ThrowOnCaptureAfterActivate=ThrowAfterActivate};
            return Last;
        }
    }
    private static bool InTemp(Func<string,bool> f)
    {
        string dir=Path.Combine(Path.GetTempPath(),"b0162w16-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try{return f(dir);}finally{Directory.Delete(dir,true);}
    }
    private static bool LoadFour(bool a,bool b)
        =>InTemp(dir=>{
            var f=new F();var host=new AtomicWorldSwap(f,new FileGenerationAuthority(dir));
            var bytes=Wire(a,b);
            if(host.TryLoad(bytes,7,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            if(!host.TrySave(7,()=>true,out byte[] raw)||!bytes.SequenceEqual(raw))return false;
            var second=new AtomicWorldSwap(new F(),new FileGenerationAuthority(dir));
            return second.TryLoad(raw,7,1,()=>true)==WorldSwapVerdict.Accepted &&
                second.IsConsumed(0,2310001)==a && second.IsConsumed(1,2310002)==b &&
                second.Generation==1;
        });
    private static bool ForkRefusal()
        =>InTemp(dir=>{
            var f=new F();var h=new AtomicWorldSwap(f,new FileGenerationAuthority(dir));
            if(h.TryLoad(Wire(true,false,5),7,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            var old=f.Last;
            var rc=h.TryLoad(Wire(false,true,5),7,1,()=>true);
            var restarted=new AtomicWorldSwap(new F(),new FileGenerationAuthority(dir));
            var second=restarted.TryLoad(Wire(false,true,5),7,1,()=>true);
            return rc==WorldSwapVerdict.StaleGeneration &&second==WorldSwapVerdict.StaleGeneration&&
                old!=null&&old.Active&& !old.Destroyed&&h.IsConsumed(0,2310001);
        });
    private static bool OldGenerationAfterRestart()
        =>InTemp(dir=>{
            var h=new AtomicWorldSwap(new F(),new FileGenerationAuthority(dir));
            if(h.TryLoad(Wire(true,false,8),7,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            var restart=new AtomicWorldSwap(new F(),new FileGenerationAuthority(dir));
            return restart.TryLoad(Wire(false,true,7),7,1,()=>true)==WorldSwapVerdict.StaleGeneration;
        });
    private static bool NewGenerationAfterRestart()
        =>InTemp(dir=>{
            var h=new AtomicWorldSwap(new F(),new FileGenerationAuthority(dir));
            if(h.TryLoad(Wire(true,false,8),7,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            var restart=new AtomicWorldSwap(new F(),new FileGenerationAuthority(dir));
            return restart.TryLoad(Wire(false,true,9),7,1,()=>true)==WorldSwapVerdict.Accepted &&
                restart.IsConsumed(1,2310002) &&!restart.IsConsumed(0,2310001);
        });
    private static bool ActivationRevalidate()
        =>InTemp(dir=>{
            var f=new F();var h=new AtomicWorldSwap(f,new FileGenerationAuthority(dir));
            if(h.TryLoad(Wire(true,false,1),7,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            var old=f.Last;
            f.MutateAfterActivate=true;
            var rc=h.TryLoad(Wire(false,true,2),7,1,()=>true);
            return rc==WorldSwapVerdict.HostException&&old!=null&&old.Active&&!old.Destroyed&&
                f.Last!=null&&f.Last.Destroyed&&h.Generation==1&&h.IsConsumed(0,2310001)&&
                new FileGenerationAuthority(dir).Evaluate(7,2,Wire(false,true,2))==AuthorityDecision.Advance;
        });
    private static bool FailedCleanupQuarantines()
        =>InTemp(dir=>{
            var f=new F();var h=new AtomicWorldSwap(f,new FileGenerationAuthority(dir));
            if(h.TryLoad(Wire(true,false,1),7,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            f.MutateAfterActivate=true;f.FailRetire=true;
            var rc=h.TryLoad(Wire(false,true,2),7,1,()=>true);
            return rc==WorldSwapVerdict.RollbackQuarantine &&h.Quarantined&&
                h.TryLoad(Wire(false,true,3),7,1,()=>true)==WorldSwapVerdict.RollbackQuarantine&&
                !h.TrySave(7,()=>true,out _);
        });
    private static bool InterruptedOldReactivateQuarantines()
        =>InTemp(dir=>{
            var f=new F();var h=new AtomicWorldSwap(f,new FileGenerationAuthority(dir));
            if(h.TryLoad(Wire(true,false,1),7,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            f.Last!.FailActivate=true;
            f.FailActivate=true;
            var rc=h.TryLoad(Wire(false,true,2),7,1,()=>true);
            return rc==WorldSwapVerdict.RollbackQuarantine &&h.Quarantined;
        });
    private static bool BusyDuringPrepare()
        =>InTemp(dir=>{
            var f=new F();bool idle=true;f.DuringPrepare=()=>idle=false;
            var h=new AtomicWorldSwap(f,new FileGenerationAuthority(dir));
            var rc=h.TryLoad(Wire(),7,1,()=>idle);
            return rc==WorldSwapVerdict.HostBusy&&!h.HasWorld&&
                f.Last!=null&&f.Last.Destroyed&&!h.Quarantined;
        });
    private static bool ReentrantLoad()
        =>InTemp(dir=>{
            var h=new AtomicWorldSwap(new F(),new FileGenerationAuthority(dir));
            WorldSwapVerdict nested=WorldSwapVerdict.Accepted;
            int called=0;
            var outc=h.TryLoad(Wire(),7,1,()=>{
                if(called++==0)nested=h.TryLoad(Wire(false,true,2),7,1,()=>true);
                return true;
            });
            return outc==WorldSwapVerdict.Accepted&&nested==WorldSwapVerdict.HostBusy;
        });
    private static bool FaultedJournalReject()
        =>InTemp(dir=>{
            var file=Path.Combine(dir,"epoch-00000000000000000005.commit");
            File.WriteAllText(file,"stolen");
            var h=new AtomicWorldSwap(new F(),new FileGenerationAuthority(dir));
            return h.TryLoad(Wire(true,false,6),7,1,()=>true)==WorldSwapVerdict.RollbackQuarantine;
        });
    private static bool UnownedEmptyDirReject()
        =>InTemp(dir=>{
            Directory.CreateDirectory(Path.Combine(dir,"alien-empty"));
            var h=new AtomicWorldSwap(new F(),new FileGenerationAuthority(dir));
            return h.TryLoad(Wire(),7,1,()=>true)==WorldSwapVerdict.RollbackQuarantine;
        });
    private static bool FailedDurableCommitQuarantines()
        =>InTemp(dir=>{
            var f=new F();var h=new AtomicWorldSwap(f,new FileGenerationAuthority(dir));
            if(h.TryLoad(Wire(true,false,3),7,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            var old=f.Last;
            // Real fault is injected AFTER validated authority preflight but BEFORE
            // activation/physical publication; final commit must quarantine and
            // restore the previous active world rather than report success.
            f.DuringPrepare=()=>File.WriteAllText(Path.Combine(dir,"UNTRUSTED"),"foreign");
            var rc=h.TryLoad(Wire(false,true,4),7,1,()=>true);
            return rc==WorldSwapVerdict.RollbackQuarantine &&h.Quarantined &&
                old!=null&&old.Active &&h.Generation==3&&h.IsConsumed(0,2310001);
        });
    private static bool RealChildProcessReplay()
        =>InTemp(dir=>{
            var payload=Wire(true,false,9);
            var f=new F();var h=new AtomicWorldSwap(f,new FileGenerationAuthority(dir));
            if(h.TryLoad(payload,7,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            string bin=Assembly.GetExecutingAssembly().Location;
            var start=new ProcessStartInfo("dotnet");
            start.ArgumentList.Add(bin);start.ArgumentList.Add("--verify-restart");start.ArgumentList.Add(dir);
            start.RedirectStandardOutput=true;start.RedirectStandardError=true;
            start.UseShellExecute=false;
            using var child=Process.Start(start);
            if(child==null)return false;
            var stdout=child.StandardOutput.ReadToEnd();
            child.WaitForExit(30000);
            return child.HasExited&&child.ExitCode==0&&stdout.Contains("ACTUAL_FRESH_PROCESS_DURABLE_REPLAY_PASS");
        });
    public static int Main(string[] args)
    {
        if(args.Length==2 &&args[0]=="--verify-restart")
        {
            try
            {
                var authority=new FileGenerationAuthority(args[1]);
                var h=new AtomicWorldSwap(new F(),authority);
                bool good=h.TryLoad(Wire(true,false,9),7,1,()=>true)==WorldSwapVerdict.Accepted &&
                    h.TryLoad(Wire(false,true,9),7,1,()=>true)==WorldSwapVerdict.StaleGeneration &&
                    h.IsConsumed(0,2310001);
                if(good)Console.WriteLine("ACTUAL_FRESH_PROCESS_DURABLE_REPLAY_PASS");
                return good?0:3;
            }
            catch{return 4;}
        }
        Check("01 frozen #556 original selected source anchored",()=>FrozenPartCatalog.SelectedSourceSha=="d94400d95d13db7f27e1533df05c9292d5380431");
        Check("02 original Light ID",()=>FrozenPartCatalog.Id(0)==2310001);
        Check("03 original Heavy ID",()=>FrozenPartCatalog.Id(1)==2310002);
        Check("04 cold restart Light attached Heavy attached",()=>LoadFour(false,false));
        Check("05 cold restart Light detached Heavy attached",()=>LoadFour(true,false));
        Check("06 cold restart Light attached Heavy detached",()=>LoadFour(false,true));
        Check("07 cold restart both detached",()=>LoadFour(true,true));
        Check("08 same generation different-world live fork rejected",ForkRefusal);
        Check("09 old generation on new host rejected by persisted authority",OldGenerationAfterRestart);
        Check("10 next generation after new host accepted",NewGenerationAfterRestart);
        Check("11 SetVisible/Activate mutates world post-activation -> rollback old",ActivationRevalidate);
        Check("12 failed Retire/Abort in cleanup gives Quarantine same call",FailedCleanupQuarantines);
        Check("13 failed old reactivation rollback gives Quarantine same call",InterruptedOldReactivateQuarantines);
        Check("14 host becomes busy while preparing, candidate retired",BusyDuringPrepare);
        Check("15 nested reentrant Load blocked",ReentrantLoad);
        Check("16 damaged persisted commit record fails closed",FaultedJournalReject);
        Check("17 foreign empty directory in commit journal fails closed",UnownedEmptyDirReject);
        Check("18 failed/uncertain durable commit quarantines and restores old",FailedDurableCommitQuarantines);
        Check("19 real separate CLR process sees generation and digest authority",RealChildProcessReplay);
        for(int i=0;i<32;i++){int n=i;Check("20+ corrupted selected #556 payload "+n,()=>InTemp(dir=>{
            var b=Wire(true,false,1);b[(n*17)%(b.Length-32)]^=(byte)(n+1);
            var h=new AtomicWorldSwap(new F(),new FileGenerationAuthority(dir));
            return h.TryLoad(b,7,1,()=>true)==WorldSwapVerdict.InvalidSnapshot &&!h.HasWorld;
        }));}
        for(int i=0;i<32;i++){int n=i;Check("20+ sequential deterministic run "+n,()=>InTemp(dir=>{
            var f=new F();var h=new AtomicWorldSwap(f,new FileGenerationAuthority(dir));
            var b=Wire((n&1)!=0,(n&2)!=0,n+1);
            if(h.TryLoad(b,7,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            var fresh=new AtomicWorldSwap(new F(),new FileGenerationAuthority(dir));
            return fresh.TryLoad(b,7,1,()=>true)==WorldSwapVerdict.Accepted&&
                fresh.IsConsumed(0,2310001)==((n&1)!=0)&&
                fresh.IsConsumed(1,2310002)==((n&2)!=0);
        }));}
        Console.WriteLine("W16_SELECTED_587_SOURCE_TOTAL "+(good+bad)+" PASS "+good+" FAIL "+bad+
          " ORIGINAL_556_W06_R2SOURCE COMPILED UNITY_G1_NOT_RUN");
        return bad==0?0:1;
    }
}
