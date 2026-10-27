using System;
using UnityEngine;

namespace B01.CHL003
{
    /// <summary>
    /// Unity Physics-bound adapter for the independent host-neutral engine.
    /// Only active colliders attached to the EXACT configured Rigidbody may
    /// trip a checkpoint; no game host controls, money, AI or rewards.
    /// </summary>
    public sealed class CHL003CourseController : MonoBehaviour
    {
        private CHL003CourseCore core;
        private Rigidbody activeActor;
        private CHL003Gate[] checkpoints;
        private CHL003Gate finish;
        private Transform spawn;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float failBelowY = -3f;
        public CHL003CourseCore Core => core;
        public Rigidbody Actor => activeActor;
        public bool IsPaused => core != null && core.Paused;

        public void Configure(Rigidbody actor, CHL003Gate[] ordered,
            CHL003Gate finishGate, Transform spawnPoint,
            float failureY = -3f, float timeLimitSeconds = 0f)
        {
            if (core != null && core.State == CHL003CourseCore.Phase.Running)
                throw new InvalidOperationException("CHL003_CONFIGURE_WHILE_RUNNING");
            if (actor == null || actor.isKinematic || actor.gameObject == null ||
                ordered == null || ordered.Length < 3 || ordered.Length > 5 ||
                finishGate == null || spawnPoint == null || float.IsNaN(failureY))
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
            activeActor = actor;
            checkpoints = (CHL003Gate[])ordered.Clone();
            finish = finishGate;
            spawn = spawnPoint;
            spawnPosition = spawnPoint.position;
            spawnRotation = spawnPoint.rotation;
            failBelowY = failureY;
            core = new CHL003CourseCore();
            core.Configure(ordered.Length, actor.GetInstanceID(), timeLimitSeconds);
            for (int i = 0; i < checkpoints.Length; i++)
                checkpoints[i].Bind(this, i);
            finish.Bind(this, -1);
        }

        public bool Begin() => core != null && core.Begin();
        public void SetPaused(bool paused) => core?.SetPaused(paused);

        public void Retry()
        {
            if (core == null || activeActor == null) return;
            core.Retry();
            activeActor.linearVelocity = Vector3.zero;
            activeActor.angularVelocity = Vector3.zero;
            activeActor.position = spawnPosition;
            activeActor.rotation = spawnRotation;
            activeActor.Sleep();
            Physics.SyncTransforms();
        }

        public void Cancel()
        {
            if (core == null) return;
            Retry();
            core.Cancel();
        }

        internal bool ReceiveGate(CHL003Gate gate, Collider collider, int ordinal)
        {
            if (core == null || collider == null || !collider.enabled ||
                !collider.gameObject.activeInHierarchy ||
                collider.attachedRigidbody != activeActor)
                return false;
            // Direct identity from the Unity Physics Rigidbody, never from tag,
            // string name or child collider instance (multi-collider duplicates).
            int token = activeActor.GetInstanceID();
            if (ReferenceEquals(gate, finish))
                return core.EnterFinish(token);
            if (ordinal < 0 || ordinal >= checkpoints.Length ||
                !ReferenceEquals(gate, checkpoints[ordinal]))
                return false;
            return core.EnterCheckpoint(token, ordinal);
        }

        private void Update()
        {
            if (core == null) return;
            core.AdvanceTime(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (core == null || core.State != CHL003CourseCore.Phase.Running ||
                activeActor == null) return;
            if (activeActor.position.y < failBelowY)
                core.Fail(activeActor.GetInstanceID(),
                    CHL003CourseCore.Failure.FellOffCourse);
        }
    }
}
