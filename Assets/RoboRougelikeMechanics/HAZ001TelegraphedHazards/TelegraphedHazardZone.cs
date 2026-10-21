using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RoboRougelike.Mechanics.HAZ001
{
    public enum HazardPhase { Idle, Telegraph, Active, Cooldown }

    /// <summary>
    /// Opt-in arena hazard. Emits damage REQUESTS only; never edits HP, vehicle parts,
    /// physics, encounter managers, or the active game scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TelegraphedHazardZone : MonoBehaviour
    {
        [Header("Cycle")]
        [SerializeField, Min(0.05f)] private float warningSeconds = 1.5f;
        [SerializeField, Min(0.05f)] private float activeSeconds = 2f;
        [SerializeField, Min(0.05f)] private float cooldownSeconds = 3f;
        [Header("Area / Requests")]
        [SerializeField, Min(0.1f)] private float radius = 3f;
        [SerializeField, Min(0.01f)] private float hitInterval = 0.5f;
        [SerializeField, Min(0f)] private float requestedDamage = 5f;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private QueryTriggerInteraction triggerPolicy = QueryTriggerInteraction.Ignore;
        [Header("Inspector events")]
        [SerializeField] private UnityEvent onWarning = new UnityEvent();
        [SerializeField] private UnityEvent onActive = new UnityEvent();
        [SerializeField] private UnityEvent onCooldown = new UnityEvent();
        [SerializeField] private UnityEvent onReady = new UnityEvent();

        // Reused buffers: no per-scan allocations from the overlap query.
        private readonly Collider[] overlapBuffer = new Collider[64];
        private readonly HashSet<int> seenThisScan = new HashSet<int>();
        private readonly Dictionary<int, float> nextAllowedTime = new Dictionary<int, float>();

        public HazardPhase Phase { get; private set; } = HazardPhase.Idle;
        public float PhaseElapsed { get; private set; }
        public bool IsDangerous => Phase == HazardPhase.Active;
        public float Radius => radius;
        public int TotalDamageRequests { get; private set; }
        public bool LastScanTruncated { get; private set; }
        public event Action<HazardPhase> PhaseChanged;
        public event Action<GameObject, float> DamageRequested;

        public void Configure(float warning, float active, float cooldown,
            float areaRadius, float interval, float damage, LayerMask layers)
        {
            warningSeconds = SafePositive(warning, 0.05f);
            activeSeconds = SafePositive(active, 0.05f);
            cooldownSeconds = SafePositive(cooldown, 0.05f);
            radius = SafePositive(areaRadius, 0.1f);
            hitInterval = SafePositive(interval, 0.01f);
            requestedDamage = SafeNonnegative(damage);
            targetLayers = layers;
        }

        public bool Begin()
        {
            if (!isActiveAndEnabled || Phase != HazardPhase.Idle) return false;
            Transition(HazardPhase.Telegraph);
            return true;
        }

        /// <summary>Silent cancellation. Does not emit an onReady event.</summary>
        public void ResetCycle()
        {
            Phase = HazardPhase.Idle;
            PhaseElapsed = 0f;
            nextAllowedTime.Clear();
            seenThisScan.Clear();
            LastScanTruncated = false;
        }

        private void OnDisable() => ResetCycle();

        private void Update() => ManualTick(Time.deltaTime);

        /// <summary>Deterministic timeline step for tests and manual driving.</summary>
        public void ManualTick(float deltaSeconds)
        {
            if (!isActiveAndEnabled || Phase == HazardPhase.Idle ||
                float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) ||
                deltaSeconds <= 0f) return;

            float remaining = deltaSeconds;
            // A single cycle has at most three transitions, even for a huge frame.
            for (int step = 0; step < 3 && Phase != HazardPhase.Idle; step++)
            {
                float duration = Phase == HazardPhase.Telegraph ? warningSeconds :
                    Phase == HazardPhase.Active ? activeSeconds : cooldownSeconds;
                float left = Mathf.Max(0f, duration - PhaseElapsed);
                if (remaining < left)
                {
                    PhaseElapsed += remaining;
                    return;
                }
                remaining -= left;
                Transition(Phase == HazardPhase.Telegraph ? HazardPhase.Active :
                    Phase == HazardPhase.Active ? HazardPhase.Cooldown : HazardPhase.Idle);
            }
        }

        private void FixedUpdate()
        {
            if (Phase == HazardPhase.Active)
                ScanAtTime(Time.time);
        }

        /// <summary>
        /// Emit at most one request per eligible target per hitInterval. A request
        /// does not apply damage: subscribers explicitly own that decision.
        /// </summary>
        public int ScanAtTime(float now)
        {
            if (!isActiveAndEnabled || Phase != HazardPhase.Active ||
                float.IsNaN(now) || float.IsInfinity(now)) return 0;

            int count = Physics.OverlapSphereNonAlloc(transform.position, radius,
                overlapBuffer, targetLayers, triggerPolicy);
            LastScanTruncated = count == overlapBuffer.Length;
            seenThisScan.Clear();
            int emitted = 0;
            for (int i = 0; i < count; i++)
            {
                Collider hit = overlapBuffer[i];
                if (hit == null || hit.transform.IsChildOf(transform)) continue;
                GameObject target = hit.attachedRigidbody != null ?
                    hit.attachedRigidbody.gameObject : hit.gameObject;
                int id = target.GetInstanceID();
                if (!seenThisScan.Add(id)) continue;
                if (nextAllowedTime.TryGetValue(id, out float next) && now < next)
                    continue;

                nextAllowedTime[id] = now + hitInterval;
                TotalDamageRequests++;
                emitted++;
                DamageRequested?.Invoke(target, requestedDamage);
            }
            return emitted;
        }

        private void Transition(HazardPhase next)
        {
            Phase = next;
            PhaseElapsed = 0f;
            if (next == HazardPhase.Active)
            {
                nextAllowedTime.Clear();
                TotalDamageRequests = 0;
            }
            switch (next)
            {
                case HazardPhase.Telegraph: onWarning.Invoke(); break;
                case HazardPhase.Active: onActive.Invoke(); break;
                case HazardPhase.Cooldown: onCooldown.Invoke(); break;
                case HazardPhase.Idle: onReady.Invoke(); break;
            }
            PhaseChanged?.Invoke(next);
        }

        private void OnValidate()
        {
            warningSeconds = SafePositive(warningSeconds, 0.05f);
            activeSeconds = SafePositive(activeSeconds, 0.05f);
            cooldownSeconds = SafePositive(cooldownSeconds, 0.05f);
            radius = SafePositive(radius, 0.1f);
            hitInterval = SafePositive(hitInterval, 0.01f);
            requestedDamage = SafeNonnegative(requestedDamage);
        }

        private static float SafePositive(float value, float minimum) =>
            float.IsNaN(value) || float.IsInfinity(value) ? minimum : Mathf.Max(minimum, value);
        private static float SafeNonnegative(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Phase == HazardPhase.Active ? Color.red :
                Phase == HazardPhase.Telegraph ? Color.yellow : Color.gray;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
