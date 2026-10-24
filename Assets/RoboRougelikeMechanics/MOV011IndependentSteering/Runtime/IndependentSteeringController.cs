using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.Mechanics.IndependentSteering
{
    /// <summary>
    /// Steering ONLY: generates per-wheel angles. Never adds torque/forces or overrides R6 TankController.
    /// An R6 drive adapter must consume wheel directions for actual crab motion.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IndependentSteeringController : MonoBehaviour
    {
        [SerializeField] private Rigidbody vehicleBody;
        [SerializeField] private SteeringMode mode = SteeringMode.FrontOnly;
        [SerializeField] private SteeringGeometry geometry = new SteeringGeometry { wheelbaseMeters=2.8f, trackMeters=2.0f };
        [SerializeField] private SteeringProfile profile = new SteeringProfile
        {
            maxCenterAngleDegrees=30f, rearSteerRatio=0.7f,
            steeringRateDegreesPerSecond=135f, halfAngleAtSpeedMetersPerSecond=12f,
            minimumSpeedFactor=0.35f
        };
        [SerializeField] private bool discoverChildrenOnAwake = true;
        [Tooltip("Disable when simulation is driven manually by a test or a custom physics adapter.")]
        [SerializeField] private bool automaticFixedUpdate = true;
        private readonly SteeringWheelModule[] wheels = new SteeringWheelModule[4];
        private float steerInput, currentFrontDegrees, currentRearDegrees;
        private SteeringSolution lastSolution;

        public SteeringMode Mode => mode;
        public SteeringSolution CurrentSolution => lastSolution;
        public float CurrentInput => steerInput;
        public Rigidbody VehicleBody => vehicleBody;

        private void Awake()
        {
            if (vehicleBody == null) vehicleBody = GetComponentInParent<Rigidbody>();
            if (discoverChildrenOnAwake)
            {
                foreach (var module in GetComponentsInChildren<SteeringWheelModule>(true)) RegisterWheel(module);
            }
            lastSolution = SteeringMath.Solve(0f, 0f, geometry);
        }
        private void OnValidate() { geometry=geometry.Sanitized(); profile=profile.Sanitized(); }
        private void FixedUpdate()
        {
            if (automaticFixedUpdate) Step(Time.fixedDeltaTime);
        }
        public void Configure(SteeringGeometry newGeometry, SteeringProfile newProfile)
        {
            geometry=newGeometry.Sanitized(); profile=newProfile.Sanitized();
        }
        public void Bind(Rigidbody body) { vehicleBody=body; }
        public void SetMode(SteeringMode newMode) { mode=newMode; }
        public void SetSteeringInput(float normalizedSteer)
        {
            steerInput=SteeringMath.SafeClamp(normalizedSteer,-1f,1f,0f);
        }
        public void SetAutomaticFixedUpdate(bool enabled) { automaticFixedUpdate=enabled; }
        public bool RegisterWheel(SteeringWheelModule wheel)
        {
            if (wheel == null) return false;
            int index=(int)wheel.Position;
            if (index < 0 || index >= wheels.Length) return false;
            if (wheels[index] != null && wheels[index] != wheel) return false;
            wheels[index]=wheel; return true;
        }
        public void UnregisterWheel(SteeringWheelModule wheel)
        {
            if (wheel == null) return;
            for (int i=0;i<wheels.Length;i++) if (wheels[i] == wheel) wheels[i]=null;
        }
        public SteeringWheelModule GetWheel(SteeringWheelPosition position)
        {
            int index=(int)position;
            return index>=0 && index < wheels.Length ? wheels[index] : null;
        }
        private float AxleHealth(int a, int b)
        {
            return ((wheels[a] != null && wheels[a].IsSteerable ? 1 : 0) +
                    (wheels[b] != null && wheels[b].IsSteerable ? 1 : 0)) * 0.5f;
        }
        public void Step(float fixedDt)
        {
            if (fixedDt <= 0f || float.IsNaN(fixedDt) || float.IsInfinity(fixedDt)) return;
            float speed=0f;
            if (vehicleBody != null)
            {
                Vector3 v=vehicleBody.linearVelocity;
                speed=new Vector2(v.x,v.z).magnitude;
            }
            SteeringMath.CenterTargets(mode,steerInput,speed,profile,AxleHealth(0,1),AxleHealth(2,3),
                                       out float targetFront,out float targetRear);
            currentFrontDegrees=SteeringMath.Approach(currentFrontDegrees,targetFront,profile.steeringRateDegreesPerSecond,fixedDt);
            currentRearDegrees=SteeringMath.Approach(currentRearDegrees,targetRear,profile.steeringRateDegreesPerSecond,fixedDt);
            lastSolution=SteeringMath.Solve(currentFrontDegrees,currentRearDegrees,geometry);
            for (int i=0;i<wheels.Length;i++)
                if (wheels[i] != null) wheels[i].Apply(lastSolution.GetAngle((SteeringWheelPosition)i));
        }
        public void ResetSteering()
        {
            steerInput=0f; currentFrontDegrees=0f; currentRearDegrees=0f;
            lastSolution=SteeringMath.Solve(0f,0f,geometry);
            for (int i=0;i<wheels.Length;i++) if (wheels[i] != null) wheels[i].Apply(0f);
        }
        private void OnDisable() { ResetSteering(); }
    }
}
