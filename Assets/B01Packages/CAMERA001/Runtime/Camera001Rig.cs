using System;
using UnityEngine;

namespace B01.CAMERA001
{
    /// <summary>
    /// OPT-IN stand-alone camera. Never attach while the production P12 camera
    /// remains enabled; only the receiver may choose the integration path.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(100)]
    public sealed class Camera001Rig : MonoBehaviour
    {
        [SerializeField] private Transform vehicleRoot;
        [SerializeField, Range(-8f, 70f)] private float pitch = 18f;
        [SerializeField] private float yaw;
        [SerializeField] private float requestedDistance = 8f;
        [SerializeField] private float minDistance = 3f;
        [SerializeField] private float maxDistance = 18f;
        [SerializeField] private float headroom = .65f;
        [SerializeField] private float aimLead = 1.7f;
        [SerializeField] private float collisionRadius = .26f;
        [SerializeField] private float worldPadding = .14f;
        [SerializeField] private float smoothSeconds = .12f;
        [SerializeField] private LayerMask worldCollisionMask = ~0;

        private readonly RaycastHit[] worldHits = new RaycastHit[128];
        private readonly Collider[] overlaps = new Collider[64];
        private Camera attachedCamera;
        private Vector3 velocity;
        private bool initialized;
        public Camera001Framing.Result LastSolution { get; private set; }
        public bool WorldClipped { get; private set; }
        public bool WorldOverlapAtAim { get; private set; }

        public void Configure(Transform root, Camera camera)
        {
            if (camera == null || camera.gameObject != gameObject)
                throw new ArgumentException("CAMERA001_CAMERA_MUST_BE_ON_RIG");
            vehicleRoot = root;
            attachedCamera = camera;
            ResetSmoothing();
        }

        public void SetOrbit(float yawDegrees, float pitchDegrees, float zoomDistance)
        {
            yaw = Mathf.Repeat(yawDegrees, 360f);
            pitch = Mathf.Clamp(pitchDegrees, -8f, 70f);
            requestedDistance = Mathf.Clamp(zoomDistance, minDistance, maxDistance);
        }

        public void OrbitBy(float deltaYawDegrees, float deltaPitchDegrees, float zoomDelta)
        {
            SetOrbit(yaw + deltaYawDegrees, pitch + deltaPitchDegrees,
                     requestedDistance + zoomDelta);
        }

        public void ResetSmoothing()
        {
            velocity = Vector3.zero;
            initialized = false;
        }

        private void Awake() { attachedCamera = GetComponent<Camera>(); }

        private void LateUpdate()
        {
            StepCamera(Time.deltaTime);
        }

