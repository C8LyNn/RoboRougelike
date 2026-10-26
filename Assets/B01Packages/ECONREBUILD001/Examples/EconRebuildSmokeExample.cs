using UnityEngine;
namespace RoboRougelike.B01.Economy.Rebuild
{
    // Data-only smoke component; does not grant money, modify R3 or mark victory.
    public sealed class EconRebuildSmokeExample : MonoBehaviour
    {
        [ContextMenu("ECON rebuild core smoke info")]
        public void Explain() { Debug.Log("ECONREBUILD001: actual R3 wallet + win hooks required; Unity gameplay integration NOT active."); }
    }
}
