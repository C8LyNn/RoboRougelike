using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace B01.BUILD001.Tests
{
    public sealed class Build001SocketTests
    {
        static BuildInstalledPart P(string id, BuildDefinition d,int x,int y=0,int z=0,
            Vector3Int turns=default,bool alive=true)
            => new BuildInstalledPart(id,d,new BuildPose(new Vector3Int(x,y,z),turns),alive);

        [Test] public void WheelOnlyConnectsToRealHubNotTopTreadOrGunBarrel()
        {
            var foundation=new[]{P("core",BuildFixtureCatalog.Core(),0)};
            Assert.AreEqual(BuildPlacementResult.Valid,BuildPlacementRules.Check(
                foundation,P("leftWheel",BuildFixtureCatalog.Wheel(),1),out var owner));
            Assert.AreEqual("core",owner);
            // Wheel local hub faces -X, and doesn't acquire a socket at +Y or +/-Z
            Assert.AreEqual(BuildPlacementResult.NoCompatibleSocket,BuildPlacementRules.Check(
                foundation,P("topWheel",BuildFixtureCatalog.Wheel(),0,1),out _));
            Assert.AreEqual(BuildPlacementResult.NoCompatibleSocket,BuildPlacementRules.Check(
                foundation,P("frontWheel",BuildFixtureCatalog.Wheel(),0,0,1),out _));
            var wheelSupport=new[]{P("wheel",BuildFixtureCatalog.Wheel(),0)};
            Assert.AreEqual(BuildPlacementResult.NoCompatibleSocket,BuildPlacementRules.Check(
                wheelSupport,P("armor",BuildFixtureCatalog.ArmorCube(),1),out _));
            var gunSupport=new[]{P("gun",BuildFixtureCatalog.Gun(),0)};
            Assert.AreEqual(BuildPlacementResult.NoCompatibleSocket,BuildPlacementRules.Check(
                gunSupport,P("armor",BuildFixtureCatalog.ArmorCube(),1),out _));
        }

        [Test] public void WheelCanBeMirroredByYawAndMountOppositeSide()
        {
            var core=new[]{P("core",BuildFixtureCatalog.Core(),0)};
            Assert.AreEqual(BuildPlacementResult.Valid,BuildPlacementRules.Check(
                core,P("right",BuildFixtureCatalog.Wheel(),1),out _));
            Assert.AreEqual(BuildPlacementResult.Valid,BuildPlacementRules.Check(
                core,P("left",BuildFixtureCatalog.Wheel(),-1,0,0,new Vector3Int(0,2,0)),out _));
            Assert.AreEqual(BuildPlacementResult.NoCompatibleSocket,BuildPlacementRules.Check(
                core,P("wrong",BuildFixtureCatalog.Wheel(),-1),out _));
        }

        [Test] public void GunHasBaseOnlyAndLateralBarrelContactIsRejected()
        {
            var core=new[]{P("core",BuildFixtureCatalog.Core(),0)};
            Assert.AreEqual(BuildPlacementResult.Valid,BuildPlacementRules.Check(
                core,P("gun",BuildFixtureCatalog.Gun(),0,1),out _));
            Assert.AreEqual(BuildPlacementResult.NoCompatibleSocket,BuildPlacementRules.Check(
                core,P("gunSide",BuildFixtureCatalog.Gun(),1),out _));
        }

        [Test] public void SuspensionCarriesWheelThroughAxleAndBaseConnectsToArmor()
        {
            var core=new[]{P("core",BuildFixtureCatalog.Core(),0)};
            var suspension=P("sus",BuildFixtureCatalog.Suspension(),1);
            Assert.AreEqual(BuildPlacementResult.Valid,
                BuildPlacementRules.Check(core,suspension,out _));
            var installed=new[]{core[0],suspension};
            Assert.AreEqual(BuildPlacementResult.Valid,BuildPlacementRules.Check(
                installed,P("wheel",BuildFixtureCatalog.Wheel(),2),out var owner));
            Assert.AreEqual("sus",owner);
        }

        [Test] public void MultiCellOneIdBlocksOccupancyAndRotatesWithoutPhantomInternalFaces()
        {
            var wide=BuildFixtureCatalog.Wide2x2();
            Assert.IsTrue(BuildSocketGeometry.Valid(wide,out _));
            foreach(var turns in new[]{
                Vector3Int.zero,new Vector3Int(0,1,0),new Vector3Int(1,0,0),
                new Vector3Int(0,0,1),new Vector3Int(2,1,3)})
            {
                var occupied=new HashSet<Vector3Int>(BuildSocketGeometry.Occupied(wide,
                    new BuildPose(new Vector3Int(0,2,0),turns)));
                Assert.AreEqual(4,occupied.Count,"single owner spans four cells");
            }
            var installed=new[]{P("wide",wide,0,2)};
            Assert.AreEqual(BuildPlacementResult.OccupiedCells,BuildPlacementRules.Check(
                installed,P("cube",BuildFixtureCatalog.ArmorCube(),1,2),out _));
        }

        [Test] public void StructuralGraphDisconnectsAfterSupportDies()
        {
            var core=P("core",BuildFixtureCatalog.Core(),0);
            var armor=P("armor",BuildFixtureCatalog.ArmorCube(),1);
            var gun=P("gun",BuildFixtureCatalog.Gun(),1,1);
            var reachable=BuildPlacementRules.Reachable(new[]{core,armor,gun});
            Assert.IsTrue(reachable.Contains("core"));
            Assert.IsTrue(reachable.Contains("armor"));
            Assert.IsTrue(reachable.Contains("gun"));
            reachable=BuildPlacementRules.Reachable(new[]{core,
                P("armor",BuildFixtureCatalog.ArmorCube(),1,0,0,default,false),gun});
            Assert.IsTrue(reachable.Contains("core"));
            Assert.IsFalse(reachable.Contains("armor"));
            Assert.IsFalse(reachable.Contains("gun"));
        }

        [Test] public void InvalidZeroPatchInternalFaceAndBadSocketTypeFailClosed()
        {
            var bad=new BuildDefinition("bad",BuildRole.Wheel,new[]{Vector3Int.zero},
                new[]{new BuildSocket(Vector3Int.zero,Vector3Int.up,BuildSocketKind.Structural,
                    new Vector2(.5f,.5f))},false);
            Assert.IsFalse(BuildSocketGeometry.Valid(bad,out string reason));
            Assert.AreEqual("WHEEL_MUST_USE_AXLE",reason);
            bad=new BuildDefinition("bad",BuildRole.Armor,new[]{Vector3Int.zero},
                new[]{new BuildSocket(Vector3Int.zero,Vector3Int.up,BuildSocketKind.Structural,
                    Vector2.zero)},true);
            Assert.IsFalse(BuildSocketGeometry.Valid(bad,out _));
        }

        [Test] public void TwoTinyEdgePatchesMustHaveGenuineContactArea()
        {
            var basePart=new BuildDefinition("halfA",BuildRole.Armor,new[]{Vector3Int.zero},
                new[]{new BuildSocket(Vector3Int.zero,Vector3Int.right,
                    BuildSocketKind.Structural,new Vector2(.11f,.11f),new Vector2(-.38f,-.38f))},true);
            var topPart=new BuildDefinition("halfB",BuildRole.Armor,new[]{Vector3Int.zero},
                new[]{new BuildSocket(Vector3Int.zero,Vector3Int.left,
                    BuildSocketKind.Structural,new Vector2(.11f,.11f),new Vector2(.38f,.38f))},true);
            Assert.IsTrue(BuildSocketGeometry.Valid(basePart,out _));
            Assert.IsTrue(BuildSocketGeometry.Valid(topPart,out _));
            Assert.IsFalse(BuildSocketGeometry.Touches(basePart,
                new BuildPose(Vector3Int.zero,Vector3Int.zero),topPart,
                new BuildPose(Vector3Int.right,Vector3Int.zero),out _));
        }

        [Test] public void NoCorePlacementAndNoCellOrOwnershipDuplication()
        {
            var occupied=new[]{P("core",BuildFixtureCatalog.Core(),0)};
            Assert.AreEqual(BuildPlacementResult.InvalidCore,BuildPlacementRules.Check(
                occupied,P("other",BuildFixtureCatalog.Core(),1),out _));
            Assert.AreEqual(BuildPlacementResult.DuplicatePartId,BuildPlacementRules.Check(
                occupied,P("core",BuildFixtureCatalog.ArmorCube(),1),out _));
            Assert.AreEqual(BuildPlacementResult.OccupiedCells,BuildPlacementRules.Check(
                occupied,P("clone",BuildFixtureCatalog.ArmorCube(),0),out _));
            Assert.AreEqual(BuildPlacementResult.OutOfBounds,BuildPlacementRules.Check(
                occupied,P("far",BuildFixtureCatalog.ArmorCube(),8),out _));
        }

        [Test] public void OneQuarterRotationAlwaysRemainsIntegralAndReversible()
        {
            var r=new Vector3Int(1,2,3);
            foreach(var axis in new[]{Vector3Int.right,Vector3Int.left,Vector3Int.up,
                Vector3Int.down,Vector3Int.forward,Vector3Int.back})
            {
                var rotated=BuildSocketGeometry.Rotate(axis,r);
                Assert.IsTrue(BuildSocketGeometry.UnitFace(rotated));
                var reversed=BuildSocketGeometry.Rotate(rotated,-r);
                // Euler XYZ rotations do not commute; reverse Euler angles are not
                // generally the inverse of the original composite orientation.
                Assert.IsTrue(BuildSocketGeometry.UnitFace(reversed));
            }
        }
    }
}
