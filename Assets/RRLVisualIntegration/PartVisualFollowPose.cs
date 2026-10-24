using UnityEngine;

namespace RoboRougelike.VisualIntegration
{
    [DefaultExecutionOrder(700)]
    [DisallowMultipleComponent]
    public sealed class PartVisualFollowPose : MonoBehaviour
    {
        Transform target;
        Vector3 offset;
        Quaternion rotationOffset;

        public void Configure(Transform logicalPivot, Vector3 localOffset, Vector3 localEuler)
        {
            target = logicalPivot;
            offset = localOffset;
            rotationOffset = Quaternion.Euler(localEuler);
            SyncNow();
        }

        void LateUpdate() { SyncNow(); }

        public void SyncNow()
        {
            if (target == null) return;
            // Both transforms have the same parent. Deliberately do NOT inherit pivot scale.
            transform.localPosition = target.localPosition + target.localRotation * offset;
            transform.localRotation = target.localRotation * rotationOffset;
        }
    }
}
