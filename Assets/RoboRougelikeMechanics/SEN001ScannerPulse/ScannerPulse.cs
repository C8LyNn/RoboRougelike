using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RoboRougelike.Mechanics.SEN001
{
    /// <summary>Manual, opt-in and read-only 3D physics pulse. No input or gameplay dependencies.</summary>
    [DisallowMultipleComponent]
    public sealed class ScannerPulse : MonoBehaviour
    {
        private const int QueryCapacity = 128;
        [SerializeField, Min(0.1f)] private float radiusMeters = 20f;
        [SerializeField, Min(0f)] private float cooldownSeconds = 5f;
        [SerializeField, Min(0f)] private float revealSeconds = 3f;
        [SerializeField] private LayerMask detectableLayers = ~0;
        [SerializeField] private ScannerSignalMask detectableSignals = ScannerSignalMask.All;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
        [SerializeField] private bool ignoreOwnHierarchy = true;
        [SerializeField] private UnityEvent onPulse = new UnityEvent();

        private readonly Collider[] queryBuffer = new Collider[QueryCapacity];
        private readonly List<Collider> lastHits = new List<Collider>(QueryCapacity);
        private readonly List<ScannerDetectable> revealed = new List<ScannerDetectable>(QueryCapacity);
        private readonly Dictionary<ScannerDetectable, float> expiry = new Dictionary<ScannerDetectable, float>(QueryCapacity);
        private readonly HashSet<ScannerDetectable> seenThisPulse = new HashSet<ScannerDetectable>();
        private float nextAllowedTime;

        public float RadiusMeters => Mathf.Max(0.1f, radiusMeters);
        public ScannerSignalMask DetectableSignals => detectableSignals;
        public float RemainingCooldown => Mathf.Max(0f, nextAllowedTime - Time.time);
        public bool Ready => isActiveAndEnabled && RemainingCooldown <= 0f;
        public bool LastPulseTruncated { get; private set; }
        public IReadOnlyList<Collider> LastHits => lastHits;
        /// <summary>Live reveal list; expired entries are pruned in Update or on the next pulse.</summary>
        public IReadOnlyList<ScannerDetectable> Revealed => revealed;
        public event Action<ScannerDetectable> OnDetected;

        public void Configure(float radius, float cooldown, float reveal, LayerMask layers)
        {
            radiusMeters = Mathf.Max(0.1f, radius);
            cooldownSeconds = Mathf.Max(0f, cooldown);
            revealSeconds = Mathf.Max(0f, reveal);
            detectableLayers = layers;
        }

        /// <summary>Restrict signal kinds without changing physics layers or target components.</summary>
        public void ConfigureSignals(ScannerSignalMask signals)
        {
            detectableSignals = signals & ScannerSignalMask.All;
        }

        /// <returns>Unique marked targets detected, or -1 while disabled/on cooldown.</returns>
        public int TryPulse(Vector3 position)
        {
            if (!Ready) return -1;
            PruneExpired();
            nextAllowedTime = Time.time + Mathf.Max(0f, cooldownSeconds);
            lastHits.Clear();
            seenThisPulse.Clear();
            int count = Physics.OverlapSphereNonAlloc(position, RadiusMeters, queryBuffer,
                detectableLayers, triggerInteraction);
            LastPulseTruncated = count == QueryCapacity;
            for (int i = 0; i < count; i++)
            {
                Collider hit = queryBuffer[i];
                if (hit == null || !hit.enabled || !hit.gameObject.activeInHierarchy) continue;
                if (ignoreOwnHierarchy && hit.transform.IsChildOf(transform)) continue;
                ScannerDetectable target = hit.GetComponentInParent<ScannerDetectable>();
                if (target == null || !target.isActiveAndEnabled) continue;
                int signalIndex = (int)target.SignalKind;
                if (signalIndex < 0 || signalIndex > (int)ScannerSignalKind.Hazard) continue;
                if ((detectableSignals & (ScannerSignalMask)(1 << signalIndex)) == 0) continue;
                if (!seenThisPulse.Add(target)) continue;

                lastHits.Add(hit);
                if (revealSeconds > 0f)
                {
                    if (!expiry.ContainsKey(target)) revealed.Add(target);
                    expiry[target] = Time.time + revealSeconds;
                }
                OnDetected?.Invoke(target);
            }
            onPulse.Invoke();
            return lastHits.Count;
        }

        public bool IsRevealed(ScannerDetectable target)
        {
            return target != null && target.isActiveAndEnabled && target.gameObject.activeInHierarchy &&
                expiry.TryGetValue(target, out float until) && Time.time < until;
        }

        public void ResetScanner()
        {
            nextAllowedTime = 0f;
            LastPulseTruncated = false;
            lastHits.Clear();
            revealed.Clear();
            expiry.Clear();
            seenThisPulse.Clear();
        }

        private void Update() => PruneExpired();

        private void PruneExpired()
        {
            float now = Time.time;
            for (int i = revealed.Count - 1; i >= 0; i--)
            {
                ScannerDetectable target = revealed[i];
                if (target == null || !target.isActiveAndEnabled || !target.gameObject.activeInHierarchy ||
                    !expiry.TryGetValue(target, out float until) || now >= until)
                {
                    expiry.Remove(target);
                    revealed.RemoveAt(i);
                }
            }
        }

        private void OnDisable() => ResetScanner();

        private void OnValidate()
        {
            radiusMeters = Mathf.Max(0.1f, radiusMeters);
            cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
            revealSeconds = Mathf.Max(0f, revealSeconds);
        }
    }
}
