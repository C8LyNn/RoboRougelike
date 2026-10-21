using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Local visual recoil. Never changes weapon mounting/aim or Rigidbody forces.</summary>
    public sealed class WeaponRecoilAnimator : MonoBehaviour
    {
        [SerializeField] private Transform animatedRoot; // decorative child, NOT physics weapon root
        [SerializeField] private Vector3 localRecoilOffset = new Vector3(0f, 0f, -.18f);
        [SerializeField, Min(.01f)] private float kickDuration = .065f;
        [SerializeField, Min(.01f)] private float recoveryDuration = .18f;
        private Vector3 _restPosition;
        private float _elapsed = 999f;
        public bool IsAnimating => _elapsed < kickDuration + recoveryDuration;

        private void Awake() { if (animatedRoot != null) Bind(animatedRoot); }

        public void Bind(Transform visualChild)
        {
            if (visualChild == null || visualChild == transform || !visualChild.IsChildOf(transform))
            {
                Debug.LogWarning("VFX-001: recoil must target a decorative CHILD, not the physics root.", this);
                return;
            }
            animatedRoot = visualChild;
            _restPosition = visualChild.localPosition;
            _elapsed = 999f;
        }

        public void PlayRecoil() { _elapsed = 0f; }
        private void Update()
        {
            if (animatedRoot == null || !IsAnimating) return;
            _elapsed += Time.deltaTime;
            float fraction;
            if (_elapsed < kickDuration) fraction = _elapsed / kickDuration;
            else fraction = 1f - Mathf.Clamp01((_elapsed - kickDuration) / recoveryDuration);
            animatedRoot.localPosition = _restPosition + localRecoilOffset * Mathf.Clamp01(fraction);
        }
        private void OnDisable()
        {
            if (animatedRoot != null) animatedRoot.localPosition = _restPosition;
            _elapsed = 999f;
        }
    }
}
