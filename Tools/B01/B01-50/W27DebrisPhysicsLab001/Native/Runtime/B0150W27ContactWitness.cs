using UnityEngine;
namespace B01.Performance.W27DebrisLab
{
    // The SAME instrumentation is attached in both baseline/candidate sessions.
    public sealed class B0150W27ContactWitness : MonoBehaviour
    {
        public int staticStays;
        public int bodyStays;
        public int enterCount;
        private void OnCollisionEnter(Collision c) { if (c != null) ++enterCount; }
        private void OnCollisionStay(Collision c)
        {
            if (c == null) return;
            if (c.body == null) ++staticStays;
            else ++bodyStays;
        }
    }
}
