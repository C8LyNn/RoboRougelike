using UnityEngine;

namespace B01.Performance.DebrisSleep001.NativeTests
{
    // Independent native collision witness; must observe real callbacks before an assertion
    // about contact safety can be considered conclusive.
    public sealed class DebrisStayWitness : MonoBehaviour
    {
        public int stays;
        private void OnCollisionStay(Collision collision) { ++stays; }
    }
}
