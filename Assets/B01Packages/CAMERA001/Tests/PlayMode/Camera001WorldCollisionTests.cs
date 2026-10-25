using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.CAMERA001.Tests
{
    public sealed class Camera001WorldCollisionTests
    {
        private GameObject root, body, cameraGo, wall;

        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("CAMERA001_PLAY_TEST_ROOT");
            body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(root.transform, false);
            body.transform.position = new Vector3(0, 2, 0);
            body.transform.localScale = new Vector3(4.5f, 4, 5.8f);
            cameraGo = new GameObject("CAMERA001_TEST_CAMERA");
            cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<Camera001Rig>().Configure(root.transform, cameraGo.GetComponent<Camera>());
            Physics.SyncTransforms();
            yield return null;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (wall != null) Object.Destroy(wall);
            if (cameraGo != null) Object.Destroy(cameraGo);
            if (root != null) Object.Destroy(root);
            yield return null;
        }

        [UnityTest] public IEnumerator IgnoresOwnCollidersWithoutDisablingThem()
        {
            var rig = cameraGo.GetComponent<Camera001Rig>();
            rig.SetOrbit(0, 18, 8);
            rig.StepCamera(0f);
            Assert.IsFalse(rig.LastSolution.OwnHullIntersectsCrosshair);
            Assert.IsFalse(rig.WorldClipped);
            Assert.IsTrue(body.GetComponent<Collider>().enabled);
            yield return null;
        }

        [UnityTest] public IEnumerator WorldObstacleShortensCameraSpringArm()
        {
            var rig = cameraGo.GetComponent<Camera001Rig>();
            rig.SetOrbit(0, 20, 12);
            rig.StepCamera(0f);
            var solution = rig.LastSolution;
            Vector3 aim = solution.AimWorld;
            Vector3 back = (solution.DesiredCameraWorld - aim).normalized;
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = aim + back * 5f;
            wall.transform.localScale = new Vector3(15, 15, .6f);
            Physics.SyncTransforms();
            rig.StepCamera(0f);
            Assert.IsTrue(rig.WorldClipped, "World occlusion must not be passed as own vehicle geometry");
            Assert.Less(Vector3.Distance(aim, cameraGo.transform.position), solution.Distance);
            Assert.IsTrue(wall.GetComponent<Collider>().enabled);
            yield return null;
        }

        [UnityTest] public IEnumerator HeavyToLightSwitchClearsPreviousSmoothing()
        {
            var rig = cameraGo.GetComponent<Camera001Rig>();
            rig.SetOrbit(73, 25, 7);
            rig.StepCamera(0f);
            var before = rig.LastSolution.FitDistance;
            body.transform.localScale = new Vector3(2.1f, 1.5f, 3.2f);
            Physics.SyncTransforms();
            rig.ResetSmoothing();
            rig.StepCamera(0f);
            Assert.Less(rig.LastSolution.FitDistance, before);
            Assert.Less(Vector3.Distance(cameraGo.transform.position,
                rig.LastSolution.DesiredCameraWorld), .001f);
            yield return null;
        }
    }
}
