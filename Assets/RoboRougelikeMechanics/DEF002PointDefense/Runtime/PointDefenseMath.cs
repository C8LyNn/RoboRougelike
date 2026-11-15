using System;
using UnityEngine;

namespace RoboRougelike.Mechanics.PointDefense
{
    /// <summary>Pure math: can be tested without scenes or changing global RNG.</summary>
    public static class PointDefenseMath
    {
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static bool Finite(Vector3 value) { return Finite(value.x) && Finite(value.y) && Finite(value.z); }
        public static float ClampPositive(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) || value <= 0f ? fallback : value;
        }

        public static bool IsThreatApproaching(Vector3 position, Vector3 velocity,
            Vector3 defendedPoint, Vector3 defendedVelocity, float lookAhead,
            float defendedRadius, out float secondsToClosest)
        {
            secondsToClosest = 0f;
            if (!Finite(position) || !Finite(velocity) || !Finite(defendedPoint) ||
                !Finite(defendedVelocity) || !Finite(lookAhead) || !Finite(defendedRadius) || defendedRadius < 0f)
                return false;
            double ox = (double)position.x - defendedPoint.x, oy = (double)position.y - defendedPoint.y,
                oz = (double)position.z - defendedPoint.z;
            double radiusSquared = (double)defendedRadius * defendedRadius;
            // A threat already in the defended radius is immediate, even if
            // a slow approach puts its closest point beyond the look-ahead.
            if (ox * ox + oy * oy + oz * oz <= radiusSquared) return true;
            double vx = (double)velocity.x - defendedVelocity.x, vy = (double)velocity.y - defendedVelocity.y,
                vz = (double)velocity.z - defendedVelocity.z;
            double square = vx * vx + vy * vy + vz * vz;
            if (square <= 0d || lookAhead <= 0f) return false;
            double closest = -(ox * vx + oy * vy + oz * vz) / square;
            if (closest <= 0d) return false;
            // The shot can enter the sphere before reaching its closest point.
            double sample = Math.Min(closest, lookAhead);
            double dx = ox + vx * sample, dy = oy + vy * sample, dz = oz + vz * sample;
            secondsToClosest = (float)sample;
            return dx * dx + dy * dy + dz * dz <= radiusSquared;
        }

        /// <summary>Analytical constant-velocity interception in shooter-relative frame.</summary>
        public static bool TryLead(Vector3 shooter, Vector3 shooterVelocity, Vector3 target,
            Vector3 targetVelocity, float projectileSpeed, float maxLeadTime,
            out Vector3 aimPoint, out float flightTime)
        {
            aimPoint = target;
            flightTime = 0f;
            if (!Finite(shooter) || !Finite(shooterVelocity) || !Finite(target) || !Finite(targetVelocity) ||
                !Finite(projectileSpeed) || !Finite(maxLeadTime) || projectileSpeed <= 0f || maxLeadTime <= 0f)
                return false;
            double rx = (double)target.x - shooter.x, ry = (double)target.y - shooter.y, rz = (double)target.z - shooter.z;
            double vx = (double)targetVelocity.x - shooterVelocity.x, vy = (double)targetVelocity.y - shooterVelocity.y,
                vz = (double)targetVelocity.z - shooterVelocity.z;
            double a = vx * vx + vy * vy + vz * vz - (double)projectileSpeed * projectileSpeed;
            double b = 2d * (rx * vx + ry * vy + rz * vz);
            double c = rx * rx + ry * ry + rz * rz;
            if (c == 0d) return true;
            double t;
            if (a == 0d)
            {
                if (b == 0d) return false;
                t = -c / b;
            }
            else
            {
                double disc = b * b - 4d * a * c;
                if (disc < 0d) return false;
                // Avoid subtracting nearly equal values when one root is small.
                double root = Math.Sqrt(disc);
                double q = -.5d * (b + (b >= 0d ? root : -root));
                double t0 = q / a, t1 = q == 0d ? double.PositiveInfinity : c / q;
                t = t0 > 0d && t1 > 0d ? Math.Min(t0, t1) : Math.Max(t0, t1);
            }
            if (double.IsNaN(t) || double.IsInfinity(t) || t <= 0d || t > maxLeadTime) return false;
            float resultTime = (float)t;
            double px = target.x + targetVelocity.x * t, py = target.y + targetVelocity.y * t,
                pz = target.z + targetVelocity.z * t;
            if (resultTime <= 0f || !Finite(resultTime) || Math.Abs(px) > float.MaxValue ||
                Math.Abs(py) > float.MaxValue || Math.Abs(pz) > float.MaxValue) return false;
            flightTime = resultTime;
            aimPoint = new Vector3((float)px, (float)py, (float)pz);
            return true;
        }

        public static bool IsAligned(Vector3 facing, Vector3 desired, float toleranceDegrees)
        {
            return desired.sqrMagnitude > 0.000001f &&
                   Vector3.Angle(facing, desired) <= Mathf.Max(0f, toleranceDegrees);
        }
    }
}
