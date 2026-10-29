// B01-21-MAP-BREAK-001 / optional engine-independent C# world-state component.
// Source ownership: Research/B01-21/P17/BREAK001/**. NOT canonical Unity code.
// No UnityEngine or System.Text.Json dependency. No changes to Assets/**.
// Handles actual original MAPGEN source cover geometry; does NOT touch live colliders.
// Integration/host transforms/real 3D PhysX belong exclusively to Codex.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace B01.MAPBREAK001
{
    public sealed class SourceBox
    {
        public int SourceElementIndex;
        public int CoverOrdinal; // -1 Boundary, 0..N-1 source Cover
        public double X, Y, Z, Width, Height, Depth;
    }

    public sealed class SourceMarker
    {
        public double X, Z;
    }

    public sealed class SourceCoverLayout
    {
        public string SourceLayoutId = "";
        public string OriginalMapGeometryHash = "";
        public int OriginalCoverCount;
        public double EyeY = 1.25;
        public double FloorHalfX = 32.0, FloorHalfZ = 32.0;
        public SourceMarker[] EnemySpawnDescriptors = Array.Empty<SourceMarker>();
        public SourceBox[] Boxes = Array.Empty<SourceBox>();
    }

    public sealed class SourceCell
    {
        public int X, Z;
        public int[] VisibleEnemyIndices = Array.Empty<int>();
        public int VisibleCount { get { return VisibleEnemyIndices.Length; } }
    }

    /// <summary>
    /// Mutable original-source cover state. Exact same source map/hash is mandatory
    /// to apply a replay receipt. Runtime never changes original immutable input.
    /// </summary>
    public sealed class CoverWorld
    {
        private const string Version = "MAP-BREAK-001";
        private const string ReceiptVersion = "B01-21-COVERWORLD-RECEIPT-v1";
        private readonly SourceCoverLayout layout;
        private readonly SourceBox[] covers;
        private readonly SourceBox[] boundaries;
        private readonly bool[] removed;

        public CoverWorld(SourceCoverLayout input, string actualMapGeometryHash)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrEmpty(input.SourceLayoutId)
                || input.OriginalMapGeometryHash.Length != 64
                || !IsHex(input.OriginalMapGeometryHash))
                throw new ArgumentException("INVALID_ORIGINAL_MAP_ID");
            if (!string.Equals(input.OriginalMapGeometryHash, actualMapGeometryHash,
                              StringComparison.Ordinal))
                throw new ArgumentException("STALE_OR_WRONG_MAP_GEOMETRY_HASH");
            if (input.OriginalCoverCount < 1 || input.OriginalCoverCount > 10000
                || input.EnemySpawnDescriptors == null
                || input.EnemySpawnDescriptors.Length != 3
                || input.Boxes == null || input.Boxes.Length < input.OriginalCoverCount)
                throw new ArgumentException("INVALID_ORIGINAL_COVER_CONTRACT");
            if (input.FloorHalfX <= 0 || input.FloorHalfZ <= 0
                || double.IsNaN(input.EyeY))
                throw new ArgumentException("INVALID_ORIGINAL_FLOOR_OR_EYE");

            var sorted = new SourceBox[input.OriginalCoverCount];
            var boundariesList = new List<SourceBox>();
            var seenIndices = new HashSet<int>();
            foreach (SourceBox box in input.Boxes)
            {
                if (box == null || box.SourceElementIndex < 0
                    || !seenIndices.Add(box.SourceElementIndex)
                    || !(box.Width > 0) || !(box.Height > 0) || !(box.Depth > 0)
                    || !Finite(box.X) || !Finite(box.Y) || !Finite(box.Z)
                    || !Finite(box.Width) || !Finite(box.Height) || !Finite(box.Depth))
                    throw new ArgumentException("INVALID_SOURCE_BOX");
                if (box.CoverOrdinal == -1)
                {
                    boundariesList.Add(box);
                }
                else
                {
                    if (box.CoverOrdinal < 0 || box.CoverOrdinal >= sorted.Length
                        || sorted[box.CoverOrdinal] != null)
                        throw new ArgumentException("COVER_ORDINAL_COLLISION_OR_RANGE");
                    sorted[box.CoverOrdinal] = box;
                }
            }
            if (sorted.Any(x => x == null) || boundariesList.Count < 4)
                throw new ArgumentException("MISSING_ORIGINAL_COVER_OR_BOUNDARY");

            layout = input;
            covers = sorted;
            boundaries = boundariesList.ToArray();
            removed = new bool[covers.Length];
        }

        public int CoverCount { get { return covers.Length; } }
        public int DestroyedCount { get { return removed.Count(x => x); } }
        public string SourceLayoutId { get { return layout.SourceLayoutId; } }
        public string OriginalMapGeometryHash { get { return layout.OriginalMapGeometryHash; } }

        public bool IsDestroyed(int originalCoverOrdinal)
        {
            CheckOrdinal(originalCoverOrdinal);
            return removed[originalCoverOrdinal];
        }

        public bool Destroy(int originalCoverOrdinal)
        {
            CheckOrdinal(originalCoverOrdinal);
            if (removed[originalCoverOrdinal]) return false;
            removed[originalCoverOrdinal] = true;
            return true;
        }

        public bool Restore(int originalCoverOrdinal)
        {
            CheckOrdinal(originalCoverOrdinal);
            if (!removed[originalCoverOrdinal]) return false;
            removed[originalCoverOrdinal] = false;
            return true;
        }

        public void RestoreAll() { Array.Clear(removed, 0, removed.Length); }

        public int[] RemainingOriginalCoverOrdinals()
        {
            return Enumerable.Range(0, covers.Length).Where(i => !removed[i]).ToArray();
        }

        // 64-digit content SHA of the SOURCE overlay only, NEVER a MAPGEN geometryHash.
        public string SourceOverlayStateHash
        {
            get
            {
                var csv = string.Join(",", RemainingOriginalCoverOrdinals()
                                         .Select(i => i.ToString(CultureInfo.InvariantCulture)));
                return Sha256Hex(layout.OriginalMapGeometryHash + ":" + Version + ":" + csv);
            }
        }

        public int[] VisibleEnemyDescriptors(double x, double z)
        {
            if (!Finite(x) || !Finite(z)) throw new ArgumentException("INVALID_SIGHT_POINT");
            var visible = new List<int>(3);
            for (int i = 0; i < layout.EnemySpawnDescriptors.Length; i++)
            {
                var enemy = layout.EnemySpawnDescriptors[i];
                if (enemy == null || !Finite(enemy.X) || !Finite(enemy.Z))
                    throw new ArgumentException("INVALID_SPAWN_SOURCE");
                bool blocked = false;
                for (int c = 0; c < covers.Length; c++)
                {
                    if (!removed[c] && HorizontalEyeSegmentIntersectsBox(
                          enemy.X, enemy.Z, x, z, covers[c], layout.EyeY))
                    { blocked = true; break; }
                }
                if (!blocked) visible.Add(i);
            }
            return visible.ToArray();
        }

        public bool HeavyReferenceClear(double x, double z)
        {
            // Reproduces exact P17 reference shape on ORIGINAL zero-rotation
            // CombatArena source XZ. Not a swept-wheel certificate.
            const double halfWidth = 5.180 / 2.0 + 0.25;
            const double halfLength = 5.8 / 2.0 + 0.25;
            if (!Finite(x) || !Finite(z)) return false;
            foreach (double dx in new[] { -halfWidth, 0.0, halfWidth })
                foreach (double dz in new[] { -halfLength, 0.0, halfLength })
                    if (Math.Abs(x + dx) > layout.FloorHalfX ||
                        Math.Abs(z + dz) > layout.FloorHalfZ) return false;
            foreach (var box in boundaries)
                if (Math.Abs(box.X - x) < box.Width / 2.0 + halfWidth &&
                    Math.Abs(box.Z - z) < box.Depth / 2.0 + halfLength) return false;
            for (int i = 0; i < covers.Length; i++)
                if (!removed[i] &&
                    Math.Abs(covers[i].X - x) < covers[i].Width / 2.0 + halfWidth &&
                    Math.Abs(covers[i].Z - z) < covers[i].Depth / 2.0 + halfLength)
                    return false;
            return true;
        }

        public SourceCell[] ExposureGrid4m()
        {
            var cells = new List<SourceCell>();
            for (int x = -24; x <= 24; x += 4)
                for (int z = -24; z <= 24; z += 4)
                    if (HeavyReferenceClear(x, z))
                        cells.Add(new SourceCell {
                            X = x, Z = z, VisibleEnemyIndices = VisibleEnemyDescriptors(x, z)
                        });
            return cells.ToArray();
        }

        /// <summary>
        /// Self-contained input integrity checksum, NOT a signature/authentication.
        /// The receipt only contains destroyed ordinals. Never serializes live Unity objects.
        /// </summary>
        public string ExportReceipt()
        {
            string csv = string.Join(",", Enumerable.Range(0, covers.Length)
                                      .Where(i => removed[i])
                                      .Select(i => i.ToString(CultureInfo.InvariantCulture)));
            string payload = ReceiptVersion + "|" + SourceLayoutId + "|" +
                             OriginalMapGeometryHash + "|" + csv;
            return payload + "|" + Sha256Hex(payload);
        }

        public bool TryImportReceipt(string receipt, out string failure)
        {
            failure = "";
            if (string.IsNullOrEmpty(receipt)) { failure = "EMPTY"; return false; }
            var parts = receipt.Split('|');
            if (parts.Length != 5 || parts[0] != ReceiptVersion)
            { failure = "UNSUPPORTED_RECEIPT"; return false; }
            if (parts[1] != SourceLayoutId || parts[2] != OriginalMapGeometryHash)
            { failure = "WRONG_MAP"; return false; }
            string payload = string.Join("|", parts.Take(4));
            if (!string.Equals(parts[4], Sha256Hex(payload), StringComparison.Ordinal))
            { failure = "CHECKSUM"; return false; }
            var prospective = new bool[covers.Length];
            int last = -1;
            if (parts[3].Length > 0)
            {
                foreach (string token in parts[3].Split(','))
                {
                    int ordinal;
                    if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out ordinal)
                        || ordinal < 0 || ordinal >= covers.Length
                        || ordinal <= last
                        || token != ordinal.ToString(CultureInfo.InvariantCulture))
                    { failure = "INVALID_CANONICAL_COVER_ORDINALS"; return false; }
                    prospective[ordinal] = true;
                    last = ordinal;
                }
            }
            // Transactional commit only after all fields have been validated.
            Array.Copy(prospective, removed, prospective.Length);
            return true;
        }

        private void CheckOrdinal(int ordinal)
        {
            if (ordinal < 0 || ordinal >= covers.Length)
                throw new ArgumentOutOfRangeException(nameof(ordinal),
                    "INVALID_SOURCE_COVER_ORDINAL");
        }

        private static bool HorizontalEyeSegmentIntersectsBox(
            double ax, double az, double bx, double bz, SourceBox box, double eyeY)
        {
            if (eyeY < box.Y - box.Height / 2.0 || eyeY > box.Y + box.Height / 2.0)
                return false;
            double near = 0.0, far = 1.0;
            if (!Clip(ax, bx, box.X - box.Width / 2.0, box.X + box.Width / 2.0,
                      ref near, ref far)) return false;
            if (!Clip(az, bz, box.Z - box.Depth / 2.0, box.Z + box.Depth / 2.0,
                      ref near, ref far)) return false;
            // Cover exactly at source/target endpoint does not count.
            return near < 1.0 && far > 0.0;
        }

        private static bool Clip(double start, double end, double min, double max,
                                 ref double near, ref double far)
        {
            double travel = end - start;
            if (Math.Abs(travel) < 1e-12) return start >= min && start <= max;
            double a = (min - start) / travel, b = (max - start) / travel;
            if (a > b) { double temp = a; a = b; b = temp; }
            near = Math.Max(near, a);
            far = Math.Min(far, b);
            return far >= near;
        }

        private static bool Finite(double d) { return !(double.IsNaN(d) || double.IsInfinity(d)); }
        private static bool IsHex(string s) { return s.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')); }
        private static string Sha256Hex(string s)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.ASCII.GetBytes(s));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
