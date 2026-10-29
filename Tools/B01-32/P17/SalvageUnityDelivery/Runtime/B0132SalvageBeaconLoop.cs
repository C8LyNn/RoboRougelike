using UnityEngine;

namespace RoboRougelike.B0132P17.SalvageUnity
{
    /// <summary>
    /// Cosmetic-only sprite playback. Standalone review asset; Codex decides whether
    /// a real game reward event owns/spawns this prefab. Not a pickup or loot API.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class B0132SalvageBeaconLoop : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames = new Sprite[SalvageBeaconClock.FrameCount];
        [SerializeField] private bool vfxEnabled = true;
        private SpriteRenderer visual;
        private readonly SalvageBeaconClock clock = new SalvageBeaconClock();

        public int Frame => clock.Frame;
        public bool IsVisible => isActiveAndEnabled && vfxEnabled
            && visual != null && visual.enabled && visual.sprite != null;

        private void Awake() => visual = GetComponent<SpriteRenderer>();

        private void OnEnable()
        {
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            clock.Reset();
            ShowCurrent();
        }

        private void Update()
        {
            if (!vfxEnabled || visual == null) { Clear(); return; }
            clock.Advance(Time.deltaTime, true);
            ShowCurrent();
        }

        public void SetVisualsEnabled(bool enabled)
        {
            vfxEnabled = enabled;
            if (!enabled) Clear();
            else { clock.Reset(); ShowCurrent(); }
        }

        private void ShowCurrent()
        {
            if (visual == null) return;
            if (!vfxEnabled || frames == null || frames.Length != SalvageBeaconClock.FrameCount)
            {
                Clear();
                return;
            }
            var sprite = frames[clock.Frame];
            if (sprite == null) { Clear(); return; }
            visual.sprite = sprite;
            visual.enabled = true;
        }

        private void Clear()
        {
            clock.Reset();
            if (visual != null) { visual.sprite = null; visual.enabled = false; }
        }

        private void OnDisable() => Clear();
        private void OnDestroy() => Clear();
    }
}
