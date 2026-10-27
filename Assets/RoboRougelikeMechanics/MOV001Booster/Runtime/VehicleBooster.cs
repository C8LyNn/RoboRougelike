using System;
using UnityEngine;

namespace RoboRougelike.Mechanics.Booster
{
    /// <summary>
    /// MOV-001: stand-alone module thrust for a shared vehicle Rigidbody.
    /// +Z of thrustMount is the thrust direction. No input or game-specific dependencies.
    /// Caller must notify detachment/destruction via SetOperational(false).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VehicleBooster : MonoBehaviour
    {
        public enum ActivationFailure
        {
            None, AlreadyActive, CoolingDown, DisabledModule, MissingRigidbody,
            KinematicRigidbody, DetachedFromRigidbody, MissingEnergySource, InsufficientEnergy
        }

        [Header("Bindings (can be provided at runtime)")]
        [SerializeField] private Rigidbody targetRigidbody;
        [SerializeField] private Transform thrustMount;
        [Tooltip("Optional MonoBehaviour implementing IBoosterEnergySource.")]
        [SerializeField] private MonoBehaviour energySource;
        [Tooltip("If true, module must remain beneath bound Rigidbody in the Transform hierarchy. Disable only when the project tracks structure separately.")]
        [SerializeField] private bool requireHierarchyAttachment = true;

        [Header("Performance")]
        [SerializeField, Min(0f)] private float thrustNewtons = 6000f;
        [SerializeField, Min(0.01f)] private float durationSeconds = 1.5f;
        [SerializeField, Min(0f)] private float cooldownSeconds = 7f;
        [Tooltip("Limiter on EXTRA acceleration from this booster (m/s^2). 0 means unlimited.")]
        [SerializeField, Min(0f)] private float maximumAddedAcceleration = 15f;
        [Tooltip("No more boost in mount-forward direction after reaching this speed (m/s). 0 means unlimited.")]
        [SerializeField, Min(0f)] private float maximumForwardSpeed = 28f;
        [Tooltip("Apply at the mounting position (causes torque). Disabled by default to avoid excessive repulsion/rotation while tuning physics.")]
        [SerializeField] private bool applyAtMountPosition;

        [Header("Optional Energy Costs")]
        [SerializeField, Min(0f)] private float energyToStart;
        [SerializeField, Min(0f)] private float energyPerSecond;

        private BoosterCycle cycle;
        private bool operational = true;
        private Func<bool> availabilityFilter;
        public void SetAvailabilityFilter(Func<bool> filter) => availabilityFilter=filter;

        public bool IsOperational => operational && (availabilityFilter==null||availabilityFilter());
        public bool IsBoosting => cycle != null && cycle.IsBoosting;
        public bool IsReady => cycle != null && cycle.IsReady;
        public float RemainingBoostSeconds => cycle != null ? cycle.BoostSecondsRemaining : 0f;
        public float RemainingCooldownSeconds => cycle != null ? cycle.CooldownSecondsRemaining : 0f;
        public ActivationFailure LastActivationFailure { get; private set; }
        public Rigidbody TargetRigidbody => targetRigidbody;

        private void Awake()
        {
            if (targetRigidbody == null)
                targetRigidbody = GetComponentInParent<Rigidbody>();
            EnsureCycle();
        }

        private void OnDisable()
        {
            if (cycle != null) cycle.Cancel();
        }

        private void OnValidate()
        {
            thrustNewtons = Mathf.Max(0f, thrustNewtons);
            durationSeconds = Mathf.Max(0.01f, durationSeconds);
            cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
            maximumAddedAcceleration = Mathf.Max(0f, maximumAddedAcceleration);
            maximumForwardSpeed = Mathf.Max(0f, maximumForwardSpeed);
            energyToStart = Mathf.Max(0f, energyToStart);
            energyPerSecond = Mathf.Max(0f, energyPerSecond);
        }

        /// <summary>Bind a vehicle root and optional physical mount (forward == boost).</summary>
        public void Bind(Rigidbody body, Transform mount = null)
        {
            if (cycle != null) cycle.Cancel();
            targetRigidbody = body;
            thrustMount = mount;
        }

        public void SetEnergySource(MonoBehaviour component)
        {
            if (component != null && !(component is IBoosterEnergySource))
                throw new ArgumentException("Energy component must implement IBoosterEnergySource.", nameof(component));
            energySource = component;
        }

        public void SetOperational(bool value)
        {
            operational = value;
            if (!value && cycle != null) cycle.Cancel();
        }

        public void SetRequireHierarchyAttachment(bool required) => requireHierarchyAttachment = required;

        /// <summary>
        /// Configure once on creation, NEVER every frame. Reconfiguring resets runtime cooldown.
        /// </summary>
        public void Configure(float newThrustNewtons, float newDurationSeconds,
            float newCooldownSeconds, float newMaxAcceleration = 15f, float newMaxForwardSpeed = 28f,
            float newActivationEnergy = 0f, float newEnergyPerSecond = 0f, bool newApplyAtMountPosition = false)
        {
            ValidateNonNegative(newThrustNewtons, nameof(newThrustNewtons));
            if (!BoosterCycle.IsFinite(newDurationSeconds) || newDurationSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(newDurationSeconds));
            ValidateNonNegative(newCooldownSeconds, nameof(newCooldownSeconds));
            ValidateNonNegative(newMaxAcceleration, nameof(newMaxAcceleration));
            ValidateNonNegative(newMaxForwardSpeed, nameof(newMaxForwardSpeed));
            ValidateNonNegative(newActivationEnergy, nameof(newActivationEnergy));
            ValidateNonNegative(newEnergyPerSecond, nameof(newEnergyPerSecond));

            thrustNewtons = newThrustNewtons;
            durationSeconds = newDurationSeconds;
            cooldownSeconds = newCooldownSeconds;
            maximumAddedAcceleration = newMaxAcceleration;
            maximumForwardSpeed = newMaxForwardSpeed;
            energyToStart = newActivationEnergy;
            energyPerSecond = newEnergyPerSecond;
            applyAtMountPosition = newApplyAtMountPosition;
            cycle = new BoosterCycle(durationSeconds, cooldownSeconds);
        }

        /// <summary>
        /// Request an activation. Call from the existing player input, AI or active-module system.
        /// No automatic key bindings; failure reason available through LastActivationFailure.
        /// </summary>
        public bool TryActivate()
        {
            EnsureCycle();
            LastActivationFailure = ActivationFailure.None;
            if (!IsOperational || !isActiveAndEnabled)
                return Fail(ActivationFailure.DisabledModule);
            if (cycle.Phase == BoosterCycle.CyclePhase.Boosting)
                return Fail(ActivationFailure.AlreadyActive);
            if (cycle.Phase == BoosterCycle.CyclePhase.CoolingDown)
                return Fail(ActivationFailure.CoolingDown);
            if (targetRigidbody == null)
                targetRigidbody = GetComponentInParent<Rigidbody>();
            if (targetRigidbody == null)
                return Fail(ActivationFailure.MissingRigidbody);
            if (targetRigidbody.isKinematic)
                return Fail(ActivationFailure.KinematicRigidbody);
            if (!IsAttachedToBody())
                return Fail(ActivationFailure.DetachedFromRigidbody);

            if (energyToStart > 0f || energyPerSecond > 0f)
            {
                var source = energySource as IBoosterEnergySource;
                if (source == null || energySource == null || !energySource.isActiveAndEnabled)
                    return Fail(ActivationFailure.MissingEnergySource);
                if (!source.TryConsume(energyToStart))
                    return Fail(ActivationFailure.InsufficientEnergy);
            }

            cycle.TryActivate();
            return true;
        }

        public void StopBoost()
        {
            if (cycle != null) cycle.Cancel();
        }

        [ContextMenu("Booster: Activate (Play Mode)")]
        private void ActivateFromContextMenu()
        {
            if (Application.isPlaying && !TryActivate())
                Debug.LogWarning($"Booster activation rejected: {LastActivationFailure}", this);
        }

        [ContextMenu("Booster: Stop (Play Mode)")]
        private void StopFromContextMenu()
        {
            if (Application.isPlaying) StopBoost();
        }

        private void FixedUpdate()
        {
            EnsureCycle();
            float dt = Time.fixedDeltaTime;

            if (!cycle.IsBoosting)
            {
                cycle.Advance(dt); // cooldown ticks regardless of current module status
                return;
            }

            if (!IsOperational || targetRigidbody == null || targetRigidbody.isKinematic || !IsAttachedToBody())
            {
                cycle.Cancel();
                cycle.Advance(dt);
                return;
            }

            float activeSeconds = Mathf.Min(dt, cycle.BoostSecondsRemaining);
            if (energyPerSecond > 0f)
            {
                var source = energySource as IBoosterEnergySource;
                if (source == null || energySource == null || !energySource.isActiveAndEnabled ||
                    !source.TryConsume(energyPerSecond * activeSeconds))
                {
                    cycle.Cancel(); // fail closed; never boost for free after energy depletion
                    cycle.Advance(dt);
                    return;
                }
            }

            float elapsedBoostSeconds = cycle.Advance(dt);
            ApplyThrust(elapsedBoostSeconds, dt);
        }

        private void ApplyThrust(float activeSeconds, float stepSeconds)
        {
            if (activeSeconds <= 0f || thrustNewtons <= 0f || stepSeconds <= 0f) return;

            Transform mount = thrustMount != null ? thrustMount : transform;
            Vector3 direction = mount.forward;
            if (direction.sqrMagnitude <= 0.000001f) return;
            direction.Normalize();

            float force = thrustNewtons;
            if (maximumAddedAcceleration > 0f)
                force = Mathf.Min(force, targetRigidbody.mass * maximumAddedAcceleration);

            if (maximumForwardSpeed > 0f)
            {
                float forwardSpeed = Vector3.Dot(targetRigidbody.linearVelocity, direction);
                float speedRemaining = maximumForwardSpeed - forwardSpeed;
                if (speedRemaining <= 0f) return;
                // Bound the boost's projected delta-v this step WITHOUT altering existing velocity.
                force = Mathf.Min(force, targetRigidbody.mass * speedRemaining / stepSeconds);
            }

            force *= activeSeconds / stepSeconds;
            if (applyAtMountPosition)
                targetRigidbody.AddForceAtPosition(direction * force, mount.position, ForceMode.Force);
            else
                targetRigidbody.AddForce(direction * force, ForceMode.Force);
        }

        private bool IsAttachedToBody()
        {
            if (!requireHierarchyAttachment || targetRigidbody == null) return true;
            return transform == targetRigidbody.transform || transform.IsChildOf(targetRigidbody.transform);
        }

        private bool Fail(ActivationFailure reason)
        {
            LastActivationFailure = reason;
            return false;
        }

        private void EnsureCycle()
        {
            if (cycle == null)
                cycle = new BoosterCycle(Mathf.Max(0.01f, durationSeconds), Mathf.Max(0f, cooldownSeconds));
        }

        private static void ValidateNonNegative(float value, string name)
        {
            if (!BoosterCycle.IsFinite(value) || value < 0f)
                throw new ArgumentOutOfRangeException(name, "Expected a finite non-negative number.");
        }

        private void OnDrawGizmosSelected()
        {
            Transform mount = thrustMount != null ? thrustMount : transform;
            Gizmos.color = new Color(0.1f, 0.75f, 1f);
            Gizmos.DrawLine(mount.position, mount.position + mount.forward * 2f);
            Gizmos.DrawSphere(mount.position + mount.forward * 2f, 0.09f);
        }
    }
}
