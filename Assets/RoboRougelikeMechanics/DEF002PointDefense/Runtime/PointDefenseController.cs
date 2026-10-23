using UnityEngine;

namespace RoboRougelike.Mechanics.PointDefense
{
    /// <summary>
    /// Self-contained PDS turret. Threat proxies adapt R6 missiles; not a separate HP system.
    /// All aiming and firing occurs on the physics clock. No global singleton.
    /// </summary>
    public sealed class PointDefenseController : MonoBehaviour
    {
        [SerializeField] private PointDefenseConfiguration settings = new PointDefenseConfiguration();
        [SerializeField] private PointDefenseInterceptor interceptorPrefab;
        [SerializeField] private Transform yawAndPitchPivot;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Rigidbody vehicleBody;
        [SerializeField] private Transform vehicleRoot;
        [SerializeField] private int factionId;
        [SerializeField] private bool operational = true;
        [SerializeField] private MonoBehaviour energySourceBehaviour;

        private readonly Collider[] nearby = new Collider[256];
        private readonly RaycastHit[] sightHits = new RaycastHit[96];
        private readonly Collider[] muzzleOverlaps = new Collider[48];
        private readonly System.Collections.Generic.HashSet<int> visited = new System.Collections.Generic.HashSet<int>();
        private IPointDefenseEnergySource energy;
        private PointDefenseThreatProxy target;
        private float scanRemaining;
        private float cooldownRemaining;
        private int ammunition;

        public PointDefenseThreatProxy CurrentTarget { get { return target; } }
        public float CooldownRemaining { get { return cooldownRemaining; } }
        public int Ammunition { get { return ammunition; } }
        public bool IsOperational { get { return operational && isActiveAndEnabled && IsAttached(); } }

        private void Awake()
        {
            if (settings == null) settings = new PointDefenseConfiguration();
            settings.Sanitize();
            ammunition = settings.startingAmmunition;
            energy = energySourceBehaviour as IPointDefenseEnergySource;
        }
        public void Configure(PointDefenseConfiguration newSettings, PointDefenseInterceptor prefab)
        {
            settings = newSettings != null ? newSettings.Copy() : new PointDefenseConfiguration();
            settings.Sanitize();
            interceptorPrefab = prefab;
            ammunition = settings.startingAmmunition;
            scanRemaining = 0f;
            cooldownRemaining = 0f;
        }
        public void Bind(Rigidbody body, Transform root, Transform turretPivot,
            Transform firingMuzzle, int ownerFaction)
        {
            vehicleBody = body;
            vehicleRoot = root;
            yawAndPitchPivot = turretPivot;
            muzzle = firingMuzzle;
            factionId = ownerFaction;
            scanRemaining = 0f;
        }
        public void SetEnergySource(MonoBehaviour source)
        {
            energySourceBehaviour = source;
            energy = source as IPointDefenseEnergySource;
        }
        public void SetOperational(bool value)
        {
            operational = value;
            if (!value) target = null;
        }
        public void AddAmmunition(int amount)
        {
            if (amount > 0) ammunition = Mathf.Min(int.MaxValue - amount, ammunition) + amount;
        }
        private bool automaticSimulation=true;
        public void SetAutomaticSimulation(bool value)=>automaticSimulation=value;
        public void RestoreState(int ammo,float cooldown)
        {ammunition=Mathf.Max(0,ammo);cooldownRemaining=float.IsNaN(cooldown)||float.IsInfinity(cooldown)?0:Mathf.Max(0,cooldown);}
        private bool IsAttached()
        {
            return vehicleRoot == null || transform == vehicleRoot || transform.IsChildOf(vehicleRoot);
        }
        private void OnDisable() { target = null; }

        private void FixedUpdate(){if(automaticSimulation)Advance(Time.fixedDeltaTime);}
        public void Advance(float dt)
        {
            if(dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - dt);
            if (!IsOperational || muzzle == null || yawAndPitchPivot == null || vehicleBody == null)
            { target = null; return; }
            scanRemaining -= dt;
            if (scanRemaining <= 0f || !ValidTarget(target))
            {
                scanRemaining = settings.scanInterval;
                target = FindThreat();
            }
            if (!ValidTarget(target)) { target = null; return; }

            var threat = target.Threat;
            Vector3 origin = muzzle.position;
            Vector3 inheritedVelocity = vehicleBody.GetPointVelocity(origin);
            Vector3 aim;
            float interceptTime;
            bool lead = PointDefenseMath.TryLead(origin, inheritedVelocity, threat.AimTransform.position,
                threat.Velocity, settings.interceptorSpeed, settings.interceptorLifetime, out aim, out interceptTime);
            if (!lead) return;
            Vector3 direction = aim - origin;
            if (direction.sqrMagnitude < 0.0001f) return;
            Quaternion desiredRotation = Quaternion.LookRotation(direction.normalized, vehicleRoot != null ? vehicleRoot.up : Vector3.up);
            yawAndPitchPivot.rotation = Quaternion.RotateTowards(yawAndPitchPivot.rotation,
                desiredRotation, settings.traverseDegreesPerSecond * dt);
            if (cooldownRemaining > 0f || interceptorPrefab == null ||
                !PointDefenseMath.IsAligned(muzzle.forward, direction, settings.firingToleranceDegrees)) return;
            if (!ClearLineOfSight(origin, threat.AimTransform.position, target)) return;
            TryFire();
        }

