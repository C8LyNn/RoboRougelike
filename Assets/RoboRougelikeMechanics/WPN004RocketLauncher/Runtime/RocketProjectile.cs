using UnityEngine;

namespace RoboRougelike.Mechanics.Rockets
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    [DisallowMultipleComponent]
    public sealed class RocketProjectile : MonoBehaviour, IRocketInterceptable
    {
        [SerializeField] private GameObject optionalExplosionVfxPrefab;
        private Rigidbody body;
        private SphereCollider sphere;
        private RaycastHit[] castBuffer;
        private Collider[] blastBuffer;
        private RaycastHit[] occlusionBuffer;
        private RocketSpec spec;
        private Transform ownerRoot;
        private Transform guidedTarget;
        private GameObject attacker;
        private Vector3 forward;
        private Vector3 inheritedVelocity;
        private float speed, age, hitPoints;
        private bool initialized, resolved, checkedLaunchClearance;

        // Notification only; original flight, blast and interception remain authoritative.
        public static event System.Action<RocketProjectile,Vector3,Vector3> Detonated;
        public bool IsResolved => resolved;
        public bool IsInitialized => initialized;
        public float HitPoints => hitPoints;
        public Transform OwnerRoot => ownerRoot;
        public Vector3 FlightVelocity => forward*speed+inheritedVelocity;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            sphere = GetComponent<SphereCollider>();
        }

        public bool TryInitialize(RocketSpec snapshot, Rigidbody firingBody, Transform owner,
            GameObject attackerSource, Transform target, Vector3 firedDirection)
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
            inheritedVelocity = firingBody.GetPointVelocity(transform.position) * spec.InheritedVelocityFraction;
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
            castBuffer = new RaycastHit[spec.MaxCastHits];
            blastBuffer = new Collider[spec.MaxBlastColliders];
            occlusionBuffer = new RaycastHit[spec.MaxCastHits];
            initialized = true;
            return true;
        }

        private void FixedUpdate()
        {
            if (!initialized || resolved) return;
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
            // SphereCast does not detect colliders already containing the origin.
            if (!checkedLaunchClearance)
            {
                checkedLaunchClearance = true;
                int initialCount = Physics.OverlapSphereNonAlloc(body.position, spec.CollisionRadius,
                    blastBuffer, spec.ImpactMask, QueryTriggerInteraction.Ignore);
                if (initialCount >= blastBuffer.Length)
                {
                    Resolve(body.position, Vector3.up, false);
                    return;
                }
                for (int i = 0; i < initialCount; i++)
                {
                    if (!IgnoreFlyingCollision(blastBuffer[i]))
                    {
                        Resolve(body.position, Vector3.up, false); // no through-wall launch
                        return;
                    }
                }
            }
            int count = Physics.SphereCastNonAlloc(body.position, spec.CollisionRadius,
                direction, castBuffer, distance, spec.ImpactMask, QueryTriggerInteraction.Ignore);
            if (count >= castBuffer.Length)
            {
                Debug.LogWarning("WPN-004: impact query overflow; rocket removed without damage.", this);
                Resolve(body.position, Vector3.up, false);
                return;
            }
            int nearest = -1;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider other = castBuffer[i].collider;
                if (IgnoreFlyingCollision(other)) continue;
                if (castBuffer[i].distance < nearestDistance)
                {
                    nearest = i;
                    nearestDistance = castBuffer[i].distance;
                }
            }
            if (nearest >= 0)
            {
                RaycastHit hit = castBuffer[nearest];
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

        private bool IgnoreFlyingCollision(Collider other)
        {
            return other == null || other.isTrigger || other.transform.IsChildOf(transform) ||
                (ownerRoot != null && other.transform.IsChildOf(ownerRoot));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!initialized || resolved || IgnoreFlyingCollision(other)) return;
            if (((1 << other.gameObject.layer) & spec.ImpactMask) == 0) return;
            Resolve(body.position, forward, true);
        }

        public void ApplyInterceptionDamage(float amount)
        {
            if (!initialized || resolved || amount <= 0f || float.IsNaN(amount)) return;
            hitPoints -= amount;
            if (hitPoints <= 0f)
                Resolve(body.position, Vector3.up, spec.ExplodeWhenIntercepted);
        }

        private void Resolve(Vector3 point, Vector3 normal, bool dealDamage)
        {
            if (resolved) return;
            resolved = true;
            if (dealDamage)
            {
                RocketBlastResult result = RocketBlastResolver.Resolve(point, normal, spec,
                    ownerRoot, attacker, blastBuffer, occlusionBuffer);
                if (result.Overflow)
                    Debug.LogWarning("WPN-004: blast overlap overflow; no explosion damage applied.", this);
                try{Detonated?.Invoke(this,point,normal);}catch(System.Exception error){Debug.LogException(error);}
            }
            if (optionalExplosionVfxPrefab != null)
                Instantiate(optionalExplosionVfxPrefab, point, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
