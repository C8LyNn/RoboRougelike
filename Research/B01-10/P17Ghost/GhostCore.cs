using System;
using System.Collections.Generic;

namespace RoboRougelike.B0110.TrainingGhost
{
    // New B01-10 training feature; never drives Physics, rewards or run state.
    // Host opt-in: copy into separate Unity assembly only after accepted G0/QA.
    public sealed class GhostPose
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Yaw { get; set; }
    }

    public sealed class GhostFrame
    {
        public float Time { get; set; }
        public GhostPose Pose { get; set; }
    }

    public sealed class GhostRun
    {
        public string CourseId { get; set; }
        public string RigId { get; set; }
        public bool Completed { get; set; }
        public int RequiredCheckpoints { get; set; }
        public float ElapsedSeconds { get; set; }
        public GhostFrame[] Frames { get; set; }
        public float[] CheckpointSeconds { get; set; }
        public string EndReason { get; set; }
    }

    public sealed class GhostComparison
    {
        public string CourseId { get; set; }
        public string ReferenceRig { get; set; }
        public string TrialRig { get; set; }
        public float[] SplitDeltaSeconds { get; set; }
        public float TotalDeltaSeconds { get; set; }
    }

    public static class GhostMath
    {
        public const float MaxDuration = 900f;
        private static bool Finite(float a) =>
            !float.IsNaN(a) && !float.IsInfinity(a);

        internal static void Check(bool ok, string message)
        {
            if (!ok) throw new ArgumentException(message);
        }

        internal static GhostPose Copy(GhostPose p)
        {
            Check(p != null, "Pose cannot be null");
            Check(Finite(p.X) && Finite(p.Y) && Finite(p.Z) && Finite(p.Yaw),
                "Nonfinite position/yaw");
            Check(Math.Abs(p.X) <= 10000f && Math.Abs(p.Y) <= 10000f
                && Math.Abs(p.Z) <= 10000f && Math.Abs(p.Yaw) <= 1000000f,
                "Pose outside finite world bounds");
            return new GhostPose { X = p.X, Y = p.Y, Z = p.Z, Yaw = Normalize(p.Yaw) };
        }

        private static float Normalize(float yaw)
        {
            var r = yaw % 360f;
            return r < 0 ? r + 360f : r;
        }

        private static GhostPose Interpolate(GhostPose a, GhostPose b, float t)
        {
            float delta = ((b.Yaw - a.Yaw + 540f) % 360f) - 180f;
            return new GhostPose
            {
                X = a.X + (b.X - a.X) * t,
                Y = a.Y + (b.Y - a.Y) * t,
                Z = a.Z + (b.Z - a.Z) * t,
                Yaw = Normalize(a.Yaw + delta * t)
            };
        }

        public static GhostPose At(GhostRun run, float seconds)
        {
            Validate(run);
            return AtValidated(run, seconds);
        }

        // Internal fast path: callers MUST own an immutable validated track.
        // Do not expose this publicly to untrusted deserialized replay data.
        internal static GhostPose AtValidated(GhostRun run, float seconds)
        {
            Check(Finite(seconds), "Playback time must be finite");
            var frames = run.Frames;
            if (seconds <= 0) return Copy(frames[0].Pose);
            if (seconds >= run.ElapsedSeconds) return Copy(frames[frames.Length - 1].Pose);
            int lo = 0, hi = frames.Length - 1;
            while (lo < hi)
            {
                int mid = lo + (hi - lo + 1) / 2;
                if (frames[mid].Time <= seconds) lo = mid;
                else hi = mid - 1;
            }
            if (lo >= frames.Length - 1) return Copy(frames[lo].Pose);
            var left = frames[lo];
            var right = frames[lo + 1];
            return Interpolate(left.Pose, right.Pose,
                (seconds - left.Time) / (right.Time - left.Time));
        }

        public static GhostRun Clone(GhostRun run)
        {
            Validate(run);
            var result = new GhostRun
            {
                CourseId = run.CourseId,
                RigId = run.RigId,
                Completed = run.Completed,
                RequiredCheckpoints = run.RequiredCheckpoints,
                ElapsedSeconds = run.ElapsedSeconds,
                EndReason = run.EndReason,
                CheckpointSeconds = (float[])run.CheckpointSeconds.Clone(),
                Frames = new GhostFrame[run.Frames.Length]
            };
            for (int i = 0; i < run.Frames.Length; i++)
                result.Frames[i] = new GhostFrame
                {
                    Time = run.Frames[i].Time, Pose = Copy(run.Frames[i].Pose)
                };
            return result;
        }

        public static void Validate(GhostRun run)
        {
            Check(run != null, "Run is missing");
            Check(!string.IsNullOrWhiteSpace(run.CourseId) && run.CourseId.Length <= 128
                && !string.IsNullOrWhiteSpace(run.RigId) && run.RigId.Length <= 128,
                "Invalid course/rig identity");
            Check(Finite(run.ElapsedSeconds) && run.ElapsedSeconds >= 0
                && run.ElapsedSeconds <= MaxDuration, "Invalid run duration");
            Check(run.Frames != null && run.Frames.Length >= 1 && run.Frames.Length <= 16384,
                "Missing or overlong track");
            Check(run.RequiredCheckpoints >= 1 && run.RequiredCheckpoints <= 8
                && run.CheckpointSeconds != null && run.CheckpointSeconds.Length <= run.RequiredCheckpoints,
                "Invalid checkpoint count");
            Check(run.Frames[0] != null && run.Frames[0].Time == 0,
                "First frame must start at zero");
            float prev = -1;
            foreach (var f in run.Frames)
            {
                Check(f != null && Finite(f.Time) && f.Time > prev
                    && f.Time <= run.ElapsedSeconds, "Frames must be unique and increasing");
                Copy(f.Pose);
                prev = f.Time;
            }
            Check(Math.Abs(prev - run.ElapsedSeconds) < .0001f,
                "Final frame must match elapsed time");
            prev = -1;
            foreach (float cp in run.CheckpointSeconds)
            {
                Check(Finite(cp) && cp >= 0 && cp >= prev && cp <= run.ElapsedSeconds,
                    "Checkpoint times invalid or unordered");
                prev = cp;
            }
            Check(!run.Completed || (run.ElapsedSeconds > 0
                && run.CheckpointSeconds.Length == run.RequiredCheckpoints),
                "Completed run needs EVERY configured checkpoint and duration");
            // W14 R8: a recorded failure with all gates reached must not be
            // promoted into a personal best or a ghost simply by toggling
            // Completed while leaving its authoritative terminal reason Failed.
            // The real CHL host remains the authority; this only rejects
            // internally contradictory source records, not forged host proof.
            Check(string.Equals(run.EndReason,
                    run.Completed ? "Completed" : "Failed",
                    StringComparison.Ordinal),
                "Completion flag disagrees with terminal event reason");
        }

        // Never reward equal elapsed time or accept a practice failure as a personal record.
        public static GhostComparison Compare(GhostRun reference, GhostRun trial)
        {
            Validate(reference); Validate(trial);
            Check(reference.Completed && trial.Completed
                && string.Equals(reference.CourseId, trial.CourseId, StringComparison.Ordinal),
                "Only two finished attempts on the SAME course can be compared");
            Check(reference.RequiredCheckpoints == trial.RequiredCheckpoints
                && reference.CheckpointSeconds.Length == trial.CheckpointSeconds.Length,
                "Different checkpoint layouts");
            var splits = new float[reference.CheckpointSeconds.Length];
            for (int i = 0; i < splits.Length; i++)
                splits[i] = trial.CheckpointSeconds[i] - reference.CheckpointSeconds[i];
            return new GhostComparison
            {
                CourseId = reference.CourseId,
                ReferenceRig = reference.RigId,
                TrialRig = trial.RigId,
                SplitDeltaSeconds = splits,
                TotalDeltaSeconds = trial.ElapsedSeconds - reference.ElapsedSeconds
            };
        }
    }

    public sealed class GhostRecorder
    {
        public const float SampleInterval = .10f;
        private readonly string course, rig;
        private readonly int checkpoints;
        private readonly List<GhostFrame> frames = new List<GhostFrame>();
        private readonly List<float> splits = new List<float>();
        private float elapsed, lastFrameAt;
        private bool ended;

        public float ElapsedSeconds => elapsed;
        public int CheckpointsReached => splits.Count;
        public bool Ended => ended;

        public GhostRecorder(string courseId, string rigId, int requiredCheckpoints, GhostPose spawn)
        {
            GhostMath.Check(!string.IsNullOrWhiteSpace(courseId) && courseId.Length <= 128
                && !string.IsNullOrWhiteSpace(rigId) && rigId.Length <= 128,
                "Valid course/rig IDs required");
            GhostMath.Check(requiredCheckpoints >= 1 && requiredCheckpoints <= 8,
                "Required checkpoint count must be 1..8");
            course = courseId; rig = rigId; checkpoints = requiredCheckpoints;
            frames.Add(new GhostFrame { Time = 0, Pose = GhostMath.Copy(spawn) });
        }

        private void Store(GhostPose pose, bool force = false)
        {
            var safe = GhostMath.Copy(pose);
            if (elapsed == lastFrameAt)
            {
                // A zero-time checkpoint is permitted, but do not insert duplicate timestamps.
                frames[frames.Count - 1].Pose = safe;
                return;
            }
            if (force || elapsed - lastFrameAt >= SampleInterval - .000001f)
            {
                GhostMath.Check(frames.Count < 16384, "Recording sample capacity reached");
                frames.Add(new GhostFrame { Time = elapsed, Pose = safe });
                lastFrameAt = elapsed;
            }
        }

        // Host provides unscaled delta, paused flag and actual physics rigidbody pose.
        // The recorder never advances physics or changes challenge completion.
        public void Tick(float unscaledDelta, bool paused, GhostPose actualPose)
        {
            GhostMath.Check(!ended, "Cannot sample a finished/cancelled run");
            GhostMath.Check(!float.IsNaN(unscaledDelta) && !float.IsInfinity(unscaledDelta)
                && unscaledDelta >= 0 && unscaledDelta <= 10, "Invalid frame delta");
            var safe = GhostMath.Copy(actualPose);
            if (paused) return;
            GhostMath.Check(elapsed + unscaledDelta <= GhostMath.MaxDuration,
                "Track exceeded 15 minute time cap");
            elapsed += unscaledDelta;
            Store(safe);
        }

        // Only pass an event that has ALREADY been validated by CHL-003.
        public void Checkpoint(int zeroBasedIndex, GhostPose actualPose)
        {
            GhostMath.Check(!ended && zeroBasedIndex == splits.Count
                && zeroBasedIndex < checkpoints, "Stale/duplicate/early checkpoint");
            var safe = GhostMath.Copy(actualPose);
            Store(safe, true);
            splits.Add(elapsed);
        }

        private GhostRun End(bool completed, string reason, GhostPose actualPose)
        {
            GhostMath.Check(!ended, "Run already ended");
            GhostMath.Check(!completed || (splits.Count == checkpoints && elapsed > 0),
                "Finish before final checkpoint/time");
            var safe = GhostMath.Copy(actualPose);
            Store(safe, true);
            var run = new GhostRun
            {
                CourseId = course, RigId = rig, Completed = completed,
                RequiredCheckpoints = checkpoints,
                ElapsedSeconds = elapsed, EndReason = reason,
                CheckpointSeconds = splits.ToArray(), Frames = frames.ToArray()
            };
            GhostMath.Validate(run);
            ended = true;
            return GhostMath.Clone(run);
        }

        public GhostRun Finish(GhostPose actualPose) => End(true, "Completed", actualPose);
        public GhostRun Fail(GhostPose actualPose) => End(false, "Failed", actualPose);
    }

    /// <summary>
    /// Safe runtime playback: validates and deep-copies ONE track on binding,
    /// then binary-searches its immutable frames in O(log N) per visual frame.
    /// No Unity, CHL, physics, timers, rewards or mutable game references.
    /// </summary>
    public sealed class GhostPlayback
    {
        private readonly GhostRun track;
        public float Duration => track.ElapsedSeconds;
        public string CourseId => track.CourseId;
        public string RigId => track.RigId;

        public GhostPlayback(GhostRun run)
        {
            track = GhostMath.Clone(run);
            GhostMath.Check(track.Completed,
                "Only completed real attempts may be presented as a reference ghost");
        }

        public GhostPose Seek(float seconds)
        {
            return GhostMath.AtValidated(track, seconds);
        }
    }

    public sealed class GhostBestBook
    {
        private readonly Dictionary<string, GhostRun> best =
            new Dictionary<string, GhostRun>(StringComparer.Ordinal);

        private static string Key(GhostRun r) => r.CourseId.Length + ":" + r.CourseId + "/" + r.RigId;

        public bool Submit(GhostRun run)
        {
            GhostMath.Validate(run);
            if (!run.Completed) return false;
            string key = Key(run);
            GhostRun old;
            if (best.TryGetValue(key, out old) && old.ElapsedSeconds <= run.ElapsedSeconds)
                return false;
            best[key] = GhostMath.Clone(run);
            return true;
        }

        public GhostRun For(string courseId, string rigId)
        {
            GhostMath.Check(courseId != null && rigId != null, "IDs required");
            GhostRun run;
            string key = courseId.Length + ":" + courseId + "/" + rigId;
            return best.TryGetValue(key, out run) ? GhostMath.Clone(run) : null;
        }
    }
}
