// B01-61 W16: ORIGINAL selected B01-23 #587 AtomicWorldSwap source is compiled untouched.
// ONLY IWorldFactory/IPreparedWorld are managed test doubles. Not Unity/PhysX/native.
// Independent security witnesses are intentionally RED; evidence PASS != release accepted.
using System;
using System.IO;
using B01.UnitySaveBridge001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
internal sealed class PhysicalWorldDouble : IPreparedWorld
{
    public RunSnapshot Snapshot;
    public bool Active, Retired, FailActivate, FailDeactivate, FailRetire, MutateOnActivation;
    public int ActivationCalls, DeactivationCalls, RetirementCalls;
    public PhysicalWorldDouble(RunSnapshot snap){Snapshot=snap;}
    public RunSnapshot Capture(){if(Retired)throw new IOException("retired captured");return Snapshot;}
    public void Activate()
    {
        ActivationCalls++;
        Active=true;
        if(MutateOnActivation)
        {
            // A valid BUT DIFFERENT active physical state; legitimate IPreparedWorld can
            // mutate Rigidbody/ledger/pose in Activate(). This must NOT be published.
            var p=Snapshot.Parts;
            Snapshot=new RunSnapshot(Snapshot.RunId,Snapshot.Generation,
                new[]{p[1],p[0]});
        }
        if(FailActivate)throw new IOException("injected activate-after-side-effect");
    }
    public void Deactivate()
    {
        DeactivationCalls++;Active=false;
        if(FailDeactivate)throw new IOException("injected cleanup failure");
    }
    public void Retire()
    {
        RetirementCalls++;Retired=true;Active=false;
        if(FailRetire)throw new IOException("injected retire-after-side-effect");
    }
}
internal sealed class FactoryDouble:IWorldFactory
{
    public PhysicalWorldDouble Last=null!;
    public bool MutateNextOnActivate,FailNextActivate,FailNextDeactivate;
    public bool FailNextRetire { get; set; }
    public Action OnPrepare=()=>{};
    public IPreparedWorld Prepare(RunSnapshot snapshot)
    {
        OnPrepare?.Invoke();
        Last=new PhysicalWorldDouble(snapshot){
          MutateOnActivation=MutateNextOnActivate,
          FailActivate=FailNextActivate,FailDeactivate=FailNextDeactivate,
          FailRetire=FailNextRetire};
        return Last;
    }
}
internal static class Program
{
    const long RUN=176161;
    static int assertions,cases,releaseRed,positive,rollback,corruptDenials,physicalFileRead;
    static void Check(bool yes,string title)
    {
        assertions++;
        if(!yes)throw new InvalidOperationException("B0161_W16_INDEPENDENT_TEST_FAIL: "+title);
    }
    static void Case(string title,Action action)
    {action();cases++;Console.WriteLine("W16_ORIGINAL_CASE_PASS "+title);}
    static PersistedPart Part(int slot,bool detached)
    {
        var zero=new Vector3d(0,0,0);
        return new PersistedPart(FrozenPartCatalog.Id(slot),FrozenPartCatalog.Owner(slot),
          detached?0:FrozenPartCatalog.Owner(slot),FrozenPartCatalog.Asset(slot),
          FrozenPartCatalog.Socket(slot),detached?PartDockState.Detached:PartDockState.Attached,
          detached,!detached,new Vector3d(1+slot,2,slot/4.0),0,0,0,1,
          detached?new Vector3d(.75,0,-.25):zero,zero,FrozenPartCatalog.Mass(slot));
    }
    static RunSnapshot Snapshot(long generation,bool light,bool heavy)
    =>new RunSnapshot(RUN,generation,new[]{Part(0,light),Part(1,heavy)});
    static byte[] Bin(long generation,bool light,bool heavy)
    =>SnapshotCodec.Encode(Snapshot(generation,light,heavy));
    static (AtomicWorldSwap bridge,FactoryDouble factory,PhysicalWorldDouble current) Start(
        long generation=5,bool light=true,bool heavy=false)
    {
        var factory=new FactoryDouble();
        var bridge=new AtomicWorldSwap(factory);
        Check(bridge.TryLoad(Bin(generation,light,heavy),RUN,generation,()=>true)==
              WorldSwapVerdict.Accepted,"original #587 initial world directly loaded");
        Check(bridge.Generation==generation&&bridge.IsConsumed(0,2310001)==light&&
              bridge.IsConsumed(1,2310002)==heavy,
              "original W06 Light+Heavy consumed status authoritative");
        Check(factory.Last.Active&&!factory.Last.Retired&&!bridge.Quarantined,
              "original source published valid active candidate");
        return (bridge,factory,factory.Last);
    }
    static void FourStableIdentitiesAndRealDisk()
    {
        foreach(bool light in new[]{false,true})foreach(bool heavy in new[]{false,true})
        {
            var (host,_,current)=Start(5,light,heavy);
            Check(host.TrySave(RUN,()=>true,out byte[] bytes)&&bytes!=null,
                "actual original #587 TrySave gets exact selected #556 bytes");
            string file=Path.Combine(Path.GetTempPath(),
                "b01-61-w16-"+Guid.NewGuid().ToString("N")+".save");
            try
            {
                File.WriteAllBytes(file,bytes!);
                byte[] onDisk=File.ReadAllBytes(file);physicalFileRead++;
                Check(SnapshotCodec.TryDecode(onDisk,out RunSnapshot? decoded)&&decoded!=null,
                    "selected #556 physical bytes genuinely read from operating system file");
                Check(decoded!.Parts[0].Id==2310001 &&decoded!.Parts[1].Id==2310002&&
                      decoded!.Parts[0].Consumed==light&&decoded!.Parts[1].Consumed==heavy,
                    "stable Light/Heavy IDs persist from exact binary bytes");
                var cold=new AtomicWorldSwap(new FactoryDouble());
                Check(cold.TryLoad(onDisk,RUN,5,()=>true)==WorldSwapVerdict.Accepted,
                    "actual #587 transaction source loads into newly constructed host");
                Check(cold.IsConsumed(0,2310001)==light&&cold.IsConsumed(1,2310002)==heavy,
                    "actual W06 consumed ID reconstructs in fresh host");
                if(light||heavy)
                {
                    var realLedger=new AtomicDockLedger();var z=new Vector3d(0,0,0);
                    int i=light?0:1;
                    Check(realLedger.TryDetach(FrozenPartCatalog.Id(i),z,z,z,
                        FrozenPartCatalog.Mass(i),0,0,out Vector3d _),
                        "original frozen #510 R2 ledger can independently consume restored ID");
                    Check(realLedger.Contains(FrozenPartCatalog.Id(i)),
                        "real source R2 confirms ID membership");
                }
            }
            finally {if(File.Exists(file))File.Delete(file);}
            Check(current.Active&&!current.Retired,"Save does not physically deactivate live world");
            positive++;
        }
    }
    static void ActivationChangesWorldRefused()
    {
        var (host,factory,old)=Start();
        factory.MutateNextOnActivate=true;
        var result=host.TryLoad(Bin(6,false,true),RUN,5,()=>true);
        Check(result==WorldSwapVerdict.HostException,
           "original #587 post-activation equivalence RED→GREEN for W15 #572");
        Check(old.Active&&!old.Retired&&host.Generation==5&&host.IsConsumed(0,2310001)&&
            !host.IsConsumed(1,2310002),"invalid staged activation cannot change old world/IDs");
        Check(factory.Last.Retired&&!factory.Last.Active&&!host.Quarantined,
           "invalid newly activated candidate compensated and disposed");
        rollback++;
    }
    static void SameGenerationForkReleaseRed()
    {
        var (host,factory,old)=Start(9,true,false);
        var verdict=host.TryLoad(Bin(9,false,true),RUN,9,()=>true);
        Check(verdict==WorldSwapVerdict.Accepted,
            "ORIGINAL_W16_RED different valid snapshot same generation accepted");
        Check(old.Retired&&factory.Last.Active&&!host.IsConsumed(0,2310001)&&
              host.IsConsumed(1,2310002)&&host.Generation==9,
            "ORIGINAL_W16_RED old world physically retired without revision advance");
        releaseRed++;
        Console.WriteLine("W16_SOURCE_RED_SAME_GENERATION_WORLD_FORK=REPRODUCED");
    }
    static void RollbackFailureMisreportedToCaller()
    {
        var (host,factory,old)=Start(11,true,false);
        factory.FailNextActivate=true;
        factory.FailNextDeactivate=true;
        var verdict=host.TryLoad(Bin(12,false,true),RUN,11,()=>true);
        Check(host.Quarantined,"original W06 candidate cleanup failure sets sticky quarantine");
        Check(verdict==WorldSwapVerdict.HostException,
            "ORIGINAL_W16_RED first caller got HostException despite quarantined rollback");
        Check(host.TryLoad(Bin(13,true,true),RUN,11,()=>true)==
            WorldSwapVerdict.RollbackQuarantine,"later caller sees actual quarantine");
        Check(!host.TrySave(RUN,()=>true,out _),"source forbids future Save when rollback failed");
        Check(old.Active&&host.Generation==11&&host.IsConsumed(0,2310001),
            "old valid published world/ID authority not overwritten");
        releaseRed++;
        Console.WriteLine("W16_SOURCE_RED_QUARANTINE_RETURN_VERDICT=REPRODUCED");
    }
    static void RefusedTransactionsDontMutate()
    {
        var (host,_,old)=Start();
        byte[] damaged=Bin(6,false,true);damaged[46]^=0x5;
        Check(host.TryLoad(damaged,RUN,5,()=>true)==WorldSwapVerdict.InvalidSnapshot,
            "signed-hash corrupt Save blocked");
        Check(host.TryLoad(Bin(6,false,true),RUN+1,5,()=>true)==WorldSwapVerdict.WrongRun,
            "foreign Run blocked");
        Check(host.TryLoad(Bin(4,false,true),RUN,1,()=>true)==WorldSwapVerdict.StaleGeneration,
            "prior generation blocked");
        Check(host.TryLoad(Bin(6,false,true),RUN,5,()=>false)==WorldSwapVerdict.HostBusy,
            "host active transaction blocked");
        Check(host.TryLoad(Bin(6,false,true),RUN,5,()=>throw new IOException("busy"))==
            WorldSwapVerdict.HostBusy,"host failure no approval");
        int invokes=0;
        Check(host.TryLoad(Bin(6,false,true),RUN,5,()=>++invokes==1)==
              WorldSwapVerdict.HostBusy&&invokes==2,"second idle denial prevents commit");
        Check(old.Active&&!old.Retired&&host.Generation==5&&
              host.IsConsumed(0,2310001)&&!host.IsConsumed(1,2310002),
              "six invalid states cannot mutate the old world/W06 consumed IDs");
        corruptDenials+=6;
    }
    static void PhysicalActivationExceptionCompensated()
    {
        var (host,factory,old)=Start();
        factory.FailNextActivate=true;
        var verdict=host.TryLoad(Bin(6,false,true),RUN,5,()=>true);
        Check(verdict==WorldSwapVerdict.HostException,"faulty newly activated physics rejected");
        Check(old.Active&&!old.Retired&&factory.Last.Retired&&
              host.Generation==5&&host.IsConsumed(0,2310001),
            "failed new activation retains original physical world and original IDs");
        Check(!host.Quarantined,"successful rollback not quarantined");
        rollback++;
    }
    static void NoDurableGenerationAuthorityOnFreshStart()
    {
        var h=new AtomicWorldSwap(new FactoryDouble());
        Check(h.TryLoad(Bin(14,true,false),RUN,1,()=>true)==WorldSwapVerdict.Accepted,
              "source reference latest generation 14");
        var fresh=new AtomicWorldSwap(new FactoryDouble());
        var result=fresh.TryLoad(Bin(13,false,true),RUN,1,()=>true);
        Check(result==WorldSwapVerdict.Accepted,
              "SOURCE_BOUNDARY fresh host without durable min-generation accepts older valid save");
        var anchored=new AtomicWorldSwap(new FactoryDouble());
        Check(anchored.TryLoad(Bin(13,false,true),RUN,14,()=>true)==WorldSwapVerdict.StaleGeneration,
            "strong external durable watermark parameter correctly rejects older save");
        releaseRed++;
        Console.WriteLine("W16_DURABLE_WATERMARK_HOST_REQUIREMENT_UNIMPLEMENTED=YES");
    }
    public static int Main(string[] args)
    {
        try
        {
            Case("real frozen #556 file Save/ColdLoad all Light Heavy ID variants",FourStableIdentitiesAndRealDisk);
            Case("#572 after-activation physical readback must prevent acceptance",ActivationChangesWorldRefused);
            Case("#588 same-generation different consumed world release RED",SameGenerationForkReleaseRed);
            Case("#582 fatal rollback quarantine verdict release RED",RollbackFailureMisreportedToCaller);
            Case("6 corrupt/run/stale/busy/reentrancy negative protections",RefusedTransactionsDontMutate);
            Case("partial activation exception restores prior physics and W06 ID",PhysicalActivationExceptionCompensated);
            Case("cross-host durable watermark requirement on genuine #556 bytes",NoDurableGenerationAuthorityOnFreshStart);
            Check(cases==7&&positive==4&&rollback==2&&corruptDenials==6&&
                  releaseRed==3&&physicalFileRead==4,"every source witness group actually executed");
            Console.WriteLine("B0161_W16_SELECTED_587_ORIGINAL_SOURCE_QA_PASS cases="+cases+
                " assertions="+assertions+" disk_files="+physicalFileRead+
                " negative_groups="+corruptDenials+" rollback_groups="+rollback+
                " SOURCE_RELEASE_RED="+releaseRed+
                " EXACT_ORIGINAL_556=YES W06=YES R2=YES UNITY_G1=NOT_RUN");
            if(args.Length>0&&args[0]=="--release"){
                Console.WriteLine("SELECTED_587_RELEASE_HOLD=SAME_GENERATION_FORK_PLUS_QUARANTINE_STATUS_PLUS_DURABLE_WATERMARK_HOST_API");
                return 3;
            }
            return 0;
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine("B0161_W16_SOURCE_QA_INFRA_FAILURE "+ex);
            return 2;
        }
    }
}
