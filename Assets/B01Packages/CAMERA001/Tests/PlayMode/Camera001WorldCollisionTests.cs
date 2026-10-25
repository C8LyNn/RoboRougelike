using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.CAMERA001.Tests
{
    public sealed class Camera001WorldCollisionTests
    {
        private GameObject root, body, cameraGo, wall, wallOther;

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
            if (wallOther != null) Object.Destroy(wallOther);
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
        // CAM-01..08: each rig/resolution is a separately named Unity test,
        // measuring ACTUAL final-frame pose, not LastSolution.DesiredCameraWorld.
        [UnityTest] public IEnumerator CAM01_Light_720_FinalPose() => CheckFinalPose("Light", 1280, 720);
        [UnityTest] public IEnumerator CAM02_Light_1080_FinalPose() => CheckFinalPose("Light", 1920, 1080);
        [UnityTest] public IEnumerator CAM03_Heavy_720_FinalPose() => CheckFinalPose("Heavy", 1280, 720);
        [UnityTest] public IEnumerator CAM04_Heavy_1080_FinalPose() => CheckFinalPose("Heavy", 1920, 1080);
        [UnityTest] public IEnumerator CAM05_Tall_720_FinalPose() => CheckFinalPose("Tall", 1280, 720);
        [UnityTest] public IEnumerator CAM06_Tall_1080_FinalPose() => CheckFinalPose("Tall", 1920, 1080);
        [UnityTest] public IEnumerator CAM07_Wide_720_FinalPose() => CheckFinalPose("Wide", 1280, 720);
        [UnityTest] public IEnumerator CAM08_Wide_1080_FinalPose() => CheckFinalPose("Wide", 1920, 1080);

        private IEnumerator CheckFinalPose(string model, int width, int height)
        {
            ConfigureModel(model);
            var camera = cameraGo.GetComponent<Camera>();
            camera.aspect = (float)width / height;
            camera.fieldOfView = 62f;
            var rig = cameraGo.GetComponent<Camera001Rig>();
            rig.ResetSmoothing(); // explicit rebuild, not each orbit frame
            rig.SetOrbit(0f, -8f, 8f);
            rig.StepCamera(0f);
            // Reproduce the original pitch transition and continue 360deg.
            for (int frame = 0; frame < 242; frame++)
            {
                rig.SetOrbit(frame * (360f / 240f), 18f, 8f);
                rig.StepCamera(1f / 60f);
                Physics.SyncTransforms();
                Assert.IsFalse(rig.ActualOwnHullIntersectsCrosshair,
                    model + " " + width + " frame=" + frame + " actual ray hits own hull");
                Assert.LessOrEqual(rig.LastAimErrorDegrees, .05f,
                    model + " " + width + " frame=" + frame + " final aim drift");
                Assert.LessOrEqual(rig.LastCameraStepMeters, .2501f,
                    "Camera travel exceeded 15m/s continuous movement");
                var ray = new Ray(camera.transform.position, camera.transform.forward);
                RaycastHit ownHit;
                Assert.IsFalse(body.GetComponent<Collider>().Raycast(ray, out ownHit, 200f),
                    "Actual rendered ray hit actual vehicle collider");
                Assert.IsFalse(rig.WorldOverlapAtCamera,
                    "World collision at final rendered pose");
            }
            yield return null;
        }

        [UnityTest] public IEnumerator CAM09_ContinuousCorridor_NoPivotSnapOrWallPenetration()
        {
            ConfigureModel("Tall");
            var rig = cameraGo.GetComponent<Camera001Rig>();
            rig.SetOrbit(0f, 18f, 8f);
            rig.StepCamera(0f);
            float wallX = body.GetComponent<Collider>().bounds.extents.x + .7f;
            float height = body.GetComponent<Collider>().bounds.size.y + 5f;
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallOther = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(wallX, height * .5f, 0f);
            wallOther.transform.position = new Vector3(-wallX, height * .5f, 0f);
            wall.transform.localScale = new Vector3(.4f, height, 64f);
            wallOther.transform.localScale = new Vector3(.4f, height, 64f);
            Physics.SyncTransforms();
            // A normal camera update cannot jump from an obstructed pivot.
            for (int i = 0; i <= 70; i++)
            {
                rig.SetOrbit(180f * i / 70f, 18f, 8f);
                rig.StepCamera(1f / 60f);
                Assert.LessOrEqual(rig.LastCameraStepMeters, .2501f,
                    "CAM-R11-02 corridor snap at sample " + i);
                Assert.IsFalse(rig.WorldOverlapAtCamera,
                    "CAM-R11-02 actual near-plane sphere contacts world wall at " + i);
                Assert.IsFalse(rig.ActualOwnHullIntersectsCrosshair,
                    "Corridor final reticle hits own collider");
            }
            yield return null;
        }

        [UnityTest] public IEnumerator CAM10_RemovedPart_RecalculatesActualAimWithoutMouseInput()
        {
            var extra = GameObject.CreatePrimitive(PrimitiveType.Cube);
            extra.transform.SetParent(root.transform, false);
            extra.transform.localPosition = new Vector3(0f, 7f, 0f);
            extra.transform.localScale = new Vector3(1f, 7f, 1f);
            Physics.SyncTransforms();
            var rig = cameraGo.GetComponent<Camera001Rig>();
            rig.SetOrbit(33f, 18f, 8f);
            rig.StepCamera(0f);
            float oldAimHeight = rig.LastSolution.AimWorld.y;
            extra.SetActive(false); // simulates immediate detachment of collider
            Physics.SyncTransforms();
            rig.StepCamera(1f / 60f);
            Assert.Less(rig.LastSolution.AimWorld.y, oldAimHeight);
            Assert.IsFalse(rig.ActualOwnHullIntersectsCrosshair);
            Assert.LessOrEqual(rig.LastAimErrorDegrees, .05f);
            Assert.LessOrEqual(rig.LastCameraStepMeters, .2501f);
            Object.Destroy(extra);
            yield return null;
        }

        private void ConfigureModel(string model)
        {
            Vector3 size = model == "Light" ? new Vector3(2.98f, 1.56f, 2.72f) :
                model == "Heavy" ? new Vector3(5.18f, 2.12f, 4.92f) :
                model == "Tall" ? new Vector3(3.8f, 9.16f, 4.92f) :
                new Vector3(10.42f, 2.12f, 4.92f);
            body.transform.position = new Vector3(0f, size.y * .5f, 0f);
            body.transform.localScale = size;
            Physics.SyncTransforms();
        }
    }
}
