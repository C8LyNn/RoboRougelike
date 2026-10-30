// B01-62 W15 – execute ACTUAL immutable #556 contract, ACTUAL #510 AtomicDockLedger
// and ACTUAL original W06 DebrisImpulseLedger with our new bridge + staged fake host.
// FAKE host is a test DOUBLE: NOT Unity, Rigidbody, PhysX or live game Save/Load.
using System;
using System.IO;
using System.Linq;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.B0162.AtomicHostBridge;

internal sealed class LogicalWorld
{
    public readonly RunSnapshot Snapshot;
    public readonly AtomicDockLedger[] Ledgers;
    public LogicalWorld(RunSnapshot s)
    {
        if(!SnapshotRules.Valid(s))throw new InvalidDataException("BAD_LOGICAL_FIXTURE");
        Snapshot=s;
        Ledgers=new AtomicDockLedger[FrozenPartCatalog.Slots];
        for(int i=0;i<Ledgers.Length;i++)
        {
            Ledgers[i]=new AtomicDockLedger();
            PersistedPart part=s.Parts[i];
            if(part.Consumed)
            {
                var zero=new Vector3d(0,0,0);
                if(!Ledgers[i].TryDetach(part.Id,zero,zero,zero,part.MassKg,0,0,out var impulse)
                    || impulse.MagnitudeSquared!=0 || !Ledgers[i].Contains(part.Id))
                    throw new InvalidDataException("CANNOT_REBUILD_ORIGINAL_R2_LEDGER");
            }
        }
    }
}

internal sealed class Stage : IPreparedWorld
{
    public LogicalWorld World;
    public bool Aborted;
    public bool IsReady {get;set;}=true;
    public Stage(RunSnapshot snap){World=new LogicalWorld(snap);}
    public int ConsumedCount(int slot)=>World.Ledgers[slot].Count;
    public bool ContainsConsumedId(int slot,long id)=>World.Ledgers[slot].Contains(id);
    public void Abort(){Aborted=true;}
}

