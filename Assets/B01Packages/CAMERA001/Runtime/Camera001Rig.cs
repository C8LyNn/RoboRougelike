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
        public Bounds LastHullBounds {get;private set;}
        public int SmoothingResetCount {get;private set;}
        public Vector3 RequestedOrbit => new Vector3(yaw,pitch,requestedDistance);
        // Receiver policy: filter follow geometry once, keep deliberate mouse orbit direct.
        // Standalone package callers retain the original translation policy by default.
        public bool ReceiverResponsiveMotion {get;set;}
        public float ReceiverHorizontalDamping {get;set;}=.06f;
        public float ReceiverVerticalDamping {get;set;}=.12f;
        public float ReceiverFramingDamping {get;set;}=.22f;
        public float ReceiverZoomMaximum {get;set;}
        public float LastFollowStepMeters {get;private set;}
        public float LastOrbitInputStepMeters {get;private set;}
        Vector3 filteredCenter,centerVelocity,filteredExtents;
        Vector2 previousOrbit;
        bool boundsInitialized,orbitInitialized;
        // Receiver-only semantic filter (for transient projectiles/wreckage).
        // Null preserves the standalone package policy and its original tests.
        public Func<Collider,bool> WorldColliderFilter {get;set;}
        public Func<Transform,Bounds> ReceiverHullBounds {get;set;}
        public bool RequireFullHullFraming {get;set;}
        public float ReceiverFitDistance {get;private set;}
        public bool ReceiverFramingSaturated {get;private set;}
        float receiverFollowSpeed;
        public float ReceiverFollowSpeed {
            get=>receiverFollowSpeed;
            set=>receiverFollowSpeed=float.IsNaN(value)||float.IsInfinity(value)?0:Mathf.Clamp(value,0,100);
        }
        public float EffectiveContinuousSpeedLimit=>Mathf.Max(Mathf.Max(.1f,maxContinuousSpeed),
            receiverFollowSpeed>maxContinuousSpeed?receiverFollowSpeed+3:0);

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
            requestedDistance = Mathf.Clamp(zoomDistance, minDistance, Mathf.Max(maxDistance,ReceiverZoomMaximum));
        }

        public void OrbitBy(float deltaYawDegrees, float deltaPitchDegrees, float zoomDelta)
        {
            SetOrbit(yaw + deltaYawDegrees, pitch + deltaPitchDegrees,
                     requestedDistance + zoomDelta);
        }

        public void ResetSmoothing()
        {
            SmoothingResetCount++;
            velocity = Vector3.zero;
            initialized = false;
            boundsInitialized=orbitInitialized=false;centerVelocity=Vector3.zero;
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
            Bounds rawBounds = ReceiverHullBounds!=null?ReceiverHullBounds(vehicleRoot):GetHullBounds(vehicleRoot);
            LastHullBounds=rawBounds;
            Bounds bounds=ReceiverResponsiveMotion?FilterFollowBounds(rawBounds,deltaTime):rawBounds;
            var request = new Camera001Framing.Request {
                HullBounds = bounds,
                YawDegrees = yaw,
                PitchDegrees = pitch,
                RequestedDistance = requestedDistance,
                MinimumDistance = Mathf.Max(.1f, minDistance),
                MaximumDistance = Mathf.Max(minDistance, maxDistance,ReceiverZoomMaximum),
                VerticalFovDegrees = attachedCamera.fieldOfView,
                Aspect = attachedCamera.aspect,
                SafetyHeadroom = Mathf.Max(0f, headroom),
                AimLead = Mathf.Max(0f, aimLead)
            };
            LastSolution = Camera001Framing.Solve(request);
            ReceiverFitDistance=LastSolution.FitDistance;
            ReceiverFramingSaturated=LastSolution.FitSaturated;
            if(RequireFullHullFraming){
                // Receiver correction uses this rig's authoritative request.
                // Centre-only fit omitted the ABOVE/AHEAD aim offset.
                float tanY=Mathf.Tan(request.VerticalFovDegrees*Mathf.Deg2Rad*.5f)*.86f;
                float tanX=tanY*request.Aspect;
                foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1}){
                    var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
                    var v=Quaternion.Inverse(LastSolution.Rotation)*(corner-LastSolution.AimWorld);
                    ReceiverFitDistance=Mathf.Max(ReceiverFitDistance,Mathf.Abs(v.x)/tanX-v.z,
                        Mathf.Abs(v.y)/tanY-v.z,attachedCamera.nearClipPlane+.01f-v.z);
                }
                // Manual zoom retains its configured range. Receiver auto-framing
                // must accommodate the measured hull rather than crop a legal
                // heavy/wide construction at the standalone demo's 18m cap.
                request.MaximumDistance=Mathf.Max(request.MaximumDistance,ReceiverFitDistance);
                request.RequestedDistance=Mathf.Clamp(Mathf.Max(request.RequestedDistance,ReceiverFitDistance),request.MinimumDistance,request.MaximumDistance);
                LastSolution=Camera001Framing.Solve(request);
                ReceiverFramingSaturated=ReceiverFitDistance>request.MaximumDistance;
            }
            if(ReceiverResponsiveMotion)
                LastSolution=ProtectResponsiveFraming(LastSolution,rawBounds,request);
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
            LastFollowStepMeters=LastOrbitInputStepMeters=0;
            if (!reset)
            {
                Vector3 orbitBase=previous;
                if(ReceiverResponsiveMotion)
                {
                    if(orbitInitialized&&!WorldOverlapAtAim&&
                        (Mathf.Abs(Mathf.DeltaAngle(previousOrbit.x,yaw))>.00001f||Mathf.Abs(previousOrbit.y-pitch)>.00001f))
                    {
                        var oldRequest=request;oldRequest.YawDegrees=previousOrbit.x;oldRequest.PitchDegrees=previousOrbit.y;
                        oldRequest.RequestedDistance=LastSolution.Distance;
                        var oldSolution=ProtectResponsiveFraming(Camera001Framing.Solve(oldRequest),rawBounds,oldRequest);
                        Quaternion turn=LastSolution.Rotation*Quaternion.Inverse(oldSolution.Rotation);
                        orbitBase=aim+turn*(previous-oldSolution.AimWorld);
                    }
                    candidate=Vector3.MoveTowards(orbitBase,desired,EffectiveContinuousSpeedLimit*deltaTime);
                }
                else
                {
                    Vector3 eased = Vector3.SmoothDamp(previous, desired,
                        ref velocity, Mathf.Max(.015f, smoothSeconds),Mathf.Infinity,deltaTime);
                    candidate=Vector3.MoveTowards(previous,eased,EffectiveContinuousSpeedLimit*deltaTime);
                }
                LastOrbitInputStepMeters=Vector3.Distance(previous,orbitBase);
                LastFollowStepMeters=Vector3.Distance(orbitBase,candidate);
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
            previousOrbit=new Vector2(yaw,pitch);orbitInitialized=true;
            LastCameraStepMeters = reset ? 0f : Vector3.Distance(previous, candidate);
            LastAimErrorDegrees = Vector3.Angle(transform.forward, aim - transform.position);
            WorldOverlapAtCamera = IsWorldOverlapping(transform.position, radius);
            ActualOwnHullIntersectsCrosshair = HitsOwnHullFromActualPose(
                transform.position, transform.forward, rawBounds);
        }

        Bounds FilterFollowBounds(Bounds raw,float dt)
        {
            if(!boundsInitialized||dt<=0){filteredCenter=raw.center;filteredExtents=raw.extents;boundsInitialized=true;centerVelocity=Vector3.zero;}
            else
            {
                filteredCenter.x=Mathf.SmoothDamp(filteredCenter.x,raw.center.x,ref centerVelocity.x,Mathf.Max(.015f,ReceiverHorizontalDamping),Mathf.Infinity,dt);
                filteredCenter.z=Mathf.SmoothDamp(filteredCenter.z,raw.center.z,ref centerVelocity.z,Mathf.Max(.015f,ReceiverHorizontalDamping),Mathf.Infinity,dt);
                filteredCenter.y=Mathf.SmoothDamp(filteredCenter.y,raw.center.y,ref centerVelocity.y,Mathf.Max(.015f,ReceiverVerticalDamping),Mathf.Infinity,dt);
                filteredExtents=Vector3.Lerp(filteredExtents,raw.extents,1-Mathf.Exp(-dt/Mathf.Max(.015f,ReceiverFramingDamping)));
            }
            return new Bounds(filteredCenter,filteredExtents*2);
        }

        Camera001Framing.Result ProtectResponsiveFraming(Camera001Framing.Result solution,Bounds raw,Camera001Framing.Request request)
        {
            // Filtered geometry may lag a bump or a newly exposed corner. The actual
            // hull, not the filtered hull, remains the authority for safety/framing.
            var aim=solution.AimWorld;var heading=Quaternion.Euler(0,request.YawDegrees,0)*Vector3.forward;
            float slope=Mathf.Tan(request.PitchDegrees*Mathf.Deg2Rad),requiredY=float.NegativeInfinity;
            for(int i=0;i<8;i++)
            {
                var e=raw.extents;var corner=raw.center+new Vector3((i&1)==0?-e.x:e.x,(i&2)==0?-e.y:e.y,(i&4)==0?-e.z:e.z);
                requiredY=Mathf.Max(requiredY,corner.y+slope*Vector3.Dot(corner-aim,heading));
            }
            aim.y=Mathf.Max(aim.y,requiredY+.08f);
            float distance=solution.Distance;
            if(RequireFullHullFraming)
            {
                float tanY=Mathf.Tan(request.VerticalFovDegrees*Mathf.Deg2Rad*.5f)*.98f,tanX=tanY*request.Aspect;
                for(int i=0;i<8;i++)
                {
                    var e=raw.extents;var corner=raw.center+new Vector3((i&1)==0?-e.x:e.x,(i&2)==0?-e.y:e.y,(i&4)==0?-e.z:e.z);
                    var v=Quaternion.Inverse(solution.Rotation)*(corner-aim);
                    distance=Mathf.Max(distance,Mathf.Abs(v.x)/tanX-v.z,Mathf.Abs(v.y)/tanY-v.z,attachedCamera.nearClipPlane+.01f-v.z);
                }
            }
            solution.AimWorld=aim;solution.Distance=distance;
            solution.DesiredCameraWorld=aim+solution.Rotation*Vector3.back*distance;
            return solution;
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
