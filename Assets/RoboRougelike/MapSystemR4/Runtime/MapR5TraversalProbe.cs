using UnityEngine;

namespace RoboRougelike.MapSystemR4
{
    public enum MapR5ProbeStatus { Clear, Blocked, InvalidConfiguration }

    public struct MapR5ProbeResult
    {
        public MapR5ProbeStatus Status;
        public string Reason;
        public Vector3 BlockedAt;
        public int SamplesChecked;
        public bool IsClear => Status == MapR5ProbeStatus.Clear;
    }

    // G1b: conservative 3D obstacle-layer corridor probe. NOT a driving proof.
    // The caller MUST use a mask that excludes roads, vehicle and trigger-only volumes.
    public static class MapR5TraversalProbe
    {
        public static MapR5ProbeResult CheckEdge(
            Vector3 a, Vector3 b, MapR4EdgeData edge, MapR4VehicleProfile vehicle,
            LayerMask obstacles, float stepMeters = 1f)
        {
            var fail = new MapR5ProbeResult
            {
                Status = MapR5ProbeStatus.InvalidConfiguration,
                Reason = "Invalid configuration", BlockedAt = a
            };
            if (edge == null || vehicle == null || obstacles.value == 0 || stepMeters <= 0f ||
                vehicle.width <= 0f || vehicle.height <= 0f || vehicle.length <= 0f ||
                vehicle.sideMargin < 0f || vehicle.groundClearance < 0f)
                return fail;
            if (edge.width < vehicle.width + 2f * vehicle.sideMargin)
            {
                fail.Reason = "Road narrower than vehicle envelope";
                return fail;
            }
            if (edge.clearHeight < vehicle.height + vehicle.verticalMargin)
            {
                fail.Reason = "Overhead clearance too small";
                return fail;
            }
            float length = MapR5RoadMath.ApproximateLength(a, b, edge);
            int count = Mathf.Clamp(Mathf.CeilToInt(length / stepMeters), 2, 512);
            float widthHalf = vehicle.width * 0.5f + vehicle.sideMargin;
            var half = new Vector3(widthHalf, vehicle.height * 0.5f, vehicle.length * 0.5f);
            Physics.SyncTransforms();
            Vector3 previousCenter = Vector3.zero;
            Quaternion previousRotation = Quaternion.identity;
            for (int i = 0; i <= count; ++i)
            {
                float t = (float)i / count;
                var point = MapR5RoadMath.Evaluate(a, b, edge, t);
                var forward = MapR5RoadMath.TangentXZ(a, b, edge, t);
                if (forward == Vector3.zero)
                {
                    fail.Reason = "Degenerate road tangent";
                    return fail;
                }
                var rotation = Quaternion.LookRotation(forward, Vector3.up);
                var center = point + Vector3.up * (vehicle.groundClearance + half.y);
                if (Physics.CheckBox(center, half, rotation, obstacles, QueryTriggerInteraction.Ignore))
                    return new MapR5ProbeResult { Status = MapR5ProbeStatus.Blocked,
                        Reason = "Obstacle intersects vehicle envelope", BlockedAt = point, SamplesChecked = i+1 };
                if (i > 0)
                {
                    Vector3 delta = center - previousCenter;
                    float distance = delta.magnitude;
                    if (distance > 0.0001f && Physics.BoxCast(previousCenter, half, delta / distance,
                        out RaycastHit hit, previousRotation, distance, obstacles, QueryTriggerInteraction.Ignore))
                        return new MapR5ProbeResult { Status = MapR5ProbeStatus.Blocked,
                            Reason = "Swept vehicle envelope intersects obstacle", BlockedAt = hit.point, SamplesChecked = i+1 };
                }
                previousCenter = center;
                previousRotation = rotation;
            }
            return new MapR5ProbeResult { Status = MapR5ProbeStatus.Clear,
                Reason = "No obstacles detected on specified layers; driving NOT proven", SamplesChecked = count+1 };
        }
    }
}
