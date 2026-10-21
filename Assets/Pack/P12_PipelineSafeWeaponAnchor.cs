using UnityEngine;

namespace RogueMachine.P12PipelineSafe
{
    public class P12PipelineSafeWeaponAnchor : MonoBehaviour
    {
        [SerializeField] private Transform turretYaw;
        [SerializeField] private Transform cannonPitch;

        [SerializeField] private Vector3 yawAnchor =
            new Vector3(0f, 1.10f, 0.10f);

        [SerializeField] private Vector3 pitchAnchor =
            new Vector3(0f, 0f, 0.52f);

        public void Configure(
            Transform yaw,
            Transform pitch,
            Vector3 yawLocalPosition,
            Vector3 pitchLocalPosition)
        {
            turretYaw = yaw;
            cannonPitch = pitch;
            yawAnchor = yawLocalPosition;
            pitchAnchor = pitchLocalPosition;

            ApplyAnchors();
        }

        private void LateUpdate()
        {
            ApplyAnchors();
        }

        private void ApplyAnchors()
        {
            if (turretYaw != null)
                turretYaw.localPosition = yawAnchor;

            if (cannonPitch != null)
                cannonPitch.localPosition = pitchAnchor;
        }
    }
}
