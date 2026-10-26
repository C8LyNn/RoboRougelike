using System;
using System.Collections.Generic;
using UnityEngine;

namespace B01.BUILD001
{
    /// <summary>
    /// Host-owned physical truth at the moment of commit. Include every actual
    /// collidable body in the candidate and all relevant nearby existing parts.
    /// Never synthesize an empty list to bypass volume QA.
    /// </summary>
    public sealed class BuildPhysicsEvidence
    {
        public readonly IReadOnlyList<Collider> Candidate;
        public readonly IReadOnlyList<Collider> Neighbors;
        public readonly bool CompleteBroadphase;
        public BuildPhysicsEvidence(IReadOnlyList<Collider> candidate,
            IReadOnlyList<Collider> neighbors, bool completeBroadphase)
        { Candidate = candidate; Neighbors = neighbors; CompleteBroadphase = completeBroadphase; }
    }

    public static class BuildVolumeValidator
    {
        public const float MaxPenetrationMeters = 0.005f;

        private static bool Supported(Collider c)
        {
            if (c == null || !c.enabled || !c.gameObject.activeInHierarchy || c.isTrigger)
                return false;
            if (c is BoxCollider || c is SphereCollider || c is CapsuleCollider) return true;
            var mesh = c as MeshCollider;
            return mesh != null && mesh.convex;
        }

        /// <summary>
        /// All colliders must be actively certified; missing/unsupported/bypassed
        /// evidence means PHYSICS_UNVERIFIED, never an inferred valid placement.
        /// </summary>
        public static BuildPlacementResult Check(BuildPhysicsEvidence input)
        {
            if (input == null || !input.CompleteBroadphase ||
                input.Candidate == null || input.Neighbors == null ||
                input.Candidate.Count == 0)
                return BuildPlacementResult.PhysicsUnverified;
            foreach (var candidate in input.Candidate)
                if (!Supported(candidate)) return BuildPlacementResult.PhysicsUnverified;
            foreach (var other in input.Neighbors)
                if (!Supported(other)) return BuildPlacementResult.PhysicsUnverified;
            Physics.SyncTransforms();
            foreach (var candidate in input.Candidate)
            foreach (var other in input.Neighbors)
            {
                if (candidate == other) return BuildPlacementResult.PhysicsUnverified;
                // All candidate/member colliders must be catalogued by the caller.
                if (Physics.ComputePenetration(candidate,
                    candidate.transform.position, candidate.transform.rotation,
                    other, other.transform.position, other.transform.rotation,
                    out _, out float depth) && depth > MaxPenetrationMeters)
                    return BuildPlacementResult.ColliderPenetration;
            }
            return BuildPlacementResult.Valid;
        }
    }
}
