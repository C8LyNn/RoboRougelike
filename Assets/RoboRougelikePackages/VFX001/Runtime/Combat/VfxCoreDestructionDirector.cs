using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>
    /// Optional cosmetic sequence AFTER gameplay declares the core destroyed.
    /// Time is scaled; never delays actual death, physics, score or detachment.
    /// The pool may refuse beats under budget pressure. No global RNG state is changed.
    /// </summary>
    public sealed class VfxCoreDestructionDirector : MonoBehaviour
    {
        [SerializeField] private VfxPoolManager pool;
        [SerializeField, Min(.01f)] private float spatialJitterMeters = .45f;
        private System.Random _random;
        private Vector3 _position, _normal;
        private float _intensity, _elapsed;
        private int _nextBeat, _sequenceId;
        private bool _running;
        public bool IsRunning => _running;
        public int EmittedBeats { get; private set; }
        public int CompletedBeats => _nextBeat;

        public bool Begin(Vector3 worldPoint, Vector3 normal, float intensity01, int sequenceId, int seed = 1)
        {
            if (pool == null) pool = GetComponentInParent<VfxPoolManager>();
            if (pool == null || !isActiveAndEnabled) return false;
            _position = worldPoint;
            _normal = normal.sqrMagnitude > .00001f ? normal.normalized : Vector3.up;
            _intensity = Mathf.Clamp01(intensity01);
            _sequenceId = sequenceId;
            _random = new System.Random(seed ^ sequenceId);
            _elapsed = 0f;
            _nextBeat = 0;
            _running = true;
            EmittedBeats = 0;
            Advance(0f);
            return true;
        }

        private void Update() { if (_running) Advance(Time.deltaTime); }

        public void Advance(float deltaSeconds)
        {
            if (!_running) return;
            _elapsed += Mathf.Max(0f, deltaSeconds);
            while (_nextBeat < VfxCoreDestructionTimeline.BeatCount &&
                   _elapsed >= VfxCoreDestructionTimeline.GetBeat(_nextBeat).AtSeconds)
            {
                var beat = VfxCoreDestructionTimeline.GetBeat(_nextBeat);
                float offsetX = (float)(_random.NextDouble() * 2d - 1d) * spatialJitterMeters;
                float offsetZ = (float)(_random.NextDouble() * 2d - 1d) * spatialJitterMeters;
                var position = beat.Kind == VfxEventKind.CoreDestroyed ? _position : _position + new Vector3(offsetX, 0f, offsetZ);
                var ev = new VfxEvent(beat.Kind, position, _normal, beat.Material,
                    _intensity * beat.RelativeIntensity, gameObject.GetInstanceID(), _sequenceId);
                if (pool.Emit(ev) != null) EmittedBeats++;
                _nextBeat++;
            }
            if (_nextBeat >= VfxCoreDestructionTimeline.BeatCount) _running = false;
        }

        public void Bind(VfxPoolManager source) { pool = source; }
        public void Cancel() { _running = false; }
        private void OnDisable() { Cancel(); }
    }
}
