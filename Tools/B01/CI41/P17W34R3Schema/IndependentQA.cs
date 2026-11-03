// B01-41 / P17 W34. Independent source-only original #923 one-writer test.
// Compiles with the UNMODIFIED #923 R3WorldActualOsQA.csproj source set.
// No real Unity or real combat attestation is present in this experiment.
#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using B01.CanonicalR3World001;
using B01.WholeGameEconomy003;
using RoboRougelike.R3;
using RoboRougelike.B01.Economy.ApprovedV7;

internal static class CI41IndependentQA
{
    private static int passed;
    private static MethodInfo Original(string method) =>
        typeof(W33Os).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new Exception("Original #923 W33Os method missing: " + method);

    private static void Check(bool value,string label)
    {
        if(!value) throw new Exception("CI41_ASSERT_FAILED " + label);
        passed++;
    }

    private static CanonicalR3SnapshotV1 OriginalWorld(R3VehicleKind kind) =>
        (CanonicalR3SnapshotV1)(Original("World").Invoke(null,new object[]{kind,1L})
            ?? throw new Exception("Original game-derived world missing"));
    private static ApprovedEconomySnapshotV7 OriginalEcon(CanonicalR3SnapshotV1 world) =>
        (ApprovedEconomySnapshotV7)(Original("Econ").Invoke(null,new object[]{world,0})
            ?? throw new Exception("Original producer V7 snapshot missing"));

    private static bool WithOriginalWriter(
        Func<WholeGameEconomySlotV3,bool> test)
    {
        string root=Path.Combine(Path.GetTempPath(),
            "ci41-w34-original-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            object?[] args={root,null,null};
            if(!(bool)(Original("Open").Invoke(null,args) ?? false) ||
               args[1]==null || args[2]==null)
                throw new Exception("Original same-writer source open failed");
            var store=(WholeGameEconomySlotV3)args[2]!;
            bool result=false;
            try { result=test(store); }
            finally
            {
                bool closed=(bool)(Original("Close").Invoke(null,
                    new object?[]{args[1],store}) ?? false);
                if(!closed) throw new Exception("Original physical owner close failure");
            }
            return result;
        }
        finally { Directory.Delete(root,true); }
    }

    private static bool WrongEconomyCategoryAccepted()
    {
        var world=OriginalWorld(R3VehicleKind.Standard);
        var econ=OriginalEcon(world);
        Check(world.Parts.Count==83,"original R3 Standard 83 intact");
        var core=world.Parts.Single(p=>p.Kind==3);
        var record=econ.Parts.Single(p=>p.Id==core.Id);
        Check(record.Category=="Core","original producer maps Core to Core");
        record.Category="Armor";  // genuine cross-world kind conflict
        Check(ApprovedEconomyPolicyV7.Valid(econ),
            "original V7 accepts nonempty category not compared with physical kind");
        return WithOriginalWriter(store=>{
            var bytes=CanonicalR3WorldCodecV1.Encode(world);
            var status=store.Commit(0,bytes,econ);
            Check(status==WholeGameResult.Accepted,
                "ACTUAL original #923 same writer accepts inconsistent core category");
            var read=store.Read(out long gen,out byte[] worldBytes,
                out ApprovedEconomySnapshotV7? result);
            Check(read==WholeGameResult.Accepted && gen==1 && result!=null,
                "actual original Store returns Accepted for inconsistent save");
            Check(CanonicalR3WorldCodecV1.TryDecode(worldBytes,out var loaded) &&
                  loaded!=null && loaded.Parts.Count==83,
                "original entire R3 world roundtrip survived");
            var saved=result!.Parts.Single(p=>p.Id==core.Id);
            Check(saved.Category=="Armor" &&
                loaded!.Parts.Single(p=>p.Id==core.Id).Kind==3,
                "mismatched economic category persists after original on-disk read");
            return true;
        });
    }

    private static bool CorruptActualHpStillDenied()
    {
        var world=OriginalWorld(R3VehicleKind.Light);
        var econ=OriginalEcon(world);
        econ.Parts[1].Hp=.5f;
        return WithOriginalWriter(store=>{
            var result=store.Commit(0,CanonicalR3WorldCodecV1.Encode(world),econ);
            Check(result==WholeGameResult.Invalid,"original world/Econ HP mismatch denied");
            Check(store.Read(out _,out _,out _)==WholeGameResult.Empty &&
                  !store.Quarantined,"invalid pair never published");
            return true;
        });
    }

    private static bool ForgedGuidStillDenied()
    {
        var world=OriginalWorld(R3VehicleKind.Heavy);
        var econ=OriginalEcon(world);
        econ.Parts[20].Id=Guid.NewGuid().ToString("N");
        return WithOriginalWriter(store=>{
            var result=store.Commit(0,CanonicalR3WorldCodecV1.Encode(world),econ);
            Check(result==WholeGameResult.Invalid,"original forged GUID mismatch denied");
            Check(store.Read(out _,out _,out _)==WholeGameResult.Empty &&
                  !store.Quarantined,"forged GUID never published");
            return true;
        });
    }

    private static int Main()
    {
        Check(WrongEconomyCategoryAccepted(),
            "original core-kind/category mismatch detected as accepted");
        Check(CorruptActualHpStillDenied(),"original HP negative remains fail closed");
        Check(ForgedGuidStillDenied(),"original GUID negative remains fail closed");
        Console.WriteLine("CI41_W34_ORIGINAL_923_STORE_INCONSISTENT_CORE_CATEGORY_ACCEPTED_PRODUCT_RED");
        Console.WriteLine("CI41_W34_ORIGINAL_923_REAL_OS_SOURCE_CROSS_FIELD_TOTAL "+
                          passed+" PASS "+passed+" FAIL 0");
        Console.WriteLine("CI41_W34_G0=HOLD_UNITY_G1=NOT_RUN_PLAYER=NOT_BUILT");
        return 0;
    }
}
