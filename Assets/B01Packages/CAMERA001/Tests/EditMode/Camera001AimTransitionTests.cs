using System;
using NUnit.Framework;
using UnityEngine;

namespace B01.CAMERA001.Tests
{
    public sealed class Camera001AimTransitionTests
    {
        private static Camera001AimTransitionDiagnostics.Sample Sample(
            Vector3 oldTarget, Vector3 currentTarget, Vector3 muzzle,
            Vector3 beforeForward, Vector3 afterForward, float dt = 1f / 60f,
            float cameraError = 0f, bool ownHit = false, bool worldHit = false)
        {
            return new Camera001AimTransitionDiagnostics.Sample {
                PreviousTargetWorld = oldTarget,
                CurrentTargetWorld = currentTarget,
                CurrentMuzzleWorld = muzzle,
                PreviousMuzzleForwardWorld = beforeForward,
                CurrentMuzzleForwardWorld = afterForward,
                DeltaTimeSeconds = dt,
                TurretSlewDegreesPerSecond = 170f,
                CameraFinalAimErrorDegrees = cameraError,
                CameraOwnHullRayHit = ownHit,
                CameraWorldOverlap = worldHit
            };
        }

        [Test] public void R12LightHitSwitchRequiresMoreThanOneFrameSlew()
        {
            // Read-only approximate coordinates reported by native QA #249.
            var muzzle = new Vector3(.00401f, 1.5552f, 27.8219f);
            var previous = new Vector3(.00391f, 1.914f, 30.8045f);
            var current = new Vector3(.00269f, 0f, 34.8804f);
            Vector3 before = previous - muzzle;
            var observed = Camera001AimTransitionDiagnostics.Measure(Sample(
                previous, current, muzzle, before, before));
            Assert.IsTrue(observed.TargetChangeExceedsOneFrameTurretBudget);
            Assert.Greater(observed.TargetDirectionChangeDegrees,
                observed.AvailableTurretSlewDegrees);
            Assert.AreEqual("TARGET_STEP_EXCEEDS_TURRET_BUDGET_PLAUSIBLE",
                observed.Classification);
            Assert.Greater(observed.LowerBoundCatchUpSeconds, 0f);
        }

        [Test] public void StableTargetAndAlignedGunAreNotFalselyBlamed()
        {
            var target = new Vector3(0, 1f, 30f);
            var muzzle = new Vector3(0, 1f, 0f);
            var result = Camera001AimTransitionDiagnostics.Measure(Sample(
                target, target, muzzle, Vector3.forward, Vector3.forward));
            Assert.AreEqual("ALIGNED", result.Classification);
            Assert.AreEqual(0f, result.CurrentMuzzleToTargetErrorDegrees, .001f);
        }

        [Test] public void SafePoseButStableTargetGapIsUnresolvedNotCameraFault()
        {
            var result = Camera001AimTransitionDiagnostics.Measure(Sample(
                Vector3.forward * 20f, Vector3.forward * 20f, Vector3.zero,
                Vector3.right, Vector3.right));
            Assert.AreEqual("UNRESOLVED_TRACKING_GAP_REQUIRES_PAIRED_NATIVE_QA",
                result.Classification);
        }

        [Test] public void ActualCameraPoseFailureHasPriority()
        {
            var result = Camera001AimTransitionDiagnostics.Measure(Sample(
                Vector3.forward * 20f, Vector3.right * 20f, Vector3.zero,
                Vector3.forward, Vector3.forward, cameraError: 0.1f));
            Assert.AreEqual("CAMERA_FINAL_POSE_UNSAFE", result.Classification);
        }

        [Test] public void ActualOwnRayOrWorldOverlapAlwaysFlagsCamera()
        {
            var a = Camera001AimTransitionDiagnostics.Measure(Sample(
                Vector3.forward, Vector3.forward, Vector3.zero,
                Vector3.forward, Vector3.forward, ownHit: true));
            var b = Camera001AimTransitionDiagnostics.Measure(Sample(
                Vector3.forward, Vector3.forward, Vector3.zero,
                Vector3.forward, Vector3.forward, worldHit: true));
            Assert.IsTrue(a.CameraFinalPoseUnsafe);
            Assert.IsTrue(b.CameraFinalPoseUnsafe);
        }

        [Test] public void InvalidZeroDeltaOrCoincidentTargetRejects()
        {
            Assert.Throws<ArgumentException>(() =>
                Camera001AimTransitionDiagnostics.Measure(Sample(
                    Vector3.forward, Vector3.forward, Vector3.zero,
                    Vector3.forward, Vector3.forward, dt: 0f)));
            Assert.Throws<ArgumentException>(() =>
                Camera001AimTransitionDiagnostics.Measure(Sample(
                    Vector3.zero, Vector3.forward, Vector3.zero,
                    Vector3.forward, Vector3.forward)));
        }
    }
}
