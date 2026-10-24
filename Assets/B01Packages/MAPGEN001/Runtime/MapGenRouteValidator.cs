using System;
using System.Collections.Generic;
using UnityEngine;

namespace B01.MAPGEN001
{
    /// <summary>
    /// Conservative XZ clearance/routing preflight, not a PhysX driving certificate.
    /// Checks a rectangular, forward-facing vehicle footprint, floor support at all
    /// corners, obstructions, a four-neighbour BFS and centreline bend radii.
    /// </summary>
    public static class MapGenRouteValidator
    {
        public struct Report
        {
            public bool Passed;
            public string Code;
            public int ExploredCells;
            public float MinimumRadius;
            public override string ToString() =>
                Code + " explored=" + ExploredCells + " min_radius=" +
                MinimumRadius.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        }

        private const float GridStep = 1f;
        private const float FootprintMargin = .25f;
        private const float SpineStep = .5f;
        private static readonly int[] XStep = { 1, -1, 0, 0 };
        private static readonly int[] ZStep = { 0, 0, 1, -1 };

        public static Report Validate(MapBlueprint plan, MapVehicleProfile vehicle)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (vehicle.Width <= 0 || vehicle.Length <= 0 ||
                !Finite(vehicle.Width) || !Finite(vehicle.Length) ||
                !Finite(vehicle.MinTurnRadius) || vehicle.MinTurnRadius < 0)
                return Fail("INVALID_PROFILE", 0, 0);
            if (plan.Elements.Count == 0) return Fail("EMPTY_MAP", 0, 0);

            float maxSlope = vehicle.MaxSlopeDegrees > 0f ? vehicle.MaxSlopeDegrees : 12f;
            if (!Finite(maxSlope) || maxSlope < 0f || maxSlope > 45f)
                return Fail("INVALID_SLOPE_LIMIT", 0, 0);
            foreach (MapElement e in plan.Elements)
            {
                if (e.Kind != MapElementKind.Ramp) continue;
                float pitch = e.Rotation.eulerAngles.x;
                pitch = Mathf.Min(pitch, 360f - pitch);
                if (pitch > maxSlope + .001f)
                    return Fail("RAMP_EXCEEDS_PROFILE_SLOPE", 0, 0);
            }

            var spine = new List<Vector3> { plan.Start };
            if (plan.Recipe == MapRecipe.ObstacleCourse)
                spine.AddRange(plan.Checkpoints);
            spine.Add(plan.Finish);
            float minRadius = float.PositiveInfinity;
            for (int i = 1; i + 1 < spine.Count; i++)
            {
                Vector2 a = new Vector2(spine[i - 1].x, spine[i - 1].z);
                Vector2 b = new Vector2(spine[i].x, spine[i].z);
                Vector2 c = new Vector2(spine[i + 1].x, spine[i + 1].z);
                float ab = Vector2.Distance(a, b);
                float bc = Vector2.Distance(b, c);
                float ac = Vector2.Distance(a, c);
                if (ab < .01f || bc < .01f) return Fail("DEGENERATE_WAYPOINTS", 0, 0);
                float cross = Mathf.Abs((b.x - a.x) * (c.y - b.y) -
                                        (b.y - a.y) * (c.x - b.x));
                if (cross > .0001f)
                {
                    float radius = ab * bc * ac / (2f * cross);
                    minRadius = Mathf.Min(minRadius, radius);
                    if (radius + .001f < vehicle.MinTurnRadius)
                        return Fail("TURN_RADIUS_TOO_SMALL", 0, radius);
                }
            }

