// Import into an isolated disposable Unity 6 project only with matching Core source.
// Never wire to gameplay projectiles, contact processing, damage or AI decisions.
#if UNITY_5_3_OR_NEWER
using System;
using B01.Performance.FxPacing;
using UnityEngine;

namespace B01.Performance.FxPacing.UnityAdapter
{
    public sealed class B0150CosmeticFxPacer : MonoBehaviour
    {
        [SerializeField, Range(1, 128)] private int maxActivationsPerFrame = 8;
        [SerializeField, Range(1, 32768)] private int maxBacklog = 384;
        [SerializeField, Range(0f, 30f)] private float visualTtlSeconds = 0.5f;
        [SerializeField, Min(0.1f)] private float spawnedVisualLifetimeSeconds = 3f;

        private sealed class VisualRequest
        {
            public GameObject Prefab;
            public Vector3 Position;
            public Quaternion Rotation;
            public VisualRequest(GameObject prefab, Vector3 position, Quaternion rotation)
            {
                Prefab = prefab; Position = position; Rotation = rotation;
            }
        }

        private CosmeticActivationBudget<VisualRequest> budget;

        public int Pending { get { return budget == null ? 0 : budget.Pending; } }
        public long TotalActivated { get { return budget == null ? 0 : budget.Activated; } }
        public long TotalDroppedExpired { get { return budget == null ? 0 : budget.DroppedExpired; } }
        public long TotalDroppedOverflow { get { return budget == null ? 0 : budget.DroppedOverflow; } }

        private void Awake()
        {
            budget = new CosmeticActivationBudget<VisualRequest>(
                Mathf.Clamp(maxActivationsPerFrame, 1, 128),
                Mathf.Clamp(maxBacklog, 1, 32768),
                Mathf.Clamp(visualTtlSeconds, 0f, 30f) * 1000d);
        }

        // Caller must ensure prefab is COSMETIC ONLY: no gameplay colliders,
        // projectile scripts, damage, AI or physics callbacks.
        public bool TryScheduleVisual(GameObject cosmeticPrefab, Vector3 worldPosition,
            Quaternion rotation, bool important = false)
        {
            if (!isActiveAndEnabled || cosmeticPrefab == null || budget == null)
                return false;
            return budget.TrySchedule(new VisualRequest(cosmeticPrefab, worldPosition, rotation),
                Time.realtimeSinceStartupAsDouble * 1000d,
                important ? CosmeticPriority.Important : CosmeticPriority.Regular);
        }

        private void Update()
        {
            if (budget == null) return;
            budget.DrainFrame(Time.realtimeSinceStartupAsDouble * 1000d, request =>
            {
                if (request.Prefab == null) return; // Unity-destroyed visual reference
                GameObject instance = Instantiate(request.Prefab, request.Position, request.Rotation);
                if (instance != null && spawnedVisualLifetimeSeconds > 0f)
                    Destroy(instance, spawnedVisualLifetimeSeconds);
            });
        }

        private void OnDisable()
        {
            if (budget != null) budget.CancelPending();
        }
    }
}
#endif
