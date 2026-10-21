using UnityEngine;

namespace RoboRougelike.Mechanics.MOV006MagneticGrapple
{
    // Physical, destructible module: the garage/damage graph owns its lifecycle.
    public sealed class MagneticGrappleEmitter : MonoBehaviour
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private bool operational = true;
        private System.Func<bool> availabilityFilter;
        public void SetAvailabilityFilter(System.Func<bool> filter)=>availabilityFilter=filter;

        public Transform Muzzle => muzzle != null ? muzzle : transform;
        public bool IsOperational => operational && isActiveAndEnabled && (availabilityFilter==null||availabilityFilter());
        public void SetOperational(bool value) => operational = value;
        public void SetMuzzle(Transform value) => muzzle = value;

        public bool BelongsTo(Rigidbody vehicle)
        {
            return vehicle != null && transform.IsChildOf(vehicle.transform) &&
                GetComponentInParent<Rigidbody>() == vehicle;
        }
    }
}
