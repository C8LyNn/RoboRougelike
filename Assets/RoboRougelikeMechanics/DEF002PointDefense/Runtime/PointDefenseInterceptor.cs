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
        private const int MaxReusableHits=4096;
        private RaycastHit[] hits = new RaycastHit[96];
        private Collider[] initialOverlaps = new Collider[48];
        private PointDefenseThreatProxy target;
        private Transform ownerRoot;
        private Vector3 velocity;
        private float damage;
        private float radius;
        private float lifetime;
        private bool initialized;

        // A physical salvo can explicitly ignore sibling collision pairs, or
        // use mutual collider/body layer exclusions. This does not identify
        // unrelated projectiles or obstacles as siblings.
        internal static bool MutuallyExcluded(Collider first,Collider second)
        {
            if(first==null||second==null)return false;
            if(Physics.GetIgnoreCollision(first,second))return true;
            int firstMask=first.excludeLayers.value,secondMask=second.excludeLayers.value;
            var firstBody=first.attachedRigidbody;var secondBody=second.attachedRigidbody;
            if(firstBody!=null)firstMask|=firstBody.excludeLayers.value;
            if(secondBody!=null)secondMask|=secondBody.excludeLayers.value;
            return (firstMask&(1<<second.gameObject.layer))!=0&&(secondMask&(1<<first.gameObject.layer))!=0;
        }

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
            int overlapCount;Collider[] overlapHits;
            while(true)
            {
                overlapCount=Physics.OverlapSphereNonAlloc(transform.position,radius,initialOverlaps,collisionMask,QueryTriggerInteraction.Collide);
                if(overlapCount<initialOverlaps.Length){overlapHits=initialOverlaps;break;}
                if(initialOverlaps.Length>=MaxReusableHits)
                {overlapHits=Physics.OverlapSphere(transform.position,radius,collisionMask,QueryTriggerInteraction.Collide);overlapCount=overlapHits.Length;break;}
                System.Array.Resize(ref initialOverlaps,Mathf.Min(initialOverlaps.Length*2,MaxReusableHits));
            }
            Collider lockedOverlap=null;
            for(int i=0;i<overlapCount;i++)
            {
                var col=overlapHits[i];
                if(col!=null&&col.GetComponentInParent<PointDefenseThreatProxy>()==target){lockedOverlap=col;break;}
            }
            for (int i = 0; i < overlapCount; i++)
            {
                Collider col = overlapHits[i];
                if (col == null || col.transform.IsChildOf(transform)) continue;
                if (ownerRoot != null && col.transform.IsChildOf(ownerRoot)) continue;
                var proxy=col.GetComponentInParent<PointDefenseThreatProxy>();
                if (proxy == target) continue;
                if (col.isTrigger) continue; // ignore unrelated trigger volumes
                if(lockedOverlap!=null&&proxy!=null&&MutuallyExcluded(lockedOverlap,col))continue;
                Destroy(gameObject); return; // solid foreign obstacle: never damage through walls
            }
            if (lockedOverlap!=null)
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
            int count;RaycastHit[] impactHits;
            while(true)
            {
                count=Physics.SphereCastNonAlloc(transform.position,radius,displacement/distance,hits,distance,collisionMask,QueryTriggerInteraction.Collide);
                if(count<hits.Length){impactHits=hits;break;}
                if(hits.Length>=MaxReusableHits)
                {impactHits=Physics.SphereCastAll(transform.position,radius,displacement/distance,distance,collisionMask,QueryTriggerInteraction.Collide);count=impactHits.Length;break;}
                System.Array.Resize(ref hits,Mathf.Min(hits.Length*2,MaxReusableHits));
            }
            float nearest = float.PositiveInfinity;
            Collider impact = null;
            for (int i = 0; i < count; i++)
            {
                Collider col = impactHits[i].collider;
                if (col == null || col.transform.IsChildOf(transform)) continue;
                if (ownerRoot != null && col.transform.IsChildOf(ownerRoot)) continue;
                if (col.isTrigger && col.GetComponentInParent<PointDefenseThreatProxy>() != target)
                    continue; // capture trigger-based rockets, not arbitrary trigger areas
                float hitDistance=impactHits[i].distance;
                if (hitDistance < nearest-.00001f)
                {
                    nearest = hitDistance;
                    impact = col;
                }
                else if(impact!=null&&Mathf.Abs(hitDistance-nearest)<=.00001f)
                {
                    var currentProxy=impact.GetComponentInParent<PointDefenseThreatProxy>();
                    var nextProxy=col.GetComponentInParent<PointDefenseThreatProxy>();
                    // Only a sibling ignored by the target can be tied with
                    // it; a wall or different projectile retains precedence.
                    if(nextProxy==target&&currentProxy!=null&&currentProxy!=target&&MutuallyExcluded(impact,col))impact=col;
                    else if(currentProxy==target&&nextProxy!=target&&
                            (nextProxy==null||!MutuallyExcluded(impact,col)))impact=col;
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
