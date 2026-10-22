using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.Mechanics.MOV008
{
    /// <summary>
    /// Differential track drive for one Rigidbody chassis; intentionally does NOT drive
    /// wheel joints, animated track links or another vehicle controller.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class CrawlerTrackController : MonoBehaviour
    {
        [Header("Standalone mode: must be coordinated with the game's movement controller")]
        [SerializeField] private bool driveEnabled = true;
        [Min(0f)] [SerializeField] private float maxForwardSpeed = 13f;
        [Min(0f)] [SerializeField] private float maxReverseSpeed = 6f;
        [Min(0.02f)] [SerializeField] private float driveResponseSeconds = 0.45f;
        [Min(0.02f)] [SerializeField] private float coastResponseSeconds = 6f;
        [Min(0.02f)] [SerializeField] private float brakeResponseSeconds = 0.18f;
        [Min(0.02f)] [SerializeField] private float lateralResponseSeconds = 0.32f;
        [Min(0f)] [SerializeField] private float maxDriveAccelerationMps2 = 7f;
        [Min(0f)] [SerializeField] private float maxBrakeAccelerationMps2 = 9f;
        [Min(0f)] [SerializeField] private float maxLateralAccelerationMps2 = 8f;

        [Header("Optional energy (only charged when actively applying drive command)")]
        [SerializeField] private MonoBehaviour energyProvider;
        [Min(0f)] [SerializeField] private float energyPerSecondAtFullInput = 0f;

        private Rigidbody chassis;
        private readonly List<CrawlerTrackModule> modules = new List<CrawlerTrackModule>();
        private readonly List<GroundedModule> contacts = new List<GroundedModule>();
        private float forward;
        private float steering;
        private bool braking;
        // Negative retains the original standalone differential tuning until a host supplies limits.
        private float commandedYawDegrees = -1f;

        private struct GroundedModule
        {
            public CrawlerTrackModule Track;
            public Vector3 Normal;
        }

        public bool DriveEnabled => driveEnabled;
        public float ForwardInput => forward;
        public float SteeringInput => steering;
        public bool Braking => braking;
        public int GroundedModuleCount { get; private set; }
        public int InstalledModuleCount => modules.Count;
        public Rigidbody Chassis => chassis;

        private void Awake()
        {
            chassis = GetComponent<Rigidbody>();
            RefreshModules();
        }

        private void OnEnable()
        {
            if (chassis == null) chassis = GetComponent<Rigidbody>();
            RefreshModules();
        }

        private void OnDisable()
        {
            forward = 0f;
            steering = 0f;
            braking = false;
            GroundedModuleCount = 0;
        }

        public void SetInput(float forwardInput, float steerInput)
        {
            forward = CrawlerTrackMath.ClampInput(forwardInput);
            steering = CrawlerTrackMath.ClampInput(steerInput);
        }

        public void SetBrake(bool active) => braking = active;

        public void SetDriveLimits(float forwardSpeed, float reverseSpeed, float yawDegrees)
        {
            maxForwardSpeed=Mathf.Clamp(CrawlerTrackMath.Finite(forwardSpeed),0f,30f);
            maxReverseSpeed=Mathf.Clamp(CrawlerTrackMath.Finite(reverseSpeed),0f,30f);
            commandedYawDegrees=Mathf.Clamp(CrawlerTrackMath.Finite(yawDegrees),0f,260f);
        }

        public void SetDriveEnabled(bool active)
        {
            driveEnabled = active;
            if (!active)
            {
                forward = 0f;
                steering = 0f;
                braking = false;
                GroundedModuleCount = 0;
            }
        }

        public void SetEnergySource(MonoBehaviour provider) => energyProvider = provider;
        public void SetEnergyCost(float unitsPerSecond) =>
            energyPerSecondAtFullInput = Mathf.Max(0f, CrawlerTrackMath.Finite(unitsPerSecond));

        public void Register(CrawlerTrackModule module)
        {
            if (module != null && !modules.Contains(module)) modules.Add(module);
        }

        public void Unregister(CrawlerTrackModule module)
        {
            if (module != null) modules.Remove(module);
        }

        public void RefreshModules()
        {
            modules.Clear();
            GetComponentsInChildren<CrawlerTrackModule>(true, modules);
        }

        private void OnValidate()
        {
            maxForwardSpeed = Mathf.Max(0f, CrawlerTrackMath.Finite(maxForwardSpeed));
            maxReverseSpeed = Mathf.Max(0f, CrawlerTrackMath.Finite(maxReverseSpeed));
            driveResponseSeconds = Mathf.Max(0.02f, CrawlerTrackMath.Finite(driveResponseSeconds));
            coastResponseSeconds = Mathf.Max(0.02f, CrawlerTrackMath.Finite(coastResponseSeconds));
            brakeResponseSeconds = Mathf.Max(0.02f, CrawlerTrackMath.Finite(brakeResponseSeconds));
            lateralResponseSeconds = Mathf.Max(0.02f, CrawlerTrackMath.Finite(lateralResponseSeconds));
            maxDriveAccelerationMps2 = Mathf.Max(0f, CrawlerTrackMath.Finite(maxDriveAccelerationMps2));
            maxBrakeAccelerationMps2 = Mathf.Max(0f, CrawlerTrackMath.Finite(maxBrakeAccelerationMps2));
            maxLateralAccelerationMps2 = Mathf.Max(0f, CrawlerTrackMath.Finite(maxLateralAccelerationMps2));
            energyPerSecondAtFullInput = Mathf.Max(0f, CrawlerTrackMath.Finite(energyPerSecondAtFullInput));
        }

        private void FixedUpdate()
        {
            GroundedModuleCount = 0;
            if (!driveEnabled || chassis == null || chassis.isKinematic || !chassis.gameObject.activeInHierarchy)
                return;

            contacts.Clear();
            for (int i = 0; i < modules.Count; ++i)
            {
                CrawlerTrackModule track = modules[i];
                if (track == null || !track.IsAvailableFor(chassis)) continue;
                if (track.TryGroundContact(chassis, out Vector3 normal))
                    contacts.Add(new GroundedModule { Track = track, Normal = normal });
            }

            GroundedModuleCount = contacts.Count;
            if (contacts.Count == 0) return;

            float left = CrawlerTrackMath.SideDemand(forward, steering, CrawlerTrackSide.Left);
            float right = CrawlerTrackMath.SideDemand(forward, steering, CrawlerTrackSide.Right);
            bool powered = true;
            float inputStrength = Mathf.Max(Mathf.Abs(left), Mathf.Abs(right));
            if (!braking && inputStrength > 0.001f && energyPerSecondAtFullInput > 0f)
            {
                ICrawlerEnergySource source = energyProvider as ICrawlerEnergySource;
                // Non-zero energy cost with a missing/disabled/invalid provider fails closed.
                powered = energyProvider != null && energyProvider.isActiveAndEnabled &&
                          source != null && source.TryConsume(energyPerSecondAtFullInput *
                                                              inputStrength * Time.fixedDeltaTime);
            }

            float massShare = chassis.mass / contacts.Count;
            float gravity = Physics.gravity.magnitude;
            for (int i = 0; i < contacts.Count; i++)
            {
                CrawlerTrackModule track = contacts[i].Track;
                Vector3 normal = contacts[i].Normal;
                Vector3 fore = CrawlerTrackMath.ForwardOnGround(track.transform.forward, normal);
                if (fore == Vector3.zero) continue;
                Vector3 sideVector = Vector3.Cross(normal, fore).normalized;
                Vector3 velocity = chassis.GetPointVelocity(track.transform.position);
                float longitudinal = Vector3.Dot(velocity, fore);
                float sideways = Vector3.Dot(velocity, sideVector);

                float gripLong = CrawlerTrackMath.SupportForceLimit(massShare,
                    track.NominalLoadKg, gravity, track.DriveGripCoefficient);
                float gripSide = Mathf.Min(massShare * maxLateralAccelerationMps2,
                    CrawlerTrackMath.SupportForceLimit(massShare,
                        track.NominalLoadKg, gravity, track.SideGripCoefficient));
                float demand = track.Side == CrawlerTrackSide.Left ? left : right;
                // On empty battery, retain passive rolling friction and manual brakes.
                if (!powered) demand = 0f;
                float forwardLimit=maxForwardSpeed,reverseLimit=maxReverseSpeed,response=driveResponseSeconds;
                if(commandedYawDegrees>=0f&&powered&&!braking)
                {
                    // Convert the host's linear/yaw targets to each physical track contact speed.
                    // All force, contact, mass and traction budgets below remain unchanged.
                    float lever=Vector3.Dot(track.transform.position-chassis.worldCenterOfMass,sideVector);
                    float target=forward*(forward>=0f?maxForwardSpeed:maxReverseSpeed)-
                        steering*commandedYawDegrees*Mathf.Deg2Rad*lever;
                    demand=Mathf.Abs(target)>.0001f?Mathf.Sign(target):0f;
                    forwardLimit=reverseLimit=Mathf.Abs(target);
                    // Differential yaw must overcome contact stiction at low target speeds.
                    // Faster response still saturates at the same finite motor/traction limits.
                    if(Mathf.Abs(steering)>.001f)response=Mathf.Min(response,.08f);
                }
                float traction = CrawlerTrackMath.LongitudinalForce(longitudinal, demand,
                    massShare, forwardLimit, reverseLimit, response,
                    coastResponseSeconds, brakeResponseSeconds,
                    powered ? Mathf.Min(track.MaxDriveForceN,
                                        massShare * maxDriveAccelerationMps2) : 0f,
                    Mathf.Min(track.MaxBrakeForceN,
                              massShare * maxBrakeAccelerationMps2), gripLong, braking);
                // No powered drive = no motor force. Passive coasting must still work.
                if (!powered && !braking)
                {
                    traction = CrawlerTrackMath.LongitudinalForce(longitudinal, 0f,
                        massShare, maxForwardSpeed, maxReverseSpeed,
                        driveResponseSeconds, coastResponseSeconds, brakeResponseSeconds,
                        0f, Mathf.Min(track.MaxBrakeForceN,
                                      massShare * maxBrakeAccelerationMps2), gripLong, false);
                }
                float lateral = CrawlerTrackMath.LateralForce(sideways, massShare,
                    lateralResponseSeconds, gripSide);
                chassis.AddForceAtPosition(fore * traction + sideVector * lateral,
                                           track.transform.position, ForceMode.Force);
            }
        }
    }
}
