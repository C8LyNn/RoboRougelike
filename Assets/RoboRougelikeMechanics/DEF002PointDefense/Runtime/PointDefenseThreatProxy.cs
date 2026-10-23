using UnityEngine;

namespace RoboRougelike.Mechanics.PointDefense
{
    /// <summary>
    /// One proxy per enemy missile/projectile, on its parent root. Map all child colliders
    /// to the same instance to avoid duplicate target scoring and interception damage.
    /// </summary>
    public sealed class PointDefenseThreatProxy : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour adapterBehaviour;
        private IPointDefenseThreat adapter;
        public IPointDefenseThreat Threat { get { return adapter; } }

        private void Awake() { Resolve(); }
        public void Bind(MonoBehaviour behaviour)
        {
            adapterBehaviour = behaviour;
            Resolve();
        }
        private void Resolve()
        {
            adapter = adapterBehaviour as IPointDefenseThreat;
            if (adapterBehaviour != null && adapter == null)
                Debug.LogError("DEF-002: Threat adapter must implement IPointDefenseThreat.", this);
        }
    }
}
