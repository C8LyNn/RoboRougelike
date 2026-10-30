// Execute actual unchanged original #596 in both the historical RED and versioned lease GREEN.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;
using B01.SecureReattachV2;
using B01.UnitySaveBridge001;
using B01.SingleSlotLease001;
internal static class Program
{
    static int pass,fail;
    static void Case(string name,Func<bool> f)
    {
        try{if(f()){pass++;Console.WriteLine("PASS "+name);}
            else{fail++;Console.WriteLine("FAIL "+name);}}
        catch(Exception ex){fail++;Console.WriteLine("FAIL "+name+" "+ex);}
    }
    static byte[] Make(bool light=true,bool heavy=false,long g=10,long run=19)
    {
        var p=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool det=i==0?light:heavy;
            var s=SecureRigCatalog.At(i);
            p[i]=new PersistedPart(s.PartId,FrozenPartCatalog.Owner(i),
                det?0:FrozenPartCatalog.Owner(i),FrozenPartCatalog.Asset(i),
                FrozenPartCatalog.Socket(i),det?PartDockState.Detached:PartDockState.Attached,
                det,!det,new Vector3d(s.RootX+s.PartOffsetX,1.2+(det?1:0),0),
                0,0,0,1,det?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),s.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,g,p));
    }
    sealed class World : IPreparedWorld
    {
        public RunSnapshot Data;public bool Active;
        public World(RunSnapshot s){Data=s;}
        public RunSnapshot Capture()=>Data;
        public void Activate()=>Active=true;
        public void Deactivate()=>Active=false;
        public void Retire()=>Active=false;
    }
    sealed class Factory : IWorldFactory
    {
        public IPreparedWorld Prepare(RunSnapshot s)=>new World(s);
    }
    static bool Temp(Func<string,bool> f)
    {
        var d=Path.Combine(Path.GetTempPath(),"B0162W18-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        try{return f(d);}finally{Directory.Delete(d,true);}
    }
    static bool OriginalMultiHostRed()
    {
        return Temp(dir=>{
            string path=Path.Combine(dir,"b0123_one_save_host_v2.checkpoint");
            var a=new AtomicWorldSwap(new Factory(),new FileCheckpointStore(path));
            var b=new AtomicWorldSwap(new Factory(),new FileCheckpointStore(path));
            if(a.TryLoad(Make(true,false,10),19,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            if(b.TryLoad(Make(false,true,11),19,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
            if(!a.TrySave(19,()=>true,out var resurrected)||!SnapshotCodec.TryDecode(resurrected,out var snap)||snap==null||snap.Generation!=12)return false;
            var c=new AtomicWorldSwap(new Factory(),new FileCheckpointStore(path));
            return c.TryLoad(resurrected,19,1,()=>true)==WorldSwapVerdict.Accepted&&
                c.IsConsumed(0,2310001)&&!c.IsConsumed(1,2310002)&&
                b.IsConsumed(1,2310002);
        });
    }
    static bool SafeSameProcess()
    {
        return Temp(dir=>{
            if(!LeasedAtomicWorldHost.TryOpen(new Factory(),dir,"b0123_one_save_host_v2",out var a)||a==null)return false;
            using(a)
            {
                if(a.TryLoad(Make(true,false,10),19,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
                bool admitted=LeasedAtomicWorldHost.TryOpen(new Factory(),dir,"b0123_one_save_host_v2",out var duplicate);
                duplicate?.Dispose();
                if(admitted||!a.Owns)return false;
                if(!a.TrySave(19,()=>true,out var bytes)||!SnapshotCodec.TryDecode(bytes,out var saved)||saved==null||saved.Generation!=11)return false;
                a.Dispose();
                if(a.Owns||a.TrySave(19,()=>true,out _)||
                   a.TryLoad(bytes,19,1,()=>true)!=WorldSwapVerdict.RollbackQuarantine)return false;
                if(!LeasedAtomicWorldHost.TryOpen(new Factory(),dir,"b0123_one_save_host_v2",out var later)||later==null)return false;
                using(later)
                {
                    return later.TryLoad(bytes,19,1,()=>true)==WorldSwapVerdict.Accepted&&
                        later.IsConsumed(0,2310001)&&!later.IsConsumed(1,2310002);
                }
            }
        });
    }
    static bool CrossProcess()
    {
        return Temp(dir=>{
            if(!LeasedAtomicWorldHost.TryOpen(new Factory(),dir,"b0123_one_save_host_v2",out var host)||host==null)return false;
            using(host)
            {
                if(!Child(dir,"DENIED"))return false;
                host.Dispose();
                return Child(dir,"ALLOWED");
            }
        });
    }
    static bool Child(string dir,string expected)
    {
        var p=new ProcessStartInfo("dotnet"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        p.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        p.ArgumentList.Add("--lease-child");
        p.ArgumentList.Add(dir);
        p.ArgumentList.Add(expected);
        using var proc=Process.Start(p);
        if(proc==null)return false;
        string output=proc.StandardOutput.ReadToEnd();
        proc.WaitForExit(30000);
        return proc.HasExited&&proc.ExitCode==0&&output.Contains("B0162_W18_CHILD_"+expected);
    }
    static bool DifferentSlots()
    {
        return Temp(dir=>{
            if(!LeasedAtomicWorldHost.TryOpen(new Factory(),dir,"slot-A",out var a)||a==null)return false;
            using(a)
            {
                if(!LeasedAtomicWorldHost.TryOpen(new Factory(),dir,"slot-B",out var b)||b==null)return false;
                using(b)return a.Owns&&b.Owns;
            }
        });
    }
    static bool RepeatedLock(int i)
    {
        return Temp(dir=>{
            var slot="original-"+i;
            if(!LeasedAtomicWorldHost.TryOpen(new Factory(),dir,slot,out var first)||first==null)return false;
            if(LeasedAtomicWorldHost.TryOpen(new Factory(),dir,slot,out var bad)){bad?.Dispose();return false;}
            first.Dispose();first.Dispose();
            if(first.Owns)return false;
            if(!LeasedAtomicWorldHost.TryOpen(new Factory(),dir,slot,out var second)||second==null)return false;
            using(second)
            {
                if(second.TryLoad(Make((i&1)!=0,(i&2)!=0,10+i),19,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
                return second.IsConsumed(0,2310001)==((i&1)!=0)&&
                    second.IsConsumed(1,2310002)==((i&2)!=0);
            }
        });
    }
    static bool Invalid(string slot)
    {
        return Temp(dir=>!SlotLease.TryAcquire(dir,slot,out var o)&&o==null);
    }
    static bool CorruptedCheckpointStillDeniesStale()
    {
        return Temp(dir=>{
            if(!LeasedAtomicWorldHost.TryOpen(new Factory(),dir,"b0123_one_save_host_v2",out var first)||first==null)return false;
            using(first)
            {
                if(first.TryLoad(Make(),19,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
                File.WriteAllText(Path.Combine(dir,"b0123_one_save_host_v2.checkpoint"),"tampered");
                return first.TryLoad(Make(false,true,11),19,1,()=>true)==WorldSwapVerdict.RollbackQuarantine&&
                    first.Quarantined;
            }
        });
    }
    static bool RejectForeignSymlink()
    {
        return Temp(dir=>{
            var leaseRoot=Path.Combine(dir,".b0162-single-host-leases");
            Directory.CreateDirectory(leaseRoot);
            string link=Path.Combine(leaseRoot,"slot.lock");
            try{File.CreateSymbolicLink(link,Path.Combine(dir,"other"));}
            catch(UnauthorizedAccessException){return true;}
            catch(PlatformNotSupportedException){return true;}
            catch(IOException){return true;}
            return !SlotLease.TryAcquire(dir,"slot",out var o)&&o==null;
        });
    }
    public static int Main(string[] args)
    {
        if(args.Length==3&&args[0]=="--lease-child")
        {
            bool opened=LeasedAtomicWorldHost.TryOpen(new Factory(),args[1],
                "b0123_one_save_host_v2",out var got);
            got?.Dispose();
            if((opened?"ALLOWED":"DENIED")==args[2])
            {Console.WriteLine("B0162_W18_CHILD_"+args[2]);return 0;}
            return 4;
        }
        Case("01 frozen #510 id",()=>FrozenPartCatalog.SelectedSourceSha=="d94400d95d13db7f27e1533df05c9292d5380431");
        Case("02 original #596 SOURCE P0 repro: resurrect stale Light world via second slot host",OriginalMultiHostRed);
        Case("03 versioned new slot lease denies second live host + stale reuse",SafeSameProcess);
        Case("04 cross process cannot steal slot before owner releases, can after",CrossProcess);
        Case("05 different save slots stay independent",DifferentSlots);
        Case("06 reject path traversal slot",()=>Invalid("../other"));
        Case("07 reject empty slot",()=>Invalid(""));
        Case("08 reject unsafe unicode slot",()=>Invalid("ąbc"));
        Case("09 reject overlong slot",()=>Invalid(new string('a',33)));
        Case("10 existing symlink lock file refuses",RejectForeignSymlink);
        Case("11 corrupt original #596 watermark quarantines",CorruptedCheckpointStillDeniesStale);
        for(int i=0;i<24;i++){int n=i;Case("race-free sequential owner lease + original #556 consumed flags "+n,()=>RepeatedLock(n));}
        Console.WriteLine("B0162_W18_SINGLE_SLOT_SOURCE_TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
        Console.WriteLine("HISTORICAL_FROZEN_596_SHARED_SLOT_RESURRECTION_SOURCE_RED_REPRODUCED_IS_NOT_RELEASE_GREEN");
        Console.WriteLine("NEW_R1_SOURCE_ONLY_NO_UNITY_NATIVE_NO_PRODUCER_ADOPTION");
        return fail==0?0:1;
    }
}
