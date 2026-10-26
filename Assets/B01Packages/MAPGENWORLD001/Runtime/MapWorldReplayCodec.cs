using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using B01.MAPGEN001;

namespace B01.MAPGENWORLD001
{
    /// <summary>
    /// A portable, deterministic replay receipt for this version of MAPGEN.
    /// The checksum detects accidental corruption, NOT adversarial forgery.
    /// Never certifies PhysX traversal or authoritative R3 vehicle dimensions.
    /// </summary>
    public static class MapWorldReplayCodec
    {
        public const string Format = "MW001R1";
        private const int FieldCount = 15;
        private const int MaxLength = 1024;
        private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        // Called only after the adapter has confirmed that the producer built
        // the very same blueprint. The request includes a measured rig envelope.
        internal static string Encode(MapWorldRequest request, MapBlueprint plan)
        {
            if (request == null || plan == null || !request.EnvelopeMeasured
                || !Hex64(plan.GeometryHash)) throw new ArgumentException("INVALID_REPLAY_SOURCE");
            var v = request.Vehicle;
            var c = request.Config;
            string[] parts = {
                Format,
                ((int)request.Recipe).ToString(CI),
                request.Seed.ToString(CI),
                c.Segments.ToString(CI),
                c.ObstacleDensity.ToString(CI),
                c.Difficulty.ToString("R", CI),
                c.LaneWidth.ToString("R", CI),
                v.Width.ToString("R", CI),
                v.Length.ToString("R", CI),
                v.MinTurnRadius.ToString("R", CI),
                v.MaxSlopeDegrees.ToString("R", CI),
                request.ClearancePerSideMeters.ToString("R", CI),
                plan.GeometryHash,
                plan.GenerationVersion.ToString(CI)
            };
            string body = string.Join("|", parts);
            return body + "|" + Sha256(body);
        }

        /// <summary>
        /// Rejects corrupt input, impossible measured profiles, unknown versions
        /// and layout drift BEFORE any scene modification.
        /// </summary>
        public static bool TryDecode(string receipt, out MapWorldRequest request,
            out MapBlueprint plan, out MapWorldFailure failure)
        {
            request = null;
            plan = null;
            failure = MapWorldFailure.InvalidReplayReceipt;
            if (string.IsNullOrEmpty(receipt) || receipt.Length > MaxLength
                || receipt.IndexOf('\n') >= 0 || receipt.IndexOf('\r') >= 0) return false;

            var x = receipt.Split('|');
            if (x.Length != FieldCount || x[0] != Format
                || !Hex64(x[12]) || !Hex64(x[14])) return false;
            int mismatch = 0;
            string digest = Sha256(string.Join("|", x, 0, FieldCount - 1));
            for (int i = 0; i < 64; ++i) mismatch |= digest[i] ^ x[14][i];
            if (mismatch != 0) return false;

            if (!Int(x[1], out int recipe) || recipe < 0 || recipe > 1
                || !Int(x[2], out int seed)
                || !Int(x[3], out int segments)
                || !Int(x[4], out int density)
                || !Float(x[5], out float difficulty)
                || !Float(x[6], out float lane)
                || !Float(x[7], out float width)
                || !Float(x[8], out float length)
                || !Float(x[9], out float turning)
                || !Float(x[10], out float slope)
                || !Float(x[11], out float clearance)
                || !Int(x[13], out int generationVersion)) return false;

            // No acceptance based merely on a checksum: the REAL generator
            // revalidates the measured hull and reconstructs its own layout.
            var candidate = new MapWorldRequest {
                Recipe = (MapRecipe)recipe, Seed = seed,
                Config = new MapGenConfig {
                    Segments = segments, ObstacleDensity = density,
                    Difficulty = difficulty, LaneWidth = lane
                },
                Vehicle = new MapVehicleProfile {
                    Width = width, Length = length,
                    MinTurnRadius = turning, MaxSlopeDegrees = slope
                },
                EnvelopeMeasured = true, ClearancePerSideMeters = clearance
            };
            if (!MapWorldPolicy.TryPlan(candidate, out var current, out failure))
                return false;

            if (current.GenerationVersion != generationVersion
                || !string.Equals(current.GeometryHash, x[12], StringComparison.Ordinal))
            {
                failure = MapWorldFailure.ReplayGeometryMismatch;
                return false;
            }
            request = candidate;
            plan = current;
            failure = MapWorldFailure.None;
            return true;
        }

        private static bool Int(string s, out int value) =>
            int.TryParse(s, NumberStyles.AllowLeadingSign, CI, out value);

        private static bool Float(string s, out float value)
        {
            if (!float.TryParse(s, NumberStyles.Float, CI, out value)) return false;
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool Hex64(string s)
        {
            if (s == null || s.Length != 64) return false;
            foreach (char c in s)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
            return true;
        }

        private static string Sha256(string source)
        {
            using (var digest = SHA256.Create())
            {
                var bytes = digest.ComputeHash(Encoding.UTF8.GetBytes(source));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
