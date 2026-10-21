using UnityEngine;

namespace RoboRougelike.Mechanics.MOV006MagneticGrapple
{
    // Optional demonstration adapter. In production, call TryFire and Release from the game's input / AI.
    public sealed class PrototypeGrappleInput : MonoBehaviour
    {
        [SerializeField] private MagneticGrappleController controller;
        [SerializeField] private MagneticGrappleEmitter emitter;
        [SerializeField] private KeyCode fireKey = KeyCode.G;
        [SerializeField] private KeyCode releaseKey = KeyCode.H;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<MagneticGrappleController>();
            if (emitter == null) emitter = GetComponentInChildren<MagneticGrappleEmitter>();
        }
        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (controller == null || emitter == null) return;
            if (Input.GetKeyDown(fireKey)) controller.TryFire(emitter, emitter.Muzzle.forward);
            if (Input.GetKeyDown(releaseKey)) controller.Release();
#endif
        }
    }
}
