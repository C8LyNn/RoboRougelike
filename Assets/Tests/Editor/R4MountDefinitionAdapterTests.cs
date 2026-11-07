using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using B01.BUILD001;
using NUnit.Framework;
using UnityEngine;

public sealed class R4MountDefinitionAdapterTests
{
    private static bool Create(string id, bool moduleSupport, out BuildDefinition definition)
    {
        var type = Type.GetType("RoboRougelike.R4_1.R4MountDefinitionAdapter, Assembly-CSharp");
        Assert.That(type, Is.Not.Null);
        var args = new object[] { id, moduleSupport, null };
        bool result = (bool)type.GetMethod("TryCreate").Invoke(null, args);
        definition = (BuildDefinition)args[2];
        return result;
    }

    [Test] public void DefinitionsRetainStructuralPatchesAndFilterModuleSupport()
    {
        Assert.That(Create("R4B_Wedge", false, out var structural), Is.True);
        Assert.That(Create("R4B_Wedge", true, out var module), Is.True);
        Assert.That(BuildSocketGeometry.Valid(structural, out var structuralError), Is.True, structuralError);
        Assert.That(BuildSocketGeometry.Valid(module, out var moduleError), Is.True, moduleError);
        Assert.That(structural.Sockets.Length, Is.EqualTo(4));
        Assert.That(module.Sockets.Length, Is.EqualTo(2));
        Assert.That(Create("R4B_OctColumn", true, out _), Is.False);
        Assert.That(Create("R4B_Tetra", true, out _), Is.False);
        Assert.That(Create("missing", false, out _), Is.False);

        var wheel = new BuildDefinition("wheel", BuildRole.Wheel, new[] { Vector3Int.zero },
            new[] { new BuildSocket(Vector3Int.zero, Vector3Int.right, BuildSocketKind.Axle,
                new Vector2(.47f, .47f)) }, false);
        var supportPose = new BuildPose(Vector3Int.zero, Vector3Int.zero);
        var wheelPose = new BuildPose(Vector3Int.left, Vector3Int.zero);
        Assert.That(BuildSocketGeometry.Touches(structural, supportPose, wheel, wheelPose, out _), Is.True,
            "The existing overlap geometry alone accepts the partial patch.");
        Assert.That(BuildSocketGeometry.Touches(module, supportPose, wheel, wheelPose, out _), Is.False,
            "The module-specific definition must exclude the partial patch.");
    }

    [Test] public void MultiCellDefinitionCopiesFootprintAndBonds()
    {
        Assert.That(Create("R4B_Beam3", false, out var first), Is.True);
        Assert.That(first.Cells.Length, Is.EqualTo(3));
        Assert.That(first.CellBonds.Length, Is.EqualTo(2));
        Assert.That(BuildSocketGeometry.Valid(first, out var error), Is.True, error);
        first.Cells[0] = new Vector3Int(99, 99, 99);
        Assert.That(Create("R4B_Beam3", false, out var second), Is.True);
        Assert.That(second.Cells[0], Is.EqualTo(Vector3Int.zero));
    }

    [Test] public void UncoveredColumnEndPatchesCannotProvideStructuralSupport()
    {
        Assert.That(Create("R4B_HexColumn",false,out _),Is.False,
            "Both declared hexagonal patches are quarantined until the source is corrected.");
        Assert.That(Create("R4B_OctColumn",false,out var oct),Is.True);
        Assert.That(oct.Sockets.Length,Is.EqualTo(4),"Only the four covered side patches remain.");
        Assert.That(oct.Sockets.All(s=>s.Face.y==0),Is.True);
        var cube=BuildFixtureCatalog.ArmorCube();
        for(int x=0;x<4;x++)for(int y=0;y<4;y++)for(int z=0;z<4;z++)
        {
            var turns=new Vector3Int(x,y,z);var support=new BuildPose(Vector3Int.zero,turns);
            var above=new BuildPose(BuildSocketGeometry.Rotate(Vector3Int.up,turns),turns);
            var side=new BuildPose(BuildSocketGeometry.Rotate(Vector3Int.right,turns),turns);
            Assert.That(BuildSocketGeometry.Touches(oct,support,cube,above,out _),Is.False,"Quarantined end: "+turns);
            Assert.That(BuildSocketGeometry.Touches(oct,support,cube,side,out _),Is.True,"Covered side remains usable: "+turns);
        }
    }

