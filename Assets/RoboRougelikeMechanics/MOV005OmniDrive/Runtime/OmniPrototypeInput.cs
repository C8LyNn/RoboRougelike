using UnityEngine;

namespace RoboRougelike.Mechanics.MOV005OmniDrive
{
    /// <summary>
    /// DEV ONLY input adapter. Uses legacy Input when enabled in Player Settings.
    /// Production should call OmniDriveController.SetInput from the game's actual input/AI.
    /// For demo: W/S forward, A/D strafe, Q/E yaw. Other controls untouched.
    /// </summary>
    public sealed class OmniPrototypeInput : MonoBehaviour
    {
        [SerializeField] private OmniDriveController controller;
        private void Awake()
        {
            if (controller == null) controller = GetComponent<OmniDriveController>();
        }
        private void Update()
        {
            if (controller == null) return;
#if ENABLE_LEGACY_INPUT_MANAGER
            float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float z = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            float yaw = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
            controller.SetInput(new Vector2(x, z), yaw);
#endif
        }
    }
}
