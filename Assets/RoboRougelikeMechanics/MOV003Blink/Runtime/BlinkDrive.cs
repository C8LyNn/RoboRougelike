using UnityEngine;

namespace RoboRougelike.Mechanics.MOV003Blink
{
    // Standalone prototype: no TankController, garage, weapons or project-specific API dependencies.
    public sealed class BlinkDrive : MonoBehaviour
    {
        private const int QueryCapacity = 256;
        private const float RayStartClearance = 1f;
        [SerializeField] private Rigidbody vehicleBody;
        [SerializeField] private BlinkSettings settings = new BlinkSettings();
        [SerializeField] private bool operational = true;
        private readonly Collider[] overlaps = new Collider[QueryCapacity];
        private readonly RaycastHit[] casts = new RaycastHit[QueryCapacity];
        private readonly RaycastHit[] groundHits = new RaycastHit[QueryCapacity];
        private readonly BlinkCooldown cooldown = new BlinkCooldown();
        private IBlinkEnergySource energy;
        private System.Func<bool> availabilityFilter;
        private System.Predicate<Collider> groundFilter;
        public void SetAvailabilityFilter(System.Func<bool> filter)=>availabilityFilter=filter;
        public void SetGroundFilter(System.Predicate<Collider> filter)=>groundFilter=filter;

        public Rigidbody VehicleBody => vehicleBody;
        public BlinkSettings Settings => settings;
        public float CooldownRemaining => cooldown.Remaining(Time.time);
        public bool Operational => operational && isActiveAndEnabled && (availabilityFilter==null||availabilityFilter());

        private void Awake()
        {
            if (vehicleBody == null) vehicleBody = GetComponentInParent<Rigidbody>();
        }

        public void BindToVehicle(Rigidbody body) { vehicleBody = body; }
        public void Configure(BlinkSettings value) { settings = value; }
        public void SetEnergySource(IBlinkEnergySource source) { energy = source; }
        public void SetOperational(bool value) { operational = value; }
        public void ResetCooldown() { cooldown.Reset(); }

        public BlinkResult TryActivate() => TryActivate(vehicleBody == null ? Vector3.zero : vehicleBody.rotation * Vector3.forward);

        public BlinkResult TryActivate(Vector3 intendedWorldDirection)
        {
            if (!Operational) return BlinkResult.NotOperational;
            if (vehicleBody == null) return BlinkResult.MissingRigidbody;
            if (vehicleBody.isKinematic || !vehicleBody.gameObject.activeInHierarchy) return BlinkResult.InvalidRigidbody;
            if (settings == null || !ValidateSettings()) return BlinkResult.InvalidSettings;
            if (settings.RequireModuleUnderVehicleTransform && !transform.IsChildOf(vehicleBody.transform))
                return BlinkResult.DetachedFromVehicle;
            if (IsMultiRigidbodyAssembly()) return BlinkResult.MultiRigidbodyAssembly;
            if (!cooldown.IsReady(Time.time)) return BlinkResult.CoolingDown;
            if (settings.EnergyCost > 0f && energy == null) return BlinkResult.MissingEnergySource;
            if (settings.EnergyCost > 0f && !energy.CanSpend(settings.EnergyCost)) return BlinkResult.InsufficientEnergy;

            if (!BlinkMath.TryPlan(vehicleBody.position, intendedWorldDirection, settings.DistanceMeters, out Vector3 destination))
                return BlinkResult.InvalidSettings;

            Physics.SyncTransforms();
            if (!BlinkHull.TryCalculate(vehicleBody, out Vector3 localCenter, out Vector3 localHalfExtents))
                return BlinkResult.NoVehicleColliders;
            Vector3 half = localHalfExtents + Vector3.one * settings.HullPaddingMeters;
            Quaternion poseRotation = vehicleBody.rotation;
            Vector3 startCenter = vehicleBody.position + poseRotation * localCenter;
            Vector3 endCenter = destination + poseRotation * localCenter;
            float projectedY;
            Vector3 right = poseRotation * Vector3.right;
            Vector3 up = poseRotation * Vector3.up;
            Vector3 forward = poseRotation * Vector3.forward;
            projectedY = Mathf.Abs(right.y) * half.x + Mathf.Abs(up.y) * half.y + Mathf.Abs(forward.y) * half.z;
            float sourceFootY = startCenter.y - projectedY;
            float destinationFootY = endCenter.y - projectedY;

            if (settings.RequireGroundSupport && !HasGroundSupport(endCenter, half, poseRotation, destinationFootY, projectedY))
                return BlinkResult.UnsupportedDestination;

            int count = Physics.OverlapBoxNonAlloc(endCenter, half, overlaps, poseRotation, settings.BlockingLayers, QueryTriggerInteraction.Ignore);
            if (count >= QueryCapacity) return BlinkResult.PhysicsQueryOverflow;
            for (int i = 0; i < count; i++)
            {
                Collider hit = overlaps[i];
                if (hit != null && !IsOwnCollider(hit) && !IsFloorLike(hit, destinationFootY))
                    return BlinkResult.DestinationBlocked;
            }

            if (settings.RequireClearPath)
            {
                Vector3 displacement = destination - vehicleBody.position;
                int swept = Physics.BoxCastNonAlloc(startCenter, half, displacement.normalized,
                    casts, poseRotation, displacement.magnitude, settings.BlockingLayers, QueryTriggerInteraction.Ignore);
                if (swept >= QueryCapacity) return BlinkResult.PhysicsQueryOverflow;
                for (int i = 0; i < swept; i++)
                {
                    Collider hit = casts[i].collider;
                    if (hit != null && !IsOwnCollider(hit) &&
                        !IsFloorLike(hit, Mathf.Min(sourceFootY, destinationFootY)))
                        return BlinkResult.PathBlocked;
                }
            }

            // All safety checks complete: charge only now. A failed attempt never consumes energy or cooldown.
            if (settings.EnergyCost > 0f && !energy.TrySpend(settings.EnergyCost)) return BlinkResult.InsufficientEnergy;
            if (!cooldown.TryStart(Time.time, settings.CooldownSeconds)) return BlinkResult.CoolingDown;
            Vector3 previousVelocity = vehicleBody.linearVelocity;
            Vector3 previousAngular = vehicleBody.angularVelocity;
            vehicleBody.position = destination;
            // Rigidbody pose changes reach Transform on the next physics step. A Blink must
            // also move its mounted parts immediately, including when called between steps.
            vehicleBody.transform.SetPositionAndRotation(destination, poseRotation);
            if (settings.PreserveVelocity)
            {
                vehicleBody.linearVelocity = previousVelocity;
                vehicleBody.angularVelocity = previousAngular;
            }
            else
            {
                vehicleBody.linearVelocity = Vector3.zero;
                vehicleBody.angularVelocity = Vector3.zero;
            }
            vehicleBody.WakeUp();
            Physics.SyncTransforms();
            return BlinkResult.Activated;
        }

