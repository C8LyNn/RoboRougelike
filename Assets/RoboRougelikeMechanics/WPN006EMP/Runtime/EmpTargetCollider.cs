using UnityEngine;

namespace RoboRougelike.Mechanics.EMP
{
    /// <summary>
    /// Attach to a target collider whose hierarchy does not contain its EmpReceiver (e.g. R6 block graph).
    /// Exactly one receiver represents each electronic module even if it owns several colliders.
    /// </summary>
    public sealed class EmpTargetCollider : MonoBehaviour
    {
        [SerializeField] private EmpReceiver receiver;
        public EmpReceiver Receiver => receiver;
        public void Bind(EmpReceiver target) => receiver = target;
    }
}
