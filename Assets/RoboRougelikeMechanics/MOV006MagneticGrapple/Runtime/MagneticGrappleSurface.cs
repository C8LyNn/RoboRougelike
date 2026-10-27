using UnityEngine;

namespace RoboRougelike.Mechanics.MOV006MagneticGrapple
{
    // Optional gameplay marker. Put on a collider or its parent if requireMagneticSurface=true.
    public sealed class MagneticGrappleSurface : MonoBehaviour
    {
        [SerializeField] private bool acceptsGrapple = true;
        public bool AcceptsGrapple => enabled && isActiveAndEnabled && acceptsGrapple;
        public void SetAcceptsGrapple(bool value) => acceptsGrapple = value;
    }
}
