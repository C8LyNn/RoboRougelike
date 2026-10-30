#nullable enable
using System;
using System.IO;
using System.Threading;
using B01.CanonicalWorldApi001;
using B01.CanonicalSlotAdapter002;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

internal static class ActualOriginalR2Child
{
    private const long Run=61923;
    private static readonly Guid Id=Guid.Parse("00c11161-2023-4111-8111-000000000729");
    private static byte[] Data(long gen,bool light,bool heavy)
    {
        var parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            var s=SecureRigCatalog.At(i);
            bool det=i==0?light:heavy;
            long owner=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(s.PartId,owner,det?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                det?PartDockState.Detached:PartDockState.Attached,
                det,!det,new Vector3d(s.RootX+s.PartOffsetX,det?2.2:1.2,0),
                0,0,0,1,det?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),s.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(Run,gen,parts));
    }
    private sealed class Gate:ICanonicalMutationGate
    {
        public bool IsQuiescent()=>true;
    }
    private sealed class World:ICanonicalStagedWorld
    {
        readonly RunSnapshot snapshot;
        public bool Live,Retired;
        public World(RunSnapshot s){snapshot=s;}
        public RunSnapshot Capture()=>snapshot;
        public void Activate(){Live=true;}
        public void Deactivate(){Live=false;}
        public void Retire(){Retired=true;Live=false;}
    }
    private sealed class Factory:ICanonicalRigFactory
    {
        public World? Last;
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot snap,ColdRestore cold)
        {
            if(cold.Snapshot.RunId!=snap.RunId||
               cold.ContainsConsumedId(0,FrozenPartCatalog.Id(0))!=snap.Parts[0].Consumed)
                throw new InvalidOperationException("R2 child actual original W06 ledger mismatch");
            return new Owned(this,snap);
        }
        private sealed class Owned:ICanonicalStageOwnership
        {
            readonly Factory owner; readonly RunSnapshot snapshot;
            public Owned(Factory f,RunSnapshot s){owner=f;snapshot=s;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                owner.Last=new World(snapshot);
                return owner.Last;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                owner.Last?.Retire();
                return true;
            }
        }
        public bool RetireAll()
        {
            Last?.Retire();
            return Last==null||Last.Retired;
        }
    }
    public static int Main(string[] args)
    {
        if(args.Length!=5)return 9;
        string slot=Path.Combine(args[0],"owned-slot");
        var factory=new Factory();
        var status=CanonicalSlotSessionR2.TryOpen(slot,Run,Id,false,out var session);
        if(status!=CanonicalSessionOpenStatus.Accepted||session==null||
           !session.Attach(new Gate(),factory))return 2;
        try
        {
            if(session.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
               factory.Last==null||!factory.Last.Live||!session.HasWorld)return 3;
            File.WriteAllText(args[1],"R2_ORIGINAL_691_712_LIVE_G1_LIGHT");
            if(!SpinWait.SpinUntil(()=>File.Exists(args[2]),16000))return 4;
            if(session.TryRestore(Data(2,false,true),Run,2)!=CanonicalGateResult.Accepted||
               factory.Last==null||!factory.Last.Live||
               !session.Read(Run,out long g,out string hash)||g!=2||
               hash!=CanonicalSlotSessionR2.SourceHash(Data(2,false,true)))
                return 5;
            File.WriteAllText(args[3],"R2_ORIGINAL_669_PHYSICAL_GEN2_HEAVY");
            if(!SpinWait.SpinUntil(()=>File.Exists(args[4]),16000))return 6;
            return 0;
        }
        finally
        {
            bool done=factory.RetireAll();
            session.TryCloseAfterPhysicalRetirement(()=>done);
            session.Dispose();
        }
    }
}
