using NUnit.Framework;
using UnityEngine;

namespace B01.BUILD001.Tests
{
    /// <summary>Regression for native RC2 gate #258: nested socket patch overlap.</summary>
    public sealed class Build001R14ContactAreaTests
    {
        private static BuildDefinition Patch(string id, BuildRole role,
            Vector3Int normal, Vector2 half, Vector2 offset = default)
        {
            return new BuildDefinition(id, role, new[] { Vector3Int.zero },
                new[] { new BuildSocket(Vector3Int.zero, normal,
                    BuildSocketKind.Structural, half, offset) }, true);
        }

        private static bool Contact(BuildDefinition a, BuildDefinition b,
            out float squareMetres, Vector3Int turns = default)
        {
            return BuildSocketGeometry.Touches(a,
                new BuildPose(Vector3Int.zero, turns), b,
                new BuildPose(BuildSocketGeometry.Rotate(Vector3Int.up, turns), turns),
                out squareMetres);
        }

        [TestCase(.02f, false)]
        [TestCase(.05f, false)]
        [TestCase(.051f, true)]
        [TestCase(.2f, true)]
        public void RC2_NestedPatchUsesRealContactArea(float half, bool shouldTouch)
        {
            var core=Patch("core", BuildRole.Core, Vector3Int.up,
                new Vector2(.48f,.48f));
            var armor=Patch("armor", BuildRole.Armor, Vector3Int.down,
                new Vector2(half,half));
            Assert.IsTrue(BuildSocketGeometry.Valid(core, out _));
            Assert.IsTrue(BuildSocketGeometry.Valid(armor, out _));
            bool result=Contact(core,armor,out float reported);
            Assert.AreEqual(shouldTouch,result);
            Assert.That(reported,Is.EqualTo(shouldTouch
                ? 4f*half*half*BuildSocketGeometry.GridMetres*
                  BuildSocketGeometry.GridMetres : 0f).Within(.000002f));
        }

        [Test] public void RC2_NestedAcceptedAreaEqualsActualPhysicalSquareMetres()
        {
            var core=Patch("core",BuildRole.Core,Vector3Int.up,new Vector2(.48f,.48f));
            var armor=Patch("armor",BuildRole.Armor,Vector3Int.down,new Vector2(.2f,.2f));
            Assert.IsTrue(Contact(core,armor,out float metres));
            Assert.That(metres,Is.EqualTo(.0484f).Within(.000001f));
        }

        [Test] public void OffsetNestedSocketNeverBorrowsSurroundingArea()
        {
            var core=Patch("core",BuildRole.Core,Vector3Int.up,
                new Vector2(.48f,.48f));
            var small=Patch("small",BuildRole.Armor,Vector3Int.down,
                new Vector2(.06f,.06f), new Vector2(.35f,0f));
            Assert.IsTrue(Contact(core,small,out float metres));
            Assert.That(metres,Is.EqualTo(.12f*.12f*.55f*.55f).Within(.000001f));
        }

        [Test] public void NonoverlapAndEdgeOnlyRemainRejected()
        {
            var core=Patch("core",BuildRole.Core,Vector3Int.up,
                new Vector2(.2f,.2f),new Vector2(-.2f,0));
            var armor=Patch("armor",BuildRole.Armor,Vector3Int.down,
                new Vector2(.2f,.2f),new Vector2(.2f,0));
            Assert.IsFalse(Contact(core,armor,out float metres));
            Assert.AreEqual(0f,metres);
        }

        [Test] public void UnequalPartiallyOverlappingRectanglesReportExactArea()
        {
            var core=Patch("core",BuildRole.Core,Vector3Int.up,
                new Vector2(.3f,.2f));
            var armor=Patch("armor",BuildRole.Armor,Vector3Int.down,
                new Vector2(.25f,.15f),new Vector2(.2f,.0f));
            // Horizontal: core [-.3,+.3], armor [-.05,+.45] => .35
            // Vertical: core [-.2,+.2], armor [-.15,+.15] => .30
            Assert.IsTrue(Contact(core,armor,out float metres));
            Assert.That(metres,Is.EqualTo(.35f*.30f*.55f*.55f).Within(.000001f));
        }

        [TestCase(0,0,0)]
        [TestCase(1,0,0)]
        [TestCase(0,1,0)]
        [TestCase(0,0,1)]
        [TestCase(1,2,3)]
        public void QuarterTurnRotationsPreserveExactNestedArea(int x,int y,int z)
        {
            var core=Patch("core",BuildRole.Core,Vector3Int.up,new Vector2(.48f,.48f));
            var armor=Patch("armor",BuildRole.Armor,Vector3Int.down,new Vector2(.2f,.2f));
            Assert.IsTrue(Contact(core,armor,out float metres,new Vector3Int(x,y,z)));
            Assert.That(metres,Is.EqualTo(.0484f).Within(.000002f));
        }

        [Test] public void ReceiverMinimumAreaIsNotCumulativeAcrossTinySockets()
        {
            var a=Patch("core",BuildRole.Core,Vector3Int.up,new Vector2(.48f,.48f));
            var b=Patch("armor",BuildRole.Armor,Vector3Int.down,new Vector2(.02f,.02f));
            Assert.IsFalse(Contact(a,b,out float area));
            Assert.AreEqual(0f,area);
        }
    }
}
