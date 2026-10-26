using UnityEngine;

namespace RoboRougelike.MapSystemR4
{
    // G1b obstacle-only probe for the interpolated junction path. Excludes ground/road layer.
    public static class MapR6JunctionClearanceProbe
    {
        public static MapR5ProbeResult Check(
            MapR6JunctionPlan plan, MapR4VehicleProfile vehicle,
            LayerMask obstacleLayers, float stepMeters = 0.5f)
        {
            if (!plan.Valid || vehicle == null)
                return new MapR5ProbeResult
                {
                    Status = MapR5ProbeStatus.InvalidConfiguration,
                    Reason = "Junction candidate is invalid or no vehicle was supplied",
                    BlockedAt = plan.Start
                };
            var virtualEdge = new MapR4EdgeData
            {
                id = "junction_probe",
                useCurve = true,
                controlA = plan.ControlA,
                controlB = plan.ControlB,
                width = plan.Width,
                clearHeight = vehicle.height + vehicle.verticalMargin + 1f,
                roadThickness = 0.2f,
                roadSegments = 24
            };
            return MapR5TraversalProbe.CheckEdge(plan.Start, plan.End,
                virtualEdge, vehicle, obstacleLayers, stepMeters);
        }
    }
}
