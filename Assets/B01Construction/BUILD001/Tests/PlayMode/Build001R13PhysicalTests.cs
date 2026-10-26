using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.BUILD001.Tests
{
    public sealed class Build001R13PhysicalTests
    {
        private GameObject core, candidate, decorative;

        private static BuildInstalledPart Part(string id, BuildDefinition definition, int x) =>
            new BuildInstalledPart(id, definition,
                new BuildPose(new Vector3Int(x,0,0),Vector3Int.zero));

        private GameObject Cube(string name, Vector3 position, Vector3 size)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name=name;
            go.transform.position=position;
            go.transform.localScale=size;
            return go;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (decorative != null) Object.Destroy(decorative);
            if (candidate != null) Object.Destroy(candidate);
            if (core != null) Object.Destroy(core);
            yield return null;
        }

        [UnityTest] public IEnumerator W9_01_DecorativeChildRayCannotBecomeStructuralSocket()
        {
            core=Cube("core",Vector3.up,Vector3.one*.52f);
            var definition=BuildFixtureCatalog.Core();
            var rootCollider=core.GetComponent<Collider>();
            var marker=core.AddComponent<BuildMountSurface>();
            marker.PartInstanceId="core";
            marker.SocketIndex=0; // explicit +X mount of Core
            var registry=new BuildSocketRegistry();
            Assert.IsTrue(registry.Register(rootCollider,core.transform,"core",definition,0));

            decorative=new GameObject("visualOnlyChild");
            decorative.transform.SetParent(core.transform,false);
            decorative.transform.localPosition=new Vector3(.31f/.52f,0f,0f);
            var visual=decorative.AddComponent<BoxCollider>();
            visual.size=new Vector3(.02f/.52f,.15f/.52f,.15f/.52f);
            Physics.SyncTransforms();

            var ray=new Ray(core.transform.position+Vector3.right,Vector3.left);
            Assert.IsTrue(Physics.Raycast(ray,out var hit,1f));
            Assert.AreSame(visual,hit.collider);
            Assert.That(hit.point.x,Is.EqualTo(.32f).Within(.02f));
            Assert.IsFalse(registry.Register(visual,core.transform,"core",definition,0));
            Assert.AreEqual(BuildHitResult.DecorativeSurface,
                BuildSocketRaycast.ValidateHit(hit,core.transform,"core",definition,
                    new BuildPose(Vector3Int.zero,Vector3Int.zero),core.transform.position,
                    registry,out var wrongFace));
            Assert.AreEqual(Vector3Int.zero,wrongFace);
            // Old fixture overload also must never promote child collider ancestry.
            Assert.AreEqual(BuildHitResult.DecorativeSurface,
                BuildSocketRaycast.ValidateHit(hit,core.transform,definition,
                    new BuildPose(Vector3Int.zero,Vector3Int.zero),core.transform.position,
                    out _));

            decorative.SetActive(false);
            Physics.SyncTransforms();
            Assert.IsTrue(Physics.Raycast(ray,out hit,1f));
            Assert.AreSame(rootCollider,hit.collider);
            Assert.AreEqual(BuildHitResult.Valid,
                BuildSocketRaycast.ValidateHit(hit,core.transform,"core",definition,
                    new BuildPose(Vector3Int.zero,Vector3Int.zero),core.transform.position,
                    registry,out var authorizedFace));
            Assert.AreEqual(Vector3Int.right,authorizedFace);
            yield return null;
        }

        [UnityTest] public IEnumerator W9_04_ActualPhysXPenetrationRejectsLargePhysicalPart()
        {
            core=Cube("core",Vector3.up,Vector3.one*.52f);
            candidate=Cube("oversize",Vector3.up+Vector3.right*.55f,
                Vector3.one*1.2f);
            Physics.SyncTransforms();
            var pair=new BuildPhysicsEvidence(
                new[]{candidate.GetComponent<Collider>()},
                new[]{core.GetComponent<Collider>()},true);
            Assert.IsTrue(Physics.ComputePenetration(candidate.GetComponent<Collider>(),
                candidate.transform.position,candidate.transform.rotation,
                core.GetComponent<Collider>(),core.transform.position,core.transform.rotation,
                out _,out float depth));
            Assert.That(depth,Is.EqualTo(.31f).Within(.002f));
            Assert.AreEqual(BuildPlacementResult.ColliderPenetration,
                BuildPlacementRules.CheckForInstallation(
                    new[]{Part("core",BuildFixtureCatalog.Core(),0)},
                    Part("oversize",BuildFixtureCatalog.ArmorCube(),1),
                    pair,out var support));
            Assert.IsNull(support);
            yield return null;
        }

        [UnityTest] public IEnumerator W9_04_GenuineNonOverlappingBodiesCanCommit()
        {
            core=Cube("core",Vector3.up,Vector3.one*.52f);
            candidate=Cube("newArmor",Vector3.up+Vector3.right*.55f,
                Vector3.one*.52f);
            Physics.SyncTransforms();
            var pair=new BuildPhysicsEvidence(
                new[]{candidate.GetComponent<Collider>()},
                new[]{core.GetComponent<Collider>()},true);
            Assert.AreEqual(BuildPlacementResult.Valid,
                BuildPlacementRules.CheckForInstallation(
                    new[]{Part("core",BuildFixtureCatalog.Core(),0)},
                    Part("newArmor",BuildFixtureCatalog.ArmorCube(),1),
                    pair,out var support));
            Assert.AreEqual("core",support);
            pair=new BuildPhysicsEvidence(
                new[]{candidate.GetComponent<Collider>()},
                new[]{core.GetComponent<Collider>()},false);
            Assert.AreEqual(BuildPlacementResult.PhysicsUnverified,
                BuildPlacementRules.CheckForInstallation(
                    new[]{Part("core",BuildFixtureCatalog.Core(),0)},
                    Part("newArmor",BuildFixtureCatalog.ArmorCube(),1),
                    pair,out _));
            yield return null;
        }

        [UnityTest] public IEnumerator DemoURPShaderIsIncludedAndSupported()
        {
            var shader=Resources.Load<Shader>("BUILD001_PreviewURP");
            Assert.IsNotNull(shader,"Source Resources shader must survive Player stripping.");
            Assert.IsTrue(shader.isSupported,"Requires Unity 6 URP pipeline.");
            var preview=new Material(shader);
            preview.SetColor("_BaseColor",Color.green);
            Assert.AreEqual(shader,preview.shader);
            Object.Destroy(preview);
            yield return null;
        }
    }
}
