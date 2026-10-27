using System;
using UnityEngine;

namespace RoboRougelike.Mechanics.MOV002JumpDrive
{
    public enum JumpDriveResult
    {
        Activated,
        NotOperational,
        CoolingDown,
        MissingRigidbody,
        InvalidRigidbody,
        DetachedFromVehicle,
        MissingGroundSupport,
        InvalidGravityOrConfiguration,
        UpwardSpeedAtLimit,
        MissingEnergySource,
        InsufficientEnergy
    }

    /// <summary>
    /// Drop-in, input-agnostic, gravity-relative jump module for Unity 6 Rigidbody vehicles.
    /// Applies instantaneous delta-v (equivalent to an impulse); never teleports the vehicle.
    /// A direct velocity update makes successive drives obey the same speed ceiling even
    /// when multiple activation requests occur before the next FixedUpdate.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JumpDrive : MonoBehaviour
    {
        [SerializeField] private Rigidbody vehicleBody;
        [SerializeField] private Transform groundProbeOrigin;
        [SerializeField] private MonoBehaviour energySourceComponent;
        [SerializeField] private JumpDriveSettings settings = new JumpDriveSettings();
        [SerializeField] private bool operationalOnSpawn = true;

        private readonly JumpDriveCooldown cooldown = new JumpDriveCooldown();
        private IJumpDriveEnergySource injectedEnergySource;
        private bool operational;
        private bool initialized;
        private Func<bool> availabilityFilter;
        private Predicate<Collider> groundFilter;
        public void SetAvailabilityFilter(Func<bool> filter) => availabilityFilter=filter;
        public void SetGroundFilter(Predicate<Collider> filter) => groundFilter=filter;

        public event Action<JumpDrive> Activated;
        public Rigidbody VehicleBody => vehicleBody;
        public bool IsOperational => (initialized ? operational : operationalOnSpawn) && (availabilityFilter==null||availabilityFilter());
        public float CooldownRemaining => cooldown.Remaining(Time.time);
        public JumpDriveSettings Settings => settings;

        private void Awake()
        {
            initialized = true;
            operational = operationalOnSpawn;
            if (vehicleBody == null) vehicleBody = GetComponentInParent<Rigidbody>();
        }

        public void BindToVehicle(Rigidbody body) => vehicleBody = body;
        public void SetGroundProbe(Transform origin) => groundProbeOrigin = origin;
        public void SetEnergySource(IJumpDriveEnergySource source) => injectedEnergySource = source;
        public void SetOperational(bool value)
        {
            initialized = true;
            operational = value;
        }
        public void Configure(JumpDriveSettings configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            settings = configuration;
        }

        public JumpDriveResult TryActivate()
        {
            if (!isActiveAndEnabled || !IsOperational) return JumpDriveResult.NotOperational;
            if (!cooldown.IsReady(Time.time)) return JumpDriveResult.CoolingDown;
            if (vehicleBody == null) return JumpDriveResult.MissingRigidbody;
            if (!vehicleBody.gameObject.activeInHierarchy || vehicleBody.isKinematic)
                return JumpDriveResult.InvalidRigidbody;
            if (settings == null) return JumpDriveResult.InvalidGravityOrConfiguration;
            if (settings.RequireModuleUnderVehicleTransform && !transform.IsChildOf(vehicleBody.transform))
                return JumpDriveResult.DetachedFromVehicle;

            Vector3 gravity = Physics.gravity;
            if (!JumpDriveMath.Valid(gravity.x) || !JumpDriveMath.Valid(gravity.y) || !JumpDriveMath.Valid(gravity.z) ||
                gravity.sqrMagnitude < 0.0001f)
                return JumpDriveResult.InvalidGravityOrConfiguration;
            Vector3 upward = -gravity.normalized;
            if (settings.RequireGroundContact && !IsSupported(upward))
                return JumpDriveResult.MissingGroundSupport;

            float upwardVelocity = Vector3.Dot(vehicleBody.linearVelocity, upward);
            if (!JumpDriveMath.TryCalculate(gravity.magnitude, settings.TargetJumpHeightMeters,
                    settings.SupportedMassKg, vehicleBody.mass, upwardVelocity,
                    settings.MaxUpwardSpeedMps, out float deltaVelocity, out _))
            {
                return JumpDriveMath.Valid(upwardVelocity) && upwardVelocity >= settings.MaxUpwardSpeedMps - 0.0001f
                    ? JumpDriveResult.UpwardSpeedAtLimit : JumpDriveResult.InvalidGravityOrConfiguration;
            }
            if (!JumpDriveMath.Valid(settings.CooldownSeconds) || settings.CooldownSeconds < 0f ||
                !JumpDriveMath.Valid(settings.EnergyCost) || settings.EnergyCost < 0f)
                return JumpDriveResult.InvalidGravityOrConfiguration;

            IJumpDriveEnergySource source = ResolveEnergySource();
            if (settings.EnergyCost > 0f && source == null) return JumpDriveResult.MissingEnergySource;
            if (settings.EnergyCost > 0f && !source.TrySpend(settings.EnergyCost))
                return JumpDriveResult.InsufficientEnergy;

            if (!cooldown.TryStart(Time.time, settings.CooldownSeconds))
            {
                if (settings.EnergyCost > 0f) source.Refund(settings.EnergyCost);
                return JumpDriveResult.CoolingDown;
            }

            // Instantaneous impulse equivalent: Δv = J/m. Preserves lateral/horizontal velocity.
            // Unlike a queued AddForce, this observes earlier jump activations in the same frame.
            vehicleBody.linearVelocity = vehicleBody.linearVelocity + upward * deltaVelocity;
            vehicleBody.WakeUp();
            Activated?.Invoke(this);
            return JumpDriveResult.Activated;
        }

        private IJumpDriveEnergySource ResolveEnergySource()
        {
            if (injectedEnergySource != null) return injectedEnergySource;
            if (energySourceComponent != null) return energySourceComponent as IJumpDriveEnergySource;
            if (vehicleBody != null) return vehicleBody.GetComponent<JumpDriveEnergyBank>();
            return null;
        }

        private bool IsSupported(Vector3 upward)
        {
            if (!JumpDriveMath.Valid(settings.GroundProbeDistanceMeters) || settings.GroundProbeDistanceMeters <= 0f ||
                !JumpDriveMath.Valid(settings.MinimumGroundNormalDot) ||
                settings.MinimumGroundNormalDot < 0f || settings.MinimumGroundNormalDot > 1f)
                return false;
            Vector3 origin = groundProbeOrigin != null ? groundProbeOrigin.position : vehicleBody.worldCenterOfMass;
            // Once per requested jump, RaycastAll avoids the truncated hit buffer problem
            // for player-built vehicles made of hundreds of colliders.
            RaycastHit[] hits = Physics.RaycastAll(origin, -upward,
                settings.GroundProbeDistanceMeters, settings.GroundLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider other = hits[i].collider;
                if (other == null || other.attachedRigidbody == vehicleBody ||
                    other.transform.IsChildOf(vehicleBody.transform))
                    continue;
                if(groundFilter!=null&&!groundFilter(other))continue;
                if (Vector3.Dot(hits[i].normal, upward) >= settings.MinimumGroundNormalDot)
                    return true;
            }
            return false;
        }

        [ContextMenu("Debug / Try Activate Jump Drive (Play Mode)")]
        private void DebugTryActivate()
        {
            if (!Application.isPlaying) { Debug.LogWarning("JumpDrive: Enter Play Mode first.", this); return; }
            Debug.Log("JumpDrive: " + TryActivate(), this);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = groundProbeOrigin != null ? groundProbeOrigin.position :
                vehicleBody != null ? vehicleBody.worldCenterOfMass : transform.position;
            Vector3 downward = Physics.gravity.sqrMagnitude > 0.0001f ? Physics.gravity.normalized : Vector3.down;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, origin + downward * (settings == null ? 2.5f : settings.GroundProbeDistanceMeters));
        }
    }
}
