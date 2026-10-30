// Direct original selected #587 C# source execution. PASS means reproducible source RED (not release accepted).
using System;
using System.IO;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.UnitySaveBridge001;

internal static class Original587Witness
{
    private sealed class W : IPreparedWorld
    {
        public RunSnapshot State;
        public bool Active, BadAfterActivation, ThrowRetire;
        public W(RunSnapshot snapshot){State=snapshot;}
        public RunSnapshot Capture()
        {
            if(Active &&BadAfterActivation)return MakeState(!State.Parts[0].Consumed,!State.Parts[1].Consumed,State.Generation,State.RunId);
            return State;
        }
        public void Activate()=>Active=true;
        public void Deactivate()=>Active=false;
        public void Retire()
        {
            Active=false;
            if(ThrowRetire)throw new IOException("intentionally failing cleanup");
        }
    }
    private sealed class F : IWorldFactory
    {
        public W? Last;
        public bool CorruptActivation, FailCleanup;
        public IPreparedWorld Prepare(RunSnapshot snapshot)
        {
            Last=new W(snapshot){BadAfterActivation=CorruptActivation,ThrowRetire=FailCleanup};
            return Last;
        }
    }
    static RunSnapshot MakeState(bool light,bool heavy,long gen,long run=7)
    {
        var p=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool detached=i==0?light:heavy;
            var s=SecureRigCatalog.At(i);
            p[i]=new PersistedPart(s.PartId,FrozenPartCatalog.Owner(i),
                detached?0:FrozenPartCatalog.Owner(i),FrozenPartCatalog.Asset(i),
                FrozenPartCatalog.Socket(i),detached?PartDockState.Detached:PartDockState.Attached,
                detached,!detached,new Vector3d(s.RootX+s.PartOffsetX,1.2+(detached?1:0),0),
                0,0,0,1,detached?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),s.PartMassKg);
        }
        return new RunSnapshot(run,gen,p);
    }
    static byte[] B(bool l,bool h,long gen=1)=>SnapshotCodec.Encode(MakeState(l,h,gen));
    private static bool SameGenerationFork()
    {
        var host=new AtomicWorldSwap(new F());
        return host.TryLoad(B(true,false,3),7,1,()=>true)==WorldSwapVerdict.Accepted&&
            host.TryLoad(B(false,true,3),7,1,()=>true)==WorldSwapVerdict.Accepted &&
            host.IsConsumed(1,2310002) && !host.IsConsumed(0,2310001);
    }
    private static bool FailedAbortVerdictMask()
    {
        var f=new F();var host=new AtomicWorldSwap(f);
        if(host.TryLoad(B(true,false,1),7,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
        f.CorruptActivation=true;f.FailCleanup=true;
        var rc=host.TryLoad(B(false,true,2),7,1,()=>true);
        return rc==WorldSwapVerdict.HostException &&host.Quarantined;
    }
    private static bool StaleAfterFreshHostRestart()
    {
        var old=new AtomicWorldSwap(new F());
        if(old.TryLoad(B(true,false,9),7,1,()=>true)!=WorldSwapVerdict.Accepted)return false;
        // Original #587 no shared externally persisted generation authority.
        var restarted=new AtomicWorldSwap(new F());
        return restarted.TryLoad(B(false,true,8),7,1,()=>true)==WorldSwapVerdict.Accepted;
    }
    public static int Main()
    {
        int passed=0;
        if(SameGenerationFork()){passed++;Console.WriteLine("EXPECTED_RED_587_SAME_GENERATION_FORK_REPRODUCED");}
        else Console.WriteLine("FAIL_EXPECTED_RED_FORK");
        if(FailedAbortVerdictMask()){passed++;Console.WriteLine("EXPECTED_RED_587_CLEANUP_QUARANTINE_MASKED_REPRODUCED");}
        else Console.WriteLine("FAIL_EXPECTED_RED_MASK");
        if(StaleAfterFreshHostRestart()){passed++;Console.WriteLine("EXPECTED_RED_587_RESTART_WATERMARK_MISSING_REPRODUCED");}
        else Console.WriteLine("FAIL_EXPECTED_RED_DURABILITY");
        Console.WriteLine("ORIGINAL_587_SECURITY_RED_TOTAL "+passed+" OF 3; NATIVE_UNITY_NOT_RUN");
        return passed==3?0:3;
    }
}
