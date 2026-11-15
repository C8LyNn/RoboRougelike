using UnityEngine;

namespace RoboRougelike.Mechanics.Rockets
{
    // Optional game-side boundary handling; the rocket package retains its
    // own solid-hit ordering and does not depend on any particular shield.
    public interface IRocketFlightBoundary
    {
        bool TryIntercept(RocketProjectile projectile,Vector3 origin,Vector3 direction,float distance);
    }
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    [DisallowMultipleComponent]
    public sealed class RocketProjectile : MonoBehaviour, IRocketInterceptable
    {
        private static readonly System.Collections.Generic.HashSet<RocketProjectile> activeFlights =
            new System.Collections.Generic.HashSet<RocketProjectile>();
        public static int ActiveFlightCount => activeFlights.Count;
        [SerializeField] private GameObject optionalExplosionVfxPrefab;
        private Rigidbody body;
        private SphereCollider sphere;
        // Flight queries execute on Unity's main thread. Share bounded scratch
        // storage, but isolate a nested physics callback if one reenters flight.
        private const int FlightQueryRetentionLimit = 256;
        private sealed class FlightQueryBuffers
        {
            public RaycastHit[] Casts = new RaycastHit[32];
            public Collider[] Overlaps = new Collider[32];
        }
        private static readonly FlightQueryBuffers sharedFlightBuffers = new FlightQueryBuffers();
        private static bool sharedFlightBuffersInUse;
        private sealed class BlastQueryBuffers
        {
            public Collider[] Overlaps=new Collider[32];
            public RaycastHit[] Rays=new RaycastHit[32];
            public bool Grow(RocketSpec spec)
            {
                bool grew=false;
                if(Overlaps.Length<spec.MaxBlastColliders)
                {System.Array.Resize(ref Overlaps,Mathf.Min(Overlaps.Length*2,spec.MaxBlastColliders));grew=true;}
                if(Rays.Length<spec.MaxCastHits)
                {System.Array.Resize(ref Rays,Mathf.Min(Rays.Length*2,spec.MaxCastHits));grew=true;}
                return grew;
            }
        }
        private static readonly BlastQueryBuffers sharedBlastBuffers=new BlastQueryBuffers();
        private static bool sharedBlastBuffersInUse;
        private RocketSpec spec;
        private Transform ownerRoot;
        private Transform guidedTarget;
        private GameObject attacker;
        private Vector3 forward;
        private Vector3 inheritedVelocity;
        private float speed, age, hitPoints;
        private bool initialized, resolved, checkedLaunchClearance;
        private IRocketFlightBoundary flightBoundary;
        private System.Action<RocketProjectile> retirementHandler;

        // Opt-in only: standalone package rockets retain their original
        // destroy-on-resolution behavior unless the integration supplies a pool.
        public void SetRetirementHandler(System.Action<RocketProjectile> handler)=>retirementHandler=handler;
        public bool PrepareForReuse()
        {
            if(gameObject.activeSelf||!resolved)return false;
            initialized=false;resolved=false;checkedLaunchClearance=false;
            ownerRoot=null;guidedTarget=null;attacker=null;flightBoundary=null;
            forward=Vector3.zero;inheritedVelocity=Vector3.zero;speed=age=hitPoints=0;
            return true;
        }

        // Notification only; original flight, blast and interception remain authoritative.
        public static event System.Action<RocketProjectile,Vector3,Vector3> Detonated;
        public bool IsResolved => resolved;
        public bool IsInitialized => initialized;
        public bool HasActiveOwner => initialized && ownerRoot != null && ownerRoot.gameObject.activeInHierarchy;
        public bool ExplodesWhenIntercepted => initialized&&spec.ExplodeWhenIntercepted;
        public float HitPoints => hitPoints;
        public Transform OwnerRoot => ownerRoot;
        public Vector3 FlightVelocity => forward*speed+inheritedVelocity;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            sphere = GetComponent<SphereCollider>();
        }
        private void OnEnable(){if(initialized&&!resolved)activeFlights.Add(this);}
        private void OnDisable(){activeFlights.Remove(this);}
        private void OnDestroy(){activeFlights.Remove(this);}

        public bool TryInitialize(RocketSpec snapshot, Rigidbody firingBody, Transform owner,
            GameObject attackerSource, Transform target, Vector3 firedDirection)
            =>TryInitializeInternal(snapshot,firingBody,owner,attackerSource,target,firedDirection,null);

