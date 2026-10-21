using UnityEngine;
using UnityEngine.Events;

namespace RoboRougelike.Mechanics.RAC001
{
    /// <summary>Optional timed course; reads a vehicle Transform without controlling it.</summary>
    [DisallowMultipleComponent]
    public sealed class CheckpointRaceChallenge : MonoBehaviour
    {
        [SerializeField] private Transform runner;
        [SerializeField] private Transform[] checkpoints = new Transform[0];
        [SerializeField, Min(0.1f)] private float radiusMeters = 3f;
        [SerializeField, Min(1f)] private float timeLimitSeconds = 120f;
        [SerializeField] private UnityEvent onCheckpoint = new UnityEvent();
        [SerializeField] private UnityEvent onWon = new UnityEvent();
        [SerializeField] private UnityEvent onTimedOut = new UnityEvent();

        public bool Running { get; private set; }
        public int NextCheckpoint { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public bool Won { get; private set; }

        public void BindRunner(Transform value) => runner = value;

        public bool Begin()
        {
            if (!isActiveAndEnabled || runner == null || checkpoints == null || checkpoints.Length == 0)
                return false;
            foreach (var checkpoint in checkpoints)
                if (checkpoint == null) return false;
            ResetRace();
            Running = true;
            return true;
        }

        public void ResetRace()
        {
            Running = false;
            Won = false;
            NextCheckpoint = 0;
            ElapsedSeconds = 0f;
        }

        private void Update()
        {
            if (!Running) return;
            ElapsedSeconds += Time.deltaTime;
            if (ElapsedSeconds >= timeLimitSeconds)
            {
                Running = false;
                onTimedOut.Invoke();
                return;
            }
            if (runner == null || checkpoints[NextCheckpoint] == null) { Running = false; return; }
            float radiusSq = radiusMeters * radiusMeters;
            if ((runner.position - checkpoints[NextCheckpoint].position).sqrMagnitude > radiusSq) return;
            NextCheckpoint++;
            onCheckpoint.Invoke();
            if (NextCheckpoint < checkpoints.Length) return;
            Won = true;
            Running = false;
            onWon.Invoke();
        }
    }
}
