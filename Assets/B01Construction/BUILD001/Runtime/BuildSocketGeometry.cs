using System;
using System.Collections.Generic;
using UnityEngine;

namespace B01.BUILD001
{
    public enum BuildRole { Core, Armor, Wheel, Gun, Suspension, Device }
    public enum BuildSocketKind { Structural, Axle, WeaponBase, SuspensionBase, MobilityHub }

    /// <summary>
    /// Structural mount patch in local 0.55m grid units. Not a render collider
    /// or RaycastHit: art never automatically creates an attachment point.
    /// A patch centre lies on an exposed face of one occupied cell.
    /// </summary>
    [Serializable]
    public struct BuildSocket
    {
        public Vector3Int Cell;
        public Vector3Int Face;
        public BuildSocketKind Kind;
        public Vector2 Offset;
        public Vector2 HalfSize;

        public BuildSocket(Vector3Int cell, Vector3Int face, BuildSocketKind kind,
            Vector2 halfSize, Vector2 offset = default)
        {
            Cell = cell; Face = face; Kind = kind;
            HalfSize = halfSize; Offset = offset;
        }
    }

    /// <summary>Author-declared physical join between two adjacent occupied cells.
    /// This is a contract; runtime colliders/joints must still be checked on import.</summary>
    public struct BuildCellBond
    {
        public readonly Vector3Int A, B;
        public readonly bool PhysicalJoin;
        public BuildCellBond(Vector3Int a, Vector3Int b, bool physicalJoin = true)
        { A = a; B = b; PhysicalJoin = physicalJoin; }
    }

    public sealed class BuildDefinition
    {
        public string Id;
        public BuildRole Role;
        public Vector3Int[] Cells;
        public BuildSocket[] Sockets;
        public bool SupportsOtherParts;
        public BuildCellBond[] CellBonds;

        public BuildDefinition(string id, BuildRole role, Vector3Int[] cells,
            BuildSocket[] sockets, bool support, BuildCellBond[] cellBonds = null)
        {
            Id = id; Role = role; Cells = cells; Sockets = sockets; SupportsOtherParts = support;
            CellBonds = cellBonds;
        }
    }

    public struct BuildPose
    {
        public Vector3Int Anchor, QuarterTurns;
        public BuildPose(Vector3Int anchor, Vector3Int turns)
        { Anchor = anchor; QuarterTurns = turns; }
    }

    public static class BuildSocketGeometry
    {
        public const float GridMetres = .55f; // Matches current R41PartGeometry.Grid; NO rescaling.
        public const float MinContactAreaCellsSquared = .01f;

        private static int Mod4(int x) => (x % 4 + 4) % 4;
        public static Quaternion Orientation(Vector3Int quarterTurns) =>
            Quaternion.Euler(Mod4(quarterTurns.x) * 90f,
                Mod4(quarterTurns.y) * 90f, Mod4(quarterTurns.z) * 90f);

        public static Vector3Int Rotate(Vector3Int v, Vector3Int quarterTurns)
        {
            var q = Orientation(quarterTurns) * (Vector3)v;
            return new Vector3Int(Mathf.RoundToInt(q.x), Mathf.RoundToInt(q.y),
                Mathf.RoundToInt(q.z));
        }

        public static bool UnitFace(Vector3Int v) => Mathf.Abs(v.x) +
            Mathf.Abs(v.y) + Mathf.Abs(v.z) == 1;

