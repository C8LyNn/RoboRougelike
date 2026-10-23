using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.WPN002Railgun.Demo
{
    // Visible physical-travel projectile with swept sphere collisions. Sample only.
    // Production: replace with an adapter to R6's OWN physical projectile + damage pipeline.
    public sealed class RailgunDemoProjectile : MonoBehaviour
    {
        private const int MaxSweepHits = 128;
        private readonly RaycastHit[] hits = new RaycastHit[MaxSweepHits];
        private readonly HashSet<int> visitedParts = new HashSet<int>();
        private RailgunShot shot;
        private Vector3 velocity;
        private float distance, energy, lifetime;
        private int penetrated;
        private bool initialized;
        public float RemainingEnergy => energy;
        public float DistanceTravelled => distance;
        public int DistinctPartsHit => penetrated;
        public bool IsTerminated { get; private set; }

        public void Initialize(RailgunShot info)
        {
            shot = info;
            velocity = info.InitialVelocity;
            energy = info.Spec.InitialEnergy;
            distance = 0f; lifetime = 0f; penetrated = 0;
            visitedParts.Clear(); initialized = true; IsTerminated = false;
            transform.position = info.Origin;
            if (velocity.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(velocity.normalized);
        }
        private void FixedUpdate() { if (initialized && !IsTerminated) SimulateStep(Time.fixedDeltaTime); }
        public void SimulateStep(float dt)
        {
            if (!initialized || IsTerminated || !RailgunRules.Finite(dt) || dt <= 0f) return;
            lifetime += dt;
            if (lifetime > 30f || velocity.sqrMagnitude < 0.01f) { Terminate(); return; }
            float move = Mathf.Min(velocity.magnitude * dt, shot.Spec.Range - distance);
            if (move <= 0f) { Terminate(); return; }
            Vector3 direction = velocity.normalized;
            int count = Physics.SphereCastNonAlloc(transform.position, shot.Spec.Radius,
                direction, hits, move, ~0, QueryTriggerInteraction.Ignore);
            if (count >= hits.Length)
            {
                // Fail closed on saturated physics buffer, do not silently skip the closest blocking surface.
                Debug.LogWarning("Railgun demo sweep overflow: projectile stopped.", this);
                Terminate(); return;
            }
            // Sort only the used segment: nearest-to-farthest in the current physics step.
            for (int i = 1; i < count; i++)
            {
                RaycastHit key = hits[i]; int j = i - 1;
                while (j >= 0 && hits[j].distance > key.distance) { hits[j + 1] = hits[j]; j--; }
                hits[j + 1] = key;
            }
            float previous = 0f;
            for (int i = 0; i < count; i++)
            {
                RaycastHit h = hits[i];
                Collider c = h.collider;
                if (c == null || IsOwnerCollider(c)) continue;
                RailgunDemoBlock target = c.GetComponentInParent<RailgunDemoBlock>();
                if (target != null && visitedParts.Contains(target.GetInstanceID())) continue;
                // A non-damageable collider (walls/ground/other) blocks the shot.
                energy = RailgunRules.AfterTravel(energy, h.distance - previous, shot.Spec.EnergyLossPerMeter);
                previous = h.distance;
                if (target == null)
                {
                    transform.position = h.point;
                    Terminate(); return;
                }
                visitedParts.Add(target.GetInstanceID());
                RailgunImpact impact = RailgunRules.Evaluate(energy, target.Resistance,
                    shot.Spec.DamagePerEnergy, shot.Spec.MaxDamagePerPart,
                    penetrated, shot.Spec.MaxDistinctParts, shot.Spec.MinEnergyToContinue,
                    target.Penetrable);
                target.Receive(impact.Damage);
                penetrated++;
                energy = impact.RemainingEnergy;
                if (!impact.Continues)
                {
                    transform.position = h.point;
                    Terminate(); return;
                }
            }
            energy = RailgunRules.AfterTravel(energy, move - previous, shot.Spec.EnergyLossPerMeter);
            distance += move;
            transform.position += direction * move;
            if (energy <= shot.Spec.MinEnergyToContinue || distance >= shot.Spec.Range - 0.0001f)
                Terminate();
        }
        private bool IsOwnerCollider(Collider c)
        {
            if (shot.OwnerRoot != null && c.transform.IsChildOf(shot.OwnerRoot.transform)) return true;
            if (shot.OwnerBody != null && c.attachedRigidbody == shot.OwnerBody) return true;
            return false;
        }
        private void Terminate() { IsTerminated = true; enabled = false; }
    }
}
