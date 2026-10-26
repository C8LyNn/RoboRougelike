using UnityEngine;

namespace RoboRougelike.MapSystemR4
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class MapR4AnchorTrigger : MonoBehaviour
    {
        public string AnchorId { get; private set; }
        private MapR4DemoController _demo;

        public void Initialize(MapR4DemoController demo, string anchorId, float radius)
        {
            _demo = demo;
            AnchorId = anchorId;
            var sphere = GetComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = radius;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_demo != null && other.GetComponentInParent<MapR4PrototypeDriver>() != null)
                _demo.OnDemoAnchor(AnchorId);
        }
    }
}
