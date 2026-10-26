using System;
using System.Collections.Generic;
using UnityEngine;

namespace B01.BUILD001
{
    public enum BuildPlacementResult
    {
        Valid, InvalidDefinition, DuplicatePartId, OccupiedCells, NoSupport,
        NoCompatibleSocket, InvalidCore, OutOfBounds, MultipleCoreRoots,
        CoreDisconnected, DisconnectedFootprint, PhysicsUnverified, ColliderPenetration
    }

    public sealed class BuildInstalledPart
    {
        public readonly string PartId;
        public readonly BuildDefinition Definition;
        public readonly BuildPose Pose;
        public readonly bool Alive;
        public BuildInstalledPart(string id, BuildDefinition definition, BuildPose pose, bool alive = true)
        { PartId=id; Definition=definition; Pose=pose; Alive=alive; }
    }

    /// <summary>
    /// External read-only opt-in policy. Does not modify R3/P12 state, HP,
    /// ammo, inventory, transforms or physics colliders.
    /// </summary>
    public static class BuildPlacementRules
    {
        public static BuildPlacementResult Check(IReadOnlyList<BuildInstalledPart> installed,
            BuildInstalledPart candidate, out string supportsPartId)
        {
            supportsPartId = null;
            if (installed == null || candidate == null || string.IsNullOrEmpty(candidate.PartId))
                return BuildPlacementResult.InvalidDefinition;
            if (!BuildSocketGeometry.Valid(candidate.Definition, out var candidateReason))
                return candidateReason == "DISCONNECTED_FOOTPRINT" ||
                    candidateReason == "UNVERIFIED_PHYSICAL_JOIN" ?
                    BuildPlacementResult.DisconnectedFootprint : BuildPlacementResult.InvalidDefinition;

            var cells = new HashSet<Vector3Int>();
            foreach (var previous in installed)
            {
                if (previous == null || previous.Definition == null)
                    return BuildPlacementResult.InvalidDefinition;
                if (!BuildSocketGeometry.Valid(previous.Definition, out var installedReason))
                    return installedReason == "DISCONNECTED_FOOTPRINT" ||
                        installedReason == "UNVERIFIED_PHYSICAL_JOIN" ?
                        BuildPlacementResult.DisconnectedFootprint : BuildPlacementResult.InvalidDefinition;
                if (previous.PartId == candidate.PartId)
                    return BuildPlacementResult.DuplicatePartId;
                foreach (var cell in BuildSocketGeometry.Occupied(previous.Definition,previous.Pose))
                    cells.Add(cell);
            }
            foreach (var cell in BuildSocketGeometry.Occupied(candidate.Definition,candidate.Pose))
            {
                if (Mathf.Abs(cell.x) > 6 || Mathf.Abs(cell.z) > 7 || cell.y < 0 || cell.y > 5)
                    return BuildPlacementResult.OutOfBounds;
                if (cells.Contains(cell))
                    return BuildPlacementResult.OccupiedCells;
            }
            if (candidate.Definition.Role == BuildRole.Core)
                return BuildPlacementResult.InvalidCore; // Existing one Core is not player-placeable.
            int livingRoots = 0;
            foreach (var previous in installed)
                if (previous.Definition.Role == BuildRole.Core) livingRoots++;
            if (livingRoots > 1) return BuildPlacementResult.MultipleCoreRoots;
            if (installed.Count == 0)
                return BuildPlacementResult.NoSupport;

            // A physically alive fragment with no surviving route to Core must
            // not become a new structural foundation in the garage.
            var coreConnected = Reachable(installed);
            foreach (var existing in installed)
            {
                if (!existing.Alive || !existing.Definition.SupportsOtherParts ||
                    !coreConnected.Contains(existing.PartId)) continue;
                if (BuildSocketGeometry.Touches(existing.Definition, existing.Pose,
                    candidate.Definition, candidate.Pose, out _))
                {
                    supportsPartId = existing.PartId;
                    return BuildPlacementResult.Valid;
                }
            }
            return BuildPlacementResult.NoCompatibleSocket;
        }

        /// <summary>
        /// Only this method can authorize installation. Check() is a pure grid
        /// preview, not a safety or save permission. Null physics evidence fails closed.
        /// </summary>
        public static BuildPlacementResult CheckForInstallation(
            IReadOnlyList<BuildInstalledPart> installed, BuildInstalledPart candidate,
            BuildPhysicsEvidence evidence, out string supportsPartId)
        {
            supportsPartId = null;
            var result = Check(installed, candidate, out var support);
            if (result != BuildPlacementResult.Valid &&
                result != BuildPlacementResult.NoCompatibleSocket) return result;
            int roots = 0;
            if (installed != null)
                foreach (var p in installed)
                    if (p != null && p.Definition != null &&
                        p.Definition.Role == BuildRole.Core) roots++;
            if (roots > 1) return BuildPlacementResult.MultipleCoreRoots;
            if (roots != 1) return BuildPlacementResult.CoreDisconnected;
            if (result == BuildPlacementResult.NoCompatibleSocket)
            {
                // Reveal orphan contact distinctly without granting that island authority.
                foreach (var p in installed)
                    if (p != null && p.Alive && p.Definition != null &&
                        p.Definition.SupportsOtherParts &&
                        BuildSocketGeometry.Touches(p.Definition, p.Pose,
                            candidate.Definition, candidate.Pose, out _) &&
                        !Reachable(installed).Contains(p.PartId))
                        return BuildPlacementResult.CoreDisconnected;
                return result;
            }
            var physical = BuildVolumeValidator.Check(evidence);
            if (physical != BuildPlacementResult.Valid) return physical;
            supportsPartId = support;
            return BuildPlacementResult.Valid;
        }

