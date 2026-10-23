using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.Weapons.RotarySaw
{
    [DisallowMultipleComponent]
    public sealed class RotarySawModule : MonoBehaviour
    {
        [SerializeField] private RotarySawConfiguration configuration = new RotarySawConfiguration();
        [SerializeField] private Transform bladeCenter;
        [SerializeField] private Rigidbody ownerRigidbody;
        [SerializeField] private Transform ownerRoot;
        [SerializeField] private MonoBehaviour damageSinkBehaviour;
        [SerializeField] private MonoBehaviour energySourceBehaviour;
        [SerializeField] private bool motorRequested;
        [SerializeField] private bool operational = true;

        private RotarySawMotor motor;
        private Collider[] overlapBuffer;
        private IRotarySawDamageSink sink;
        private IRotarySawEnergySource energy;
        private readonly HashSet<int> ownerColliderIds = new HashSet<int>();
        private readonly HashSet<int> seenPartIds = new HashSet<int>();
        private readonly List<SawContact> selected = new List<SawContact>(32);
        private bool warnedMissingSink;

        private struct SawContact
        {
            public SawTarget target;
            public Collider collider;
            public Vector3 hitPoint;
        }

        public RotarySawMotor Motor { get { EnsureInitialized(); return motor; } }
        public bool IsOperational { get { return operational; } }
        public bool IsMotorRequested { get { return motorRequested; } }
        public bool OverflowedLastTick { get; private set; }
        public int DamageEventsLastTick { get; private set; }

        private void Awake() { EnsureInitialized(); ResolveInterfaces(); }
        private void OnValidate() { if (configuration != null) configuration.ClampToSafeValues(); }
        private void OnDisable() { motorRequested = false; if (motor != null) motor.StopImmediately(); }

        public void Bind(Rigidbody ownerBody, Transform vehicleRoot, Transform actualBladeCenter)
        {
            ownerRigidbody = ownerBody;
            ownerRoot = vehicleRoot;
            bladeCenter = actualBladeCenter;
        }

        public void SetDamageSink(MonoBehaviour behaviour)
        {
            damageSinkBehaviour = behaviour;
            sink = behaviour as IRotarySawDamageSink;
            warnedMissingSink = false;
        }

        public void SetEnergySource(MonoBehaviour behaviour)
        {
            energySourceBehaviour = behaviour;
            energy = behaviour as IRotarySawEnergySource;
        }

        public void SetMotorRequested(bool requested) { motorRequested = requested; }

        // MUST be called when the structural graph detaches/destroys the module.
        public void SetOperational(bool value)
        {
            operational = value;
            if (!value) { motorRequested = false; if (motor != null) motor.StopImmediately(); }
        }

        public void RegisterOwnerCollider(Collider collider)
        {
            if (collider != null) ownerColliderIds.Add(collider.GetInstanceID());
        }
        public void UnregisterOwnerCollider(Collider collider)
        {
            if (collider != null) ownerColliderIds.Remove(collider.GetInstanceID());
        }
        public void ClearRegisteredOwnerColliders() { ownerColliderIds.Clear(); }

        // For isolated tests, editor tuning and game adapters. Does not mutate external R6 systems.
        public void Configure(RotarySawConfiguration newConfiguration)
        {
            configuration = (newConfiguration ?? new RotarySawConfiguration()).Copy();
            configuration.ClampToSafeValues();
            motor = new RotarySawMotor(configuration);
            overlapBuffer = new Collider[configuration.maxOverlapHits];
        }

        private void EnsureInitialized()
        {
            if (motor != null) return;
            if (configuration == null) configuration = new RotarySawConfiguration();
            configuration.ClampToSafeValues();
            motor = new RotarySawMotor(configuration);
            overlapBuffer = new Collider[configuration.maxOverlapHits];
            if (bladeCenter == null) bladeCenter = transform;
            if (ownerRigidbody == null) ownerRigidbody = GetComponentInParent<Rigidbody>();
            if (ownerRoot == null && ownerRigidbody != null) ownerRoot = ownerRigidbody.transform;
        }

        private void ResolveInterfaces()
        {
            sink = damageSinkBehaviour as IRotarySawDamageSink;
            energy = energySourceBehaviour as IRotarySawEnergySource;
        }

        private bool IsOwnCollider(Collider collider)
        {
            if (collider == null) return true;
            if (ownerColliderIds.Contains(collider.GetInstanceID())) return true;
            if (ownerRoot != null && collider.transform.IsChildOf(ownerRoot)) return true;
            if (ownerRigidbody != null && collider.attachedRigidbody == ownerRigidbody) return true;
            return false;
        }

        private Vector3 CurrentAxle(Transform origin)
        {
            switch (configuration.axleAxis)
            {
                case SawAxleAxis.Up: return origin.up;
                case SawAxleAxis.Forward: return origin.forward;
                default: return origin.right;
            }
        }

        private bool automaticSimulation=true;
        public void SetAutomaticSimulation(bool value)=>automaticSimulation=value;
        private void FixedUpdate(){if(automaticSimulation)Advance(Time.fixedDeltaTime);}
        public void Advance(float dt)
        {
            if(dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            EnsureInitialized();
            OverflowedLastTick = false;
            DamageEventsLastTick = 0;
            bool ready = operational && enabled && isActiveAndEnabled && ownerRigidbody != null &&
                         bladeCenter != null && sink != null && damageSinkBehaviour != null && damageSinkBehaviour.isActiveAndEnabled;
            if (!ready && sink == null && !warnedMissingSink && motorRequested)
            {
                Debug.LogWarning("WPN-005: no IRotarySawDamageSink; refusing damage. Integrate R6 adapter.", this);
                warnedMissingSink = true;
            }
            bool energyAvailable = true;
            if (ready && motorRequested && motor.JamRemaining <= 0f && !motor.Overheated &&
                configuration.energyPerSecond > 0f)
            {
                // Missing/deactivated provider => fail closed (no free energy).
                energyAvailable = energySourceBehaviour != null && energySourceBehaviour.isActiveAndEnabled &&
                                  energy != null && energy.TryConsume(configuration.energyPerSecond * dt);
            }
            motor.Tick(motorRequested, energyAvailable, ready, dt);
            if (!motor.CanCut) return;

            Vector3 center = bladeCenter.position;
            Vector3 axle = CurrentAxle(bladeCenter);
            // Conservative broadphase: if buffer fills, do no damage this physics step.
            int count = Physics.OverlapSphereNonAlloc(center,
                Mathf.Sqrt(configuration.bladeRadius * configuration.bladeRadius +
                           configuration.bladeHalfThickness * configuration.bladeHalfThickness),
                overlapBuffer, configuration.contactLayers, QueryTriggerInteraction.Ignore);
            if (count >= overlapBuffer.Length)
            {
                OverflowedLastTick = true;
                return;
            }

            selected.Clear();
            seenPartIds.Clear();
            for (int i = 0; i < count; i++)
            {
                Collider collider = overlapBuffer[i];
                if (IsOwnCollider(collider)) continue;
                if (!sink.TryResolve(collider, out SawTarget target)) continue;
                if (target.partId == 0 || seenPartIds.Contains(target.partId)) continue;
                if (float.IsNaN(target.materialResistance) || float.IsInfinity(target.materialResistance) || target.materialResistance <= 0f)
                    continue;
                Vector3 hitPoint = collider.ClosestPoint(center);
                if (!RotarySawContactMath.IsOnCuttingDisc(hitPoint - center, axle,
                    configuration.bladeRadius, configuration.bladeHalfThickness, configuration.hubDeadZone)) continue;
                seenPartIds.Add(target.partId);
                selected.Add(new SawContact { target = target, collider = collider, hitPoint = hitPoint });
            }

            // Fail the ENTIRE contact batch rather than damaging easy targets before a hard jam.
            for (int i = 0; i < selected.Count; i++)
            {
                if (selected[i].target.materialResistance > configuration.maxCuttableResistance)
                {
                    motor.Jam();
                    return;
                }
            }

            float remainingReaction = configuration.maximumTotalReactionForceNewtons;
            for (int i = 0; i < selected.Count; i++)
            {
                SawContact c = selected[i];
                float damage = RotarySawContactMath.Damage(configuration.damagePerSecondAtFullRpm,
                    motor.NormalizedRpm, dt, c.target.materialResistance);
                if (damage <= 0f) continue;
                sink.ApplyDamage(new SawDamageEvent(c.target.partId, damage, c.hitPoint, axle, c.collider, this));
                DamageEventsLastTick++;
                if (ownerRigidbody != null && !ownerRigidbody.isKinematic && remainingReaction > 0f)
                {
                    float force = Mathf.Min(remainingReaction,
                        configuration.reactionForceNewtonsPerContact * motor.NormalizedRpm);
                    if (force > 0f)
                    {
                        Vector3 radial = c.hitPoint - center;
                        if (radial.sqrMagnitude > 0.00001f)
                            ownerRigidbody.AddForceAtPosition(-radial.normalized * force, c.hitPoint, ForceMode.Force);
                        remainingReaction -= force;
                    }
                }
            }
        }
    }
}
