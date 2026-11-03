// B01-62 W32 P0: executes ORIGINAL current game R3VehicleCatalog.Create
// and ORIGINAL frozen #556 SnapshotRules/SnapshotCodec. Not a new writer,
// substitute R3 physics, or Unity-native Editor/Player test.
#nullable enable
using System;
using System.IO;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using RoboRougelike.R3;
internal static class R3RealWorldContractQA
{
    static int pass,fail;
    static void Check(string name, Func<bool> fn)
    {
        try{if(fn()){pass++;Console.WriteLine("PASS "+name);}
            else{fail++;Console.WriteLine("FAIL "+name);}}
        catch(Exception e){fail++;Console.WriteLine("FAIL "+name+" "+e);}
    }
    static readonly Vector3d Zero=new Vector3d(0,0,0);
    static PersistedPart OriginalFrozenPart(int i)
      =>new PersistedPart(FrozenPartCatalog.Id(i),FrozenPartCatalog.Owner(i),
          FrozenPartCatalog.Owner(i),FrozenPartCatalog.Asset(i),
          FrozenPartCatalog.Socket(i),PartDockState.Attached,false,true,
          new Vector3d(i*3,1.2,0),0,0,0,1,Zero,Zero,FrozenPartCatalog.Mass(i));
    static RunSnapshot SnapshotWithCount(int count)
    {
        var parts=new PersistedPart[count];
        for(int i=0;i<count;i++)
            parts[i]=i<FrozenPartCatalog.Slots?OriginalFrozenPart(i):
               new PersistedPart(9000000+i,41001,41001,
                 "b0123001000000000000000000000001",
                 "b0123011000000000000000000000011",
                 PartDockState.Attached,false,true,Zero,0,0,0,1,
                 Zero,Zero,1.0);
        return new RunSnapshot(6320062,1,parts);
    }
    static bool ActualGameCatalog(R3VehicleKind kind,int expected)
    {
        var design=R3VehicleCatalog.Create(kind); // ORIGINAL actual game source
        return design.Parts.Count==expected&&design.Parts.Count>
           FrozenPartCatalog.Slots;
    }
    static bool OriginalContractAcceptsOnlyItsTwoFixedFixtureParts()
    {
        var x=SnapshotWithCount(2);
        if(FrozenPartCatalog.Slots!=2||!SnapshotRules.Valid(x))return false;
        byte[] bytes=SnapshotCodec.Encode(x); // ORIGINAL #556 codec
        return bytes.Length>0&&
            SnapshotCodec.TryDecode(bytes,out RunSnapshot? decoded)&&
            decoded!=null&&decoded.Parts.Length==2&&
            ColdRestore.TryStage(bytes,out ColdRestore? cold)&&cold!=null;
    }
    static bool OriginalContractCannotEncodeActualGameCount(R3VehicleKind kind)
    {
        int count=R3VehicleCatalog.Create(kind).Parts.Count;
        var snap=SnapshotWithCount(count);
        if(count<=2||SnapshotRules.Valid(snap))return false;
        try
        {
            SnapshotCodec.Encode(snap);
            return false; // original fixed catalog MUST reject this, fail-closed
        }
        catch(InvalidDataException)
        {
            Console.WriteLine("B0162_W32_EXPECTED_GAME_WORLD_REJECT_PARTS="+count);
            return true;
        }
    }
    static int Main()
    {
        Check("Actual R3 Light factory creates 20 parts",
            ()=>ActualGameCatalog(R3VehicleKind.Light,20));
        Check("Actual R3 Standard factory creates 83 parts",
            ()=>ActualGameCatalog(R3VehicleKind.Standard,83));
        Check("Actual R3 Heavy factory creates 139 parts",
            ()=>ActualGameCatalog(R3VehicleKind.Heavy,139));
        Check("Original frozen #556 two-physical-rig source still losslessly works",
            OriginalContractAcceptsOnlyItsTwoFixedFixtureParts);
        Check("Original 556 codec fail-closes true factory Light count",
            ()=>OriginalContractCannotEncodeActualGameCount(R3VehicleKind.Light));
        Check("Original 556 codec fail-closes true factory Standard count",
            ()=>OriginalContractCannotEncodeActualGameCount(R3VehicleKind.Standard));
        Check("Original 556 codec fail-closes true factory Heavy count",
            ()=>OriginalContractCannotEncodeActualGameCount(R3VehicleKind.Heavy));
        Console.WriteLine("B0162_W32_R3_VS_556_REAL_ORIGINAL_CODE_TOTAL "+
            (pass+fail)+" PASS "+pass+" FAIL "+fail);
        Console.WriteLine("B0162_W32_PRODUCT_COMPATIBILITY=RED_EXPECTED_SOURCE_WITNESS");
        Console.WriteLine("B0162_W32_NATIVE_UNITY_EDIT_PLAY_PLAYER=NOT_RUN");
        return fail==0?0:1;
    }
}
