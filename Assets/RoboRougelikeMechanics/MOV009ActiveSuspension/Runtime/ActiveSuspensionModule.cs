using UnityEngine;

namespace RoboRougelike.Mechanics.MOV009
{
    /// <summary>
    /// A single physical suspension support point, mounted under the same Rigidbody
    /// as the chassis. It never teleports or reorients the chassis.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActiveSuspensionModule : MonoBehaviour
    {
        [Header("Condition")]
        [SerializeField] private bool operational = true;

        [Header("Ground sensing: WORLD DOWN")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private bool staticGroundOnly = true;
        [Min(0f)] [SerializeField] private float probeAboveMount = 0.2f;
        [Min(0.05f)] [SerializeField] private float probeBelowMount = 1.35f;
        [Range(0f, 80f)] [SerializeField] private float maximumGroundSlopeDegrees = 55f;

        [Header("Physical suspension")]
        [Min(0.05f)] [SerializeField] private float rideHeightMeters = 0.55f;
        [Min(0.01f)] [SerializeField] private float minimumRideHeightMeters = 0.15f;
        [Min(0.05f)] [SerializeField] private float maximumRideHeightMeters = 1.1f;
        [Min(0f)] [SerializeField] private float stiffnessNewtonsPerMeter = 8000f;
        [Min(0f)] [SerializeField] private float dampingNewtonSecondsPerMeter = 1050f;
        [Min(0f)] [SerializeField] private float maximumForceNewtons = 9000f;

        private ActiveSuspensionController owner;
        private RaycastHit[] rayBuffer = new RaycastHit[16];
        private System.Func<Collider,bool> groundFilter;
        private System.Func<bool> availabilityFilter;
        // Host-game policies remain outside this independent assembly.
        public void SetGroundFilter(System.Func<Collider,bool> filter) => groundFilter=filter;
        public void SetAvailabilityFilter(System.Func<bool> filter) => availabilityFilter=filter;

        public bool Operational => operational;
        public float RideHeightMeters => rideHeightMeters;
        public float LastAppliedForceNewtons { get; internal set; }
        public bool LastGroundContact { get; private set; }
        public float LastGroundDistanceMeters { get; private set; }

        public void SetOperational(bool value)
        {
            operational = value;
            if (!value) ResetDiagnostics();
        }

        /// <summary>May be used by future garage upgrades. Values are validated.</summary>
        public void ConfigureSpring(float rideHeight, float stiffness, float damping, float forceLimit)
        {
            rideHeightMeters = Mathf.Max(0.05f, SuspensionMath.Finite(rideHeight, 0.55f));
            stiffnessNewtonsPerMeter = Mathf.Max(0f, SuspensionMath.Finite(stiffness));
            dampingNewtonSecondsPerMeter = Mathf.Max(0f, SuspensionMath.Finite(damping));
            maximumForceNewtons = Mathf.Max(0f, SuspensionMath.Finite(forceLimit));
        }

        private void OnEnable() => BindOwner();
        private void OnDisable() { UnbindOwner(); ResetDiagnostics(); }
        private void OnDestroy() => UnbindOwner();
        private void OnTransformParentChanged() => BindOwner();

        private void BindOwner()
        {
            ActiveSuspensionController next = GetComponentInParent<ActiveSuspensionController>();
            if (owner == next) return;
            UnbindOwner();
            owner = next;
            if (owner != null && isActiveAndEnabled) owner.Register(this);
        }

        private void UnbindOwner()
        {
            if (owner == null) return;
            owner.Unregister(this);
            owner = null;
        }

        public bool IsAvailableFor(Rigidbody chassis)
        {
            return chassis != null && operational && isActiveAndEnabled &&
                   (availabilityFilter == null || availabilityFilter()) &&
                   transform.IsChildOf(chassis.transform) &&
                   GetComponentInParent<Rigidbody>() == chassis;
        }

        internal void ResetDiagnostics()
        {
            LastAppliedForceNewtons = 0f;
            LastGroundContact = false;
            LastGroundDistanceMeters = 0f;
        }

        internal float EvaluateLiftNewtons(Rigidbody chassis, float rideHeightOffsetMeters, out Vector3 forcePoint)
        {
            forcePoint = transform.position;
            ResetDiagnostics();
            if (!IsAvailableFor(chassis) || !TryGetGroundDistance(chassis, out float distance))
                return 0f;
            LastGroundContact = true;
            LastGroundDistanceMeters = distance;
            float target = SuspensionMath.DesiredHeight(rideHeightMeters, rideHeightOffsetMeters,
                minimumRideHeightMeters, maximumRideHeightMeters);
            float verticalVelocity = Vector3.Dot(chassis.GetPointVelocity(forcePoint), Vector3.up);
            return SuspensionMath.LiftForceNewtons(target, distance, verticalVelocity,
                stiffnessNewtonsPerMeter, dampingNewtonSecondsPerMeter, maximumForceNewtons);
        }

        private bool TryGetGroundDistance(Rigidbody chassis, out float distanceBelowMount)
        {
            distanceBelowMount = 0f;
            float above = Mathf.Max(0f, probeAboveMount);
            float below = Mathf.Max(0.05f, probeBelowMount);
            Vector3 origin = transform.position + Vector3.up * above;
            int hitsCount;
            while (true)
            {
                hitsCount = Physics.RaycastNonAlloc(origin, Vector3.down, rayBuffer, above + below,
                    groundLayers, QueryTriggerInteraction.Ignore);
                if (hitsCount < rayBuffer.Length) break;
                if (rayBuffer.Length >= 256) return false; // fail closed, not a truncated hit list
                rayBuffer = new RaycastHit[rayBuffer.Length * 2];
            }
            float nearest = float.PositiveInfinity;
            bool found = false;
            float requiredUp = Mathf.Cos(maximumGroundSlopeDegrees * Mathf.Deg2Rad);
            for (int i = 0; i < hitsCount; i++)
            {
                Collider collider = rayBuffer[i].collider;
                if (collider == null || collider.transform.IsChildOf(chassis.transform) ||
                    collider.attachedRigidbody == chassis) continue;
                if (staticGroundOnly && collider.attachedRigidbody != null) continue;
                if (groundFilter != null && !groundFilter(collider)) continue;
                if (Vector3.Dot(rayBuffer[i].normal, Vector3.up) < requiredUp) continue;
                if (rayBuffer[i].distance >= nearest) continue;
                nearest = rayBuffer[i].distance;
                found = true;
            }
            if (!found) return false;
            distanceBelowMount = Mathf.Max(0f, nearest - above);
            return true;
        }

        private void OnValidate()
        {
            probeAboveMount = Mathf.Max(0f, SuspensionMath.Finite(probeAboveMount));
            probeBelowMount = Mathf.Max(0.05f, SuspensionMath.Finite(probeBelowMount, 1.35f));
            maximumGroundSlopeDegrees = Mathf.Clamp(maximumGroundSlopeDegrees, 0f, 80f);
            minimumRideHeightMeters = Mathf.Max(0.01f, SuspensionMath.Finite(minimumRideHeightMeters, 0.15f));
            maximumRideHeightMeters = Mathf.Max(minimumRideHeightMeters, SuspensionMath.Finite(maximumRideHeightMeters, 1.1f));
            rideHeightMeters = Mathf.Clamp(SuspensionMath.Finite(rideHeightMeters, 0.55f), minimumRideHeightMeters, maximumRideHeightMeters);
            stiffnessNewtonsPerMeter = Mathf.Max(0f, SuspensionMath.Finite(stiffnessNewtonsPerMeter));
            dampingNewtonSecondsPerMeter = Mathf.Max(0f, SuspensionMath.Finite(dampingNewtonSecondsPerMeter));
            maximumForceNewtons = Mathf.Max(0f, SuspensionMath.Finite(maximumForceNewtons));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = operational ? Color.cyan : Color.red;
            float target = SuspensionMath.DesiredHeight(rideHeightMeters, 0f,
                minimumRideHeightMeters, maximumRideHeightMeters);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * target);
            Gizmos.DrawWireSphere(transform.position + Vector3.down * target, 0.07f);
        }
    }
}