        public bool TryInitializeWithInheritedVelocity(RocketSpec snapshot,Rigidbody firingBody,Transform owner,
            GameObject attackerSource,Transform target,Vector3 firedDirection,Vector3 inherited)
            =>TryInitializeInternal(snapshot,firingBody,owner,attackerSource,target,firedDirection,inherited);

        private bool TryInitializeInternal(RocketSpec snapshot,Rigidbody firingBody,Transform owner,
            GameObject attackerSource,Transform target,Vector3 firedDirection,Vector3? inherited)
        {
            if (initialized || firingBody == null || owner == null || firedDirection.sqrMagnitude < 1e-8f)
                return false;
            if (body == null) body = GetComponent<Rigidbody>();
            if (sphere == null) sphere = GetComponent<SphereCollider>();
            if (body == null || sphere == null) return false;
            spec = snapshot;
            ownerRoot = owner;
            attacker = attackerSource;
            guidedTarget = target;
            forward = firedDirection.normalized;
            inheritedVelocity = inherited ?? firingBody.GetPointVelocity(transform.position) * spec.InheritedVelocityFraction;
            speed = spec.InitialSpeed;
            age = 0f;
            hitPoints = spec.RocketHitPoints;
            sphere.isTrigger = true;
            sphere.radius = spec.CollisionRadius;
            sphere.center = Vector3.zero;
            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            flightBoundary = GetComponent<IRocketFlightBoundary>();
            initialized = true;
            if(isActiveAndEnabled)activeFlights.Add(this);
            return true;
        }

