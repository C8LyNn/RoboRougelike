using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.MapSystemR4
{
    // G1a+: route-level geometric check including turns at junctions.
    // This is NOT equivalent to real vehicle/PhysX driving QA.
    public static class MapR6RouteValidator
    {
        private struct SearchState
        {
            public string Node;
            public string IncomingEdge;
            public SearchState(string node, string incomingEdge)
            {
                Node = node; IncomingEdge = incomingEdge;
            }
            public string Key => Node + "::" + (IncomingEdge ?? "start");
        }

        public static bool CanReachWithTurns(
            MapR4MapData map, string mapStateId, string fromNode, string toNode,
            MapR4VehicleProfile vehicle, out string reason)
        {
            reason = "Unknown error";
            if (map == null || vehicle == null || MapR4GraphValidator.Validate(map).Count > 0)
            {
                reason = "Map/vehicle definition invalid";
                return false;
            }
            if (string.IsNullOrEmpty(fromNode) || string.IsNullOrEmpty(toNode))
            {
                reason = "Missing start/end node";
                return false;
            }
            var nodes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var n in map.nodes) nodes.Add(n.id);
            if (!nodes.Contains(fromNode) || !nodes.Contains(toNode))
            {
                reason = "Unknown start/end node";
                return false;
            }
            if (!TryGetBlocked(map, mapStateId, out var blocked))
            {
                reason = "Unknown map state";
                return false;
            }
            if (fromNode == toNode)
            {
                reason = "Already at target node";
                return true;
            }
            var starts = new Queue<SearchState>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var start = new SearchState(fromNode, null);
            starts.Enqueue(start);
            visited.Add(start.Key);
            while (starts.Count > 0)
            {
                var current = starts.Dequeue();
                foreach (var nextEdge in map.edges)
                {
                    if (blocked.Contains(nextEdge.id) || !Touches(nextEdge, current.Node)) continue;
                    string other = nextEdge.fromNode == current.Node ? nextEdge.toNode : nextEdge.fromNode;
                    if (!IsAllowedEdge(map, nextEdge, vehicle)) continue;
                    if (current.IncomingEdge != null)
                    {
                        var turn = MapR6JunctionPlanner.Plan(map, current.IncomingEdge, nextEdge.id, current.Node, vehicle);
                        if (!turn.Valid) continue;
                    }
                    if (other == toNode)
                    {
                        reason = "Geometric graph-and-junction route found; Rigidbody driving NOT proven";
                        return true;
                    }
                    var next = new SearchState(other, nextEdge.id);
                    if (visited.Add(next.Key)) starts.Enqueue(next);
                }
            }
            reason = "No geometric route meeting edge AND junction turn constraints";
            return false;
        }

        public static bool ValidateExplicitRoute(
            MapR4MapData map, string stateId, string startNode,
            string[] orderedEdges, MapR4VehicleProfile vehicle, out string reason)
        {
            reason = "Invalid route";
            if (map == null || vehicle == null || orderedEdges == null || orderedEdges.Length == 0 ||
                MapR4GraphValidator.Validate(map).Count > 0 || !TryGetBlocked(map,stateId,out var blocked)) return false;
            string at = startNode;
            string prior = null;
            foreach (var edgeId in orderedEdges)
            {
                MapR4EdgeData edge = null;
                foreach (var e in map.edges) if (e.id == edgeId) { edge = e; break; }
                if (edge == null || blocked.Contains(edgeId) || !Touches(edge, at) || !IsAllowedEdge(map, edge, vehicle))
                {
                    reason = "Non-traversable or disconnected edge " + edgeId;
                    return false;
                }
                if (prior != null)
                {
                    var turn = MapR6JunctionPlanner.Plan(map, prior, edgeId, at, vehicle);
                    if (!turn.Valid)
                    {
                        reason = "Junction " + at + ": " + turn.Reason;
                        return false;
                    }
                }
                at = edge.fromNode == at ? edge.toNode : edge.fromNode;
                prior = edgeId;
            }
            reason = "Explicit route passes sampled geometric constraints; not a driving proof; reached " + at;
            return true;
        }

        private static bool TryGetBlocked(MapR4MapData map, string stateId, out HashSet<string> blocked)
        {
            blocked = null;
            if (map == null || map.states == null) return false;
            foreach (var s in map.states)
            {
                if (s == null || s.id != stateId) continue;
                blocked = new HashSet<string>(s.disabledEdgeIds ?? Array.Empty<string>(), StringComparer.Ordinal);
                return true;
            }
            return false;
        }

        private static bool IsAllowedEdge(MapR4MapData map, MapR4EdgeData edge, MapR4VehicleProfile vehicle)
        {
            if (!MapR4GraphValidator.IsEdgeTraversable(edge, vehicle)) return false;
            MapR4NodeData a = null, b = null;
            foreach (var n in map.nodes)
            {
                if (n.id == edge.fromNode) a = n;
                if (n.id == edge.toNode) b = n;
            }
            if (a == null || b == null) return false;
            if (MapR5RoadMath.MeasuredMaxSlopeDeg(a.position,b.position,edge) > vehicle.maxSlopeDeg) return false;
            if (MapR5RoadMath.MeasuredMinRadiusXZ(a.position,b.position,edge) < vehicle.minTurnRadius) return false;
            return true;
        }
        private static bool Touches(MapR4EdgeData edge, string node)
        {
            return edge.fromNode == node || edge.toNode == node;
        }
    }
}
