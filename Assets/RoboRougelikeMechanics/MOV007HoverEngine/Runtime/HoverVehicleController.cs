using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.Mechanics.MOV007
{
    /// <summary>
    /// Standalone prototype. Exactly one controller per active vehicle Rigidbody.
    /// Owns all lift pods; drive input comes from an adapter (no dependency on TankController).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class HoverVehicleController : MonoBehaviour
    {
        [Header("Vehicle")]
        [SerializeField] private Rigidbody vehicleBody;
        [Tooltip("Set false when TankController or another drive mode is driving the same Rigidbody.")]
        public bool hoverModeEnabled = true;
        [Tooltip("Optional auto-refresh handles new/remounted/detached pods during prototyping.")]
        [Min(0)] public int rescanEveryFixedSteps = 25;

        [Header("Horizontal Drive")]
        [Min(0f)] public float forwardSpeedMetersPerSec = 14f;
        [Min(0f)] public float strafeSpeedMetersPerSec = 10f;
        [Min(0f)] public float thrustNewtonsPerPoweredPod = 800f;
        [Min(0f)] public float yawTorqueNmPerPoweredPod = 650f;
        [Min(0f)] public float yawDegreesPerSecond = 95f;
        [Min(0.05f)] public float inputResponseSeconds = 0.5f;
        [Min(0.05f)] public float idleDragResponseSeconds = 1.4f;
        [Min(0.05f)] public float brakeResponseSeconds = 0.3f;
        [Tooltip("When disabled, external boosters can exceed this motor's nominal speed.")]
        public bool counteractExternalBoost;

        public Rigidbody Body => vehicleBody;
        public int RegisteredPodCount => _pods.Count;
        public int LastSupportedPodCount { get; private set; }
        public Vector2 MoveInput { get; private set; }
        public float TurnInput { get; private set; }
        public bool BrakeInput { get; private set; }

        private readonly List<HoverLiftPod> _pods = new List<HoverLiftPod>();
        private readonly List<HoverLiftPod> _available = new List<HoverLiftPod>();
        private readonly List<HoverLiftPod> _supported = new List<HoverLiftPod>();
        private readonly List<float> _heights = new List<float>();
        private int _fixedTicks;

        private void Awake()
        {
            if (vehicleBody == null) vehicleBody = GetComponent<Rigidbody>();
            RefreshModules();
        }
        private void OnEnable() { RefreshModules(); }

        public void SetMoveInput(Vector2 input) { MoveInput = Vector2.ClampMagnitude(input, 1f); }
        public void SetTurnInput(float input) { TurnInput = Mathf.Clamp(input, -1f, 1f); }
        public void SetBrakeInput(bool pressed) { BrakeInput = pressed; }
        public void SetHoverMode(bool enabled)
        {
            hoverModeEnabled = enabled;
            if (!enabled) { ClearInput(); ResetPodDiagnostics(); }
        }
        public void ClearInput() { MoveInput = Vector2.zero; TurnInput = 0f; BrakeInput = false; }
        public void RefreshModules()
        {
            _pods.Clear();
            // Include disabled components so a damaged part can re-enable without a scan.
            foreach (var pod in GetComponentsInChildren<HoverLiftPod>(true))
                if (pod != null) _pods.Add(pod);
        }
        private void ResetPodDiagnostics()
        {
            foreach (var pod in _pods) if (pod != null) pod.ResetDiagnostics();
            LastSupportedPodCount = 0;
        }

        private void FixedUpdate()
        {
            if (vehicleBody == null || vehicleBody.isKinematic || !hoverModeEnabled)
            {
                ResetPodDiagnostics();
                return;
            }
            if (rescanEveryFixedSteps > 0 && ++_fixedTicks >= rescanEveryFixedSteps)
            {
                _fixedTicks = 0;
                RefreshModules();
            }

            _available.Clear();
            _supported.Clear();
            _heights.Clear();
            float dt = Time.fixedDeltaTime;
            for (int i = 0; i < _pods.Count; i++)
            {
                HoverLiftPod pod = _pods[i];
                if (pod == null) continue;
                pod.ResetDiagnostics();
                if (!pod.IsAvailableFor(vehicleBody) || !pod.CanSpendEnergy(dt)) continue;
                _available.Add(pod);
            }
            int availableCount = _available.Count;
            // World-up hover is only defined for downward vertical gravity.
            float gravity = -Physics.gravity.y;
            if (gravity <= 0.001f || availableCount == 0) { LastSupportedPodCount = 0; return; }

            // Probe first. A pod over a hole does not automatically transfer all its load
            // to other pods, so partial loss of support can cause a physical roll/pitch.
            for (int i = 0; i < availableCount; i++)
            {
                HoverLiftPod pod = _available[i];
                if (!pod.TryProbeGround(vehicleBody, out float height)) continue;
                _supported.Add(pod);
                _heights.Add(height);
            }

            // Shared tanks are debited once per active, ground-supported pod per step.
            // A failed debit means no lift and no thrust from that pod this frame.
            int paidCount = 0;
            for (int i = 0; i < _supported.Count; i++)
            {
                HoverLiftPod pod = _supported[i];
                float height = _heights[i];
                float pointSpeed = Vector3.Dot(vehicleBody.GetPointVelocity(pod.transform.position), pod.LiftDirection);
                float liftForce = HoverPhysicsMath.LiftForceNewtons(
                    vehicleBody.mass, availableCount, gravity, height,
                    pod.targetHeightMeters, pointSpeed, pod.springFrequencyHz,
                    pod.dampingRatio, pod.nominalLoadKg, pod.maximumLiftToNominalWeight);
                if (liftForce <= 0f) { pod.UpdateDiagnostics(true, height, 0f); continue; }
                if (!pod.TrySpendEnergy(dt)) { pod.UpdateDiagnostics(false, -1f, 0f); continue; }
                vehicleBody.AddForceAtPosition(pod.LiftDirection * liftForce, pod.transform.position, ForceMode.Force);
                pod.UpdateDiagnostics(true, height, liftForce);
                paidCount++;
            }
            LastSupportedPodCount = paidCount;
            if (paidCount > 0) ApplyPlanarDrive(paidCount);
        }

        private void ApplyPlanarDrive(int supportingPodCount)
        {
            Vector3 desired = HoverPhysicsMath.DesiredPlanarVelocity(
                transform, MoveInput, forwardSpeedMetersPerSec, strafeSpeedMetersPerSec);
            float response = BrakeInput ? brakeResponseSeconds :
                (MoveInput.sqrMagnitude < 0.001f ? idleDragResponseSeconds : inputResponseSeconds);
            if (BrakeInput) desired = Vector3.zero;
            Vector3 force = HoverPhysicsMath.PlanarMotorForce(
                vehicleBody.linearVelocity, desired, vehicleBody.mass, response,
                supportingPodCount * thrustNewtonsPerPoweredPod,
                counteractExternalBoost || BrakeInput || MoveInput.sqrMagnitude < 0.001f);
            vehicleBody.AddForce(force, ForceMode.Force);

            float targetYaw = TurnInput * yawDegreesPerSecond * Mathf.Deg2Rad;
            if (BrakeInput) targetYaw = 0f;
            float inertia = HoverPhysicsMath.ApproximateWorldUpInertia(vehicleBody);
            float torque = HoverPhysicsMath.YawMotorTorque(
                Vector3.Dot(vehicleBody.angularVelocity, Vector3.up), targetYaw,
                inertia, response, supportingPodCount * yawTorqueNmPerPoweredPod);
            vehicleBody.AddTorque(Vector3.up * torque, ForceMode.Force);
        }

        private void OnValidate()
        {
            if (vehicleBody == null) vehicleBody = GetComponent<Rigidbody>();
            rescanEveryFixedSteps = Mathf.Max(0, rescanEveryFixedSteps);
            forwardSpeedMetersPerSec = Mathf.Max(0f, forwardSpeedMetersPerSec);
            strafeSpeedMetersPerSec = Mathf.Max(0f, strafeSpeedMetersPerSec);
            thrustNewtonsPerPoweredPod = Mathf.Max(0f, thrustNewtonsPerPoweredPod);
            yawTorqueNmPerPoweredPod = Mathf.Max(0f, yawTorqueNmPerPoweredPod);
            yawDegreesPerSecond = Mathf.Max(0f, yawDegreesPerSecond);
            inputResponseSeconds = Mathf.Max(0.05f, inputResponseSeconds);
            idleDragResponseSeconds = Mathf.Max(0.05f, idleDragResponseSeconds);
            brakeResponseSeconds = Mathf.Max(0.05f, brakeResponseSeconds);
        }
    }
}
