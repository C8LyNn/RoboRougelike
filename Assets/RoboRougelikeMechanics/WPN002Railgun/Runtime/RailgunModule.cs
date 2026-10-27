using System;
using UnityEngine;

namespace RoboRougelike.WPN002Railgun
{
    public enum RailgunFireFailure
    {
        None, Inactive, ClockInvalid, MissingMuzzle, MissingSink, MissingOwner,
        Detached, Cooldown, ResourceUnavailable, EmissionRejected
    }

    [DisallowMultipleComponent]
    public sealed class RailgunModule : MonoBehaviour
    {
        [Header("Binding to the R6 vehicle")]
        [SerializeField] private Transform muzzle;
        [SerializeField] private Rigidbody ownerBody;
        [SerializeField] private GameObject ownerRoot;
        [Tooltip("An enabled MonoBehaviour implementing IRailgunShotSink (required).")]
        [SerializeField] private MonoBehaviour shotSinkComponent;
        [Tooltip("Optional resource adapter implementing IRailgunShotResourceBank.")]
        [SerializeField] private MonoBehaviour resourceComponent;
        [SerializeField] private bool requireHierarchyAttachment = true;
        [SerializeField] private RailgunConfig configuration = new RailgunConfig();
        private readonly RailgunFireGate gate = new RailgunFireGate();
        public RailgunFireFailure LastFailure { get; private set; }
        public RailgunSpec CurrentSpec => (configuration ?? new RailgunConfig()).Snapshot();
        public uint SuccessfulShots => gate.SuccessfulShots;
        public float NextReadyTime => gate.NextReadyTime;
        public bool Operational => gate.Operational;

        private void Awake()
        {
            if (ownerBody == null) ownerBody = GetComponentInParent<Rigidbody>();
            if (ownerRoot == null && ownerBody != null) ownerRoot = ownerBody.transform.root.gameObject;
        }
        public void Bind(Rigidbody owner, Transform muzzleTransform, GameObject vehicleRoot = null)
        {
            ownerBody = owner; muzzle = muzzleTransform;
            ownerRoot = vehicleRoot != null ? vehicleRoot : (owner != null ? owner.transform.root.gameObject : null);
        }
        public void BindSink(MonoBehaviour sink)
        {
            if (sink != null && !(sink is IRailgunShotSink))
                throw new ArgumentException("Sink must implement IRailgunShotSink", nameof(sink));
            shotSinkComponent = sink;
        }
        public void BindResourceBank(MonoBehaviour bank)
        {
            if (bank != null && !(bank is IRailgunShotResourceBank))
                throw new ArgumentException("Bank must implement IRailgunShotResourceBank", nameof(bank));
            resourceComponent = bank;
        }
        public void SetOperational(bool active) => gate.SetOperational(active);
        public void SetRequireHierarchyAttachment(bool enabled) => requireHierarchyAttachment = enabled;
        public void Configure(RailgunConfig config) => configuration = config ?? new RailgunConfig();
        public bool TryFire() => TryFireAtTime(Time.time);
        public bool TryFireAtTime(float now)
        {
            LastFailure = RailgunFireFailure.None;
            if (!isActiveAndEnabled || !gate.Operational) return Fail(RailgunFireFailure.Inactive);
            if (!RailgunRules.Finite(now)) return Fail(RailgunFireFailure.ClockInvalid);
            if (muzzle == null) return Fail(RailgunFireFailure.MissingMuzzle);
            if (ownerBody == null || ownerRoot == null) return Fail(RailgunFireFailure.MissingOwner);
            if (requireHierarchyAttachment && !transform.IsChildOf(ownerBody.transform))
                return Fail(RailgunFireFailure.Detached);
            IRailgunShotSink sink = shotSinkComponent as IRailgunShotSink;
            if (shotSinkComponent == null || sink == null || !shotSinkComponent.isActiveAndEnabled)
                return Fail(RailgunFireFailure.MissingSink);
            if (!gate.CanFire(now)) return Fail(RailgunFireFailure.Cooldown);
            IRailgunShotResourceBank bank = resourceComponent as IRailgunShotResourceBank;
            if (resourceComponent != null && (bank == null || !resourceComponent.isActiveAndEnabled))
                return Fail(RailgunFireFailure.ResourceUnavailable);
            if (bank != null && !bank.TryConsumeShot())
                return Fail(RailgunFireFailure.ResourceUnavailable);

            RailgunSpec spec = (configuration ?? new RailgunConfig()).Snapshot();
            Vector3 direction = muzzle.forward.normalized;
            Vector3 inherited = ownerBody.GetPointVelocity(muzzle.position) * spec.InheritedVelocityFactor;
            RailgunShot shot = new RailgunShot(gate.SuccessfulShots, muzzle.position, direction,
                inherited + direction * spec.Speed, spec, ownerBody, ownerRoot);
            bool spawned = false;
            try { spawned = sink.TrySpawnShot(shot); }
            catch (Exception ex) { Debug.LogException(ex, this); }
            if (!spawned)
            {
                if (bank != null) bank.RefundShot();
                return Fail(RailgunFireFailure.EmissionRejected);
            }
            gate.Commit(now, spec.Cooldown);
            if (!ownerBody.isKinematic && spec.RecoilImpulse > 0f)
            {
                Vector3 impulse = -direction * spec.RecoilImpulse;
                if (spec.RecoilAtMuzzle)
                    ownerBody.AddForceAtPosition(impulse, muzzle.position, ForceMode.Impulse);
                else ownerBody.AddForce(impulse, ForceMode.Impulse);
            }
            return true;
        }
        private bool Fail(RailgunFireFailure reason) { LastFailure = reason; return false; }
    }
}