        public static bool Valid(BuildDefinition def, out string reason)
        {
            reason = null;
            if (def == null || string.IsNullOrWhiteSpace(def.Id) ||
                def.Cells == null || def.Cells.Length == 0 ||
                def.Sockets == null || def.Sockets.Length == 0)
            { reason = "MISSING_DEFINITION"; return false; }

            var cells = new HashSet<Vector3Int>();
            foreach (var cell in def.Cells)
                if (!cells.Add(cell)) { reason = "DUPLICATE_OCCUPIED_CELL"; return false; }

            // A single owner cannot bridge separated voxels. All multi-cell
            // ownership requires a declared, orthogonal, physically joined tree.
            if (cells.Count > 1)
            {
                if (def.CellBonds == null || def.CellBonds.Length == 0)
                { reason = "DISCONNECTED_FOOTPRINT"; return false; }
                var adjacency = new Dictionary<Vector3Int, List<Vector3Int>>();
                foreach (var c in cells) adjacency.Add(c, new List<Vector3Int>());
                foreach (var bond in def.CellBonds)
                {
                    if (!bond.PhysicalJoin || !cells.Contains(bond.A) ||
                        !cells.Contains(bond.B) || !UnitFace(bond.A - bond.B))
                    { reason = "UNVERIFIED_PHYSICAL_JOIN"; return false; }
                    adjacency[bond.A].Add(bond.B);
                    adjacency[bond.B].Add(bond.A);
                }
                var reached = new HashSet<Vector3Int>();
                var queue = new Queue<Vector3Int>();
                reached.Add(def.Cells[0]); queue.Enqueue(def.Cells[0]);
                while (queue.Count > 0)
                    foreach (var next in adjacency[queue.Dequeue()])
                        if (reached.Add(next)) queue.Enqueue(next);
                if (reached.Count != cells.Count)
                { reason = "DISCONNECTED_FOOTPRINT"; return false; }
            }

            foreach (var s in def.Sockets)
            {
                if (!cells.Contains(s.Cell) || !UnitFace(s.Face))
                { reason = "SOCKET_OFF_PART"; return false; }
                if (cells.Contains(s.Cell + s.Face))
                { reason = "SOCKET_INSIDE_OWN_FOOTPRINT"; return false; }
                if (!Finite(s.Offset.x) || !Finite(s.Offset.y) ||
                    !Finite(s.HalfSize.x) || !Finite(s.HalfSize.y) ||
                    s.HalfSize.x <= 0f || s.HalfSize.y <= 0f ||
                    Mathf.Abs(s.Offset.x) + s.HalfSize.x > .5001f ||
                    Mathf.Abs(s.Offset.y) + s.HalfSize.y > .5001f)
                { reason = "INVALID_CONTACT_PATCH"; return false; }
            }

            // A wheel can only present an axle. Gun exposes only its base.
            foreach (var s in def.Sockets)
            {
                if (def.Role == BuildRole.Wheel && s.Kind != BuildSocketKind.Axle)
                { reason = "WHEEL_MUST_USE_AXLE"; return false; }
                if (def.Role == BuildRole.Gun && s.Kind != BuildSocketKind.WeaponBase)
                { reason = "GUN_MUST_USE_BASE"; return false; }
            }
            if (def.Role == BuildRole.Wheel && def.SupportsOtherParts)
            { reason = "WHEEL_IS_NOT_STRUCTURAL_SUPPORT"; return false; }
            return true;
        }

        private static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);

        public static IEnumerable<Vector3Int> Occupied(BuildDefinition def, BuildPose pose)
        {
            foreach (var cell in def.Cells)
                yield return pose.Anchor + Rotate(cell, pose.QuarterTurns);
        }

        private static void Tangents(Vector3Int face, out Vector3 u, out Vector3 v)
        {
            // Orthonormal tangent frame; 90 degree rotations keep projected patches axis aligned.
            u = face.x != 0 ? Vector3.up : Vector3.right;
            v = Vector3.Cross((Vector3)face, u);
        }

