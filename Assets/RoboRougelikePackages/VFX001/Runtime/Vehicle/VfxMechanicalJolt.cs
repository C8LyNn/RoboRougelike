using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>
    /// Hit reaction and damaged vibration on one VISUAL CHILD. Does not act on Rigidbody,
    /// colliders, physical module root, assembly graph, or aiming controller.
    /// Never bind to an object also animated by another positional visual animator.
    /// </summary>
    public sealed class VfxMechanicalJolt : MonoBehaviour
    {
        [SerializeField] private Transform visualChild;
        [SerializeField, Range(0f, .25f)] private float maximumOffset = .055f;
        [SerializeField, Range(0f, 20f)] private float maximumAngleDegrees = 7f;
        [SerializeField, Min(.02f)] private float recoverySeconds = .32f;
        [SerializeField, Range(0f, 1f)] private float criticalVibration = .25f;
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private Vector3 _kickDirection = Vector3.back;
        private float _kick;
        private float _age;
        private float _damage;
        private bool _detached;
        private bool _bound;
        public bool IsBound => _bound && visualChild != null;
        public float RemainingKick01 => _kick;

        private void Awake() { if (visualChild != null) Bind(visualChild); }

        public bool Bind(Transform child)
        {
            if (child == null || child == transform || !child.IsChildOf(transform)) return false;
            Restore();
            visualChild = child;
            _restPosition = child.localPosition;
            _restRotation = child.localRotation;
            _age = 0f;
            _kick = 0f;
            _bound = true;
            return true;
        }

        public void SetCondition(float damage01, bool detached)
        {
            _damage = Mathf.Clamp01(damage01);
            _detached = detached;
            if (detached) { _kick = 0f; Restore(); }
        }

        public void React(float strength01, Vector3 localDirection)
        {
            if (!IsBound || _detached || !VfxImpulseEnvelope.Finite(localDirection)) return;
            _kick = Mathf.Clamp01(Mathf.Max(_kick, strength01));
            if (localDirection.sqrMagnitude > .00001f) _kickDirection = localDirection.normalized;
            _age = 0f;
        }

        private void LateUpdate() { Tick(Time.deltaTime); }
        public void Tick(float deltaTime)
        {
            if (!IsBound) return;
            float dt = Mathf.Max(0f, deltaTime);
            _age += dt;
            _kick = Mathf.Max(0f, _kick - dt / Mathf.Max(.02f, recoverySeconds));
            float oscillation = Mathf.Sin(_age * 31f) * VfxImpulseEnvelope.Decay(_age, recoverySeconds);
            float tremble = _detached ? 0f : Mathf.Max(0f, _damage - .6f) * criticalVibration * Mathf.Sin(_age * 17f);
            float displacement = _kick * oscillation + tremble;
            visualChild.localPosition = _restPosition + _kickDirection * (displacement * maximumOffset);
            visualChild.localRotation = _restRotation * Quaternion.Euler(
                displacement * maximumAngleDegrees, 0f, -displacement * maximumAngleDegrees * .65f);
        }

        private void Restore()
        {
            if (_bound && visualChild != null)
            {
                visualChild.localPosition = _restPosition;
                visualChild.localRotation = _restRotation;
            }
        }
        private void OnDisable() { Restore(); _kick = 0f; }
        private void OnDestroy() { Restore(); }
    }
}
