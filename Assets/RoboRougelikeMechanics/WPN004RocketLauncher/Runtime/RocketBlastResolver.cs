using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.Mechanics.Rockets
{
    public readonly struct RocketBlastResult
    {
        public readonly bool Overflow;
        public readonly int DamagedParts;
        public readonly float TotalDamage;
        public RocketBlastResult(bool overflow, int damagedParts, float totalDamage)
        {
            Overflow = overflow; DamagedParts = damagedParts; TotalDamage = totalDamage;
        }
    }

    public static class RocketBlastResolver
    {
        private struct Candidate
        {
            public Collider Collider;
            public Vector3 Point;
            public float Distance;
            public Rigidbody Body;
        }

        public static RocketBlastResult Resolve(Vector3 center, Vector3 surfaceNormal, RocketSpec spec,
            Transform ownerRoot, GameObject attacker, Collider[] overlaps, RaycastHit[] rayHits)
        {
            if (overlaps == null || overlaps.Length == 0 || rayHits == null || rayHits.Length == 0)
                return new RocketBlastResult(true, 0, 0f);
            int count = Physics.OverlapSphereNonAlloc(center, spec.Radius,
                overlaps, spec.ExplosionMask, QueryTriggerInteraction.Ignore);
            if (count >= overlaps.Length)
                return new RocketBlastResult(true, 0, 0f); // fail closed, never partially damage

            var unique = new Dictionary<IRocketDamageReceiver, Candidate>();
            for (int i = 0; i < count; i++)
            {
                Collider c = overlaps[i];
                if (c == null || c.isTrigger || (!spec.DamageOwner && IsOwned(c, ownerRoot))) continue;
                IRocketDamageReceiver receiver = ReceiverFor(c);
                if (receiver == null) continue;
                Vector3 point = c.ClosestPoint(center);
                float distance = Vector3.Distance(center, point);
                if (unique.TryGetValue(receiver, out Candidate previous) && previous.Distance <= distance)
                    continue;
                unique[receiver] = new Candidate
                {
                    Collider = c, Point = point, Distance = distance, Body = c.attachedRigidbody
                };
            }
            // Validate every line of sight before applying any damage; overflow blocks safely.
            var visible = new List<KeyValuePair<IRocketDamageReceiver, Candidate>>();
            foreach (var pair in unique)
            {
                Candidate candidate = pair.Value;
                if (candidate.Distance > spec.Radius) continue;
                bool overflow;
                bool hasSight = CanSee(center, candidate, pair.Key, spec, rayHits, out overflow);
                if (overflow) return new RocketBlastResult(true, 0, 0f);
                if (hasSight) visible.Add(pair);
            }

            // Stable ordering matters when many R6 blocks share a single Rigidbody impulse budget.
            visible.Sort((a, b) =>
            {
                int d = a.Value.Distance.CompareTo(b.Value.Distance);
                if (d != 0) return d;
                var ac = a.Key as Component;
                var bc = b.Key as Component;
                return (ac != null ? ac.GetInstanceID() : 0).CompareTo(
                    bc != null ? bc.GetInstanceID() : 0);
            });
            var impulseSpent = new Dictionary<Rigidbody, float>();
            float total = 0f;
            int damaged = 0;
            foreach (var pair in visible)
            {
                Candidate c = pair.Value;
                float damage = RocketMath.BlastDamage(c.Distance, spec.Radius,
                    spec.CenterDamage, spec.EdgeDamageFraction);
                if (damage <= 0f) continue;
                Vector3 direction = c.Point - center;
                if (direction.sqrMagnitude < 0.000001f)
                    direction = surfaceNormal.sqrMagnitude > 0.000001f ? surfaceNormal : Vector3.up;
                direction.Normalize();
                float fraction = 1f - Mathf.Clamp01(c.Distance / spec.Radius);
                float wanted = spec.MaxImpulsePerBody * fraction;
                float approved = wanted;
                if (c.Body != null)
                {
                    impulseSpent.TryGetValue(c.Body, out float used);
                    approved = Mathf.Max(0f, Mathf.Min(wanted, spec.MaxImpulsePerBody - used));
                    impulseSpent[c.Body] = used + approved;
                }
                pair.Key.ReceiveRocketDamage(damage, c.Point, direction * approved, attacker);
                damaged++;
                total += damage;
            }
            return new RocketBlastResult(false, damaged, total);
        }

        private static bool CanSee(Vector3 center, Candidate c, IRocketDamageReceiver receiver,
            RocketSpec spec, RaycastHit[] hits, out bool overflow)
        {
            overflow = false;
            if (c.Distance < 0.0001f) return true;
            Vector3 direction = (c.Point - center) / c.Distance;
            int count = Physics.RaycastNonAlloc(center, direction, hits,
                c.Distance + 0.035f, spec.OcclusionMask, QueryTriggerInteraction.Ignore);
            if (count >= hits.Length)
            {
                overflow = true;
                return false;
            }
            float closest = float.PositiveInfinity;
            Collider first = null;
            for (int i = 0; i < count; i++)
            {
                Collider hit = hits[i].collider;
                if (hit == null || hit.isTrigger) continue;
                if (hits[i].distance < closest)
                {
                    closest = hits[i].distance;
                    first = hit;
                }
            }
            return first == null || ReferenceEquals(ReceiverFor(first), receiver);
        }

        public static bool IsOwned(Collider collider, Transform ownerRoot) =>
            collider != null && ownerRoot != null && collider.transform.IsChildOf(ownerRoot);

        public static IRocketDamageReceiver ReceiverFor(Collider collider)
        {
            if (collider == null) return null;
            Transform node = collider.transform;
            // Walk just this collider's ancestry; each block can have an independent receiver.
            while (node != null)
            {
                var behaviours = node.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (behaviour is IRocketDamageReceiver receiver && behaviour != null && behaviour.isActiveAndEnabled)
                        return receiver;
                }
                node = node.parent;
            }
            return null;
        }
    }
}
