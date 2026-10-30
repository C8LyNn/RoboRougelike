// Manual opt-in visual scene test only; no integration with Codex's gameplay.
// Assign a disposable AudioClip and Transform listener in the Unity Inspector.
#if UNITY_5_3_OR_NEWER
using UnityEngine;
namespace B01.Performance.OptionalAudio.UnityAdapter
{
    public sealed class B0150OptionalSfxBurstDemo : MonoBehaviour
    {
        [SerializeField] private B0150OptionalSfxVoices allocator;
        [SerializeField] private AudioClip optionalFoleyClip;
        [SerializeField, Range(1,200)] private int triggerCount = 30;
        [SerializeField, Range(0f,2f)] private float spacing = 0.2f;

        [ContextMenu("SFXVOICE001 / Burst optional foley")]
        public void BurstOptionalSounds()
        {
            if (allocator == null || optionalFoleyClip == null) return;
            for (int i = 0; i < triggerCount; i++)
            {
                Vector3 p = transform.position + new Vector3((i % 10)*spacing, 0, (i / 10)*spacing);
                allocator.TryPlayOptional(optionalFoleyClip,"preview_foley_" + (i % 4),p,
                    accented: (i % 9 == 0), volume: 0.2f);
            }
        }
    }
}
#endif
