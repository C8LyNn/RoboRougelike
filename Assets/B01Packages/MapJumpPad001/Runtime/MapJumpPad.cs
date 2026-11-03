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
        // Reused scratch avoids per-trigger allocations while retiring missing part colliders.
        private readonly List<Collider> staleContacts = new List<Collider>(4);

        public bool IsActive => active;
        public Vector2 FootprintMeters => footprintMeters;
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
            // A destroyed or disabled vehicle part may never deliver OnTriggerExit.
            // Remove only invalid contacts; live colliders still suppress double launches.
            staleContacts.Clear();
            foreach (Collider recorded in colliders)
            {
                if (!recorded || !recorded.enabled ||
                    !recorded.gameObject.activeInHierarchy ||
                    recorded.attachedRigidbody != body)
                    staleContacts.Add(recorded);
            }
            for (int i = 0; i < staleContacts.Count; i++)
                colliders.Remove(staleContacts[i]);
            staleContacts.Clear();

            // Launch only on a genuine new body entry, not the second live wheel.
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
