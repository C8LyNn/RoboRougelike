using System.Reflection;
using System.Collections;
using System.Linq;
using UnityEngine.TestTools;
using RoboRougelike.R4;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4_1;
using UnityEngine;

public sealed class WheelMountOrientationTests
{
    [UnityTest] public IEnumerator RealVisualPortPointsToRotatedSupport()
    {
        yield return new EnterPlayMode();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        foreach(var style in new[]{R3WheelStyle.Offroad,R3WheelStyle.Slick,R3WheelStyle.LargeOffroad,R3WheelStyle.Monster})
        foreach(var turns in new[]{Vector3Int.zero,new Vector3Int(0,0,1)})
        foreach(var sign in new[]{-1,1})
        {
            var axis=R3MountingGrid.Rotate(Vector3Int.right,turns)*sign;var cell=new Vector3Int(2,2,2);
            var design=new R3VehicleDesign{Name="Visual mount QA"};
            design.Parts.Add(new R3PartState(cell,R3PartKind.Wheel){RotationQuarterTurns=turns,WheelStyle=style});
            design.Parts.Add(new R3PartState(cell+axis,R3PartKind.Armor));
            var model=new R3VehicleModel(design);var state=model.At(cell);
            var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.localScale=Vector3.one*R41PartGeometry.BlockSize;
            block.transform.rotation=R3MountingGrid.Rotation(turns);
            bool mirror=R4WheelArt.MirrorForMount(model,state);
            var physical=R41PartGeometry.Payload(block.transform,R3PartKind.Wheel,R4WeaponKind.Basic,material,true,out _,style,mirror);
            Assert.That(R4WheelArt.TryAttach(block.transform,physical,mirror,style),Is.Not.Null);
            var socket=block.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Socket_Mount");
            var rotor=block.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(style==R3WheelStyle.Monster?"R4M_WheelXL_Side_Monster_Rotor":style==R3WheelStyle.LargeOffroad?"R4M_WheelL_Side_Offroad_Rotor":style==R3WheelStyle.Slick?"R4M_WheelM_Side_Slick_Rotor":"R4M_WheelM_Side_Offroad_Rotor"));
            Assert.That(Vector3.Dot((socket.position-rotor.position).normalized,(Vector3)axis),Is.GreaterThan(.9f),style+" "+turns+" "+sign);
            if(R3VehicleModel.HasSizedSideWheel(style))
                Assert.That(Vector3.Distance(socket.position,block.transform.position+(Vector3)axis*.26f),Is.LessThan(.015f),style+" wheel port must meet the support face.");
            Object.Destroy(block);
        }
        Object.Destroy(material);yield return null;yield return new ExitPlayMode();
    }
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
