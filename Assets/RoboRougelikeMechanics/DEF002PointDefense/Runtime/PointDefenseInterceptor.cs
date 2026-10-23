using UnityEngine;

namespace RoboRougelike.Mechanics.PointDefense
{
    /// <summary>
    /// Swept physical counter-projectile. It damages only the locked threat proxy;
    /// unrelated colliders stop it. It does not damage vehicles or blocks.
    /// </summary>
    public sealed class PointDefenseInterceptor : MonoBehaviour
    {
        [SerializeField] private LayerMask collisionMask = ~0;
        private readonly RaycastHit[] hits = new RaycastHit[96];
        private readonly Collider[] initialOverlaps = new Collider[48];
        private PointDefenseThreatProxy target;
        private Transform ownerRoot;
        private Vector3 velocity;
        private float damage;
        private float radius;
        private float lifetime;
        private bool initialized;

        public bool IsInitialized { get { return initialized; } }
        public void Initialize(PointDefenseThreatProxy lockedTarget, Transform shooterOwnerRoot,
            Vector3 worldVelocity, float interceptDamage, float hitRadius, float seconds)
        {
            target = lockedTarget;
            ownerRoot = shooterOwnerRoot;
            velocity = worldVelocity;
            damage = Mathf.Max(0f, interceptDamage);
            radius = Mathf.Max(0.01f, hitRadius);
            lifetime = Mathf.Max(0.02f, seconds);
            initialized = true;
        }
        private void FixedUpdate()
        {
            if (!initialized) return;
            if (target == null || target.Threat == null || !target.Threat.IsActiveThreat)
            { Destroy(gameObject); return; }
            // SphereCast may not report a collider overlapping its origin.
            // Explicitly reject a muzzle or flight step embedded in foreign geometry.
            int overlapCount = Physics.OverlapSphereNonAlloc(transform.position, radius,
                initialOverlaps, collisionMask, QueryTriggerInteraction.Collide);
            if (overlapCount >= initialOverlaps.Length) { Destroy(gameObject); return; }
            bool touchingLockedTarget = false;
            for (int i = 0; i < overlapCount; i++)
            {
                Collider col = initialOverlaps[i];
                if (col == null || col.transform.IsChildOf(transform)) continue;
                if (ownerRoot != null && col.transform.IsChildOf(ownerRoot)) continue;
                if (col.GetComponentInParent<PointDefenseThreatProxy>() == target)
                { touchingLockedTarget = true; continue; }
                if (col.isTrigger) continue; // ignore unrelated trigger volumes
                Destroy(gameObject); return; // solid foreign obstacle: never damage through walls
            }
            if (touchingLockedTarget)
            {
                target.Threat.TryIntercept(damage);
                Destroy(gameObject);
                return;
            }
            float dt = Time.fixedDeltaTime;
            if (lifetime <= 0f) { Destroy(gameObject); return; }
            lifetime -= dt;
            Vector3 displacement = velocity * dt;
            float distance = displacement.magnitude;
            if (distance < 0.00001f) return;
            int count = Physics.SphereCastNonAlloc(transform.position, radius, displacement / distance,
                hits, distance, collisionMask, QueryTriggerInteraction.Collide);
            // A full buffer makes sorting/visibility ambiguous: fail closed.
            if (count >= hits.Length) { Destroy(gameObject); return; }
            float nearest = float.PositiveInfinity;
            Collider impact = null;
            for (int i = 0; i < count; i++)
            {
                Collider col = hits[i].collider;
                if (col == null || col.transform.IsChildOf(transform)) continue;
                if (ownerRoot != null && col.transform.IsChildOf(ownerRoot)) continue;
                if (col.isTrigger && col.GetComponentInParent<PointDefenseThreatProxy>() != target)
                    continue; // capture trigger-based rockets, not arbitrary trigger areas
                if (hits[i].distance < nearest)
                {
                    nearest = hits[i].distance;
                    impact = col;
                }
            }
            if (impact != null)
            {
                var proxy = impact.GetComponentInParent<PointDefenseThreatProxy>();
                if (proxy != null && proxy == target && target.Threat != null && target.Threat.IsActiveThreat)
                    target.Threat.TryIntercept(damage);
                Destroy(gameObject);
                return;
            }
            transform.position += displacement;
        }
    }
}
