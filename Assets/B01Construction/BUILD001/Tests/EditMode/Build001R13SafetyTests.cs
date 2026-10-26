using NUnit.Framework;
using UnityEngine;

namespace B01.BUILD001.Tests
{
    public sealed class Build001R13SafetyTests
    {
        private static BuildInstalledPart Part(string id, BuildDefinition definition, int x) =>
            new BuildInstalledPart(id, definition,
                new BuildPose(new Vector3Int(x,0,0), Vector3Int.zero));

        [Test] public void W9_02_TwoLoadedCoreRootsCannotAuthorizeIsland()
        {
            var installed = new[] {
                Part("trusted",BuildFixtureCatalog.Core(),0),
                Part("other",BuildFixtureCatalog.Core(),4)
            };
            Assert.IsEmpty(BuildPlacementRules.Reachable(installed));
            var verdict = BuildPlacementRules.Check(installed,
                Part("armor",BuildFixtureCatalog.ArmorCube(),5),out var support);
            Assert.AreEqual(BuildPlacementResult.MultipleCoreRoots,verdict);
            Assert.IsNull(support);
        }

        [Test] public void W9_02_StrictInstallReportsDisconnectedFragment()
        {
            var installed = new[] {
                Part("core",BuildFixtureCatalog.Core(),0),
                Part("island",BuildFixtureCatalog.ArmorCube(),4)
            };
            var verdict = BuildPlacementRules.CheckForInstallation(installed,
                Part("armor",BuildFixtureCatalog.ArmorCube(),5),
                null,out var support);
            Assert.AreEqual(BuildPlacementResult.CoreDisconnected,verdict);
            Assert.IsNull(support);
        }

        [Test] public void W9_03_DisjointCellsCannotShareOneOwner()
        {
            var definition = new BuildDefinition("bad",BuildRole.Device,
                new[]{Vector3Int.zero,Vector3Int.right*2},
                BuildFixtureCatalog.ArmorCube().Sockets,true,
                new[]{new BuildCellBond(Vector3Int.zero,Vector3Int.right*2)});
            Assert.IsFalse(BuildSocketGeometry.Valid(definition,out var why));
            Assert.AreEqual("UNVERIFIED_PHYSICAL_JOIN",why);
            var verdict = BuildPlacementRules.Check(
                new[]{Part("core",BuildFixtureCatalog.Core(),0)},
                Part("bridge",definition,1),out _);
            Assert.AreEqual(BuildPlacementResult.DisconnectedFootprint,verdict);
        }

        [Test] public void W9_03_AdjacentCellsWithoutDeclaredPhysicalJoinCannotRelay()
        {
            var definition = new BuildDefinition("missingJoint",BuildRole.Device,
                new[]{Vector3Int.zero,Vector3Int.right,Vector3Int.right*2},
                BuildFixtureCatalog.ArmorCube().Sockets,true,
                new[]{new BuildCellBond(Vector3Int.zero,Vector3Int.right)});
            Assert.IsFalse(BuildSocketGeometry.Valid(definition,out var why));
            Assert.AreEqual("DISCONNECTED_FOOTPRINT",why);
        }

        [Test] public void W9_03_Joined2x2RotationStillWorks()
        {
            var wide=BuildFixtureCatalog.Wide2x2();
            Assert.IsTrue(BuildSocketGeometry.Valid(wide,out var why),why);
            Assert.AreEqual(4,wide.Cells.Length);
            Assert.AreEqual(3,wide.CellBonds.Length);
            Assert.IsTrue(BuildSocketGeometry.Valid(wide,out _));
        }

        [Test] public void W9_04_NullEvidenceMustNotBeCommitted()
        {
            var result = BuildPlacementRules.CheckForInstallation(
                new[]{Part("core",BuildFixtureCatalog.Core(),0)},
                Part("armor",BuildFixtureCatalog.ArmorCube(),1),
                null,out var supporter);
            Assert.AreEqual(BuildPlacementResult.PhysicsUnverified,result);
            Assert.IsNull(supporter);
        }

        [Test] public void TypedMobilityHubDoesNotWildcardIntoStructural()
        {
            var def=new BuildDefinition("typedHub",BuildRole.Device,
                new[]{Vector3Int.zero},
                new[]{new BuildSocket(Vector3Int.zero,Vector3Int.left,
                    BuildSocketKind.MobilityHub,new Vector2(.4f,.4f))},false);
            Assert.IsTrue(BuildSocketGeometry.Valid(def,out _));
            Assert.AreEqual(BuildPlacementResult.NoCompatibleSocket,
                BuildPlacementRules.Check(
                    new[]{Part("core",BuildFixtureCatalog.Core(),0)},
                    Part("hub",def,1),out _));
        }
    }
}
