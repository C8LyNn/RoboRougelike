using System;
using NUnit.Framework;
using UnityEngine;

namespace B01.CAMERA001.Tests
{
    public sealed class Camera001AimTimelineTests
    {
        private static Camera001AimTimeline.Frame F(int frame, int collider, Vector3 target,
            Vector3 muzzle, Vector3 forward, bool unsafePose = false)
        {
            return new Camera001AimTimeline.Frame {
                FrameIndex = frame,
                HitColliderInstanceId = collider,
                HitKind = collider == 1 ? "Enemy/Armor" : "Floor",
                Sample = new Camera001AimTransitionDiagnostics.Sample {
                    CurrentTargetWorld = target,
                    CurrentMuzzleWorld = muzzle,
                    CurrentMuzzleForwardWorld = forward,
                    DeltaTimeSeconds = 1f / 60f,
                    TurretSlewDegreesPerSecond = 170f,
                    CameraFinalAimErrorDegrees = unsafePose ? .2f : 0f
                }
            };
        }

        [Test] public void RealQaCoordinatesIdentifyActualTargetJumpAndRateBudget()
        {
            var q = new Camera001AimTimeline();
            var muzzle = new Vector3(.00401f, 1.5552f, 27.8219f);
            var oldTarget = new Vector3(.00391f, 1.914f, 30.8045f);
            var newTarget = new Vector3(.00269f, 0f, 34.8804f);
            var originalForward = (oldTarget - muzzle).normalized;
            q.Capture(F(30, 1, oldTarget, muzzle, originalForward));
            var r = q.Capture(F(31, 2, newTarget, muzzle, originalForward));
            Assert.IsTrue(r.FirstHitChanged);
            Assert.IsTrue(r.Observation.TargetChangeExceedsOneFrameTurretBudget);
            Assert.Greater(r.Observation.TargetDirectionChangeDegrees, 19f);
            Assert.AreEqual(1, q.RetargetOverBudgetCount);
            Assert.Greater(q.PeakTrackingErrorDegrees, 15f);
        }

        [Test] public void UnchangedTargetWithChangedHitIdIsNotAnAngularJump()
        {
            var q = new Camera001AimTimeline();
            var target = new Vector3(0f, 1f, 30f);
            q.Capture(F(1, 1, target, Vector3.zero, target.normalized));
            var result = q.Capture(F(2, 2, target, Vector3.zero, target.normalized));
            Assert.IsTrue(result.FirstHitChanged);
            Assert.IsFalse(result.Observation.TargetChangeExceedsOneFrameTurretBudget);
            Assert.AreEqual(0, q.RetargetOverBudgetCount);
        }

        [Test] public void CallerCannotOverwriteRecordedPreviousTarget()
        {
            var q = new Camera001AimTimeline();
            var previous = new Vector3(0f, 1f, 30f);
            var current = new Vector3(0f, 0f, 35f);
            q.Capture(F(5, 1, previous, Vector3.zero, previous.normalized));
            var next = F(6, 2, current, Vector3.zero, previous.normalized);
            next.Sample.PreviousTargetWorld = current; // attempted tampering
            var r = q.Capture(next);
            Assert.Greater(r.Observation.TargetDirectionChangeDegrees, .05f);
        }

        [Test] public void InvalidFrameDoesNotMutateRecorder()
        {
            var q = new Camera001AimTimeline(2);
            q.Capture(F(10, 1, Vector3.forward * 20f,
                Vector3.zero, Vector3.forward));
            var bad = F(11, 2, Vector3.forward * 20f,
                Vector3.zero, Vector3.forward);
            bad.Sample.DeltaTimeSeconds = 0f;
            Assert.Throws<ArgumentException>(() => q.Capture(bad));
            Assert.AreEqual(1, q.Count);
            Assert.AreEqual(0, q.ChangedFirstHitCount);
            q.Capture(F(11, 2, Vector3.forward * 20f, Vector3.zero,
                Vector3.forward));
            Assert.AreEqual(2, q.Count);
            Assert.Throws<InvalidOperationException>(() => q.Capture(
                F(12, 2, Vector3.forward * 20f, Vector3.zero, Vector3.forward)));
            Assert.AreEqual(2, q.Count);
        }

        [Test] public void MonotonicFrameAndCsvQuoteEscapeFailClosed()
        {
            var q = new Camera001AimTimeline();
            var t = F(1, 1, Vector3.forward * 20f, Vector3.zero, Vector3.forward);
            t.HitKind = "Enemy,\"Armor\"";
            q.Capture(t);
            Assert.Throws<ArgumentException>(() => q.Capture(t));
            Assert.AreEqual(1, q.Count);
            StringAssert.Contains("\"Enemy,\"\"Armor\"\"\"", q.ToCsv());
        }

        [Test] public void UnsafeCameraSampleNeverLabelsRateAsProvenCausality()
        {
            var q = new Camera001AimTimeline();
            var first = F(0, 1, Vector3.forward * 20f, Vector3.zero,
                Vector3.forward);
            q.Capture(first);
            var second = F(1, 2, Vector3.right * 20f, Vector3.zero,
                Vector3.forward, unsafePose: true);
            var r = q.Capture(second);
            Assert.AreEqual("CAMERA_FINAL_POSE_UNSAFE", r.Observation.Classification);
            Assert.AreEqual(1, q.CameraPoseUnsafeCount);
        }
    }
}
