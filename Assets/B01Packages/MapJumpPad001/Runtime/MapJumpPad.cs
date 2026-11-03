using System.Collections.Generic;
using UnityEngine;

namespace B01.MapJumpPad001
{
    // Standalone level module. No map generator, robot authority or Save data writes.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class MapJumpPad : MonoBehaviour
    {
        [Header("Launch")]
        [SerializeField, Min(0f)] private float upVelocityChange = 8.5f;
        [SerializeField, Min(0f)] private float forwardVelocityChange = 2f;
        [SerializeField, Min(0f)] private float cooldownSeconds = 1.25f;
        [SerializeField] private bool cancelDownwardVelocity = true;
        [SerializeField] private LayerMask acceptedLayers = ~0;
        [SerializeField] private bool active = true;
        [Header("Grid footprint")]
        [SerializeField] private Vector2 footprintMeters = Vector2.one;

        // Track each actual collider: multi-collider robots must not launch twice.
        private readonly Dictionary<Rigidbody, HashSet<Collider>> contacts =
            new Dictionary<Rigidbody, HashSet<Collider>>();
        private readonly Dictionary<Rigidbody, double> lastLaunch =
            new Dictionary<Rigidbody, double>();

        // A wreckage Rigidbody may be Destroyed/disabled inside a trigger.
        // PhysX does not guarantee OnTriggerExit for those lifecycle events.
        // Reuse these buffers so long encounters do not allocate each tick.
        private readonly List<Rigidbody> staleBodies = new List<Rigidbody>();
        private readonly List<Collider> staleColliders = new List<Collider>();

        public bool IsActive => active;
        public Vector2 FootprintMeters => footprintMeters;
        // Diagnostics only; do not expose mutable dictionaries to callers.
        public int TrackedContactBodies => contacts.Count;
        public int CooldownHistoryBodies => lastLaunch.Count;
        public void SetActive(bool value) { active = value; }
        public void Configure(float up, float forward, float cooldown)
        {
            upVelocityChange = Mathf.Max(0f, up);
            forwardVelocityChange = Mathf.Max(0f, forward);
            cooldownSeconds = Mathf.Max(0f, cooldown);
        }

        private void Reset()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1f, 0.55f, 1f);
            box.center = new Vector3(0f, 0.28f, 0f);
        }

        private void Awake()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (!box.isTrigger)
            {
                Debug.LogError("MAP-JUMPPAD-001 needs a trigger collider; module disabled.", this);
                active = false;
            }
        }

        public bool CanLaunch(Rigidbody body)
        {
            if (!active || !isActiveAndEnabled || !body || body.isKinematic) return false;
            if ((acceptedLayers.value & (1 << body.gameObject.layer)) == 0) return false;
            if (lastLaunch.TryGetValue(body, out double last) &&
                Time.fixedTimeAsDouble - last < cooldownSeconds) return false;
            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other || other.isTrigger) return;
            Rigidbody body = other.attachedRigidbody;
            if (!body || body == GetComponent<Rigidbody>()) return;
            if (!contacts.TryGetValue(body, out HashSet<Collider> colliders))
            {
                colliders = new HashSet<Collider>();
                contacts.Add(body, colliders);
            }
            // Launch only on a genuine new body entry, not the second wheel collider.
            bool first = colliders.Count == 0;
            colliders.Add(other);
            if (!first || !CanLaunch(body)) return;
            Vector3 velocity = body.linearVelocity;
            if (cancelDownwardVelocity && velocity.y < 0f)
                body.linearVelocity = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 velocityChange = transform.up.normalized * upVelocityChange +
                                     transform.forward.normalized * forwardVelocityChange;
            body.AddForce(velocityChange, ForceMode.VelocityChange);
            lastLaunch[body] = Time.fixedTimeAsDouble;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other) return;
            Rigidbody body = other.attachedRigidbody;
            if (!body || !contacts.TryGetValue(body, out HashSet<Collider> colliders)) return;
            colliders.Remove(other);
            if (colliders.Count == 0) contacts.Remove(body);
        }

        // B01-25 W30: original gameplay pad must forget transient R3
        // debris that dies while overlapping it. Do NOT drop live cooldowns.
        // R3Debris can disable colliders for 0.45s before its 10s retirement.
        private void FixedUpdate()
        {
            if (contacts.Count == 0 && lastLaunch.Count == 0) return;
            staleBodies.Clear();
            foreach (var entry in contacts)
            {
                if (!entry.Key)
                {
                    staleBodies.Add(entry.Key);
                    continue;
                }
                staleColliders.Clear();
                foreach (var collider in entry.Value)
                    if (!collider || !collider.enabled ||
                        !collider.gameObject.activeInHierarchy ||
                        collider.attachedRigidbody != entry.Key)
                        staleColliders.Add(collider);
                foreach (var collider in staleColliders)
                    entry.Value.Remove(collider);
                if (entry.Value.Count == 0)
                    staleBodies.Add(entry.Key);
            }
            foreach (var body in staleBodies)
                contacts.Remove(body);
            staleBodies.Clear();

            // The old cooldown history must also release destroyed bodies;
            // retained live bodies still obey exactly the original cooldown.
            foreach (var entry in lastLaunch)
                if (!entry.Key) staleBodies.Add(entry.Key);
            foreach (var body in staleBodies)
                lastLaunch.Remove(body);
            staleBodies.Clear();
            staleColliders.Clear();
        }

        private void OnDisable()
        {
            contacts.Clear();
            lastLaunch.Clear(); // Pad disabled between scenes; no Save authority.
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0f, 0.28f, 0f),
                               new Vector3(footprintMeters.x, 0.55f, footprintMeters.y));
            Gizmos.DrawRay(Vector3.up * 0.3f, Vector3.up * 2.5f);
        }
    }
}
