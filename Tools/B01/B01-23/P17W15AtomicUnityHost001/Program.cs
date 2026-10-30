using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;
using B01.SecureSaveHost001;

internal sealed class FakeWorld : IIsolatedRigWorld
{
    public RunSnapshot Snapshot;
    public bool IsVisible {get;private set;}
    public bool IsBusy {get;set;}
    public bool FailHide, FailShow, FailDispose, Disposed;
    public FakeWorld(RunSnapshot snap){Snapshot=snap;}
    public RunSnapshot Capture(long run,long gen)
    {
        return new RunSnapshot(run,gen,Snapshot.Parts);
    }
    public void SetVisible(bool visible)
    {
        if(visible&&FailShow)throw new InvalidOperationException("failed activation");
        if(!visible&&FailHide)throw new InvalidOperationException("failed deactivation");
        IsVisible=visible;
    }
    public void Dispose(){if(FailDispose)throw new IOException("failed cleanup");Disposed=true;IsVisible=false;}
}
internal sealed class FakeFactory : IIsolatedRigFactory
{
    public readonly List<FakeWorld> Worlds =new List<FakeWorld>();
    public bool ThrowOnBuild;
    public bool AlterId;
    public bool MakeVisible;
    public bool FailNextShow;
    public Action? DuringBuild;
    public IIsolatedRigWorld Build(ColdRestore plan)
    {
        DuringBuild?.Invoke();
        if(ThrowOnBuild)throw new IOException("injected factory failure BEFORE return");
        var parts=plan.Snapshot.Parts;
        if(AlterId)
        {
            var p=parts[0];
            parts[0]=new PersistedPart(99999,p.OwnerId,p.ParentId,p.AssetGuid,p.SocketGuid,
                p.State,p.Consumed,p.Kinematic,p.Position,p.Qx,p.Qy,p.Qz,p.Qw,
                p.LinearVelocity,p.AngularVelocity,p.MassKg);
        }
        var world=new FakeWorld(new RunSnapshot(plan.Snapshot.RunId,plan.Snapshot.Generation,parts));
        world.FailShow=FailNextShow;Worlds.Add(world);
        if(MakeVisible)world.SetVisible(true);
        return world;
    }
}
internal static class Program
{
    static int passes;
    static void Assert(bool value,string label)
    {
        if(!value)throw new Exception("ASSERTION_RED "+label);
        Console.WriteLine("PASS "+label);passes++;
    }
    static PersistedPart Part(int n,bool detached,long? id=null,bool? consumed=null)
    {
        long owner=FrozenPartCatalog.Owner(n);
        return new PersistedPart(id??FrozenPartCatalog.Id(n),owner,detached?0:owner,
            FrozenPartCatalog.Asset(n),FrozenPartCatalog.Socket(n),
            detached?PartDockState.Detached:PartDockState.Attached,consumed??detached,!detached,
            new Vector3d(n==0?1:5,2,0),0,0,0,1,
            detached?new Vector3d(.25,0,0):new Vector3d(0,0,0),
            new Vector3d(0,0,0),FrozenPartCatalog.Mass(n));
    }
    static byte[] Save(bool a,bool b,long gen=1,long run=77)
        =>SnapshotCodec.Encode(new RunSnapshot(run,gen,new[]{Part(0,a),Part(1,b)}));
    static AtomicColdUnityHostBridge Host(FakeFactory factory) =>
        new AtomicColdUnityHostBridge(77,factory,()=>true);
    static void TestCold(bool a,bool b)
    {
        var factory=new FakeFactory();var h=Host(factory);
        Assert(h.TryLoad(Save(a,b,1),1)==RestoreVerdict.Accepted,"cold first load "+a+b);
        Assert(h.HasWorld&&factory.Worlds[0].IsVisible,"first world active "+a+b);
        Assert(h.TrySave(out byte[] bytes)==RestoreVerdict.Accepted,"physical state saved "+a+b);
        Assert(ColdRestore.TryStage(bytes,out ColdRestore? cold)&&cold!=null,"original #556 W06 ledger recreated "+a+b);
        Assert(cold!.ContainsConsumedId(0,2310001)==a && cold!.ContainsConsumedId(1,2310002)==b,"detached consumed stable IDs "+a+b);
        var restart=Host(new FakeFactory());
        Assert(restart.TryLoad(bytes,1)==RestoreVerdict.Accepted,"new process/host restart cold loads "+a+b);
        Assert(restart.TrySave(out byte[] again)==RestoreVerdict.Accepted,"cold round-trip save "+a+b);
        Assert(bytes.SequenceEqual(again),"same exact source serialization after restart "+a+b);
    }
    static void Main()
    {
        TestCold(false,false);TestCold(true,false);TestCold(false,true);TestCold(true,true);
        var f=new FakeFactory();var h=Host(f);
        Assert(h.TryLoad(Save(true,false,4),1)==RestoreVerdict.Accepted,"baseline consumed detached world");
        var prev=f.Worlds[0];
        Assert(h.TryLoad(Save(false,true,5),1)==RestoreVerdict.Accepted,"whole world switched to new state");
        Assert(!prev.IsVisible&&prev.Disposed,"old world disposed only after successful new activation");
        Assert(h.TrySave(out byte[] saved)==RestoreVerdict.Accepted,"new world save success");
        Assert(ColdRestore.TryStage(saved,out ColdRestore? now)&&now!.ContainsConsumedId(1,2310002)&&!now!.ContainsConsumedId(0,2310001),
            "consumed original W06 ledger follows only new world");
        var active=f.Worlds[1];
        var corrupt=(byte[])Save(true,true,6).Clone();corrupt[100]^=1;
        Assert(h.TryLoad(corrupt,1)==RestoreVerdict.InvalidSnapshot,"corrupt checksum rejected before staging");
        Assert(ReferenceEquals(f.Worlds[1],active)&&active.IsVisible&&f.Worlds.Count==2,"corrupt save zero new physical worlds");
        Assert(h.TryLoad(Save(true,true,6,88),1)==RestoreVerdict.WrongRun,"foreign run rejected");
        Assert(h.TryLoad(Save(true,true,2),1)==RestoreVerdict.StaleGeneration,"stale generation rejected");
        Assert(h.TryLoad(Save(true,true,6).Take(90).ToArray(),1)==RestoreVerdict.InvalidSnapshot,"truncated save rejected");
        Assert(h.TryLoad(Save(true,true,6).Concat(new byte[]{1}).ToArray(),1)==RestoreVerdict.InvalidSnapshot,"trailing save rejected");
        Assert(active.IsVisible&&h.Generation==5,"rejected saves preserve old visible state and generation");
        f.AlterId=true;
        Assert(h.TryLoad(Save(true,true,6),1)==RestoreVerdict.InvalidSnapshot,"foreign staged ID denied before switch");
        Assert(f.Worlds.Last().Disposed&&active.IsVisible&&h.Generation==5,"invalid staged world safely disposed");
        f.AlterId=false;
        f.MakeVisible=true;
        Assert(h.TryLoad(Save(true,true,6),1)==RestoreVerdict.Quarantine,"factory violated stage-invisible rule");
        Assert(f.Worlds.Last().Disposed&&active.IsVisible&&h.Generation==5,"visible foreign staging cleaned");
        f.MakeVisible=false;
        f.FailNextShow=true;
        Assert(h.TryLoad(Save(true,true,6),1)==RestoreVerdict.Quarantine,"injected physical candidate activation exception");
        Assert(active.IsVisible&&f.Worlds.Last().Disposed&&h.Generation==5,"activation rollback old world preserved");
        f.FailNextShow=false;
        active.IsBusy=true;
        Assert(h.TryLoad(Save(true,true,6),1)==RestoreVerdict.TransactionBusy,"mid-transaction active host busy denied");
        Assert(h.TrySave(out byte[] zero)==RestoreVerdict.TransactionBusy&&zero.Length==0,"busy host blocks Save");
        active.IsBusy=false;
        f.ThrowOnBuild=true;
        Assert(h.TryLoad(Save(true,true,6),1)==RestoreVerdict.Quarantine,"factory throws with zero published changes");
        Assert(h.IsQuarantined&&active.IsVisible&&h.Generation==5,"factory fault quarantined and old world preserved");
        Assert(h.TryLoad(Save(true,true,7),1)==RestoreVerdict.Quarantine,"quarantine cannot silently recover");
        var f2=new FakeFactory();var h2=Host(f2);
        Assert(h2.TryLoad(Save(true,false,1),1)==RestoreVerdict.Accepted,"second independent baseline");
        var old2=f2.Worlds[0];
        old2.FailHide=true;
        Assert(h2.TryLoad(Save(false,true,2),1)==RestoreVerdict.Quarantine,"injected old deactivation failure");
        Assert(old2.IsVisible&&!f2.Worlds[1].IsVisible&&f2.Worlds[1].Disposed,"old hide failure does not publish");
        var f3=new FakeFactory();var bridge=Host(f3);
        Assert(bridge.TryLoad(Save(true,false),1)==RestoreVerdict.Accepted,"reentry baseline");
        var old3=f3.Worlds[0];RestoreVerdict nested=RestoreVerdict.Accepted;
        f3.DuringBuild=()=>nested=bridge.TryLoad(Save(false,true,3),1);
        Assert(bridge.TryLoad(Save(false,true,2),1)==RestoreVerdict.Accepted&&nested==RestoreVerdict.TransactionBusy,
            "nested build cannot load while outer load owns transaction");
        Assert(!old3.IsVisible&&f3.Worlds[1].IsVisible,"nested denial preserved outer atomic swap");
        var f4=new FakeFactory();bool idle=true;
        var host4=new AtomicColdUnityHostBridge(77,f4,()=>idle);
        Assert(host4.TryLoad(Save(true,false),1)==RestoreVerdict.Accepted,"idle callback baseline");
        var old4=f4.Worlds[0];
        f4.DuringBuild=()=>idle=false;
        Assert(host4.TryLoad(Save(false,true,2),1)==RestoreVerdict.TransactionBusy,
            "second host idle check denies after-stage busy");
        Assert(old4.IsVisible&&host4.Generation==1&&f4.Worlds[1].Disposed,
            "after-stage busy preserves exact old world/ID");
        var f5=new FakeFactory();var host5=Host(f5);
        Assert(host5.TryLoad(Save(true,false),1)==RestoreVerdict.Accepted,"cleanup rollback baseline");
        f5.Worlds[0].FailDispose=true;
        Assert(host5.TryLoad(Save(false,true,2),1)==RestoreVerdict.Quarantine,
            "after-commit old-world disposal fault not reported success");
        Assert(host5.IsQuarantined&&host5.Generation==2&&f5.Worlds[1].IsVisible,
            "commit fault quarantines without claiming rollback success");
        Console.WriteLine("ATOMIC_UNITY_HOST_SOURCE_ASSERTIONS_PASS="+passes+
            " REAL_ORIGINAL_W06_AND_FROZEN_556=YES UNITY_NATIVE=NOT_RUN CANONICAL_510_INTEGRATED=NO");
    }
}
