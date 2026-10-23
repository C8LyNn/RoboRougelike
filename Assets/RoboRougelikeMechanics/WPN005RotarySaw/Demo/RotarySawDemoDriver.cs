using UnityEngine;

namespace RoboRougelike.Weapons.RotarySaw
{
    // No reliance on legacy Input Manager. Set driveMotor in Inspector during PlayMode.
    public sealed class RotarySawDemoDriver : MonoBehaviour
    {
        [SerializeField] private RotarySawModule saw;
        [SerializeField] private bool driveMotor = true;
        public void SetEnabled(bool enabled) { driveMotor = enabled; }
        private void Start() { if (saw == null) saw = GetComponent<RotarySawModule>(); }
        private void Update() { if (saw != null) saw.SetMotorRequested(driveMotor); }
    }
}
