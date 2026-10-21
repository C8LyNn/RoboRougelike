using UnityEngine;

namespace RogueMachine.P12PipelineSafe
{
    public class P12PipelineSafeHierarchyValidator : MonoBehaviour
    {
        [SerializeField] private Transform hull;
        [SerializeField] private Transform wheels;
        [SerializeField] private Transform turretYaw;

        public void Configure(
            Transform hullTransform,
            Transform wheelsTransform,
            Transform turretTransform)
        {
            hull = hullTransform;
            wheels = wheelsTransform;
            turretYaw = turretTransform;
        }

        private void Start()
        {
            Validate("Hull", hull, Vector3.zero);
            Validate("Wheels", wheels, Vector3.zero);

            if (turretYaw == null)
                Debug.LogError("[Pack 12] Turret_Yaw reference is missing.");
        }

        private static void Validate(
            string label,
            Transform target,
            Vector3 expectedLocalPosition)
        {
            if (target == null)
            {
                Debug.LogError(
                    $"[Pack 12] {label} reference is missing."
                );
                return;
            }

            float error =
                Vector3.Distance(
                    target.localPosition,
                    expectedLocalPosition
                );

            if (error > 0.001f)
            {
                Debug.LogError(
                    $"[Pack 12] {label} has invalid local offset {target.localPosition}. Expected {expectedLocalPosition}."
                );
            }
            else
            {
                Debug.Log(
                    $"[Pack 12] {label} local origin OK."
                );
            }
        }
    }
}
