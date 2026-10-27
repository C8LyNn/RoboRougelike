using System;
using UnityEngine;

namespace B01.CHL003
{
    /// <summary>
    /// CHL-only Unity Physics adapter. The same-step finish is deferred to
    /// the following FixedUpdate AND requires actual current overlap.
    /// Does not modify R3, W8, inventory, reward or active Codex play.
    /// </summary>
    public sealed class CHL003CourseController : MonoBehaviour
    {
        private CHL003CourseCore core;
        private Rigidbody activeActor;
        private CHL003Gate[] checkpoints;
        private CHL003Gate finish;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float failBelowY = -3f;
        private long fixedStep;
        private int attempt;
        private float nominalWidth, nominalLength;
        private string profile;
        private readonly CHL003FinishArbiter finishArbiter = new CHL003FinishArbiter();
        private readonly CHL003TrialMetrics metrics = new CHL003TrialMetrics();
        public CHL003CourseCore Core => core;
        public Rigidbody Actor => activeActor;
        public bool IsPaused => core != null && core.Paused;
        public CHL003TrialMetrics Metrics => metrics;
        public bool FinishPending => finishArbiter.IsQueued;
        public long PhysicsStep => fixedStep;

        public void Configure(Rigidbody actor, CHL003Gate[] ordered,
            CHL003Gate finishGate, Transform spawnPoint,
            float failureY = -3f, float timeLimitSeconds = 0f,
            string rigProfile = "LIGHT", float widthMeters = 1f,
            float lengthMeters = 1f, int trialNumber = 1)
        {
            if (core != null && core.State == CHL003CourseCore.Phase.Running)
                throw new InvalidOperationException("CHL003_CONFIGURE_WHILE_RUNNING");
            if (actor == null || actor.isKinematic || actor.gameObject == null ||
                ordered == null || ordered.Length < 3 || ordered.Length > 5 ||
                finishGate == null || spawnPoint == null ||
                float.IsNaN(failureY) || float.IsInfinity(failureY))
                throw new ArgumentException("CHL003_INVALID_PHYSICAL_CONFIGURATION");
            for (int i = 0; i < ordered.Length; i++)
            {
                if (ordered[i] == null || !ordered[i].HasTrigger ||
                    ordered[i].Type != CHL003Gate.GateType.Checkpoint)
                    throw new ArgumentException("CHL003_INVALID_ORDERED_GATE");
                for (int j = 0; j < i; j++)
                    if (ReferenceEquals(ordered[i], ordered[j]))
                        throw new ArgumentException("CHL003_DUPLICATE_GATE_REFERENCE");
            }
            if (!finishGate.HasTrigger || finishGate.Type != CHL003Gate.GateType.Finish)
                throw new ArgumentException("CHL003_INVALID_FINISH_TRIGGER");
            // Keep side-effect-free reject on bad telemetry inputs.
            var scratch = new CHL003TrialMetrics();
            scratch.Reset(rigProfile, trialNumber, actor.mass,
                widthMeters, lengthMeters);
            activeActor = actor;
            checkpoints = (CHL003Gate[])ordered.Clone();
            finish = finishGate;
            spawnPosition = spawnPoint.position;
            spawnRotation = spawnPoint.rotation;
            failBelowY = failureY;
            core = new CHL003CourseCore();
            core.Configure(ordered.Length, actor.GetInstanceID(), timeLimitSeconds);
            core.CheckpointReached += reached =>
                metrics.RecordCheckpoint(reached, core.ElapsedSeconds);
            core.Completed += elapsed =>
                metrics.RecordCompleted(elapsed);
            core.Failed += reason =>
                metrics.RecordFailed(reason.ToString(), core.ElapsedSeconds);
            attempt = trialNumber;
            profile = rigProfile;
            nominalWidth = widthMeters;
            nominalLength = lengthMeters;
            metrics.Reset(profile, attempt, activeActor.mass,
                nominalWidth, nominalLength);
            finishArbiter.Reset();
            fixedStep = 0;
            for (int i = 0; i < checkpoints.Length; i++)
                checkpoints[i].Bind(this, i);
            finish.Bind(this, -1);
        }

        public bool Begin()
        {
            if (core == null || !core.Begin()) return false;
            metrics.Begin();
            return true;
        }

