using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace B01.CAMERA001
{
    /// <summary>
    /// Bounded, passive, in-memory per-frame observation for a matched native
    /// CAMERA/BASE receiver. Nothing here chooses targets, rotates guns,
    /// changes input, raycasts, or writes files. The receiver supplies actual
    /// pre-fire hit and muzzle states in a precisely documented phase.
    /// </summary>
    public sealed class Camera001AimTimeline
    {
        public const string CapturePhase = "AFTER_WEAPON_LATEUPDATE_BEFORE_END_OF_FRAME";
        public struct Frame
        {
            public int FrameIndex;
            public int HitColliderInstanceId; // 0 means no hit; stable only within one native process
            public string HitKind;
            public Camera001AimTransitionDiagnostics.Sample Sample;
        }

        public struct Record
        {
            public int FrameIndex;
            public int HitColliderInstanceId;
            public string HitKind;
            public bool FirstHitChanged;
            public Camera001AimTransitionDiagnostics.Observation Observation;
        }

        private readonly List<Record> records;
        private readonly int capacity;
        private Frame previous;
        private bool hasPrevious;
        private int changedFirstHitCount;
        private int retargetOverBudgetCount;
        private int cameraPoseUnsafeCount;
        private float peakTrackingError;
        public int Count => records.Count;
        public int ChangedFirstHitCount => changedFirstHitCount;
        public int RetargetOverBudgetCount => retargetOverBudgetCount;
        public int CameraPoseUnsafeCount => cameraPoseUnsafeCount;
        public float PeakTrackingErrorDegrees => peakTrackingError;

        public Camera001AimTimeline(int maxFrames = 8192)
        {
            if (maxFrames < 2 || maxFrames > 262144)
                throw new ArgumentOutOfRangeException(nameof(maxFrames));
            capacity = maxFrames;
            records = new List<Record>(Math.Min(maxFrames, 256));
        }

        public Record Capture(Frame frame)
        {
            if (records.Count >= capacity)
                throw new InvalidOperationException("CAMERA001_AIM_TIMELINE_CAPACITY");
            if (frame.FrameIndex < 0)
                throw new ArgumentException("CAMERA001_AIM_TIMELINE_NEGATIVE_FRAME");
            if (hasPrevious && frame.FrameIndex <= previous.FrameIndex)
                throw new ArgumentException("CAMERA001_AIM_TIMELINE_NONMONOTONIC_FRAME");
            // The measured aim jump is compared with the angular budget for
            // *one* current-frame dt. If intermediate frames were dropped,
            // classifying that multi-frame jump as a one-frame servo failure
            // is mathematically invalid. Reject before state mutation.
            // Use widened arithmetic to prevent int overflow at the limit.
            if (hasPrevious && (long)frame.FrameIndex - previous.FrameIndex != 1L)
                throw new ArgumentException("CAMERA001_AIM_TIMELINE_SKIPPED_FRAME");
            if (string.IsNullOrEmpty(frame.HitKind) || frame.HitKind.Length > 256)
                throw new ArgumentException("CAMERA001_AIM_TIMELINE_HIT_KIND_INVALID");

            // Previous target and forward come exclusively from actual PRIOR
            // captured frame, never caller's unverified previous-value fields.
            var sample = frame.Sample;
            if (hasPrevious)
            {
                sample.PreviousTargetWorld = previous.Sample.CurrentTargetWorld;
                sample.PreviousMuzzleForwardWorld = previous.Sample.CurrentMuzzleForwardWorld;
            }
            else
            {
                sample.PreviousTargetWorld = sample.CurrentTargetWorld;
                sample.PreviousMuzzleForwardWorld = sample.CurrentMuzzleForwardWorld;
            }
            var measure = Camera001AimTransitionDiagnostics.Measure(sample); // validate BEFORE commit
            bool changedHit = hasPrevious &&
                frame.HitColliderInstanceId != previous.HitColliderInstanceId;
            var record = new Record {
                FrameIndex = frame.FrameIndex,
                HitColliderInstanceId = frame.HitColliderInstanceId,
                HitKind = frame.HitKind,
                FirstHitChanged = changedHit,
                Observation = measure
            };
            frame.Sample = sample;
            records.Add(record);
            previous = frame;
            hasPrevious = true;
            if (changedHit) changedFirstHitCount++;
            if (changedHit && measure.TargetChangeExceedsOneFrameTurretBudget)
                retargetOverBudgetCount++;
            if (measure.CameraFinalPoseUnsafe) cameraPoseUnsafeCount++;
            peakTrackingError = Mathf.Max(peakTrackingError,
                measure.CurrentMuzzleToTargetErrorDegrees);
            return record;
        }

        public Record[] Snapshot() => records.ToArray();

        public string ToCsv()
        {
            var b = new StringBuilder();
            b.AppendLine("frame,phase,hitId,hitKind,hitChanged,targetStepDeg,availableSlewDeg,worldMuzzleStepDeg,trackingErrorDeg,catchupLowerSec,rateStepOverBudget,cameraPoseUnsafe,worldMuzzleStepOverBudget,classification");
            foreach (var r in records)
            {
                var o = r.Observation;
                b.Append(r.FrameIndex).Append(',').Append(CapturePhase).Append(',')
                 .Append(r.HitColliderInstanceId).Append(',')
                 .Append(CsvText(r.HitKind)).Append(',')
                 .Append(Bool(r.FirstHitChanged)).Append(',')
                 .Append(Num(o.TargetDirectionChangeDegrees)).Append(',')
                 .Append(Num(o.AvailableTurretSlewDegrees)).Append(',')
                 .Append(Num(o.ObservedMuzzleDirectionChangeDegrees)).Append(',')
                 .Append(Num(o.CurrentMuzzleToTargetErrorDegrees)).Append(',')
                 .Append(Num(o.LowerBoundCatchUpSeconds)).Append(',')
                 .Append(Bool(o.TargetChangeExceedsOneFrameTurretBudget)).Append(',')
                 .Append(Bool(o.CameraFinalPoseUnsafe)).Append(',')
                 .Append(Bool(o.MuzzleWorldStepExceedsTurretBudget)).Append(',')
                 .Append(o.Classification).Append('\n');
            }
            return b.ToString();
        }

        private static string Num(float n) => n.ToString("R", CultureInfo.InvariantCulture);
        private static string Bool(bool b) => b ? "true" : "false";
        private static string CsvText(string s) =>
            "\"" + s.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
    }
}
