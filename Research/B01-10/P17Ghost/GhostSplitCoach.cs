using System;
using System.Collections.Generic;

namespace RoboRougelike.B0110.TrainingGhost
{
    // Opt-in CHL practice HUD data: consumes real, ALREADY VALIDATED controller gates.
    // No game physics, rewards, player control, score, save or ghost frames are mutated.
    public enum GhostPace { Ahead, Even, Behind }

    public sealed class GhostSplitResult
    {
        public int GateNumber { get; set; }
        public float SegmentSeconds { get; set; }
        public float ReferenceSegmentSeconds { get; set; }
        public float SegmentDeltaSeconds { get; set; }
        public float CumulativeDeltaSeconds { get; set; }
        public GhostPace Pace { get; set; }
    }

    public sealed class GhostPracticeSummary
    {
        public bool Completed { get; set; }
        public float TrialSeconds { get; set; }
        public float ReferenceSeconds { get; set; }
        public float TotalDeltaSeconds { get; set; }
        public int BiggestLossGate { get; set; }
        public float BiggestLossSeconds { get; set; }
        public string Guidance { get; set; }
    }

    public sealed class GhostSplitCoach
    {
        // Significant to a human without false precision from fixed 10 Hz poses.
        public const float PaceToleranceSeconds = .15f;
        private readonly GhostRun reference;
        private readonly List<GhostSplitResult> splits = new List<GhostSplitResult>();
        private float lastSeconds;
        private bool finished;

        public int GatesReached => splits.Count;
        public int RequiredGates => reference.RequiredCheckpoints;
        public bool IsFinished => finished;
        public string CourseId => reference.CourseId;
        public string ReferenceRig => reference.RigId;
        public GhostSplitResult LastSplit =>
            splits.Count == 0 ? null : Copy(splits[splits.Count - 1]);

        public GhostSplitCoach(GhostRun completedReference)
        {
            // Clone independently and validate at construction, fail closed on forged
            // or failed benchmark records. A subsequent caller edit never affects us.
            reference = GhostMath.Clone(completedReference);
            GhostMath.Check(reference.Completed,
                "Only completed attempts can be a training benchmark");
        }

        private static GhostSplitResult Copy(GhostSplitResult a)
        {
            return new GhostSplitResult
            {
                GateNumber = a.GateNumber,
                SegmentSeconds = a.SegmentSeconds,
                ReferenceSegmentSeconds = a.ReferenceSegmentSeconds,
                SegmentDeltaSeconds = a.SegmentDeltaSeconds,
                CumulativeDeltaSeconds = a.CumulativeDeltaSeconds,
                Pace = a.Pace
            };
        }

        public GhostSplitResult AcceptValidatedGate(int zeroBasedIndex,
                                                     float authoritativeRunSeconds)
        {
            GhostMath.Check(!finished && zeroBasedIndex == splits.Count
                            && zeroBasedIndex < reference.RequiredCheckpoints,
                "Gate callback skipped, duplicated, out of order or after trial ended");
            GhostMath.Check(!float.IsNaN(authoritativeRunSeconds)
                            && !float.IsInfinity(authoritativeRunSeconds)
                            && authoritativeRunSeconds > lastSeconds
                            && authoritativeRunSeconds <= GhostMath.MaxDuration,
                "Real gate elapsed must be finite, increasing and positive");
            float prevReference = zeroBasedIndex == 0
                ? 0 : reference.CheckpointSeconds[zeroBasedIndex - 1];
            float benchmarkSegment =
                reference.CheckpointSeconds[zeroBasedIndex] - prevReference;
            GhostMath.Check(benchmarkSegment > 0,
                "Reference contains an unmeasurable zero-duration segment");

            float segment = authoritativeRunSeconds - lastSeconds;
            float segmentDelta = segment - benchmarkSegment;
            float cumulative =
                authoritativeRunSeconds - reference.CheckpointSeconds[zeroBasedIndex];
            GhostPace pace = segmentDelta < -PaceToleranceSeconds
                ? GhostPace.Ahead
                : segmentDelta > PaceToleranceSeconds
                    ? GhostPace.Behind : GhostPace.Even;
            var result = new GhostSplitResult
            {
                GateNumber = zeroBasedIndex + 1,
                SegmentSeconds = segment,
                ReferenceSegmentSeconds = benchmarkSegment,
                SegmentDeltaSeconds = segmentDelta,
                CumulativeDeltaSeconds = cumulative,
                Pace = pace
            };
            // Atomic commit: no callback state mutates before ALL validations pass.
            splits.Add(result);
            lastSeconds = authoritativeRunSeconds;
            return Copy(result);
        }

        public GhostPracticeSummary EndTrial(bool completed,
                                             float authoritativeRunSeconds)
        {
            GhostMath.Check(!finished, "Trial already finalized");
            GhostMath.Check(!float.IsNaN(authoritativeRunSeconds)
                            && !float.IsInfinity(authoritativeRunSeconds)
                            && authoritativeRunSeconds >= lastSeconds
                            && authoritativeRunSeconds <= GhostMath.MaxDuration
                            && authoritativeRunSeconds >= 0,
                "Invalid final authoritative game elapsed");
            GhostMath.Check(!completed || (splits.Count == reference.RequiredCheckpoints
                            && authoritativeRunSeconds > lastSeconds),
                "Cannot present a successful practice result before all true gates + finish");

            float maxLoss = 0;
            int worstGate = 0;
            foreach (var s in splits)
            {
                if (s.SegmentDeltaSeconds > maxLoss)
                {
                    maxLoss = s.SegmentDeltaSeconds;
                    worstGate = s.GateNumber;
                }
            }

            string guidance;
            if (!completed)
                guidance = "Attempt ended before finish; retry to measure full-course pace.";
            else if (worstGate != 0 && maxLoss > PaceToleranceSeconds)
                guidance = "Practice segment ending at checkpoint "
                           + worstGate.ToString()
                           + ": largest measured time loss versus reference.";
            else
                guidance = "No checkpoint segment exceeded the time-loss tolerance.";
            // Stopwatch outcome only; do not report "vehicle unstable", collisions or
            // other diagnoses not supported by signed physical sensor evidence.
            var summary = new GhostPracticeSummary
            {
                Completed = completed,
                TrialSeconds = authoritativeRunSeconds,
                ReferenceSeconds = reference.ElapsedSeconds,
                TotalDeltaSeconds = authoritativeRunSeconds - reference.ElapsedSeconds,
                BiggestLossGate = worstGate,
                BiggestLossSeconds = maxLoss,
                Guidance = guidance
            };
            finished = true;
            return summary;
        }

        // Safe retry starts fresh event order; keeps validated immutable benchmark.
        public GhostSplitCoach Retry() => new GhostSplitCoach(reference);
    }
}
