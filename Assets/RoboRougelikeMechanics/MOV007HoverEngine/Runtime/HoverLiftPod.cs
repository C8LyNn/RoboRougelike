using UnityEngine;

namespace RoboRougelike.Mechanics.MOV007
{
    /// <summary>A single physical lift point. Mounted as a CHILD of the vehicle Rigidbody.</summary>
    [DisallowMultipleComponent]
    public sealed class HoverLiftPod : MonoBehaviour
    {
        [Header("Hover Physics")]
        [Min(0.1f)] public float targetHeightMeters = 1.3f;
        [Min(0.1f)] public float probeExtraRangeMeters = 1.4f;
        [Min(0f)] public float probeStartAbovePod = 0.3f;
        [Range(0.1f, 9f)] public float springFrequencyHz = 1.6f;
        [Range(0f, 3f)] public float dampingRatio = 0.95f;
        [Min(1f)] public float nominalLoadKg = 160f;
        [Min(1f)] public float maximumLiftToNominalWeight = 2.6f;
        [Tooltip("Prefer only level terrain / arena layers; raycast filters own compound colliders.")]
        public LayerMask terrainLayers = ~0;
        [Tooltip("If true, other dynamic vehicles do not count as support surfaces.")]
        public bool staticGroundOnly = true;

        [Header("State / Energy")]
        public bool operational = true;
        public bool requireEnergy;
        [Min(0f)] public float energyPerSecond = 2f;
        [Tooltip("Optional MonoBehaviour implementing IHoverEnergySource. Shared energy is supported.")]
        public MonoBehaviour energySourceBehaviour;

        public bool HasGroundContact { get; private set; }
        public float LastMeasuredGroundDistance { get; private set; } = -1f;
        public float LastLiftForceNewtons { get; private set; }
        public bool HasPowerSource => !requireEnergy || energySourceBehaviour is IHoverEnergySource;

        private RaycastHit[] _hits = new RaycastHit[32];
        private System.Func<bool> availabilityFilter;
        private System.Predicate<Collider> groundFilter;
        private bool useMountAxes;
        // Default mounts retain the original gravity-stabilized hover behavior.
        // A sideways/inverted nozzle probes and pushes along its physical axis.
        public Vector3 LiftDirection=>useMountAxes?transform.up:Vector3.up;
        public void SetUseMountAxes(bool value)=>useMountAxes=value;
        public void SetAvailabilityFilter(System.Func<bool> filter)=>availabilityFilter=filter;
        public void SetGroundFilter(System.Predicate<Collider> filter)=>groundFilter=filter;

        public bool IsAvailableFor(Rigidbody body)
        {
            return isActiveAndEnabled && operational && (availabilityFilter==null||availabilityFilter()) && body != null &&
                   GetComponentInParent<Rigidbody>() == body && HasPowerSource;
        }

        public void SetOperational(bool available) { operational = available; if (!available) ResetDiagnostics(); }

        internal bool CanSpendEnergy(float dt)
        {
            if (!requireEnergy) return true;
            var source = energySourceBehaviour as IHoverEnergySource;
            return source != null && source.CanSpend(Mathf.Max(0f, energyPerSecond) * dt);
        }
        internal bool TrySpendEnergy(float dt)
        {
            if (!requireEnergy) return true;
            var source = energySourceBehaviour as IHoverEnergySource;
            return source != null && source.TrySpend(Mathf.Max(0f, energyPerSecond) * dt);
        }

        internal bool TryProbeGround(Rigidbody body, out float distance)
        {
            distance = -1f;
            if (body == null) return false;
            Vector3 origin = transform.position + LiftDirection * probeStartAbovePod;
            float rayLength = targetHeightMeters + probeExtraRangeMeters + probeStartAbovePod;
            int count = Physics.RaycastNonAlloc(origin, -LiftDirection, _hits, rayLength,
                                               terrainLayers, QueryTriggerInteraction.Ignore);
            // Avoid silent false negatives on heavily modular (100+ collider) vehicles.
            while (count == _hits.Length && _hits.Length < 2048)
            {
                _hits = new RaycastHit[_hits.Length * 2];
                count = Physics.RaycastNonAlloc(origin, -LiftDirection, _hits, rayLength,
                                               terrainLayers, QueryTriggerInteraction.Ignore);
            }
            if (count == _hits.Length) return false; // fail closed on buffer saturation

            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider collider = _hits[i].collider;
                if (collider == null || collider.transform.IsChildOf(body.transform)) continue;
                if (staticGroundOnly && collider.attachedRigidbody != null) continue;
                if(groundFilter!=null&&!groundFilter(collider))continue;
                float d = _hits[i].distance - probeStartAbovePod;
                if (d < best) best = d;
            }
            if (best == float.MaxValue) return false;
            distance = Mathf.Max(0f, best);
            return true;
        }

        internal void UpdateDiagnostics(bool contact, float height, float force)
        {
            HasGroundContact = contact;
            LastMeasuredGroundDistance = contact ? height : -1f;
            LastLiftForceNewtons = force;
        }
        internal void ResetDiagnostics() { UpdateDiagnostics(false, -1f, 0f); }
        private void OnDisable() { ResetDiagnostics(); }

        private void OnValidate()
        {
            targetHeightMeters = Mathf.Max(0.1f, targetHeightMeters);
            probeExtraRangeMeters = Mathf.Max(0.1f, probeExtraRangeMeters);
            probeStartAbovePod = Mathf.Max(0f, probeStartAbovePod);
            nominalLoadKg = Mathf.Max(1f, nominalLoadKg);
            maximumLiftToNominalWeight = Mathf.Max(1f, maximumLiftToNominalWeight);
            energyPerSecond = Mathf.Max(0f, energyPerSecond);
        }
    }
}
