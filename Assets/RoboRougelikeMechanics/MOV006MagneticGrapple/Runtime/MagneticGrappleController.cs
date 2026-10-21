using System;
using UnityEngine;

namespace RoboRougelike.Mechanics.MOV006MagneticGrapple
{
    /// <summary>
    /// Single active rope per vehicle. Cast -> validate nearest obstruction -> latch -> reel/tension.
    /// No joints, teleportation, collider movement or velocity rewriting.
    /// One controller on the vehicle Rigidbody root; many damageable emitters are supported.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MagneticGrappleController : MonoBehaviour
    {
        [SerializeField] private Rigidbody vehicle;
        [SerializeField] private GrappleTuning tuning = new GrappleTuning();
        [SerializeField] private MonoBehaviour energyProvider;
        [SerializeField] private LineRenderer optionalRopeRenderer;
        [SerializeField] private bool showDebugRay = false;

        private bool attached;
        private Rigidbody originalAnchorBody;
        private MagneticGrappleEmitter activeEmitter;
        private Collider anchorCollider;
        private Rigidbody anchorBody;
        private Vector3 localAnchor;
        private float ropeLength;
        private float attachmentElapsed;
        private float cooldownRemaining;
        private bool reelCommand;
        private readonly RaycastHit[] lineHits = new RaycastHit[64];

        public GrappleTuning Settings => tuning;
        public bool IsAttached => attached;
        public float CooldownRemaining => cooldownRemaining;
        public float CurrentRopeLength => ropeLength;
        public Rigidbody AttachedRigidbody => anchorBody;
        public MagneticGrappleEmitter ActiveEmitter => activeEmitter;
        public event Action Attached;
        public event Action Released;

        /// <summary>Optional allow-list, e.g. prevent friendly-target grapples. Return false to BLOCK the ray.</summary>
        public Predicate<Collider> TargetFilter { get; set; }

        private void Awake()
        {
            if (vehicle == null) vehicle = GetComponent<Rigidbody>();
            tuning.Sanitize();
            if (optionalRopeRenderer != null)
            {
                optionalRopeRenderer.useWorldSpace = true;
                optionalRopeRenderer.enabled = false;
            }
        }

        private void OnValidate()
        {
            if (tuning == null) tuning = new GrappleTuning();
            tuning.Sanitize();
        }

        public void BindVehicle(Rigidbody body)
        {
            if (IsAttached) Release();
            vehicle = body;
        }

        public void SetEnergyProvider(MonoBehaviour provider) => energyProvider = provider;
        public void SetRopeRenderer(LineRenderer line)
        {
            if (optionalRopeRenderer != null) optionalRopeRenderer.enabled = false;
            optionalRopeRenderer = line;
            if (optionalRopeRenderer != null)
            {
                optionalRopeRenderer.useWorldSpace = true;
                optionalRopeRenderer.enabled = IsAttached;
            }
        }

        public bool TryFire(MagneticGrappleEmitter emitter, Vector3 worldDirection)
        {
            if (!isActiveAndEnabled || IsAttached || cooldownRemaining > 0f ||
                vehicle == null || vehicle.isKinematic ||
                emitter == null || !emitter.IsOperational || !emitter.BelongsTo(vehicle) ||
                !emitter.Muzzle.IsChildOf(vehicle.transform) ||
                !GrappleMath.HasDirection(worldDirection))
                return false;

            tuning.Sanitize();
            Vector3 origin = emitter.Muzzle.position;
            Vector3 direction = worldDirection.normalized;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, tuning.maxRange,
                tuning.targetAndOccluderLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // Ignore own vehicle only. Any other solid that is not grappleable BLOCKS the shot.
            foreach (RaycastHit hit in hits)
            {
                Collider other = hit.collider;
                if (other == null || IsOwnCollider(other)) continue;
                Rigidbody body = other.attachedRigidbody;
                if (body != null && !body.isKinematic && !tuning.allowDynamicTargets)
                    return false;
                if (tuning.requireMagneticSurface)
                {
                    MagneticGrappleSurface surface = other.GetComponentInParent<MagneticGrappleSurface>();
                    if (surface == null || !surface.AcceptsGrapple) return false;
                }
                if (TargetFilter != null && !TargetFilter(other)) return false;
                if (tuning.useEnergy)
                {
                    IGrappleEnergySource energy = energyProvider as IGrappleEnergySource;
                    if (energyProvider == null || !energyProvider.isActiveAndEnabled ||
                        energy == null || !energy.TrySpend(tuning.activationEnergy)) return false;
                }

                attached = true;
                activeEmitter = emitter;
                anchorCollider = other;
                originalAnchorBody = body;
                anchorBody = body != null && !body.isKinematic ? body : null;
                Transform anchorTransform = anchorBody != null ? anchorBody.transform : other.transform;
                localAnchor = anchorTransform.InverseTransformPoint(hit.point);
                ropeLength = Mathf.Max(0.1f, Vector3.Distance(origin, hit.point));
                reelCommand = tuning.reelAutomatically;
                attachmentElapsed = 0f;
                if (optionalRopeRenderer != null)
                {
                    optionalRopeRenderer.positionCount = 2;
                    optionalRopeRenderer.enabled = true;
                }
                Attached?.Invoke();
                return true;
            }
            return false;
        }

        public void SetReeling(bool active) => reelCommand = active;

        public void Release()
        {
            if (!IsAttached) return;
            attached = false;
            anchorCollider = null;
            anchorBody = null;
            originalAnchorBody = null;
            activeEmitter = null;
            attachmentElapsed = 0f;
            ropeLength = 0f;
            reelCommand = false;
            cooldownRemaining = Mathf.Max(cooldownRemaining, tuning.cooldownSeconds);
            if (optionalRopeRenderer != null) optionalRopeRenderer.enabled = false;
            Released?.Invoke();
        }

        private bool IsOwnCollider(Collider candidate)
        {
            return candidate.attachedRigidbody == vehicle ||
                candidate.transform.IsChildOf(vehicle.transform);
        }

        private bool IsOccluded(Vector3 start, Vector3 end)
        {
            Vector3 offset = end - start;
            float distance = offset.magnitude;
            if (distance < 0.05f) return false;
            int count = Physics.RaycastNonAlloc(start, offset / distance, lineHits,
                Mathf.Max(0f, distance - 0.02f), tuning.targetAndOccluderLayers,
                QueryTriggerInteraction.Ignore);
            // With many blocks, non-alloc query can overflow. Fall back to a complete query
            // only in that case; do not break simply because the car has 64+ own colliders.
            if (count == lineHits.Length)
            {
                RaycastHit[] all = Physics.RaycastAll(start, offset / distance,
                    Mathf.Max(0f, distance - 0.02f), tuning.targetAndOccluderLayers,
                    QueryTriggerInteraction.Ignore);
                foreach (RaycastHit item in all)
                {
                    Collider candidate = item.collider;
                    if (candidate == null || IsOwnCollider(candidate) || candidate == anchorCollider) continue;
                    if (anchorBody != null && candidate.attachedRigidbody == anchorBody) continue;
                    return true;
                }
                return false;
            }
            for (int i = 0; i < count; i++)
            {
                Collider c = lineHits[i].collider;
                if (c == null || IsOwnCollider(c) || c == anchorCollider) continue;
                if (anchorBody != null && c.attachedRigidbody == anchorBody) continue;
                return true;
            }
            return false;
        }

        private Vector3 AnchorPosition()
        {
            return anchorBody != null
                ? anchorBody.transform.TransformPoint(localAnchor)
                : anchorCollider.transform.TransformPoint(localAnchor);
        }

        private void FixedUpdate()
        {
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.fixedDeltaTime);
            if (!IsAttached) return;
            if (vehicle == null || vehicle.isKinematic ||
                activeEmitter == null || !activeEmitter.IsOperational ||
                !activeEmitter.BelongsTo(vehicle) ||
                !activeEmitter.Muzzle.IsChildOf(vehicle.transform) ||
                anchorCollider == null || !anchorCollider.enabled ||
                (TargetFilter!=null&&!TargetFilter(anchorCollider)) ||
                anchorCollider.attachedRigidbody != originalAnchorBody ||
                !anchorCollider.gameObject.activeInHierarchy ||
                (anchorBody != null && !anchorBody.gameObject.activeInHierarchy))
            {
                Release();
                return;
            }

            attachmentElapsed += Time.fixedDeltaTime;
            if (tuning.maximumAttachmentSeconds > 0f &&
                attachmentElapsed >= tuning.maximumAttachmentSeconds)
            {
                Release();
                return;
            }

            Vector3 origin = activeEmitter.Muzzle.position;
            Vector3 anchor = AnchorPosition();
            Vector3 delta = anchor - origin;
            float distance = delta.magnitude;
            if (distance > tuning.maxStretchDistance || distance < 0.0001f)
            {
                Release();
                return;
            }
            if (tuning.breakWhenLineObstructed && IsOccluded(origin, anchor))
            {
                Release();
                return;
            }
            if (reelCommand)
                ropeLength = GrappleMath.ReelLength(ropeLength,
                    tuning.minRopeLength, tuning.reelSpeed, Time.fixedDeltaTime);

            Vector3 ropeDirection = delta / distance;
            Vector3 myVelocity = vehicle.GetPointVelocity(origin);
            Vector3 anchorVelocity = anchorBody != null ? anchorBody.GetPointVelocity(anchor) : Vector3.zero;
            float separatingSpeed = Vector3.Dot(anchorVelocity - myVelocity, ropeDirection);
            bool dynamicTarget = anchorBody != null && !anchorBody.isKinematic;
            float tension = GrappleMath.Tension(distance, ropeLength, separatingSpeed,
                tuning.springStiffness, tuning.damping,
                tuning.maximumTensionNewtons, vehicle.mass, tuning.maximumVehicleAcceleration,
                dynamicTarget ? anchorBody.mass : 0f, tuning.maximumTargetAcceleration,
                dynamicTarget);
            if (tension > 0f)
            {
                Vector3 force = ropeDirection * tension;
                vehicle.AddForceAtPosition(force, origin, ForceMode.Force);
                if (dynamicTarget) anchorBody.AddForceAtPosition(-force, anchor, ForceMode.Force);
            }
            if (showDebugRay) Debug.DrawLine(origin, anchor, Color.cyan, Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            if (!IsAttached || optionalRopeRenderer == null) return;
            if (activeEmitter == null || anchorCollider == null)
            {
                Release();
                return;
            }
            optionalRopeRenderer.SetPosition(0, activeEmitter.Muzzle.position);
            optionalRopeRenderer.SetPosition(1, AnchorPosition());
        }

        private void OnDisable() => Release();
        private void OnDestroy() => Release();
    }
}
