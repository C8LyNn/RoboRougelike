using UnityEngine;

namespace B01.BUILD001
{
    public enum BuildHitResult { Valid, NoOwner, DecorativeSurface, OutsidePatch, ObliqueHit }

    /// <summary>
    /// A real ray/Collider hit is not automatically a mount. Caller must first
    /// resolve hit.collider to ONE authoritative logical part ID. This adapter
    /// checks that the hit lies on a structural patch, not tire/barrel visual art.
    /// </summary>
    public static class BuildSocketRaycast
    {
        public static BuildHitResult ValidateHit(RaycastHit hit, Transform authoritativeRoot,
            BuildDefinition owner, BuildPose pose, Vector3 gridWorldOrigin,
            out Vector3Int worldFace, float maxSurfaceGapMeters = .075f)
        {
            worldFace = Vector3Int.zero;
            if (owner == null || authoritativeRoot == null || hit.collider == null ||
                !hit.collider.transform.IsChildOf(authoritativeRoot) ||
                !BuildSocketGeometry.Valid(owner, out _))
                return BuildHitResult.NoOwner;
            if (!owner.SupportsOtherParts || owner.Role == BuildRole.Wheel ||
                owner.Role == BuildRole.Gun)
                return BuildHitResult.DecorativeSurface;

            bool oblique = false;
            foreach (var s in owner.Sockets)
            {
                if (s.Kind != BuildSocketKind.Structural &&
                    s.Kind != BuildSocketKind.Axle) continue;
                var normal = BuildSocketGeometry.Rotate(s.Face, pose.QuarterTurns);
                if (Vector3.Dot(hit.normal, (Vector3)normal) < .90f)
                { oblique = true; continue; }

                Vector3 u = s.Face.x != 0 ? Vector3.up : Vector3.right;
                Vector3 v = Vector3.Cross((Vector3)s.Face,u);
                var rot = BuildSocketGeometry.Orientation(pose.QuarterTurns);
                Vector3 centre = gridWorldOrigin + BuildSocketGeometry.GridMetres *
                    (pose.Anchor + rot * ((Vector3)s.Cell + .5f*(Vector3)s.Face +
                        u*s.Offset.x + v*s.Offset.y));
                Vector3 delta=hit.point-centre;
                if(Mathf.Abs(Vector3.Dot(delta,(Vector3)normal))>maxSurfaceGapMeters) continue;
                Vector3 au=rot*u,av=rot*v;
                if(Mathf.Abs(Vector3.Dot(delta,au))>
                       s.HalfSize.x*BuildSocketGeometry.GridMetres+.0001f ||
                   Mathf.Abs(Vector3.Dot(delta,av))>
                       s.HalfSize.y*BuildSocketGeometry.GridMetres+.0001f)
                    continue;
                worldFace=normal;
                return BuildHitResult.Valid;
            }
            return oblique ? BuildHitResult.ObliqueHit : BuildHitResult.OutsidePatch;
        }
    }
}
