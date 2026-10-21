using UnityEngine;

namespace RoboRougelike.Mechanics.MOV005OmniDrive
{
    /// <summary>
    /// Physical, destructible drive part. Install under the vehicle Rigidbody.
    /// The coordinator sums module acceleration; it never grants unlimited top speed for stacking.
    /// The module may have its own collider, health, mass and salvage data from the game.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OmniGroundProbe))]
    public sealed class OmniDriveModule : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float forwardAndStrafeMaxSpeed = 12f;
        [SerializeField, Min(0f)] private float linearAcceleration = 6f;
        [SerializeField, Min(0f)] private float yawSpeedDegrees = 105f;
        [SerializeField, Min(0f)] private float yawAngularAcceleration = 3.5f;
        [SerializeField, Min(0f)] private float energyCostPerSecond = 0f;
        [SerializeField] private bool operational = true;
        private OmniGroundProbe groundProbe;
        private System.Func<bool> availabilityFilter;
        public void SetAvailabilityFilter(System.Func<bool> filter)=>availabilityFilter=filter;

        public bool IsOperational { get { return operational&&(availabilityFilter==null||availabilityFilter()); } }
        public float MaximumSpeed { get { return forwardAndStrafeMaxSpeed; } }
        public float LinearAcceleration { get { return linearAcceleration; } }
        public float YawSpeedDegrees { get { return yawSpeedDegrees; } }
        public float YawAngularAcceleration { get { return yawAngularAcceleration; } }
        public float EnergyCostPerSecond { get { return energyCostPerSecond; } }
        public OmniGroundProbe GroundProbe { get { return groundProbe != null ? groundProbe : (groundProbe = GetComponent<OmniGroundProbe>()); } }

        public void Configure(float speed, float acceleration, float yawSpeed, float yawAcceleration,
            float energyPerSecond = 0f)
        {
            forwardAndStrafeMaxSpeed = OmniDriveMath.NonNegative(speed);
            linearAcceleration = OmniDriveMath.NonNegative(acceleration);
            yawSpeedDegrees = OmniDriveMath.NonNegative(yawSpeed);
            yawAngularAcceleration = OmniDriveMath.NonNegative(yawAcceleration);
            energyCostPerSecond = OmniDriveMath.NonNegative(energyPerSecond);
        }

        public void SetOperational(bool value) { operational = value; }

        public bool CanDrive(Rigidbody rootBody)
        {
            return IsOperational && isActiveAndEnabled && rootBody != null &&
                transform.IsChildOf(rootBody.transform) &&
                GetComponentInParent<Rigidbody>() == rootBody &&
                GroundProbe != null && GroundProbe.TouchesGround(rootBody);
        }

        private void OnValidate()
        {
            Configure(forwardAndStrafeMaxSpeed, linearAcceleration, yawSpeedDegrees,
                yawAngularAcceleration, energyCostPerSecond);
        }
    }
}
