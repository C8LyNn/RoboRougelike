using System.Collections.Generic;
using UnityEngine;

namespace B01.BUILD001
{
    public enum BuildHitResult { Valid, NoOwner, DecorativeSurface, OutsidePatch, ObliqueHit }

    /// <summary>An explicit, per-robot collider-to-socket authorization registry.
    /// A decorative child, even under the same root, is NEVER implicitly authorized.</summary>
    public sealed class BuildSocketRegistry
    {
        private sealed class Binding
        {
            public string PartId;
            public Transform Root;
            public BuildDefinition Definition;
            public int Index;
        }
        private readonly Dictionary<Collider, Binding> bindings =
            new Dictionary<Collider, Binding>();

        public bool Register(Collider collider, Transform root, string partId,
            BuildDefinition definition, int socketIndex)
        {
            if (collider == null || root == null || string.IsNullOrEmpty(partId) ||
                definition == null || !BuildSocketGeometry.Valid(definition, out _) ||
                socketIndex < 0 || socketIndex >= definition.Sockets.Length ||
                !collider.transform.IsChildOf(root) || !collider.enabled ||
                !definition.SupportsOtherParts || definition.Role == BuildRole.Wheel ||
                definition.Role == BuildRole.Gun) return false;
            var marker = collider.GetComponent<BuildMountSurface>();
            if (marker == null || marker.PartInstanceId != partId ||
                marker.SocketIndex != socketIndex) return false;
            var socket = definition.Sockets[socketIndex];
            if (socket.Kind != BuildSocketKind.Structural &&
                socket.Kind != BuildSocketKind.Axle) return false;
            if (bindings.ContainsKey(collider)) return false; // No remapping authority.
            bindings.Add(collider, new Binding { PartId = partId, Root = root,
                Definition = definition, Index = socketIndex });
            return true;
        }

        public void Unregister(Collider collider) { if (collider != null) bindings.Remove(collider); }
        public void Clear() { bindings.Clear(); }

        public bool TryGet(Collider collider, Transform root, string partId,
            BuildDefinition definition, out int socketIndex)
        {
            socketIndex = -1;
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy ||
                !bindings.TryGetValue(collider, out var binding) || binding.Root != root ||
                binding.PartId != partId || binding.Definition != definition) return false;
            socketIndex = binding.Index;
            return true;
        }
    }

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
                !BuildSocketGeometry.Valid(owner, out _))
                return BuildHitResult.NoOwner;
            if (!owner.SupportsOtherParts || owner.Role == BuildRole.Wheel ||
                owner.Role == BuildRole.Gun) return BuildHitResult.DecorativeSurface;
            // Compatibility for standalone R1.2 root primitive fixtures ONLY:
            // never trust an arbitrary descendant collider by ancestry/proximity.
            if (hit.collider != authoritativeRoot.GetComponent<Collider>())
                return BuildHitResult.DecorativeSurface;
            return ValidateAuthorizedSocket(hit, owner, pose, gridWorldOrigin,
                out worldFace, -1, maxSurfaceGapMeters);
        }

        /// <summary>Strict production API. Exact collider, live part ID and
        /// registered socket ID must all match before geometry is evaluated.</summary>
        public static BuildHitResult ValidateHit(RaycastHit hit, Transform authoritativeRoot,
            string partInstanceId, BuildDefinition owner, BuildPose pose,
            Vector3 gridWorldOrigin, BuildSocketRegistry registry,
            out Vector3Int worldFace, float maxSurfaceGapMeters = .075f)
        {
            worldFace = Vector3Int.zero;
            if (hit.collider == null || authoritativeRoot == null || owner == null ||
                string.IsNullOrEmpty(partInstanceId) || registry == null ||
                !BuildSocketGeometry.Valid(owner, out _)) return BuildHitResult.NoOwner;
            if (!registry.TryGet(hit.collider, authoritativeRoot, partInstanceId,
                owner, out var socketIndex)) return BuildHitResult.DecorativeSurface;
            return ValidateAuthorizedSocket(hit, owner, pose, gridWorldOrigin,
                out worldFace, socketIndex, maxSurfaceGapMeters);
        }

        private static BuildHitResult ValidateAuthorizedSocket(RaycastHit hit,
            BuildDefinition owner, BuildPose pose, Vector3 gridWorldOrigin,
            out Vector3Int worldFace, int onlyIndex, float maxSurfaceGapMeters)
        {
            worldFace = Vector3Int.zero;
            bool oblique = false;
            for (int i = 0; i < owner.Sockets.Length; i++)
            {
                if (onlyIndex >= 0 && i != onlyIndex) continue;
                var s = owner.Sockets[i];
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
