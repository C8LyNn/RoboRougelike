using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.BUILD001.Tests.ReceiverQA
{
    public sealed class ReceiverBuildPhysicalTests
    {
        GameObject root;
        static BuildInstalledPart Part(string id,BuildDefinition def,Vector3Int cell)=>new BuildInstalledPart(id,def,new BuildPose(cell,Vector3Int.zero));
        GameObject Box(string name,Vector3 position,Vector3 size)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root.transform,false);go.transform.position=position;go.transform.localScale=size;return go;
        }
        [UnityTearDown] public IEnumerator Cleanup(){if(root!=null)Object.Destroy(root);yield return null;}

        [UnityTest] public IEnumerator BUILD_W9_01_DecorativeChildCannotAuthorizeAContactFace()
        {
            int rejected=0;
            for(int attempt=1;attempt<=2;attempt++)
            {
                root=new GameObject("QA authoritative Core owner");root.transform.position=Vector3.up;
                var structural=Box("Authorized structural box",Vector3.up,Vector3.one*.52f);
                var decoration=Box("Decorative child collider",Vector3.up+Vector3.right*.29f,new Vector3(.04f,.2f,.2f));
                Physics.SyncTransforms();yield return null;
                Assert.IsTrue(Physics.Raycast(new Ray(Vector3.up+new Vector3(1,.18f,0),Vector3.left),out var control,2));
                Assert.AreSame(structural.GetComponent<Collider>(),control.collider);
                Assert.AreEqual(BuildHitResult.Valid,BuildSocketRaycast.ValidateHit(control,root.transform,BuildFixtureCatalog.Core(),new BuildPose(Vector3Int.zero,Vector3Int.zero),Vector3.up,out _));
                Assert.IsTrue(Physics.Raycast(new Ray(Vector3.up+Vector3.right,Vector3.left),out var hit,2));
                Assert.AreSame(decoration.GetComponent<Collider>(),hit.collider);
                var actual=BuildSocketRaycast.ValidateHit(hit,root.transform,BuildFixtureCatalog.Core(),new BuildPose(Vector3Int.zero,Vector3Int.zero),Vector3.up,out var face);
                TestContext.WriteLine($"BUILD-W9-01 attempt={attempt} hit={hit.collider.name} point={hit.point} normal={hit.normal} result={actual} face={face} expected=reject decorative child");
                if(actual!=BuildHitResult.Valid)rejected++;
                Object.Destroy(root);yield return null;
            }
            Assert.AreEqual(2,rejected,"BUILD-W9-01: a child collider near a socket plane is not itself an authorized mount collider");
        }

        [UnityTest] public IEnumerator BUILD_W9_03_DisconnectedCellsCannotRelaySupportAcrossEmptySpace()
        {
            int rejected=0;
            for(int attempt=1;attempt<=2;attempt++)
            {
                root=new GameObject("QA disconnected material islands");
                Box("Core",new Vector3(-.55f,1,0),Vector3.one*.52f);
                Box("Part island A",Vector3.up,Vector3.one*.52f);
                Box("Part island B",new Vector3(1.1f,1,0),Vector3.one*.52f);
                Physics.SyncTransforms();yield return null;
                bool gapOccupied=Physics.OverlapBox(new Vector3(.55f,1,0),Vector3.one*.2f).Any(c=>c.transform.IsChildOf(root.transform));
                Assert.IsFalse(gapOccupied,"The test must contain a real empty collider gap");
                var cells=new[]{Vector3Int.zero,Vector3Int.right*2};
                var sockets=new[]{new BuildSocket(cells[0],Vector3Int.left,BuildSocketKind.Structural,new Vector2(.48f,.48f)),new BuildSocket(cells[1],Vector3Int.right,BuildSocketKind.Structural,new Vector2(.48f,.48f))};
                var islands=new BuildDefinition("Disconnected two-island part",BuildRole.Device,cells,sockets,true);
                bool definitionValid=BuildSocketGeometry.Valid(islands,out var reason);
                var core=Part("core",BuildFixtureCatalog.Core(),Vector3Int.left);var bridge=Part("islands",islands,Vector3Int.zero);
                var attached=BuildPlacementRules.Check(new[]{core},bridge,out _);
                var downstream=BuildPlacementRules.Check(new[]{core,bridge},Part("tail",BuildFixtureCatalog.ArmorCube(),Vector3Int.right*3),out var owner);
                TestContext.WriteLine($"BUILD-W9-03 attempt={attempt} emptyPhysicalGap={!gapOccupied} definitionValid={definitionValid} reason={reason} bridge={attached} downstream={downstream} owner={owner} expected=no unsupported relay across empty cell");
                if(attached!=BuildPlacementResult.Valid||downstream!=BuildPlacementResult.Valid)rejected++;
                Object.Destroy(root);yield return null;
            }
            Assert.AreEqual(2,rejected,"BUILD-W9-03: a single owner ID cannot turn disconnected physical islands into a coherent support bridge");
        }

        [UnityTest] public IEnumerator BUILD_W9_04_OversizedColliderMustNotPassPlacementIntoAnOccupiedVolume()
        {
            int safelyHandled=0;
            for(int attempt=1;attempt<=2;attempt++)
            {
                root=new GameObject("QA physical envelope versus grid");
                var core=Box("Core",Vector3.up,Vector3.one*.52f).GetComponent<Collider>();
                var oversized=Box("One-cell declared but oversized collider",new Vector3(.55f,1,0),new Vector3(1.2f,.52f,.52f)).GetComponent<Collider>();
                Physics.SyncTransforms();yield return null;
                bool penetration=Physics.ComputePenetration(core,core.transform.position,core.transform.rotation,oversized,oversized.transform.position,oversized.transform.rotation,out var direction,out float distance);
                Assert.IsTrue(penetration,"Actual Physics.ComputePenetration must prove the oversized-collider overlap");
                var actual=BuildPlacementRules.Check(new[]{Part("core",BuildFixtureCatalog.Core(),Vector3Int.zero)},Part("armor",BuildFixtureCatalog.ArmorCube(),Vector3Int.right),out var support);
                TestContext.WriteLine($"BUILD-W9-04 attempt={attempt} result={actual} support={support} physicalOverlap={penetration} depth={distance:F4} direction={direction} declaredCells=1 actualSize=(1.2,.52,.52) expected=reject physical collision");
                if(actual!=BuildPlacementResult.Valid)safelyHandled++;
                Object.Destroy(root);yield return null;
            }
            Assert.AreEqual(2,safelyHandled,"BUILD-W9-04: logical non-overlap alone cannot authorize colliding physical envelopes; boundary ownership needs a design ruling");
        }
    }
}
