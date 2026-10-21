using UnityEngine;

namespace RoboRougelike.Mechanics.MOV003Blink
{
    // Optional stand-alone demo input ONLY; Codex should remove/disable it for the real game.
    public sealed class BlinkPrototypeInput : MonoBehaviour
    {
        [SerializeField] private BlinkDrive drive;
        [SerializeField] private KeyCode activationKey = KeyCode.B;
        private void Awake() { if (drive == null) drive = GetComponent<BlinkDrive>(); }
        private void Update()
        {
            if (drive != null && Input.GetKeyDown(activationKey))
                Debug.Log($"[MOV003] Blink result: {drive.TryActivate()}", this);
        }
    }
}
