using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Cosmetic exhaust. Does not alter forces; never rescales its parent vehicle.</summary>
    public sealed class ThrusterVfxController : MonoBehaviour
    {
        [SerializeField] private Transform flameVisual; // IMPORTANT: must be a separate child of the nozzle.
        [SerializeField] private Light optionalLight;
        [SerializeField, Min(0)] private float maxLength = 1.2f;
        [SerializeField, Min(0)] private float maxWidth = .32f;
        [SerializeField, Min(.1f)] private float responsiveness = 6f;
        [SerializeField, Min(0)] private float maxLightIntensity = 2f;
        private float _current;
        private float _target;
        private Vector3 _initialScale;
        public float CurrentThrust01 => _current;
        public float TargetThrust01 => _target;

        public void SetThrust01(float value) { _target = Mathf.Clamp01(value); }

        private void Awake() { if (flameVisual != null) Bind(flameVisual, optionalLight); }

        public void Bind(Transform visualChild, Light lightSource = null)
        {
            if (visualChild == null || visualChild == transform || !visualChild.IsChildOf(transform))
            {
                Debug.LogWarning("VFX-001: flame visual must be a dedicated CHILD of the nozzle.", this);
                return;
            }
            flameVisual = visualChild;
            optionalLight = lightSource;
            _initialScale = visualChild.localScale;
        }
        private void Update()
        {
            _current = Mathf.MoveTowards(_current, _target, responsiveness * Time.deltaTime);
            if (flameVisual != null)
            {
                flameVisual.localScale = Vector3.Scale(_initialScale,
                    new Vector3(maxWidth * Mathf.Max(.01f, _current), maxLength * Mathf.Max(.01f, _current), maxWidth * Mathf.Max(.01f, _current)));
            }
            if (optionalLight != null)
            {
                optionalLight.enabled = _current > .02f;
                optionalLight.intensity = maxLightIntensity * _current;
            }
        }
        private void OnDisable()
        {
            if (optionalLight != null) optionalLight.enabled = false;
        }
    }
}