        public void ScanNow() { target = IsOperational ? FindThreat() : null; }

        private bool ValidTarget(PointDefenseThreatProxy proxy)
        {
            if (proxy == null || proxy.Threat == null || !proxy.Threat.IsActiveThreat ||
                proxy.Threat.AimTransform == null || proxy.Threat.FactionId == factionId) return false;
            Vector3 origin = vehicleBody != null ? vehicleBody.worldCenterOfMass : transform.position;
            return (proxy.Threat.AimTransform.position - origin).sqrMagnitude <=
                settings.detectionRadius * settings.detectionRadius;
        }

        private PointDefenseThreatProxy FindThreat()
        {
            if (vehicleBody == null) return null;
            Vector3 center = vehicleBody.worldCenterOfMass;
            int count = Physics.OverlapSphereNonAlloc(center, settings.detectionRadius, nearby,
                settings.threatLayerMask, QueryTriggerInteraction.Collide);
            if (count >= nearby.Length) return null; // overflow => no shot, not incomplete targeting
            visited.Clear();
            PointDefenseThreatProxy best = null;
            float bestT = float.PositiveInfinity;
            float bestDistance = float.PositiveInfinity;
            int bestId = int.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider hit = nearby[i];
                if (hit == null || (vehicleRoot != null && hit.transform.IsChildOf(vehicleRoot))) continue;
                PointDefenseThreatProxy proxy = hit.GetComponentInParent<PointDefenseThreatProxy>();
                if (proxy == null || !visited.Add(proxy.GetInstanceID()) || !ValidTarget(proxy)) continue;
                IPointDefenseThreat threat = proxy.Threat;
                float t;
                if (!PointDefenseMath.IsThreatApproaching(threat.AimTransform.position, threat.Velocity,
                    center, vehicleBody.linearVelocity, settings.threatLookAhead, settings.defendedRadius, out t)) continue;
                if (!ClearLineOfSight(muzzle != null ? muzzle.position : transform.position,
                    threat.AimTransform.position, proxy)) continue;
                float dist = (threat.AimTransform.position - center).sqrMagnitude;
                int id = proxy.GetInstanceID();
                if (t < bestT - 0.0001f ||
                    (Mathf.Abs(t - bestT) <= 0.0001f && (dist < bestDistance - 0.0001f ||
                    (Mathf.Abs(dist - bestDistance) <= 0.0001f && id < bestId))))
                { best = proxy; bestT = t; bestDistance = dist; bestId = id; }
            }
            return best;
        }

        private bool ClearLineOfSight(Vector3 origin, Vector3 targetPoint, PointDefenseThreatProxy threat)
        {
            Vector3 displacement = targetPoint - origin;
            float distance = displacement.magnitude;
            if (distance <= 0.00001f) return false;
            // Raycast may miss colliders enveloping the muzzle. Fail closed.
            int overlaps = Physics.OverlapSphereNonAlloc(origin, 0.025f, muzzleOverlaps,
                settings.blockerLayerMask, QueryTriggerInteraction.Ignore);
            if (overlaps >= muzzleOverlaps.Length) return false;
            for (int i = 0; i < overlaps; i++)
            {
                Collider col = muzzleOverlaps[i];
                if (col == null) continue;
                if (vehicleRoot != null && col.transform.IsChildOf(vehicleRoot)) continue;
                if (col.transform.IsChildOf(threat.transform)) continue;
                return false;
            }
            int count = Physics.RaycastNonAlloc(origin, displacement / distance, sightHits,
                distance, settings.blockerLayerMask, QueryTriggerInteraction.Ignore);
            if (count >= sightHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Collider col = sightHits[i].collider;
                if (col == null) continue;
                if (vehicleRoot != null && col.transform.IsChildOf(vehicleRoot)) continue;
                if (col.transform.IsChildOf(threat.transform)) continue;
                if (sightHits[i].distance + 0.005f < distance) return false;
            }
            return true;
        }

        private bool TryFire()
        {
            if (!IsOperational || target == null || target.Threat == null ||
                !target.Threat.IsActiveThreat || interceptorPrefab == null || muzzle == null) return false;
            if (settings.useAmmunition && ammunition <= 0) return false;
            if (settings.energyPerShot > 0f && (energy == null || !energy.TrySpend(settings.energyPerShot))) return false;
            // Only instantiate a prevalidated prefab; no ammo consumption before success.
            PointDefenseInterceptor projectile = Instantiate(interceptorPrefab, muzzle.position, muzzle.rotation);
            if (projectile == null) return false;
            Vector3 inherited = vehicleBody != null ? vehicleBody.GetPointVelocity(muzzle.position) : Vector3.zero;
            projectile.Initialize(target, vehicleRoot, inherited + muzzle.forward * settings.interceptorSpeed,
                settings.interceptorDamage, settings.interceptorRadius, settings.interceptorLifetime);
            if (!projectile.gameObject.activeSelf) projectile.gameObject.SetActive(true);
            if (settings.useAmmunition) ammunition--;
            cooldownRemaining = settings.shotInterval;
            if (vehicleBody != null && settings.recoilImpulse > 0f)
                vehicleBody.AddForceAtPosition(-muzzle.forward * settings.recoilImpulse,
                    muzzle.position, ForceMode.Impulse);
            return true;
        }
    }
}
