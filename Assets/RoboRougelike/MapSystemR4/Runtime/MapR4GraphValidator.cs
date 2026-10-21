using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.MapSystemR4
{
    // Fast G0/G1a logical filter only. NOT a swept-body or PhysX traversal proof.
    public static class MapR4GraphValidator
    {
        public static List<string> Validate(MapR4MapData map)
        {
            var errors = new List<string>();
            if (map == null) { errors.Add("Map is null"); return errors; }
            if (string.IsNullOrWhiteSpace(map.mapId)) errors.Add("Missing mapId");
            if (map.nodes == null || map.nodes.Length == 0) errors.Add("No nodes");
            if (map.edges == null) errors.Add("No edges collection");
            if (map.anchors == null) errors.Add("No anchors collection");
            if (map.states == null || map.states.Length == 0) errors.Add("No states");
            if (errors.Count != 0) return errors;
            var nodes = new HashSet<string>(StringComparer.Ordinal);
            var edges = new HashSet<string>(StringComparer.Ordinal);
            var anchors = new HashSet<string>(StringComparer.Ordinal);
            var states = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in map.nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.id) || !nodes.Add(node.id))
                    errors.Add("Duplicate / empty node ID");
                if (node != null && node.padWidth <= 0f) errors.Add("Invalid pad width: " + node.id);
            }
            foreach (var edge in map.edges)
            {
                if (edge == null) { errors.Add("Null edge"); continue; }
                if (string.IsNullOrWhiteSpace(edge.id) || !edges.Add(edge.id)) errors.Add("Duplicate / empty edge ID");
                if (!nodes.Contains(edge.fromNode) || !nodes.Contains(edge.toNode) || edge.fromNode == edge.toNode)
                    errors.Add("Invalid endpoints: " + edge.id);
                if (edge.width <= 0f || edge.clearHeight <= 0f || edge.availableTurnRadius < 0f || edge.declaredSlopeDeg < 0f)
                    errors.Add("Invalid clearance / slope: " + edge.id);
                if (edge.roadSegments < 2 || edge.roadSegments > 256 || edge.roadThickness <= 0f)
                    errors.Add("Invalid road tessellation / thickness: " + edge.id);
                if (nodes.Contains(edge.fromNode) && nodes.Contains(edge.toNode))
                {
                    // Geometry is checked after node table is constructed, below.
                }
            }
            var positions = new Dictionary<string, Vector3>(StringComparer.Ordinal);
            foreach (var node in map.nodes)
                if (node != null && !string.IsNullOrWhiteSpace(node.id)) positions[node.id] = node.position;
            foreach (var edge in map.edges)
            {
                if (edge == null || !positions.TryGetValue(edge.fromNode ?? "", out var from) ||
                    !positions.TryGetValue(edge.toNode ?? "", out var to)) continue;
                if (new Vector2(to.x - from.x, to.z - from.z).sqrMagnitude < 0.0001f)
                {
                    errors.Add("Zero horizontal road: " + edge.id); continue;
                }
                float actualSlope = MapR5RoadMath.MeasuredMaxSlopeDeg(from, to, edge);
                if (actualSlope > edge.declaredSlopeDeg + 0.6f)
                    errors.Add("Measured road slope exceeds declared maximum: " + edge.id);
                if (MapR5RoadMath.TangentXZ(from, to, edge, 0f) == Vector3.zero ||
                    MapR5RoadMath.TangentXZ(from, to, edge, 1f) == Vector3.zero)
                    errors.Add("Degenerate road curve: " + edge.id);
            }
            foreach (var anchor in map.anchors)
            {
                if (anchor == null) { errors.Add("Null anchor"); continue; }
                if (string.IsNullOrWhiteSpace(anchor.id) || !anchors.Add(anchor.id)) errors.Add("Duplicate / empty anchor ID");
                if (!nodes.Contains(anchor.nodeId)) errors.Add("Missing anchor node: " + anchor.id);
            }
            foreach (var state in map.states)
            {
                if (state == null) { errors.Add("Null state"); continue; }
                if (string.IsNullOrWhiteSpace(state.id) || !states.Add(state.id)) errors.Add("Duplicate / empty state ID");
                if (state.disabledEdgeIds == null) continue;
                foreach (var id in state.disabledEdgeIds)
                    if (!edges.Contains(id)) errors.Add("Unknown edge in state " + state.id + ": " + id);
            }
            return errors;
        }

        public static bool IsEdgeTraversable(MapR4EdgeData edge, MapR4VehicleProfile vehicle)
        {
            if (edge == null || vehicle == null) return false;
            if (edge.width < vehicle.width + 2f * vehicle.sideMargin) return false;
            if (edge.clearHeight < vehicle.height + vehicle.verticalMargin) return false;
            if (edge.availableTurnRadius < vehicle.minTurnRadius) return false;
            if (edge.declaredSlopeDeg > vehicle.maxSlopeDeg) return false;
            if (edge.requiredCapabilities != null)
                foreach (var requirement in edge.requiredCapabilities)
                {
                    bool found = false;
                    if (vehicle.capabilities != null)
                        foreach (var capability in vehicle.capabilities)
                            if (string.Equals(capability, requirement, StringComparison.Ordinal)) { found = true; break; }
                    if (!found) return false;
                }
            return true;
        }

        public static bool CanReach(MapR4MapData map, string stateId, string fromNode, string toNode, MapR4VehicleProfile vehicle)
        {
            if (Validate(map).Count > 0 || vehicle == null) return false;
            MapR4StateData state = null;
            foreach (var item in map.states) if (item.id == stateId) { state = item; break; }
            if (state == null) return false;
            var blocked = new HashSet<string>(state.disabledEdgeIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            var positions = new Dictionary<string, Vector3>(StringComparer.Ordinal);
            foreach (var node in map.nodes) positions[node.id] = node.position;
            var adjacency = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var n in map.nodes) adjacency[n.id] = new List<string>();
            if (!adjacency.ContainsKey(fromNode) || !adjacency.ContainsKey(toNode)) return false;
            foreach (var edge in map.edges)
            {
                if (blocked.Contains(edge.id) || !IsEdgeTraversable(edge, vehicle)) continue;
                Vector3 delta = positions[edge.toNode] - positions[edge.fromNode];
                var horizontalDistance = new Vector2(delta.x, delta.z).magnitude;
                var measuredSlope = Mathf.Atan2(Mathf.Abs(delta.y), Mathf.Max(0.0001f, horizontalDistance)) * Mathf.Rad2Deg;
                if (measuredSlope > vehicle.maxSlopeDeg) continue;
                if (MapR5RoadMath.MeasuredMaxSlopeDeg(positions[edge.fromNode], positions[edge.toNode], edge) > vehicle.maxSlopeDeg) continue;
                if (MapR5RoadMath.MeasuredMinRadiusXZ(positions[edge.fromNode], positions[edge.toNode], edge) < vehicle.minTurnRadius) continue;
                adjacency[edge.fromNode].Add(edge.toNode);
                adjacency[edge.toNode].Add(edge.fromNode);
            }
            var queue = new Queue<string>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            queue.Enqueue(fromNode);
            visited.Add(fromNode);
            while (queue.Count != 0)
            {
                var cur = queue.Dequeue();
                if (cur == toNode) return true;
                foreach (var next in adjacency[cur])
                    if (visited.Add(next)) queue.Enqueue(next);
            }
            return false;
        }
    }
}
