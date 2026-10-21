using UnityEngine;

namespace RoboRougelike.Mechanics.MOV005OmniDrive
{
    /// <summary>
    /// Add ONCE to the vehicle root. Modules live on child transforms. Commands must be
    /// provided by one input owner (player OR AI). No direct position/velocity writes.
    /// MOV-001/MOV-002/MOV-004 can share the same Rigidbody because only forces are used.
    /// IMPORTANT: disable the existing wheel/controller TRANSLATIONAL/YAW force output in Omni mode;
    /// do not simply have two drive controllers compete.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class OmniDriveController : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private MonoBehaviour energySourceComponent;
        [SerializeField, Min(0.1f)] private float maxCombinedLinearAcceleration = 25f;
        [SerializeField, Min(1f)] private float accelerationReferenceMassKg = 150f;
        [SerializeField, Min(0.1f)] private float maxCombinedAngularAcceleration = 10f;
        [SerializeField] private bool coastOnReleasedInput = true;
        [SerializeField, Min(0f)] private float idleLinearBrakeAcceleration = 5f;
        [SerializeField, Min(0f)] private float idleYawBrakeAcceleration = 0f;
        [SerializeField, Min(0f)] private float commandTimeoutSeconds = 0.4f;

        private OmniDriveModule[] modules = new OmniDriveModule[0];
        private IOmniEnergySource energySource;
        private Vector2 moveInput;
        private float yawInput;
        private float lastCommandTime;
        private bool operational = true;

        public Rigidbody VehicleBody { get { return body; } }
        public Vector3 LastLinearAcceleration { get; private set; }
        public float LastYawAcceleration { get; private set; }
        public int ModuleCount { get { return modules.Length; } }

        private void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody>();
            if (energySourceComponent != null) energySource = energySourceComponent as IOmniEnergySource;
            RebuildModules();
            lastCommandTime = Time.time;
        }

        public void RebuildModules()
        {
            // Call after garage mount/unmount, reconstruction and structural graph detachment.
            modules = GetComponentsInChildren<OmniDriveModule>(true);
        }

        public void SetEnergySource(IOmniEnergySource source) { energySource = source; }
        public void SetOperational(bool canDrive) { operational = canDrive; if (!canDrive) SetInput(Vector2.zero, 0f); }
        public void SetInput(Vector2 localForwardStrafe, float normalizedYaw)
        {
            moveInput = OmniDriveMath.ClampInput(localForwardStrafe);
            yawInput = OmniDriveMath.ClampTurn(normalizedYaw);
            lastCommandTime = Time.time;
        }
        public void SetCoasting(bool coast) { coastOnReleasedInput = coast; }
        public void SetCommandTimeout(float seconds) { commandTimeoutSeconds = OmniDriveMath.NonNegative(seconds); }
        public void SetLimits(float linearAccel, float angularAccel)
        {
            maxCombinedLinearAcceleration = Mathf.Max(0.1f, OmniDriveMath.NonNegative(linearAccel));
            maxCombinedAngularAcceleration = Mathf.Max(0.1f, OmniDriveMath.NonNegative(angularAccel));
        }

        private void OnDisable() { SetInput(Vector2.zero, 0f); LastLinearAcceleration = Vector3.zero; LastYawAcceleration = 0f; }

        private void FixedUpdate()
        {
            LastLinearAcceleration = Vector3.zero;
            LastYawAcceleration = 0f;
            if (!operational || body == null || body.isKinematic || !body.gameObject.activeInHierarchy) return;
            if (body.transform != transform) return; // Refuse accidental attachment to a child Rigidbody.
            if (commandTimeoutSeconds > 0f && Time.time - lastCommandTime > commandTimeoutSeconds)
            {
                moveInput = Vector2.zero;
                yawInput = 0f;
            }
            float totalLinear = 0f, weightedSpeed = 0f;
            float totalAngular = 0f, weightedYawSpeed = 0f;
            float totalEnergyRate = 0f;
            for (int i = 0; i < modules.Length; i++)
            {
                OmniDriveModule drive = modules[i];
                if (drive == null || !drive.CanDrive(body)) continue;
                totalLinear += drive.LinearAcceleration;
                weightedSpeed += drive.MaximumSpeed * drive.LinearAcceleration;
                totalAngular += drive.YawAngularAcceleration;
                weightedYawSpeed += drive.YawSpeedDegrees * drive.YawAngularAcceleration;
                totalEnergyRate += drive.EnergyCostPerSecond;
            }
            if (totalLinear <= 0f && totalAngular <= 0f) return;
            bool moveRequested = moveInput.sqrMagnitude > 0.000001f;
            bool yawRequested = Mathf.Abs(yawInput) > 0.000001f;
            // Energy is only charged while requested movement is present; zero-cost modules
            // still work with no provider. Positive cost without a provider FAILS CLOSED.
            if ((moveRequested || yawRequested) && totalEnergyRate > 0f)
            {
                float demand = totalEnergyRate * Time.fixedDeltaTime *
                    Mathf.Max(moveInput.magnitude, Mathf.Abs(yawInput));
                if (energySource == null || !energySource.TrySpend(demand)) return;
            }
            float speed = totalLinear > 0f ? weightedSpeed / totalLinear : 0f;
            float yawSpeed = totalAngular > 0f ? weightedYawSpeed / totalAngular : 0f;
            // Modules supply a finite reference thrust. Actual acceleration falls with
            // increasing total vehicle mass, including armor and recovered parts.
            float referenceThrust = totalLinear * accelerationReferenceMassKg;
            float acceleration = Mathf.Min(referenceThrust / Mathf.Max(body.mass, 0.001f),
                maxCombinedLinearAcceleration);
            float angularAcceleration = Mathf.Min(totalAngular, maxCombinedAngularAcceleration);
            float idleBrake = coastOnReleasedInput ? 0f : idleLinearBrakeAcceleration;
            LastLinearAcceleration = OmniDriveMath.LinearAcceleration(body.linearVelocity, body.rotation,
                moveInput, speed, acceleration, idleBrake, Time.fixedDeltaTime);
            float yawBrake = coastOnReleasedInput ? 0f : idleYawBrakeAcceleration;
            LastYawAcceleration = OmniDriveMath.YawAcceleration(Vector3.Dot(body.angularVelocity, Vector3.up),
                yawInput, yawSpeed, angularAcceleration, yawBrake, Time.fixedDeltaTime);
            if (LastLinearAcceleration.sqrMagnitude > 0f)
                body.AddForce(LastLinearAcceleration * body.mass, ForceMode.Force);
            if (Mathf.Abs(LastYawAcceleration) > 0f)
                body.AddTorque(Vector3.up * LastYawAcceleration, ForceMode.Acceleration);
        }

        private void OnValidate()
        {
            maxCombinedLinearAcceleration = Mathf.Max(0.1f, OmniDriveMath.NonNegative(maxCombinedLinearAcceleration));
            maxCombinedAngularAcceleration = Mathf.Max(0.1f, OmniDriveMath.NonNegative(maxCombinedAngularAcceleration));
            accelerationReferenceMassKg = Mathf.Max(1f, OmniDriveMath.NonNegative(accelerationReferenceMassKg));
            idleLinearBrakeAcceleration = OmniDriveMath.NonNegative(idleLinearBrakeAcceleration);
            idleYawBrakeAcceleration = OmniDriveMath.NonNegative(idleYawBrakeAcceleration);
            commandTimeoutSeconds = OmniDriveMath.NonNegative(commandTimeoutSeconds);
        }
    }
}
