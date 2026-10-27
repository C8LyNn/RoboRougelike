using UnityEngine;

namespace B01.CHL003
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CHL003Gate : MonoBehaviour
    {
        public enum GateType { Checkpoint, Finish }
        [SerializeField] private GateType type;
        private CHL003CourseController owner;
        private int ordinal;
        public GateType Type => type;
        public bool HasTrigger
        {
            get
            {
                var c = GetComponent<BoxCollider>();
                return c != null && c.enabled && c.isTrigger;
            }
        }
        public void Configure(GateType gateType) => type = gateType;
        public void Bind(CHL003CourseController controller, int index)
        {
            owner = controller;
            ordinal = index;
        }
        private void OnTriggerEnter(Collider other)
        {
            if (HasTrigger && owner != null)
                owner.ReceiveGate(this, other, ordinal);
        }
    }
}
