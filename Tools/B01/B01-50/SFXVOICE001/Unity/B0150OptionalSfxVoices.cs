// Optional Unity 6 sandbox adapter. NO edits to original Codex Assets or AudioMixer.
// Never queue important combat telegraphs, warnings, dialogue, gameplay events or accessibility audio.
#if UNITY_5_3_OR_NEWER
using UnityEngine;
using B01.Performance.OptionalAudio;

namespace B01.Performance.OptionalAudio.UnityAdapter
{
    public sealed class B0150OptionalSfxVoices : MonoBehaviour
    {
        [SerializeField, Range(1,32)] private int maxConcurrentOptionalVoices = 8;
        [SerializeField, Range(1f,100f)] private float maxAudibleDistance = 40f;
        [SerializeField, Range(0f,1000f)] private float perKeyCooldownMs = 85f;
        [SerializeField] private Transform listenerTransform;

        private OptionalSfxVoiceBudget budget;
        private AudioSource[] sources;
        private long[] issuedTokens;

        public int ActiveOptionalVoiceCount => budget == null ? 0 : budget.ActiveCount;
        public long RejectedForCapacity => budget == null ? 0 : budget.RejectedCapacity;
        public long RejectedForDistance => budget == null ? 0 : budget.RejectedDistance;
        public long RejectedForCooldown => budget == null ? 0 : budget.RejectedCooldown;

        private void Awake()
        {
            int count = Mathf.Clamp(maxConcurrentOptionalVoices, 1, 32);
            budget = new OptionalSfxVoiceBudget(count, Mathf.Clamp(maxAudibleDistance, 1f, 100f),
                Mathf.Clamp(perKeyCooldownMs, 0, 1000));
            sources = new AudioSource[count];
            issuedTokens = new long[count];
            for (int i=0; i<count; i++)
            {
                var child = new GameObject("SFXVOICE optional sound " + i);
                child.transform.SetParent(transform, false);
                var source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.maxDistance = Mathf.Clamp(maxAudibleDistance,1f,100f);
                sources[i] = source;
            }
        }

        // Only explicitly approved dispensable ambience/foley. Must be invoked on Unity main thread.
        public bool TryPlayOptional(AudioClip clip, string categoryKey, Vector3 worldPosition,
            bool accented = false, float volume = 1f)
        {
            if (!isActiveAndEnabled || budget == null || clip == null || listenerTransform == null)
                return false;
            double durationMs = (double)clip.length * 1000d;
            double distance = Vector3.Distance(listenerTransform.position, worldPosition);
            var result = budget.Request(categoryKey,
                accented ? OptionalSfxTier.Accented : OptionalSfxTier.Ambient,
                distance, Time.realtimeSinceStartupAsDouble * 1000d, durationMs);
            if (!result.Granted) return false;
            var voice = sources[result.Slot];
            if (voice.isPlaying) voice.Stop(); // actual preempted purely cosmetic sound
            voice.clip = clip;
            voice.volume = Mathf.Clamp01(volume);
            voice.transform.position = worldPosition;
            issuedTokens[result.Slot] = result.Token;
            voice.Play();
            return true;
        }

        private void Update()
        {
            if (budget == null) return;
            budget.Advance(Time.realtimeSinceStartupAsDouble * 1000d);
            for (int i=0; i<sources.Length; i++)
            {
                if (issuedTokens[i] == 0) continue;
                if (budget.TokenAt(i) != issuedTokens[i])
                {
                    sources[i].Stop();
                    issuedTokens[i] = 0;
                }
                else if (!sources[i].isPlaying)
                {
                    budget.Stop(i, issuedTokens[i]);
                    issuedTokens[i] = 0;
                }
            }
        }

        private void OnDisable()
        {
            if (sources != null)
            {
                foreach (var source in sources) if (source != null) source.Stop();
            }
            if (issuedTokens != null) System.Array.Clear(issuedTokens,0,issuedTokens.Length);
            if (budget != null) budget.Clear();
        }
    }
}
#endif
