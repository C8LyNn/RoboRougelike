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
        private Vector3 positionBeforePhysics;
        private Quaternion spawnRotation;
        private float failBelowY = -3f;
        private long fixedStep;
        private int attempt;
        private float nominalWidth, nominalLength;
        private string profile;
        private Vector3 pausedVelocity, pausedAngularVelocity;
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
            positionBeforePhysics = actor.position;
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
            if (core == null || activeActor == null ||
                core.State != CHL003CourseCore.Phase.Running ||
                core.Paused == paused) return;
            if (paused)
            {
                pausedVelocity = activeActor.linearVelocity;
                pausedAngularVelocity = activeActor.angularVelocity;
                activeActor.isKinematic = true; // real physics pause, not HUD-only
                core.SetPaused(true);
                finishArbiter.CancelPending();
            }
            else
            {
                activeActor.isKinematic = false;
                activeActor.linearVelocity = pausedVelocity;
                activeActor.angularVelocity = pausedAngularVelocity;
                positionBeforePhysics = activeActor.position;
                core.SetPaused(false);
            }
        }

        public void Retry()
        {
            if (core == null || activeActor == null) return;
            finishArbiter.Reset();
            core.Retry();
            if (activeActor.isKinematic) activeActor.isKinematic = false;
            pausedVelocity = pausedAngularVelocity = Vector3.zero;
            activeActor.linearVelocity = Vector3.zero;
            activeActor.angularVelocity = Vector3.zero;
            activeActor.position = spawnPosition;
            positionBeforePhysics = spawnPosition;
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

        private bool ForwardHullCrossesGate(CHL003Gate gate)
        {
            if (activeActor == null || gate == null || gate.transform == null)
                return false;
            var box = gate.GetComponent<BoxCollider>();
            if (box == null || !box.enabled || !box.isTrigger)
                return false;
            Transform g = gate.transform;
            Vector3 unit = g.lossyScale;
            float sx = Mathf.Abs(unit.x), sy = Mathf.Abs(unit.y);
            float sz = Mathf.Abs(unit.z);
            if (sx <= 0f || sy <= 0f || sz <= 0f) return false;
            Vector3 previous = g.InverseTransformPoint(positionBeforePhysics);
            Vector3 current = g.InverseTransformPoint(activeActor.position);
            var actorColliders = activeActor.GetComponentsInChildren<Collider>();
            bool any = false;
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            for (int i = 0; i < actorColliders.Length; i++)
            {
                Collider c = actorColliders[i];
                if (c == null || !c.enabled || c.isTrigger ||
                    !c.gameObject.activeInHierarchy ||
                    c.attachedRigidbody != activeActor) continue;
                Bounds worldAabb = c.bounds; // actual COLLIDER bounds, not render mesh
                if (worldAabb.size.sqrMagnitude <= 0f) continue;
                // Conservative world AABB corners transformed to the local gate
                // frame. Yaw can only make this HARDER, never false accept.
                for (int x=-1;x<=1;x+=2)
                for (int y=-1;y<=1;y+=2)
                for (int z=-1;z<=1;z+=2)
                {
                    Vector3 corner = worldAabb.center +
                        Vector3.Scale(worldAabb.extents, new Vector3(x,y,z));
                    Vector3 v = g.InverseTransformPoint(corner);
                    minX = Mathf.Min(minX, v.x*sx);
                    maxX = Mathf.Max(maxX, v.x*sx);
                    minY = Mathf.Min(minY, v.y*sy);
                    maxY = Mathf.Max(maxY, v.y*sy);
                }
                any = true;
            }
            if (!any) return false;
            return CHL003GateCrossing.IsForwardFullBodyCrossing(
                previous.z*sz, current.z*sz, minX, maxX, minY, maxY,
                (box.center.x-box.size.x*.5f)*sx,
                (box.center.x+box.size.x*.5f)*sx,
                (box.center.y-box.size.y*.5f)*sy,
                (box.center.y+box.size.y*.5f)*sy,
                Vector3.Dot(activeActor.transform.forward,g.forward));
        }

        internal bool ReceiveGate(CHL003Gate gate, Collider collider, int ordinal)
        {
            if (core == null || core.State != CHL003CourseCore.Phase.Running ||
                core.Paused || collider == null || !collider.enabled ||
                !collider.gameObject.activeInHierarchy ||
                collider.attachedRigidbody != activeActor)
                return false;
            // Trigger callbacks ONLY propose candidates. The real actor center
            // must cross the gate plane forward, and the WHOLE real physics
            // collider envelope must clear its aperture. Neither reversing
            // nor a single wheel grazing the volume can score.
            if (!ForwardHullCrossesGate(gate)) return false;
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

        private void FixedUpdate()
        {
            fixedStep++;
            // Sole authoritative trial clock: exactly one fixed physics
            // delta per simulated step, never once per rendered Update frame.
            // This preserves fair elapsed times at 30/60/144 FPS.
            if (core != null && core.State == CHL003CourseCore.Phase.Running &&
                !core.Paused)
                core.AdvanceTime(Time.fixedDeltaTime);
            if (core != null && core.State != CHL003CourseCore.Phase.Running)
                finishArbiter.CancelPending();
            if (activeActor != null && core != null && !core.Paused)
                positionBeforePhysics = activeActor.position;
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
