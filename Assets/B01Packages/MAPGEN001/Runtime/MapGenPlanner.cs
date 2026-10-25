using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace B01.MAPGEN001
{
    /// <summary>Pure deterministic layout planner; does not modify a Unity scene or gameplay state.</summary>
    public static class MapGenPlanner
    {
        public static MapBlueprint Plan(MapRecipe recipe, int seed, MapGenConfig config, MapVehicleProfile vehicle)
        {
            CheckConfig(recipe, config, vehicle);
            var plan = new MapBlueprint { Recipe = recipe, Seed = seed };
            // Separate per-recipe random streams, so adding an arena rule does not reroll courses.
            var rng = new System.Random(unchecked(seed * 486187739 + (int)recipe * 16777619 + 101));
            if (recipe == MapRecipe.CombatArena) MakeArena(plan, rng, config);
            else MakeCourse(plan, rng, config);
            // Reject unusable generated geometry before building any scene objects.
            var route = MapGenRouteValidator.Validate(plan, vehicle);
            if (!route.Passed)
                throw new ArgumentException("IMPOSSIBLE_GENERATED_ROUTE: " + route.Code);
            plan.GeometryHash = HashLayout(plan);
            plan.Diagnostics = "MAPGEN001/v1 recipe=" + recipe + " seed=" + seed
                + " geometry=" + plan.GeometryHash + " elements=" + plan.Elements.Count
                + " guaranteed_clearance=" + plan.GuaranteedClearanceWidth.ToString("F2", CultureInfo.InvariantCulture)
                + " route=" + route.ToString();
            return plan;
        }

        public static void CheckConfig(MapRecipe recipe, MapGenConfig config, MapVehicleProfile vehicle)
        {
            if (!Enum.IsDefined(typeof(MapRecipe), recipe))
                throw new ArgumentException("Unknown map recipe");
            if (!Finite(vehicle.Width) || !Finite(vehicle.Length) || !Finite(vehicle.MinTurnRadius)
                || vehicle.Width <= 0f || vehicle.Length <= 0f || vehicle.MinTurnRadius < 0f)
                throw new ArgumentException("Invalid vehicle envelope");
            if (!Finite(config.Difficulty) || config.Difficulty < 0f || config.Difficulty > 1f
                || config.ObstacleDensity < 0 || config.ObstacleDensity > 100)
                throw new ArgumentException("Invalid difficulty or obstacle density");
            if (recipe == MapRecipe.ObstacleCourse)
            {
                if (config.Segments < 3 || config.Segments > 5
                    || !Finite(config.LaneWidth) || config.LaneWidth < 10f || config.LaneWidth > 30f)
                    throw new ArgumentException("Course requires 3..5 segments and lane width 10..30m");
                if (vehicle.Width + .5f > config.LaneWidth * .60f - 3f)
                    throw new ArgumentException("IMPOSSIBLE_COURSE_PROFILE: reserved centre clearance cannot fit vehicle");
                if (vehicle.MinTurnRadius > 15f)
                    throw new ArgumentException("IMPOSSIBLE_COURSE_TURN_RADIUS");
            }
            else if (vehicle.Width > 6f || vehicle.Length > 10f)
                throw new ArgumentException("IMPOSSIBLE_ARENA_PROFILE: reserved lanes cannot fit vehicle");
        }

        private static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);

        private static void Add(MapBlueprint p, MapElementKind kind, float x, float y, float z,
            float sx, float sy, float sz, float pitch = 0f)
        {
            p.Elements.Add(new MapElement(kind, new Vector3(x, y, z), new Vector3(sx, sy, sz),
                Quaternion.Euler(pitch, 0f, 0f)));
        }

        private static void MakeArena(MapBlueprint p, System.Random rng, MapGenConfig config)
        {
            // Fixed traversable north/south avenue; generated side covers vary with seed.
            // Spawns are outside all cover cells and behind a clear 12m centre corridor.
            p.Bounds = new Bounds(Vector3.zero, new Vector3(64f, 8f, 64f));
            p.Start = new Vector3(0f, 1.25f, -25f);
            p.Finish = new Vector3(0f, 1.25f, 25f);
            p.EnemySpawns.Add(new Vector3(-17f, 1.25f, 25f));
            p.EnemySpawns.Add(new Vector3(0f, 1.25f, 25f));
            p.EnemySpawns.Add(new Vector3(17f, 1.25f, 25f));
            p.GuaranteedClearanceWidth = 12f;
            Add(p, MapElementKind.Floor, 0, -.45f, 0, 64, .9f, 64);
            Add(p, MapElementKind.Boundary, -32.5f, 1.8f, 0, 1f, 3.6f, 66);
            Add(p, MapElementKind.Boundary, 32.5f, 1.8f, 0, 1f, 3.6f, 66);
            Add(p, MapElementKind.Boundary, 0, 1.8f, -32.5f, 66, 3.6f, 1);
            Add(p, MapElementKind.Boundary, 0, 1.8f, 32.5f, 66, 3.6f, 1);
            for (int ix = -2; ix <= 2; ix++)
            for (int iz = -2; iz <= 2; iz++)
            {
                if (ix == 0) continue; // protected route for both reference vehicle sizes
                if (Math.Abs(iz) == 2 && Math.Abs(ix) == 2) continue; // spawn shoulders
                if (rng.Next(100) > config.ObstacleDensity) continue;
                float scale = 1.8f + (float)rng.NextDouble() * (1.1f + config.Difficulty * .6f);
                float jitter = .45f + config.Difficulty * .8f;
                float dx = ((float)rng.NextDouble() * 2f - 1f) * jitter;
                float dz = ((float)rng.NextDouble() * 2f - 1f) * jitter;
                Add(p, MapElementKind.Cover, ix * 10f + dx, scale * .5f,
                    iz * 10f + dz, scale, scale, scale);
            }
        }

        private static void MakeCourse(MapBlueprint p, System.Random rng, MapGenConfig config)
        {
            float width = config.LaneWidth;
            float[] cx = new float[config.Segments];
            // Narrow 10m lanes cannot absorb the same lateral wander as 30m
            // lanes. Bound each segment's shift by lateral free-space.
            float lateralStep = Mathf.Min(.35f + config.Difficulty * .8f,
                .8f + (width - 10f) * .05f);
            for (int i = 1; i < cx.Length; i++)
                cx[i] = cx[i - 1] + rng.Next(-2, 3) * lateralStep;
            float maxX = cx.Max(x => Mathf.Abs(x));
            p.Bounds = new Bounds(new Vector3(0, 0, config.Segments * 8),
                new Vector3(2 * (maxX + width * .5f + 3), 8, config.Segments * 16f + 5));
            // Start is >= 3.25m from the edge to support the heavy vehicle's full footprint.
            p.Start = new Vector3(cx[0], 1.25f, 5f);
            p.Finish = new Vector3(cx[cx.Length - 1], 1.25f, config.Segments * 16f - 3);
            p.GuaranteedClearanceWidth = width * .60f - 3f; // full free width between side ramps
            for (int i = 0; i < cx.Length; i++)
            {
                float z = 8f + 16f * i;
                Add(p, MapElementKind.Floor, cx[i], -.45f, z, width, .9f, 17f);
                Add(p, MapElementKind.Boundary, cx[i] - width * .5f - .5f, 1f, z, .8f, 2f, 17f);
                Add(p, MapElementKind.Boundary, cx[i] + width * .5f + .5f, 1f, z, .8f, 2f, 17f);
                p.Checkpoints.Add(new Vector3(cx[i], 1.25f, z + 5f));
                int side = rng.Next(2) == 0 ? -1 : 1;
                if (rng.Next(100) <= config.ObstacleDensity)
                {
                    // Side slalom: never intrudes in guaranteed straight-through centre corridor.
                    Add(p, MapElementKind.Obstacle, cx[i] + side * width * .38f,
                        1f, z, 1.6f + config.Difficulty * .6f, 2f, 2f);
                }
                if (i == 1 || (i > 1 && rng.Next(100) < config.ObstacleDensity / 2))
                {
                    // Optional physical ramp in the opposite side lane; centre stays passable.
                    float pitch = 4f + config.Difficulty * 4f;
                    // Put ramp in the outer side lane, with a small gap to
                    // boundary. This protects the Light 10m-lane route even
                    // when its centreline changes between segments.
                    // A narrow 10m lane cannot host the old 3m wide side
                    // ramp plus a drifting centreline and a complete Light
                    // footprint. Keep the ramp fully in the outside shoulder,
                    // leaving a continuous route for the actual 3x3 probe.
                    // Wide-lane geometry remains backward-compatible.
                    float rampWidth = width <= 12f ? 1.8f : 3f;
                    float rampLateral = width * .5f - rampWidth * .5f - .1f;
                    Add(p, MapElementKind.Ramp, cx[i] - side * rampLateral,
                        .32f, z - 1f, rampWidth, .45f, 7f, pitch);
                }
            }
        }

        public static string HashLayout(MapBlueprint plan)
        {
            var sb = new StringBuilder();
            sb.Append((int)plan.Recipe).Append('|').Append(plan.Elements.Count);
            foreach (var e in plan.Elements)
            {
                sb.Append('|').Append((int)e.Kind);
                Field(sb, e.Position.x); Field(sb, e.Position.y); Field(sb, e.Position.z);
                Field(sb, e.Size.x); Field(sb, e.Size.y); Field(sb, e.Size.z);
                Field(sb, e.Rotation.eulerAngles.x);
            }
            using (var sha = SHA256.Create())
            {
                byte[] data = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                return BitConverter.ToString(data).Replace("-", "").ToLowerInvariant();
            }
        }

        private static void Field(StringBuilder sb, float n) =>
            sb.Append(',').Append(n.ToString("F3", CultureInfo.InvariantCulture));
    }
}
