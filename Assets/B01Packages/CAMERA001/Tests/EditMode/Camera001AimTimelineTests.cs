using System;
using System.Globalization;
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

        [Test] public void SkippedFramesCannotBeMisclassifiedAsSingleFrameGunSlew()
        {
            var t = new Camera001AimTimeline();
            var muzzle = new Vector3(0f, 1.5f, 27f);
            var enemy = new Vector3(0f, 2f, 30f);
            var ground = new Vector3(0f, 0f, 35f);
            var forward = (enemy - muzzle).normalized;
            t.Capture(F(100, 1, enemy, muzzle, forward));
            // Previous permissive source treated ten missing frames as one
            // 1/60s slew opportunity: false blame of limited turret speed.
            var skipped = F(110, 2, ground, muzzle, forward);
            var before = t.ToCsv();
            var ex = Assert.Throws<ArgumentException>(() => t.Capture(skipped));
            StringAssert.Contains("SKIPPED_FRAME", ex.Message);
            Assert.AreEqual(1, t.Count);
            Assert.AreEqual(0, t.ChangedFirstHitCount);
            Assert.AreEqual(0, t.RetargetOverBudgetCount);
            Assert.AreEqual(before, t.ToCsv());
            // After reject the next contiguous frame must still work.
            var actual = t.Capture(F(101, 2, ground, muzzle, forward));
            Assert.IsTrue(actual.FirstHitChanged);
            Assert.IsTrue(actual.Observation.TargetChangeExceedsOneFrameTurretBudget);
        }

        [Test] public void NegativeFrameRejectedAndMaxIntFrameCannotWrap()
        {
            var t = new Camera001AimTimeline();
            var v = F(-1, 1, Vector3.forward * 30f,
                Vector3.zero, Vector3.forward);
            Assert.Throws<ArgumentException>(() => t.Capture(v));
            Assert.AreEqual(0, t.Count);
            v.FrameIndex = int.MaxValue;
            t.Capture(v);
            var overflow = F(int.MinValue, 2, Vector3.forward * 30f,
                Vector3.zero, Vector3.forward);
            Assert.Throws<ArgumentException>(() => t.Capture(overflow));
            Assert.AreEqual(1, t.Count);
        }

        [Test] public void RawProvenanceSurvivesCallerSpoofAndIsRecomputable()
        {
            var log = new Camera001AimTimeline();
            var origin = new Vector3(.00401f, 1.5552f, 27.8219f);
            var enemy = new Vector3(.00391f, 1.914f, 30.8045f);
            var floor = new Vector3(.00269f, 0f, 34.8804f);
            var gun = (enemy - origin).normalized;
            log.Capture(F(9, 1, enemy, origin, gun));
            var second = F(10, 2, floor, origin, gun);
            second.Sample.PreviousTargetWorld = floor; // forged prior
            second.Sample.PreviousMuzzleForwardWorld = Vector3.right;
            var observed = log.Capture(second);
            Assert.AreEqual(enemy, observed.Sample.PreviousTargetWorld);
            Assert.AreEqual(gun, observed.Sample.PreviousMuzzleForwardWorld);
            Assert.AreEqual(floor, observed.Sample.CurrentTargetWorld);
            Assert.AreEqual(origin, observed.Sample.CurrentMuzzleWorld);
            Assert.AreEqual(170f, observed.Sample.TurretSlewDegreesPerSecond);
            float recalculated = Vector3.Angle(
                observed.Sample.PreviousTargetWorld - observed.Sample.CurrentMuzzleWorld,
                observed.Sample.CurrentTargetWorld - observed.Sample.CurrentMuzzleWorld);
            Assert.AreEqual(recalculated,
                observed.Observation.TargetDirectionChangeDegrees, .0001f);
            Assert.Greater(observed.Observation.CurrentMuzzleToTargetErrorDegrees, 10f);
            Assert.AreEqual(observed.Sample.CurrentTargetWorld, log.Snapshot()[1].Sample.CurrentTargetWorld);
        }

        [Test] public void StableHitFramesRetainUnresolvedMuzzleLagWithRawCoordinates()
        {
            var log = new Camera001AimTimeline();
            var muzzle = new Vector3(0f, 1.5552f, 27.8219f);
            var floor = new Vector3(0f, 0f, 34.8804f);
            var stationaryGun = Vector3.forward;
            log.Capture(F(100, 7, floor, muzzle, stationaryGun));
            var record = log.Capture(F(101, 7, floor, muzzle, stationaryGun));
            Assert.IsFalse(record.FirstHitChanged);
            Assert.IsFalse(record.Observation.TargetChangeExceedsOneFrameTurretBudget);
            Assert.Greater(record.Observation.CurrentMuzzleToTargetErrorDegrees, 10f);
            Assert.AreEqual("UNRESOLVED_TRACKING_GAP_REQUIRES_PAIRED_NATIVE_QA",
                record.Observation.Classification);
            string[] lines = log.ToCsv().TrimEnd().Split('\n');
            Assert.AreEqual(3, lines.Length);
            Assert.AreEqual(34, lines[0].Split(',').Length);
            Assert.AreEqual(34, lines[2].Split(',').Length);
            Assert.AreEqual("false", lines[2].Split(',')[4]);
        }

        [Test] public void CsvRawFloatsRemainInvariantInCommaDecimalLocale()
        {
            var oldCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture =
                    CultureInfo.GetCultureInfo("pl-PL");
                var log = new Camera001AimTimeline();
                var target = new Vector3(.1f, 1.25f, 30.875f);
                var muzzle = new Vector3(.1f, 1.5f, 0f);
                log.Capture(F(1, 3, target, muzzle, Vector3.forward));
                string[] lines = log.ToCsv().TrimEnd().Split('\n');
                Assert.AreEqual(34, lines[0].Split(',').Length);
                Assert.AreEqual(34, lines[1].Split(',').Length);
                StringAssert.Contains("1.25", lines[1]);
                StringAssert.Contains("30.875", lines[1]);
                StringAssert.Contains("1.5", lines[1]);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = oldCulture;
            }
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
