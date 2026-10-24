using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.BUILD001.Tests
{
    public sealed class Build001PhysicalHitTests
    {
        GameObject core;
        GameObject wheel;

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (core != null) Object.Destroy(core);
            if (wheel != null) Object.Destroy(wheel);
            yield return null;
        }

        [UnityTest] public IEnumerator RealCubeColliderFaceIsSelectedButWheelTreadIsNot()
        {
            core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name = "AuthoritativeCore";
            core.transform.localScale = Vector3.one * .52f;
            core.transform.position = new Vector3(0f, 1f, 0f);
            wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "WheelVisualNotStructural";
            wheel.transform.position = core.transform.position +
                Vector3.right * BuildSocketGeometry.GridMetres;
            wheel.transform.localScale = new Vector3(.3f, .26f, .3f);
            // Hide the nearby wheel for this isolated core hit; otherwise the
            // deliberately adjacent tire collider intercepts the ray first.
            wheel.SetActive(false);
            Physics.SyncTransforms();

            var forwardRay = new Ray(core.transform.position +
                new Vector3(1f, 0f, 0f), Vector3.left);
            Assert.IsTrue(Physics.Raycast(forwardRay, out RaycastHit hit, .75f));
            Assert.AreSame(core.GetComponent<Collider>(),hit.collider);
            var response = BuildSocketRaycast.ValidateHit(hit,core.transform,
                BuildFixtureCatalog.Core(),new BuildPose(Vector3Int.zero,Vector3Int.zero),
                core.transform.position,out var face);
            Assert.AreEqual(BuildHitResult.Valid,response);
            Assert.AreEqual(Vector3Int.right,face);

            wheel.SetActive(true);
            Physics.SyncTransforms();
            var tireRay = new Ray(wheel.transform.position + Vector3.up,
                Vector3.down);
            Assert.IsTrue(Physics.Raycast(tireRay,out var tireHit,2f));
            response = BuildSocketRaycast.ValidateHit(tireHit,wheel.transform,
                BuildFixtureCatalog.Wheel(),new BuildPose(Vector3Int.right,Vector3Int.zero),
                core.transform.position,out face);
            Assert.AreEqual(BuildHitResult.DecorativeSurface,response);
            Assert.AreEqual(Vector3Int.zero,face);
            Assert.IsTrue(wheel.GetComponent<Collider>().enabled);
            yield return null;
        }

        [UnityTest] public IEnumerator TopFaceCanSelectStructuralSocketWithoutCreatingArtOwnedPorts()
        {
            core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.transform.localScale = Vector3.one * .52f;
            core.transform.position = Vector3.up;
            Physics.SyncTransforms();
            var ray = new Ray(core.transform.position + Vector3.up,Vector3.down);
            Assert.IsTrue(Physics.Raycast(ray,out var hit,2f));
            var result=BuildSocketRaycast.ValidateHit(hit,core.transform,
                BuildFixtureCatalog.ArmorCube(),new BuildPose(Vector3Int.zero,Vector3Int.zero),
                core.transform.position,out var face);
            Assert.AreEqual(BuildHitResult.Valid,result);
            Assert.AreEqual(Vector3Int.up,face);
            yield return null;
        }
    }
}
