// W30 B01-62 source OS QA. Legacy envelope construction is TEST ONLY,
// never an alternate production Save writer. Genuine #780+855 are linked.
#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using B01.CanonicalWorldApi001;
using B01.WholeGameEconomy003;
using B01.CanonicalColdRead003;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using RoboRougelike.B01.Economy.ApprovedV7;
using RoboRougelike.B01.Economy.AdmissionW29;
internal static class W30Qa
{
    const long Run=6300062;
    static readonly Guid Slot=Guid.Parse("63002630-0000-4000-8000-000000000556");
    static int pass,fail;
    static void Check(string name,Func<bool> test)
    {
        try {if(test()){pass++;Console.WriteLine("PASS "+name);}
            else{fail++;Console.WriteLine("FAIL "+name);}}
        catch(Exception e){fail++;Console.WriteLine("FAIL "+name+" "+e);}
    }
    static bool Scratch(Func<string,bool> work)
    {
        string root=Path.Combine(Path.GetTempPath(),
            "b0162-w30-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{return work(root);} finally{Directory.Delete(root,true);}
    }
    static string Dir(string root)=>Path.Combine(root,
        "b0162_canonical_single_authority_v3",
        "run_"+Run+"_slot_"+Slot.ToString("N"));
    sealed class Gate:ICanonicalMutationGate{public bool IsQuiescent()=>false;}
    sealed class NoStage:ICanonicalRigFactory
    {
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore c)
          =>throw new InvalidOperationException("No Unity objects in OS test");
    }
    sealed class NoCommit:ICanonicalCommitPort
    {
        public bool Read(long r,out long g,out string h){g=0;h="";return false;}
        public bool Commit(long r,long prior,string ph,long n,string h)=>false;
    }
    static bool Open(string root,out CanonicalWorldGateway? gateway,
        out WholeGameEconomySlotV3? store)
    {
        gateway=null;store=null;
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out var owner)||
            owner==null)return false;
        gateway=new CanonicalWorldGateway(new Gate(),new NoStage(),
            new NoCommit(),owner);
        Directory.CreateDirectory(Path.GetDirectoryName(Dir(root))!);
        return WholeGameEconomySlotV3.Create(Dir(root),Run,Slot,owner,
            out store)==WholeGameResult.Accepted&&store!=null;
    }
    static bool Close(CanonicalWorldGateway? gateway,WholeGameEconomySlotV3? store)
    {
        store?.Dispose();
        return gateway!=null&&gateway.TryCloseAfterPhysicalRetirement(()=>true);
    }
    static byte[] World(long gen)
    {
        var arr=new PersistedPart[FrozenPartCatalog.Slots];
        for(int i=0;i<arr.Length;i++)
        {
            var s=SecureRigCatalog.At(i);long owner=FrozenPartCatalog.Owner(i);
            arr[i]=new PersistedPart(s.PartId,owner,owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                PartDockState.Attached,false,true,
                new Vector3d(s.RootX+s.PartOffsetX,1.2,0),
                0,0,0,1,new Vector3d(0,0,0),
                new Vector3d(0,0,0),s.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(Run,gen,arr));
    }
    static ApprovedEconomySnapshotV7 Econ(long gen,int price=100)
    {
        var parts=new List<OwnedModuleV7>();
        for(int i=0;i<FrozenPartCatalog.Slots;i++)
            parts.Add(new OwnedModuleV7 {
                Id=WholeGameEconomySlotV3.EconomicPartId(FrozenPartCatalog.Id(i)),
                Category=i==0?"Core":"EssentialDrive",
                Hp=i==0?1:0,PurchaseScrap=i==0?100:price,
                ReservationId=""
            });
        return new ApprovedEconomySnapshotV7{
            Version=7,OwnerId=Slot.ToString("N"),RunId=Run.ToString(),
            Generation=gen,Scrap=0,Wins=0,FreeUsedForWin=0,
            Parts=parts,Pending=new List<RebuildTicketV7>(),
            ConsumedVictoryIds=new List<string>(),
            Loaner=new OneShotLoanerV7 {
                Phase=LoanerPhaseV7.None,EverEntered=false,
                EncounterId="",PermitId=""
            }
        };
    }
    static ApprovedEconomySnapshotV7 FakeFree()
    {
        var econ=Econ(1);
        var drive=econ.Parts[1];
        drive.ReservationId="w30-source-fixture-invalid-early-free";
        econ.Pending.Add(new RebuildTicketV7{
            ModuleId=drive.Id,ReservationId=drive.ReservationId,
            PurchaseScrap=100,ChargedScrap=0,AfterWins=0,Free=true
        });
        return econ;
    }
    static bool CommitCannotMintEarlyFree()
      =>Scratch(root=>{
        var fake=FakeFree();
        if(!ApprovedEconomyPolicyV7.Valid(fake)||
           !CanonicalEconomySectionV7.TryEncode(fake,out _,out _)||
           EconomyV7AdmissionW29.TryAdmit(fake,
            new CanonicalEconomyIdentityW29(Slot.ToString("N"),Run.ToString(),1),
            out string reason)||
           reason!="FREE_TICKET_BEFORE_FIRST_AUTHENTIC_VICTORY")
            return false;
        if(!Open(root,out var gateway,out var store)||store==null)return false;
        bool result=false;
        try
        {
            string path=Path.Combine(Dir(root),"save.envelope");
            if(store.Commit(0,World(1),fake)!=WholeGameResult.Invalid||
               store.Quarantined||File.Exists(path)||
               store.Read(out _,out _,out _)!=WholeGameResult.Empty)
                return false;
            if(store.Commit(0,World(1),Econ(1))!=WholeGameResult.Accepted||
               !File.Exists(path))return false;
            result=true;
            Console.WriteLine("W30_MALFORMED_FREE_DENIED_VALID_SAVE_CAN_CONTINUE=PASS");
        }
        finally{if(!Close(gateway,store))result=false;}
        return result;
      });

    // Historical checksum-valid V3 envelope using ORIGINAL unguarded #780
    // is manufactured only inside disposable QA scratch, not a new writer.
    static byte[] LegacyFixture(byte[] world,ApprovedEconomySnapshotV7 econ)
    {
        if(!CanonicalEconomySectionV7.TryEncode(econ,out byte[] section,out _))
            throw new InvalidDataException("Original V7 witness missing");
        using var ms=new MemoryStream();
        using(var w=new BinaryWriter(ms,Encoding.UTF8,true))
        {
            w.Write(Encoding.ASCII.GetBytes("B0162G02"));
            w.Write(2);w.Write(Run);w.Write(Slot.ToByteArray());
            w.Write(econ.Generation);w.Write(world.Length);
            w.Write(section.Length);w.Write(world);w.Write(section);
        }
        byte[] body=ms.ToArray();
        using(var sha=SHA256.Create())
        {
            byte[] digest=sha.ComputeHash(body);
            ms.Write(digest,0,digest.Length);
        }
        return ms.ToArray();
    }
    static bool LegacyInvalidFullSaveQuarantines()
      =>Scratch(root=>{
        if(!Open(root,out var gateway,out var store)||store==null)return false;
        bool closed=false;
        try
        {
            byte[] forged=LegacyFixture(World(1),FakeFree());
            File.WriteAllBytes(Path.Combine(Dir(root),"save.envelope"),forged);
            if(store.Read(out _,out _,out _)!=WholeGameResult.Corrupt||
               !store.Quarantined||
               store.Commit(0,World(1),Econ(1))!=WholeGameResult.Quarantined)
               return false;
        }
        finally{closed=Close(gateway,store);}
        if(!closed)return false;
        if(CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
            out long generation,out byte[] world,out var wallet)!=
            WholeGameResult.Corrupt||
            generation!=0||world.Length!=0||wallet!=null)return false;
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,
             out var owner)||owner==null||!owner.TryCancelUnbound())
            return false;
        Console.WriteLine("W30_ORIGINAL_780_VALID_DIGEST_INVALID_FREE_COLD_QUARANTINE=PASS");
        return true;
      });
    static bool LegitimatePaidZero()
      =>Scratch(root=>{
        if(!Open(root,out var gateway,out var store)||store==null)return false;
        bool result=false;
        try
        {
            var e1=Econ(1,1);
            if(store.Commit(0,World(1),e1)!=WholeGameResult.Accepted||
               !ApprovedEconomyPolicyV7.TryReserveDestroyed(e1,
                 e1.Parts[1].Id,out var e2,out _)||
               e2.Pending.Single().Free||
               e2.Pending.Single().ChargedScrap!=0||
               store.Commit(1,World(2),e2)!=WholeGameResult.Accepted||
               store.Read(out long gen,out _,out var wallet)!=
                    WholeGameResult.Accepted||wallet==null||gen!=2||
               wallet.Pending.Single().Free||
               wallet.Pending.Single().ChargedScrap!=0)return false;
            result=true;
            Console.WriteLine("W30_ZERO_PAID_NEVER_FREE_AND_STABLE_FULL_SAVE=PASS");
        }
        finally{if(!Close(gateway,store))result=false;}
        return result;
      });
    static bool LegitimateVictoryFreeReplay()
      =>Scratch(root=>{
        if(!Open(root,out var gateway,out var store)||store==null)return false;
        bool result=false;
        try
        {
            var e1=Econ(1);
            if(store.Commit(0,World(1),e1)!=WholeGameResult.Accepted||
              !ApprovedEconomyPolicyV7.TryStageTrustedVictory(e1,
                 "test-only-win-1","test-only-fight-1",out var e2,out _)||
              e2==null||store.Commit(1,World(2),e2)!=WholeGameResult.Accepted||
              !ApprovedEconomyPolicyV7.TryReserveDestroyed(e2,e2.Parts[1].Id,
                 out var e3,out _)||e3==null||!e3.Pending.Single().Free||
              store.Commit(2,World(3),e3)!=WholeGameResult.Accepted||
              !ApprovedEconomyPolicyV7.TryStageTrustedVictory(e3,
                 "test-only-win-2","test-only-fight-2",out var e4,out _)||
              e4==null||e4.Pending.Count!=0||e4.Parts[1].Hp!=1||
              store.Commit(3,World(4),e4)!=WholeGameResult.Accepted||
              store.Read(out long gen,out _,out var wallet)!=
                 WholeGameResult.Accepted||wallet==null||gen!=4||
              wallet.Wins!=2||wallet.ConsumedVictoryIds.Count!=2||
              ApprovedEconomyPolicyV7.TryStageTrustedVictory(wallet,
                 "test-only-win-2","test-only-fight-2",out _,out _))
                 return false;
            result=true;
            Console.WriteLine("W30_WIN1_FREE_WIN2_REBUILD_DISTINCT_RECEIPT_REPLAY_DENIED=PASS");
        }
        finally{if(!Close(gateway,store))result=false;}
        return result;
      });
    static int Main()
    {
        Check("WIN0 forged FREE cannot commit",CommitCannotMintEarlyFree);
        Check("Legacy checksum-valid malformed envelope quarantined cold",LegacyInvalidFullSaveQuarantines);
        Check("P1 zero paid is not victory free",LegitimatePaidZero);
        Check("Win1 legal FREE settles after Win2 and replay denied",LegitimateVictoryFreeReplay);
        Console.WriteLine("B0162_W30_ADMISSION_SINGLE_WRITER_REAL_OS_TOTAL "+
           (pass+fail)+" PASS "+pass+" FAIL "+fail);
        Console.WriteLine("B0162_W30_REAL_OS_SOURCE_ONLY_UNITY_NOT_RUN");
        return fail==0?0:1;
    }
}
