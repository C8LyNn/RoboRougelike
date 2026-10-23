using System;
using UnityEngine;

namespace RoboRougelike.Mechanics.EMP
{
    /// <summary>
    /// Represents ONE electronic module, not one collider or one vehicle.
    /// NEVER disable this component or its GameObject in response to EMP: it owns the recovery clock.
    /// R6 adapter must combine HasStructuralPower/HP and !IsDisrupted to compute effective operation.
    /// </summary>
    public sealed class EmpReceiver : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float resistance;
        [SerializeField] private bool susceptible = true;
        [SerializeField] private Transform vehicleRoot;
        [SerializeField] private int vehicleId;
        [SerializeField] private int teamId;
        private readonly EmpDisruptionTimer timer = new EmpDisruptionTimer();
        private bool operational = true;

        /// <summary>True = temporarily unable to operate; receiver itself remains alive.</summary>
        public bool IsDisrupted => timer.IsDisrupted;
        public float RemainingSeconds => timer.Remaining;
        public float Resistance => Mathf.Clamp01(resistance);
        public bool CanBeDisrupted => isActiveAndEnabled && susceptible && operational;
        public Transform VehicleRoot => vehicleRoot != null ? vehicleRoot : transform.root;
        public int VehicleId => vehicleId;
        public int TeamId => teamId;
        public event Action<EmpReceiver, bool> DisruptionChanged;
        public event Action<EmpReceiver> Disabling;

        public void ConfigureOwnership(Transform root, int stableVehicleId = 0, int factionId = 0)
        {
            vehicleRoot = root;
            vehicleId = stableVehicleId;
            teamId = factionId;
        }

        public void ConfigureSusceptibility(float resistance01, bool canBeAffected = true)
        {
            resistance = EmpPulseMath.IsValidFinite(resistance01) ? Mathf.Clamp01(resistance01) : 1f;
            susceptible = canBeAffected;
        }

        public void SetOperational(bool value)
        {
            operational = value;
            if (!operational) ClearEmp();
        }

        public void ApplyEmp(float duration)
        {
            if (!CanBeDisrupted) return;
            if (timer.Apply(duration)) DisruptionChanged?.Invoke(this, true);
        }

        public void Advance(float deltaTime)
        {
            if (timer.Tick(deltaTime)) DisruptionChanged?.Invoke(this, false);
        }

        public void ClearEmp()
        {
            bool wasDisrupted = IsDisrupted;
            timer.Clear();
            if (wasDisrupted) DisruptionChanged?.Invoke(this, false);
        }

        private void Update() => Advance(Time.deltaTime);

        // Unexpectedly disabling the receiver does not leave its consumers latched in EMP state.
        // R6 adapters MUST NOT interpret this as repairing destroyed / detached modules.
        private void OnDisable(){Disabling?.Invoke(this);ClearEmp();}

        private void OnValidate() => resistance = Mathf.Clamp01(resistance);
    }
}