            // The clearance certificate is required for an actual continuous
            // chosen route, not merely a disconnected collection of free cells.
            for (int i = 1; i < spine.Count; i++)
            {
                Vector3 a = spine[i - 1], b = spine[i];
                int samples = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / SpineStep));
                for (int j = 0; j <= samples; j++)
                {
                    Vector3 pt = Vector3.Lerp(a, b, (float)j / samples);
                    if (!IsFree(plan, vehicle, pt.x, pt.z))
                        return Fail("SPINE_FOOTPRINT_BLOCKED", 0, minRadius);
                }
            }

            int minX = Mathf.FloorToInt(plan.Bounds.min.x);
            int minZ = Mathf.FloorToInt(plan.Bounds.min.z);
            int width = Mathf.CeilToInt(plan.Bounds.max.x) - minX + 1;
            int height = Mathf.CeilToInt(plan.Bounds.max.z) - minZ + 1;
            if (width < 1 || height < 1 || (long)width * height > 60000)
                return Fail("GRID_OUT_OF_RANGE", 0, minRadius);

            int startX = Mathf.RoundToInt(plan.Start.x) - minX;
            int startZ = Mathf.RoundToInt(plan.Start.z) - minZ;
            int goalX = Mathf.RoundToInt(plan.Finish.x) - minX;
            int goalZ = Mathf.RoundToInt(plan.Finish.z) - minZ;
            if (!InGrid(startX, startZ, width, height) ||
                !InGrid(goalX, goalZ, width, height))
                return Fail("START_OR_FINISH_OUTSIDE_GRID", 0, minRadius);

            int n = width * height;
            var visited = new bool[n];
            var cachedWalkable = new byte[n]; // 0 untested, 1 clear, 2 blocked
            var queue = new int[n];
            int head = 0, tail = 0, sampledCells = 0;
            int first = startZ * width + startX;
            int last = goalZ * width + goalX;
            if (!Walkable(first, width, minX, minZ, plan, vehicle, cachedWalkable, ref sampledCells))
                return Fail("START_BLOCKED", sampledCells, minRadius);
            if (!Walkable(last, width, minX, minZ, plan, vehicle, cachedWalkable, ref sampledCells))
                return Fail("FINISH_BLOCKED", sampledCells, minRadius);
            visited[first] = true;
            queue[tail++] = first;
            bool reachable = false;
            while (head < tail)
            {
                int node = queue[head++];
                if (node == last) { reachable = true; break; }
                int x = node % width, z = node / width;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + XStep[d], nz = z + ZStep[d];
                    if (!InGrid(nx, nz, width, height)) continue;
                    int index = nz * width + nx;
                    if (visited[index]) continue;
                    if (!Walkable(index, width, minX, minZ, plan, vehicle,
                                  cachedWalkable, ref sampledCells)) continue;
                    visited[index] = true;
                    queue[tail++] = index;
                }
            }
            if (!reachable) return Fail("BFS_NO_CONNECTED_ROUTE", sampledCells, minRadius);
            return new Report { Passed = true, Code = "BFS_SPINE_CLEARANCE_OK",
                ExploredCells = sampledCells, MinimumRadius = minRadius };
        }

        private static bool Walkable(int index, int width, int minX, int minZ,
            MapBlueprint plan, MapVehicleProfile vehicle, byte[] cache, ref int sampled)
        {
            if (cache[index] == 0)
            {
                float x = minX + index % width;
                float z = minZ + index / width;
                cache[index] = (byte)(IsFree(plan, vehicle, x, z) ? 1 : 2);
                sampled++;
            }
            return cache[index] == 1;
        }

        private static bool InGrid(int x, int z, int width, int height) =>
            x >= 0 && z >= 0 && x < width && z < height;

        private static bool IsFree(MapBlueprint plan, MapVehicleProfile vehicle, float x, float z)
        {
            float halfW = vehicle.Width * .5f + FootprintMargin;
            float halfL = vehicle.Length * .5f + FootprintMargin;
            // Four-corner floor support rejects cliff edges and open floor seams.
            if (!FloorAt(plan, x - halfW, z - halfL) ||
                !FloorAt(plan, x - halfW, z + halfL) ||
                !FloorAt(plan, x + halfW, z - halfL) ||
                !FloorAt(plan, x + halfW, z + halfL)) return false;
            foreach (MapElement e in plan.Elements)
            {
                if (e.Kind == MapElementKind.Floor) continue;
                // Treat even drivable ramps as obstructions when certifying a
                // conservative flat bypass; optional ramp traversals need PhysX QA.
                float halfZ = e.Size.z * .5f;
                if (e.Kind == MapElementKind.Ramp)
                {
                    float a = e.Rotation.eulerAngles.x * Mathf.Deg2Rad;
                    halfZ = Mathf.Abs(Mathf.Cos(a)) * e.Size.z * .5f +
                            Mathf.Abs(Mathf.Sin(a)) * e.Size.y * .5f;
                }
                if (Mathf.Abs(e.Position.x - x) < e.Size.x * .5f + halfW &&
                    Mathf.Abs(e.Position.z - z) < halfZ + halfL) return false;
            }
            return true;
        }

        private static bool FloorAt(MapBlueprint plan, float x, float z)
        {
            foreach (MapElement e in plan.Elements)
                if (e.Kind == MapElementKind.Floor &&
                    Mathf.Abs(e.Position.x - x) <= e.Size.x * .5f &&
                    Mathf.Abs(e.Position.z - z) <= e.Size.z * .5f)
                    return true;
            return false;
        }

        private static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
        private static Report Fail(string code, int explored, float radius) =>
            new Report { Passed = false, Code = code,
                ExploredCells = explored, MinimumRadius = radius };
    }
}
