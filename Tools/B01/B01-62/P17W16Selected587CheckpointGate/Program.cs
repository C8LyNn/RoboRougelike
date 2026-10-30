using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.UnitySaveBridge001;
using B01.B0162.Selected587CheckpointGate;

internal sealed class World : IPreparedWorld
{
    public RunSnapshot State;
    public bool Active,Retired,MutationOnActivate,ThrowActivate,ThrowRetire;
    public AtomicDockLedger[] ActualOriginal510Ledgers=new AtomicDockLedger[2];
    public World(RunSnapshot s)
    {
        State=s;
        for(int i=0;i<2;i++)
        {
            ActualOriginal510Ledgers[i]=new AtomicDockLedger();
            var part=s.Parts[i];
            if(part.Consumed)
            {
                var z=new Vector3d(0,0,0);
                if(!ActualOriginal510Ledgers[i].TryDetach(part.Id,z,z,z,part.MassKg,0,0,out var impulse) ||
                    impulse.MagnitudeSquared!=0)throw new InvalidOperationException("W06 ID not rehydrated");
            }
        }
    }
    public RunSnapshot Capture()
    {
        if(!MutationOnActivate||!Active)return State;
        // Physically active host returned an alternate VALID world on activation.
        return Program.Snapshot(false,false,State.Generation);
    }
    public void Activate()
    {
        Active=true;
        if(ThrowActivate)throw new IOException("after-activation side effects");
    }
    public void Deactivate(){Active=false;}
    public void Retire(){if(ThrowRetire)throw new IOException("retirement failure");Retired=true;Active=false;}
}
internal sealed class Factory : IWorldFactory
{
    public World? Last;
    public bool MutationOnActivate,ThrowActivate,ThrowRetire;
    public IPreparedWorld Prepare(RunSnapshot s)
    {
        Last=new World(s){MutationOnActivate=MutationOnActivate,
            ThrowActivate=ThrowActivate,ThrowRetire=ThrowRetire};
        return Last;
    }
}
internal static class Program
{
    private static int checks,negative,restores,rollbacks;
    static void Check(bool c,string label)
    {
        if(!c)throw new Exception("B0162_W16_ASSERTION_FAIL: "+label);
        checks++;
    }
    private static PersistedPart Part(int i,bool detach)
    {
        long id=FrozenPartCatalog.Id(i),owner=FrozenPartCatalog.Owner(i);
        var z=new Vector3d(0,0,0);
        return new PersistedPart(id,owner,detach?0:owner,FrozenPartCatalog.Asset(i),
            FrozenPartCatalog.Socket(i),detach?PartDockState.Detached:PartDockState.Attached,
            detach,!detach,new Vector3d(i+1,detach?2:1,0),0,0,0,1,
            detach?new Vector3d(.3,0,0):z,z,FrozenPartCatalog.Mass(i));
    }
    public static RunSnapshot Snapshot(bool l,bool h,long gen=5,long run=77)
        =>new RunSnapshot(run,gen,new[]{Part(0,l),Part(1,h)});
    static byte[] Bytes(bool l,bool h,long gen=5,long run=77)=>SnapshotCodec.Encode(Snapshot(l,h,gen,run));
    static void OriginalRed()
    {
        var f=new Factory();var producer=new AtomicWorldSwap(f);
        Check(producer.TryLoad(Bytes(true,false),77,1,()=>true)==WorldSwapVerdict.Accepted,
            "selected producer initial original Save");
        World old=f.Last!;
        Check(producer.TryLoad(Bytes(false,true),77,1,()=>true)==WorldSwapVerdict.Accepted,
            "RAW ORIGINAL #587 SAME GEN DIFFERENT WORLD RELEASE RED REPRODUCED");
        Check(old.Retired&&producer.IsConsumed(1,2310002)&&!producer.IsConsumed(0,2310001),
            "original producer swapped W06 consumed ledger at same generation");
        Console.WriteLine("ORIGINAL_SELECTED_587_SAME_GENERATION_FORK_RED=REPRODUCED");
        var p=new AtomicWorldSwap(new Factory{MutationOnActivate=true});
        Check(p.TryLoad(Bytes(true,false),77,1,()=>true)==WorldSwapVerdict.HostException,
              "#572 producer after-activation capture denies mutated candidate before publish");
        Check(!p.HasWorld,"#572 no invalid source publication");
    }
    static void ColdRestart(string directory)
    {
        var f=new Factory();var raw=new AtomicWorldSwap(f);
        using(var authority=new FileCheckpointAuthority(directory))
        {
            var gate=new Selected587CheckpointGate(raw,authority);
            Check(gate.TryLoad(Bytes(true,false),77,1,()=>true)==GateVerdict.Accepted,
                "first valid selected original #587 host load");
            Check(gate.TrySave(77,()=>true,out var saved)==GateVerdict.Accepted&&
                saved!.SequenceEqual(Bytes(true,false)),"same fingerprint Save accepted");
            Check(gate.TryLoad(Bytes(false,true),77,1,()=>true)==GateVerdict.SameGenerationFork,
                "same run+generation different original W06 consumed-ID rejected");
            Check(raw.IsConsumed(0,2310001)&&!raw.IsConsumed(1,2310002),
                "rejected conflicting epoch did not touch original physical authority");
            Check(gate.TryLoad(Bytes(false,true,6),77,1,()=>true)==GateVerdict.Accepted,
                "new revision with bigger generation accepted");
            Check(raw.IsConsumed(1,2310002)&&!raw.IsConsumed(0,2310001),
                "original W06 consumed ID persisted across real original transaction");
            restores++;
        }
        // Release process lease then execute a DIFFERENT CLR process against same real files.
        var psi=new ProcessStartInfo("dotnet");
        psi.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        psi.ArgumentList.Add("--verify");
        psi.ArgumentList.Add(directory);
        psi.RedirectStandardOutput=true;psi.RedirectStandardError=true;psi.UseShellExecute=false;
        using var child=Process.Start(psi)??throw new IOException("child failed to start");
        string stdout=child.StandardOutput.ReadToEnd();string stderr=child.StandardError.ReadToEnd();
        child.WaitForExit();
        Check(child.ExitCode==0 && stdout.Contains("CHILD_ORIGINAL_SAVE_AUTHORITY_RESTART_PASS"),
              "GENUINE new child CLR process retains committed checkpoint run 77/gen6: "+stderr);
        restores++;
    }
    static void ChildVerify(string directory)
    {
        using var authority=new FileCheckpointAuthority(directory);
        Check(authority.Committed!=null&&authority.Committed.Run==77&&
              authority.Committed.Generation==6&&!authority.Pending,
              "child sees committed generation from disk");
        var host=new AtomicWorldSwap(new Factory());
        var gate=new Selected587CheckpointGate(host,authority);
        Check(gate.TryLoad(Bytes(true,false,5),77,1,()=>true)==GateVerdict.StaleGeneration,
              "child refuses older generation with fresh original coordinator");
        Check(gate.TryLoad(Bytes(true,false,6),77,1,()=>true)==GateVerdict.SameGenerationFork,
              "child refuses same revision different original consumed W06 state");
        Check(gate.TryLoad(Bytes(false,true,6),77,1,()=>true)==GateVerdict.Accepted,
              "child can cold rehydrate same exact committed generation after fresh process");
        Check(host.IsConsumed(1,2310002),"fresh original W06 cold committed ID");
        Console.WriteLine("CHILD_ORIGINAL_SAVE_AUTHORITY_RESTART_PASS");
    }
    static void Invalids(string root)
    {
        string dir=Path.Combine(root,"invalid");Directory.CreateDirectory(dir);
        using(var auth=new FileCheckpointAuthority(dir))
        {
            var f=new Factory();var raw=new AtomicWorldSwap(f);
            var gate=new Selected587CheckpointGate(raw,auth);
            var bad=Bytes(true,false);bad[50]^=9;
            Check(gate.TryLoad(bad,77,1,()=>true)==GateVerdict.InvalidSnapshot,"hash tamper denied");
            Check(gate.TryLoad(Bytes(true,false,5,99),77,1,()=>true)==GateVerdict.WrongRun,"foreign run");
            Check(gate.TryLoad(Bytes(true,false),77,6,()=>true)==GateVerdict.StaleGeneration,"minimum generation");
            Check(gate.TryLoad(Bytes(true,false),77,1,()=>false)==GateVerdict.Busy,"host activity refused");
            Check(!auth.Pending&&!raw.HasWorld,"no pending intent after negative");
            negative+=4;
        }
        // Honest source exception on activation: compensated without committed change
        string rollback=Path.Combine(root,"activation");Directory.CreateDirectory(rollback);
        using(var auth=new FileCheckpointAuthority(rollback))
        {
            var f=new Factory{ThrowActivate=true};
            var host=new AtomicWorldSwap(f);
            var gate=new Selected587CheckpointGate(host,auth);
            Check(gate.TryLoad(Bytes(true,false),77,1,()=>true)==GateVerdict.Rejected,
                "activation exception / successful original compensation must not commit");
            Check(!auth.Pending&&auth.Committed==null&&!host.HasWorld,
                  "rejected candidate retired, pending cleared");
            rollbacks++;
        }
        // Source runtime's false-green cleanup after old-world retire throw must become
        // QUARANTINE at the approved B01-62 receiver gate, with durable PENDING record.
        string cleanup=Path.Combine(root,"cleanup");Directory.CreateDirectory(cleanup);
        using(var auth=new FileCheckpointAuthority(cleanup))
        {
            var f=new Factory();var host=new AtomicWorldSwap(f);
            var gate=new Selected587CheckpointGate(host,auth);
            Check(gate.TryLoad(Bytes(true,false),77,1,()=>true)==GateVerdict.Accepted,
                "precondition initial world");
            f.ThrowRetire=false;
            f.Last!.ThrowRetire=true; // old world's cleanup fail AFTER candidate publish
            Check(gate.TryLoad(Bytes(false,true,6),77,1,()=>true)==GateVerdict.Quarantine,
                "old cleanup exception must override ACCEPTED even when #587 CleanupPending");
            Check(gate.Quarantined&&auth.Pending,
                "cleanup crash-like state leaves persistent unresolved pending intent");
            Check(gate.TrySave(77,()=>true,out _)==GateVerdict.Quarantine,"sticky quarantine");
            rollbacks++;
        }
        using(var auth=new FileCheckpointAuthority(cleanup))
        {
            var newBridge=new Selected587CheckpointGate(new AtomicWorldSwap(new Factory()),auth);
            Check(auth.Pending&&newBridge.TryLoad(Bytes(false,true,6),77,1,()=>true)==GateVerdict.Quarantine,
                  "crashed pending checkpoint refuses silent restart promotion");
            rollbacks++;
        }
        // Real directory scanner must REFUSE foreign empty directory without deleting it.
        string foreign=Path.Combine(root,"foreign");Directory.CreateDirectory(foreign);
        Directory.CreateDirectory(Path.Combine(foreign,"DO_NOT_DELETE_EMPTY"));
        bool denied=false;
        try{using var _=new FileCheckpointAuthority(foreign);}catch(IOException){denied=true;}
        Check(denied&&Directory.Exists(Path.Combine(foreign,"DO_NOT_DELETE_EMPTY")),
              "foreign empty directory preserved and authority refuses unsafe ownership");
        negative++;
    }
    public static int Main(string[] args)
    {
        try
        {
            if(args.Length==2&&args[0]=="--verify"){ChildVerify(args[1]);return 0;}
            OriginalRed();
            string root=Path.Combine(Path.GetTempPath(),"b01-62-w16-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                string fresh=Path.Combine(root,"cold-restart");Directory.CreateDirectory(fresh);
                ColdRestart(fresh);
                Invalids(root);
            }
            finally{Directory.Delete(root,recursive:true);}
            Console.WriteLine($"B0162_SELECTED_587_GATE_SOURCE_ASSERTIONS_PASS={checks}");
            Console.WriteLine($"ACTUAL_CROSS_PROCESS_RESTARTS={restores} NEGATIVE_GROUPS={negative} ROLLBACK_QUARANTINE_CASES={rollbacks}");
            Console.WriteLine("G0_SOURCE_ONLY_NOT_NATIVE_UNITY LIVE_510_PRIVATE_LEDGER_NOT_BOUND G1_NOT_RUN G2_HOLD");
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
