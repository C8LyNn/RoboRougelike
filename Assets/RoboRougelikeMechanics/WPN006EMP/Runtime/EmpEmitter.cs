using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.Mechanics.EMP
{
    /// <summary>
    /// Single non-damaging EMP pulse. Scans target colliders once per activation, deduplicates by module,
    /// verifies occlusion, then commits energy/cooldown and finally disrupts receivers.
    /// No dependencies on R6 damage/physics controllers.
    /// </summary>
    public sealed class EmpEmitter : MonoBehaviour
    {
        [Header("Pulse")]
        [SerializeField, Min(0.1f)] private float radius = 9f;
        [SerializeField, Min(0.1f)] private float baseDuration = 3f;
        [SerializeField, Range(0f, 1f)] private float edgeDurationFraction = 0.35f;
        [SerializeField, Min(0f)] private float cooldown = 12f;
        [SerializeField, Min(0f)] private float activationEnergy = 0f;
        [Header("Ownership / teams")]
        [SerializeField] private Transform pulseOrigin;
        [SerializeField] private Transform vehicleRoot;
        [SerializeField] private int vehicleId;
        [SerializeField] private int teamId;
        [SerializeField] private bool affectOwner;
        [SerializeField] private bool affectAllies;
        [Header("Physics")]
        [SerializeField] private LayerMask receiverLayers = ~0;
        [SerializeField] private LayerMask occlusionLayers = ~0;
        [SerializeField, Min(16)] private int maxOverlapColliders = 1024;
        [SerializeField, Min(8)] private int maxRayHits = 128;
        [Header("Energy")]
        [Tooltip("MonoBehaviour implementing IEmpEnergyBank; required when activationEnergy > 0")]
        [SerializeField] private MonoBehaviour energySource;

        private Collider[] overlapBuffer;
        private RaycastHit[] lineOfSightBuffer;
        private readonly Dictionary<EmpReceiver, Collider> moduleColliders = new Dictionary<EmpReceiver, Collider>();
        private readonly List<EmpCandidate> pending = new List<EmpCandidate>();
        private readonly EmpCooldownTimer cooldownTimer = new EmpCooldownTimer();
        private bool operational = true;
        private struct EmpCandidate { public EmpReceiver Receiver; public float Duration; }
        public float CooldownRemaining => cooldownTimer.Remaining;
        public bool Ready => operational && isActiveAndEnabled && cooldownTimer.Ready;
        public event Action<EmpPulseReport> Pulsed;

        public void Configure(float newRadius, float newDuration, float newCooldown, float newEnergy = 0f,
            float newEdgeFraction = 0.35f)
        {
            radius = newRadius;
            baseDuration = newDuration;
            cooldown = newCooldown;
            activationEnergy = newEnergy;
            edgeDurationFraction = newEdgeFraction;
        }

        public void BindVehicle(Transform root, int stableVehicleId = 0, int factionId = 0)
        {
            vehicleRoot = root;
            vehicleId = stableVehicleId;
            teamId = factionId;
        }

        public void SetOrigin(Transform origin) => pulseOrigin = origin;
        public void SetEnergySource(MonoBehaviour bank) => energySource = bank;
        public void SetOperational(bool value) => operational = value;

        public void ConfigurePhysics(LayerMask targetMask, LayerMask obstacleMask, int targetBudget = 1024,
            int rayBudget = 128)
        {
            receiverLayers = targetMask;
            occlusionLayers = obstacleMask;
            maxOverlapColliders = Mathf.Max(16, targetBudget);
            maxRayHits = Mathf.Max(8, rayBudget);
            overlapBuffer = null;
            lineOfSightBuffer = null;
        }

        private Transform OwnRoot => vehicleRoot != null ? vehicleRoot : transform.root;
        private Vector3 Origin => pulseOrigin != null ? pulseOrigin.position : transform.position;

        private bool automaticSimulation=true;
        public void SetAutomaticSimulation(bool value)=>automaticSimulation=value;
        public void Advance(float seconds)=>cooldownTimer.Tick(seconds);
        public void RestoreCooldown(float seconds)=>cooldownTimer.Commit(seconds);
        private void Update(){if(automaticSimulation)Advance(Time.deltaTime);}

        public bool TryPulse(out EmpPulseReport report)
        {
            report = new EmpPulseReport { Radius = radius };
            if (!operational || !isActiveAndEnabled)
                return Fail(ref report, EmpPulseFailure.NotOperational);
            if (!cooldownTimer.Ready)
                return Fail(ref report, EmpPulseFailure.CoolingDown);
            if (!EmpPulseMath.IsValidFinite(radius) || radius <= 0f
                || !EmpPulseMath.IsValidFinite(baseDuration) || baseDuration <= 0f
                || !EmpPulseMath.IsValidFinite(cooldown) || cooldown < 0f
                || !EmpPulseMath.IsValidFinite(activationEnergy) || activationEnergy < 0f
                || !EmpPulseMath.IsValidFinite(edgeDurationFraction))
                return Fail(ref report, EmpPulseFailure.InvalidConfiguration);

            EnsureBuffers();
            moduleColliders.Clear();
            pending.Clear();
            Vector3 source = Origin;
            int count = Physics.OverlapSphereNonAlloc(source, radius, overlapBuffer,
                receiverLayers, QueryTriggerInteraction.Collide);
            // When full, there may be additional targets; refuse pulse before spending energy.
            if (count >= overlapBuffer.Length)
                return Fail(ref report, EmpPulseFailure.ColliderQueryOverflow);

            for (int i = 0; i < count; i++)
            {
                Collider collider = overlapBuffer[i];
                if (collider == null) continue;
                EmpTargetCollider mapping = collider.GetComponent<EmpTargetCollider>();
                if (mapping == null) mapping = collider.GetComponentInParent<EmpTargetCollider>();
                EmpReceiver receiver = mapping != null ? mapping.Receiver : collider.GetComponentInParent<EmpReceiver>();
                if (receiver == null || !receiver.CanBeDisrupted || receiver.Resistance >= 1f
                    || IsFilteredByOwnership(receiver) || moduleColliders.ContainsKey(receiver))
                    continue;
                moduleColliders.Add(receiver, collider);
            }

            foreach (KeyValuePair<EmpReceiver, Collider> pair in moduleColliders)
            {
                EmpReceiver receiver = pair.Key;
                Collider collider = pair.Value;
                Vector3 point = collider.ClosestPoint(source);
                // ClosestPoint returns origin when the source is inside the collider.
                if ((point - source).sqrMagnitude < 0.0001f)
                    point = collider.bounds.center;
                float distance = Vector3.Distance(source, point);
                if (!EmpPulseMath.IsWithinRadius(distance, radius)) continue;
                report.ReceiversDetected++;
                if (!IsVisible(source, point, receiver))
                {
                    report.ReceiversOccluded++;
                    continue;
                }
                float duration = EmpPulseMath.CalculateDuration(baseDuration, distance, radius,
                    receiver.Resistance, edgeDurationFraction);
                if (duration > 0f) pending.Add(new EmpCandidate { Receiver = receiver, Duration = duration });
            }

            if (activationEnergy > 0f)
            {
                IEmpEnergyBank bank = energySource as IEmpEnergyBank;
                if (bank == null) return Fail(ref report, EmpPulseFailure.MissingEnergySource);
                if (!bank.TrySpend(activationEnergy))
                    return Fail(ref report, EmpPulseFailure.InsufficientEnergy);
            }

            cooldownTimer.Commit(cooldown);
            foreach (EmpCandidate item in pending)
            {
                // Receiver may have been destroyed in synchronous energy-bank callback.
                if (item.Receiver == null || !item.Receiver.CanBeDisrupted) continue;
                item.Receiver.ApplyEmp(item.Duration);
                report.ReceiversAffected++;
            }
            Pulsed?.Invoke(report);
            return true;
        }

        private bool IsFilteredByOwnership(EmpReceiver receiver)
        {
            bool sameVehicle = (vehicleId != 0 && receiver.VehicleId != 0 && vehicleId == receiver.VehicleId)
                || (OwnRoot != null && OwnRoot == receiver.VehicleRoot);
            if (!affectOwner && sameVehicle) return true;
            if (!affectAllies && !sameVehicle && teamId != 0 && receiver.TeamId != 0
                && teamId == receiver.TeamId) return true;
            return false;
        }

        private bool IsVisible(Vector3 origin, Vector3 point, EmpReceiver target)
        {
            Vector3 travel = point - origin;
            float distance = travel.magnitude;
            if (distance < 0.001f) return true;
            int hits = Physics.RaycastNonAlloc(origin, travel / distance, lineOfSightBuffer,
                distance, occlusionLayers, QueryTriggerInteraction.Ignore);
            // Conservative: unknown hits may hide a wall.
            if (hits >= lineOfSightBuffer.Length) return false;
            for (int i = 0; i < hits; i++)
            {
                Collider obstacle = lineOfSightBuffer[i].collider;
                if (obstacle == null) continue;
                // The emitting vehicle and the target's own colliders must not shadow their EMP.
                Transform occluder = obstacle.transform;
                if (OwnRoot != null && occluder.IsChildOf(OwnRoot)) continue;
                EmpTargetCollider mapped = obstacle.GetComponent<EmpTargetCollider>();
                if (mapped == null) mapped = obstacle.GetComponentInParent<EmpTargetCollider>();
                EmpReceiver hitReceiver = mapped != null ? mapped.Receiver : obstacle.GetComponentInParent<EmpReceiver>();
                if (hitReceiver == target) continue;
                if (occluder.IsChildOf(target.transform)) continue;
                return false;
            }
            return true;
        }

        private void EnsureBuffers()
        {
            int targetCapacity = Mathf.Clamp(maxOverlapColliders, 16, 8192);
            int rayCapacity = Mathf.Clamp(maxRayHits, 8, 512);
            if (overlapBuffer == null || overlapBuffer.Length != targetCapacity)
                overlapBuffer = new Collider[targetCapacity];
            if (lineOfSightBuffer == null || lineOfSightBuffer.Length != rayCapacity)
                lineOfSightBuffer = new RaycastHit[rayCapacity];
        }

        private static bool Fail(ref EmpPulseReport report, EmpPulseFailure reason)
        {
            report.Failure = reason;
            report.ReceiversAffected = 0;
            return false;
        }
    }
}
