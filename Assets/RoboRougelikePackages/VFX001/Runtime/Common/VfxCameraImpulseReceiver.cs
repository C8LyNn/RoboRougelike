using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>
    /// Additive cosmetic shake on a DECORATIVE CAMERA CHILD (not movement rig).
    /// The main camera rig, physics bodies, aim direction and projectiles are untouched.
    /// Supply the child transform at runtime through Bind or Inspector.
    /// </summary>
    public sealed class VfxCameraImpulseReceiver : MonoBehaviour
    {
        private struct Impulse
        {
            public float Age;
            public float Duration;
            public float Amplitude;
            public float Seed;
        }
        [SerializeField] private Transform cameraVisualChild;
        [SerializeField, Min(.001f)] private float durationSeconds = .3f;
        [SerializeField, Range(0f, .25f)] private float translationMax = .045f;
        [SerializeField, Range(0f, 8f)] private float rotationMaxDegrees = 1.6f;
        [SerializeField, Min(0f)] private float fullStrengthDistance = 5f;
        [SerializeField, Min(.1f)] private float maximumDistance = 28f;
        [SerializeField, Range(1, 16)] private int maxSimultaneousImpulses = 6;
        [SerializeField] private bool useUnscaledTime;
        private readonly List<Impulse> _impulses = new List<Impulse>(8);
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private bool _bound;
        private int _serial;
        public bool IsBound => _bound && cameraVisualChild != null;
        public int ActiveCount => _impulses.Count;
        public bool UsesUnscaledTime => useUnscaledTime;

        private void Awake() { if (cameraVisualChild != null) Bind(cameraVisualChild); }
        public bool Bind(Transform child)
        {
            if (child == null || child == transform || !child.IsChildOf(transform)) return false;
            Restore();
            _impulses.Clear();
            cameraVisualChild = child;
            _restPosition = child.localPosition;
            _restRotation = child.localRotation;
            _bound = true;
            return true;
        }

        public bool Accept(VfxEvent evt)
        {
            if (evt.Kind != VfxEventKind.Hit && evt.Kind != VfxEventKind.Detached &&
                evt.Kind != VfxEventKind.Explosion && evt.Kind != VfxEventKind.CoreDestroyed) return false;
            float scale = evt.Kind == VfxEventKind.CoreDestroyed ? 1f :
                evt.Kind == VfxEventKind.Explosion ? .7f : evt.Kind == VfxEventKind.Detached ? .25f : .12f;
            return AddImpulse(evt.Position, evt.Intensity * scale);
        }

        public bool AddImpulse(Vector3 worldPosition, float intensity01)
        {
            if (!IsBound || !VfxImpulseEnvelope.Finite(worldPosition) || float.IsNaN(intensity01)) return false;
            float radius = Mathf.Max(fullStrengthDistance + .0001f, maximumDistance);
            float attenuation = VfxImpulseEnvelope.Attenuation(
                Vector3.Distance(transform.position, worldPosition), fullStrengthDistance, radius);
            float amount = Mathf.Clamp01(intensity01) * attenuation;
            if (amount <= .001f) return false;
            if (_impulses.Count >= Mathf.Max(1, maxSimultaneousImpulses)) _impulses.RemoveAt(0);
            _serial++;
            _impulses.Add(new Impulse
            {
                Age = 0f,
                Duration = Mathf.Max(.001f, durationSeconds),
                Amplitude = amount,
                Seed = _serial * .731f
            });
            return true;
        }

        private void LateUpdate() { Tick(useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime); }
        public void Tick(float deltaTime)
        {
            if (!IsBound) return;
            float dt = Mathf.Max(0f, deltaTime);
            Vector3 shift = Vector3.zero;
            Vector3 angles = Vector3.zero;
            for (int i = _impulses.Count - 1; i >= 0; i--)
            {
                var impulse = _impulses[i];
                impulse.Age += dt;
                if (impulse.Age >= impulse.Duration) { _impulses.RemoveAt(i); continue; }
                _impulses[i] = impulse;
                float magnitude = impulse.Amplitude * VfxImpulseEnvelope.Decay(impulse.Age, impulse.Duration);
                float phase = impulse.Age * 51f + impulse.Seed;
                shift += new Vector3(Mathf.Sin(phase), Mathf.Cos(phase * 1.23f), 0f) * magnitude;
                angles += new Vector3(Mathf.Cos(phase * .7f), Mathf.Sin(phase * 1.13f), 0f) * magnitude;
            }
            shift = VfxImpulseEnvelope.Bounded(shift, 1f);
            angles = VfxImpulseEnvelope.Bounded(angles, 1f);
            cameraVisualChild.localPosition = _restPosition + shift * translationMax;
            cameraVisualChild.localRotation = _restRotation * Quaternion.Euler(angles * rotationMaxDegrees);
        }

        public void ResetImpulses()
        {
            _impulses.Clear();
            Restore();
        }
        private void Restore()
        {
            if (!_bound || cameraVisualChild == null) return;
            cameraVisualChild.localPosition = _restPosition;
            cameraVisualChild.localRotation = _restRotation;
        }
        private void OnDisable() { ResetImpulses(); }
        private void OnDestroy() { ResetImpulses(); }
    }
}
