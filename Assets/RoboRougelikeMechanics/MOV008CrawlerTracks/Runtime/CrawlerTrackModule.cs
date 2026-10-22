using UnityEngine;

namespace RoboRougelike.Mechanics.MOV008
{
    /// <summary>
    /// One physical track module, mounted as a child of the chassis Rigidbody.
    /// No per-link or per-track Rigidbody is created. Each module probes the floor independently.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrawlerTrackModule : MonoBehaviour
    {
        [Header("Placement")]
        [SerializeField] private CrawlerTrackSide side = CrawlerTrackSide.Left;
        [SerializeField] private bool operational = true;

        [Header("Ground probe - world down; length along chassis local forward")]
        [Min(0.05f)] [SerializeField] private float probeStartAboveModule = 0.25f;
        [Min(0.05f)] [SerializeField] private float probeReachBelowModule = 0.85f;
        [Min(0f)] [SerializeField] private float halfTrackLength = 0.75f;
        [Range(1, 3)] [SerializeField] private int minimumGroundSamples = 1;
        [Range(0f, 80f)] [SerializeField] private float maxSlopeDegrees = 45f;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private bool staticGroundOnly = true;

        [Header("Traction and load")]
        [Min(0f)] [SerializeField] private float nominalLoadKg = 2500f;
        [Min(0f)] [SerializeField] private float maxDriveForceN = 15000f;
        [Min(0f)] [SerializeField] private float maxBrakeForceN = 22000f;
        [Min(0f)] [SerializeField] private float driveGripCoefficient = 1.25f;
        [Min(0f)] [SerializeField] private float sideGripCoefficient = 1.6f;

        private RaycastHit[] hits = new RaycastHit[16];
        private CrawlerTrackController owner;
        private System.Func<bool> availabilityFilter;
        private System.Predicate<Collider> groundFilter;
        private bool useMountAxes;
        public Vector3 ContactUp=>useMountAxes?transform.up:Vector3.up;
        public void SetUseMountAxes(bool value)=>useMountAxes=value;
        public void SetAvailabilityFilter(System.Func<bool> filter)=>availabilityFilter=filter;
        public void SetGroundFilter(System.Predicate<Collider> filter)=>groundFilter=filter;
        public void ConfigureTraction(float loadKg,float motorNewtons,float brakeNewtons)
        {
            nominalLoadKg=Mathf.Max(1,CrawlerTrackMath.Finite(loadKg));
            maxDriveForceN=Mathf.Max(0,CrawlerTrackMath.Finite(motorNewtons));
            maxBrakeForceN=Mathf.Max(0,CrawlerTrackMath.Finite(brakeNewtons));
        }

        public CrawlerTrackSide Side => side;
        public bool Operational => operational&&(availabilityFilter==null||availabilityFilter());
        public float NominalLoadKg => nominalLoadKg;
        public float MaxDriveForceN => maxDriveForceN;
        public float MaxBrakeForceN => maxBrakeForceN;
        public float DriveGripCoefficient => driveGripCoefficient;
        public float SideGripCoefficient => sideGripCoefficient;

        public void SetOperational(bool value) => operational = value;
        public void SetSide(CrawlerTrackSide value) => side = value;

        private void OnEnable() => BindOwner();
        private void OnDisable() => UnbindOwner();
        private void OnDestroy() => UnbindOwner();
        private void OnTransformParentChanged() => BindOwner();

        private void BindOwner()
        {
            CrawlerTrackController next = GetComponentInParent<CrawlerTrackController>();
            if (next == owner) return;
            UnbindOwner();
            owner = next;
            if (owner != null && isActiveAndEnabled) owner.Register(this);
        }

        private void UnbindOwner()
        {
            if (owner == null) return;
            owner.Unregister(this);
            owner = null;
        }

        public bool IsAvailableFor(Rigidbody chassis)
        {
            return chassis != null && Operational && isActiveAndEnabled &&
                   transform.IsChildOf(chassis.transform) &&
                   GetComponentInParent<Rigidbody>() == chassis;
        }

        private void OnValidate()
        {
            probeStartAboveModule = Mathf.Max(0.05f, CrawlerTrackMath.Finite(probeStartAboveModule));
            probeReachBelowModule = Mathf.Max(0.05f, CrawlerTrackMath.Finite(probeReachBelowModule));
            halfTrackLength = Mathf.Max(0f, CrawlerTrackMath.Finite(halfTrackLength));
            minimumGroundSamples = Mathf.Clamp(minimumGroundSamples, 1, 3);
            maxSlopeDegrees = Mathf.Clamp(maxSlopeDegrees, 0f, 80f);
            nominalLoadKg = Mathf.Max(0f, CrawlerTrackMath.Finite(nominalLoadKg));
            maxDriveForceN = Mathf.Max(0f, CrawlerTrackMath.Finite(maxDriveForceN));
            maxBrakeForceN = Mathf.Max(0f, CrawlerTrackMath.Finite(maxBrakeForceN));
            driveGripCoefficient = Mathf.Max(0f, CrawlerTrackMath.Finite(driveGripCoefficient));
            sideGripCoefficient = Mathf.Max(0f, CrawlerTrackMath.Finite(sideGripCoefficient));
        }

        public bool TryGroundContact(Rigidbody chassis, out Vector3 normal)
        {
            normal = Vector3.zero;
            if (!IsAvailableFor(chassis)) return false;
            int valid = 0;
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < 3; i++)
            {
                float zOffset = (i - 1) * halfTrackLength;
                Vector3 basePoint = transform.TransformPoint(new Vector3(0f, 0f, zOffset));
                Vector3 rayStart = basePoint + ContactUp * probeStartAboveModule;
                float distance = probeStartAboveModule + probeReachBelowModule;
                if (TryRaycastGround(chassis, rayStart, distance, out Vector3 groundNormal))
                {
                    valid++;
                    sum += groundNormal;
                }
            }
            if (valid < minimumGroundSamples || sum.sqrMagnitude < 0.0001f) return false;
            normal = sum.normalized;
            return true;
        }

        private bool TryRaycastGround(Rigidbody chassis, Vector3 origin, float distance, out Vector3 normal)
        {
            normal = Vector3.zero;
            int count;
            while (true)
            {
                count = Physics.RaycastNonAlloc(origin, -ContactUp, hits, distance,
                    groundLayers, QueryTriggerInteraction.Ignore);
                // Never use a truncated buffer as it might omit the true nearest valid hit.
                if (count < hits.Length) break;
                if (hits.Length >= 256) return false; // fail closed when scene is pathological
                hits = new RaycastHit[hits.Length * 2];
            }

            float nearest = float.PositiveInfinity;
            bool found = false;
            float minUp = Mathf.Cos(maxSlopeDegrees * Mathf.Deg2Rad);
            for (int i = 0; i < count; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null || collider.transform.IsChildOf(chassis.transform) ||
                    collider.attachedRigidbody == chassis) continue;
                if (staticGroundOnly && collider.attachedRigidbody != null) continue;
                if(groundFilter!=null&&!groundFilter(collider))continue;
                if (Vector3.Dot(hits[i].normal, ContactUp) < minUp) continue;
                if (hits[i].distance >= nearest) continue;
                nearest = hits[i].distance;
                normal = hits[i].normal;
                found = true;
            }
            return found;
        }
    }
}
