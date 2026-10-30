// B01-62 W25 original frozen #556 WORLD + original B01-22 #764 economic reducer,
// both one real OS owned physical file; test-world is managed only, NOT Unity PhysX.
#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using B01.WholeGameEconomy001;
using B01.CanonicalWholeGameAdapter001;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using RoboRougelike.B01.Economy.ApprovedV6;
internal static class W25GameEconomyQA
{
    private const long Run=6250062;
    private static readonly Guid Slot=Guid.Parse("62502625-0000-4000-8000-000000000556");
    private static int passed,failed;
    private static void Check(string name,Func<bool> work)
    {
        try{if(work()){passed++;Console.WriteLine("PASS "+name);}
            else{failed++;Console.WriteLine("FAIL "+name);}}
        catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+" "+ex);}
    }
    private sealed class Gate:ICanonicalMutationGate{public bool IsQuiescent()=>true;}
    private sealed class Factory:ICanonicalRigFactory
    {
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore c)
            =>throw new InvalidOperationException("No Unity staging permitted in source QA");
    }
    private sealed class Port:ICanonicalCommitPort
    {
        public bool Read(long run,out long gen,out string hash){gen=0;hash="";return false;}
        public bool Commit(long run,long prior,string priorHash,long next,string hash)=>false;
    }
    private sealed class AckLost:IOneFilePublisher
    {
        public void Publish(string stage,string dest)
        {
            new OneFileOsPublisher().Publish(stage,dest);
            throw new IOException("TRUE on-disk atomic rename followed by LOST ACK");
        }
    }
    private sealed class BeforePublish:IOneFilePublisher
    {
        public void Publish(string stage,string dest)
            =>throw new IOException("fault before actual physical publish");
    }
    private static string Dir(string root)=>Path.Combine(root,
        "b0162_canonical_single_authority_v3","run_"+Run+"_slot_"+Slot.ToString("N"));
    private static bool Scratch(Func<string,bool> execute)
    {
        string scratch=Path.Combine(Path.GetTempPath(),
            "b0162-w25-game-econ-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        try{return execute(scratch);}
        finally{Directory.Delete(scratch,true);}
    }
    private static bool OpenOwner(string root,out CanonicalWorldSlotLease? owner,
        out CanonicalWorldGateway? gateway)
    {
        owner=null;gateway=null;
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out owner)||
            owner==null)return false;
        gateway=new CanonicalWorldGateway(new Gate(),new Factory(),new Port(),owner);
        return true;
    }
    private static byte[] World(long generation,bool light,bool heavy)
    {
        PersistedPart[] arr=new PersistedPart[FrozenPartCatalog.Slots];
        for(int i=0;i<arr.Length;i++)
        {
            bool detached=i==0?light:heavy;
            var s=SecureRigCatalog.At(i);long owner=FrozenPartCatalog.Owner(i);
            arr[i]=new PersistedPart(s.PartId,owner,detached?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                detached?PartDockState.Detached:PartDockState.Attached,
                detached,!detached,
                new Vector3d(s.RootX+s.PartOffsetX,detached?2.2:1.2,0),
                0,0,0,1,new Vector3d(detached?0.25:0,0,0),
                new Vector3d(0,0,0),s.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(Run,generation,arr));
    }
    private static ApprovedEconomySnapshotV6 Economy(long generation,int money=60)
    {
        return new ApprovedEconomySnapshotV6 {
            Version=6,OwnerId=Slot.ToString("N"),RunId=Run.ToString(),
            Generation=generation,Scrap=money,Wins=0,FreeUsedForWin=0,
            Parts=new List<OwnedModuleV6> {
                new OwnedModuleV6{Id=WholeGameEconomySlotV1.EconomicPartId(FrozenPartCatalog.Id(0)),
                    Category="Core",Hp=1,PurchaseScrap=100,ReservationId=""},
                new OwnedModuleV6{Id=WholeGameEconomySlotV1.EconomicPartId(FrozenPartCatalog.Id(1)),
                    Category="Weapon",Hp=1,PurchaseScrap=100,ReservationId=""}
            },
            Pending=new List<RebuildTicketV6>(),
            ConsumedVictoryIds=new List<string>(),
            Loaner=new OneShotLoanerV6{Phase=LoanerPhaseV6.None,EverEntered=false,
                EncounterId="",PermitId=""}
        };
    }
    private static bool RoundtripAndVictory()
    =>Scratch(baseDir=>{
        string root=Path.Combine(baseDir,"private");Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.GetDirectoryName(Dir(root))!);
        if(!OpenOwner(root,out var owner,out var gw)||owner==null||gw==null)return false;
        WholeGameEconomySlotV1? writer=null;
        try
        {
            string wrong=Path.Combine(baseDir,"FOREIGN",
                "run_"+Run+"_slot_"+Slot.ToString("N"));
            if(WholeGameEconomySlotV1.Create(wrong,Run,Slot,owner,out var foreign)
                !=WholeGameResult.Invalid||foreign!=null||Directory.Exists(wrong))
                return false;
            string caseAlias=Path.Combine(root,"B0162_canonical_single_authority_v3",
                "run_"+Run+"_slot_"+Slot.ToString("N"));
            if(WholeGameEconomySlotV1.Create(caseAlias,Run,Slot,owner,out foreign)
                !=WholeGameResult.Invalid||foreign!=null)return false;
            if(WholeGameEconomySlotV1.Create(Dir(root),Run,Slot,owner,out writer)
                !=WholeGameResult.Accepted||writer==null)return false;
            var e1=Economy(1);
            byte[] w1=World(1,true,false);
            if(!ApprovedEconomyPolicyV6.Valid(e1)||
                writer.Commit(0,w1,e1)!=WholeGameResult.Accepted)return false;
            if(WholeGameEconomySlotV1.Open(Dir(root),Run,Slot,owner,out var second)
                !=WholeGameResult.Busy||second!=null)return false;
            if(!ApprovedEconomyPolicyV6.TryStageTrustedVictory(
                e1,"victory-real-receipt-1","encounter-1",out var e2,out var reason)||
                e2==null||reason!="OK"||e2.Generation!=2||e2.Wins!=1)
                return false;
            e2.Scrap=39; // actual owner transaction's wallet balance, not UI sidecar
            byte[] w2=World(2,false,true);
            if(writer.Commit(1,w2,e2)!=WholeGameResult.Accepted)return false;
            if(writer.Read(out long gen,out byte[] current,
                out var econ)!=WholeGameResult.Accepted||
                gen!=2||!w2.SequenceEqual(current)||econ==null||
                econ.Scrap!=39||econ.Wins!=1||econ.ConsumedVictoryIds.Count!=1)
                return false;
            if(ApprovedEconomyPolicyV6.TryStageTrustedVictory(
                econ,"victory-real-receipt-1","encounter-1",out _,out _))
                return false;
            if(writer.Commit(1,World(2,true,false),e2)!=WholeGameResult.Stale)
                return false;
            writer.Dispose();
            if(WholeGameEconomySlotV1.Open(Dir(root),Run,Slot,owner,out second)
                !=WholeGameResult.Busy||second!=null)return false;
            if(!gw.TryCloseAfterPhysicalRetirement(()=>true))return false;
            if(!OpenOwner(root,out var coldLease,out var coldGateway)||
                coldLease==null||coldGateway==null)return false;
            try
            {
                if(WholeGameEconomySlotV1.Open(Dir(root),Run,Slot,coldLease,out var cold)
                    !=WholeGameResult.Accepted||cold==null)return false;
                try
                {
                    if(cold.Read(out long reopened,out byte[] world,
                        out var wallet)!=WholeGameResult.Accepted||reopened!=2||
                        !world.SequenceEqual(w2)||wallet==null||
                        wallet.Scrap!=39||wallet.Wins!=1||
                        wallet.ConsumedVictoryIds[0]!="victory-real-receipt-1")
                        return false;
                }
                finally{cold.Dispose();}
                Console.WriteLine("W25_GAME_ECON_ONE_FILE_WORLD_WALLET_VICTORY_COLD=PASS");
                return true;
            }
            finally{coldGateway.TryCloseAfterPhysicalRetirement(()=>true);}
        }
        finally{writer?.Dispose();gw.TryCloseAfterPhysicalRetirement(()=>true);}
    });
    private static bool Fault(bool after)
    =>Scratch(baseDir=>{
        string root=Path.Combine(baseDir,"private");Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.GetDirectoryName(Dir(root))!);
        if(!OpenOwner(root,out var owner,out var gw)||owner==null||gw==null)return false;
        WholeGameEconomySlotV1? store=null;
        try
        {
            if(WholeGameEconomySlotV1.Create(Dir(root),Run,Slot,owner,out store,
                after?(IOneFilePublisher)new AckLost():new BeforePublish())
                !=WholeGameResult.Accepted||store==null)return false;
            if(store.Commit(0,World(1,true,false),Economy(1,77))!=
                WholeGameResult.Quarantined||!store.Quarantined)return false;
            string final=Path.Combine(Dir(root),"save.envelope");
            if(File.Exists(final)!=after)return false;
            if(store.Commit(0,World(1,false,true),Economy(1,19))!=
                WholeGameResult.Quarantined)return false;
            if(!gw.TryCloseAfterPhysicalRetirement(()=>true))return false;
            if(!OpenOwner(root,out var later,out var newGateway)||
                later==null||newGateway==null)return false;
            try
            {
                if(WholeGameEconomySlotV1.Open(Dir(root),Run,Slot,later,out var fresh)
                    !=WholeGameResult.Accepted||fresh==null)return false;
                try
                {
                    WholeGameResult verdict=fresh.Read(out long gen,out byte[] world,
                        out var econ);
                    if(after)
                    {
                        if(verdict!=WholeGameResult.Accepted||gen!=1||econ==null||
                            econ.Scrap!=77||!world.SequenceEqual(World(1,true,false)))
                            return false;
                    }
                    else if(verdict!=WholeGameResult.Empty)return false;
                    Console.WriteLine(after
                        ?"W25_GAME_ECON_AFTER_ATOMIC_RENAME_LOST_ACK_COLD_BOTH=PASS"
                        :"W25_GAME_ECON_BEFORE_RENAME_NO_HALF_WALLET_AND_WORLD=PASS");
                    return true;
                }
                finally{fresh.Dispose();}
            }
            finally{newGateway.TryCloseAfterPhysicalRetirement(()=>true);}
        }
        finally{store?.Dispose();gw.TryCloseAfterPhysicalRetirement(()=>true);}
    });
    private static bool CorruptionAndPhysicalId()
    =>Scratch(baseDir=>{
        string root=Path.Combine(baseDir,"private");Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.GetDirectoryName(Dir(root))!);
        if(!OpenOwner(root,out var owner,out var gw)||owner==null||gw==null)return false;
        try
        {
            if(WholeGameEconomySlotV1.Create(Dir(root),Run,Slot,owner,out var store)
                !=WholeGameResult.Accepted||store==null)return false;
            try
            {
                var econ=Economy(1);
                econ.Parts[0].Id=Guid.NewGuid().ToString("N");
                if(store.Commit(0,World(1,true,false),econ)!=WholeGameResult.Invalid)
                    return false;
                econ=Economy(1);
                if(store.Commit(0,World(1,true,false),econ)!=WholeGameResult.Accepted)
                    return false;
                string file=Path.Combine(Dir(root),"save.envelope");
                byte[] saved=File.ReadAllBytes(file);saved[saved.Length/2]^=0x10;
                File.WriteAllBytes(file,saved);
                if(store.Read(out _,out _,out _)!=WholeGameResult.Corrupt ||
                   !store.Quarantined||store.Commit(1,World(2,false,true),
                       Economy(2))!=WholeGameResult.Quarantined)return false;
                Console.WriteLine("W25_GAME_ECON_WRONG_PHYSICAL_ID_AND_CORRUPT_FULL_FILE_DENIED=PASS");
                return true;
            }
            finally{store.Dispose();}
        }
        finally{gw.TryCloseAfterPhysicalRetirement(()=>true);}
    });
    private static bool OneShotAndQuoteContract()
    {
        var s=Economy(1,100);s.Parts[1].Hp=0;
        var facts=new CanonicalDeploymentFactsV6{
            OwnerId=s.OwnerId,RunId=s.RunId,Generation=1,
            CoreId=s.Parts[0].Id,EncounterId="next-challenge",
            CoreFunctional=true,CanDeploy=false,HasGenuineNextChallenge=true,
            EssentialDestroyedPhysicalIds=new[]{s.Parts[1].Id}
        };
        string permit=Guid.NewGuid().ToString("N");
        if(!ApprovedEconomyPolicyV6.TryReserveOneShotLoaner(s,facts,permit,
            out var reserved,out _)||reserved==null||
            !ApprovedEconomyPolicyV6.TryConsumeLoanerOnTrustedChallengeEntry(
                reserved,"next-challenge",permit,out var entered,out _)||
            entered==null||!entered.Loaner.EverEntered||
            ApprovedEconomyPolicyV6.TryReserveOneShotLoaner(
                entered,facts,permit,out _,out _))return false;
        // Owner #731 strict lower for price=1 vs owner #764 V6 actual source:
        // independent explicit contract RED; never call this full game ready.
        if(!ApprovedEconomyPolicyV6.TryPaidQuote(1,out int q)||q!=1)return false;
        Console.WriteLine("W25_764_PRICE_ONE_STRICT_LOWER_CONTRACT=RED_REQUIRES_OWNER_V7");
        Console.WriteLine("W25_LOANER_PERMIT_ENTERED_ONCE_MODEL_ONLY=PASS");
        return true;
    }
    private sealed class StagedSourceWorld:ICanonicalStagedWorld
    {
        private readonly RunSnapshot original;
        public bool Active,Retired;
        public StagedSourceWorld(RunSnapshot s){original=s;}
        public RunSnapshot Capture()=>original;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Retired=true;Active=false;}
    }
    private sealed class ManagedFactory:ICanonicalRigFactory
    {
        public readonly List<StagedSourceWorld> All=new List<StagedSourceWorld>();
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot snap,ColdRestore plan)
            =>new OwnedStage(this,snap);
        public bool AllGone()=>All.All(w=>w.Retired);
        private sealed class OwnedStage:ICanonicalStageOwnership
        {
            private readonly ManagedFactory parent;
            private readonly RunSnapshot snapshot;
            private StagedSourceWorld? made;
            public OwnedStage(ManagedFactory p,RunSnapshot s){parent=p;snapshot=s;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                made=new StagedSourceWorld(snapshot);parent.All.Add(made);return made;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                made?.Retire();return made==null||made.Retired;
            }
        }
    }
    private static bool ActualOriginal729GatewayOneEconomyCommitPort()
    =>Scratch(parent=>{
        string root=Path.Combine(parent,"game-authority");Directory.CreateDirectory(root);
        var factory=new ManagedFactory();
        if(CanonicalWholeGameSessionV1.TryOpen(root,Run,Slot,true,
            new Gate(),factory,out var session)!=WholeGameSessionOpen.Accepted||
            session==null)return false;
        try
        {
            byte[] w1=World(1,true,false);var e1=Economy(1,70);
            if(session.TryRestore(w1,e1,Run,1)!=CanonicalGateResult.Accepted||
               !session.HasWorld||session.Quarantined)return false;
            if(!session.TryCapture(out byte[] preview,
                out ApprovedEconomySnapshotV6? wallet)||
               wallet==null||wallet.Scrap!=70||!preview.SequenceEqual(w1))
                return false;
            if(!ApprovedEconomyPolicyV6.TryStageTrustedVictory(e1,
                "verified-gameplay-victory-1","real-encounter-a",
                out var e2,out var reason)||e2==null||reason!="OK")return false;
            e2.Scrap=42;
            byte[] w2=World(2,false,true);
            if(session.TryRestore(w2,e2,Run,2)!=CanonicalGateResult.Accepted||
               !session.TryCapture(out preview,out wallet)||wallet==null||
               !preview.SequenceEqual(w2)||wallet.Generation!=2||
               wallet.Scrap!=42||wallet.Wins!=1)return false;
            var altered=e2.Copy();altered.Scrap=999;
            if(session.TryRestore(w2,altered,Run,2)!=
                CanonicalGateResult.StaleGeneration)return false;
            // Freeze test-world to retire; original gateway is the only
            // owner allowed to release its OS .owner lease.
            if(!session.TryCloseAfterPhysicalRetirement(factory.AllGone))
                return false;
            var coldFactory=new ManagedFactory();
            if(CanonicalWholeGameSessionV1.TryOpen(root,Run,Slot,false,
                new Gate(),coldFactory,out var cold)!=WholeGameSessionOpen.Accepted||
                cold==null)return false;
            try
            {
                if(cold.TryRestore(w2,e2,Run,2)!=CanonicalGateResult.Accepted||
                   !cold.TryCapture(out preview,out wallet)||
                   wallet==null||wallet.Wins!=1||wallet.Scrap!=42||
                   !preview.SequenceEqual(w2))return false;
                Console.WriteLine("W25_CANONICAL_729_GATEWAY_ONE_FILE_WORLD_AND_WALLET=PASS");
                Console.WriteLine("W25_CANONICAL_SAME_GENERATION_WALLET_FORK_REJECTED=PASS");
                return true;
            }
            finally
            {
                cold.TryCloseAfterPhysicalRetirement(coldFactory.AllGone);
                cold.Dispose();
            }
        }
        finally
        {
            session.TryCloseAfterPhysicalRetirement(factory.AllGone);
            session.Dispose();
        }
    });
    public static int Main()
    {
        Check("Actual single original #556 + original economy #764 one-file durable cold wallet+victory",RoundtripAndVictory);
        Check("Actual original #729 gateway uses ONE full world+economy commit port and rejects same-epoch wallet fork",ActualOriginal729GatewayOneEconomyCommitPort);
        Check("Real physical rename then lost ACK does not split wallet and world",()=>Fault(true));
        Check("Before rename no authoritative partial wallet/world",()=>Fault(false));
        Check("Physical part ID map and corrupt one-file digest reject",CorruptionAndPhysicalId);
        Check("Original #764 one-shot loaner model valid; price=1 contract still RED",OneShotAndQuoteContract);
        Console.WriteLine("W25_GAME_ECON_ONE_ENVELOPE_SOURCE_TOTAL "+
            (passed+failed)+" PASS "+passed+" FAIL "+failed);
        Console.WriteLine("W25_GAME_ECON_SOURCE_ONLY_G0_NOT_READY_NATIVE_UNITY_NOT_RUN");
        return failed==0?0:1;
    }
}
