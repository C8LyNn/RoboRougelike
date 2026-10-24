using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Legacy one-shot cleanup. Lifetime can be configured after AddComponent.</summary>
    public sealed class TransientAutoDestroy : MonoBehaviour
    {
        [SerializeField] private float lifetime = 1f;
        private bool _started;
        public void SetLifetime(float value)
        {
            lifetime = Mathf.Max(.01f, value);
            if (_started) Schedule();
        }
        private void Start() { _started = true; Schedule(); }
        private void Schedule()
        {
            CancelInvoke(nameof(DestroySelf));
            Invoke(nameof(DestroySelf), lifetime);
        }
        private void OnDisable() { CancelInvoke(nameof(DestroySelf)); }
        private void DestroySelf() { if (gameObject != null) Destroy(gameObject); }
    }
}
