using System;

namespace Swarm.B01.Playtesting
{
    /// <summary>
    /// Technical observation only: attempts to move with no significant *planar* displacement.
    /// Does NOT know input mappings, game movement, collisions, vehicle velocity or physics.
    /// No persistent data, global objects, network, IDs or gameplay writes.
    /// </summary>
    public sealed class MotionStallLedger
    {
        public enum State { Idle, Attempting, SuspectedStall, JustRecovered }

        public const double MinimumTravelSpeedMetersPerSecond = 0.10;
        public const double SuspectAfterSeconds = 2.00;
        public const double MaximumSampleGapSeconds = 0.50;
        public const double TeleportDistanceMeters = 50.0;

        private bool started;
        private double previousTime;
        private double previousX;
        private double previousZ;
        private double stillSince;
        private bool attemptActive;

        public State CurrentState { get; private set; }
        public int AttemptCount { get; private set; }
        public int SuspectedStallCount { get; private set; }
        public int RecoveryCount { get; private set; }
        public int DiscontinuityCount { get; private set; }
        public bool Started { get { return started; } }

        public void Begin(double time, double x, double z)
        {
            Validate(time, x, z);
            previousTime = time;
            previousX = x;
            previousZ = z;
            stillSince = time;
            started = true;
            attemptActive = false;
            CurrentState = State.Idle;
            AttemptCount = 0;
            SuspectedStallCount = 0;
            RecoveryCount = 0;
            DiscontinuityCount = 0;
        }

        /// <returns>true only when a new SUSPECTED_STALL event is raised.</returns>
        public bool Observe(double time, double x, double z, bool forwardOrBackHeld)
        {
            if (!started) throw new InvalidOperationException("Call Begin before Observe.");
            Validate(time, x, z);
            if (time < previousTime)
                throw new ArgumentOutOfRangeException("time", "The sample clock moved backwards.");
            double dt = time - previousTime;
            double dx = x - previousX;
            double dz = z - previousZ;
            double distance = Math.Sqrt(dx * dx + dz * dz);
            if (double.IsNaN(distance) || double.IsInfinity(distance))
                throw new ArgumentOutOfRangeException("x", "Unrepresentable world delta.");

            previousTime = time;
            previousX = x;
            previousZ = z;

            if (dt > MaximumSampleGapSeconds || distance > TeleportDistanceMeters)
            {
                // A pause, lost frames, or a teleport must NOT invent a "stuck" event.
                DiscontinuityCount++;
                attemptActive = false;
                stillSince = time;
                CurrentState = State.Idle;
                return false;
            }

            if (!forwardOrBackHeld)
            {
                attemptActive = false;
                stillSince = time;
                CurrentState = State.Idle;
                return false;
            }

            if (!attemptActive)
            {
                attemptActive = true;
                AttemptCount++;
                stillSince = time;
                CurrentState = State.Attempting;
                return false;
            }

            bool moved = dt > 0 && distance / dt >= MinimumTravelSpeedMetersPerSecond;
            if (moved)
            {
                bool recovered = CurrentState == State.SuspectedStall;
                if (recovered) RecoveryCount++;
                stillSince = time;
                CurrentState = recovered ? State.JustRecovered : State.Attempting;
                return false;
            }

            if (CurrentState == State.SuspectedStall)
                return false;
            if (time - stillSince >= SuspectAfterSeconds)
            {
                SuspectedStallCount++;
                CurrentState = State.SuspectedStall;
                return true;
            }
            CurrentState = State.Attempting;
            return false;
        }

        private static void Validate(double time, double x, double z)
        {
            if (double.IsNaN(time) || double.IsInfinity(time) || time < 0 ||
                double.IsNaN(x) || double.IsInfinity(x) ||
                double.IsNaN(z) || double.IsInfinity(z))
                throw new ArgumentOutOfRangeException("sample", "All sample values must be finite and time nonnegative.");
        }
    }
}