        public void SetPaused(bool paused)
        {
            if (core == null) return;
            core.SetPaused(paused);
            if (paused) finishArbiter.CancelPending();
        }

        public void Retry()
        {
            if (core == null || activeActor == null) return;
            finishArbiter.Reset();
            core.Retry();
            activeActor.linearVelocity = Vector3.zero;
            activeActor.angularVelocity = Vector3.zero;
            activeActor.position = spawnPosition;
            activeActor.rotation = spawnRotation;
            activeActor.Sleep();
            Physics.SyncTransforms();
            attempt++;
            metrics.Reset(profile, attempt, activeActor.mass,
                nominalWidth, nominalLength);
        }

        public void Cancel()
        {
            if (core == null) return;
            Retry();
            core.Cancel();
        }

        internal void RecordContact(Collision collision, bool entering)
        {
            if (core == null || core.State != CHL003CourseCore.Phase.Running ||
                core.Paused || collision == null) return;
            string otherName = collision.collider != null ?
                collision.collider.gameObject.name : "UNNAMED";
            metrics.RecordCollision(otherName, collision.contactCount, entering);
        }

        internal bool ReceiveGate(CHL003Gate gate, Collider collider, int ordinal)
        {
            if (core == null || core.State != CHL003CourseCore.Phase.Running ||
                core.Paused || collider == null || !collider.enabled ||
                !collider.gameObject.activeInHierarchy ||
                collider.attachedRigidbody != activeActor)
                return false;
            int token = activeActor.GetInstanceID();
            if (ReferenceEquals(gate, finish))
            {
                // Last-CP and Finish callbacks may occur in either order
                // in the same physics tick. Never fire Completed inline.
                return finishArbiter.ObserveFinishContact(fixedStep,
                    core.CheckpointIndex >= core.CheckpointCount - 1);
            }
            if (ordinal < 0 || ordinal >= checkpoints.Length ||
                !ReferenceEquals(gate, checkpoints[ordinal]))
                return false;
            bool accepted = core.EnterCheckpoint(token, ordinal);
            if (accepted && core.CheckpointIndex == core.CheckpointCount)
                finishArbiter.LastCheckpointAccepted(fixedStep);
            return accepted;
        }

        private bool PhysicallyOverlapsFinish()
        {
            if (activeActor == null || finish == null) return false;
            var trigger = finish.GetComponent<BoxCollider>();
            if (trigger == null || !trigger.enabled || !trigger.isTrigger) return false;
            // Actual narrow-phase overlap, not Collider.bounds AABB proximity.
            var colliders = activeActor.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                var other = colliders[i];
                if (other == null || !other.enabled || other.isTrigger ||
                    other.attachedRigidbody != activeActor) continue;
                if (Physics.ComputePenetration(
                    trigger, trigger.transform.position, trigger.transform.rotation,
                    other, other.transform.position, other.transform.rotation,
                    out _, out _))
                    return true;
            }
            return false;
        }

        private void Update()
        {
            if (core == null) return;
            core.AdvanceTime(Time.deltaTime);
            if (core.State != CHL003CourseCore.Phase.Running)
                finishArbiter.CancelPending();
        }

        private void FixedUpdate()
        {
            fixedStep++;
            if (core == null || core.State != CHL003CourseCore.Phase.Running ||
                activeActor == null || core.Paused) return;
            Vector3 pos = activeActor.position;
            metrics.RecordPhysics(pos.x, pos.y, pos.z,
                activeActor.linearVelocity.magnitude, core.ElapsedSeconds);
            if (pos.y < failBelowY)
            {
                core.Fail(activeActor.GetInstanceID(),
                    CHL003CourseCore.Failure.FellOffCourse);
                finishArbiter.CancelPending();
                return;
            }
            // A real actor must STILL physically intersect the finish trigger
            // after the CP-last acceptance tick; stale/early/retry callbacks
            // cannot win. Never synthesize a Completed event from elapsed alone.
            if (finishArbiter.IsQueued &&
                finishArbiter.TryCommit(fixedStep,
                    core.CheckpointIndex == core.CheckpointCount,
                    PhysicallyOverlapsFinish(), core.Paused))
                core.EnterFinish(activeActor.GetInstanceID());
        }
    }
}
