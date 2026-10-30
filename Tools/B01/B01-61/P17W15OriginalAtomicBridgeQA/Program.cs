// B01-61 P17 W15: direct execution of the exact NEW original B01-23 AtomicWorldSwap
// C# source (not a cloned transaction), original frozen #556 codec, original W06,
// and original #510 R2. FakeWorld models only IWorldFactory/IPreparedWorld
// because native activated Unity/PhysX is NOT available in CI.
using System;
using System.IO;
using B01.UnitySaveBridge001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

internal sealed class FakeWorld : IPreparedWorld
{
    public RunSnapshot Recorded;
    public bool Active, Retired, ThrowOnActivate, ThrowOnDeactivate;
    public bool CorruptCapture;
    public int ActivatedCount, DeactivatedCount, RetiredCount;
    public FakeWorld(RunSnapshot snapshot) { Recorded=snapshot; }
    public RunSnapshot Capture()
    {
        if(Retired)throw new InvalidOperationException("retired");
        if(CorruptCapture)return new RunSnapshot(Recorded.RunId,Recorded.Generation,
            new []{Recorded.Parts[1],Recorded.Parts[0]});
        return Recorded;
    }
    public void Activate()
    {
        ActivatedCount++;
        Active=true;
        if(ThrowOnActivate)throw new IOException("injected partial activation fault");
    }
    public void Deactivate()
    {
        DeactivatedCount++;
        Active=false;
        if(ThrowOnDeactivate)throw new IOException("injected deactivation rollback fault");
    }
    public void Retire()
    {
        RetiredCount++;
        Retired=true;
        Active=false;
    }
}
internal sealed class FakeFactory : IWorldFactory
{
    public FakeWorld Last;
    public bool ThrowPrepare, ThrowOnActivate, ThrowOnDeactivate, CorruptCapture;
    public int PrepareCount;
    public IPreparedWorld Prepare(RunSnapshot snapshot)
    {
        PrepareCount++;
        if(ThrowPrepare)throw new IOException("injected prepare failure before any publish");
        Last=new FakeWorld(snapshot) {
            ThrowOnActivate=ThrowOnActivate, ThrowOnDeactivate=ThrowOnDeactivate,
            CorruptCapture=CorruptCapture
        };
        return Last;
    }
}
internal static class Program
{
    const long RUN=77;
    static int assertions,success,refusals,rolledBack,quarantine,sourceRed;
    static void Check(bool ok,string label)
    {
        assertions++;
        if(!ok)throw new InvalidOperationException("B01_61_REAL_BRIDGE_QA_FAIL: "+label);
    }
    static PersistedPart P(int i,bool detached)
    {
        var zero=new Vector3d(0,0,0);
        return new PersistedPart(FrozenPartCatalog.Id(i),FrozenPartCatalog.Owner(i),
           detached?0:FrozenPartCatalog.Owner(i),
           FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
           detached?PartDockState.Detached:PartDockState.Attached,
           detached,!detached,new Vector3d(i+.5,1.25,-.125),
           0,0,0,1,detached?new Vector3d(.4,.5,-.2):zero,
           detached?new Vector3d(.02,-.03,0):zero,FrozenPartCatalog.Mass(i));
    }
    static byte[] Wire(long gen,bool light,bool heavy)=>
        SnapshotCodec.Encode(new RunSnapshot(RUN,gen,new[]{P(0,light),P(1,heavy)}));
    static (AtomicWorldSwap bridge,FakeFactory factory) Start(long gen=1,bool light=true,bool heavy=false)
    {
        var factory=new FakeFactory();
        var bridge=new AtomicWorldSwap(factory);
        Check(bridge.TryLoad(Wire(gen,light,heavy),RUN,gen,()=>true)==WorldSwapVerdict.Accepted,
             "real bridge actual original #556 cold checkpoint initial ACCEPTED");
        Check(bridge.HasWorld&&!bridge.Busy&&!bridge.Quarantined&&
              bridge.Generation==gen,"real bridge initial logical live state");
        Check(bridge.IsConsumed(0,2310001)==light&&bridge.IsConsumed(1,2310002)==heavy,
              "real W06 consumed ID rebuilt in original bridge");
        Check(factory.Last.Active&&!factory.Last.Retired,"candidate activated through original bridge");
        success++;
        return (bridge,factory);
    }
    static void Still(AtomicWorldSwap bridge,FakeWorld old,long generation,
                      bool light,bool heavy,string step)
    {
        Check(!bridge.Busy&&!bridge.Quarantined&&bridge.HasWorld&&bridge.Generation==generation,
              step+" previous valid published world remains");
        Check(old.Active&&!old.Retired,step+" original physical-world abstraction still active");
        Check(bridge.IsConsumed(0,2310001)==light &&
              bridge.IsConsumed(1,2310002)==heavy,
              step+" original W06 consumed ID unchanged");
        RunSnapshot parsed=null;
        Check(bridge.TrySave(RUN,()=>true,out var bytes)&&bytes!=null &&
              SnapshotCodec.TryDecode(bytes,out parsed)&&parsed!=null,
              step+" real source guarded save remains available");
        Check(parsed!.Parts[0].State==(light?PartDockState.Detached:PartDockState.Attached) &&
              parsed.Parts[1].State==(heavy?PartDockState.Detached:PartDockState.Attached),
              step+" saved old physical abstract state unchanged");
        rolledBack++;
    }
    static void NormalAndInvalid()
    {
        var (bridge,factory)=Start();
        var first=factory.Last;
        RunSnapshot record=null;
        Check(bridge.TrySave(RUN,()=>true,out byte[] save)&&save!=null &&
              SnapshotCodec.TryDecode(save,out record)&&record!=null,
              "actual original bridge Save of published world");
        Check(record!.Parts[0].Id==SecureRigCatalog.Light.PartId &&
              record.Parts[1].Id==SecureRigCatalog.Heavy.PartId,
              "actual frozen #510 stable IDs survive Save");
        Check(bridge.TryLoad(Wire(2,false,true),RUN,2,()=>true)==WorldSwapVerdict.Accepted,
              "original source fresh-world replacement accepted with new generation");
        Check(first.Retired&&!first.Active&&factory.Last.Active&&
              bridge.Generation==2&&!bridge.IsConsumed(0,2310001)&&
              bridge.IsConsumed(1,2310002),
              "old abstract world retired and new W06 consumed ID published atomically");
        success++;
        var valid=factory.Last;
        var corrupt=Wire(3,true,false);corrupt[corrupt.Length/2]^=1;
        Check(bridge.TryLoad(corrupt,RUN,2,()=>true)==WorldSwapVerdict.InvalidSnapshot,
              "corrupt checkpoint fail closed before any world replacement");
        refusals++;Still(bridge,valid,2,false,true,"corrupted bytes");
        Check(bridge.TryLoad(Wire(1,true,false),RUN,1,()=>true)==WorldSwapVerdict.StaleGeneration,
              "older generation rejected after valid committed update");
        refusals++;Still(bridge,valid,2,false,true,"stale generation");
        Check(bridge.TryLoad(Wire(3,true,false),RUN+1,1,()=>true)==WorldSwapVerdict.WrongRun,
              "wrong run request rejected");
        refusals++;Still(bridge,valid,2,false,true,"wrong run");
        Check(bridge.TryLoad(Wire(3,true,false),RUN,1,()=>false)==WorldSwapVerdict.HostBusy,
              "busy host cannot stage");
        refusals++;Still(bridge,valid,2,false,true,"busy host");
        Check(bridge.TryLoad(Wire(3,true,false),RUN,1,()=>throw new IOException("busy"))==
              WorldSwapVerdict.HostBusy,"throwing hostIdle before stage rejects");
        refusals++;Still(bridge,valid,2,false,true,"throwing host");
        int calls=0;
        Check(bridge.TryLoad(Wire(3,true,false),RUN,1,()=>{
           calls++;return calls==1;})==WorldSwapVerdict.HostBusy&&calls==2,
           "second idle callback rejects host that became busy before publication");
        refusals++;Still(bridge,valid,2,false,true,"second idle fail");
        Check(bridge.TrySave(RUN,()=>false,out byte[] skipped)==false&&skipped==null,
              "host busy disallows original bridge Save without leaked partial bytes");
        success++;
    }
    static void FaultsAndRollback()
    {
        // Four independently created hosts avoid stale fault flags affecting other tests.
        foreach(string fault in new []{"prepare","identity","activate","old-deactivate"})
        {
            var (bridge,factory)=Start(4,true,false);
            var old=factory.Last;
            if(fault=="prepare")factory.ThrowPrepare=true;
            if(fault=="identity")factory.CorruptCapture=true;
            if(fault=="activate")factory.ThrowOnActivate=true;
            if(fault=="old-deactivate")old.ThrowOnDeactivate=true;
            var result=bridge.TryLoad(Wire(5,false,true),RUN,1,()=>true);
            Check(result==WorldSwapVerdict.HostException,
                  fault+" actual original bridge catches injection");
            Still(bridge,old,4,true,false,fault);
            if(fault!="prepare")
            {
                Check(factory.Last.Retired&&!factory.Last.Active,
                      fault+" failed prepared candidate retired, not published");
                if(fault=="activate")
                    Check(factory.Last.DeactivatedCount>0,
                          "partial Activate exception invokes Deactivate before cleanup");
            }
            success++;
        }
    }
    static void QuarantineAndReentrant()
    {
        var (bridge,factory)=Start();
        var old=factory.Last;
        factory.ThrowOnActivate=true;
        factory.ThrowOnDeactivate=true;
        Check(bridge.TryLoad(Wire(2,false,true),RUN,1,()=>true)==WorldSwapVerdict.HostException,
             "source catches combined failed activation + failed rollback");
        Check(bridge.Quarantined && !bridge.Busy && bridge.HasWorld &&
              bridge.IsConsumed(0,2310001),
              "real original core sticky quarantine protects consumed old W06 state");
        Check(bridge.TryLoad(Wire(3,false,false),RUN,1,()=>true)==WorldSwapVerdict.RollbackQuarantine,
              "new restore refused in quarantine");
        Check(!bridge.TrySave(RUN,()=>true,out byte[] result)&&result==null,
              "save refused in rollback quarantine");
        Check(old.Active,"previous abstract world reactivated despite candidate quarantine");
        quarantine++;
        var (b2,f2)=Start();
        WorldSwapVerdict inner=WorldSwapVerdict.Accepted;
        Check(b2.TryLoad(Wire(2,false,true),RUN,1,()=>{
            inner=b2.TryLoad(Wire(3,true,true),RUN,1,()=>true);
            return true;
        })==WorldSwapVerdict.Accepted &&
        inner==WorldSwapVerdict.HostBusy,
        "actual original bridge transaction Busy blocks nested load before callback");
        Check(b2.Generation==2&&!b2.IsConsumed(0,2310001)&&
              b2.IsConsumed(1,2310002)&&f2.Last.Active,
              "only outer state accepted after rejected nested load");
        success++;
    }
    static void SameGenerationPhysicalReplacement()
    {
        var (bridge,factory)=Start(9,true,false);
        var old=factory.Last;
        byte[] competing=Wire(9,false,true);
        Check(SnapshotCodec.TryDecode(competing,out RunSnapshot parsed)&&parsed!=null,
             "independently valid same-generation conflicting frozen source save bytes");
        var outcome=bridge.TryLoad(competing,RUN,9,()=>true);
        Check(outcome==WorldSwapVerdict.Accepted,
             "NEW BRIDGE SOURCE RELEASE RED: different same-gen save accepted");
        Check(old.Retired&&!old.Active&&factory.Last.Active&&
              bridge.Generation==9 &&
              !bridge.IsConsumed(0,2310001)&&bridge.IsConsumed(1,2310002),
             "NEW BRIDGE SOURCE RED physically ABSTRACT world swapped with no generation bump");
        sourceRed++;
        Console.WriteLine("REAL_ORIGINAL_BRIDGE_SAME_GENERATION_DIFFERENT_WORLD_RED=REPRODUCED");
    }
    public static int Main()
    {
        try
        {
            NormalAndInvalid();
            FaultsAndRollback();
            QuarantineAndReentrant();
            SameGenerationPhysicalReplacement();
            Check(sourceRed==1 && refusals==6 && rolledBack==10 &&
                  quarantine==1 && success==15,
                  "all original new bridge rollback, ID and same-gen cases were executed");
            Console.WriteLine("ORIGINAL_NEW_ATOMIC_WORLD_SWAP_QA_EXECUTION_PASS asserts="+assertions+
                " success_cases="+success+" rejection_cases="+refusals+
                " rollback_checks="+rolledBack+" quarantine="+quarantine+
                " source_release_red="+sourceRed);
            Console.WriteLine("WORLD_BRIDGE_RELEASE_HOLD=SAME_GENERATION_CONFLICTING_WORLD_ACCEPTED");
            Console.WriteLine("ORIGINAL_556_AND_W06_AND_510_COMPILATION=YES");
            Console.WriteLine("ACTUAL_LICENSED_UNITY_PHYSX=NOT_RUN; MANAGED_HOST_FAKE_ONLY=YES");
            return 0; // QA pass means actual regression reproduced, NOT release GO.
        }
        catch(Exception ex) {Console.Error.WriteLine("INDEPENDENT_REAL_BRIDGE_QA_FAILURE "+ex);return 1;}
    }
}
