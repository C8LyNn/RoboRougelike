using UnityEngine;
using UnityEngine.Events;

namespace RoboRougelike.Mechanics.RAC001
{
    /// <summary>Opt-in race; observes a Transform and never controls the vehicle.</summary>
    [DisallowMultipleComponent]
    public sealed class CheckpointRaceChallenge : MonoBehaviour
    {
        [SerializeField] private Transform runner;
        [SerializeField] private Transform[] checkpoints = new Transform[0];
        [SerializeField, Min(0.1f)] private float radiusMeters = 3f;
        [SerializeField, Min(1f)] private float timeLimitSeconds = 120f;
        [SerializeField, Min(1f)] private float maximumTravelMetersPerSecond = 250f;
        [SerializeField] private UnityEvent onCheckpoint = new UnityEvent();
        [SerializeField] private UnityEvent onWon = new UnityEvent();
        [SerializeField] private UnityEvent onTimedOut = new UnityEvent();
        [SerializeField] private UnityEvent onInvalidated = new UnityEvent();

        private readonly CheckpointRaceState state = new CheckpointRaceState();
        private Rigidbody observedBody;
        public RacePhase Phase => state.Phase;
        public bool Running => Phase == RacePhase.Running;
        public bool Won => Phase == RacePhase.Won;
        public int NextCheckpoint => state.NextCheckpoint;
        public int CheckpointCount => state.CheckpointCount;
        public float ElapsedSeconds => state.ElapsedSeconds;
        public UnityEvent OnCheckpoint => onCheckpoint;
        public UnityEvent OnWon => onWon;
        public UnityEvent OnTimedOut => onTimedOut;

        public void BindRunner(Transform value)
        { runner=value; observedBody=value!=null?value.GetComponent<Rigidbody>():null; }

        /// <summary>Opt-in configuration for isolated demos and tests; never edits assets.</summary>
        public void Configure(Transform value, Transform[] course, float radius, float seconds, float speed = 250f)
        {
            ResetRace();
            BindRunner(value);
            checkpoints = course;
            radiusMeters = radius;
            timeLimitSeconds = seconds;
            maximumTravelMetersPerSecond = speed;
        }

        public bool Begin()
        {
            if (!isActiveAndEnabled || runner == null || checkpoints == null || checkpoints.Length == 0)
                return false;
            Vector3[] points = new Vector3[checkpoints.Length];
            for (int i = 0; i < points.Length; i++)
            {
                if (checkpoints[i] == null) return false;
                points[i] = checkpoints[i].position;
            }
            return state.Begin(runner.position, points, radiusMeters, timeLimitSeconds, maximumTravelMetersPerSecond);
        }

        public void ResetRace() => state.Reset();

        // Observe physics motion on its own clock. A render frame can have a tiny
        // delta after a fixed body step; comparing that step with render delta
        // falsely triggers the movement guard and disarms a genuine checkpoint.
        private void Update() { if(observedBody==null)Advance(Time.deltaTime); }
        private void FixedUpdate() { if(observedBody!=null)Advance(Time.fixedDeltaTime); }

        /// <summary>Manual deterministic step for a separate adapter/test; never mutates runner.</summary>
        public RaceStepResult Advance(float deltaSeconds)
        {
            if (!Running) return RaceStepResult.None;
            if (runner == null)
            {
                state.Invalidate();
                onInvalidated.Invoke();
                return RaceStepResult.Invalidated;
            }
            RaceStepResult result = state.Advance(observedBody!=null?observedBody.position:runner.position, deltaSeconds);
            if (result == RaceStepResult.Checkpoint || result == RaceStepResult.Won)
                onCheckpoint.Invoke();
            if (result == RaceStepResult.Won) onWon.Invoke();
            else if (result == RaceStepResult.TimedOut) onTimedOut.Invoke();
            else if (result == RaceStepResult.Invalidated) onInvalidated.Invoke();
            return result;
        }
    }
}

