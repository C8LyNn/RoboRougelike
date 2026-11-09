using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEngine;

public sealed class R3MultiCellWheelTests
{
    [Test] public void InstalledSideWheelRetainsOnlyItsRealSocketAcrossSaveAndRemount()
    {
        var design=new R3VehicleDesign{Name="Wheel contact QA",CoreHp=100,Speed=7,TurnSpeed=82};
        design.Parts.Add(new R3PartState(new Vector3Int(0,1,0),R3PartKind.Core));
        var model=new R3VehicleModel(design);model.Award(100);
        Assert.That(model.PurchaseToInventory(R3PartKind.Wheel),Is.True);
        var wheel=model.Inventory.Single();var id=wheel.Id;var right=new Vector3Int(1,1,0);
        Assert.That(model.Mount(id,right),Is.True);
        Assert.That(model.At(right).ContactMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.left)),"The wheel's single real socket faces its support.");
        var saved=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,null,0,out var loaded,out var error),Is.True,error);
        Assert.That(loaded.At(right).ContactMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.left)));
        Assert.That(loaded.Remove(right),Is.True);
        var left=new Vector3Int(-1,1,0);Assert.That(loaded.Mount(id,left),Is.True);
        Assert.That(loaded.At(left).ContactMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.right)),"The same recovered wheel can attach on the other side.");
        Assert.That(loaded.At(left).Id,Is.EqualTo(id));
        Assert.That(loaded.RotateMounted(left,new Vector3Int(0,1,0)),Is.False,"Rotation cannot disconnect the only axle contact.");
        var direct=new R3VehicleModel(design);direct.Award(100);
        Assert.That(direct.Buy(right,R3PartKind.Wheel),Is.True);
        Assert.That(direct.At(right).ContactMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.left)),"Direct purchase also records the real socket.");
        Assert.That(direct.Buy(new Vector3Int(0,2,0),R3PartKind.Wheel),Is.False,"A side wheel cannot attach by its top or bottom.");
    }
    [Test] public void InventoryCaptionsDistinguishOldAndNewLargeWheelsInBothLayouts()
    {
        var caption=typeof(R1RunHUD).GetMethod("CatalogItemCaption",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(caption,Is.Not.Null,"The actual inventory rows must show the footprint before selecting an item.");
        foreach(var style in new[]{R3WheelStyle.Monster,R3WheelStyle.LargeOffroad})
        foreach(bool compact in new[]{false,true})
        {
            var old=new R3PartState(Vector3Int.zero,R3PartKind.Wheel,.55f){WheelStyle=style};
            var updated=old.Copy();updated.FootprintRevision=1;
            var oldText=(string)caption.Invoke(null,new object[]{old,compact});
            var newText=(string)caption.Invoke(null,new object[]{updated,compact});
            Assert.That(oldText,Does.Contain("1 pole"));Assert.That(newText,Does.Contain("3×3"));
            Assert.That(oldText,Does.Contain("55%"));Assert.That(newText,Does.Contain("55%"));
        }
        var ordinary=new R3PartState(Vector3Int.zero,R3PartKind.Wheel,1);
        var ordinaryText=(string)caption.Invoke(null,new object[]{ordinary,true});
        Assert.That(ordinaryText,Does.Not.Contain("pole"));Assert.That(ordinaryText,Does.Not.Contain("3×3"));
    }
    static R3PartState Monster(Vector3Int cell)=>new R3PartState(cell,R3PartKind.Wheel){
        WheelStyle=R3WheelStyle.Monster,FootprintRevision=1,
        ContactMask=R3MountingGrid.FaceBit(Vector3Int.left)|R3MountingGrid.FaceBit(Vector3Int.right)};

    [Test] public void MonsterOccupiesNineRotatingCellsButKeepsOneIdentity()
    {
        var wheel=Monster(new Vector3Int(1,1,0));string id=wheel.Id;
        var flat=R3VehicleModel.OccupiedCells(wheel).ToArray();
        Assert.That(flat.Distinct().Count(),Is.EqualTo(9));
        Assert.That(flat,Does.Contain(new Vector3Int(1,0,-1)));
        Assert.That(flat,Does.Contain(new Vector3Int(1,2,1)));
        wheel.RotationQuarterTurns=new Vector3Int(0,1,0);
        var rotated=R3VehicleModel.OccupiedCells(wheel).ToArray();
        Assert.That(rotated.Distinct().Count(),Is.EqualTo(9));
        Assert.That(rotated,Is.EquivalentTo(flat.Select(cell=>wheel.Cell+R3MountingGrid.Rotate(cell-wheel.Cell,wheel.RotationQuarterTurns))));
        Assert.That(wheel.Id,Is.EqualTo(id));
    }

    [Test] public void TireCellsAreOccupiedButDoNotBecomeMountingSockets()
    {
        var core=new R3PartState(new Vector3Int(0,1,0),R3PartKind.Core);
        var wheel=Monster(new Vector3Int(1,1,0));
        var unsupported=new R3PartState(new Vector3Int(1,3,0),R3PartKind.Armor);
        Assert.That(R3VehicleModel.Connected(new[]{core,wheel}),Is.True,"The central axle connects to the chassis.");
        Assert.That(R3VehicleModel.Connected(new[]{core,wheel,unsupported}),Is.False,"The tire's outer cell is not a socket.");
    }

    [Test] public void NewLargeOffroadWheelUsesItsOwnNineCellFootprintAndLegacyStillFits()
    {
        var design=new R3VehicleDesign{Name="Large wheel QA"};
        design.Parts.Add(new R3PartState(new Vector3Int(0,1,0),R3PartKind.Core));
        var model=new R3VehicleModel(design);model.Award(100);
        Assert.That(model.PurchaseToInventory(R3PartKind.Wheel,default,R3WheelStyle.LargeOffroad),Is.True);
        var part=model.Inventory.Last();var anchor=new Vector3Int(1,1,0);
        Assert.That(part.FootprintRevision,Is.EqualTo(1));
        Assert.That(model.OwnedPlacementError(part.Id,anchor,Vector3Int.zero),Is.Null);
        Assert.That(model.Mount(part.Id,anchor),Is.True);
        Assert.That(R3VehicleModel.OccupiedCells(model.At(anchor)).Count(),Is.EqualTo(9));
        Assert.That(model.At(anchor+Vector3Int.up).Id,Is.EqualTo(part.Id));
        var save=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(save,R3VehicleKind.Light,null,0,out var loaded,out var error),Is.True,error);
        Assert.That(loaded.At(anchor+Vector3Int.up).Id,Is.EqualTo(part.Id));
        var legacy=part.Copy();legacy.FootprintRevision=0;
        Assert.That(R3VehicleModel.OccupiedCells(legacy).Count(),Is.EqualTo(1));
        Assert.That(R3VehicleModel.SameFusionType(part,legacy),Is.False);
    }

    [Test] public void NewAndLegacyFootprintsRoundTripWithoutReinterpretingOldItems()
    {
        var design=new R3VehicleDesign{Name="test",CoreHp=100,Speed=7.5f,TurnSpeed=82};
        design.Parts.Add(new R3PartState(new Vector3Int(0,1,0),R3PartKind.Core));
        design.Parts.Add(Monster(new Vector3Int(1,1,0)));
        var source=new R3VehicleModel(design);var saved=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(source.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,null,0,out var restored,out var error),Is.True,error);
        var wheel=restored.Parts.Single(p=>p.Kind==R3PartKind.Wheel);
        Assert.That(wheel.FootprintRevision,Is.EqualTo(1));Assert.That(R3VehicleModel.OccupiedCells(wheel).Count(),Is.EqualTo(9));
        wheel.FootprintRevision=0;Assert.That(R3VehicleModel.OccupiedCells(wheel).Count(),Is.EqualTo(1),"Old saved items retain their footprint.");
        Assert.That(R3VehicleModel.SameFusionType(wheel,source.Parts.Single(p=>p.Kind==R3PartKind.Wheel)),Is.False);
        saved.mounted.Single(p=>p.Kind==R3PartKind.Wheel).FootprintRevision=99;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,null,0,out _,out _),Is.False,"Unknown footprint revisions fail closed.");
    }
}
