// B01-62 W29 cold entry from exact original #826 world/econ bytes
// SOURCE / ACTUAL OS FILES ONLY. NOT Unity Editor/PhysX/Player gameplay.
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using B01.CanonicalWorldApi001;
using B01.CanonicalColdRead003;
using B01.DetachedSaveContract001;
using B01.WholeGameEconomy003;
using B01.SecureReattachV2;
using RoboRougelike.B01.Economy.ApprovedV7;

internal static class W29ColdReadQa
{
    private const long Run=6290062;
    private static readonly Guid Slot=Guid.Parse("62902629-0000-4000-8000-000000000556");
    private static int pass,fail;
    private sealed class Gate:ICanonicalMutationGate { public bool IsQuiescent()=>false; }
    private sealed class Factory:ICanonicalRigFactory
    {
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore c)
            =>throw new InvalidOperationException("No source fixture can stage physical Unity");
    }
    private sealed class Port:ICanonicalCommitPort
    {
        public bool Read(long run,out long gen,out string hash)
        {gen=0;hash="";return false;}
        public bool Commit(long run,long prior,string oldHash,long next,string nextHash)
            =>false;
    }
    private static void Check(string name,Func<bool> test)
    {
        try
        {
            if(test()){pass++;Console.WriteLine("PASS "+name);}
            else{fail++;Console.WriteLine("FAIL "+name);}
        }
        catch(Exception ex){fail++;Console.WriteLine("FAIL "+name+" "+ex);}
    }
    private static bool Scratch(Func<string,bool> action)
    {
        string path=Path.Combine(Path.GetTempPath(),
            "b0162-w29-cold-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        try{return action(path);}
        finally{Directory.Delete(path,true);}
    }
    private static string StoreDir(string root)
        =>Path.Combine(root,"b0162_canonical_single_authority_v3",
          "run_"+Run+"_slot_"+Slot.ToString("N"));
    private static byte[] World(long gen,bool lightDetached)
    {
        var parts=new PersistedPart[FrozenPartCatalog.Slots];
        for(int i=0;i<parts.Length;i++)
        {
            bool detached=i==0&&lightDetached;
            var spec=SecureRigCatalog.At(i);
            long owned=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(spec.PartId,owned,detached?0:owned,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                detached?PartDockState.Detached:PartDockState.Attached,
                detached,!detached,
                new Vector3d(spec.RootX+spec.PartOffsetX,detached?2.2:1.2,0),
                0,0,0,1,new Vector3d(detached?0.25:0,0,0),
                new Vector3d(0,0,0),spec.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(Run,gen,parts));
    }
    private static ApprovedEconomySnapshotV7 Economy(long gen,int scrap)
    {
        var parts=new List<OwnedModuleV7>();
        for(int i=0;i<FrozenPartCatalog.Slots;i++)
            parts.Add(new OwnedModuleV7{
                Id=WholeGameEconomySlotV3.EconomicPartId(FrozenPartCatalog.Id(i)),
                Category=i==0?"Core":"Weapon",Hp=1,PurchaseScrap=100,
                ReservationId=""
            });
        return new ApprovedEconomySnapshotV7{
            Version=7,OwnerId=Slot.ToString("N"),RunId=Run.ToString(),
            Generation=gen,Scrap=scrap,Wins=0,FreeUsedForWin=0,
            Parts=parts,Pending=new List<RebuildTicketV7>(),
            ConsumedVictoryIds=new List<string>(),
            Loaner=new OneShotLoanerV7{Phase=LoanerPhaseV7.None,
                EverEntered=false,EncounterId="",PermitId=""}
        };
    }
    private static bool Save(string root,bool second,out byte[] expected,
        out ApprovedEconomySnapshotV7? expectedEconomy)
    {
        expected=Array.Empty<byte>();expectedEconomy=null;
        CanonicalWorldSlotLease? owner=null;
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out owner)||
            owner==null)return false;
        var gateway=new CanonicalWorldGateway(new Gate(),new Factory(),new Port(),owner);
        Directory.CreateDirectory(Path.GetDirectoryName(StoreDir(root))!);
        WholeGameEconomySlotV3? writer=null;
        bool result=false;
        try
        {
            if(WholeGameEconomySlotV3.Create(StoreDir(root),Run,Slot,owner,
                out writer)!=WholeGameResult.Accepted||writer==null)
                return false;
            byte[] w1=World(1,true);
            var e1=Economy(1,75);
            if(writer.Commit(0,w1,e1)!=WholeGameResult.Accepted)return false;
            expected=w1;expectedEconomy=e1;
            if(second)
            {
                if(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(e1,
                    "w29-genuine-fixture-win-1","w29-fixture-encounter-1",
                    out var e2,out _)||e2==null)return false;
                e2.Scrap=24;
                byte[] w2=World(2,false);
                if(writer.Commit(1,w2,e2)!=WholeGameResult.Accepted)return false;
                expected=w2;expectedEconomy=e2;
            }
            result=true;
        }
        finally
        {
            writer?.Dispose();
            if(!gateway.TryCloseAfterPhysicalRetirement(()=>true))
                result=false;
        }
        return result;
    }
    private static bool EmptyNonMutating()
      =>Scratch(root=>{
        if(CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
            out long gen,out byte[] world,out var econ)!=WholeGameResult.Empty||
            gen!=0||world.Length!=0||econ!=null||
            Directory.Exists(Path.Combine(root,"b0123_canonical_world_authority")))
            return false;
        Console.WriteLine("W29_EMPTY_NO_DISK_LEASE_CREATION=PASS");
        return true;
      });
    private static bool Cold(bool second)
      =>Scratch(root=>{
        if(!Save(root,second,out byte[] expected,out var expectedEconomy)||
            expectedEconomy==null)return false;
        if(CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
            out long gen,out byte[] loaded,out var econ)!=WholeGameResult.Accepted||
            econ==null||gen!=expectedEconomy.Generation||
            !loaded.SequenceEqual(expected)||econ.Scrap!=expectedEconomy.Scrap||
            econ.Wins!=expectedEconomy.Wins||
            econ.ConsumedVictoryIds.Count!=expectedEconomy.ConsumedVictoryIds.Count)
            return false;
        if(!ColdRestore.TryStage(loaded,out var staged)||staged==null||
            staged.Snapshot.Generation!=gen)return false;
        // The temporary read-only .owner MUST be retired so normal sessions
        // can re-acquire the exact slot immediately.
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out var newOwner)||
            newOwner==null)return false;
        if(!newOwner.TryCancelUnbound())return false;
        if(second&&ApprovedEconomyPolicyV7.TryStageTrustedVictory(econ,
            "w29-genuine-fixture-win-1","w29-fixture-encounter-1",
            out _,out _))return false;
        Console.WriteLine(second
            ?"W29_COLD_PAIR_V7_VICTORY_AFTER_SECOND_ATOMIC_REPLACE=PASS"
            :"W29_COLD_PAIR_V7_FIRST_GENERATION=PASS");
        return true;
      });
    private static bool BusyDoesNotLeak()
      =>Scratch(root=>{
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out var owner)||
            owner==null)return false;
        var gateway=new CanonicalWorldGateway(new Gate(),new Factory(),new Port(),owner);
        Directory.CreateDirectory(Path.GetDirectoryName(StoreDir(root))!);
        WholeGameEconomySlotV3? writer=null;
        try
        {
            if(WholeGameEconomySlotV3.Create(StoreDir(root),Run,Slot,owner,
                out writer)!=WholeGameResult.Accepted||writer==null||
                writer.Commit(0,World(1,true),Economy(1,34))!=
                    WholeGameResult.Accepted)return false;
            if(CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
                out long gen,out byte[] world,out var econ)!=WholeGameResult.Busy||
                gen!=0||world.Length!=0||econ!=null)return false;
            Console.WriteLine("W29_READ_WHILE_OWNER_LIVE_FAILS_CLOSED_NO_PAIR=PASS");
            return true;
        }
        finally
        {
            writer?.Dispose();
            gateway.TryCloseAfterPhysicalRetirement(()=>true);
        }
      });
    private static bool CorruptFailsClosed()
      =>Scratch(root=>{
        if(!Save(root,false,out _,out _))return false;
        string path=Path.Combine(StoreDir(root),"save.envelope");
        byte[] corrupt=File.ReadAllBytes(path);corrupt[corrupt.Length-3]^=0x73;
        File.WriteAllBytes(path,corrupt);
        if(CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
            out long gen,out byte[] world,out var econ)!=WholeGameResult.Corrupt||
            gen!=0||world.Length!=0||econ!=null)return false;
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out var owner)||
            owner==null||!owner.TryCancelUnbound())return false;
        Console.WriteLine("W29_CORRUPT_COLD_PAIR_NO_LEAK_OWNER_RELEASED=PASS");
        return true;
      });

    // Source-only controlled physical-world stand-in. Uses the actual
    // #729 gateway/session/CAS, not an independent test Save implementation.
    private sealed class QuiescentGate:ICanonicalMutationGate
    {
        public bool IsQuiescent()=>true;
    }
    private sealed class ReplayedWorld:ICanonicalStagedWorld
    {
        private readonly RunSnapshot source;
        public bool retired;
        public ReplayedWorld(RunSnapshot snapshot){source=snapshot;}
        public RunSnapshot Capture()=>source;
        public void Activate(){}
        public void Deactivate(){}
        public void Retire(){retired=true;}
    }
    private sealed class ReplayFactory:ICanonicalRigFactory
    {
        private readonly List<ReplayedWorld> worlds=new List<ReplayedWorld>();
        public bool AllGone()=>worlds.All(x=>x.retired);
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot snapshot,
            ColdRestore plan)=>new OwnedReplayStage(this,snapshot);
        private sealed class OwnedReplayStage:ICanonicalStageOwnership
        {
            private readonly ReplayFactory factory;
            private readonly RunSnapshot snapshot;
            private ReplayedWorld? world;
            public OwnedReplayStage(ReplayFactory f,RunSnapshot s)
            {factory=f;snapshot=s;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                world=new ReplayedWorld(snapshot);
                factory.worlds.Add(world);
                return world;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                if(world!=null)world.Retire();
                return world==null||world.retired;
            }
        }
    }
    private static bool ColdReadThenOriginalTypedGateway()
      =>Scratch(root=>{
        if(!Save(root,true,out byte[] saved,out var savedEconomy)||
            savedEconomy==null)return false;
        string file=Path.Combine(StoreDir(root),"save.envelope");
        byte[] before=File.ReadAllBytes(file);
        if(CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
            out long gen,out byte[] coldBytes,out var coldEcon)!=
            WholeGameResult.Accepted||coldEcon==null||
            gen!=2||!coldBytes.SequenceEqual(saved))return false;
        var factory=new ReplayFactory();
        if(B01.CanonicalWholeGameAdapter003.CanonicalWholeGameSessionV3.TryOpen(
            root,Run,Slot,false,new QuiescentGate(),factory,out var session)!=
            B01.CanonicalWholeGameAdapter003.WholeGameSessionOpen.Accepted||
            session==null)return false;
        try
        {
            if(session.TryRestore(coldBytes,coldEcon,Run,gen)!=
               CanonicalGateResult.Accepted||!session.HasWorld||
               !session.TryCapture(out byte[] preview,out var wallet)||
               wallet==null||!preview.SequenceEqual(coldBytes)||
               wallet.Scrap!=24||wallet.Wins!=1||
               wallet.ConsumedVictoryIds.Count!=1)return false;
            if(B01.CanonicalWholeGameAdapter003.CanonicalWholeGameSessionV3.TryOpen(
               root,Run,Slot,false,new QuiescentGate(),new ReplayFactory(),
               out var concurrent)!=B01.CanonicalWholeGameAdapter003.WholeGameSessionOpen.Busy||
               concurrent!=null)return false;
            if(!File.ReadAllBytes(file).SequenceEqual(before))return false;
            if(ApprovedEconomyPolicyV7.TryStageTrustedVictory(wallet,
                "w29-genuine-fixture-win-1","w29-fixture-encounter-1",
                out _,out _))return false;
            if(!session.TryCloseAfterPhysicalRetirement(factory.AllGone))
                return false;
            Console.WriteLine("W29_COLD_BOOT_ORIGINAL_826_TYPED_GATEWAY_RESTORE=PASS");
            return true;
        }
        finally
        {
            session.TryCloseAfterPhysicalRetirement(factory.AllGone);
            session.Dispose();
        }
      });
    public static int Main()
    {
        Check("No Save file: cold reader does not create owner",EmptyNonMutating);
        Check("Actual disk generation1 read with ECON V7",()=>Cold(false));
        Check("Actual disk generation2 victory read with ECON V7",()=>Cold(true));
        Check("Busy writer blocks cold reader without bytes",BusyDoesNotLeak);
        Check("Corrupt original envelope blocks cold read and releases owner",CorruptFailsClosed);
        Check("Cold pair restores via original #826 gateway with same owner",ColdReadThenOriginalTypedGateway);
        Console.WriteLine("B0162_W29_COLD_READ_REAL_OS_TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
        Console.WriteLine("B0162_W29_SOURCE_ONLY_NATIVE_UNITY_NOT_RUN");
        return fail==0?0:1;
    }
}
