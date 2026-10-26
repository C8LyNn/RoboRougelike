using UnityEngine;

namespace RoboRougelike.MapSystemR4
{
    public static class MapR4ClearanceProbe
    {
        // Early G1b probe for explicitly marked obstacle layers. Excludes ground/roads by mask.
        // No turning/suspension/vehicle dynamics simulation. Never claim full traversability.
        public static bool CheckStraightCorridor(
            Vector3 fromGround, Vector3 toGround, float vehicleWidth, float vehicleHeight,
            int obstacleLayerMask, out Vector3 blockedAt, float stepMeters = 1.5f, float margin = 0.3f)
        {
            blockedAt = fromGround;
            if (vehicleWidth <= 0f || vehicleHeight <= 0f || stepMeters <= 0f || obstacleLayerMask == 0)
                return false; // Fail closed if no obstacle layers were configured.

            var diff = toGround - fromGround;
            var horizontal = new Vector3(diff.x, 0f, diff.z);
            if (horizontal.sqrMagnitude < 0.0001f) return false;
            var rotation = Quaternion.LookRotation(horizontal.normalized, Vector3.up);
            var half = new Vector3(vehicleWidth * 0.5f + margin, vehicleHeight * 0.5f, 0.5f * stepMeters);
            int samples = Mathf.Max(1, Mathf.CeilToInt(diff.magnitude / stepMeters));
            for (int i = 0; i <= samples; i++)
            {
                float t = (float)i / samples;
                var p = Vector3.Lerp(fromGround, toGround, t);
                var center = p + Vector3.up * (vehicleHeight * 0.5f + 0.1f);
                if (Physics.CheckBox(center, half, rotation, obstacleLayerMask, QueryTriggerInteraction.Ignore))
                {
                    blockedAt = p;
                    return false;
                }
            }
            return true;
        }
    }
}