    [Test] public void RotatedWheelSupportsUseOnlyFullPatchesInAllOrientations()
    {
        Assert.That(Create("R4B_Wedge", false, out var structural), Is.True);
        Assert.That(Create("R4B_Wedge", true, out var wedgeModule), Is.True);
        Assert.That(Create("R4B_Cube", true, out var cubeModule), Is.True);
        var wheel = new BuildDefinition("wheel", BuildRole.Wheel, new[] { Vector3Int.zero },
            new[] { new BuildSocket(Vector3Int.zero, Vector3Int.right, BuildSocketKind.Axle,
                new Vector2(.47f, .47f)) }, false);
        var orientations = new HashSet<string>();
        for (int x=0; x<4; x++) for (int y=0; y<4; y++) for (int z=0; z<4; z++)
        {
            var turns = new Vector3Int(x,y,z);
            var right = BuildSocketGeometry.Rotate(Vector3Int.right,turns);
            var up = BuildSocketGeometry.Rotate(Vector3Int.up,turns);
            orientations.Add(right.ToString()+"/"+up.ToString());
            var support = new BuildPose(Vector3Int.zero,turns);
            var attached = new BuildPose(BuildSocketGeometry.Rotate(Vector3Int.left,turns),turns);
            Assert.That(BuildSocketGeometry.Touches(structural,support,wheel,attached,out _),Is.True,
                "A partial structural overlap should remain: "+turns);
            Assert.That(BuildSocketGeometry.Touches(wedgeModule,support,wheel,attached,out _),Is.False,
                "A rotated partial port cannot support a wheel: "+turns);
            Assert.That(BuildSocketGeometry.Touches(cubeModule,support,wheel,attached,out _),Is.True,
                "A rotated full port should support a wheel: "+turns);
        }
        Assert.That(orientations.Count,Is.EqualTo(24));
    }

    [Test] public void RotatedGunBasesCannotUsePartialStructuralPatches()
    {
        Assert.That(Create("R4B_Wedge",false,out var structural),Is.True);
        Assert.That(Create("R4B_Wedge",true,out var module),Is.True);
        Assert.That(Create("R4B_Cube",true,out var cube),Is.True);
        var gun=new BuildDefinition("gun",BuildRole.Gun,new[]{Vector3Int.zero},
            new[]{new BuildSocket(Vector3Int.zero,Vector3Int.right,BuildSocketKind.WeaponBase,
                new Vector2(.47f,.47f))},false);
        var orientations=new HashSet<string>();
        for(int x=0;x<4;x++)for(int y=0;y<4;y++)for(int z=0;z<4;z++)
        {
            var turns=new Vector3Int(x,y,z);
            orientations.Add(BuildSocketGeometry.Rotate(Vector3Int.right,turns)+"/"+BuildSocketGeometry.Rotate(Vector3Int.up,turns));
            var support=new BuildPose(Vector3Int.zero,turns);
            var attached=new BuildPose(BuildSocketGeometry.Rotate(Vector3Int.left,turns),turns);
            Assert.That(BuildSocketGeometry.Touches(structural,support,gun,attached,out _),Is.True,
                "Structural overlap alone is not authorization for a gun: "+turns);
            Assert.That(BuildSocketGeometry.Touches(module,support,gun,attached,out _),Is.False,
                "A rotated partial port must not authorize a gun: "+turns);
            Assert.That(BuildSocketGeometry.Touches(cube,support,gun,attached,out _),Is.True,
                "A rotated full port can support a gun: "+turns);
        }
        Assert.That(orientations.Count,Is.EqualTo(24));
    }

    [Test] public void EveryAuthoredShapeKeepsOneOwnerAndBlocksEachRotatedOccupiedCell()
    {
        var catalog=Type.GetType("R4BlockCatalog, Assembly-CSharp");Assert.That(catalog,Is.Not.Null);
        var entries=(IEnumerable)catalog.GetField("All").GetValue(null);int count=0;
        foreach(var entry in entries)
        {
            string id=(string)entry.GetType().GetField("Id").GetValue(entry);
            if(id=="R4B_HexColumn")
            {
                Assert.That(Create(id,false,out _),Is.False,"All hex ports remain quarantined.");count++;continue;
            }
            Assert.That(Create(id,false,out var shape),Is.True,id);
            Assert.That(BuildSocketGeometry.Valid(shape,out var reason),Is.True,id+": "+reason);
            var rotations=new HashSet<string>();
            for(int x=0;x<4;x++)for(int y=0;y<4;y++)for(int z=0;z<4;z++)
            {
                var turns=new Vector3Int(x,y,z);
                rotations.Add(BuildSocketGeometry.Rotate(Vector3Int.right,turns)+"/"+BuildSocketGeometry.Rotate(Vector3Int.up,turns));
                var offsets=shape.Cells.Select(c=>BuildSocketGeometry.Rotate(c,turns)).ToArray();
                var anchor=new Vector3Int(0,-offsets.Min(c=>c.y),0);
                var pose=new BuildPose(anchor,turns);
                var cells=BuildSocketGeometry.Occupied(shape,pose).ToArray();
                Assert.That(cells.Distinct().Count(),Is.EqualTo(shape.Cells.Length),id+" "+turns);
                Assert.That(cells.All(c=>Math.Abs(c.x)<=6&&Math.Abs(c.z)<=7&&c.y>=0&&c.y<=5),Is.True,id+" "+turns);
                var installed=new[]{new BuildInstalledPart("one-owner",shape,pose)};
                foreach(var cell in cells)
                {
                    var collision=new BuildInstalledPart("another-part",BuildFixtureCatalog.ArmorCube(),new BuildPose(cell,Vector3Int.zero));
                    Assert.That(BuildPlacementRules.Check(installed,collision,out _),Is.EqualTo(BuildPlacementResult.OccupiedCells),id+" "+turns+" "+cell);
                }
            }
            Assert.That(rotations.Count,Is.EqualTo(24),id);count++;
        }
        Assert.That(count,Is.EqualTo(30));
    }
}
