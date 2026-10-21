using UnityEngine;

namespace RoboRougelike.Mechanics.COV001
{
    /// <summary>Manual demonstration only; never scans for gameplay objects.</summary>
    [RequireComponent(typeof(DestructibleArenaCover))]
    public sealed class COV001DemoDriver : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float demoHit = 20f;

        [ContextMenu("COV-001 / Apply Demo Hit")]
        public void ApplyDemoHit() => GetComponent<DestructibleArenaCover>().ApplyDamage(demoHit);

        [ContextMenu("COV-001 / Reset Cover")]
        public void ResetDemo() => GetComponent<DestructibleArenaCover>().ResetCover();
    }
}
