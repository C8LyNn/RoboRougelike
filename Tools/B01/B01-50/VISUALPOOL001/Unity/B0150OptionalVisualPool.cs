// B01-50 VISUALPOOL-001: opt-in Unity 6 cosmetic-only GameObject pool.
// Never use for projectiles, actual physical debris, damage, gameplay, AI or colliders.
#if UNITY_5_3_OR_NEWER
using UnityEngine;
using B01.Performance.VisualPool;

namespace B01.Performance.VisualPool.UnityAdapter
{
    public sealed class B0150OptionalVisualPool : MonoBehaviour
    {
        [SerializeField] private GameObject purelyDecorativePrefab;
        [SerializeField, Range(1, 256)] private int prewarmedInstances = 32;
        [SerializeField, Range(0.01f, 60f)] private float displayLifetimeSeconds = 1.5f;

        private OptionalVisualLeasePool<GameObject> pool;

        public int ActiveVisuals => pool == null ? 0 : pool.ActiveCount;
        public int PrewarmedVisuals => pool == null ? 0 : pool.Registered;
        public long VisualsGranted => pool == null ? 0 : pool.Granted;
        public long RejectedAtCapacity => pool == null ? 0 : pool.RejectedCapacity;

        private static void Deactivate(GameObject instance)
        {
            if (instance != null) instance.SetActive(false);
        }

        private void Awake()
        {
            if (purelyDecorativePrefab == null) return;
            // FAIL CLOSED: even if a prefab is labelled "cosmetic", never accept
            // any gameplay/physics/callback components on it or its descendants.
            if (purelyDecorativePrefab.GetComponentsInChildren<Collider>(true).Length != 0 ||
                purelyDecorativePrefab.GetComponentsInChildren<Collider2D>(true).Length != 0 ||
                purelyDecorativePrefab.GetComponentsInChildren<Rigidbody>(true).Length != 0 ||
                purelyDecorativePrefab.GetComponentsInChildren<Rigidbody2D>(true).Length != 0 ||
                purelyDecorativePrefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0 ||
                purelyDecorativePrefab.GetComponentsInChildren<AudioSource>(true).Length != 0)
            {
                Debug.LogError("VISUALPOOL-001: refusing prefab with physics, custom MonoBehaviour or sound. Only dispensable visuals.", this);
                return;
            }
            int count = Mathf.Clamp(prewarmedInstances, 1, 256);
            pool = new OptionalVisualLeasePool<GameObject>(count);
            for (int i = 0; i < count; i++)
            {
                // Instantiation occurs only during prewarm, not on the visual burst.
                GameObject instance = Instantiate(purelyDecorativePrefab, transform);
                instance.name = "B0150 optional pooled visual " + i;
                instance.SetActive(false);
                pool.Register(instance);
            }
        }

        // Gameplay-important visuals, hit detection and projectiles are NOT allowed.
        // If the budget is full we intentionally skip a dispensable visual.
        public bool TryShowOptional(Vector3 position, Quaternion rotation)
        {
            if (!isActiveAndEnabled || pool == null) return false;
            if (!pool.TryAcquire(Time.realtimeSinceStartupAsDouble * 1000d,
                    Mathf.Clamp(displayLifetimeSeconds, 0.01f, 60f) * 1000d, out VisualLease lease))
                return false;

            GameObject visual = pool.Get(lease);
            try
            {
                visual.transform.SetPositionAndRotation(position, rotation);
                visual.SetActive(true);
                // ParticleSystem visuals must restart on each reuse; never control
                // collision, triggers or any gameplay scripts.
                ParticleSystem[] particles = visual.GetComponentsInChildren<ParticleSystem>(true);
                foreach (ParticleSystem particle in particles)
                {
                    particle.Clear(true);
                    particle.Play(true);
                }
                return true;
            }
            catch
            {
                pool.TryRelease(lease, Deactivate);
                throw;
            }
        }

        private void Update()
        {
            if (pool != null)
                pool.RetireExpired(Time.realtimeSinceStartupAsDouble * 1000d, Deactivate);
        }

        private void OnDisable()
        {
            if (pool != null) pool.ReleaseAll(Deactivate);
        }

        // All GameObjects are ordinary children; Unity owns destruction when parent is destroyed.
        // No per-burst Destroy calls or source game scene modification.
    }
}
#endif
