using UnityEngine;

namespace RoboRougelike.Mechanics.MOV005OmniDrive
{
    /// <summary>Non-allocating ground probe, ignoring every collider belonging to this vehicle.</summary>
    public sealed class OmniGroundProbe : MonoBehaviour
    {
        [SerializeField] private bool requireGroundContact = true;
        [SerializeField, Min(0.01f)] private float startHeight = 0.55f;
        [SerializeField, Min(0.01f)] private float groundReach = 0.95f;
        [SerializeField, Min(0.01f)] private float probeRadius = 0.12f;
        [SerializeField, Range(0f, 89f)] private float maximumSlopeDegrees = 45f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private readonly RaycastHit[] contacts = new RaycastHit[64];
        private System.Predicate<Collider> groundFilter;
        public void SetGroundFilter(System.Predicate<Collider> filter)=>groundFilter=filter;
        public bool RequireGroundContact { get { return requireGroundContact; } }

        public void Configure(bool required, float height = 0.55f, float reach = 0.95f,
            float radius = 0.12f, float maxSlope = 45f)
        {
            requireGroundContact = required;
            startHeight = Mathf.Max(0.01f, OmniDriveMath.NonNegative(height));
            groundReach = Mathf.Max(0.01f, OmniDriveMath.NonNegative(reach));
            probeRadius = Mathf.Max(0.01f, OmniDriveMath.NonNegative(radius));
            maximumSlopeDegrees = Mathf.Clamp(OmniDriveMath.NonNegative(maxSlope), 0f, 89f);
        }

        public bool TouchesGround(Rigidbody vehicle)
        {
            if (!requireGroundContact) return true;
            if (vehicle == null) return false;
            Vector3 origin = transform.position + Vector3.up * startHeight;
            int count = Physics.SphereCastNonAlloc(origin, probeRadius, Vector3.down,
                contacts, startHeight + groundReach, groundLayers, QueryTriggerInteraction.Ignore);
            // The buffer may be saturated by self-colliders. Refuse instead of falsely claiming ground.
            if (count == contacts.Length) return false;
            float minimumDot = Mathf.Cos(maximumSlopeDegrees * Mathf.Deg2Rad);
            for (int i = 0; i < count; i++)
            {
                Collider hit = contacts[i].collider;
                if (hit == null || hit.transform.IsChildOf(vehicle.transform)) continue;
                if(groundFilter!=null&&!groundFilter(hit))continue;
                if (Vector3.Dot(contacts[i].normal, Vector3.up) >= minimumDot) return true;
            }
            return false;
        }

        private void OnValidate()
        {
            startHeight = Mathf.Max(0.01f, OmniDriveMath.NonNegative(startHeight));
            groundReach = Mathf.Max(0.01f, OmniDriveMath.NonNegative(groundReach));
            probeRadius = Mathf.Max(0.01f, OmniDriveMath.NonNegative(probeRadius));
            maximumSlopeDegrees = Mathf.Clamp(OmniDriveMath.NonNegative(maximumSlopeDegrees), 0f, 89f);
        }
    }
}