internal sealed class FakeWorldHost : IAtomicWorldHost
{
    public LogicalWorld Current;
    public bool Idle=true, ThrowCapture, ThrowStage, RejectPublish, PublishThenFalse,
       PublishThenThrow, FailRestore, StageNotReady, StageCorrupt,
       MutateWorldWhileStaging, RejectOnSecondIdle;
    public bool Quarantined;
    public int PublishCalls, RestoreCalls, StageCalls, IdleCalls;
    public Stage? LastStage;
    public Action? IdleCallback;
    public FakeWorldHost(RunSnapshot initial){Current=new LogicalWorld(initial);}
    public object CurrentWorldToken=>Current;
    public bool IsQuiescent()
    {
        IdleCalls++;
        IdleCallback?.Invoke();
        return Idle && !(RejectOnSecondIdle && IdleCalls>1);
    }
    public RunSnapshot CaptureCurrent()
    {
        if(ThrowCapture)throw new IOException("capture injection");
        return Current.Snapshot;
    }
    public IPreparedWorld PrepareFreshWorld(RunSnapshot snapshot)
    {
        StageCalls++;
        if(ThrowStage)throw new IOException("staging injection");
        LastStage=new Stage(snapshot){IsReady=!StageNotReady};
        if(StageCorrupt)
            LastStage.World.Ledgers[0].ClearForNewRun();
        if(MutateWorldWhileStaging)
            Current=new LogicalWorld(snapshot);
        return LastStage;
    }
    public bool TryPublish(object previous,IPreparedWorld stage)
    {
        PublishCalls++;
        if(!ReferenceEquals(previous,Current))return false;
        if(RejectPublish)return false;
        Current=((Stage)stage).World; // one managed-pointer swap simulates host atomicity
        if(PublishThenFalse)return false;
        if(PublishThenThrow)throw new IOException("after logical commit injection");
        return true;
    }
    public bool RestorePrevious(object previous)
    {
        RestoreCalls++;
        if(FailRestore)return false;
        Current=(LogicalWorld)previous;
        return true;
    }
    public void Quarantine(string cause){Quarantined=true;}
}
internal static class Program
{
    static int checks,denials,roundtrips,rollbacks;
    static readonly Vector3d Zero=new Vector3d(0,0,0);
    static void Check(bool c,string message)
    {
        if(!c)throw new Exception("W15_BRIDGE_QA_FAIL "+message);
        checks++;
    }
    static PersistedPart Part(int slot,bool detached)
        =>new PersistedPart(FrozenPartCatalog.Id(slot),FrozenPartCatalog.Owner(slot),
           detached?0:FrozenPartCatalog.Owner(slot),FrozenPartCatalog.Asset(slot),
           FrozenPartCatalog.Socket(slot),
           detached?PartDockState.Detached:PartDockState.Attached,
           detached,!detached,new Vector3d(slot+.75,0,-.5),
           0,0,0,1,detached?new Vector3d(.3,0,0):Zero,Zero,
           FrozenPartCatalog.Mass(slot));
    static RunSnapshot Snap(bool light,bool heavy,long gen=3,long run=77)
        =>new RunSnapshot(run,gen,new[]{Part(0,light),Part(1,heavy)});
    static byte[] Wire(bool a,bool b,long gen=3,long run=77)
        =>SnapshotCodec.Encode(Snap(a,b,gen,run));
    static void Inspect(FakeWorldHost h,bool light,bool heavy)
    {
        for(int i=0;i<2;i++)
        {
            bool expect=i==0?light:heavy;
            var p=h.Current.Snapshot.Parts[i];
            Check(p.Id==FrozenPartCatalog.Id(i)&&p.AssetGuid==FrozenPartCatalog.Asset(i),
                "stable original part ID and GUID");
            Check(p.Consumed==expect&&(p.State==PartDockState.Detached)==expect,
                "detached/consumed flags persist");
            Check(h.Current.Ledgers[i].Count==(expect?1:0)&&
                h.Current.Ledgers[i].Contains(p.Id)==expect,"ACTUAL original #510 consumed-ID ledger");
            Check(p.MassKg==FrozenPartCatalog.Mass(i),"light/heavy mass identity");
        }
    }
    static void Roundtrip(bool a,bool b,string directory)
    {
        // Original real binary #556 codec Save from actual W06/510 logical world.
        var src=new FakeWorldHost(Snap(a,b));
        var save=new FrozenSaveHostBridge(src);
        Check(save.TrySave(out byte[]? bytes)==BridgeResult.Accepted&&bytes!=null,
              "host Save returns actual #556 binary");
        string file=Path.Combine(directory,$"{a}_{b}.b01save");
        File.WriteAllBytes(file,bytes!);
        // Simulate cold process restart with entirely new host, no shared ledger.
        var cold=new FakeWorldHost(Snap(false,false,1));
        var load=new FrozenSaveHostBridge(cold);
        Check(load.TryLoad(File.ReadAllBytes(file),77,1)==BridgeResult.Accepted,
              "new host cold Load through atomic bridge");
        Inspect(cold,a,b);
        Check(cold.Current.Snapshot.Generation==3,"exact original generation restored");
        Check(cold.LastStage!=null&&!cold.LastStage.Aborted,"published staged world not discarded");
        Check(!load.Quarantined,"cold load not quarantined");
        // Once detached ID was reconstructed, original #510 reattach one time only.
        for(int slot=0;slot<2;slot++)
        {
            if(cold.Current.Snapshot.Parts[slot].Consumed)
            {
                long id=FrozenPartCatalog.Id(slot);
                var ledger=cold.Current.Ledgers[slot];
                Check(!ledger.TryDetach(id,Zero,Zero,Zero,1,0,0,out _),
                      "double detach denied after cold restore");
                Check(ledger.TryReattach(id,()=>{},()=>{}),"real R2 original first rearm accepted");
                Check(!ledger.TryReattach(id,()=>{},()=>{}),"real R2 original repeat rearm rejected");
            }
        }
        roundtrips++;
    }
    static void Refuse(FakeWorldHost host,byte[]? wire,BridgeResult expected,string name)
    {
        var old=host.Current;
        var b=new FrozenSaveHostBridge(host);
        Check(b.TryLoad(wire,77,1)==expected,name+" expected verdict");
        Check(ReferenceEquals(old,host.Current),name+" no old world mutation");
        denials++;
    }
    static void Negatives()
    {
        var raw=Wire(true,true);
        var corrupt=(byte[])raw.Clone();corrupt[corrupt.Length/2]^=0x40;
        Refuse(new FakeWorldHost(Snap(false,false,1)),corrupt,
            BridgeResult.InvalidSnapshot,"tampered checksum");
        Refuse(new FakeWorldHost(Snap(false,false,1)),null,
            BridgeResult.InvalidSnapshot,"missing bytes");
        Refuse(new FakeWorldHost(Snap(false,false,1)),new byte[3],
            BridgeResult.InvalidSnapshot,"truncation");
        Refuse(new FakeWorldHost(Snap(false,false,1)),Wire(true,true,3,78),
            BridgeResult.WrongRun,"foreign run");
        Refuse(new FakeWorldHost(Snap(false,false,4)),Wire(true,true,3),
            BridgeResult.StaleGeneration,"stale generation");
        Refuse(new FakeWorldHost(Snap(false,false,1)){Idle=false},raw,
            BridgeResult.Busy,"active detach dock or reset");
        var notReady=new FakeWorldHost(Snap(false,false,1)){StageNotReady=true};
        Refuse(notReady,raw,BridgeResult.Rejected,"stage not ready");
        Check(notReady.LastStage!.Aborted,"failed stage disposed");
        var wrongLedger=new FakeWorldHost(Snap(false,false,1)){StageCorrupt=true};
        Refuse(wrongLedger,raw,BridgeResult.Rejected,"staged W06 ledger mismatch");
        Check(wrongLedger.LastStage!.Aborted,"wrong ledger stage discarded");
        var rejected=new FakeWorldHost(Snap(false,false,1)){RejectPublish=true};
        Refuse(rejected,raw,BridgeResult.Rejected,"host rejects publication");
        Check(rejected.LastStage!.Aborted,"rejected stage discarded");
        Refuse(new FakeWorldHost(Snap(false,false,1)){RejectOnSecondIdle=true},raw,
            BridgeResult.Busy,"host becomes busy before commit");
        var throwsStage=new FakeWorldHost(Snap(false,false,1)){ThrowStage=true};
        Refuse(throwsStage,raw,BridgeResult.Quarantine,"offstage creation throws");
        Check(throwsStage.Quarantined,"stage throw quarantined");
        var beforePublish=new FakeWorldHost(Snap(false,false,1)){MutateWorldWhileStaging=true};
        var original=beforePublish.Current;
        var bridge=new FrozenSaveHostBridge(beforePublish);
        Check(bridge.TryLoad(raw,77,1)==BridgeResult.Busy,
             "host pointer changed while staging refused");
        Check(!ReferenceEquals(original,beforePublish.Current),
             "host's own invalid mutation visible: bridge did not claim successful load");
        // Enforced integration requirement: host MUST stage without touching live world.
        var after=new FakeWorldHost(Snap(false,false,1)){PublishThenThrow=true};
        Refuse(after,raw,BridgeResult.Rejected,"after-publish exception rolled back");
        Check(after.RestoreCalls==1&&!after.Quarantined,"real compensation called");
        rollbacks++;
        var afterFalse=new FakeWorldHost(Snap(false,false,1)){PublishThenFalse=true};
        Refuse(afterFalse,raw,BridgeResult.Rejected,"false-after-commit compensated");
        Check(afterFalse.RestoreCalls==1,"false-with-side-effects detected");
        rollbacks++;
        var cannotRollback=new FakeWorldHost(Snap(false,false,1)){PublishThenThrow=true,FailRestore=true};
        var old=cannotRollback.Current;
        var quarantineBridge=new FrozenSaveHostBridge(cannotRollback);
        Check(quarantineBridge.TryLoad(raw,77,1)==BridgeResult.Quarantine,
              "rollback failure returns quarantine NOT ACCEPTED");
        Check(quarantineBridge.Quarantined&&cannotRollback.Quarantined,
              "rollback failure permanently quarantines session");
        Check(!ReferenceEquals(old,cannotRollback.Current),"partial state exposed to caller as quarantine");
        Check(quarantineBridge.TryLoad(raw,77,1)==BridgeResult.Quarantine,
              "quarantined bridge cannot be used");
        denials++;
        var saving=new FakeWorldHost(Snap(true,false));
        var saver=new FrozenSaveHostBridge(saving);
        Check(saver.TrySave(out var saved)==BridgeResult.Accepted && saved!=null,
              "valid capture from ACTUAL W06 ledger");
        saving.Idle=false;
        Check(saver.TrySave(out var deniedSave)==BridgeResult.Busy && deniedSave==null,
              "deny Save during active transaction");
        saving.Idle=true;saving.ThrowCapture=true;
        Check(saver.TrySave(out _)==BridgeResult.Quarantine && saver.Quarantined,
              "capture exception quarantined");
        var callback=new FakeWorldHost(Snap(false,false,1));
        var nestedBridge=new FrozenSaveHostBridge(callback);
        callback.IdleCallback=()=>{
            callback.IdleCallback=null;
            Check(nestedBridge.TryLoad(raw,77,1)==BridgeResult.Busy,
                  "synchronous nested load blocked");
        };
        Check(nestedBridge.TryLoad(raw,77,1)==BridgeResult.Accepted,
              "outer load still succeeds despite rejected nested attempt");
        var badBytes=(byte[])raw.Clone();
        var malicious=new FakeWorldHost(Snap(false,false,1));
        var maliciousBridge=new FrozenSaveHostBridge(malicious);
        malicious.IdleCallback=()=>{
            malicious.IdleCallback=null;
            Array.Fill(badBytes,(byte)0);
        };
        Check(maliciousBridge.TryLoad(badBytes,77,1)==BridgeResult.Accepted,
              "input buffer copied before malicious host callback");
        Inspect(malicious,true,true);
        var invalidCapture=new FakeWorldHost(Snap(false,false,1));
        invalidCapture.MutateWorldWhileStaging=true;
    }
    public static int Main()
    {
        try
        {
            var temp=Path.Combine(Path.GetTempPath(),"b01_62_w15_"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                foreach(bool a in new[]{false,true})
                foreach(bool b in new[]{false,true})Roundtrip(a,b,temp);
                Negatives();
            }
            finally{Directory.Delete(temp,true);}
            Console.WriteLine($"B01_62_W15_ATOMIC_HOST_PORT_ASSERTIONS_PASS={checks}");
            Console.WriteLine($"ORIGINAL_556_DISK_COLD_ROUNDTRIPS={roundtrips} NEGATIVE_DENIAL_GROUPS={denials} EXCEPTION_ROLLBACKS={rollbacks}");
            Console.WriteLine("UNITY_HOST_PRIVATE_510_BINDING=NOT_IMPLEMENTED NATIVE_UNITY_G1=NOT_RUN G2_HOLD");
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
