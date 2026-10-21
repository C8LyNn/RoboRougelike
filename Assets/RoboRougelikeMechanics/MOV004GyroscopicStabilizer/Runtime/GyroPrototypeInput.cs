using UnityEngine;

namespace RoboRougelike.Mechanics.MOV004GyroscopicStabilizer
{
    /// <summary>Optional quick scene demo. Uses legacy Input ONLY when the backend is enabled.</summary>
    public sealed class GyroPrototypeInput : MonoBehaviour
    {
        [SerializeField] private GyroStabilizer stabilizer;
        [SerializeField] private KeyCode toggleKey = KeyCode.G;

        private void Awake()
        {
            if (stabilizer == null) stabilizer = GetComponent<GyroStabilizer>();
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (stabilizer != null && Input.GetKeyDown(toggleKey))
                stabilizer.SetRequestedActive(!stabilizer.IsRequestedActive);
#endif
        }
    }
}
