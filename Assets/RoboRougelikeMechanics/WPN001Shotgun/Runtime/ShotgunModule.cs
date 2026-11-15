using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.WPN001Shotgun
{
    public enum ShotgunFireFailure
    {
        None, ModuleInactive, MissingMuzzle, MissingSink, InvalidClock,
        Detached, Cooldown, ResourceUnavailable, EmissionRejected
    }

    [DisallowMultipleComponent]
    public sealed class ShotgunModule : MonoBehaviour
    {
        [Header("Connections")]
        [SerializeField] private Transform muzzle;
        [SerializeField] private Rigidbody ownerBody;
        [SerializeField] private GameObject ownerRoot;
        [Tooltip("MonoBehaviour implementing IShotgunVolleySink. Required.")]
        [SerializeField] private MonoBehaviour projectileSinkComponent;
        [Tooltip("Optional MonoBehaviour implementing IShotgunShotResourceBank.")]
        [SerializeField] private MonoBehaviour resourceBankComponent;
        [Header("Safety and tuning")]
        [SerializeField] private bool requireHierarchyAttachment = true;
        [SerializeField] private uint basePatternSeed = 1234567u;
        [SerializeField] private ShotgunConfig configuration = new ShotgunConfig();

        private readonly ShotgunFireGate gate = new ShotgunFireGate();
        // Each view describes one shot without allocating an array proportional
        // to pellet count. Later shots cannot change a retained view.
        private sealed class VolleyView : IReadOnlyList<ShotgunPelletSpawn>
        {
            private readonly ShotgunSpread.IndexedPattern pattern;
            private readonly ShotgunSpec spec;
            private readonly uint index;
            private readonly Vector3 origin,inherited;
            private readonly Rigidbody body;
            private readonly GameObject owner;
            public int Count => spec.PelletCount;
            public VolleyView(ShotgunSpread.IndexedPattern spread,ShotgunSpec shot,uint volleyIndex,
                Vector3 shotOrigin,Vector3 inheritedVelocity,Rigidbody ownerBody,GameObject ownerRoot)
            {pattern=spread;spec=shot;index=volleyIndex;origin=shotOrigin;inherited=inheritedVelocity;body=ownerBody;owner=ownerRoot;}
            public ShotgunPelletSpawn this[int pellet] => new ShotgunPelletSpawn(pellet,index,origin,
                pattern.DirectionAt(pellet),inherited,spec.ProjectileSpeed,spec.DamagePerPellet,
                spec.MaxTravelDistance,spec.ProjectileLifetime,body,owner);
            public IEnumerator<ShotgunPelletSpawn> GetEnumerator()
            {for(int i=0;i<Count;i++)yield return this[i];}
            IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
        }
        public ShotgunFireFailure LastFailure { get; private set; }
        public ShotgunSpec CurrentSpec => (configuration ?? new ShotgunConfig()).Snapshot();
        public uint SuccessfulVolleys => gate.SuccessfulVolleys;
        public float NextReadyTime => gate.NextReadyTime;
        public bool Operational => gate.Operational;

        private void Awake()
        {
            if (ownerBody == null) ownerBody = GetComponentInParent<Rigidbody>();
            if (ownerRoot == null && ownerBody != null) ownerRoot = ownerBody.transform.root.gameObject;
        }

        public void Bind(Rigidbody vehicleBody, Transform muzzleTransform, GameObject vehicleRoot = null)
        {
            ownerBody = vehicleBody;
            muzzle = muzzleTransform;
            ownerRoot = vehicleRoot != null ? vehicleRoot :
                (vehicleBody != null ? vehicleBody.transform.root.gameObject : null);
        }

        public void BindSink(MonoBehaviour sink)
        {
            if (sink != null && !(sink is IShotgunVolleySink))
                throw new ArgumentException("Sink must implement IShotgunVolleySink", nameof(sink));
            projectileSinkComponent = sink;
        }

        public void BindResourceBank(MonoBehaviour bank)
        {
            if (bank != null && !(bank is IShotgunShotResourceBank))
                throw new ArgumentException("Bank must implement IShotgunShotResourceBank", nameof(bank));
            resourceBankComponent = bank;
        }

        public void SetOperational(bool enabled) => gate.SetOperational(enabled);
        public void SetRequireHierarchyAttachment(bool require) => requireHierarchyAttachment = require;
        public void SetPatternSeed(uint seed) => basePatternSeed = seed;
        public void Configure(ShotgunConfig config) => configuration = config ?? new ShotgunConfig();

        public bool TryFire() => TryFireAtTime(Time.time);

        // Explicit time injection makes deterministic tests possible.
        // Caller should supply a monotonically increasing game-time clock.
        public bool TryFireAtTime(float now)
        {
            LastFailure = ShotgunFireFailure.None;
            if (!isActiveAndEnabled || !gate.Operational)
                return Fail(ShotgunFireFailure.ModuleInactive);
            if (!ShotgunFireGate.IsFiniteTime(now))
                return Fail(ShotgunFireFailure.InvalidClock);
            if (muzzle == null) return Fail(ShotgunFireFailure.MissingMuzzle);
            IShotgunVolleySink sink = projectileSinkComponent as IShotgunVolleySink;
            if (projectileSinkComponent == null || sink == null || !projectileSinkComponent.isActiveAndEnabled)
                return Fail(ShotgunFireFailure.MissingSink);
            if (requireHierarchyAttachment && (ownerBody == null || !transform.IsChildOf(ownerBody.transform)))
                return Fail(ShotgunFireFailure.Detached);
            if (!gate.CanFire(now)) return Fail(ShotgunFireFailure.Cooldown);

            ShotgunSpec spec = (configuration ?? new ShotgunConfig()).Snapshot();
            var pattern=new ShotgunSpread.IndexedPattern(muzzle.forward,spec.PelletCount,spec.FullConeAngleDegrees,
                ShotgunSpread.VolleySeed(basePatternSeed,gate.SuccessfulVolleys));
            Vector3 inherited = ownerBody != null
                ? ownerBody.GetPointVelocity(muzzle.position) * spec.InheritedVelocityFactor
                : Vector3.zero;
            var volley=new VolleyView(pattern,spec,gate.SuccessfulVolleys,muzzle.position,inherited,ownerBody,ownerRoot);

            IShotgunShotResourceBank bank = resourceBankComponent as IShotgunShotResourceBank;
            if (resourceBankComponent != null && (bank == null || !resourceBankComponent.isActiveAndEnabled))
                return Fail(ShotgunFireFailure.ResourceUnavailable);
            if (bank != null && !bank.TryConsumeShot())
                return Fail(ShotgunFireFailure.ResourceUnavailable);

            bool emitted = false;
            try { emitted = sink.TrySpawnVolley(volley); }
            catch (Exception ex) { Debug.LogException(ex, this); }
            if (!emitted)
            {
                if (bank != null) bank.RefundShot();
                return Fail(ShotgunFireFailure.EmissionRejected);
            }

            // Should always succeed because this module has exclusive ownership of the gate.
            gate.RegisterSuccessfulVolley(now, spec.CooldownSeconds);
            if (spec.RecoilImpulse > 0f && ownerBody != null && !ownerBody.isKinematic)
            {
                Vector3 recoil = -muzzle.forward * spec.RecoilImpulse;
                if (spec.RecoilAtMuzzle)
                    ownerBody.AddForceAtPosition(recoil, muzzle.position, ForceMode.Impulse);
                else
                    ownerBody.AddForce(recoil, ForceMode.Impulse);
            }
            return true;
        }

        private bool Fail(ShotgunFireFailure failure)
        {
            LastFailure = failure;
            return false;
        }
    }
}
