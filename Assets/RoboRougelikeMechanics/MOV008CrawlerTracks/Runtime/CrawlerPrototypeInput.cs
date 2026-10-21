using UnityEngine;

namespace RoboRougelike.Mechanics.MOV008
{
    /// <summary>Demo only: legacy input. Do not include on the integrated game vehicle.</summary>
    [RequireComponent(typeof(CrawlerTrackController))]
    public sealed class CrawlerPrototypeInput : MonoBehaviour
    {
        private CrawlerTrackController controller;
        private void Awake() => controller = GetComponent<CrawlerTrackController>();

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            float forward = (Input.GetKey(KeyCode.W) ? 1f : 0f) -
                            (Input.GetKey(KeyCode.S) ? 1f : 0f);
            float yaw = (Input.GetKey(KeyCode.D) ? 1f : 0f) -
                        (Input.GetKey(KeyCode.A) ? 1f : 0f);
            controller.SetInput(forward, yaw);
            controller.SetBrake(Input.GetKey(KeyCode.Space));
#else
            // With Input System-only projects, use a game-specific adapter calling SetInput/SetBrake.
            controller.SetInput(0f, 0f);
            controller.SetBrake(false);
#endif
        }
        private void OnDisable()
        {
            if (controller != null)
            {
                controller.SetInput(0f, 0f);
                controller.SetBrake(false);
            }
        }
    }
}
