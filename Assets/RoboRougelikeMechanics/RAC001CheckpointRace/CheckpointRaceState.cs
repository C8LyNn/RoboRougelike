using UnityEngine;

namespace RoboRougelike.Mechanics.RAC001
{
    public enum RacePhase { Idle, Running, Won, TimedOut, Invalidated }
    public enum RaceStepResult { None, Checkpoint, Won, TimedOut, Invalidated }

    /// <summary>Deterministic, opt-in checkpoint rules. No vehicle or scene dependencies.</summary>
    public sealed class CheckpointRaceState
    {
        private Vector3[] course = new Vector3[0];
        private Vector3 previousPosition;
        private float radiusSquared;
        private float timeLimit;
        private float maxSpeed;
        private bool entryArmed;

        public RacePhase Phase { get; private set; } = RacePhase.Idle;
        public int NextCheckpoint { get; private set; }
        public int CheckpointCount => course.Length;
        public float ElapsedSeconds { get; private set; }

        public bool Begin(Vector3 start, Vector3[] checkpoints, float radius, float limit, float maximumSpeed = 250f)
        {
            Reset();
            if (!Finite(start) || checkpoints == null || checkpoints.Length == 0 ||
                !Finite(radius) || radius <= 0f || !Finite(limit) || limit <= 0f ||
                !Finite(maximumSpeed) || maximumSpeed <= 0f)
                return false;

            for (int i = 0; i < checkpoints.Length; i++)
                if (!Finite(checkpoints[i])) return false;

            course = (Vector3[])checkpoints.Clone(); // Stable route for one attempt.
            radiusSquared = radius * radius;
            timeLimit = limit;
            maxSpeed = maximumSpeed;
            previousPosition = start;
            entryArmed = (start - course[0]).sqrMagnitude > radiusSquared;
            Phase = RacePhase.Running;
            return true;
        }

        public RaceStepResult Advance(Vector3 position, float deltaSeconds)
        {
            if (Phase != RacePhase.Running) return RaceStepResult.None;
            if (!Finite(position) || !Finite(deltaSeconds) || deltaSeconds < 0f)
            {
                Phase = RacePhase.Invalidated;
                return RaceStepResult.Invalidated;
            }

            // Zero-time ticks cannot advance movement or score checkpoints.
            if (deltaSeconds == 0f) return RaceStepResult.None;

            ElapsedSeconds += deltaSeconds;
            if (ElapsedSeconds >= timeLimit)
            {
                ElapsedSeconds = timeLimit;
                Phase = RacePhase.TimedOut;
                return RaceStepResult.TimedOut;
            }

            // Reject implausible jumps. Landing directly inside a checkpoint never arms it.
            // A fixed per-tick allowance can be exploited by tiny time steps.
            // Tolerance must scale with the simulated time, not the call count.
            float allowedTravel = maxSpeed * deltaSeconds * 1.05f;
            if ((position - previousPosition).sqrMagnitude > allowedTravel * allowedTravel)
            {
                previousPosition = position;
                entryArmed = false;
                return RaceStepResult.None;
            }

            Vector3 target = course[NextCheckpoint];
            if (!entryArmed)
            {
                if ((position - target).sqrMagnitude > radiusSquared) entryArmed = true;
                previousPosition = position;
                return RaceStepResult.None;
            }

            bool entered = SegmentHitsSphere(previousPosition, position, target, radiusSquared);
            previousPosition = position;
            if (!entered) return RaceStepResult.None;

            NextCheckpoint++;
            if (NextCheckpoint >= course.Length)
            {
                Phase = RacePhase.Won;
                entryArmed = false;
                return RaceStepResult.Won;
            }

            // Overlapping checkpoints require a fresh exit before the next entry.
            entryArmed = (position - course[NextCheckpoint]).sqrMagnitude > radiusSquared;
            return RaceStepResult.Checkpoint;
        }

        public void Invalidate()
        {
            if (Phase == RacePhase.Running) Phase = RacePhase.Invalidated;
        }

        public void Reset()
        {
            Phase = RacePhase.Idle;
            NextCheckpoint = 0;
            ElapsedSeconds = 0f;
            course = new Vector3[0];
            previousPosition = Vector3.zero;
            entryArmed = false;
            radiusSquared = 0f;
            timeLimit = 0f;
            maxSpeed = 0f;
        }

        private static bool SegmentHitsSphere(Vector3 a, Vector3 b, Vector3 center, float radiusSq)
        {
            Vector3 delta = b - a;
            float lengthSq = delta.sqrMagnitude;
            float t = lengthSq > 0.000001f ? Mathf.Clamp01(Vector3.Dot(center - a, delta) / lengthSq) : 0f;
            return (a + t * delta - center).sqrMagnitude <= radiusSq;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
    }
}