        /// <summary>Deterministic explicit test entry. Does not read or own gameplay input.</summary>
        public void StepCamera(float deltaTime)
        {
            if (vehicleRoot == null || attachedCamera == null) return;
            Bounds bounds = GetHullBounds(vehicleRoot);
            var request = new Camera001Framing.Request {
                HullBounds = bounds,
                YawDegrees = yaw,
                PitchDegrees = pitch,
                RequestedDistance = requestedDistance,
                MinimumDistance = Mathf.Max(.1f, minDistance),
                MaximumDistance = Mathf.Max(minDistance, maxDistance),
                VerticalFovDegrees = attachedCamera.fieldOfView,
                Aspect = attachedCamera.aspect,
                SafetyHeadroom = Mathf.Max(0f, headroom),
                AimLead = Mathf.Max(0f, aimLead)
            };
            LastSolution = Camera001Framing.Solve(request);
            Vector3 aim = LastSolution.AimWorld;
            Vector3 away = LastSolution.DesiredCameraWorld - aim;
            float length = away.magnitude;
            Vector3 direction = away / Mathf.Max(length, .00001f);
            WorldClipped = false;
            WorldOverlapAtAim = false;

            // A dynamic camera must not pass through WORLD geometry. Ignore only
            // the vehicle's own hierarchy and triggers; do not disable any colliders.
            int overlapCount = Physics.OverlapSphereNonAlloc(aim,
                Mathf.Max(.01f, collisionRadius), overlaps,
                worldCollisionMask, QueryTriggerInteraction.Ignore);
            if (overlapCount >= overlaps.Length)
                WorldOverlapAtAim = true; // fail-closed inconclusive overlap query
            for (int i = 0; i < overlapCount && i < overlaps.Length; i++)
                if (IsWorld(overlaps[i]))
                    WorldOverlapAtAim = true;
            int count = Physics.SphereCastNonAlloc(aim, Mathf.Max(.01f, collisionRadius),
                direction, worldHits, length, worldCollisionMask,
                QueryTriggerInteraction.Ignore);
            float safe = length;
            if (count >= worldHits.Length) { safe = 0f; WorldClipped = true; }
            else for (int i = 0; i < count; i++)
            {
                Collider c = worldHits[i].collider;
                if (!IsWorld(c)) continue;
                safe = Mathf.Min(safe, Mathf.Max(0f, worldHits[i].distance -
                                 Mathf.Max(0f, worldPadding)));
                WorldClipped = true;
            }

            Vector3 desired = aim + direction * safe;
            // When the pivot itself overlaps scenery, stay with an explicitly
            // marked BLOCKED proposal rather than pretending the camera is safe.
            if (WorldOverlapAtAim) { desired = aim; WorldClipped = true; }

            if (!initialized || deltaTime <= 0f)
            {
                transform.position = desired;
                velocity = Vector3.zero;
                initialized = true;
            }
            else
            {
                // The smoothed intermediate position must ALSO be collision-safe;
                // recompute a spherecast from the current aim toward the candidate.
                Vector3 intermediate = Vector3.SmoothDamp(transform.position, desired,
                    ref velocity, Mathf.Max(.015f, smoothSeconds),
                    Mathf.Infinity, deltaTime);
                Vector3 candidate = intermediate - aim;
                float candidateLength = candidate.magnitude;
                if (candidateLength > .001f)
                {
                    Vector3 candDir = candidate / candidateLength;
                    int n = Physics.SphereCastNonAlloc(aim, Mathf.Max(.01f, collisionRadius),
                        candDir, worldHits, candidateLength, worldCollisionMask,
                        QueryTriggerInteraction.Ignore);
                    if (n >= worldHits.Length) { intermediate = aim; velocity = Vector3.zero; WorldClipped = true; }
                    else for (int i = 0; i < n; i++)
                        if (IsWorld(worldHits[i].collider))
                        {
                            float d = Mathf.Max(0f, worldHits[i].distance - Mathf.Max(0f, worldPadding));
                            if (d < (intermediate - aim).magnitude)
                            {
                                intermediate = aim + candDir * d;
                                velocity = Vector3.zero;
                                WorldClipped = true;
                            }
                        }
                }
                transform.position = intermediate;
            }
            transform.rotation = LastSolution.Rotation;
        }

        private bool IsWorld(Collider c)
        {
            return c != null && c.enabled && !c.isTrigger &&
                c.gameObject.activeInHierarchy &&
                (vehicleRoot == null || !c.transform.IsChildOf(vehicleRoot));
        }

        public static Bounds GetHullBounds(Transform root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            bool any = false;
            var total = new Bounds(root.position + Vector3.up, Vector3.one * 2f);
            foreach (var c in root.GetComponentsInChildren<Collider>())
            {
                if (c == null || !c.enabled || c.isTrigger || !c.gameObject.activeInHierarchy) continue;
                if (!any) { total = c.bounds; any = true; } else total.Encapsulate(c.bounds);
            }
            if (!any)
                foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                    if (renderer.enabled) { if (!any) { total = renderer.bounds; any = true; } else total.Encapsulate(renderer.bounds); }
            return total;
        }
    }
}
