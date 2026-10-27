using UnityEngine;

namespace RoboRougelike.Mechanics.MOV009
{
    /// <summary>OPTIONAL legacy Input Manager demo. Omit when using New Input System.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ActiveSuspensionController))]
    public sealed class ActiveSuspensionPrototypeInput : MonoBehaviour
    {
        [Min(0.01f)] [SerializeField] private float offsetChangeMetersPerSecond = 0.25f;
        private ActiveSuspensionController controller;

        private void Awake() => controller = GetComponent<ActiveSuspensionController>();

#if ENABLE_LEGACY_INPUT_MANAGER
        private void Update()
        {
            float direction = 0f;
            if (Input.GetKey(KeyCode.R)) direction += 1f;
            if (Input.GetKey(KeyCode.F)) direction -= 1f;
            if (Input.GetKeyDown(KeyCode.T)) controller.SetRideHeightOffset(0f);
            if (direction != 0f)
            {
                controller.SetRideHeightOffset(controller.RequestedRideHeightOffsetMeters +
                    direction * offsetChangeMetersPerSecond * Time.deltaTime);
            }
        }
#endif
    }
}
