using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RoboRougelike.Mechanics.SEN001
{
    /// <summary>Manual, read-only scanner: never changes targets, AI, health or rewards.</summary>
    [DisallowMultipleComponent]
    public sealed class ScannerPulse : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float radiusMeters = 20f;
        [SerializeField, Min(0f)] private float cooldownSeconds = 5f;
        [SerializeField] private LayerMask detectableLayers = ~0;
        [SerializeField] private UnityEvent onPulse = new UnityEvent();
        private readonly Collider[] buffer = new Collider[128];
        private readonly List<Collider> lastHits = new List<Collider>(128);
        private float nextAllowedTime;

        public IReadOnlyList<Collider> LastHits => lastHits;
        public bool Ready => isActiveAndEnabled && Time.time >= nextAllowedTime;

        public int TryPulse(Vector3 position)
        {
            if (!Ready) return -1;
            nextAllowedTime = Time.time + cooldownSeconds;
            lastHits.Clear();
            int count = Physics.OverlapSphereNonAlloc(position, radiusMeters, buffer,
                detectableLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider hit = buffer[i];
                if (hit != null && !lastHits.Contains(hit)) lastHits.Add(hit);
            }
            onPulse.Invoke();
            return lastHits.Count;
        }

        public void ResetScanner()
        {
            nextAllowedTime = 0f;
            lastHits.Clear();
        }

        private void OnDisable() => ResetScanner();
    }
}
