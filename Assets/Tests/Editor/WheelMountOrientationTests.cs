using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4_1;
using UnityEngine;

public sealed class WheelMountOrientationTests
{
    [TestCase(0,0,0,1,0,0,true)]
    [TestCase(0,0,0,-1,0,0,false)]
    [TestCase(0,0,1,0,1,0,true)]
    [TestCase(0,0,1,0,-1,0,false)]
    public void MountFacesItsActualNeighbourAfterRotation(int x,int y,int z,int nx,int ny,int nz,bool mirror)
    {
        var design=new R3VehicleDesign{Name="Mount QA"};
        var cell=new Vector3Int(2,2,2);var turns=new Vector3Int(x,y,z);
        design.Parts.Add(new R3PartState(cell,R3PartKind.Wheel){RotationQuarterTurns=turns});
        design.Parts.Add(new R3PartState(cell+new Vector3Int(nx,ny,nz),R3PartKind.Armor));
        var model=new R3VehicleModel(design);var wheel=model.At(cell);
        var method=typeof(R4WheelArt).GetMethod("MirrorForMount",BindingFlags.Public|BindingFlags.Static);
        Assert.That(method,Is.Not.Null,"Visual mount must consider the actual support and rotation.");
        Assert.That((bool)method.Invoke(null,new object[]{model,wheel}),Is.EqualTo(mirror));
    }
}
