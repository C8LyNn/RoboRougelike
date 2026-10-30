// Manual burst only; never auto-hooks game VFX or Codex systems.
#if UNITY_5_3_OR_NEWER
using UnityEngine;

namespace B01.Performance.VisualPool.UnityAdapter
{
    public sealed class B0150OptionalVisualPoolDemo : MonoBehaviour
    {
        [SerializeField] private B0150OptionalVisualPool pool;
        [SerializeField, Range(1, 256)] private int burstCount = 80;
        [SerializeField] private float spacing = 0.25f;

        [ContextMenu("VISUALPOOL-001 / Burst optional visual instances")]
        public void BurstVisuals()
        {
            if (!Application.isPlaying || pool == null) return;
            for (int i = 0; i < burstCount; i++)
            {
                Vector3 point = transform.position +
                    new Vector3((i % 16) * spacing, 0, (i / 16) * spacing);
                pool.TryShowOptional(point, Quaternion.identity);
            }
        }
    }
}
#endif
