// Optional visual-only smoke demo in disposable Unity 6 scene.
// Requires explicit assigned effect prefab and FXPACE adapter; no automated gameplay changes.
#if UNITY_5_3_OR_NEWER
using UnityEngine;

namespace B01.Performance.FxPacing.UnityAdapter
{
    public sealed class B0150CosmeticBurstDemo : MonoBehaviour
    {
        [SerializeField] private B0150CosmeticFxPacer pacer;
        [SerializeField] private GameObject cosmeticOnlyPrefab;
        [SerializeField, Range(1, 500)] private int burstCount = 120;
        [SerializeField] private float spacing = 0.15f;

        [ContextMenu("FXPACE-001 / Schedule cosmetic-only burst")]
        public void ScheduleBurst()
        {
            if (pacer == null || cosmeticOnlyPrefab == null) return;
            for (int i = 0; i < burstCount; ++i)
            {
                var location = transform.position +
                    new Vector3((i % 16) * spacing, 0f, (i / 16) * spacing);
                pacer.TryScheduleVisual(cosmeticOnlyPrefab, location, Quaternion.identity,
                    important: i % 7 == 0);
            }
        }
    }
}
#endif