        private bool ValidateSettings()
        {
            return BlinkMath.IsFinite(settings.DistanceMeters) && settings.DistanceMeters > 0f &&
                   BlinkMath.IsFinite(settings.CooldownSeconds) && settings.CooldownSeconds >= 0f &&
                   BlinkMath.IsFinite(settings.EnergyCost) && settings.EnergyCost >= 0f &&
                   BlinkMath.IsFinite(settings.MaximumDropMeters) && settings.MaximumDropMeters >= 0f &&
                   BlinkMath.IsFinite(settings.AllowedRiseMeters) && settings.AllowedRiseMeters >= 0f &&
                   BlinkMath.IsFinite(settings.HullPaddingMeters) && settings.HullPaddingMeters >= 0f &&
                   settings.RequiredSupportSamples >= 1 && settings.RequiredSupportSamples <= 5 &&
                   BlinkMath.IsFinite(settings.MinimumGroundNormalY) && settings.MinimumGroundNormalY >= 0f &&
                   settings.MinimumGroundNormalY <= 1f;
        }

        private bool IsMultiRigidbodyAssembly()
        {
            // Atomic Blink is only guaranteed for one dynamic Rigidbody plus child colliders.
            // Separate joined bodies must be handled by the integrator as a group.
            Rigidbody[] bodies = vehicleBody.GetComponentsInChildren<Rigidbody>(false);
            for (int i = 0; i < bodies.Length; i++)
                if (bodies[i] != null && bodies[i] != vehicleBody) return true;
            Joint[] joints = vehicleBody.GetComponentsInChildren<Joint>(false);
            for (int i = 0; i < joints.Length; i++)
                if (joints[i] != null && joints[i].connectedBody != null &&
                    joints[i].connectedBody != vehicleBody) return true;
            return false;
        }

        private bool HasGroundSupport(Vector3 center, Vector3 half, Quaternion poseRotation, float footY, float projectedY)
        {
            // Five footprint samples to avoid teleporting a wide vehicle onto a tiny ledge.
            // This is still a conservative prototype; production should sample wheel/leg contacts.
            int supported = 0;
            for (int index = 0; index < 5; index++)
            {
                Vector3 localOffset = Vector3.zero;
                if (index > 0)
                {
                    localOffset.x = ((index & 1) == 0 ? 1f : -1f) * half.x * 0.55f;
                    localOffset.z = ((index & 2) == 0 ? 1f : -1f) * half.z * 0.55f;
                }
                Vector3 point = center + poseRotation * localOffset;
                Vector3 origin = new Vector3(point.x, center.y + projectedY + RayStartClearance, point.z);
                float distance = projectedY * 2f + RayStartClearance + settings.MaximumDropMeters;
                int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits, distance,
                    settings.SupportLayers, QueryTriggerInteraction.Ignore);
                if (count >= QueryCapacity) return false;
                bool found = false;
                for (int i = 0; i < count; i++)
                {
                    RaycastHit hit = groundHits[i];
                    if (hit.collider == null || IsOwnCollider(hit.collider)) continue;
                    if(groundFilter!=null&&!groundFilter(hit.collider))continue;
                    if (hit.normal.y < settings.MinimumGroundNormalY) continue;
                    float delta = hit.point.y - footY;
                    if (delta <= settings.AllowedRiseMeters && delta >= -settings.MaximumDropMeters)
                    {
                        found = true;
                        break;
                    }
                }
                if (found) supported++;
                if (supported >= settings.RequiredSupportSamples) return true;
            }
            return false;
        }

        private bool IsFloorLike(Collider collider, float footY)
        {
            // Only treat an obstruction as floor if it belongs to the support mask and its TOP
            // does not rise into the vehicle hull. Solid vertical obstacles still block Blink.
            return (settings.SupportLayers.value & (1 << collider.gameObject.layer)) != 0 &&
                   (groundFilter==null||groundFilter(collider)) &&
                   collider.bounds.max.y <= footY + settings.AllowedRiseMeters;
        }

        private bool IsOwnCollider(Collider collider) =>
            collider.attachedRigidbody == vehicleBody && collider.transform.IsChildOf(vehicleBody.transform);
    }
}
