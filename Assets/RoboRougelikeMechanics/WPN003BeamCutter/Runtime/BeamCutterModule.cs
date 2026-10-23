using System;
using UnityEngine;

namespace RoboRougelike.Mechanics.BeamCutter
{
    /// <summary>
    /// Non-allocating ray query for the first non-self obstacle. Damage only via injected R6 sink.
    /// Each FixedUpdate computes DPS * ACTUAL firing seconds; no direct target HP changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BeamCutterModule : MonoBehaviour
    {
        private const int MaxRayHits = 128;
        private const int MaxMuzzleOverlaps = 64;

        [SerializeField] private BeamCutterSettings settings = new BeamCutterSettings();
        [SerializeField] private Transform muzzle;
        [SerializeField] private Rigidbody ownerBody;
        [Tooltip("The root of OWN colliders only (not an arena parent). Use SetOwnColliderFilter for split Rigidbodies.")]
        [SerializeField] private Transform ownerColliderRoot;
        [SerializeField] private MonoBehaviour damageSinkBehaviour;
        [SerializeField] private MonoBehaviour energySourceBehaviour;
        [SerializeField] private LineRenderer beamLine;
        [SerializeField] private bool automaticFixedUpdate = true;

        private readonly RaycastHit[] _hits = new RaycastHit[MaxRayHits];
        private readonly Collider[] _muzzleOverlaps = new Collider[MaxMuzzleOverlaps];
        private BeamCutterSettings _runtimeSettings;
        private BeamThermalState _thermal;
        private IBeamDamageSink _sink;
        private IBeamEnergySource _energy;
        private Func<Collider, bool> _additionalSelfFilter;
        private bool _triggerHeld;
        private bool _operational = true;
        private bool _attached = true;

        public bool IsOperational => _operational && _attached && isActiveAndEnabled;
        public bool TriggerHeld => _triggerHeld;
        public bool IsOverheated => _thermal != null && _thermal.IsOverheated;
        public float HeatFraction => _thermal != null ? _thermal.HeatFraction : 0f;
        public BeamCutterSettings Settings => (settings ?? new BeamCutterSettings()).Sanitized();
        public BeamStepResult LastResult { get; private set; }

        private void Awake()
        {
            ApplySettings();
            _sink = damageSinkBehaviour as IBeamDamageSink;
            _energy = energySourceBehaviour as IBeamEnergySource;
            HideBeam();
        }

        private void OnValidate()
        {
            if (settings == null) settings = new BeamCutterSettings();
            settings = settings.Sanitized();
        }

        private void OnDisable()
        {
            _triggerHeld = false;
            HideBeam();
        }

        private void FixedUpdate()
        {
            if (automaticFixedUpdate) ManualFixedStep(Time.fixedDeltaTime);
        }

        /// <summary>Use false for deterministic PlayMode tests or an external fixed-step orchestrator.</summary>
        public void SetAutomaticSimulation(bool enabled) => automaticFixedUpdate = enabled;
        public void SetTriggerHeld(bool held) => _triggerHeld = held && _operational && _attached;
        public void SetOperational(bool operational)
        {
            _operational = operational;
            if (!operational) { _triggerHeld = false; HideBeam(); }
        }
        public void SetAttached(bool attached)
        {
            _attached = attached;
            if (!attached) { _triggerHeld = false; HideBeam(); }
        }

        public void Bind(Rigidbody body, Transform muzzleTransform, Transform ownColliderRoot,
            IBeamDamageSink damageSink = null, IBeamEnergySource energySource = null)
        {
            ownerBody = body;
            muzzle = muzzleTransform;
            ownerColliderRoot = ownColliderRoot;
            _sink = damageSink;
            _energy = energySource;
            damageSinkBehaviour = damageSink as MonoBehaviour;
            energySourceBehaviour = energySource as MonoBehaviour;
        }

        /// <summary>For split-graph vehicles, return true for ANY own collider, including detached-but-still-friendly parts.</summary>
        public void SetOwnColliderFilter(Func<Collider, bool> isOwnCollider) => _additionalSelfFilter = isOwnCollider;
        public void SetLineRenderer(LineRenderer line) { beamLine = line; HideBeam(); }
        public void SetDamageSink(IBeamDamageSink sink) { _sink = sink; damageSinkBehaviour = sink as MonoBehaviour; }
        public void SetEnergySource(IBeamEnergySource source) { _energy = source; energySourceBehaviour = source as MonoBehaviour; }

        public void Configure(BeamCutterSettings configuration)
        {
            settings = (configuration ?? new BeamCutterSettings()).Sanitized();
            ApplySettings(); // Explicit configuration resets heat; call between encounters.
        }

        public float Heat => EnsureThermal().Heat;
        public void RestoreThermalState(float heat,bool locked) => EnsureThermal().Restore(heat,locked);
        public void ResetThermalState() => EnsureThermal().Reset();

        public BeamStepResult ManualFixedStep(float stepSeconds)
        {
            BeamThermalState thermal = EnsureThermal();
            Vector3 end = muzzle != null ? muzzle.position : transform.position;
            if (!BeamCutterSettings.IsFinite(stepSeconds) || stepSeconds <= 0f)
                return Store(new BeamStepResult(BeamStepStatus.Idle, 0f, null, 0f, end));

            if (!IsOperational)
            {
                thermal.AdvanceIdle(stepSeconds);
                HideBeam();
                return Store(new BeamStepResult(BeamStepStatus.Inoperable, 0f, null, 0f, end));
            }
            if (ownerBody == null || muzzle == null)
            {
                thermal.AdvanceIdle(stepSeconds);
                HideBeam();
                return Store(new BeamStepResult(BeamStepStatus.MissingBindings, 0f, null, 0f, end));
            }
            if (!_triggerHeld)
            {
                thermal.AdvanceIdle(stepSeconds);
                HideBeam();
                return Store(new BeamStepResult(BeamStepStatus.Idle, 0f, null, 0f, end));
            }

            float firedSeconds = thermal.AvailableFireSeconds(stepSeconds);
            if (firedSeconds <= 0f)
            {
                thermal.AdvanceIdle(stepSeconds);
                HideBeam();
                return Store(new BeamStepResult(BeamStepStatus.Overheated, 0f, null, 0f, end));
            }

            BeamCutterSettings cfg = _runtimeSettings;
            Vector3 forward = muzzle.forward.normalized;
            if (forward.sqrMagnitude < 0.5f)
            {
                thermal.AdvanceIdle(stepSeconds);
                HideBeam();
                return Store(new BeamStepResult(BeamStepStatus.MissingBindings, 0f, null, 0f, end));
            }
            Vector3 origin = muzzle.position;
            end = origin + forward * cfg.rangeMeters;
            if (IsMuzzleInsideNonSelfCollider(origin, cfg.hitLayers))
            {
                thermal.AdvanceIdle(stepSeconds);
                HideBeam();
                return Store(new BeamStepResult(BeamStepStatus.ObstructedAtMuzzle, 0f, null, 0f, origin));
            }
            if (!TryFindFirstNonSelfHit(origin, forward, cfg.rangeMeters, cfg.hitLayers, out RaycastHit firstHit, out bool overflow))
            {
                // RaycastNonAlloc buffer saturation is treated as an unsafe/ambiguous query.
                // Fail closed: no damage, energy charge or recoil.
                if (overflow)
                {
                    thermal.AdvanceIdle(stepSeconds);
                    HideBeam();
                    return Store(new BeamStepResult(BeamStepStatus.HitBufferOverflow, 0f, null, 0f, origin));
                }
            }
            float requiredEnergy = cfg.energyPerSecond * firedSeconds;
            if (requiredEnergy > 0f && (_energy == null || !_energy.TryConsume(requiredEnergy)))
            {
                thermal.AdvanceIdle(stepSeconds);
                HideBeam();
                return Store(new BeamStepResult(BeamStepStatus.NoEnergy, 0f, null, 0f, end));
            }

            Collider hitCollider = firstHit.collider;
            float damage = 0f;
            if (hitCollider != null)
            {
                end = firstHit.point;
                damage = cfg.damagePerSecond * firedSeconds;
                if (_sink != null && damage > 0f)
                {
                    _sink.ApplyBeamHit(new BeamHitRequest(hitCollider, firstHit.point, firstHit.normal,
                        forward, damage, firedSeconds, ownerBody, gameObject));
                }
            }

            if (cfg.recoilNewtons > 0f)
            {
                // ForceMode.Force integrated over the full FixedUpdate gives the correct partial-tick impulse.
                ownerBody.AddForceAtPosition(-forward * cfg.recoilNewtons * (firedSeconds / stepSeconds),
                    muzzle.position, ForceMode.Force);
            }
            thermal.AdvanceFiring(firedSeconds, stepSeconds);
            ShowBeam(origin, end, cfg.enableLineRenderer);
            return Store(new BeamStepResult(BeamStepStatus.Firing, firedSeconds, hitCollider, damage, end));
        }

        private BeamStepResult Store(BeamStepResult result) { LastResult = result; return result; }
        private BeamThermalState EnsureThermal()
        {
            if (_thermal == null) ApplySettings();
            if (_sink == null && damageSinkBehaviour != null) _sink = damageSinkBehaviour as IBeamDamageSink;
            if (_energy == null && energySourceBehaviour != null) _energy = energySourceBehaviour as IBeamEnergySource;
            return _thermal;
        }
        private void ApplySettings()
        {
            _runtimeSettings = (settings ?? new BeamCutterSettings()).Sanitized();
            _thermal = new BeamThermalState(_runtimeSettings);
        }

        private bool IsMuzzleInsideNonSelfCollider(Vector3 origin, LayerMask layers)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, 0.01f, _muzzleOverlaps, layers, QueryTriggerInteraction.Ignore);
            if (count >= _muzzleOverlaps.Length) return true; // fail closed
            for (int i = 0; i < count; i++)
            {
                Collider collider = _muzzleOverlaps[i];
                if (collider != null && !IsOwnCollider(collider)) return true;
            }
            return false;
        }

        private bool TryFindFirstNonSelfHit(Vector3 origin, Vector3 direction, float distance,
            LayerMask layers, out RaycastHit closest, out bool overflow)
        {
            closest = default;
            int count = Physics.RaycastNonAlloc(origin, direction, _hits, distance, layers, QueryTriggerInteraction.Ignore);
            overflow = count >= _hits.Length;
            if (overflow) return false;
            float nearest = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                Collider collider = hit.collider;
                if (collider == null || IsOwnCollider(collider)) continue;
                if (hit.distance < 0f || hit.distance >= nearest) continue;
                nearest = hit.distance;
                closest = hit;
                found = true;
            }
            return found;
        }

        private bool IsOwnCollider(Collider collider)
        {
            if (collider == null) return false;
            if (ownerColliderRoot != null && collider.transform.IsChildOf(ownerColliderRoot)) return true;
            if (ownerBody != null && collider.attachedRigidbody == ownerBody) return true;
            return _additionalSelfFilter != null && _additionalSelfFilter(collider);
        }

        private void ShowBeam(Vector3 start, Vector3 end, bool draw)
        {
            if (beamLine == null) return;
            beamLine.enabled = draw;
            if (!draw) return;
            beamLine.useWorldSpace = true;
            beamLine.positionCount = 2;
            beamLine.SetPosition(0, start);
            beamLine.SetPosition(1, end);
        }
        private void HideBeam() { if (beamLine != null) beamLine.enabled = false; }
    }
}
