using B01.BUILD001;
using NUnit.Framework;
using UnityEngine;

// Receiver gate for real contact area, not an implementation or weakened producer test.
public sealed class BuildRC2SafetyTests
{
    static BuildDefinition Patch(string id,BuildRole role,Vector3Int face,float half)=>
        new BuildDefinition(id,role,new[]{Vector3Int.zero},
            new[]{new BuildSocket(Vector3Int.zero,face,BuildSocketKind.Structural,new Vector2(half,half))},true);
    [TestCase(.02f,false)]
    [TestCase(.2f,true)]
    public void NestedPatchMustSatisfyActualMinimumContactArea(float half,bool expected)
    {
        var support=Patch("Core",BuildRole.Core,Vector3Int.up,.48f);
        var candidate=Patch("Armor",BuildRole.Armor,Vector3Int.down,half);
        Assert.That(BuildSocketGeometry.Valid(support,out _),Is.True);
        Assert.That(BuildSocketGeometry.Valid(candidate,out _),Is.True);
        float actualCellsArea=4*half*half;
        Assert.That(actualCellsArea>BuildSocketGeometry.MinContactAreaCellsSquared,Is.EqualTo(expected));
        Assert.That(BuildSocketGeometry.Touches(support,new BuildPose(Vector3Int.zero,default),
            candidate,new BuildPose(Vector3Int.up,default),out _),Is.EqualTo(expected),
            "A nested patch cannot obtain contact area from the larger surrounding socket");
    }
    [Test] public void AcceptedNestedPatchReportsItsTrueGeometricArea()
    {
        var support=Patch("Core",BuildRole.Core,Vector3Int.up,.48f);
        var candidate=Patch("Armor",BuildRole.Armor,Vector3Int.down,.2f);
        Assert.That(BuildSocketGeometry.Touches(support,new BuildPose(Vector3Int.zero,default),
            candidate,new BuildPose(Vector3Int.up,default),out float area),Is.True);
        Assert.That(area,Is.EqualTo(.4f*.4f*.55f*.55f).Within(.000001f),
            "Overlap is the intersection of the two intervals, including nested intervals");
    }
}
