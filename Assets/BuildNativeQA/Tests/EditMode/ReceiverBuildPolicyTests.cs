using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace B01.BUILD001.Tests.ReceiverQA
{
    // Receiver requirements, not producer assertions. Pure policy APIs run in native Unity.
    public sealed class ReceiverBuildPolicyTests
    {
        static BuildInstalledPart Part(string id,BuildDefinition definition,Vector3Int cell,Vector3Int turns=default)
            =>new BuildInstalledPart(id,definition,new BuildPose(cell,turns));

        [TestCase("wheel",1,0,0,1,0,0)]
        [TestCase("wheel",2,0,0,1,0,0)]
        [TestCase("wheel",3,0,0,1,0,0)]
        [TestCase("wheel",0,1,0,0,0,-1)]
        [TestCase("wheel",0,2,0,-1,0,0)]
        [TestCase("wheel",0,3,0,0,0,1)]
        [TestCase("wheel",0,0,1,0,1,0)]
        [TestCase("wheel",0,0,2,-1,0,0)]
        [TestCase("wheel",0,0,3,0,-1,0)]
        [TestCase("gun",1,0,0,0,0,1)]
        [TestCase("gun",2,0,0,0,-1,0)]
        [TestCase("gun",3,0,0,0,0,-1)]
        [TestCase("gun",0,1,0,0,1,0)]
        [TestCase("gun",0,2,0,0,1,0)]
        [TestCase("gun",0,3,0,0,1,0)]
        [TestCase("gun",0,0,1,-1,0,0)]
        [TestCase("gun",0,0,2,0,-1,0)]
        [TestCase("gun",0,0,3,1,0,0)]
        public void TypedMountAtQuarterTurns(string kind,int rx,int ry,int rz,int dx,int dy,int dz)
        {
            var origin=new Vector3Int(0,2,0);
            var core=Part("authorizedCore",BuildFixtureCatalog.Core(),origin);
            var candidate=Part("candidate",kind=="wheel"?BuildFixtureCatalog.Wheel():BuildFixtureCatalog.Gun(),origin+new Vector3Int(dx,dy,dz),new Vector3Int(rx,ry,rz));
            var result=BuildPlacementRules.Check(new[]{core},candidate,out var owner);
            TestContext.WriteLine($"POSITIVE typed={kind} turns={rx},{ry},{rz} anchor={candidate.Pose.Anchor} result={result} support={owner}");
            Assert.AreEqual(BuildPlacementResult.Valid,result);Assert.AreEqual("authorizedCore",owner);
        }

        [Test] public void ArmorUsesStructuralFace()
        {
            var core=Part("core",BuildFixtureCatalog.Core(),Vector3Int.zero);
            Assert.AreEqual(BuildPlacementResult.Valid,BuildPlacementRules.Check(new[]{core},Part("armor",BuildFixtureCatalog.ArmorCube(),Vector3Int.right),out _));
        }

        [Test] public void FourCellModuleKeepsSingleOwnerAndMounts()
        {
            var core=Part("core",BuildFixtureCatalog.Core(),Vector3Int.left);
            var wide=Part("wide",BuildFixtureCatalog.Wide2x2(),Vector3Int.zero);
            Assert.AreEqual(4,new HashSet<Vector3Int>(BuildSocketGeometry.Occupied(wide.Definition,wide.Pose)).Count);
            Assert.AreEqual(BuildPlacementResult.Valid,BuildPlacementRules.Check(new[]{core},wide,out _));
        }

        [Test] public void RemovingAndRebuildingBridgeRestoresReachability()
        {
            var core=Part("core",BuildFixtureCatalog.Core(),Vector3Int.zero);
            var bridge=Part("bridge",BuildFixtureCatalog.ArmorCube(),Vector3Int.right);
            var tail=Part("tail",BuildFixtureCatalog.ArmorCube(),Vector3Int.right*2);
            var installed=new List<BuildInstalledPart>{core,tail};
            Assert.IsFalse(BuildPlacementRules.Reachable(installed).Contains("tail"));
            Assert.AreEqual(BuildPlacementResult.Valid,BuildPlacementRules.Check(installed,bridge,out _));
            installed.Add(bridge);Assert.IsTrue(BuildPlacementRules.Reachable(installed).Contains("tail"));
            Assert.AreEqual(BuildPlacementResult.Valid,BuildPlacementRules.Check(installed,Part("new",BuildFixtureCatalog.ArmorCube(),Vector3Int.right*3),out var support));
            Assert.AreEqual("tail",support);
        }

        [Test] public void BUILD_W9_02_UnauthorizedSecondCoreCannotSeedASeparateIsland()
        {
            int rejected=0;
            for(int attempt=1;attempt<=2;attempt++)
            {
                var first=Part("authorizedCore",BuildFixtureCatalog.Core(),Vector3Int.zero);
                var other=Part("untrustedLoadedCore",BuildFixtureCatalog.Core(),Vector3Int.right*4);
                Assert.AreEqual(BuildPlacementResult.InvalidCore,BuildPlacementRules.Check(new[]{first},other,out _),"Direct second-Core placement control");
                var loaded=new[]{first,other};
                var result=BuildPlacementRules.Check(loaded,Part("candidate",BuildFixtureCatalog.ArmorCube(),Vector3Int.right*5),out var owner);
                bool secondReachable=BuildPlacementRules.Reachable(loaded).Contains(other.PartId);
                TestContext.WriteLine($"BUILD-W9-02 attempt={attempt} result={result} support={owner} secondRootReachable={secondReachable} expected=reject untrusted second root");
                if(result!=BuildPlacementResult.Valid)rejected++;
            }
            Assert.AreEqual(2,rejected,"BUILD-W9-02: every loaded island must be rooted in the one authorized Core; direct-placement rejection alone is insufficient");
        }
    }
}
