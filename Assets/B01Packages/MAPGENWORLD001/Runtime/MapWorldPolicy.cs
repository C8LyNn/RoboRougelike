using System;
using B01.MAPGEN001;
using UnityEngine;

namespace B01.MAPGENWORLD001
{
    /// <summary>Pure source-level preflight; never claims actual PhysX swept-path safety.</summary>
    public static class MapWorldPolicy
    {
        public const string Version = "1.0.0";
        public static bool TryPlan(MapWorldRequest request, out MapBlueprint plan,
            out MapWorldFailure failure)
        {
            plan = null;
            failure = MapWorldFailure.MissingRequest;
            if (request == null) return false;
            failure = MapWorldFailure.UnmeasuredEnvelope;
            if (!request.EnvelopeMeasured) return false;
            failure = MapWorldFailure.InvalidClearance;
            if (float.IsNaN(request.ClearancePerSideMeters) ||
                float.IsInfinity(request.ClearancePerSideMeters) ||
                request.ClearancePerSideMeters < 0.25f) return false;
            failure = MapWorldFailure.RejectedGeneratorConfig;
            try { plan = MapGenPlanner.Plan(request.Recipe, request.Seed, request.Config, request.Vehicle); }
            catch (ArgumentException) { return false; }
            failure = MapWorldFailure.InsufficientMainRouteClearance;
            if (plan.GuaranteedClearanceWidth + 0.00001f <
                request.Vehicle.Width + 2f * request.ClearancePerSideMeters) return false;
            failure = MapWorldFailure.InvalidWorldEndpoints;
            if (string.IsNullOrEmpty(plan.GeometryHash) ||
                !InsideXZ(plan.Bounds, plan.Start) || !InsideXZ(plan.Bounds, plan.Finish) ||
                Vector3.Distance(plan.Start, plan.Finish) <= 1f) return false;
            foreach (var p in plan.Checkpoints) if (!InsideXZ(plan.Bounds, p)) return false;
            foreach (var p in plan.EnemySpawns) if (!InsideXZ(plan.Bounds, p)) return false;
            failure = MapWorldFailure.None;
            return true;
        }

        private static bool InsideXZ(Bounds bounds, Vector3 p)
        {
            return FloatValid(p.x) && FloatValid(p.y) && FloatValid(p.z) &&
                   p.x >= bounds.min.x && p.x <= bounds.max.x &&
                   p.z >= bounds.min.z && p.z <= bounds.max.z;
        }
        private static bool FloatValid(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
    }
}
