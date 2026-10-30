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
        // True only after every validated gate and an authoritative finish.
        // An aborted run MUST NOT look faster than a completed benchmark.
        public bool HasFullCourseComparison { get; set; }
        public int BiggestLossGate { get; set; }
        // A finish-segment loss is NOT a checkpoint loss: gate=0, this flag=true.
        public bool BiggestLossIsFinishSegment { get; set; }
        public float BiggestLossSeconds { get; set; }
        public float FinishSegmentSeconds { get; set; }
        public float ReferenceFinishSegmentSeconds { get; set; }
        public float FinishSegmentDeltaSeconds { get; set; }
        public string Guidance { get; set; }
    }

    // Shared UI decision contract: a measured maximum below tolerance is
    // informative data, not an actionable "focus" recommendation.
    // Compiled in both portable .NET tests and Unity; UI must call this rather
    // than infer focus from BiggestLossGate alone.
    public static class GhostPracticeFocus
    {
        public static string Text(GhostPracticeSummary summary, int requiredGates)
        {
            GhostMath.Check(summary != null, "Practice summary is missing");
            GhostMath.Check(requiredGates >= 1 && requiredGates <= 8,
                "Practice gate count must be 1..8");
            GhostMath.Check(!float.IsNaN(summary.BiggestLossSeconds)
                            && !float.IsInfinity(summary.BiggestLossSeconds)
                            && summary.BiggestLossSeconds >= 0f,
                "Practice loss must be finite and non-negative");
            if (!summary.Completed || !summary.HasFullCourseComparison)
                return "Retry: no finish segment measured";
            if (summary.BiggestLossSeconds <= GhostSplitCoach.PaceToleranceSeconds)
                return "No significant loss (finish included)";
            if (summary.BiggestLossIsFinishSegment)
                return "Focus: finish after gate " + requiredGates;
            GhostMath.Check(summary.BiggestLossGate >= 1
                            && summary.BiggestLossGate <= requiredGates,
                "Significant checkpoint loss without a valid gate");
            return "Focus: gate " + summary.BiggestLossGate;
        }
    }

    // A read-only replay of the actual reference segment where the player
    // lost the most *significant* time. Constructed only by a finished coach.
    // Not a teleport, training checkpoint, collision body or game timer.
    public sealed class GhostPracticeLoop
    {
        private readonly GhostPlayback playback;
        public float StartSeconds { get; private set; }
        public float EndSeconds { get; private set; }
        public float SegmentDurationSeconds => EndSeconds - StartSeconds;
        public bool IsFinishSegment { get; private set; }
        public int GateNumber { get; private set; }
        public string CourseId => playback.CourseId;
        public string RigId => playback.RigId;
        public string Label => IsFinishSegment
            ? "finish after gate " + GateNumber
            : "gate " + GateNumber;

        internal GhostPracticeLoop(GhostRun reference, int gateNumber, bool finish)
        {
            GhostRun validated = GhostMath.Clone(reference);
            GhostMath.Check(validated.Completed, "Loop requires a completed reference");
            int count = validated.RequiredCheckpoints;
            GhostMath.Check(gateNumber >= 1 && gateNumber <= count,
                "Loop target gate not present");
            GhostMath.Check(!finish || gateNumber == count,
                "Finish loop must follow the last gate");
            float start = finish ? validated.CheckpointSeconds[count - 1]
                : gateNumber == 1 ? 0f : validated.CheckpointSeconds[gateNumber - 2];
            float end = finish ? validated.ElapsedSeconds
                : validated.CheckpointSeconds[gateNumber - 1];
            GhostMath.Check(end > start,
                "Loop requires a positive-duration measured segment");
            StartSeconds = start;
            EndSeconds = end;
            IsFinishSegment = finish;
            GateNumber = gateNumber;
            playback = new GhostPlayback(validated);
        }

        // The host explicitly supplies a nonnegative visual-only replay clock.
        // Sampling never advances that clock, moves the real robot or fires gates.
        // 24-hour cap prevents unbounded float precision and invalid input.
        public GhostPose PoseAt(double loopElapsedSeconds)
        {
            GhostMath.Check(!double.IsNaN(loopElapsedSeconds)
                            && !double.IsInfinity(loopElapsedSeconds)
                            && loopElapsedSeconds >= 0d
                            && loopElapsedSeconds <= 86400d,
                "Practice replay clock must be finite and bounded");
            double phase = loopElapsedSeconds % SegmentDurationSeconds;
            return playback.Seek(StartSeconds + (float)phase);
        }
    }

    public sealed class GhostSplitCoach
    {
        // Significant to a human without false precision from fixed 10 Hz poses.
        public const float PaceToleranceSeconds = .15f;
        private readonly GhostRun reference;
        private readonly List<GhostSplitResult> splits = new List<GhostSplitResult>();
        private float lastSeconds;
        private bool finished;
        private GhostPracticeLoop recommendedLoop;

        public GhostPracticeLoop RecommendedLoop => recommendedLoop;
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
            // Each genuine gate and finish leg must have a measurable positive
            // duration. GhostRun permits zero-time source event markers, but a
            // split COACH cannot promise accurate per-segment advice for them.
            float previous = 0f;
            foreach (float gate in reference.CheckpointSeconds)
            {
                GhostMath.Check(gate > previous,
                    "Practice benchmark has zero-duration checkpoint segment");
                previous = gate;
            }
            GhostMath.Check(reference.ElapsedSeconds > previous,
                "Practice benchmark has zero-duration finish segment");
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

            // W10 fix: the old evaluator stopped at checkpoint #N and silently
            // ignored the full last leg to finish. In a four-gate course this
            // created a misleading "no loss" tip even when the finish cost 5s.
            // Only compare an authoritative completed finish, not a failed run.
            float finishSegment = 0f;
            float referenceFinishSegment = 0f;
            float finishDelta = 0f;
            bool biggestLossIsFinish = false;
            if (completed)
            {
                finishSegment = authoritativeRunSeconds - lastSeconds;
                referenceFinishSegment = reference.ElapsedSeconds
                    - reference.CheckpointSeconds[reference.RequiredCheckpoints - 1];
                finishDelta = finishSegment - referenceFinishSegment;
                if (finishDelta > maxLoss)
                {
                    maxLoss = finishDelta;
                    worstGate = 0;
                    biggestLossIsFinish = true;
                }
            }

            string guidance;
            if (!completed)
                guidance = "Attempt ended before finish; retry to measure full-course pace.";
            else if (biggestLossIsFinish && maxLoss > PaceToleranceSeconds)
                guidance = "Practice finish segment after checkpoint "
                           + reference.RequiredCheckpoints.ToString()
                           + ": largest measured time loss versus reference.";
            else if (worstGate != 0 && maxLoss > PaceToleranceSeconds)
                guidance = "Practice segment ending at checkpoint "
                           + worstGate.ToString()
                           + ": largest measured time loss versus reference.";
            else
                guidance = "No measured segment (including finish) exceeded time-loss tolerance.";
            // Stopwatch outcome only; do not report "vehicle unstable", collisions or
            // other diagnoses not supported by signed physical sensor evidence.
            var summary = new GhostPracticeSummary
            {
                Completed = completed,
                TrialSeconds = authoritativeRunSeconds,
                ReferenceSeconds = reference.ElapsedSeconds,
                TotalDeltaSeconds = completed
                    ? authoritativeRunSeconds - reference.ElapsedSeconds : 0f,
                HasFullCourseComparison = completed,
                BiggestLossGate = worstGate,
                BiggestLossIsFinishSegment = biggestLossIsFinish,
                BiggestLossSeconds = maxLoss,
                FinishSegmentSeconds = finishSegment,
                ReferenceFinishSegmentSeconds = referenceFinishSegment,
                FinishSegmentDeltaSeconds = finishDelta,
                Guidance = guidance
            };
            // Only a verified completion can produce a replay drill. Small
            // deviations and failed trials never generate actionable loops.
            GhostPracticeLoop nextLoop = null;
            if (completed && maxLoss > PaceToleranceSeconds)
            {
                int targetGate = biggestLossIsFinish
                    ? reference.RequiredCheckpoints : worstGate;
                nextLoop = new GhostPracticeLoop(reference, targetGate,
                    biggestLossIsFinish);
            }
            recommendedLoop = nextLoop;
            finished = true;
            return summary;
        }

        // Safe retry starts fresh event order; keeps validated immutable benchmark.
        public GhostSplitCoach Retry() => new GhostSplitCoach(reference);
    }
}
