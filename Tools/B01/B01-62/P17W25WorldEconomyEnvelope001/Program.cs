#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using B01.WorldEconomyEnvelope001;
using B01.DetachedSaveContract001;
using B01.SecureReattachV2;
using B01.DebrisRepulse001;
using RoboRougelike.B01.Economy.ApprovedV6;
internal static class OriginalWorldPlusApprovedEconomyTests
{
    const long Run=62562;static readonly Guid Slot=Guid.Parse("62562625-0000-4000-8000-000000000777");
    static int pass,fail;
    static void Check(string name,Func<bool> f){try{if(f()){pass++;Console.WriteLine("PASS "+name);}else{fail++;Console.WriteLine("FAIL "+name);}}catch(Exception e){fail++;Console.WriteLine("FAIL "+name+" "+e);}}
    static byte[] Snapshot(long gen,bool light,bool heavy)
    {
        var arr=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            var s=SecureRigCatalog.At(i);
            var d=i==0?light:heavy;
            long owner=FrozenPartCatalog.Owner(i);
            arr[i]=new PersistedPart(s.PartId,owner,d?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                d?PartDockState.Detached:PartDockState.Attached,d,!d,
                new Vector3d(s.RootX+s.PartOffsetX,d?2.2:1.2,0),
                0,0,0,1,new Vector3d(d?0.25:0,0,0),
                new Vector3d(0,0,0),s.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(Run,gen,arr));
    }
    static ApprovedEconomySnapshotV6 Economy(long gen,bool loaner=false)
    {
        var e=new ApprovedEconomySnapshotV6{
            Version=6,OwnerId=Slot.ToString("N"),RunId=Run.ToString(),
            Generation=gen,Scrap=150,Wins=0,FreeUsedForWin=0,
            Loaner=new OneShotLoanerV6{
                Phase=loaner?LoanerPhaseV6.Spent:LoanerPhaseV6.None,
                EverEntered=loaner,EncounterId=loaner?"real-enc-1":null,
                PermitId=loaner?"00000000000040008000000000000077":null},
            ConsumedVictoryIds=new List<string>(),
            Parts=new List<OwnedModuleV6>{
                new OwnedModuleV6{Id="00000000000040008000000000000001",Category="Armor",Hp=0,PurchaseScrap=100,ReservationId=null},
                new OwnedModuleV6{Id="00000000000040008000000000000002",Category="Drive",Hp=1,PurchaseScrap=5,ReservationId=null}}
        };
        return e;
    }
    static bool Roundtrip()
    {
        var raw=Snapshot(1,true,false);var econ=Economy(1,true);
        if(!ApprovedEconomyPolicyV6.Valid(econ))return false;
        if(WorldEconomyPacketV1.TryEncode(Run,Slot,1,raw,econ,out var bytes)!=PacketStatus.Accepted)return false;
        if(WorldEconomyPacketV1.TryDecode(bytes,Run,Slot,1,out var v)!=PacketStatus.Accepted||v==null||
            !v.OriginalWorld556.SequenceEqual(raw)||v.Economy.Loaner.Phase!=LoanerPhaseV6.Spent||
            v.Economy.Scrap!=150||v.Economy.Parts.Count!=2)return false;
        bytes[bytes.Length-1]^=1;
        return WorldEconomyPacketV1.TryDecode(bytes,Run,Slot,1,out _) == PacketStatus.Corrupt;
    }
    static bool ForeignAndStale()
    {
        var raw=Snapshot(3,false,true);var econ=Economy(3);
        if(WorldEconomyPacketV1.TryEncode(Run,Slot,3,raw,econ,out var bytes)!=PacketStatus.Accepted)return false;
        return WorldEconomyPacketV1.TryDecode(bytes,Run,Guid.NewGuid(),0,out _)==PacketStatus.ForeignOwner &&
          WorldEconomyPacketV1.TryDecode(bytes,Run+1,Slot,0,out _)==PacketStatus.ForeignOwner &&
          WorldEconomyPacketV1.TryDecode(bytes,Run,Slot,4,out _)==PacketStatus.Stale &&
          WorldEconomyPacketV1.TryDecode(bytes,Run,Slot,3,out var v)==PacketStatus.Accepted &&v!=null&&v.Generation==3;
    }
    static bool DualIdentityDeny()
    {
        var raw=Snapshot(1,true,false);
        var e=Economy(2);
        bool wrongGen=WorldEconomyPacketV1.TryEncode(Run,Slot,1,raw,e,out _)==PacketStatus.ForeignOwner;
        e=Economy(1);e.OwnerId=Guid.NewGuid().ToString("N");
        bool wrongOwner=WorldEconomyPacketV1.TryEncode(Run,Slot,1,raw,e,out _)==PacketStatus.ForeignOwner;
        e=Economy(1);e.Wins=1;
        bool wrongIds=WorldEconomyPacketV1.TryEncode(Run,Slot,1,raw,e,out _)==PacketStatus.Invalid;
        return wrongGen&&wrongOwner&&wrongIds;
    }
    static bool GenuineSourceOriginal556()
    {
        var original=Snapshot(1,true,false);
        if(!ColdRestore.TryStage(original,out var cold)||cold==null||
            !cold.ContainsConsumedId(0,FrozenPartCatalog.Id(0)))return false;
        var e=Economy(1);
        if(WorldEconomyPacketV1.TryEncode(Run,Slot,1,original,e,out var joined)!=PacketStatus.Accepted)return false;
        return !ColdRestore.TryStage(joined,out _);
    }
    static bool SingleFilePhysicalRoundtrip()
    {
        string root=Path.Combine(Path.GetTempPath(),"b0162-w25-envelope-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var raw=Snapshot(1,true,false);
            if(WorldEconomyPacketV1.TryEncode(Run,Slot,1,raw,Economy(1),out var value)!=PacketStatus.Accepted)return false;
            string stage=Path.Combine(root,"pending.stage"),dst=Path.Combine(root,"one-complete-game.envelope");
            using(var f=new FileStream(stage,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
            {f.Write(value,0,value.Length);f.Flush(true);}
            File.Move(stage,dst,true);
            byte[] loaded=File.ReadAllBytes(dst);
            if(WorldEconomyPacketV1.TryDecode(loaded,Run,Slot,1,out var v)!=PacketStatus.Accepted||v==null)return false;
            return v.Economy.Scrap==150&&ColdRestore.TryStage(v.OriginalWorld556,out _);
        }
        finally{Directory.Delete(root,true);}
    }
    static int Main()
    {
        Check("01 original full #556 + original #764 V6 atomic canonical binary roundtrip and SHA corrupt",Roundtrip);
        Check("02 foreign slot/run and stale checkpoint reject",ForeignAndStale);
        Check("03 mismatched world/economy epoch owner and duplicate win IDs reject",DualIdentityDeny);
        Check("04 frozen #556 bytes unchanged, composite not readable as legacy",GenuineSourceOriginal556);
        Check("05 one physical file write-through flush atomic rename and cold full-game readback",SingleFilePhysicalRoundtrip);
        Console.WriteLine("B0162_W25_REAL_ORIGINAL_556_764_FULL_GAME_PACKET "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
        Console.WriteLine("B0162_W25_STORE_INTEGRATION=NOT_WIRED_TO_SELECTED_773_R3_NATIVE_G1_NOT_RUN");
        return fail==0?0:1;
    }
}