        private void FixedUpdate()
        {
            if (!initialized || resolved) return;
            if (RetireLostOwner()) return;
            float dt = Time.fixedDeltaTime;
            age += dt;
            if (age >= spec.Lifetime)
            {
                Resolve(transform.position, Vector3.up, false);
                return;
            }
            if (guidedTarget != null && spec.GuidanceDegreesPerSecond > 0f)
                forward = RocketMath.Steer(forward, guidedTarget.position - transform.position,
                    spec.GuidanceDegreesPerSecond, dt);
            speed = RocketMath.SpeedAfter(speed, spec.Acceleration, spec.MaxSpeed, dt);
            Vector3 step = (forward * speed + inheritedVelocity) * dt;
            if (step.sqrMagnitude < 1e-8f) return;
            float distance = step.magnitude;
            Vector3 direction = step / distance;
            bool shared = !sharedFlightBuffersInUse;
            var buffers = shared ? sharedFlightBuffers : new FlightQueryBuffers();
            if (shared) sharedFlightBuffersInUse = true;
            try
            {
                // SphereCast does not detect colliders already containing the origin.
                if (!checkedLaunchClearance)
                {
                    checkedLaunchClearance = true;
                    int initialCount;Collider[] initialHits;
                    while(true)
                    {
                        initialCount=Physics.OverlapSphereNonAlloc(body.position,spec.CollisionRadius,
                            buffers.Overlaps,spec.ImpactMask,QueryTriggerInteraction.Ignore);
                        if(initialCount<buffers.Overlaps.Length){initialHits=buffers.Overlaps;break;}
                        if(buffers.Overlaps.Length>=Mathf.Min(spec.MaxBlastColliders,FlightQueryRetentionLimit))
                        {
                            initialHits=Physics.OverlapSphere(body.position,spec.CollisionRadius,spec.ImpactMask,QueryTriggerInteraction.Ignore);
                            initialCount=initialHits.Length;break;
                        }
                        System.Array.Resize(ref buffers.Overlaps,Mathf.Min(Mathf.Min(spec.MaxBlastColliders,FlightQueryRetentionLimit),buffers.Overlaps.Length*2));
                    }
                    for (int i = 0; i < initialCount; i++)
                    {
                        if (!IgnoreFlyingCollision(initialHits[i]))
                        {
                            Resolve(body.position, Vector3.up, false); // no through-wall launch
                            return;
                        }
                    }
                }
                int count;RaycastHit[] impactHits;
                while(true)
                {
                    count=Physics.SphereCastNonAlloc(body.position,spec.CollisionRadius,
                        direction,buffers.Casts,distance,spec.ImpactMask,QueryTriggerInteraction.Ignore);
                    if(count<buffers.Casts.Length){impactHits=buffers.Casts;break;}
                    if(buffers.Casts.Length>=Mathf.Min(spec.MaxCastHits,FlightQueryRetentionLimit))
                    {
                        impactHits=Physics.SphereCastAll(body.position,spec.CollisionRadius,direction,distance,spec.ImpactMask,QueryTriggerInteraction.Ignore);
                        count=impactHits.Length;break;
                    }
                    System.Array.Resize(ref buffers.Casts,Mathf.Min(Mathf.Min(spec.MaxCastHits,FlightQueryRetentionLimit),buffers.Casts.Length*2));
                }
                int nearest = -1;
                float nearestDistance = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    Collider other = impactHits[i].collider;
                    if (IgnoreFlyingCollision(other)) continue;
                    if (impactHits[i].distance < nearestDistance)
                    {
                        nearest = i;
                        nearestDistance = impactHits[i].distance;
                    }
                }
                float boundaryDistance=nearest>=0?Mathf.Max(0f,nearestDistance-.0001f):distance;
                if(boundaryDistance>0f&&flightBoundary!=null&&flightBoundary.TryIntercept(this,body.position,direction,boundaryDistance))return;
                if (nearest >= 0)
                {
                    RaycastHit hit = impactHits[nearest];
                    Resolve(hit.point + hit.normal * 0.02f, hit.normal, true);
                    return;
                }
                body.MovePosition(body.position + step);
                if (forward.sqrMagnitude > 0.0001f)
                {
                    Vector3 up = Mathf.Abs(Vector3.Dot(forward.normalized, Vector3.up)) > 0.98f
                        ? Vector3.forward : Vector3.up;
                    body.MoveRotation(Quaternion.LookRotation(forward, up));
                }
            }
            finally { if (shared) sharedFlightBuffersInUse = false; }
        }

        private bool IgnoreFlyingCollision(Collider other)
        {
            return other == null || other.isTrigger || other.transform.IsChildOf(transform) ||
                (ownerRoot != null && other.transform.IsChildOf(ownerRoot));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!initialized || resolved || RetireLostOwner() || IgnoreFlyingCollision(other)) return;
            if (((1 << other.gameObject.layer) & spec.ImpactMask) == 0) return;
            Resolve(body.position, forward, true);
        }

        public void ApplyInterceptionDamage(float amount)
        {
            if (!initialized || resolved || RetireLostOwner() || amount <= 0f || float.IsNaN(amount)) return;
            hitPoints -= amount;
            if (hitPoints <= 0f)
                Resolve(body.position, Vector3.up, spec.ExplodeWhenIntercepted);
        }

        private void Resolve(Vector3 point, Vector3 normal, bool dealDamage)
        {
            if (resolved || RetireLostOwner()) return;
            resolved = true;
            activeFlights.Remove(this);
            if (dealDamage)
            {
                RocketBlastResult result=ResolveBlast(point,normal);
                if (result.Overflow)
                    Debug.LogWarning("WPN-004: blast overlap overflow; no explosion damage applied.", this);
                try{Detonated?.Invoke(this,point,normal);}catch(System.Exception error){Debug.LogException(error);}
            }
            if (optionalExplosionVfxPrefab != null)
                Instantiate(optionalExplosionVfxPrefab, point, Quaternion.identity);
            Retire();
        }

        private RocketBlastResult ResolveBlast(Vector3 point,Vector3 normal)
        {
            // Damage callbacks can trigger another explosion before this one
            // finishes; that nested blast must never overwrite our results.
            bool shared=!sharedBlastBuffersInUse;
            var buffers=shared?sharedBlastBuffers:new BlastQueryBuffers();
            if(shared)sharedBlastBuffersInUse=true;
            try
            {
                RocketBlastResult result;
                do
                {
                    result=RocketBlastResolver.Resolve(point,normal,spec,ownerRoot,attacker,
                        buffers.Overlaps,buffers.Rays,flightBoundary as IRocketBlastBoundary);
                    if(!result.Overflow)return result;
                }while(buffers.Grow(spec));
                // Each incomplete attempt is atomic. If configured buffers
                // still overflow, retry with space for every active collider.
                int capacity=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length+1;
                return RocketBlastResolver.Resolve(point,normal,spec,ownerRoot,attacker,
                    new Collider[capacity],new RaycastHit[capacity],flightBoundary as IRocketBlastBoundary);
            }
            finally
            {
                System.Array.Clear(buffers.Overlaps,0,buffers.Overlaps.Length);
                if(shared)sharedBlastBuffersInUse=false;
            }
        }

        private bool RetireLostOwner()
        {
            if (!initialized || HasActiveOwner) return false;
            resolved = true;gameObject.SetActive(false);Retire();return true;
        }
        private void Retire()
        {
            if(retirementHandler==null){Destroy(gameObject);return;}
            gameObject.SetActive(false);
            try{retirementHandler(this);}
            catch(System.Exception error){Debug.LogException(error);Destroy(gameObject);}
        }
    }
}
