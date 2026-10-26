using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.CAMERA001.Tests
{
    /// <summary>Real PhysX collider ray fixture, NOT canonical R3/P12 turret test.</summary>
    public sealed class Camera001AimTimelinePhysicsTests
    {
        [UnityTest] public IEnumerator NativeRayChangesEnemyToFloorAndRecordsSlewBudget()
        {
            GameObject enemy = null, floor = null, muzzle = null, camera = null;
            try
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "CAMERA001_W13_REAL_FLOOR";
                floor.transform.position = new Vector3(0f, -.5f, 30f);
                floor.transform.localScale = new Vector3(100f, 1f, 160f);
                enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                enemy.name = "CAMERA001_W13_REAL_ENEMY";
                enemy.transform.position = new Vector3(0f, 1f, 30f);
                enemy.transform.localScale = new Vector3(2f, 2f, 2f);
                muzzle = new GameObject("CAMERA001_W13_MEASURED_MUZZLE");
                muzzle.transform.position = new Vector3(0f, 1.5552f, 27.8219f);
                camera = new GameObject("CAMERA001_W13_RAY_CAMERA");
                camera.transform.position = new Vector3(0f, 6f, 20f);
                camera.transform.rotation = Quaternion.LookRotation(
                    new Vector3(0f, 1f, 30f) - camera.transform.position);
                Physics.SyncTransforms();

                var ray = new Ray(camera.transform.position, camera.transform.forward);
                RaycastHit beforeHit;
                Assert.IsTrue(Physics.Raycast(ray, out beforeHit, 100f, ~0,
                    QueryTriggerInteraction.Ignore));
                Assert.AreEqual(enemy.GetComponent<Collider>(), beforeHit.collider,
                    "Must actually first hit enemy collider in Unity PhysX");

                var timeline = new Camera001AimTimeline();
                Vector3 firstDirection = (beforeHit.point - muzzle.transform.position).normalized;
                muzzle.transform.rotation = Quaternion.LookRotation(firstDirection);
                var first = new Camera001AimTimeline.Frame {
                    FrameIndex = 100,
                    HitColliderInstanceId = beforeHit.collider.GetInstanceID(),
                    HitKind = "EnemyBox",
                    Sample = new Camera001AimTransitionDiagnostics.Sample {
                        CurrentTargetWorld = beforeHit.point,
                        CurrentMuzzleWorld = muzzle.transform.position,
                        CurrentMuzzleForwardWorld = muzzle.transform.forward,
                        DeltaTimeSeconds = 1f / 60f,
                        TurretSlewDegreesPerSecond = 170f
                    }
                };
                timeline.Capture(first);
                enemy.SetActive(false);
                Physics.SyncTransforms();
                RaycastHit afterHit;
                Assert.IsTrue(Physics.Raycast(ray, out afterHit, 100f, ~0,
                    QueryTriggerInteraction.Ignore));
                Assert.AreEqual(floor.GetComponent<Collider>(), afterHit.collider,
                    "Same camera ray must now hit physical floor, not a synthetic point");

                Vector3 desired = (afterHit.point - muzzle.transform.position).normalized;
                Vector3 slewed = Vector3.RotateTowards(
                    muzzle.transform.forward, desired, 170f * Mathf.Deg2Rad / 60f, 0f);
                muzzle.transform.rotation = Quaternion.LookRotation(slewed);
                var next = new Camera001AimTimeline.Frame {
                    FrameIndex = 101,
                    HitColliderInstanceId = afterHit.collider.GetInstanceID(),
                    HitKind = "FloorCollider",
                    Sample = new Camera001AimTransitionDiagnostics.Sample {
                        CurrentTargetWorld = afterHit.point,
                        CurrentMuzzleWorld = muzzle.transform.position,
                        CurrentMuzzleForwardWorld = muzzle.transform.forward,
                        DeltaTimeSeconds = 1f / 60f,
                        TurretSlewDegreesPerSecond = 170f
                    }
                };
                var second = timeline.Capture(next);
                Assert.IsTrue(second.FirstHitChanged);
                Assert.AreEqual(1, timeline.ChangedFirstHitCount);
                Assert.IsTrue(second.Observation.TargetChangeExceedsOneFrameTurretBudget);
                Assert.Greater(second.Observation.CurrentMuzzleToTargetErrorDegrees, 1f);
                Assert.IsFalse(second.Observation.CameraFinalPoseUnsafe);
                Assert.LessOrEqual(second.Observation.ObservedMuzzleDirectionChangeDegrees,
                    170f / 60f + .051f);
                Assert.IsFalse(second.Observation.MuzzleWorldStepExceedsTurretBudget);
                StringAssert.Contains("FloorCollider", timeline.ToCsv());
            }
            finally
            {
                if (enemy != null) Object.Destroy(enemy);
                if (floor != null) Object.Destroy(floor);
                if (muzzle != null) Object.Destroy(muzzle);
                if (camera != null) Object.Destroy(camera);
            }
            yield return null;
        }

        [UnityTest] public IEnumerator RealCameraFinalPoseFlagOutranksTargetRetargetClassification()
        {
            GameObject root = null, body = null, camera = null;
            try
            {
                root = new GameObject("CAMERA001_W13_OWN_HULL");
                body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = new Vector3(0f, .8f, 0f);
                body.transform.localScale = new Vector3(2.98f, 1.56f, 2.72f);
                camera = new GameObject("CAMERA001_W13_CAMERA_RIG");
                var view = camera.AddComponent<Camera>();
                view.fieldOfView = 62f;
                view.aspect = 16f / 9f;
                var rig = camera.AddComponent<Camera001Rig>();
                rig.enabled = false;
                rig.Configure(root.transform, view);
                rig.SetOrbit(0f, 18f, 8f);
                Physics.SyncTransforms();
                rig.StepCamera(0f);
                Assert.IsFalse(rig.WorldOverlapAtCamera);
                Assert.IsFalse(rig.ActualOwnHullIntersectsCrosshair);

                var observer = new Camera001AimTimeline();
                var aim = rig.LastSolution.AimWorld;
                var muzzle = root.transform.position + Vector3.up;
                var forward = (aim - muzzle).normalized;
                var f = new Camera001AimTimeline.Frame {
                    FrameIndex = 1,
                    HitColliderInstanceId = 0,
                    HitKind = "NoHit",
                    Sample = new Camera001AimTransitionDiagnostics.Sample {
                        CurrentTargetWorld = aim,
                        CurrentMuzzleWorld = muzzle,
                        CurrentMuzzleForwardWorld = forward,
                        DeltaTimeSeconds = 1f / 60f,
                        TurretSlewDegreesPerSecond = 170f,
                        CameraFinalAimErrorDegrees = rig.LastAimErrorDegrees,
                        CameraOwnHullRayHit = rig.ActualOwnHullIntersectsCrosshair,
                        CameraWorldOverlap = rig.WorldOverlapAtCamera
                    }
                };
                var good = observer.Capture(f);
                Assert.AreEqual("ALIGNED", good.Observation.Classification);
                // Inject an ACTUAL (test-owned) camera pose safety failure rather
                // than changing a threshold: this verifies fail-closed routing.
                f.FrameIndex = 2;
                f.Sample.CameraOwnHullRayHit = true;
                var bad = observer.Capture(f);
                Assert.AreEqual("CAMERA_FINAL_POSE_UNSAFE", bad.Observation.Classification);
                Assert.AreEqual(1, observer.CameraPoseUnsafeCount);
            }
            finally
            {
                if (camera != null) Object.Destroy(camera);
                if (root != null) Object.Destroy(root);
            }
            yield return null;
        }
    }
}
