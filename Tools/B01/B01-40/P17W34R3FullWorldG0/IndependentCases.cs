// B01-40 / PASS17 W34. Real ORIGINAL #923 CanonicalR3WorldCodecV1
// is compiled with ORIGINAL #877 R3VehicleCatalog.Create, through the
// original #923 explicit Unity compile-only shim.
// Tests are independent of B01-62 producer Program.cs; NO Unity/PhysX,
// no real R3/Rig[] adapter, no trusted game victory receipt.
#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using B01.CanonicalR3World001;
using RoboRougelike.R3;

internal static class IndependentW34
{
    private static int pass, fail;
    static void Check(string label, Func<bool> f)
    {
        try { if(f()) { pass++; Console.WriteLine("PASS "+label); }
              else { fail++; Console.WriteLine("FAIL "+label); } }
        catch(Exception e) { fail++; Console.WriteLine("FAIL "+label+" "+e); }
    }
    private static R3SavedPartV1 NewPart(int n,int kind=0)
    {
        return new R3SavedPartV1 {
            Id=Guid.NewGuid().ToString("N"), PhysicalId=800000+n,
            OwnerPhysicalId=700001, Location=R3PartLocationV1.Mounted,
            Kind=kind, Weapon=0, CellX=n%300, CellY=n/300, CellZ=0,
            Contacts=63,Hp=1,Ammo=-1,DefenseCharge=-1,
            DefenseRechargeAge=float.PositiveInfinity,
            Qw=1,Mass=1,Source="B0140-W34-independent-OS"
        };
    }
    private static CanonicalR3SnapshotV1 World(R3VehicleKind kind)
    {
        var design=R3VehicleCatalog.Create(kind); // TRUE original factory source
        var w=new CanonicalR3SnapshotV1 {
            RunId=6340062,Generation=1,VehicleKind=(int)kind
        };
        int n=0;
        foreach(var part in design.Parts)
        {
            var p=NewPart(++n,(int)part.Kind);
            p.CellX=part.Cell.x;p.CellY=part.Cell.y;p.CellZ=part.Cell.z;
            p.Px=p.CellX*.55f;p.Py=p.CellY*.55f;p.Pz=p.CellZ*.55f;
            w.Parts.Add(p);
        }
        return w;
    }
    static bool FactoryRoundTrip(R3VehicleKind kind,int expected)
    {
        var original=R3VehicleCatalog.Create(kind);
        var world=World(kind);
        if(original.Parts.Count!=expected||world.Parts.Count!=expected||
           !CanonicalR3WorldCodecV1.Valid(world)||
           world.Parts.Count(x=>x.Kind==3)!=1)return false;
        byte[] encoded=CanonicalR3WorldCodecV1.Encode(world);
        if(!CanonicalR3WorldCodecV1.TryDecode(encoded,out var cold)||
           cold==null||cold.Parts.Count!=expected||
           !encoded.SequenceEqual(CanonicalR3WorldCodecV1.Encode(cold)))
           return false;
        for(int i=0;i<expected;i++)
        {
            var left=world.Parts[i];var right=cold.Parts[i];
            if(left.Id!=right.Id||left.PhysicalId!=right.PhysicalId||
               left.OwnerPhysicalId!=right.OwnerPhysicalId||
               left.Kind!=right.Kind||left.CellX!=right.CellX||
               left.CellY!=right.CellY||left.CellZ!=right.CellZ||
               left.Px!=right.Px||left.Py!=right.Py||left.Pz!=right.Pz||
               right.Px!=right.CellX*.55f||
               right.Py!=right.CellY*.55f||
               right.Pz!=right.CellZ*.55f)
                return false;
        }
        Console.WriteLine("B0140_W34_ORIGINAL_R3_PARTS_"+expected+"_EXACT_DATA_ROUNDTRIP_PASS");
        return true;
    }
    static bool Max1024StrictLimit()
    {
        var w=World(R3VehicleKind.Light);
        for(int i=w.Parts.Count;i<1024;i++)w.Parts.Add(NewPart(i+1));
        if(w.Parts.Count!=CanonicalR3WorldCodecV1.MaxParts||
           !CanonicalR3WorldCodecV1.Valid(w))return false;
        byte[] encoded=CanonicalR3WorldCodecV1.Encode(w);
        if(!CanonicalR3WorldCodecV1.TryDecode(encoded,out var cold)||
           cold==null||cold.Parts.Count!=1024)return false;
        w.Parts.Add(NewPart(1025));
        if(CanonicalR3WorldCodecV1.Valid(w))return false;
        try { CanonicalR3WorldCodecV1.Encode(w); return false; }
        catch(System.IO.InvalidDataException) { return true; }
    }
    static bool DifferentGuidCollisionDenied()
    {
        var w=World(R3VehicleKind.Standard);
        w.Parts[3].Id=w.Parts[2].Id;
        return !CanonicalR3WorldCodecV1.Valid(w);
    }
    static bool DifferentPhysicalIdCollisionDenied()
    {
        var w=World(R3VehicleKind.Heavy);
        w.Parts[12].PhysicalId=w.Parts[11].PhysicalId;
        return !CanonicalR3WorldCodecV1.Valid(w);
    }
    static bool MultipleCoresDenied()
    {
        var w=World(R3VehicleKind.Light);
        w.Parts[2].Kind=3;
        return !CanonicalR3WorldCodecV1.Valid(w);
    }
    static bool MissingMountedCoreDenied()
    {
        var w=World(R3VehicleKind.Standard);
        var core=w.Parts.Single(p=>p.Kind==3);
        core.Location=R3PartLocationV1.Inventory;
        return !CanonicalR3WorldCodecV1.Valid(w);
    }
    static bool FakeDetachedStateDenied()
    {
        var w=World(R3VehicleKind.Light);
        w.Parts[1].Location=R3PartLocationV1.Detached;
        w.Parts[1].ConsumedDetach=false;
        return !CanonicalR3WorldCodecV1.Valid(w);
    }
    static bool ActualDetachedAndInventoryHaveDistinctTags()
    {
        var w=World(R3VehicleKind.Heavy);
        var inventory=w.Parts.First(p=>p.Kind==0);
        var detached=w.Parts.First(p=>p.Kind==1);
        inventory.Location=R3PartLocationV1.Inventory;
        inventory.Ammo=43;
        inventory.UpgradeId="B0140-salvaged";
        inventory.UpgradeAmount=.42f;
        detached.Location=R3PartLocationV1.Detached;
        detached.ConsumedDetach=true;
        detached.Vx=2.75f;detached.Wz=-1.75f;
        var raw=CanonicalR3WorldCodecV1.Encode(w);
        return CanonicalR3WorldCodecV1.TryDecode(raw,out var cold)&&cold!=null&&
            cold.Parts.Count==139&&
            cold.Parts.Single(p=>p.Id==inventory.Id).Location==R3PartLocationV1.Inventory&&
            cold.Parts.Single(p=>p.Id==inventory.Id).Ammo==43&&
            cold.Parts.Single(p=>p.Id==inventory.Id).UpgradeId=="B0140-salvaged"&&
            cold.Parts.Single(p=>p.Id==detached.Id).Location==R3PartLocationV1.Detached&&
            cold.Parts.Single(p=>p.Id==detached.Id).ConsumedDetach&&
            cold.Parts.Single(p=>p.Id==detached.Id).Vx==2.75f&&
            cold.Parts.Single(p=>p.Id==detached.Id).Wz==-1.75f;
    }
    static bool NonUnitQuaternionAndNanDenied()
    {
        var w=World(R3VehicleKind.Standard);
        w.Parts[1].Qw=.1f;
        bool badQuaternion=!CanonicalR3WorldCodecV1.Valid(w);
        w.Parts[1].Qw=1;
        w.Parts[1].ThermalHeat=float.NaN;
        return badQuaternion&&!CanonicalR3WorldCodecV1.Valid(w);
    }
    static bool InvalidResourceAndCellDenied()
    {
        var w=World(R3VehicleKind.Light);
        w.Parts[2].Hp=-.1f;
        bool badHp=!CanonicalR3WorldCodecV1.Valid(w);
        w.Parts[2].Hp=1;
        w.Parts[2].Ammo=-2;
        bool badAmmo=!CanonicalR3WorldCodecV1.Valid(w);
        w.Parts[2].Ammo=-1;
        w.Parts[2].CellX=513;
        return badHp&&badAmmo&&!CanonicalR3WorldCodecV1.Valid(w);
    }
    static bool WrongMagicVersionAndTruncationDenied()
    {
        var bytes=CanonicalR3WorldCodecV1.Encode(World(R3VehicleKind.Light));
        var bad=(byte[])bytes.Clone();bad[0]^=1;
        var version=(byte[])bytes.Clone();version[8]^=1;
        var tail=bytes.Concat(new byte[]{0xff}).ToArray();
        var shortOne=bytes.Take(bytes.Length-1).ToArray();
        return !CanonicalR3WorldCodecV1.TryDecode(bad,out _)&&
               !CanonicalR3WorldCodecV1.TryDecode(version,out _)&&
               !CanonicalR3WorldCodecV1.TryDecode(tail,out _)&&
               !CanonicalR3WorldCodecV1.TryDecode(shortOne,out _);
    }
    static bool EmptyOrNoPartsDenied()
    {
        var w=World(R3VehicleKind.Light);
        w.Parts.Clear();
        return !CanonicalR3WorldCodecV1.Valid(w)&&
               !CanonicalR3WorldCodecV1.TryDecode(Array.Empty<byte>(),out _);
    }
    static int Main()
    {
        Check("actual original R3 Light20 0.55m geometry IDs",()=>FactoryRoundTrip(R3VehicleKind.Light,20));
        Check("actual original R3 Standard83 0.55m geometry IDs",()=>FactoryRoundTrip(R3VehicleKind.Standard,83));
        Check("actual original R3 Heavy139 0.55m geometry IDs",()=>FactoryRoundTrip(R3VehicleKind.Heavy,139));
        Check("1024 accepted / 1025 rejected on actual codec",Max1024StrictLimit);
        Check("duplicate stable GUID rejected",DifferentGuidCollisionDenied);
        Check("duplicate physical ID rejected",DifferentPhysicalIdCollisionDenied);
        Check("multiple Cores fail closed",MultipleCoresDenied);
        Check("unmounted Core fails closed",MissingMountedCoreDenied);
        Check("detached tag requires consumed flag",FakeDetachedStateDenied);
        Check("inventory and detached resource state survives codec",ActualDetachedAndInventoryHaveDistinctTags);
        Check("nonunit orientation and NaN thermal rejected",NonUnitQuaternionAndNanDenied);
        Check("out-of-range HP/ammo/cell rejected",InvalidResourceAndCellDenied);
        Check("wrong magic/version/junk/truncation rejected",WrongMagicVersionAndTruncationDenied);
        Check("empty missing parts rejected",EmptyOrNoPartsDenied);
        Console.WriteLine("B0140_W34_INDEPENDENT_ORIGINAL_R3_CODEC_TOTAL "+
            (pass+fail)+" PASS "+pass+" FAIL "+fail);
        Console.WriteLine("B0140_W34_NATIVE_UNITY_NOT_RUN_NO_REAL_PHYSICS_BINDER");
        return fail==0?0:1;
    }
}
