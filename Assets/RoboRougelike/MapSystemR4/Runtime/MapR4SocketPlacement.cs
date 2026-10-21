using UnityEngine;

namespace RoboRougelike.MapSystemR4
{
    public static class MapR4SocketPlacement
    {
        // World-space align source socket to target socket with opposing forward axes.
        // Assumes prefab roots use unit scale. Collision safety must be checked separately.
        public static bool TryAlign(Transform sourceRoot, MapR4ModuleSocket source, MapR4ModuleSocket target)
        {
            if (sourceRoot == null || source == null || target == null) return false;
            if (!source.transform.IsChildOf(sourceRoot) || sourceRoot.lossyScale != Vector3.one) return false;
            if (Mathf.Abs(source.clearWidth - target.clearWidth) > 0.05f) return false;
            if (Mathf.Abs(source.clearHeight - target.clearHeight) > 0.05f) return false;
            var localSocketPos = sourceRoot.InverseTransformPoint(source.transform.position);
            var localSocketRotation = Quaternion.Inverse(sourceRoot.rotation) * source.transform.rotation;
            var desiredSocketRot = target.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
            var desiredRootRot = desiredSocketRot * Quaternion.Inverse(localSocketRotation);
            var angleSlope = Vector3.Angle(desiredSocketRot * Vector3.up, Vector3.up);
            if (angleSlope > Mathf.Min(source.maxSlopeDifferenceDeg, target.maxSlopeDifferenceDeg)) return false;
            sourceRoot.rotation = desiredRootRot;
            sourceRoot.position = target.transform.position - desiredRootRot * localSocketPos;
            return true;
        }
    }
}
