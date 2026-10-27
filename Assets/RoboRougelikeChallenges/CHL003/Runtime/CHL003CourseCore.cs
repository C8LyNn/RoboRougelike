using System;

namespace B01.CHL003
{
    /// <summary>
    /// Host-neutral checkpoint state machine. Only an already-validated physics
    /// adapter can submit the configured actor token. No R1/R3/MAPGEN, rewards,
    /// garage state, Unity API or static global run state.
    /// </summary>
    public sealed class CHL003CourseCore
    {
        public enum Phase { Unconfigured, Ready, Running, Completed, Failed }
        public enum Failure { None, FellOffCourse, TimeLimit, CancelledByHost }
        public Phase State { get; private set; } = Phase.Unconfigured;
        public Failure LastFailure { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public int CheckpointIndex { get; private set; }
        public int CheckpointCount { get; private set; }
        public int ActorToken { get; private set; }
        public bool Paused { get; private set; }
        public float TimeLimitSeconds { get; private set; }

        public event Action<int> CheckpointReached;
        public event Action<float> Completed;
        public event Action<Failure> Failed;

        public void Configure(int checkpoints, int actorToken, float timeLimitSeconds = 0f)
        {
            if (State == Phase.Running)
                throw new InvalidOperationException("CHL003_CONFIGURE_WHILE_RUNNING");
            if (checkpoints < 3 || checkpoints > 5)
                throw new ArgumentOutOfRangeException(nameof(checkpoints),
                    "CHL003 requires 3–5 ordered checkpoints");
            if (actorToken == 0) throw new ArgumentException("CHL003_ACTOR_ZERO");
            if (float.IsNaN(timeLimitSeconds) || float.IsInfinity(timeLimitSeconds)
                || timeLimitSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(timeLimitSeconds));
            CheckpointCount = checkpoints;
            ActorToken = actorToken;
            TimeLimitSeconds = timeLimitSeconds;
            Retry();
        }

        public bool Begin()
        {
            if (State != Phase.Ready) return false;
            State = Phase.Running;
            return true;
        }

        public bool EnterCheckpoint(int physicsActorToken, int index)
        {
            if (State != Phase.Running || Paused ||
                physicsActorToken != ActorToken || index != CheckpointIndex)
                return false;
            CheckpointIndex++;
            CheckpointReached?.Invoke(CheckpointIndex);
            return true;
        }

        public bool EnterFinish(int physicsActorToken)
        {
            if (State != Phase.Running || Paused || physicsActorToken != ActorToken ||
                CheckpointIndex != CheckpointCount)
                return false;
            State = Phase.Completed;
            Completed?.Invoke(ElapsedSeconds);
            return true;
        }

        public bool Fail(int physicsActorToken, Failure reason)
        {
            if (State != Phase.Running || physicsActorToken != ActorToken ||
                reason == Failure.None) return false;
            State = Phase.Failed;
            LastFailure = reason;
            Failed?.Invoke(reason);
            return true;
        }

        public void SetPaused(bool paused)
        {
            if (State == Phase.Running) Paused = paused;
        }

        public void AdvanceTime(float deltaTimeSeconds)
        {
            if (float.IsNaN(deltaTimeSeconds) || float.IsInfinity(deltaTimeSeconds) ||
                deltaTimeSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTimeSeconds));
            if (State != Phase.Running || Paused) return;
            ElapsedSeconds += deltaTimeSeconds;
            if (TimeLimitSeconds > 0f && ElapsedSeconds >= TimeLimitSeconds)
            {
                ElapsedSeconds = TimeLimitSeconds;
                Fail(ActorToken, Failure.TimeLimit);
            }
        }

        public void Retry()
        {
            if (State == Phase.Unconfigured)
                throw new InvalidOperationException("CHL003_NOT_CONFIGURED");
            State = Phase.Ready;
            LastFailure = Failure.None;
            ElapsedSeconds = 0f;
            CheckpointIndex = 0;
            Paused = false;
        }

        public void Cancel()
        {
            if (State == Phase.Unconfigured) return;
            Retry(); // no completion or reward callback from cancel
        }
    }
}
