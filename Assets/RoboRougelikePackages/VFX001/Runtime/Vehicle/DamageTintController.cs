using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Optional color override for compatible URP materials. Avoids Renderer.material allocations.
    /// NOTE: MaterialPropertyBlock disables SRP Batcher for affected renderers; measure before using on hundreds of parts.</summary>
    public sealed class DamageTintController : MonoBehaviour
    {
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private Color damagedColor = new Color(.65f, .21f, .14f, 1f);
        [SerializeField] private Color criticalColor = new Color(1f, .43f, .2f, 1f);
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private Color[] _baseColors;
        private float _damage;
        private bool _critical;
        public float Damage01 => _damage;
        public bool Critical => _critical;

        private void Awake()
        {
            if (targetRenderers == null || targetRenderers.Length == 0) targetRenderers = GetComponentsInChildren<Renderer>();
            _baseColors = new Color[targetRenderers.Length];
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                var renderer = targetRenderers[i];
                var mat = renderer != null ? renderer.sharedMaterial : null;
                _baseColors[i] = mat != null && mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor")
                    : mat != null && mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            }
        }
        public void SetDamage01(float value) { _damage = Mathf.Clamp01(value); Apply(); }
        public void SetCriticalPulse(bool active) { _critical = active; Apply(); }
        private void Update() { if (_critical) Apply(); }
        private void Apply()
        {
            if (_baseColors == null) return;
            float pulse = _critical ? .5f + .5f * Mathf.Sin(Time.time * 9f) : 0f;
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                var renderer = targetRenderers[i];
                if (renderer == null) continue;
                var mat = renderer.sharedMaterial;
                bool hasBase = mat != null && mat.HasProperty("_BaseColor");
                bool hasColor = mat != null && mat.HasProperty("_Color");
                if (!hasBase && !hasColor) continue;
                Color color = Color.Lerp(_baseColors[i], damagedColor, _damage * .65f);
                if (_critical) color = Color.Lerp(color, criticalColor, pulse * .35f);
                renderer.GetPropertyBlock(_block);
                _block.SetColor(hasBase ? "_BaseColor" : "_Color", color);
                renderer.SetPropertyBlock(_block);
            }
        }
    }
}
