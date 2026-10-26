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
        [SerializeField, Min(.1f)] private float maxContinuousSpeed = 15f;
        [SerializeField] private LayerMask worldCollisionMask = ~0;

        private readonly RaycastHit[] worldHits = new RaycastHit[128];
        private readonly Collider[] overlaps = new Collider[64];
        private Camera attachedCamera;
        private Vector3 velocity;
        private bool initialized;
        public Camera001Framing.Result LastSolution { get; private set; }
        public bool WorldClipped { get; private set; }
        public bool WorldOverlapAtAim { get; private set; }
        public bool WorldOverlapAtCamera { get; private set; }
        public bool ActualOwnHullIntersectsCrosshair { get; private set; }
        public float LastCameraStepMeters { get; private set; }
        public float LastAimErrorDegrees { get; private set; }
        // Receiver-only semantic filter (for transient projectiles/wreckage).
        // Null preserves the standalone package policy and its original tests.
        public Func<Collider,bool> WorldColliderFilter {get;set;}

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

        /// <summary>
        /// One camera owner. Called by LateUpdate in production; tests can use the
        /// deterministic explicit step without injecting LMB/RMB or game input.
        /// Pose safety always checks the *actual* rendered position and rotation.
        /// </summary>
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
            float radius = SafeCameraRadius();

            WorldClipped = false;
            WorldOverlapAtAim = IsWorldOverlapping(aim, radius);
            Vector3 desired = LastSolution.DesiredCameraWorld;

            // Never use "desired = aim" when the pivot is inside a wall.
            // Such a fallback caused a 9.1732m displacement in QA #234.
            // Keep the previous safe pose while the pivot is blocked; no snap.
            if (WorldOverlapAtAim)
            {
                WorldClipped = true;
                if (initialized) desired = transform.position;
            }
            else
            {
                float safe = CastWorldClearance(aim, direction, length, radius);
                if (safe < length - .0001f)
                {
                    WorldClipped = true;
                    desired = aim + direction * safe;
                }
            }

            Vector3 previous = transform.position;
            bool reset = !initialized || deltaTime <= 0f;
            Vector3 candidate = desired;
            if (!reset)
            {
                Vector3 eased = Vector3.SmoothDamp(previous, desired,
                    ref velocity, Mathf.Max(.015f, smoothSeconds),
                    Mathf.Infinity, deltaTime);
                // A genuine orbit/follow update can never traverse 9m in one
                // ordinary frame; intentional Configure/ResetSmoothing is distinct.
                candidate = Vector3.MoveTowards(previous, eased,
                    Mathf.Max(.1f, maxContinuousSpeed) * deltaTime);
                Vector3 movement = candidate - previous;
                float distance = movement.magnitude;
                if (distance > .00001f)
                {
                    float clearance = CastWorldClearance(previous,
                        movement / distance, distance, radius);
                    if (clearance < distance - .0001f)
                    {
                        candidate = previous + movement / distance * clearance;
                        velocity = Vector3.zero;
                        WorldClipped = true;
                    }
                }
            }

            // The camera volume (conservative near-plane sphere), NOT just its
            // centre ray, must have actual world clearance. A blocked/overlapped
            // previous pose is reported, never silently counted as safe.
            if (IsWorldOverlapping(candidate, radius))
            {
                WorldClipped = true;
                velocity = Vector3.zero;
                if (!reset && !IsWorldOverlapping(previous, radius))
                    candidate = previous;
                // During initial placement any irrecoverable overlap stays RED.
            }

            transform.position = candidate;
            // Solve rotation from the FINAL smoothed/clipped camera position.
            // Applying desired rotation to an earlier smoothed position caused
            // 26 real temporary own-hull reticle intersections in QA #234.
            Vector3 actualSightline = aim - candidate;
            Quaternion actualRotation = actualSightline.sqrMagnitude > .0001f
                ? Quaternion.LookRotation(actualSightline.normalized, Vector3.up)
                : LastSolution.Rotation;
            // Even when the desired pivot intersects a wall, keep the
            // crosshair aligned with the final actual position; only the
            // translational orbit is held, not the sightline / weapon aim.
            transform.rotation = actualRotation; // atomic final-pose commit

            initialized = true;
            LastCameraStepMeters = reset ? 0f : Vector3.Distance(previous, candidate);
            LastAimErrorDegrees = Vector3.Angle(transform.forward, aim - transform.position);
            WorldOverlapAtCamera = IsWorldOverlapping(transform.position, radius);
            ActualOwnHullIntersectsCrosshair = HitsOwnHullFromActualPose(
                transform.position, transform.forward, bounds);
        }

        private float SafeCameraRadius()
        {
            // Conservative sphere containing the near-plane rectangle.
            float near = Mathf.Max(.01f, attachedCamera.nearClipPlane);
            float halfHeight = near * Mathf.Tan(attachedCamera.fieldOfView * Mathf.Deg2Rad * .5f);
            float halfWidth = halfHeight * attachedCamera.aspect;
            return Mathf.Max(.01f, collisionRadius,
                Mathf.Sqrt(near * near + halfHeight * halfHeight + halfWidth * halfWidth));
        }

        private bool IsWorldOverlapping(Vector3 center, float radius)
        {
            int count = Physics.OverlapSphereNonAlloc(center,
                radius + Mathf.Max(0f, worldPadding), overlaps,
                worldCollisionMask, QueryTriggerInteraction.Ignore);
            if (count >= overlaps.Length) return true; // overflow fails closed
            for (int i = 0; i < count; i++)
                if (IsWorld(overlaps[i])) return true;
            return false;
        }

        private float CastWorldClearance(Vector3 start, Vector3 direction,
            float distance, float radius)
        {
            if (distance <= .00001f) return 0f;
            int count = Physics.SphereCastNonAlloc(start, radius,
                direction, worldHits, distance, worldCollisionMask,
                QueryTriggerInteraction.Ignore);
            if (count >= worldHits.Length) return 0f; // fail closed
            float allowed = distance;
            for (int i = 0; i < count; i++)
            {
                if (!IsWorld(worldHits[i].collider)) continue;
                allowed = Mathf.Min(allowed,
                    Mathf.Max(0f, worldHits[i].distance - Mathf.Max(0f, worldPadding)));
            }
            return allowed;
        }

        private bool HitsOwnHullFromActualPose(Vector3 origin,
            Vector3 forward, Bounds bounds)
        {
            if (forward.sqrMagnitude < .000001f) return true;
            Ray ray = new Ray(origin, forward);
            float entry;
            if (bounds.Contains(origin)) return true;
            if (bounds.IntersectRay(ray, out entry) && entry >= 0f)
                return true; // conservative protection for unloaded colliders
            foreach (var c in vehicleRoot.GetComponentsInChildren<Collider>())
            {
                if (c == null || !c.enabled || c.isTrigger || !c.gameObject.activeInHierarchy)
                    continue;
                RaycastHit hit;
                if (c.Raycast(ray, out hit, Mathf.Infinity)) return true;
                // Collider.Raycast alone cannot report a ray starting inside.
                if ((c.ClosestPoint(origin) - origin).sqrMagnitude < .000001f)
                    return true;
            }
            return false;
        }

        private bool IsWorld(Collider c)
        {
            return c != null && c.enabled && !c.isTrigger &&
                c.gameObject.activeInHierarchy &&
                (WorldColliderFilter==null||WorldColliderFilter(c)) &&
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