        /// <summary>
        /// A destroyed support can disconnect the graph. Only a living Core
        /// seeds connected ownership, and wheel/gun items cannot relay connections.
        /// This diagnostic does NOT replace R3/P12 canonical damage/detach authority.
        /// </summary>
        public static HashSet<string> Reachable(IReadOnlyList<BuildInstalledPart> installed)
        {
            var result = new HashSet<string>();
            if (installed == null) return result;
            var queue = new Queue<int>();
            int rootIndex = -1;
            for (int i = 0; i < installed.Count; i++)
            {
                var p = installed[i];
                if (p == null || p.Definition == null ||
                    p.Definition.Role != BuildRole.Core) continue;
                if (rootIndex != -1) return result; // Even dead extra Core quarantines save.
                rootIndex = i;
            }
            if (rootIndex < 0 || !installed[rootIndex].Alive) return result;
            result.Add(installed[rootIndex].PartId);
            queue.Enqueue(rootIndex);
            while (queue.Count != 0)
            {
                var src = installed[queue.Dequeue()];
                for (int i = 0; i < installed.Count; i++)
                {
                    var other = installed[i];
                    if (other == null || !other.Alive || result.Contains(other.PartId))
                        continue;
                    if (BuildSocketGeometry.Touches(src.Definition,src.Pose,
                        other.Definition,other.Pose,out _) && result.Add(other.PartId))
                        queue.Enqueue(i);
                }
            }
            return result;
        }
    }

    /// <summary>Minimal original fixture definitions, not production R3 kind registration.</summary>
    public static class BuildFixtureCatalog
    {
        private static BuildSocket S(Vector3Int face, BuildSocketKind kind,
            Vector3Int cell = default, float half = .48f)
            => new BuildSocket(cell,face,kind,new Vector2(half,half));

        public static BuildDefinition ArmorCube()
        {
            var faces = new[] {Vector3Int.right,Vector3Int.left,Vector3Int.up,
                Vector3Int.down,Vector3Int.forward,Vector3Int.back};
            var sockets=new BuildSocket[faces.Length];
            for(int i=0;i<faces.Length;i++)
                sockets[i]=S(faces[i],BuildSocketKind.Structural);
            return new BuildDefinition("ArmorCube",BuildRole.Armor,
                new[]{Vector3Int.zero},sockets,true);
        }

        public static BuildDefinition Core() =>
            new BuildDefinition("Core",BuildRole.Core,new[]{Vector3Int.zero},
                ArmorCube().Sockets,true);

        public static BuildDefinition Wheel() =>
            new BuildDefinition("WheelHub",BuildRole.Wheel,new[]{Vector3Int.zero},
                new[]{S(Vector3Int.left,BuildSocketKind.Axle,default,.19f)},false);

        public static BuildDefinition Gun() =>
            new BuildDefinition("GunBase",BuildRole.Gun,new[]{Vector3Int.zero},
                new[]{S(Vector3Int.down,BuildSocketKind.WeaponBase,default,.32f)},false);

        public static BuildDefinition Suspension() =>
            new BuildDefinition("Suspension",BuildRole.Suspension,new[]{Vector3Int.zero},
                new[]{S(Vector3Int.left,BuildSocketKind.SuspensionBase),
                      S(Vector3Int.right,BuildSocketKind.Axle,default,.19f)},true);

        public static BuildDefinition Wide2x2()
        {
            // One module ID across 2x2x1, one HP owner, exposed sockets only.
            var cells = new[]{Vector3Int.zero,Vector3Int.right,Vector3Int.forward,
                Vector3Int.right+Vector3Int.forward};
            var sockets=new List<BuildSocket>();
            foreach(var cell in cells)
            foreach(var face in new[]{Vector3Int.left,Vector3Int.right,Vector3Int.up,
                Vector3Int.down,Vector3Int.forward,Vector3Int.back})
            {
                bool internalFace=false;
                foreach(var occupied in cells)
                    if (occupied==cell+face) { internalFace=true; break; }
                if (!internalFace) sockets.Add(S(face,BuildSocketKind.Structural,cell));
            }
            return new BuildDefinition("Wide2x2OneId",BuildRole.Device,
                cells,sockets.ToArray(),true, new[] {
                    new BuildCellBond(Vector3Int.zero, Vector3Int.right),
                    new BuildCellBond(Vector3Int.zero, Vector3Int.forward),
                    new BuildCellBond(Vector3Int.right,
                        Vector3Int.right+Vector3Int.forward)
                });
        }
    }
}
