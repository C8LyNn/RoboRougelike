// B01-62 W33: original R3 factory sizes with actual original single Save V3.
// The game factory below is ORIGINAL SOURCE compiled through a tiny type shim.
// There is no real Unity Physics or authenticated combat receipt in this test.
#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using B01.CanonicalR3World001;
using B01.CanonicalWorldApi001;
using B01.WholeGameEconomy003;
using B01.CanonicalColdRead003;
using B01.DetachedSaveContract001;
using B01.SecureReattachV2;
using B01.DebrisRepulse001;
using RoboRougelike.R3;
using RoboRougelike.B01.Economy.ApprovedV7;
internal static class W33Os
{
    const long Run=6330062;
    static readonly Guid Slot=Guid.Parse("63300033-0000-4000-8000-000000000062");
    static int pass,fail;
    static void Test(string label,Func<bool> act)
    {
        try{if(act()){pass++;Console.WriteLine("PASS "+label);}
             else{fail++;Console.WriteLine("FAIL "+label);}}
        catch(Exception x){fail++;Console.WriteLine("FAIL "+label+" "+x);}
    }
    static bool Temp(Func<string,bool> action)
    {
        string dir=Path.Combine(Path.GetTempPath(),"b0162-w33-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try{return action(dir);}finally{Directory.Delete(dir,true);}
    }
    static string SlotDir(string root)=>Path.Combine(root,
      "b0162_canonical_single_authority_v3",
      "run_"+Run+"_slot_"+Slot.ToString("N"));
    sealed class Idle:ICanonicalMutationGate{public bool IsQuiescent()=>false;}
    sealed class NoRig:ICanonicalRigFactory
    {
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore c)
            =>throw new InvalidOperationException("No Unity physical stage in this OS test");
    }
    sealed class NoCommit:ICanonicalCommitPort
    {
        public bool Read(long run,out long g,out string h){g=0;h="";return false;}
        public bool Commit(long run,long prior,string old,long next,string hash)=>false;
    }
    static bool Open(string root,out CanonicalWorldGateway? gateway,
        out WholeGameEconomySlotV3? store,IOneFilePublisher? publisher=null)
    {
        gateway=null;store=null;
        if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out var lease)||
            lease==null)return false;
        gateway=new CanonicalWorldGateway(new Idle(),new NoRig(),new NoCommit(),lease);
        Directory.CreateDirectory(Path.GetDirectoryName(SlotDir(root))!);
        return WholeGameEconomySlotV3.Create(SlotDir(root),Run,Slot,lease,out store,publisher)
              ==WholeGameResult.Accepted&&store!=null;
    }
    static bool Close(CanonicalWorldGateway? gateway,WholeGameEconomySlotV3? store)
    {
        store?.Dispose();
        return gateway!=null&&gateway.TryCloseAfterPhysicalRetirement(()=>true);
    }
    static CanonicalR3SnapshotV1 World(R3VehicleKind kind,long generation)
    {
        var original=R3VehicleCatalog.Create(kind); // exact current game factory
        var snap=new CanonicalR3SnapshotV1{
            RunId=Run,Generation=generation,VehicleKind=(int)kind
        };
        long counter=0;
        foreach(var p in original.Parts)
        {
            counter++;
            snap.Parts.Add(new R3SavedPartV1{
                Id=Guid.NewGuid().ToString("N"),
                PhysicalId=770000+counter,OwnerPhysicalId=770000,
                Kind=(int)p.Kind,Weapon=0,Location=R3PartLocationV1.Mounted,
                CellX=p.Cell.x,CellY=p.Cell.y,CellZ=p.Cell.z,
                Contacts=63,Hp=1,Ammo=-1,Mass=1,Qw=1,
                DefenseCharge=-1,DefenseRechargeAge=float.PositiveInfinity,
                Source="real game factory source",
                Px=p.Cell.x*.55f,Py=p.Cell.y*.55f,Pz=p.Cell.z*.55f
            });
        }
        return snap;
    }
    static ApprovedEconomySnapshotV7 Econ(CanonicalR3SnapshotV1 w,int scrap=0)
    {
        var e=new ApprovedEconomySnapshotV7{
            Version=7,OwnerId=Slot.ToString("N"),RunId=Run.ToString(),
            Generation=w.Generation,Scrap=scrap,Wins=0,FreeUsedForWin=0,
            Parts=new List<OwnedModuleV7>(),Pending=new List<RebuildTicketV7>(),
            ConsumedVictoryIds=new List<string>(),
            Loaner=new OneShotLoanerV7{
                Phase=LoanerPhaseV7.None,EverEntered=false,
                EncounterId="",PermitId=""
            }
        };
        foreach(var p in w.Parts)
            e.Parts.Add(new OwnedModuleV7{
                Id=p.Id,Hp=p.Hp,
                Category=p.Kind==3?"Core":p.Kind==1?"Wheel":
                         p.Kind==2?"Gun":"Armor",
                PurchaseScrap=100,ReservationId=""
            });
        return e;
    }
    static bool FactoryRoundtrip(R3VehicleKind kind,int parts)
    {
        var actual=R3VehicleCatalog.Create(kind);
        var world=World(kind,1);
        if(actual.Parts.Count!=parts||world.Parts.Count!=parts||
           !CanonicalR3WorldCodecV1.Valid(world))return false;
        byte[] raw=CanonicalR3WorldCodecV1.Encode(world);
        if(!CanonicalR3WorldCodecV1.TryDecode(raw,out var decoded)||
           decoded==null||decoded.Parts.Count!=parts||
           !raw.SequenceEqual(CanonicalR3WorldCodecV1.Encode(decoded))||
           decoded.Parts.Any(p=>!float.IsPositiveInfinity(p.DefenseRechargeAge))||
           !decoded.Parts.Select(x=>x.Id).SequenceEqual(world.Parts.Select(x=>x.Id)))
            return false;
        Console.WriteLine("W33_REAL_R3_FACTORY_PARTS="+parts+"_FULL_R3_PAYLOAD_ROUNDTRIP=PASS");
        return true;
    }
    static bool ActualDisk83()
      =>Temp(root=>{
        var w=World(R3VehicleKind.Standard,1);
        var e=Econ(w,88);
        if(!Open(root,out var gateway,out var slot)||slot==null)return false;
        bool ok=false;
        try
        {
            var a=CanonicalR3WorldCodecV1.Encode(w);
            if(slot.Commit(0,a,e)!=WholeGameResult.Accepted)return false;
            w.Generation=2;e.Generation=2;e.Scrap=17;
            w.Parts[10].ThermalHeat=.25f;
            w.Parts[10].Ammo=44;
            if(slot.Commit(1,CanonicalR3WorldCodecV1.Encode(w),e)!=
                 WholeGameResult.Accepted||slot.Quarantined)return false;
            ok=true;
        }
        finally{if(!Close(gateway,slot))ok=false;}
        if(!ok)return false;
        if(CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
             out long gen,out byte[] cold,out var wallet)!=
             WholeGameResult.Accepted||gen!=2||wallet==null||
             wallet.Scrap!=17||!CanonicalR3WorldCodecV1.TryDecode(cold,out var loaded)||
             loaded==null||loaded.Parts.Count!=83||
             loaded.Parts[10].Ammo!=44||
             loaded.Parts[10].ThermalHeat!=.25f)return false;
        Console.WriteLine("W33_REAL_ONE_OWNER_ATOMIC_FILE_REPLACE_R3_83_AND_COLD=PASS");
        return true;
      });
    static bool DenyWorldEconomyMismatch()
      =>Temp(root=>{
        var w=World(R3VehicleKind.Light,1);
        var e=Econ(w);
        byte[] raw=CanonicalR3WorldCodecV1.Encode(w);
        if(!Open(root,out var gateway,out var slot)||slot==null)return false;
        bool ok=false;
        try
        {
            e.Parts[2].Hp=.5f;
            if(slot.Commit(0,raw,e)!=WholeGameResult.Invalid||slot.Quarantined)
                return false;
            e.Parts[2].Hp=1;e.Parts[2].Id=Guid.NewGuid().ToString("N");
            if(slot.Commit(0,raw,e)!=WholeGameResult.Invalid||slot.Quarantined)
                return false;
            e.Parts[2].Id=w.Parts[2].Id;
            ok=slot.Commit(0,raw,e)==WholeGameResult.Accepted;
        }
        finally{if(!Close(gateway,slot))ok=false;}
        return ok;
      });
    static bool InputNegatives()
    {
        var w=World(R3VehicleKind.Light,1);
        var p=w.Parts[1];
        string id=p.Id;
        p.Id=w.Parts[0].Id;
        if(CanonicalR3WorldCodecV1.Valid(w))return false;
        p.Id=id;p.PhysicalId=w.Parts[0].PhysicalId;
        if(CanonicalR3WorldCodecV1.Valid(w))return false;
        p.PhysicalId=9000099;p.ConsumedDetach=true;
        if(CanonicalR3WorldCodecV1.Valid(w))return false;
        p.ConsumedDetach=false;p.Mass=0;
        if(CanonicalR3WorldCodecV1.Valid(w))return false;
        p.Mass=1;p.Qw=0;
        if(CanonicalR3WorldCodecV1.Valid(w))return false;
        p.Qw=1;p.CellX=999999;
        if(CanonicalR3WorldCodecV1.Valid(w))return false;
        p.CellX=0;
        var bytes=CanonicalR3WorldCodecV1.Encode(w);
        var v=(byte[])bytes.Clone();v[8]^=0x10;
        return !CanonicalR3WorldCodecV1.TryDecode(v,out _)&&
           !CanonicalR3WorldCodecV1.TryDecode(bytes.Concat(new byte[]{1}).ToArray(),out _)&&
           !CanonicalR3WorldCodecV1.TryDecode(bytes.Take(bytes.Length-1).ToArray(),out _);
    }
    static bool NoMigrationLegacy556()
      =>Temp(root=>{
        var pieces=new PersistedPart[FrozenPartCatalog.Slots];
        for(int i=0;i<pieces.Length;i++)
        {
            var spec=SecureRigCatalog.At(i);
            long owner=FrozenPartCatalog.Owner(i);
            pieces[i]=new PersistedPart(spec.PartId,owner,owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                PartDockState.Attached,false,true,
                new Vector3d(spec.RootX+spec.PartOffsetX,1.2,0),0,0,0,1,
                new Vector3d(0,0,0),new Vector3d(0,0,0),spec.PartMassKg);
        }
        byte[] legacy=SnapshotCodec.Encode(new RunSnapshot(Run,1,pieces));
        var eco=new ApprovedEconomySnapshotV7{
            Version=7,OwnerId=Slot.ToString("N"),RunId=Run.ToString(),
            Generation=1,Scrap=0,Wins=0,FreeUsedForWin=0,
            Parts=pieces.Select(p=>new OwnedModuleV7{
                Id=WholeGameEconomySlotV3.EconomicPartId(p.Id),
                Category="Core",Hp=1,PurchaseScrap=100,ReservationId=""
            }).ToList(),
            Pending=new List<RebuildTicketV7>(),
            ConsumedVictoryIds=new List<string>(),
            Loaner=new OneShotLoanerV7{Phase=LoanerPhaseV7.None,
                EverEntered=false,EncounterId="",PermitId=""}
        };
        if(!Open(root,out var gateway,out var slot)||slot==null)return false;
        bool ok=false;
        try
        {
            if(slot.Commit(0,legacy,eco)!=WholeGameResult.Accepted)return false;
            var newWorld=World(R3VehicleKind.Light,2);
            if(slot.Commit(1,CanonicalR3WorldCodecV1.Encode(newWorld),
                Econ(newWorld))!=WholeGameResult.Invalid)return false;
            ok=slot.Read(out long gen,out byte[] unchanged,out var oldE)==
                WholeGameResult.Accepted&&gen==1&&oldE!=null&&
                unchanged.SequenceEqual(legacy)&&!slot.Quarantined;
        }
        finally{if(!Close(gateway,slot))ok=false;}
        return ok;
      });
    static bool V7PaidZeroNextModeledVictory()
      =>Temp(root=>{
        var w=World(R3VehicleKind.Standard,1);
        var broken=w.Parts.First(p=>p.Kind==0);
        broken.Hp=0;
        var e=Econ(w,0);
        e.Parts.Single(p=>p.Id==broken.Id).PurchaseScrap=1;
        if(!Open(root,out var gateway,out var slot)||slot==null)return false;
        bool ok=false;
        try
        {
            if(slot.Commit(0,CanonicalR3WorldCodecV1.Encode(w),e)!=WholeGameResult.Accepted)
                return false;
            if(!ApprovedEconomyPolicyV7.TryReserveDestroyed(e,broken.Id,
              out var booked,out _)||booked==null||booked.Pending.Count!=1||
              booked.Pending[0].Free||booked.Pending[0].ChargedScrap!=0||
              booked.Parts.Single(p=>p.Id==broken.Id).Hp!=0)return false;
            w.Generation=2;
            if(slot.Commit(1,CanonicalR3WorldCodecV1.Encode(w),booked)!=
                 WholeGameResult.Accepted)return false;
            // Test-only simulated "trusted" IDs: no real combat attestation.
            if(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(booked,
                "test-only-w33-win1","test-only-w33-fight1",
                out var settled,out _)||settled==null||settled.Wins!=1||
                settled.Pending.Count!=0||
                settled.Parts.Single(p=>p.Id==broken.Id).Hp!=1)return false;
            w.Generation=3;broken.Hp=1;
            if(slot.Commit(2,CanonicalR3WorldCodecV1.Encode(w),settled)!=
                 WholeGameResult.Accepted||
               ApprovedEconomyPolicyV7.TryStageTrustedVictory(settled,
                "test-only-w33-win1","test-only-w33-fight1",out _,out _))
                return false;
            ok=true;
        }
        finally{if(!Close(gateway,slot))ok=false;}
        if(!ok)return false;
        return CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
            out long gen,out byte[] payload,out var finalWallet)==
             WholeGameResult.Accepted&&gen==3&&finalWallet!=null&&
            finalWallet.Wins==1&&finalWallet.Scrap==0&&
            finalWallet.ConsumedVictoryIds.Count==1&&
            CanonicalR3WorldCodecV1.TryDecode(payload,out var finalWorld)&&
            finalWorld!=null&&finalWorld.Parts.Single(p=>p.Id==broken.Id).Hp==1;
      });
    static bool StickyCorrupt()
      =>Temp(root=>{
        var w=World(R3VehicleKind.Light,1);
        if(!Open(root,out var gateway,out var slot)||slot==null)return false;
        bool ok=false;
        try
        {
            byte[] raw=CanonicalR3WorldCodecV1.Encode(w);
            if(slot.Commit(0,raw,Econ(w))!=WholeGameResult.Accepted)return false;
            string path=Path.Combine(SlotDir(root),"save.envelope");
            var bytes=File.ReadAllBytes(path);
            bytes[bytes.Length-33]^=0x7f;
            File.WriteAllBytes(path,bytes);
            if(slot.Read(out _,out _,out _)!=WholeGameResult.Corrupt||
               !slot.Quarantined||slot.Commit(1,raw,Econ(w))!=
                   WholeGameResult.Quarantined)return false;
            ok=true;
        }
        finally{if(!Close(gateway,slot))ok=false;}
        return ok&&CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
          out _,out byte[] cold,out var e)==WholeGameResult.Corrupt&&
          cold.Length==0&&e==null;
      });
    static bool InventoryDetachedPhysicalResourceRoundtrip()
      =>Temp(root=>{
        var w=World(R3VehicleKind.Heavy,1);
        var inv=w.Parts.First(x=>x.Kind==0);
        var detached=w.Parts.First(x=>x.Kind==1);
        inv.Location=R3PartLocationV1.Inventory;
        inv.UpgradeId="r3-upgrade-test";
        inv.UpgradeTitle="Exact inventory bonus";
        inv.UpgradeDescription="Preserve resources and own GUID";
        inv.UpgradeEffect=2;inv.UpgradeAmount=.75f;
        inv.Ammo=27;
        detached.Location=R3PartLocationV1.Detached;
        detached.ConsumedDetach=true;detached.Vx=3.75f;
        detached.Wz=-1.25f;detached.ThermalHeat=.125f;
        if(!CanonicalR3WorldCodecV1.Valid(w))return false;
        if(!Open(root,out var gateway,out var slot)||slot==null)return false;
        bool ok=false;
        try
        {
            ok=slot.Commit(0,CanonicalR3WorldCodecV1.Encode(w),Econ(w))==
                WholeGameResult.Accepted;
        }
        finally{if(!Close(gateway,slot))ok=false;}
        if(!ok||CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
             out long gen,out byte[] world,out var wallet)!=WholeGameResult.Accepted||
            gen!=1||wallet==null||wallet.Parts.Count!=139||
            !CanonicalR3WorldCodecV1.TryDecode(world,out var saved)||saved==null)
            return false;
        var restoredInventory=saved.Parts.Single(x=>x.Id==inv.Id);
        var restoredDetached=saved.Parts.Single(x=>x.Id==detached.Id);
        return restoredInventory.Location==R3PartLocationV1.Inventory&&
            restoredInventory.UpgradeId=="r3-upgrade-test"&&
            restoredInventory.UpgradeAmount==.75f&&
            restoredInventory.Ammo==27&&
            restoredDetached.Location==R3PartLocationV1.Detached&&
            restoredDetached.ConsumedDetach&&restoredDetached.Vx==3.75f&&
            restoredDetached.Wz==-1.25f&&
            restoredDetached.ThermalHeat==.125f;
      });
    static bool FreeWin1BookedThenSettledOnlyModeledWin2()
      =>Temp(root=>{
        var w=World(R3VehicleKind.Standard,1);
        var damaged=w.Parts.First(x=>x.Kind==0);
        damaged.Hp=0;
        var initial=Econ(w,0);
        if(!Open(root,out var gateway,out var slot)||slot==null)return false;
        bool ok=false;
        try
        {
            if(slot.Commit(0,CanonicalR3WorldCodecV1.Encode(w),initial)!=
                WholeGameResult.Accepted)return false;
            // ONLY synthetic QA source reducer receipts. These must never be
            // passed by UI in actual Unity; real combat attestation is missing.
            if(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(initial,
                "w33-source-only-first-win","w33-source-only-first-fight",
                out var win1,out _)||win1==null||win1.Wins!=1||
                win1.Parts.Single(p=>p.Id==damaged.Id).Hp!=0)
                return false;
            w.Generation=2;
            if(slot.Commit(1,CanonicalR3WorldCodecV1.Encode(w),win1)!=
                WholeGameResult.Accepted)return false;
            if(!ApprovedEconomyPolicyV7.TryReserveDestroyed(win1,damaged.Id,
                out var booked,out _)||booked==null||
                booked.Pending.Count!=1||!booked.Pending[0].Free||
                booked.Pending[0].ChargedScrap!=0||
                booked.Parts.Single(p=>p.Id==damaged.Id).Hp!=0)
                return false;
            w.Generation=3;
            if(slot.Commit(2,CanonicalR3WorldCodecV1.Encode(w),booked)!=
               WholeGameResult.Accepted)return false;
            if(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(booked,
                "w33-source-only-second-win","w33-source-only-second-fight",
                out var win2,out _)||win2==null||win2.Wins!=2||
                win2.Pending.Count!=0||
                win2.Parts.Single(p=>p.Id==damaged.Id).Hp!=1)
                return false;
            w.Generation=4;damaged.Hp=1;
            if(slot.Commit(3,CanonicalR3WorldCodecV1.Encode(w),win2)!=
                WholeGameResult.Accepted||
               ApprovedEconomyPolicyV7.TryStageTrustedVictory(win2,
                "w33-source-only-second-win","w33-source-only-second-fight",
                out _,out _))return false;
            ok=true;
        }
        finally{if(!Close(gateway,slot))ok=false;}
        if(!ok)return false;
        return CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
            out long gen,out byte[] payload,out var econ)==
               WholeGameResult.Accepted&&gen==4&&econ!=null&&
               econ.Wins==2&&econ.ConsumedVictoryIds.Count==2&&
               econ.Pending.Count==0&&econ.Scrap==0&&
               CanonicalR3WorldCodecV1.TryDecode(payload,out var saved)&&
               saved!=null&&saved.Parts.Single(p=>p.Id==damaged.Id).Hp==1;
      });
    static bool ActualR4WeaponAndMountedOwnerNegatives()
    {
        // The REAL selected #877 R4WeaponKind has nine members (0..8).
        // Previous original W33 codec accepted arbitrary enum 9..255.
        var w=World(R3VehicleKind.Standard,1);
        var gun=w.Parts.Single(x=>x.Kind==2);
        gun.Weapon=255;
        if(CanonicalR3WorldCodecV1.Valid(w))return false;
        gun.Weapon=9;
        if(CanonicalR3WorldCodecV1.Valid(w))return false;
        gun.Weapon=8; // legit original WPN-EMP
        if(!CanonicalR3WorldCodecV1.Valid(w))return false;
        gun.Weapon=0;
        var armor=w.Parts.First(x=>x.Kind==0);
        armor.Weapon=1; // ONLY R3 Guns may carry non-default weapon kind
        if(CanonicalR3WorldCodecV1.Valid(w))return false;
        armor.Weapon=0;
        long prior=armor.OwnerPhysicalId;
        armor.OwnerPhysicalId=prior+101; // foreign mounted rig within same owner Save
        if(CanonicalR3WorldCodecV1.Valid(w))return false;
        armor.OwnerPhysicalId=prior;
        if(!CanonicalR3WorldCodecV1.Valid(w))return false;
        // Real R3 mounted-family limit is 2, but garage inventory can
        // stock arbitrarily many weapon kinds without mounting them.
        var upgrade=w.Parts.Where(x=>x.Kind==0).Take(2).ToArray();
        if(upgrade.Length!=2)return false;
        upgrade[0].Kind=2;upgrade[0].Weapon=1;
        upgrade[1].Kind=2;upgrade[1].Weapon=2;
        if(CanonicalR3WorldCodecV1.Valid(w))return false; // 0+1+2
        upgrade[1].Location=R3PartLocationV1.Inventory;
        if(!CanonicalR3WorldCodecV1.Valid(w))return false; // 0+1 mounted
        upgrade[1].Kind=0;upgrade[1].Weapon=0;
        upgrade[1].Location=R3PartLocationV1.Mounted;
        return CanonicalR3WorldCodecV1.Valid(w);
    }
    sealed class SecondPublishFault:IOneFilePublisher
    {
        readonly bool afterReplace;
        int calls;
        public SecondPublishFault(bool after){afterReplace=after;}
        public void Publish(string staging,string authoritative)
        {
            calls++;
            if(calls==2&&!afterReplace)
                throw new IOException("W34 intentionally before second File.Replace");
            new OneFileOsPublisher().Publish(staging,authoritative);
            if(calls==2&&afterReplace)
                throw new IOException("W34 intentionally lost ACK after second File.Replace");
        }
    }
    static bool DynamicAtomicFailClosedAndCold(bool afterRename)
      =>Temp(root=>{
        var w=World(R3VehicleKind.Heavy,1);
        var e=Econ(w,91);
        if(!Open(root,out var gateway,out var slot,
                 new SecondPublishFault(afterRename))||slot==null)return false;
        bool complete=false;
        try
        {
            if(slot.Commit(0,CanonicalR3WorldCodecV1.Encode(w),e)!=
                WholeGameResult.Accepted)return false;
            w.Generation=2;
            e.Generation=2;e.Scrap=17;
            var watched=w.Parts.Single(x=>x.Kind==2);
            watched.Ammo=13;watched.ThermalHeat=.5f;
            // The original one-writer publish hook intentionally throws
            // either before or after physical SAME-DIRECTORY replacement.
            var outcome=slot.Commit(1,CanonicalR3WorldCodecV1.Encode(w),e);
            if(outcome!=WholeGameResult.Quarantined||!slot.Quarantined)
                return false;
            complete=true;
        }
        finally{if(!Close(gateway,slot))complete=false;}
        if(!complete)return false;
        if(CanonicalColdReadV3.ReadClosedSlot(root,Run,Slot,
             out long gen,out byte[] payload,out var wallet)!=
             WholeGameResult.Accepted||wallet==null||
             !CanonicalR3WorldCodecV1.TryDecode(payload,out var recovered)||
             recovered==null||recovered.Parts.Count!=139)return false;
        var gun=recovered.Parts.Single(x=>x.Kind==2);
        // Either whole prior world+economy or whole new world+economy,
        // NEVER an impossible mixed wallet+world success.
        return afterRename
            ? gen==2&&wallet.Scrap==17&&gun.Ammo==13&&gun.ThermalHeat==.5f
            : gen==1&&wallet.Scrap==91&&gun.Ammo==-1&&gun.ThermalHeat==0;
      });
    static bool ActualOriginalGatewayCannotStageR3Yet()
      =>Temp(root=>{
        var w=World(R3VehicleKind.Light,1);
        var e=Econ(w,27);
        if(!Open(root,out var gateway,out var slot)||slot==null)return false;
        bool complete=false;
        try
        {
            complete=slot.Commit(0,CanonicalR3WorldCodecV1.Encode(w),e)==
                     WholeGameResult.Accepted;
        }
        finally{if(!Close(gateway,slot))complete=false;}
        if(!complete)return false;
        var open=B01.CanonicalWholeGameAdapter003.CanonicalWholeGameSessionV3.TryOpen(
            root,Run,Slot,false,new Idle(),new NoRig(),out var session);
        if(open!=B01.CanonicalWholeGameAdapter003.WholeGameSessionOpen.Accepted||
           session==null)return false;
        try
        {
            var verdict=session.TryRestore(CanonicalR3WorldCodecV1.Encode(w),
                e,Run,1);
            // Existing #729 physical gate is 556-only and MUST NOT pretend
            // it restored an R3; this is an explicit NEXT-OWNER blocker.
            return verdict==CanonicalGateResult.InvalidSnapshot&&
                !session.HasWorld&&session.OwnsSlot;
        }
        finally{session.TryCloseAfterPhysicalRetirement(()=>true);}
      });
    static int Main()
    {
        Test("Exact R3 Light 20 original parts complete roundtrip",
             ()=>FactoryRoundtrip(R3VehicleKind.Light,20));
        Test("Exact R3 Standard 83 original parts complete roundtrip",
             ()=>FactoryRoundtrip(R3VehicleKind.Standard,83));
        Test("Exact R3 Heavy 139 original parts complete roundtrip",
             ()=>FactoryRoundtrip(R3VehicleKind.Heavy,139));
        Test("One ORIGINAL V3 Save standard 83 g1/g2 atomic + cold",ActualDisk83);
        Test("V7 mismatched HP or GUID rejected pre-publish",DenyWorldEconomyMismatch);
        Test("Duplicate physical/GUID, invalid state, unsupported wire denied",InputNegatives);
        Test("Original frozen two-slot Save cannot silently become R3",NoMigrationLegacy556);
        Test("V7 P1=0 Scrap paid pending until next modeled distinct win",V7PaidZeroNextModeledVictory);
        Test("Dynamic R3 corrupted envelope sticky fail-closed",StickyCorrupt);
        Test("Heavy inventory+detached physics/resource parts survive cold",
             InventoryDetachedPhysicalResourceRoundtrip);
        Test("Win1 FREE booked HP0; only later modeled Win2 restores same GUID",
             FreeWin1BookedThenSettledOnlyModeledWin2);
        Test("Current R4 kinds 0..8 and ONE physical mounted-rig owner enforced",
             ActualR4WeaponAndMountedOwnerNegatives);
        Test("139-part pre-replace disk fault cannot mix wallet and physics",
             ()=>DynamicAtomicFailClosedAndCold(false));
        Test("139-part post-replace lost ACK cold-reconciles BOTH sections",
             ()=>DynamicAtomicFailClosedAndCold(true));
        Test("Original two-rig #729 gateway correctly refuses R3 physical stage",
             ActualOriginalGatewayCannotStageR3Yet);
        Console.WriteLine("B0162_W33_R3_SAME_ONE_WRITER_REAL_OS_TOTAL "+
            (pass+fail)+" PASS "+pass+" FAIL "+fail);
        Console.WriteLine("B0162_W33_UNITY_NATIVE=NOT_RUN_GAME_R3_BINDER=NOT_INTEGRATED");
        return fail==0?0:1;
    }
}
