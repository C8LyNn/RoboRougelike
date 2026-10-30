using System;
using System.Collections.Generic;

namespace B01.DebrisRepulse001
{
    public readonly struct Vector3d
    {
        public readonly double X, Y, Z;
        public Vector3d(double x, double y, double z) { X = x; Y = y; Z = z; }
        public double MagnitudeSquared => X * X + Y * Y + Z * Z;
        public static Vector3d operator -(Vector3d a, Vector3d b) => new Vector3d(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static Vector3d operator *(Vector3d a, double scalar) => new Vector3d(a.X*scalar,a.Y*scalar,a.Z*scalar);
        public static double Dot(Vector3d a, Vector3d b) => a.X*b.X+a.Y*b.Y+a.Z*b.Z;
    }

    /// <summary>Deterministic outward impulse on the first valid detach event per stable part ID.</summary>
    public sealed class DebrisImpulseLedger
    {
        private readonly HashSet<long> seen = new HashSet<long>();
        private const double Diagonal = 0.7071067811865475244;
        public int Count => seen.Count;
        public bool Contains(long id) => seen.Contains(id);
        public bool RearmAfterReattach(long id) => id > 0 && seen.Remove(id);
        public void ClearForNewRun() => seen.Clear();

        private static bool Valid(double x, double max) =>
            !double.IsNaN(x) && !double.IsInfinity(x) && x >= -max && x <= max;
        private static bool ValidVector(Vector3d v, double max) =>
            Valid(v.X,max) && Valid(v.Y,max) && Valid(v.Z,max);

        private static Vector3d CoincidentFallback(long id)
        {
            switch ((int)(id & 7L))
            {
                case 0: return new Vector3d(1,0,0);
                case 1: return new Vector3d(Diagonal,0,Diagonal);
                case 2: return new Vector3d(0,0,1);
                case 3: return new Vector3d(-Diagonal,0,Diagonal);
                case 4: return new Vector3d(-1,0,0);
                case 5: return new Vector3d(-Diagonal,0,-Diagonal);
                case 6: return new Vector3d(0,0,-1);
                default: return new Vector3d(Diagonal,0,-Diagonal);
            }
        }

        /// <summary>
        /// World metres, m/s, kilograms; returns Newton-second impulse. Invalid input
        /// never consumes the part ID. First valid event consumes even if impulse=0.
        /// </summary>
        public bool TryDetach(long id, Vector3d ownerCentre, Vector3d partCentre,
            Vector3d linearVelocity, double massKg, double desiredSpeed,
            double maxAddedSpeed, out Vector3d impulse)
        {
            impulse = new Vector3d(0,0,0);
            if (id <= 0 || seen.Contains(id) ||
                !ValidVector(ownerCentre,1000000) || !ValidVector(partCentre,1000000) ||
                !ValidVector(linearVelocity,10000) ||
                !Valid(massKg,10000) || massKg <= 0 ||
                !Valid(desiredSpeed,50) || desiredSpeed < 0 ||
                !Valid(maxAddedSpeed,50) || maxAddedSpeed < 0)
                return false;

            Vector3d delta = partCentre - ownerCentre;
            double lengthSq = delta.MagnitudeSquared;
            Vector3d outward = lengthSq <= 1e-12
                ? CoincidentFallback(id)
                : delta * (1.0 / Math.Sqrt(lengthSq));
            double needed = desiredSpeed - Vector3d.Dot(linearVelocity,outward);
            if (needed < 0) needed = 0;
            if (needed > maxAddedSpeed) needed = maxAddedSpeed;
            Vector3d candidate = outward * (massKg * needed);
            if (!ValidVector(candidate,10000000)) return false;
            seen.Add(id);
            impulse = candidate;
            return true;
        }
    }
}
