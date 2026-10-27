using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.Mechanics.MOV009
{
    public enum ActiveSuspensionSupportMode
    {
        /// <summary>Primary suspension. Do NOT pair with a full-force WheelCollider spring.</summary>
        FullSupport = 0,
        /// <summary>Small corrective force for vehicles that already have primary support.</summary>
        AssistOnly = 1
    }

    /// <summary>
    /// Coordinates child suspension modules on one chassis Rigidbody. Applies upward
    /// force at each mount, so loss of a corner naturally introduces roll/pitch.
    /// Uses no position/rotation changes and does not drive horizontal velocity.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ActiveSuspensionController : MonoBehaviour
    {
        [Header("Mode - choose to avoid double suspension")]
        [SerializeField] private ActiveSuspensionSupportMode supportMode = ActiveSuspensionSupportMode.AssistOnly;
        [Range(0f, 1f)] [SerializeField] private float assistOnlyForceMultiplier = 0.25f;
        [Min(0f)] [SerializeField] private float maximumTotalUpwardAcceleration = 18f;
        [Range(0f, 89f)] [SerializeField] private float maximumChassisTilt = 70f;
        [SerializeField] private bool suspensionEnabled = true;

        [Header("Adjustable ride height")]
        [Min(0f)] [SerializeField] private float maximumHeightOffsetMeters = 0.35f;
        [Min(0.01f)] [SerializeField] private float heightAdjustmentMetersPerSecond = 0.35f;
        [Min(0f)] [SerializeField] private float heightAdjustmentEnergyPerMeter = 0f;
        [Tooltip("Optional MonoBehaviour implementing IActiveSuspensionEnergySource.")]
        [SerializeField] private MonoBehaviour energyProvider;

        private readonly List<ActiveSuspensionModule> modules = new List<ActiveSuspensionModule>();
        private readonly List<ActiveSuspensionModule> evaluatedModules = new List<ActiveSuspensionModule>();
        private readonly List<Vector3> evaluatedPoints = new List<Vector3>();
        private readonly List<float> evaluatedForces = new List<float>();
        private Rigidbody chassis;
        private IActiveSuspensionEnergySource energySource;
        private float requestedOffset;
        private float currentOffset;

        public float RequestedRideHeightOffsetMeters => requestedOffset;
        public float CurrentRideHeightOffsetMeters => currentOffset;
        public int RegisteredModuleCount => modules.Count;
        public ActiveSuspensionSupportMode SupportMode => supportMode;
        public bool SuspensionEnabled => suspensionEnabled;
        public float LastTotalLiftNewtons { get; private set; }

        private void Awake()
        {
            chassis = GetComponent<Rigidbody>();
            RefreshEnergySource();
            RefreshModules();
        }

        private void OnEnable()
        {
            if (chassis == null) chassis = GetComponent<Rigidbody>();
            RefreshModules();
        }

        public void Register(ActiveSuspensionModule module)
        {
            if (module != null && !modules.Contains(module)) modules.Add(module);
        }

        public void Unregister(ActiveSuspensionModule module)
        {
            if (module != null) modules.Remove(module);
        }

        public void RefreshModules()
        {
            modules.Clear();
            ActiveSuspensionModule[] found = GetComponentsInChildren<ActiveSuspensionModule>(true);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null && found[i].isActiveAndEnabled) Register(found[i]);
            }
        }

        public void SetSupportMode(ActiveSuspensionSupportMode mode) => supportMode = mode;
        public void SetSuspensionEnabled(bool value) => suspensionEnabled = value;

        public void SetRideHeightOffset(float offsetMeters)
        {
            float limit = Mathf.Max(0f, maximumHeightOffsetMeters);
            requestedOffset = Mathf.Clamp(SuspensionMath.Finite(offsetMeters), -limit, limit);
        }

        public void SetMaximumHeightOffset(float maxOffsetMeters)
        {
            maximumHeightOffsetMeters = Mathf.Max(0f, SuspensionMath.Finite(maxOffsetMeters));
            SetRideHeightOffset(requestedOffset);
        }

        public void SetEnergyConsumption(float unitsPerMeter)
        {
            heightAdjustmentEnergyPerMeter = Mathf.Max(0f, SuspensionMath.Finite(unitsPerMeter));
        }

        public void SetEnergySource(MonoBehaviour source)
        {
            energyProvider = source;
            RefreshEnergySource();
        }

        private void RefreshEnergySource() => energySource = energyProvider as IActiveSuspensionEnergySource;

        private void FixedUpdate()
        {
            LastTotalLiftNewtons = 0f;
            for (int i = 0; i < modules.Count; i++)
                if (modules[i] != null) modules[i].ResetDiagnostics();

            if (chassis == null || !suspensionEnabled || chassis.isKinematic) return;
            if (Vector3.Dot(transform.up, Vector3.up) < Mathf.Cos(maximumChassisTilt * Mathf.Deg2Rad))
                return; // don't make an upside-down vehicle fly

            UpdateHeightOffset(Time.fixedDeltaTime);
            evaluatedModules.Clear();
            evaluatedPoints.Clear();
            evaluatedForces.Clear();

            float requestedTotal = 0f;
            float modeScale = supportMode == ActiveSuspensionSupportMode.AssistOnly
                ? Mathf.Clamp01(assistOnlyForceMultiplier) : 1f;
            for (int i = modules.Count - 1; i >= 0; i--)
            {
                ActiveSuspensionModule module = modules[i];
                if (module == null) { modules.RemoveAt(i); continue; }
                if (!module.IsAvailableFor(chassis)) continue;
                float requested = module.EvaluateLiftNewtons(chassis, currentOffset, out Vector3 point) * modeScale;
                if (requested <= 0f) continue;
                evaluatedModules.Add(module);
                evaluatedPoints.Add(point);
                evaluatedForces.Add(requested);
                requestedTotal += requested;
            }

            float maxForce = Mathf.Max(0f, maximumTotalUpwardAcceleration) * chassis.mass;
            float scale = SuspensionMath.SafeForceScale(requestedTotal, maxForce);
            for (int i = 0; i < evaluatedModules.Count; i++)
            {
                float actual = evaluatedForces[i] * scale;
                evaluatedModules[i].LastAppliedForceNewtons = actual;
                chassis.AddForceAtPosition(Vector3.up * actual, evaluatedPoints[i], ForceMode.Force);
                LastTotalLiftNewtons += actual;
            }
        }

        private void UpdateHeightOffset(float deltaTime)
        {
            float next = Mathf.MoveTowards(currentOffset, requestedOffset,
                Mathf.Max(0.01f, heightAdjustmentMetersPerSecond) * deltaTime);
            float difference = Mathf.Abs(next - currentOffset);
            if (difference <= 0f) return;
            float cost = difference * Mathf.Max(0f, heightAdjustmentEnergyPerMeter);
            if (cost > 0f && (energySource == null || !energySource.TryConsumeEnergy(cost))) return;
            currentOffset = next;
        }

        private void OnDisable()
        {
            LastTotalLiftNewtons = 0f;
            for (int i = 0; i < modules.Count; i++)
                if (modules[i] != null) modules[i].ResetDiagnostics();
        }

        private void OnValidate()
        {
            assistOnlyForceMultiplier = Mathf.Clamp01(SuspensionMath.Finite(assistOnlyForceMultiplier));
            maximumTotalUpwardAcceleration = Mathf.Max(0f, SuspensionMath.Finite(maximumTotalUpwardAcceleration));
            maximumChassisTilt = Mathf.Clamp(maximumChassisTilt, 0f, 89f);
            maximumHeightOffsetMeters = Mathf.Max(0f, SuspensionMath.Finite(maximumHeightOffsetMeters));
            heightAdjustmentMetersPerSecond = Mathf.Max(0.01f, SuspensionMath.Finite(heightAdjustmentMetersPerSecond, 0.35f));
            heightAdjustmentEnergyPerMeter = Mathf.Max(0f, SuspensionMath.Finite(heightAdjustmentEnergyPerMeter));
            requestedOffset = Mathf.Clamp(requestedOffset, -maximumHeightOffsetMeters, maximumHeightOffsetMeters);
        }
    }
}
