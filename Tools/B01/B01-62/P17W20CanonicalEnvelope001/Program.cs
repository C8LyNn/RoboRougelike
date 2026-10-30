using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using B01.CanonicalAtomicSave001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

internal static class Program
{
    private const long Run=62020;
    private static readonly Guid Slot=Guid.Parse("11111111-2222-4333-8444-555555555555");
    private static int pass,fail;
    private static void Check(string id,Func<bool> f)
    {
        try{if(f()){pass++;Console.WriteLine("PASS "+id);}
            else{fail++;Console.WriteLine("FAIL "+id);}}
        catch(Exception e){fail++;Console.WriteLine("FAIL "+id+" "+e);}
    }
    private static byte[] Snapshot(long generation,bool light,bool heavy,long run=Run)
    {
        PersistedPart[] parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool detached=i==0?light:heavy;
            var spec=SecureRigCatalog.At(i);
            long owner=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(spec.PartId,owner,detached?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                detached?PartDockState.Detached:PartDockState.Attached,
                detached,!detached,
                new Vector3d(spec.RootX+spec.PartOffsetX,1.2+(detached?1:0),0),
                0,0,0,1,detached?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),spec.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,generation,parts));
    }
    private static bool Is(PersistedAuthority? p,long g,bool light,bool heavy)
    {
        if(p==null||p.Generation!=g||
           !ColdRestore.TryStage(p.Original556Bytes,out ColdRestore? cold)||
           cold==null||cold.Snapshot.Generation!=g||cold.Snapshot.RunId!=Run)
            return false;
        return cold.ContainsConsumedId(0,FrozenPartCatalog.Id(0))==light&&
            cold.ContainsConsumedId(1,FrozenPartCatalog.Id(1))==heavy;
    }
    private static bool Temp(Func<string,bool> f)
    {
        string root=Path.Combine(Path.GetTempPath(),"B0162-W20-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{return f(root);}
        finally{Directory.Delete(root,true);}
    }
    private sealed class Fault:ICommitFault
    {
        private readonly CommitFaultPhase when;
        public int hits;
        public Fault(CommitFaultPhase when){this.when=when;}
        public void At(CommitFaultPhase phase)
        {
            if(phase==when){hits++;throw new IOException("injected process-level fault");}
        }
    }
    private static bool Seed(out string root,out AtomicEnvelopeStore store)
    {
        root=Path.Combine(Path.GetTempPath(),"B0162-W20-SEED-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        if(!AtomicEnvelopeStore.TryOpen(root,Run,Slot,out AtomicEnvelopeStore? lease)||lease==null)
            throw new Exception("failed seed lease");
        store=lease;
        if(store.Commit(Snapshot(1,true,false),0,"",out PersistedAuthority? p)!=SaveCommitVerdict.Accepted||
            !Is(p,1,true,false))throw new Exception("failed seed commit");
        return true;
    }
    private static bool RoundTrip(int index)
    {
        return Temp(root=>{
            if(!AtomicEnvelopeStore.TryOpen(root,Run,Slot,out var first)||first==null)return false;
            string evidence;
            using(first)
            {
                bool a=(index&1)!=0,b=(index&2)!=0;
                if(first.Read(out var missing)!=SaveReadVerdict.Missing||missing!=null)return false;
                if(first.Commit(Snapshot(1,a,b),0,"",out var published)!=SaveCommitVerdict.Accepted||
                    published==null||published.Evidence.Length!=64)return false;
                evidence=published.Evidence;
                if(!Is(published,1,a,b))return false;
                if(first.Commit(Snapshot(2,!a,!b),1,evidence,out var second)!=SaveCommitVerdict.Accepted||
                    second==null||second.Evidence==evidence)return false;
                if(!Is(second,2,!a,!b))return false;
                evidence=second.Evidence;
                if(first.Commit(Snapshot(2,a,b),1,"wrong",out _)!=SaveCommitVerdict.Invalid)return false;
                if(first.Commit(Snapshot(3,a,b),1,evidence,out _)!=SaveCommitVerdict.Conflict)return false;
            }
            if(!AtomicEnvelopeStore.TryOpen(root,Run,Slot,out var restarted)||restarted==null)return false;
            using(restarted)
            {
                if(restarted.Read(out var latest)!=SaveReadVerdict.Accepted||latest==null||
                   latest.Evidence!=evidence||!Is(latest,2,(index&1)==0,(index&2)==0))
                    return false;
                if(restarted.Commit(Snapshot(3,(index&1)!=0,(index&2)!=0),2,
                   latest.Evidence,out var third)!=SaveCommitVerdict.Accepted||third==null)
                    return false;
                return Is(third,3,(index&1)!=0,(index&2)!=0);
            }
        });
    }
    private static bool LostAck(CommitFaultPhase phase)
    {
        string root;AtomicEnvelopeStore seed;
        Seed(out root,out seed);
        try
        {
            string previous;
            using(seed)
            {
                if(seed.Read(out var p)!=SaveReadVerdict.Accepted||p==null)return false;
                previous=p.Evidence;
            }
            var fault=new Fault(phase);
            if(!AtomicEnvelopeStore.TryOpen(root,Run,Slot,out var next,fault)||next==null)return false;
            using(next)
            {
                if(next.Commit(Snapshot(2,false,true),1,previous,out var result)!=
                   SaveCommitVerdict.Quarantined||result!=null||
                   !next.Quarantined||fault.hits!=1)return false;
                if(next.Commit(Snapshot(2,true,false),1,previous,out _)!=
                   SaveCommitVerdict.Quarantined)return false;
            }
            if(!AtomicEnvelopeStore.TryOpen(root,Run,Slot,out var cold)||cold==null)return false;
            using(cold)
            {
                var r=cold.Read(out var latest);
                if(phase==CommitFaultPhase.BeforeReplace)
                    return r==SaveReadVerdict.Accepted&&Is(latest,1,true,false);
                return r==SaveReadVerdict.Accepted&&Is(latest,2,false,true)&&
                    cold.Commit(Snapshot(2,true,false),1,previous,out _)==SaveCommitVerdict.Conflict;
            }
        }
        finally{Directory.Delete(root,true);}
    }
    private static bool Corruption()
    {
        string root;AtomicEnvelopeStore original;Seed(out root,out original);
        try
        {
            string file=Path.Combine(root,"b0162_canonical_envelope_v1",
                original.SlotFileName);
            using(original){}
            byte[] b=File.ReadAllBytes(file);b[34]^=0x5a;File.WriteAllBytes(file,b);
            if(!AtomicEnvelopeStore.TryOpen(root,Run,Slot,out var restored)||restored==null)return false;
            using(restored)
            {
                return restored.Read(out var p)==SaveReadVerdict.Corrupt&&p==null&&
                    restored.Commit(Snapshot(2,false,true),1,"",out _)==
                        SaveCommitVerdict.Invalid&&
                    restored.Commit(Snapshot(2,false,true),1,new string('0',64),out _)==
                        SaveCommitVerdict.Quarantined&&restored.Quarantined;
            }
        }
        finally{Directory.Delete(root,true);}
    }
    private static bool OtherRunOrSlot()
    {
        return Temp(root=>{
            if(!AtomicEnvelopeStore.TryOpen(root,Run,Slot,out var first)||first==null)return false;
            using(first)
            {
                if(first.Commit(Snapshot(1,true,false),0,"",out var p)!=SaveCommitVerdict.Accepted||
                   p==null)return false;
                var different=Guid.Parse("11111111-2222-4333-8444-555555555556");
                if(!AtomicEnvelopeStore.TryOpen(root,Run,different,out var s)||s==null)return false;
                using(s)
                {
                    if(s.Read(out _)!=SaveReadVerdict.Missing)return false;
                    if(s.Commit(Snapshot(1,false,true),0,"",out _)!=SaveCommitVerdict.Accepted)return false;
                }
                if(!AtomicEnvelopeStore.TryOpen(root,Run+1,Slot,out var r)||r==null)return false;
                using(r)
                {
                    if(r.Commit(Snapshot(1,true,true,Run+1),0,"",out var p2)!=SaveCommitVerdict.Accepted||
                       p2==null)return false;
                    return r.Read(out var saved)==SaveReadVerdict.Accepted&&saved!=null&&
                        SnapshotCodec.TryDecode(saved.Original556Bytes,out var v)&&
                        v!=null&&v.RunId==Run+1;
                }
            }
        });
    }
    private static bool CrossProcess()
    {
        return Temp(root=>{
            if(!AtomicEnvelopeStore.TryOpen(root,Run,Slot,out var first)||first==null)return false;
            using(first)
            {
                if(!Child(root,false))return false;
                first.Dispose();
                return Child(root,true);
            }
        });
    }
    private static bool Child(string root,bool expected)
    {
        var start=new ProcessStartInfo("dotnet"){
            UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        start.ArgumentList.Add("--child");start.ArgumentList.Add(root);
        start.ArgumentList.Add(expected?"GRANTED":"DENIED");
        using var proc=Process.Start(start);
        if(proc==null)return false;
        string output=proc.StandardOutput.ReadToEnd();
        if(!proc.WaitForExit(30000))return false;
        return proc.ExitCode==0&&output.Contains("W20_CHILD_"+(expected?"GRANTED":"DENIED"));
    }
    public static int Main(string[] args)
    {
        if(args.Length==3&&args[0]=="--child")
        {
            bool got=AtomicEnvelopeStore.TryOpen(args[1],Run,Slot,out var store);
            store?.Dispose();
            string actual=got?"GRANTED":"DENIED";
            Console.WriteLine("W20_CHILD_"+actual);
            return actual==args[2]?0:7;
        }
        Check("01 selected immutable #510 actual original ID",()=>FrozenPartCatalog.SelectedSourceSha==
            "d94400d95d13db7f27e1533df05c9292d5380431");
        Check("02 #556 exact binary consistent",()=>ColdRestore.TryStage(Snapshot(1,true,false),out var x)
            &&x!=null&&x.ContainsConsumedId(0,FrozenPartCatalog.Id(0)));
        Check("03 open denies relative root",()=>!AtomicEnvelopeStore.TryOpen("relative",Run,Slot,out _));
        Check("04 invalid zero run",()=>Temp(root=>!AtomicEnvelopeStore.TryOpen(root,0,Slot,out _)));
        Check("05 invalid empty slot",()=>Temp(root=>!AtomicEnvelopeStore.TryOpen(root,Run,Guid.Empty,out _)));
        Check("06 missing existing trusted root",()=>!AtomicEnvelopeStore.TryOpen(
            Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")),Run,Slot,out _));
        Check("07 same process competing owner denied",()=>Temp(root=>{
            if(!AtomicEnvelopeStore.TryOpen(root,Run,Slot,out var a)||a==null)return false;
            using(a)return !AtomicEnvelopeStore.TryOpen(root,Run,Slot,out _);
        }));
        Check("08 real independent child CLR crossprocess owner lock and safe recovery",CrossProcess);
        Check("09 foreign original #556 run rejected",()=>Temp(root=>{
            if(!AtomicEnvelopeStore.TryOpen(root,Run,Slot,out var a)||a==null)return false;
            using(a)return a.Commit(Snapshot(1,true,false,Run+1),0,"",out _)==SaveCommitVerdict.Invalid;
        }));
        Check("10 initial generation zero candidate denied",()=>Temp(root=>{
            if(!AtomicEnvelopeStore.TryOpen(root,Run,Slot,out var a)||a==null)return false;
            using(a)return a.Commit(Snapshot(2,true,false),0,"",out _)==SaveCommitVerdict.Invalid;
        }));
        Check("11 invalid original binary denied before write",()=>Temp(root=>{
            if(!AtomicEnvelopeStore.TryOpen(root,Run,Slot,out var a)||a==null)return false;
            using(a){byte[] corrupt=Snapshot(1,true,false);corrupt[7]^=0x22;
                return a.Commit(corrupt,0,"",out _)==SaveCommitVerdict.Invalid&&
                       a.Read(out _)==SaveReadVerdict.Missing;}
        }));
        Check("12 lost-ACK BEFORE rename fails closed without changing durable g1",()=>
            LostAck(CommitFaultPhase.BeforeReplace));
        Check("13 lost-ACK AFTER rename fails closed but cold host sees durable g2",()=>
            LostAck(CommitFaultPhase.AfterReplace));
        Check("14 corrupted envelope never accepted or silently overwritten",Corruption);
        Check("15 different GUID slots and foreign runs physically isolated",OtherRunOrSlot);
        Check("16 disposed host cannot read or commit",()=>Temp(root=>{
            if(!AtomicEnvelopeStore.TryOpen(root,Run,Slot,out var a)||a==null)return false;
            a.Dispose();return a.Read(out _)==SaveReadVerdict.Unavailable&&
                a.Commit(Snapshot(1,true,false),0,"",out _)==SaveCommitVerdict.Unavailable;
        }));
        Check("17 valid slot lock file is never unlinked during lease release",()=>Temp(root=>{
            if(!AtomicEnvelopeStore.TryOpen(root,Run,Slot,out var a)||a==null)return false;
            string path=Path.Combine(root,"b0162_canonical_envelope_v1",a.SlotFileName+".owner");
            a.Dispose();return File.Exists(path);
        }));
        Check("18 cold committed generation cannot replay identical g1 as a new Commit",()=>Temp(root=>{
            if(!AtomicEnvelopeStore.TryOpen(root,Run,Slot,out var a)||a==null)return false;
            using(a){
                if(a.Commit(Snapshot(1,true,false),0,"",out var x)!=SaveCommitVerdict.Accepted)return false;
                return a.Commit(Snapshot(1,true,false),0,"",out _)==SaveCommitVerdict.Conflict;
            }
        }));
        for(int i=0;i<24;i++){int n=i;Check("W20_TRUE_556_LIGHT_HEAVY_DURABLE_EPOCH_RESTART_"+n,
            ()=>RoundTrip(n));}
        Console.WriteLine("B0162_W20_CANONICAL_ENVELOPE_TOTAL "+(pass+fail)+
            " PASS "+pass+" FAIL "+fail);
        Console.WriteLine("B0162_W20_SOURCE_ONLY_TRUE_556_W06_510_NO_NATIVE_UNITY_G1_NOT_RUN");
        return fail==0?0:1;
    }
}
