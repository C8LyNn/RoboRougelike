using UnityEngine;

namespace RoboRougelike.Mechanics.MOV004GyroscopicStabilizer
{
    /// <summary>
    /// Add to a child module on the SAME Rigidbody hierarchy as the vehicle root.
    /// This applies angular acceleration only: it NEVER writes Transform.rotation,
    /// Rigidbody.rotation, velocity or angularVelocity.
    ///
    /// TargetUp defaults to WORLD UP. Override via SetTargetUp(...) for specialized vehicles.
    /// Integrate module detach/HP via SetOperational(false) BEFORE removing the mount.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GyroStabilizer : MonoBehaviour
    {
        [Header("Vehicle / mounting")]
        [SerializeField] private Rigidbody vehicleBody;
        [SerializeField] private bool startRequestedActive = true;
        [SerializeField] private bool requireSameRigidbodyHierarchy = true;

        [Header("Upright tuning (angular acceleration, not torque units)")]
        [SerializeField, Min(0f)] private float stiffness = 15f;
        [SerializeField, Min(0f)] private float angularDamping = 6.5f;
        [SerializeField, Min(0.01f)] private float maxAngularAcceleration = 12f;
        [SerializeField, Min(0f)] private float activationThreshold = 0.025f;

        [Header("Optional shared energy")]
        [Tooltip("Energy units per second when the gyro actually corrects tilt. Zero disables energy usage.")]
        [SerializeField, Min(0f)] private float energyCostPerSecond = 0f;
        [Tooltip("Optional component implementing IGyroEnergySource. For production, bind the game's adapter.")]
        [SerializeField] private MonoBehaviour energySourceComponent;

        private IGyroEnergySource energySource;
        private Vector3 targetUp = Vector3.up;
        private bool requestedActive;
        private bool operational = true;
        private System.Func<bool> availabilityFilter;
        public void SetAvailabilityFilter(System.Func<bool> filter) => availabilityFilter=filter;

        public Rigidbody VehicleBody { get { return vehicleBody; } }
        public bool IsRequestedActive { get { return requestedActive; } }
        public bool IsOperational { get { return operational && (availabilityFilter==null||availabilityFilter()); } }
        public bool IsApplyingCorrection { get { return LastAngularAcceleration.sqrMagnitude > 0f; } }
        public Vector3 LastAngularAcceleration { get; private set; }

        private void Awake()
        {
            requestedActive = startRequestedActive;
            if (vehicleBody == null) vehicleBody = GetComponentInParent<Rigidbody>();
            if (energySourceComponent != null)
                energySource = energySourceComponent as IGyroEnergySource;
        }

        private void OnDisable() { LastAngularAcceleration = Vector3.zero; }

        /// <summary>May be called when a part is mounted or vehicle root changes.</summary>
        public bool BindToVehicle(Rigidbody body)
        {
            if (body == null || !transform.IsChildOf(body.transform) || GetComponentInParent<Rigidbody>() != body) return false;
            vehicleBody = body;
            LastAngularAcceleration = Vector3.zero;
            return true;
        }

        public void SetRequestedActive(bool active)
        {
            requestedActive = active;
            if (!active) LastAngularAcceleration = Vector3.zero;
        }

        public void SetOperational(bool active)
        {
            operational = active;
            if (!active) LastAngularAcceleration = Vector3.zero;
        }

        /// <summary>Use from terrain adaptation if desired; zero-vector resets to world up.</summary>
        public void SetTargetUp(Vector3 worldUp)
        {
            targetUp = IsFinite(worldUp) && worldUp.sqrMagnitude > 0.000001f
                ? worldUp.normalized : Vector3.up;
        }

        /// <summary>Attach the shared energy provider. Null means no provider; any positive cost blocks usage.</summary>
        public void SetEnergySource(IGyroEnergySource source) { energySource = source; }

        public void Configure(float stiffnessValue, float dampingValue,
            float maxAccelerationValue, float minEffectiveAcceleration = 0.025f)
        {
            stiffness = Mathf.Max(0f, stiffnessValue);
            angularDamping = Mathf.Max(0f, dampingValue);
            maxAngularAcceleration = Mathf.Max(0.01f, maxAccelerationValue);
            activationThreshold = Mathf.Max(0f, minEffectiveAcceleration);
        }

        public void SetEnergyCostPerSecond(float cost)
        {
            energyCostPerSecond = IsFinite(cost) ? Mathf.Max(0f, cost) : 0f;
        }

        private void FixedUpdate()
        {
            LastAngularAcceleration = Vector3.zero;
            if (!requestedActive || !IsOperational || vehicleBody == null || !vehicleBody.gameObject.activeInHierarchy)
                return;
            if (vehicleBody.isKinematic) return;

            // Handles common detachment case (module reparented or converted to own Rigidbody).
            if (requireSameRigidbodyHierarchy &&
                (!transform.IsChildOf(vehicleBody.transform) || GetComponentInParent<Rigidbody>() != vehicleBody))
                return;

            Vector3 angularAcc = GyroStabilizationMath.Calculate(vehicleBody.rotation,
                vehicleBody.angularVelocity, targetUp, stiffness, angularDamping, maxAngularAcceleration);
            if (angularAcc.sqrMagnitude <= activationThreshold * activationThreshold) return;

            // Fail closed: a positive energy cost requires a functioning provider.
            if (energyCostPerSecond > 0f)
            {
                if (energySource == null || !energySource.TrySpend(energyCostPerSecond * Time.fixedDeltaTime))
                    return;
            }

            // ForceMode.Acceleration represents angular acceleration; yaw is untouched by math policy.
            // This is deliberately a vehicle-level controller, independent of exact part location.
            vehicleBody.AddTorque(angularAcc, ForceMode.Acceleration);
            LastAngularAcceleration = angularAcc;
        }

        private void OnValidate()
        {
            stiffness = Clean(stiffness);
            angularDamping = Clean(angularDamping);
            maxAngularAcceleration = Mathf.Max(0.01f, Clean(maxAngularAcceleration));
            activationThreshold = Clean(activationThreshold);
            energyCostPerSecond = Clean(energyCostPerSecond);
        }

        private static float Clean(float value)
        {
            return IsFinite(value) ? Mathf.Max(0f, value) : 0f;
        }
        private static bool IsFinite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }
    }
}