        private static bool CompatibleKinds(BuildRole supportRole, BuildSocketKind support,
            BuildRole attachedRole, BuildSocketKind attached)
        {
            if (supportRole == BuildRole.Wheel || supportRole == BuildRole.Gun)
                return false; // No tire-side or gun-barrel scaffolding, even with rotated art.

            switch (attachedRole)
            {
                case BuildRole.Wheel:
                    return attached == BuildSocketKind.Axle &&
                        (support == BuildSocketKind.Structural || support == BuildSocketKind.Axle) &&
                        (supportRole == BuildRole.Armor || supportRole == BuildRole.Core ||
                         supportRole == BuildRole.Suspension);
                case BuildRole.Gun:
                    return attached == BuildSocketKind.WeaponBase &&
                        support == BuildSocketKind.Structural;
                case BuildRole.Suspension:
                    return attached == BuildSocketKind.SuspensionBase &&
                        support == BuildSocketKind.Structural;
                default:
                    return attached == BuildSocketKind.Structural &&
                        support == BuildSocketKind.Structural;
            }
        }

        /// <summary>Checks opposing faces, coincident planes and real patch overlap area.</summary>
        public static bool Touches(BuildDefinition support, BuildPose supportPose,
            BuildDefinition candidate, BuildPose candidatePose, out float contactAreaM2)
        {
            contactAreaM2 = 0f;
            if (support == null || candidate == null || !support.SupportsOtherParts)
                return false;
            foreach (var a in support.Sockets)
            foreach (var b in candidate.Sockets)
            {
                if (!CompatibleKinds(support.Role, a.Kind, candidate.Role, b.Kind))
                    continue;
                Vector3Int an = Rotate(a.Face, supportPose.QuarterTurns);
                Vector3Int bn = Rotate(b.Face, candidatePose.QuarterTurns);
                if (an != -bn) continue;

                Tangents(a.Face, out Vector3 au, out Vector3 av);
                Tangents(b.Face, out Vector3 bu, out Vector3 bv);
                Quaternion ar = Orientation(supportPose.QuarterTurns);
                Quaternion br = Orientation(candidatePose.QuarterTurns);
                Vector3 ac = supportPose.Anchor + ar *
                    ((Vector3)a.Cell + (Vector3)a.Face * .5f + au*a.Offset.x + av*a.Offset.y);
                Vector3 bc = candidatePose.Anchor + br *
                    ((Vector3)b.Cell + (Vector3)b.Face * .5f + bu*b.Offset.x + bv*b.Offset.y);
                if (Mathf.Abs(Vector3.Dot(bc-ac, an)) > .0001f) continue;

                Tangents(an, out Vector3 planeU, out Vector3 planeV);
                Vector3 awu = ar * au, awv = ar * av, bwu = br * bu, bwv = br * bv;
                float aw = Mathf.Abs(Vector3.Dot(awu,planeU))*a.HalfSize.x+
                           Mathf.Abs(Vector3.Dot(awv,planeU))*a.HalfSize.y;
                float ah = Mathf.Abs(Vector3.Dot(awu,planeV))*a.HalfSize.x+
                           Mathf.Abs(Vector3.Dot(awv,planeV))*a.HalfSize.y;
                float bw = Mathf.Abs(Vector3.Dot(bwu,planeU))*b.HalfSize.x+
                           Mathf.Abs(Vector3.Dot(bwv,planeU))*b.HalfSize.y;
                float bh = Mathf.Abs(Vector3.Dot(bwu,planeV))*b.HalfSize.x+
                           Mathf.Abs(Vector3.Dot(bwv,planeV))*b.HalfSize.y;
                float du = Mathf.Abs(Vector3.Dot(bc-ac,planeU));
                float dv = Mathf.Abs(Vector3.Dot(bc-ac,planeV));
                float overlapU = Mathf.Max(0f,aw+bw-du);
                float overlapV = Mathf.Max(0f,ah+bh-dv);
                float area = overlapU*overlapV*GridMetres*GridMetres;
                if (area <= GridMetres*GridMetres*MinContactAreaCellsSquared)
                    continue;
                contactAreaM2 = Mathf.Max(contactAreaM2,area);
            }
            return contactAreaM2 > 0f;
        }
    }
}
