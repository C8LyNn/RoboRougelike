using System;
using UnityEngine;

namespace RoboRougelike.MapSystemR4
{
    // Junction manoeuvres are separate from individual edge curvature.
    // A positive result means a sampled geometric candidate, NOT a proven drive with wheel physics.
    public struct MapR6JunctionPlan
    {
        public string IncomingEdgeId;
        public string OutgoingEdgeId;
        public string JunctionNodeId;
        public Vector3 Start;
        public Vector3 End;
        public Vector3 ControlA;
        public Vector3 ControlB;
        public float Width;
        public float MinMeasuredRadius;
        public float MaxMeasuredSlopeDeg;
        public float LengthMeters;
        public bool Valid;
        public string Reason;

        public Vector3 Evaluate(float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            Vector3 p = u * u * u * Start + 3f * u * u * t * ControlA +
                        3f * u * t * t * ControlB + t * t * t * End;
            p.y = Mathf.Lerp(Start.y, End.y, t * t * (3f - 2f * t));
            return p;
        }
    }

    public static class MapR6JunctionPlanner
    {
        public static MapR6JunctionPlan Plan(
            MapR4MapData map, string incomingId, string outgoingId,
            string nodeId, MapR4VehicleProfile vehicle)
        {
            var result = new MapR6JunctionPlan
            {
                IncomingEdgeId = incomingId, OutgoingEdgeId = outgoingId,
                JunctionNodeId = nodeId, Reason = "Unknown failure", Valid = false
            };
            if (map == null || vehicle == null || string.IsNullOrWhiteSpace(nodeId) ||
                string.IsNullOrWhiteSpace(incomingId) || string.IsNullOrWhiteSpace(outgoingId) ||
                incomingId == outgoingId || map.edges == null || map.nodes == null ||
                vehicle.width <= 0f || vehicle.length <= 0f || vehicle.minTurnRadius < 0f)
            {
                result.Reason = "Invalid input / U-turn via identical edge unsupported";
                return result;
            }
            MapR4NodeData junction = null;
            MapR4EdgeData incoming = null, outgoing = null;
            var nodes = new System.Collections.Generic.Dictionary<string, MapR4NodeData>(StringComparer.Ordinal);
            foreach (var node in map.nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.id)) continue;
                nodes[node.id] = node;
                if (node.id == nodeId) junction = node;
            }
            foreach (var edge in map.edges)
            {
                if (edge == null) continue;
                if (edge.id == incomingId) incoming = edge;
                if (edge.id == outgoingId) outgoing = edge;
            }
            if (junction == null || incoming == null || outgoing == null ||
                !Touches(incoming, nodeId) || !Touches(outgoing, nodeId) ||
                !nodes.TryGetValue(incoming.fromNode, out var ia) ||
                !nodes.TryGetValue(incoming.toNode, out var ib) ||
                !nodes.TryGetValue(outgoing.fromNode, out var oa) ||
                !nodes.TryGetValue(outgoing.toNode, out var ob))
            {
                result.Reason = "Unknown or disconnected junction / edge";
                return result;
            }
            if (incoming.width < vehicle.width + 2f * vehicle.sideMargin ||
                outgoing.width < vehicle.width + 2f * vehicle.sideMargin ||
                incoming.clearHeight < vehicle.height + vehicle.verticalMargin ||
                outgoing.clearHeight < vehicle.height + vehicle.verticalMargin)
            {
                result.Reason = "Vehicle envelope exceeds edge clearance";
                return result;
            }
            float incomingLength = MapR5RoadMath.ApproximateLength(ia.position, ib.position, incoming);
            float outgoingLength = MapR5RoadMath.ApproximateLength(oa.position, ob.position, outgoing);
            if (incomingLength < 0.01f || outgoingLength < 0.01f || junction.padWidth < 2f)
            {
                result.Reason = "Degenerate edges or junction pad";
                return result;
            }
            float trimMeters = junction.padWidth * 0.43f;
            float inT = Mathf.Clamp(trimMeters / incomingLength, 0.03f, 0.38f);
            float outT = Mathf.Clamp(trimMeters / outgoingLength, 0.03f, 0.38f);
            bool inForward = incoming.toNode == nodeId;
            bool outForward = outgoing.fromNode == nodeId;
            float entryT = inForward ? 1f - inT : inT;
            float exitT = outForward ? outT : 1f - outT;
            Vector3 start = MapR5RoadMath.Evaluate(ia.position, ib.position, incoming, entryT);
            Vector3 end = MapR5RoadMath.Evaluate(oa.position, ob.position, outgoing, exitT);
            Vector3 headingIn = MapR5RoadMath.TangentXZ(ia.position, ib.position, incoming, entryT) * (inForward ? 1f : -1f);
            Vector3 headingOut = MapR5RoadMath.TangentXZ(oa.position, ob.position, outgoing, exitT) * (outForward ? 1f : -1f);
            if (headingIn == Vector3.zero || headingOut == Vector3.zero ||
                Vector3.Angle(headingIn, headingOut) > 170f ||
                Vector3.Distance(start, end) < 0.5f)
            {
                result.Reason = "Degenerate or near-reversing turn";
                return result;
            }
            float distance = Vector3.Distance(start, end);
            float handle = Mathf.Clamp(distance * 0.48f, 0.5f, junction.padWidth * 0.65f);
            Vector3 c1 = start + headingIn * handle;
            Vector3 c2 = end - headingOut * handle;
            c1.y = start.y;
            c2.y = end.y;
            var surrogate = new MapR4EdgeData
            {
                id = "junction_" + nodeId,
                width = Mathf.Min(incoming.width, outgoing.width),
                clearHeight = Mathf.Min(incoming.clearHeight, outgoing.clearHeight),
                roadThickness = 0.18f, roadSegments = 32,
                useCurve = true, controlA = c1, controlB = c2
            };
            result.Start = start; result.End = end; result.ControlA = c1; result.ControlB = c2;
            result.Width = surrogate.width;
            result.MinMeasuredRadius = MapR5RoadMath.MeasuredMinRadiusXZ(start, end, surrogate, 96);
            result.MaxMeasuredSlopeDeg = MapR5RoadMath.MeasuredMaxSlopeDeg(start, end, surrogate, 96);
            result.LengthMeters = MapR5RoadMath.ApproximateLength(start, end, surrogate, 96);
            if (result.MaxMeasuredSlopeDeg > vehicle.maxSlopeDeg + 0.001f)
            {
                result.Reason = "Junction exceeds vehicle slope limit";
                return result;
            }
            if (result.MinMeasuredRadius + 0.05f < vehicle.minTurnRadius)
            {
                result.Reason = "Junction turn radius below vehicle limit";
                return result;
            }
            // Explicitly disallow a join with a very sharp entrance/exit in the reference planner.
            // The sampled curvature filter above still does not guarantee a vehicle can drive it.
            result.Valid = true;
            result.Reason = "Sampled junction geometry acceptable; driving NOT verified";
            return result;
        }

        private static bool Touches(MapR4EdgeData e, string nodeId)
        {
            return e.fromNode == nodeId || e.toNode == nodeId;
        }
    }
}
